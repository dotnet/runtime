<#
.SYNOPSIS
    Analyzes VMR codeflow PR status for dotnet repositories.

.DESCRIPTION
    Checks whether a codeflow PR (backflow from dotnet/dotnet VMR) is up to date,
    detects staleness warnings, traces specific fixes through the pipeline, and
    provides actionable recommendations.

.PARAMETER PRNumber
    GitHub PR number to analyze.

.PARAMETER Repository
    Target repository (default: dotnet/sdk). Format: owner/repo.

.PARAMETER TraceFix
    Optional. A repo PR to trace through the pipeline (e.g., "dotnet/runtime#123974").
    Checks if the fix has flowed through VMR into the codeflow PR.

.PARAMETER ShowCommits
    Show individual VMR commits between the PR snapshot and current branch HEAD.

.EXAMPLE
    ./Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk"

.EXAMPLE
    ./Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk" -TraceFix "dotnet/runtime#123974"
#>

param(
    [Parameter(Mandatory = $true)]
    [int]$PRNumber,

    [string]$Repository = "dotnet/sdk",

    [string]$TraceFix,

    [switch]$ShowCommits
)

$ErrorActionPreference = "Stop"

# --- Helpers ---

function Invoke-GitHubApi {
    param(
        [string]$Endpoint,
        [switch]$Raw
    )
    try {
        $args = @($Endpoint)
        if ($Raw) {
            $args += '-H'
            $args += 'Accept: application/vnd.github.raw'
        }
        $result = gh api @args 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "GitHub API call failed: $Endpoint"
            return $null
        }
        if ($Raw) { return $result -join "`n" }
        return ($result -join "`n") | ConvertFrom-Json
    }
    catch {
        Write-Warning "Error calling GitHub API: $_"
        return $null
    }
}

function Get-ShortSha {
    param([string]$Sha, [int]$Length = 12)
    if (-not $Sha) { return "(unknown)" }
    return $Sha.Substring(0, [Math]::Min($Length, $Sha.Length))
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

function Write-Status {
    param([string]$Label, [string]$Value, [string]$Color = "White")
    Write-Host "  ${Label}: " -NoNewline
    Write-Host $Value -ForegroundColor $Color
}

# --- Parse repo owner/name ---
$repoParts = $Repository -split '/'
if ($repoParts.Count -ne 2) {
    Write-Error "Repository must be in format 'owner/repo' (e.g., 'dotnet/sdk')"
    return
}

# --- Step 1: Get PR details (single call for PR + comments + commits) ---
Write-Section "Codeflow PR #$PRNumber in $Repository"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed or not in PATH. Install from https://cli.github.com/"
    return
}

$prJson = gh pr view $PRNumber -R $Repository --json body,title,state,author,headRefName,baseRefName,createdAt,updatedAt,url,comments,commits 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Could not fetch PR #$PRNumber from $Repository. Ensure you are authenticated (gh auth login)."
    return
}
$pr = ($prJson -join "`n") | ConvertFrom-Json

Write-Status "Title" $pr.title
Write-Status "State" $pr.state
Write-Status "Branch" "$($pr.headRefName) -> $($pr.baseRefName)"
Write-Status "Created" $pr.createdAt
Write-Status "Updated" $pr.updatedAt
Write-Host "  URL: $($pr.url)"

# Check if this is actually a codeflow PR and detect flow direction
$isMaestroPR = $pr.author.login -eq "dotnet-maestro[bot]"
$isBackflow = $pr.title -match "Source code updates from dotnet/dotnet"
$isForwardFlow = $pr.title -match "Source code updates from (dotnet/\S+)" -and -not $isBackflow
if (-not $isMaestroPR -and -not $isBackflow -and -not $isForwardFlow) {
    Write-Warning "This does not appear to be a codeflow PR (author: $($pr.author.login), title: $($pr.title))"
    Write-Warning "Expected author 'dotnet-maestro[bot]' and title containing 'Source code updates from'"
}

if ($isForwardFlow) {
    $sourceRepo = $Matches[1]
    Write-Status "Flow" "Forward ($sourceRepo → $Repository)" "Cyan"
}
elseif ($isBackflow) {
    Write-Status "Flow" "Backflow (dotnet/dotnet → $Repository)" "Cyan"
}

