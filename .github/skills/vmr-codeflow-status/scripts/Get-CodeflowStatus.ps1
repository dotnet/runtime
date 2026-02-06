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
    param([string]$Endpoint)
    try {
        $result = gh api $Endpoint 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "GitHub API call failed: $Endpoint"
            return $null
        }
        return $result | ConvertFrom-Json
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

function ConvertFrom-Base64Content {
    param([string]$Content)
    try {
        $clean = $Content -replace '\s', ''
        return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($clean))
    }
    catch {
        Write-Warning "Failed to decode Base64 content: $_"
        return $null
    }
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
$repoOwner = $repoParts[0]
$repoName = $repoParts[1]

# --- Step 1: Get PR details ---
Write-Section "Codeflow PR #$PRNumber in $Repository"

$pr = Invoke-GitHubApi "/repos/$Repository/pulls/$PRNumber"
if (-not $pr) {
    Write-Error "Could not fetch PR #$PRNumber from $Repository"
    return
}

Write-Status "Title" $pr.title
Write-Status "State" $pr.state
Write-Status "Branch" "$($pr.head.ref) -> $($pr.base.ref)"
Write-Status "Created" $pr.created_at
Write-Status "Updated" $pr.updated_at
Write-Host "  URL: $($pr.html_url)"

# Check if this is actually a codeflow PR
$isMaestroPR = $pr.user.login -eq "dotnet-maestro[bot]"
$isCodeflowPR = $pr.title -match "Source code updates from dotnet/dotnet"
if (-not $isMaestroPR -and -not $isCodeflowPR) {
    Write-Warning "This does not appear to be a codeflow PR (author: $($pr.user.login), title: $($pr.title))"
    Write-Warning "Expected author 'dotnet-maestro[bot]' and title containing 'Source code updates from dotnet/dotnet'"
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

# Extract VMR commit
$vmrCommit = $null
if ($body -match '\*\*Commit\*\*:\s*\[([a-f0-9]+)\]') {
    $vmrCommit = $Matches[1]
    Write-Status "VMR Commit" $vmrCommit
}

# Extract build info
if ($body -match '\*\*Build\*\*:\s*\[([^\]]+)\]\(([^\)]+)\)') {
    Write-Status "Build" "$($Matches[1])"
    Write-Status "Build URL" $Matches[2]
}

# Extract date produced
if ($body -match '\*\*Date Produced\*\*:\s*(.+)') {
    Write-Status "Date Produced" $Matches[1].Trim()
}

# Extract VMR branch
$vmrBranch = $null
if ($body -match '\*\*Branch\*\*:\s*\[([^\]]+)\]') {
    $vmrBranch = $Matches[1]
    Write-Status "VMR Branch" $vmrBranch
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
        $vmrBranch = $pr.base.ref
        Write-Status "Inferred VMR Branch" $vmrBranch "(from PR target)"
    }
}

# --- Step 3: Check VMR freshness ---
Write-Section "VMR Freshness"

$vmrHeadSha = $null
$aheadBy = 0