# --- Step 2: Parse PR body metadata ---
Write-Section "Codeflow Metadata"

$body = $pr.body

# Extract subscription ID
$subscriptionId = $null
if ($body -match '\(Begin:([a-f0-9-]+)\)') {
    $subscriptionId = $Matches[1]
    Write-Status "Subscription" $subscriptionId
}

# Extract source commit (VMR commit for backflow, repo commit for forward flow)
$sourceCommit = $null
if ($body -match '\*\*Commit\*\*:\s*\[([a-f0-9]+)\]') {
    $sourceCommit = $Matches[1]
    $commitLabel = if ($isForwardFlow) { "Source Commit" } else { "VMR Commit" }
    Write-Status $commitLabel $sourceCommit
}
# Keep $vmrCommit alias for backflow compatibility
$vmrCommit = $sourceCommit

# Extract build info
if ($body -match '\*\*Build\*\*:\s*\[([^\]]+)\]\(([^\)]+)\)') {
    Write-Status "Build" "$($Matches[1])"
    Write-Status "Build URL" $Matches[2]
}

# Extract date produced
if ($body -match '\*\*Date Produced\*\*:\s*(.+)') {
    Write-Status "Date Produced" $Matches[1].Trim()
}

# Extract source branch
$vmrBranch = $null
if ($body -match '\*\*Branch\*\*:\s*\[([^\]]+)\]') {
    $vmrBranch = $Matches[1]
    $branchLabel = if ($isForwardFlow) { "Source Branch" } else { "VMR Branch" }
    Write-Status $branchLabel $vmrBranch
}

# Extract commit diff
if ($body -match '\*\*Commit Diff\*\*:\s*\[([^\]]+)\]\(([^\)]+)\)') {
    Write-Status "Commit Diff" $Matches[1]
}

# Extract associated repo changes from footer
$repoChanges = @()
$changeMatches = [regex]::Matches($body, '- (https://github\.com/([^/]+/[^/]+)/compare/([a-f0-9]+)\.\.\.([a-f0-9]+))')
foreach ($m in $changeMatches) {
    $repoChanges += @{
        URL      = $m.Groups[1].Value
        Repo     = $m.Groups[2].Value
        FromSha  = $m.Groups[3].Value
        ToSha    = $m.Groups[4].Value
    }
}
if ($repoChanges.Count -gt 0) {
    Write-Status "Associated Repos" "$($repoChanges.Count) repos with source changes"
}

if (-not $vmrCommit -or -not $vmrBranch) {
    Write-Warning "Could not parse VMR metadata from PR body. This may not be a codeflow PR."
    if (-not $vmrBranch) {
        # Try to infer from the PR target branch
        $vmrBranch = $pr.baseRefName
        Write-Status "Inferred VMR Branch" "$vmrBranch (from PR target)"
    }
}

# --- Step 3: Check source freshness ---
$freshnessLabel = if ($isForwardFlow) { "Source Freshness" } else { "VMR Freshness" }
Write-Section $freshnessLabel

$sourceHeadSha = $null
$aheadBy = 0
$behindBy = 0
$compareStatus = $null

# For backflow: compare against VMR (dotnet/dotnet) branch HEAD
# For forward flow: compare against product repo branch HEAD
$freshnessRepo = if ($isForwardFlow) { $sourceRepo } else { "dotnet/dotnet" }
$freshnessRepoLabel = if ($isForwardFlow) { $sourceRepo } else { "VMR" }

if ($vmrCommit -and $vmrBranch) {
    # Get current branch HEAD (URL-encode branch name for path segments with /)
    $encodedBranch = [uri]::EscapeDataString($vmrBranch)
    $branchHead = Invoke-GitHubApi "/repos/$freshnessRepo/commits/$encodedBranch"
    if ($branchHead) {
        $sourceHeadSha = $branchHead.sha
        $sourceHeadDate = $branchHead.commit.committer.date
        Write-Status "PR snapshot" "$(Get-ShortSha $vmrCommit) (from PR body)"
        Write-Status "$freshnessRepoLabel HEAD" "$(Get-ShortSha $sourceHeadSha) ($sourceHeadDate)"

        if ($vmrCommit -eq $sourceHeadSha) {
            Write-Host "  ✅ PR is up to date with $freshnessRepoLabel branch" -ForegroundColor Green
        }
        else {
            # Compare to find how many commits differ
            $compare = Invoke-GitHubApi "/repos/$freshnessRepo/compare/$vmrCommit...$sourceHeadSha"
            if ($compare) {
                $aheadBy = $compare.ahead_by
                $behindBy = $compare.behind_by
                $compareStatus = $compare.status

                switch ($compareStatus) {
                    'ahead' {
                        Write-Host "  ⚠️  $freshnessRepoLabel is $aheadBy commit(s) ahead of the PR snapshot" -ForegroundColor Yellow
                    }
                    'behind' {
                        Write-Host "  ⚠️  $freshnessRepoLabel is $behindBy commit(s) behind the PR snapshot" -ForegroundColor Yellow
                    }
                    'diverged' {
                        Write-Host "  ⚠️  $freshnessRepoLabel and PR snapshot have diverged: $aheadBy commit(s) ahead and $behindBy commit(s) behind" -ForegroundColor Yellow
                    }
                    default {
                        Write-Host "  ⚠️  $freshnessRepoLabel and PR snapshot differ (status: $compareStatus)" -ForegroundColor Yellow
                    }
                }

                if ($compare.total_commits -and $compare.commits) {
                    $returnedCommits = @($compare.commits).Count
                    if ($returnedCommits -lt $compare.total_commits) {
                        Write-Host "  ⚠️  Compare API returned $returnedCommits of $($compare.total_commits) commits; listing may be incomplete." -ForegroundColor Yellow
                    }
                }

                if ($ShowCommits -and $compare.commits) {
                    Write-Host ""
                    $commitLabel = switch ($compareStatus) {
                        'ahead'  { "Commits since PR snapshot:" }
                        'behind' { "Commits in PR snapshot but not in $freshnessRepoLabel`:" }
                        default  { "Commits differing:" }
                    }
                    Write-Host "  $commitLabel" -ForegroundColor Yellow
                    foreach ($c in $compare.commits) {
                        $msg = ($c.commit.message -split "`n")[0]
                        if ($msg.Length -gt 100) { $msg = $msg.Substring(0, 97) + "..." }
                        $date = $c.commit.committer.date
                        Write-Host "    $($c.sha.Substring(0,8)) $date $msg"
                    }
                }

                # Check which repos have updates in the missing commits
                $missingRepoUpdates = @()
                if ($compare.commits) {
                    foreach ($c in $compare.commits) {
                        $msg = ($c.commit.message -split "`n")[0]
                        if ($msg -match 'Source code updates from ([^\s(]+)') {
                            $missingRepoUpdates += $Matches[1]
                        }
                    }
                }
                if ($missingRepoUpdates.Count -gt 0) {
                    $uniqueRepos = $missingRepoUpdates | Select-Object -Unique
                    Write-Host ""
                    Write-Host "  Missing updates from: $($uniqueRepos -join ', ')" -ForegroundColor Yellow
                }
            }
        }
    }
}
else {
    Write-Warning "Cannot check freshness without source commit and branch info"
}

# --- Step 4: Check staleness warnings (using comments from gh pr view) ---
Write-Section "Staleness Check"

$stalenessWarnings = @()
$lastStalenessComment = $null

if ($pr.comments) {
    foreach ($comment in $pr.comments) {
        $commentAuthor = $comment.author.login
        if ($commentAuthor -eq "dotnet-maestro[bot]" -or $commentAuthor -eq "dotnet-maestro") {
            if ($comment.body -match "codeflow cannot continue" -or $comment.body -match "darc trigger-subscriptions") {
                $stalenessWarnings += $comment
                $lastStalenessComment = $comment
            }
        }
    }
}