if ($vmrCommit -and $vmrBranch) {
    # Get current VMR branch HEAD
    $vmrHead = Invoke-GitHubApi "/repos/dotnet/dotnet/commits/$vmrBranch"
    if ($vmrHead) {
        $vmrHeadSha = $vmrHead.sha
        $vmrHeadDate = $vmrHead.commit.committer.date
        Write-Status "PR snapshot" "$(Get-ShortSha $vmrCommit) (from PR body)"
        Write-Status "VMR HEAD" "$(Get-ShortSha $vmrHeadSha) ($vmrHeadDate)"

        if ($vmrCommit -eq $vmrHeadSha -or $vmrHeadSha.StartsWith($vmrCommit)) {
            Write-Host "  ✅ PR is up to date with VMR branch" -ForegroundColor Green
        }
        else {
            # Compare to find how many commits ahead the VMR is
            $compare = Invoke-GitHubApi "/repos/dotnet/dotnet/compare/$(Get-ShortSha $vmrCommit)...$(Get-ShortSha $vmrHeadSha)"
            if ($compare) {
                $aheadBy = $compare.ahead_by
                $behindBy = $compare.behind_by
                Write-Host "  ⚠️  VMR is $aheadBy commit(s) ahead of the PR snapshot" -ForegroundColor Yellow

                if ($ShowCommits -and $compare.commits) {
                    Write-Host ""
                    Write-Host "  Commits since PR snapshot:" -ForegroundColor Yellow
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
    Write-Warning "Cannot check VMR freshness without VMR commit and branch info"
}

# --- Step 4: Check staleness warnings ---
Write-Section "Staleness Check"

$comments = Invoke-GitHubApi "/repos/$Repository/issues/$PRNumber/comments?per_page=100"
$stalenessWarnings = @()
$lastStalenessComment = $null

if ($comments) {
    if ($comments.Count -ge 100) {
        Write-Warning "PR has 100+ comments — staleness warnings beyond the first page may be missed"
    }
    foreach ($comment in $comments) {
        if ($comment.user.login -eq "dotnet-maestro[bot]" -and
            ($comment.body -match "codeflow cannot continue" -or $comment.body -match "darc trigger-subscriptions")) {
            $stalenessWarnings += $comment
            $lastStalenessComment = $comment
        }
    }
}

if ($stalenessWarnings.Count -gt 0) {
    Write-Host "  ⚠️  Staleness warning detected ($($stalenessWarnings.Count) warning(s))" -ForegroundColor Yellow
    Write-Status "Latest warning" $lastStalenessComment.created_at
    Write-Host "  The VMR received opposite codeflow (forward flow merged) while this PR was open." -ForegroundColor Yellow
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

# --- Step 5: Analyze PR branch commits ---
Write-Section "PR Branch Analysis"

$manualCommits = @()
$prCommits = Invoke-GitHubApi "/repos/$Repository/pulls/$PRNumber/commits?per_page=100"
if ($prCommits) {
    if ($prCommits.Count -ge 100) {
        Write-Warning "PR has 100+ commits — commits beyond the first page may be missed"
    }
    $maestroCommits = @()
    $manualCommits = @()
    $mergeCommits = @()

    foreach ($c in $prCommits) {
        $msg = ($c.commit.message -split "`n")[0]
        $author = $c.commit.author.name

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
            $msg = ($c.commit.message -split "`n")[0]
            if ($msg.Length -gt 80) { $msg = $msg.Substring(0, 77) + "..." }
            Write-Host "    $($c.sha.Substring(0,8)) [$($c.commit.author.name)] $msg"
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

        # Check if the fix PR is merged
        $fixPR = Invoke-GitHubApi "/repos/$traceFullRepo/pulls/$traceNumber"
        if ($fixPR) {
            Write-Status "Fix PR" "${traceFullRepo}#${traceNumber}: $($fixPR.title)"
            Write-Status "State" $fixPR.state
            Write-Status "Merged" "$(if ($fixPR.merged) { '✅ Yes' } else { '❌ No' })" "$(if ($fixPR.merged) { 'Green' } else { 'Red' })"
            if ($fixPR.merged) {
                Write-Status "Merged at" $fixPR.merged_at
                Write-Status "Merge commit" $fixPR.merge_commit_sha
                $fixMergeCommit = $fixPR.merge_commit_sha
                $fixTargetBranch = $fixPR.base.ref
            }
        }

        # Check if the fix is in the VMR source-manifest.json on the target branch
        if ($fixPR.merged -and $vmrBranch) {
            Write-Host ""
            Write-Host "  Checking VMR source-manifest.json on $vmrBranch..." -ForegroundColor White

            $manifestUrl = "/repos/dotnet/dotnet/contents/src/source-manifest.json?ref=$vmrBranch"
            $manifestResponse = Invoke-GitHubApi $manifestUrl
            if ($manifestResponse -and $manifestResponse.content) {
                $manifestJson = ConvertFrom-Base64Content $manifestResponse.content
                if (-not $manifestJson) {
                    Write-Warning "Failed to decode source-manifest.json from VMR branch"
                }
                else {
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
                        $ancestorCheck = Invoke-GitHubApi "/repos/$traceFullRepo/compare/$(Get-ShortSha $fixMergeCommit)...$(Get-ShortSha $manifestCommit)"
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
                        $snapshotManifest = Invoke-GitHubApi $snapshotManifestUrl
                        if ($snapshotManifest -and $snapshotManifest.content) {
                            $snapshotJson = ConvertFrom-Base64Content $snapshotManifest.content
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
                                    $snapshotCheck = Invoke-GitHubApi "/repos/$traceFullRepo/compare/$(Get-ShortSha $fixMergeCommit)...$(Get-ShortSha $snapshotCommit)"
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
                }
                else {
                    Write-Warning "Could not find $traceRepo in VMR source-manifest.json"
                }
                }
            }
        }
    }
}

# --- Step 7: Recommendations ---
Write-Section "Recommendations"

$issues = @()
$recommendations = @()

# Summarize issues
if ($stalenessWarnings.Count -gt 0) {
    $issues += "Staleness warning active — codeflow is blocked"
}

if ($vmrCommit -and $vmrHead -and $vmrCommit -ne $vmrHeadSha -and -not $vmrHeadSha.StartsWith($vmrCommit)) {
    $issues += "VMR branch is ahead of PR snapshot ($aheadBy commits behind)"
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