if ($stalenessWarnings.Count -gt 0) {
    Write-Host "  ⚠️  Staleness warning detected ($($stalenessWarnings.Count) warning(s))" -ForegroundColor Yellow
    Write-Status "Latest warning" $lastStalenessComment.createdAt
    $oppositeFlow = if ($isForwardFlow) { "backflow from VMR merged into $sourceRepo" } else { "forward flow merged into VMR" }
    Write-Host "  Opposite codeflow ($oppositeFlow) while this PR was open." -ForegroundColor Yellow
    Write-Host "  Maestro has blocked further codeflow updates to this PR." -ForegroundColor Yellow

    # Extract darc commands from the warning
    if ($lastStalenessComment.body -match 'darc trigger-subscriptions --id ([a-f0-9-]+)(?:\s+--force)?') {
        Write-Host ""
        Write-Host "  Suggested commands from Maestro:" -ForegroundColor White
        if ($lastStalenessComment.body -match '(darc trigger-subscriptions --id [a-f0-9-]+)\s*\n') {
            Write-Host "    Normal trigger: $($Matches[1])"
        }
        if ($lastStalenessComment.body -match '(darc trigger-subscriptions --id [a-f0-9-]+ --force)') {
            Write-Host "    Force trigger:  $($Matches[1])"
        }
    }
}
else {
    Write-Host "  ✅ No staleness warnings found" -ForegroundColor Green
}

# --- Step 5: Analyze PR branch commits (using commits from gh pr view) ---
Write-Section "PR Branch Analysis"

$manualCommits = @()
$prCommits = $pr.commits
if ($prCommits) {
    $maestroCommits = @()
    $manualCommits = @()
    $mergeCommits = @()

    foreach ($c in $prCommits) {
        $msg = $c.messageHeadline
        $author = if ($c.authors -and $c.authors.Count -gt 0) { $c.authors[0].name } else { "unknown" }

        if ($msg -match "^Merge branch") {
            $mergeCommits += $c
        }
        elseif ($author -eq "dotnet-maestro[bot]" -or $msg -eq "Update dependencies") {
            $maestroCommits += $c
        }
        else {
            $manualCommits += $c
        }
    }

    Write-Status "Total commits" $prCommits.Count
    Write-Status "Maestro auto-updates" $maestroCommits.Count
    Write-Status "Merge commits" $mergeCommits.Count
    Write-Status "Manual commits" $manualCommits.Count "$(if ($manualCommits.Count -gt 0) { 'Yellow' } else { 'Green' })"

    if ($manualCommits.Count -gt 0) {
        Write-Host ""
        Write-Host "  Manual commits (at risk if PR is closed/force-triggered):" -ForegroundColor Yellow
        foreach ($c in $manualCommits) {
            $msg = $c.messageHeadline
            if ($msg.Length -gt 80) { $msg = $msg.Substring(0, 77) + "..." }
            $authorName = if ($c.authors -and $c.authors.Count -gt 0) { $c.authors[0].name } else { "unknown" }
            Write-Host "    $(Get-ShortSha $c.oid 8) [$authorName] $msg"
        }
    }
}

# --- Step 6: Trace a specific fix (optional) ---
if ($TraceFix) {
    Write-Section "Tracing Fix: $TraceFix"

    # Parse TraceFix format: "owner/repo#number" or "repo#number"
    $traceMatch = [regex]::Match($TraceFix, '(?:([^/]+)/)?([^#]+)#(\d+)')
    if (-not $traceMatch.Success) {
        Write-Warning "Could not parse TraceFix format. Expected: 'owner/repo#number' or 'repo#number'"
    }
    else {
        $traceOwner = if ($traceMatch.Groups[1].Value) { $traceMatch.Groups[1].Value } else { "dotnet" }
        $traceRepo = $traceMatch.Groups[2].Value
        $traceNumber = $traceMatch.Groups[3].Value
        $traceFullRepo = "$traceOwner/$traceRepo"

        # Check if the fix PR is merged (use merged_at since REST may not include merged boolean)
        $fixPR = Invoke-GitHubApi "/repos/$traceFullRepo/pulls/$traceNumber"
        $fixIsMerged = $false
        if ($fixPR) {
            $fixIsMerged = $null -ne $fixPR.merged_at
            Write-Status "Fix PR" "${traceFullRepo}#${traceNumber}: $($fixPR.title)"
            Write-Status "State" $fixPR.state
            Write-Status "Merged" "$(if ($fixIsMerged) { '✅ Yes' } else { '❌ No' })" "$(if ($fixIsMerged) { 'Green' } else { 'Red' })"
            if ($fixIsMerged) {
                Write-Status "Merged at" $fixPR.merged_at
                Write-Status "Merge commit" $fixPR.merge_commit_sha
                $fixMergeCommit = $fixPR.merge_commit_sha
            }
        }

        # Check if the fix is in the VMR source-manifest.json on the target branch
        # For forward flow, the VMR target is the PR base branch; for backflow, use $vmrBranch
        $vmrManifestBranch = if ($isForwardFlow -and $pr.baseRefName) { $pr.baseRefName } else { $vmrBranch }
        if ($fixIsMerged -and $vmrManifestBranch) {
            Write-Host ""
            Write-Host "  Checking VMR source-manifest.json on $vmrManifestBranch..." -ForegroundColor White

            $encodedManifestBranch = [uri]::EscapeDataString($vmrManifestBranch)
            $manifestUrl = "/repos/dotnet/dotnet/contents/src/source-manifest.json?ref=$encodedManifestBranch"
            $manifestJson = Invoke-GitHubApi $manifestUrl -Raw
            if ($manifestJson) {
                $manifest = $manifestJson | ConvertFrom-Json

                # Find the repo in the manifest
                $escapedRepo = [regex]::Escape($traceRepo)
                $repoEntry = $manifest.repositories | Where-Object {
                    $_.remoteUri -match "${escapedRepo}(\.git)?$" -or $_.path -eq $traceRepo
                }

                if ($repoEntry) {
                    $manifestCommit = $repoEntry.commitSha
                    Write-Status "VMR manifest commit" "$(Get-ShortSha $manifestCommit) for $($repoEntry.path)"

                    # Check if the fix merge commit is an ancestor of the manifest commit
                    if ($fixMergeCommit -eq $manifestCommit) {
                        Write-Host "  ✅ Fix merge commit IS the VMR manifest commit" -ForegroundColor Green
                    }
                    else {
                        # Check if fix is an ancestor of the manifest commit
                        $ancestorCheck = Invoke-GitHubApi "/repos/$traceFullRepo/compare/$fixMergeCommit...$manifestCommit"
                        if ($ancestorCheck) {
                            if ($ancestorCheck.status -eq "ahead" -or $ancestorCheck.status -eq "identical") {
                                Write-Host "  ✅ Fix is included in VMR manifest (manifest is ahead or identical)" -ForegroundColor Green
                            }
                            elseif ($ancestorCheck.status -eq "behind") {
                                Write-Host "  ❌ Fix is NOT in VMR manifest yet (manifest is behind the fix)" -ForegroundColor Red
                            }
                            else {
                                Write-Host "  ⚠️  Fix and manifest have diverged (status: $($ancestorCheck.status))" -ForegroundColor Yellow
                            }
                        }
                    }

                    # Now check if the PR's VMR snapshot includes this
                    if ($vmrCommit) {
                        Write-Host ""
                        Write-Host "  Checking if fix is in the PR's VMR snapshot..." -ForegroundColor White

                        $snapshotManifestUrl = "/repos/dotnet/dotnet/contents/src/source-manifest.json?ref=$vmrCommit"
                        $snapshotJson = Invoke-GitHubApi $snapshotManifestUrl -Raw
                        if ($snapshotJson) {
                            $snapshotData = $snapshotJson | ConvertFrom-Json

                            $snapshotEntry = $snapshotData.repositories | Where-Object {
                                $_.remoteUri -match "${escapedRepo}(\.git)?$" -or $_.path -eq $traceRepo
                            }

                            if ($snapshotEntry) {
                                $snapshotCommit = $snapshotEntry.commitSha
                                Write-Status "PR snapshot commit" "$(Get-ShortSha $snapshotCommit) for $($snapshotEntry.path)"

                                if ($snapshotCommit -eq $fixMergeCommit) {
                                    Write-Host "  ✅ Fix IS in the PR's VMR snapshot" -ForegroundColor Green
                                }
                                else {
                                    $snapshotCheck = Invoke-GitHubApi "/repos/$traceFullRepo/compare/$fixMergeCommit...$snapshotCommit"
                                    if ($snapshotCheck) {
                                        if ($snapshotCheck.status -eq "ahead" -or $snapshotCheck.status -eq "identical") {
                                            Write-Host "  ✅ Fix is included in PR snapshot" -ForegroundColor Green
                                        }
                                        else {
                                            Write-Host "  ❌ Fix is NOT in the PR's VMR snapshot" -ForegroundColor Red
                                            Write-Host "  The PR needs a codeflow update to pick up this fix." -ForegroundColor Yellow
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    Write-Warning "Could not find $traceRepo in VMR source-manifest.json"
                }
            }
        }
    }
}

# --- Step 7: Recommendations ---
Write-Section "Recommendations"

$issues = @()

# Summarize issues
if ($stalenessWarnings.Count -gt 0) {
    $issues += "Staleness warning active — codeflow is blocked"
}

if ($vmrCommit -and $sourceHeadSha -and $vmrCommit -ne $sourceHeadSha) {
    switch ($compareStatus) {
        'ahead'    { $issues += "$freshnessRepoLabel is $aheadBy commit(s) ahead of PR snapshot" }
        'behind'   { $issues += "$freshnessRepoLabel is $behindBy commit(s) behind PR snapshot" }
        'diverged' { $issues += "$freshnessRepoLabel and PR snapshot diverged ($aheadBy ahead, $behindBy behind)" }
        default    { $issues += "$freshnessRepoLabel and PR snapshot differ" }
    }
}

if ($manualCommits -and $manualCommits.Count -gt 0) {
    $issues += "$($manualCommits.Count) manual commit(s) on PR branch"
}

if ($issues.Count -eq 0) {
    Write-Host "  ✅ CODEFLOW HEALTHY" -ForegroundColor Green
    Write-Host "  The PR appears to be up to date with no issues detected."
}
else {
    Write-Host "  ⚠️  CODEFLOW NEEDS ATTENTION" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Issues:" -ForegroundColor White
    foreach ($issue in $issues) {
        Write-Host "    • $issue" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "  Options:" -ForegroundColor White

    if ($stalenessWarnings.Count -gt 0 -and $manualCommits.Count -gt 0) {
        Write-Host "    1. Merge as-is — keep manual commits, get remaining changes in next codeflow PR" -ForegroundColor White
        Write-Host "    2. Force trigger — updates codeflow but may revert manual commits" -ForegroundColor White
        if ($subscriptionId) {
            Write-Host "       darc trigger-subscriptions --id $subscriptionId --force" -ForegroundColor DarkGray
        }
        Write-Host "    3. Close & reopen — loses manual commits, gets fresh codeflow" -ForegroundColor White
    }
    elseif ($stalenessWarnings.Count -gt 0) {
        Write-Host "    1. Merge as-is — get remaining changes in next codeflow PR" -ForegroundColor White
        Write-Host "    2. Close & reopen — gets fresh codeflow with all updates" -ForegroundColor White
        Write-Host "    3. Force trigger — forces codeflow update into this PR" -ForegroundColor White
        if ($subscriptionId) {
            Write-Host "       darc trigger-subscriptions --id $subscriptionId --force" -ForegroundColor DarkGray
        }
    }
    elseif ($manualCommits.Count -gt 0) {
        Write-Host "    1. Wait — Maestro should auto-update (if not stale)" -ForegroundColor White
        Write-Host "    2. Trigger manually — if auto-updates seem delayed" -ForegroundColor White
        if ($subscriptionId) {
            Write-Host "       darc trigger-subscriptions --id $subscriptionId" -ForegroundColor DarkGray
        }
    }
    else {
        Write-Host "    1. Wait — Maestro should auto-update the PR" -ForegroundColor White
        Write-Host "    2. Trigger manually — if auto-updates seem delayed" -ForegroundColor White
        if ($subscriptionId) {
            Write-Host "       darc trigger-subscriptions --id $subscriptionId" -ForegroundColor DarkGray
        }
    }
}
