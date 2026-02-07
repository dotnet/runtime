<#
.SYNOPSIS
    Analyzes VMR codeflow PR status for dotnet repositories.

.DESCRIPTION
    Checks whether a codeflow PR (backflow from dotnet/dotnet VMR) is up to date,
    detects staleness warnings, traces specific fixes through the pipeline, and
    provides actionable recommendations.

    Can also check if a backflow PR is expected but missing for a given repo/branch.

.PARAMETER PRNumber
    GitHub PR number to analyze. Required unless -CheckMissing is used.

.PARAMETER Repository
    Target repository (default: dotnet/sdk). Format: owner/repo.

.PARAMETER TraceFix
    Optional. A repo PR to trace through the pipeline (e.g., "dotnet/runtime#123974").
    Checks if the fix has flowed through VMR into the codeflow PR.

.PARAMETER ShowCommits
    Show individual VMR commits between the PR snapshot and current branch HEAD.

.PARAMETER CheckMissing
    Check if backflow PRs are expected but missing for a repository. When used,
    PRNumber is not required. Finds the most recent merged backflow PR for each branch,
    extracts its VMR commit, and compares against current VMR branch HEAD.

.PARAMETER Branch
    Optional. When used with -CheckMissing, only check a specific branch instead of all.

.EXAMPLE
    ./Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk"

.EXAMPLE
    ./Get-CodeflowStatus.ps1 -PRNumber 52727 -Repository "dotnet/sdk" -TraceFix "dotnet/runtime#123974"

.EXAMPLE
    ./Get-CodeflowStatus.ps1 -Repository "dotnet/roslyn" -CheckMissing

.EXAMPLE
    ./Get-CodeflowStatus.ps1 -Repository "dotnet/roslyn" -CheckMissing -Branch "main"
#>

param(
    [int]$PRNumber,

    [string]$Repository = "dotnet/sdk",

    [string]$TraceFix,

    [switch]$ShowCommits,

    [switch]$CheckMissing,

    [string]$Branch
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
if ($Repository -notmatch '^[^/]+/[^/]+$') {
    Write-Error "Repository must be in format 'owner/repo' (e.g., 'dotnet/sdk')"
    return
}

# --- CheckMissing mode: find expected but missing backflow PRs ---
if ($CheckMissing) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Error "GitHub CLI (gh) is not installed or not in PATH. Install from https://cli.github.com/"
        return
    }

    Write-Section "Checking for missing backflow PRs in $Repository"

    # Find open backflow PRs (to know which branches are already covered)
    $openPRsJson = gh search prs --repo $Repository --author "dotnet-maestro[bot]" --state open "Source code updates from dotnet/dotnet" --json number,title --limit 50 2>$null
    $openPRs = @()
    if ($LASTEXITCODE -eq 0 -and $openPRsJson) {
        try { $openPRs = ($openPRsJson -join "`n") | ConvertFrom-Json } catch { $openPRs = @() }
    }
    $openBranches = @{}
    foreach ($opr in $openPRs) {
        if ($opr.title -match '^\[([^\]]+)\]') {
            $openBranches[$Matches[1]] = $opr.number
        }
    }

    if ($openPRs.Count -gt 0) {
        Write-Host "  Open backflow PRs already exist:" -ForegroundColor White
        foreach ($opr in $openPRs) {
            Write-Host "    #$($opr.number): $($opr.title)" -ForegroundColor Green
        }
        Write-Host ""
    }

    # Find recently merged backflow PRs to discover branches and VMR commit mapping
    $mergedPRsJson = gh search prs --repo $Repository --author "dotnet-maestro[bot]" --state closed --merged "Source code updates from dotnet/dotnet" --limit 30 --sort updated --json number,title,closedAt 2>$null
    $mergedPRs = @()
    if ($LASTEXITCODE -eq 0 -and $mergedPRsJson) {
        try { $mergedPRs = ($mergedPRsJson -join "`n") | ConvertFrom-Json } catch { $mergedPRs = @() }
    }

    if ($mergedPRs.Count -eq 0 -and $openPRs.Count -eq 0) {
        Write-Host "  No backflow PRs found (open or recently merged). This repo may not have backflow subscriptions." -ForegroundColor Yellow
        return
    }

    # Group merged PRs by branch, keeping only the most recent per branch
    $branchLastMerged = @{}
    foreach ($mpr in $mergedPRs) {
        if ($mpr.title -match '^\[([^\]]+)\]') {
            $branchName = $Matches[1]
            if ($Branch -and $branchName -ne $Branch) { continue }
            if (-not $branchLastMerged.ContainsKey($branchName)) {
                $branchLastMerged[$branchName] = $mpr
            }
        }
    }

    if ($Branch -and -not $branchLastMerged.ContainsKey($Branch) -and -not $openBranches.ContainsKey($Branch)) {
        Write-Host "  No backflow PRs found for branch '$Branch'." -ForegroundColor Yellow
        return
    }

    # For each branch without an open PR, check if VMR has moved past the last merged commit
    $missingCount = 0
    $coveredCount = 0
    $upToDateCount = 0

    foreach ($branchName in ($branchLastMerged.Keys | Sort-Object)) {
        $lastPR = $branchLastMerged[$branchName]
        Write-Host ""
        Write-Host "  Branch: $branchName" -ForegroundColor White

        if ($openBranches.ContainsKey($branchName)) {
            Write-Host "    ✅ Open backflow PR #$($openBranches[$branchName]) exists" -ForegroundColor Green
            $coveredCount++
            continue
        }

        # Get the PR body to extract VMR commit and VMR branch
        $prDetailJson = gh pr view $lastPR.number -R $Repository --json body 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "    ⚠️  Could not fetch PR #$($lastPR.number) details" -ForegroundColor Yellow
            continue
        }
        $prDetail = ($prDetailJson -join "`n") | ConvertFrom-Json

        $vmrCommitFromPR = $null
        $vmrBranchFromPR = $null
        if ($prDetail.body -match '\*\*Commit\*\*:\s*\[([a-fA-F0-9]+)\]') {
            $vmrCommitFromPR = $Matches[1]
        }
        if ($prDetail.body -match '\*\*Branch\*\*:\s*\[([^\]]+)\]') {
            $vmrBranchFromPR = $Matches[1]
        }

        if (-not $vmrCommitFromPR -or -not $vmrBranchFromPR) {
            Write-Host "    ⚠️  Could not parse VMR metadata from last merged PR #$($lastPR.number)" -ForegroundColor Yellow
            continue
        }

        Write-Host "    Last merged: PR #$($lastPR.number) on $($lastPR.closedAt)" -ForegroundColor DarkGray
        Write-Host "    VMR branch: $vmrBranchFromPR" -ForegroundColor DarkGray
        Write-Host "    VMR commit: $(Get-ShortSha $vmrCommitFromPR)" -ForegroundColor DarkGray

        # Get current VMR branch HEAD
        $encodedVmrBranch = [uri]::EscapeDataString($vmrBranchFromPR)
        $vmrHead = Invoke-GitHubApi "/repos/dotnet/dotnet/commits/$encodedVmrBranch"
        if (-not $vmrHead) {
            Write-Host "    ⚠️  Could not fetch VMR branch HEAD for $vmrBranchFromPR" -ForegroundColor Yellow
            continue
        }

        $vmrHeadSha = $vmrHead.sha
        $vmrHeadDate = $vmrHead.commit.committer.date

        if ($vmrCommitFromPR -eq $vmrHeadSha -or $vmrHeadSha.StartsWith($vmrCommitFromPR) -or $vmrCommitFromPR.StartsWith($vmrHeadSha)) {
            Write-Host "    ✅ VMR branch is at same commit — no backflow needed" -ForegroundColor Green
            $upToDateCount++
        }
        else {
            # Check how far ahead
            $compare = Invoke-GitHubApi "/repos/dotnet/dotnet/compare/$vmrCommitFromPR...$vmrHeadSha"
            $ahead = if ($compare) { $compare.ahead_by } else { "?" }

            Write-Host "    🔴 MISSING BACKFLOW PR" -ForegroundColor Red
            Write-Host "    VMR is $ahead commit(s) ahead since last merged PR" -ForegroundColor Yellow
            Write-Host "    VMR HEAD: $(Get-ShortSha $vmrHeadSha) ($vmrHeadDate)" -ForegroundColor DarkGray
            Write-Host "    Last merged VMR commit: $(Get-ShortSha $vmrCommitFromPR)" -ForegroundColor DarkGray

            # Check how long ago the last PR merged
            $mergedTime = [DateTimeOffset]::Parse($lastPR.closedAt).UtcDateTime
            $elapsed = [DateTime]::UtcNow - $mergedTime
            if ($elapsed.TotalHours -gt 6) {
                Write-Host "    ⚠️  Last PR merged $([math]::Round($elapsed.TotalHours, 1)) hours ago — Maestro may be stuck" -ForegroundColor Yellow
            }
            else {
                Write-Host "    ℹ️  Last PR merged $([math]::Round($elapsed.TotalHours, 1)) hours ago — Maestro may still be processing" -ForegroundColor DarkGray
            }
            $missingCount++
        }
    }

    # Also check open-only branches (that weren't in merged list)
    foreach ($branchName in ($openBranches.Keys | Sort-Object)) {
        if (-not $branchLastMerged.ContainsKey($branchName)) {
            if ($Branch -and $branchName -ne $Branch) { continue }
            Write-Host ""
            Write-Host "  Branch: $branchName" -ForegroundColor White
            Write-Host "    ✅ Open backflow PR #$($openBranches[$branchName]) exists" -ForegroundColor Green
            $coveredCount++
        }
    }

    Write-Section "Summary"
    Write-Host "  Branches with open backflow PRs: $coveredCount" -ForegroundColor Green
    Write-Host "  Branches up to date (no PR needed): $upToDateCount" -ForegroundColor Green
    if ($missingCount -gt 0) {
        Write-Host "  Branches MISSING backflow PRs: $missingCount" -ForegroundColor Red
    }
    else {
        Write-Host "  No missing backflow PRs detected ✅" -ForegroundColor Green
    }
    return
}

# --- Validate PRNumber for non-CheckMissing mode ---
if (-not $PRNumber) {
    Write-Error "PRNumber is required unless -CheckMissing is used."
    return
}

# --- Step 1: Get PR details (single call for PR + comments + commits) ---
Write-Section "Codeflow PR #$PRNumber in $Repository"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed or not in PATH. Install from https://cli.github.com/"
    return
}

$prJson = gh pr view $PRNumber -R $Repository --json body,title,state,author,headRefName,baseRefName,createdAt,updatedAt,url,comments,commits
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
if ($body -match '\*\*Commit\*\*:\s*\[([a-fA-F0-9]+)\]') {
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
$changeMatches = [regex]::Matches($body, '- (https://github\.com/([^/]+/[^/]+)/compare/([a-fA-F0-9]+)\.\.\.([a-fA-F0-9]+))')
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
        # For backflow: infer from PR target (which is the product repo branch = VMR branch name)
        # For forward flow: infer from PR head branch pattern or source repo context
        if ($isForwardFlow) {
            $vmrBranch = $pr.headRefName -replace '^darc-', '' -replace '-[a-f0-9-]+$', ''
            if (-not $vmrBranch) { $vmrBranch = $pr.baseRefName }
        }
        else {
            $vmrBranch = $pr.baseRefName
        }
        Write-Status "Inferred Branch" "$vmrBranch (from PR metadata)"
    }
}

# For backflow: compare against VMR (dotnet/dotnet) branch HEAD
# For forward flow: compare against product repo branch HEAD
$freshnessRepo = if ($isForwardFlow) { $sourceRepo } else { "dotnet/dotnet" }
$freshnessRepoLabel = if ($isForwardFlow) { $sourceRepo } else { "VMR" }

# Pre-load PR commits for use in validation and later analysis
$prCommits = $pr.commits

# --- Step 2b: Cross-reference PR body snapshot against actual branch commits ---
$branchVmrCommit = $null
if ($prCommits) {
    # Look through PR branch commits (newest first) for "Backflow from" or "Forward flow from" messages
    # containing the actual VMR/source SHA that was used to create the branch content
    $reversedCommits = @($prCommits)
    [Array]::Reverse($reversedCommits)
    foreach ($c in $reversedCommits) {
        $msg = $c.messageHeadline
        # Backflow commits: "Backflow from https://github.com/dotnet/dotnet / <sha> build <id>"
        if ($msg -match '(?:Backflow|Forward flow) from .+ / ([a-fA-F0-9]+)') {
            $branchVmrCommit = $Matches[1]
            # Keep scanning — we want the most recent (last in original order = first in reversed)
            break
        }
    }
}

if ($branchVmrCommit -or $vmrCommit) {
    Write-Section "Snapshot Validation"
    $usedBranchSnapshot = $false
    if ($branchVmrCommit -and $vmrCommit) {
        $bodyShort = Get-ShortSha $vmrCommit
        $branchShort = $branchVmrCommit  # already short from commit message
        if ($vmrCommit.StartsWith($branchVmrCommit) -or $branchVmrCommit.StartsWith($vmrCommit)) {
            Write-Host "  ✅ PR body snapshot ($bodyShort) matches branch commit ($branchShort)" -ForegroundColor Green
        }
        else {
            Write-Host "  ⚠️  MISMATCH: PR body claims $(Get-ShortSha $vmrCommit) but branch commit references $branchVmrCommit" -ForegroundColor Red
            Write-Host "  The PR body may be stale — using branch commit ($branchVmrCommit) for freshness check" -ForegroundColor Yellow
            # Resolve the short SHA from the branch commit to a full SHA for accurate comparison
            $resolvedCommit = Invoke-GitHubApi "/repos/$freshnessRepo/commits/$branchVmrCommit"
            if ($resolvedCommit) {
                $vmrCommit = $resolvedCommit.sha
                $usedBranchSnapshot = $true
            }
            else {
                Write-Host "  ⚠️  Could not resolve branch commit SHA $branchVmrCommit — falling back to PR body" -ForegroundColor Yellow
            }
        }
    }
    elseif ($branchVmrCommit -and -not $vmrCommit) {
        Write-Host "  ⚠️  PR body has no commit reference, but branch commit references $branchVmrCommit" -ForegroundColor Yellow
        Write-Host "  Using branch commit for freshness check" -ForegroundColor Yellow
        $resolvedCommit = Invoke-GitHubApi "/repos/$freshnessRepo/commits/$branchVmrCommit"
        if ($resolvedCommit) {
            $vmrCommit = $resolvedCommit.sha
            $usedBranchSnapshot = $true
        }
    }
    elseif ($vmrCommit -and -not $branchVmrCommit) {
        $commitCount = if ($prCommits) { $prCommits.Count } else { 0 }
        if ($commitCount -eq 1) {
            $firstMsg = $prCommits[0].messageHeadline
            if ($firstMsg -match "^Initial commit for subscription") {
                Write-Host "  ℹ️  PR has only an initial subscription commit — PR body snapshot ($(Get-ShortSha $vmrCommit)) not yet verifiable from branch" -ForegroundColor DarkGray
            }
            else {
                Write-Host "  ⚠️  No VMR SHA found in branch commit messages — trusting PR body ($(Get-ShortSha $vmrCommit))" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  ⚠️  No VMR SHA found in $commitCount branch commit messages — trusting PR body ($(Get-ShortSha $vmrCommit))" -ForegroundColor Yellow
        }
    }
}

# --- Step 3: Check source freshness ---
$freshnessLabel = if ($isForwardFlow) { "Source Freshness" } else { "VMR Freshness" }
Write-Section $freshnessLabel

$sourceHeadSha = $null
$aheadBy = 0
$behindBy = 0
$compareStatus = $null

if ($vmrCommit -and $vmrBranch) {
    # Get current branch HEAD (URL-encode branch name for path segments with /)
    $encodedBranch = [uri]::EscapeDataString($vmrBranch)
    $branchHead = Invoke-GitHubApi "/repos/$freshnessRepo/commits/$encodedBranch"
    if ($branchHead) {
        $sourceHeadSha = $branchHead.sha
        $sourceHeadDate = $branchHead.commit.committer.date
        $snapshotSource = if ($usedBranchSnapshot) { "from branch commit" } else { "from PR body" }
        Write-Status "PR snapshot" "$(Get-ShortSha $vmrCommit) ($snapshotSource)"
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
                    'identical' {
                        Write-Host "  ✅ PR is up to date with $freshnessRepoLabel branch" -ForegroundColor Green
                    }
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
                        Write-Host "    $(Get-ShortSha $c.sha 8) $date $msg"
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

                # --- For backflow PRs that are behind: check pending forward flow PRs ---
                if ($isBackflow -and $compareStatus -eq 'ahead' -and $aheadBy -gt 0 -and $vmrBranch) {
                    $forwardPRsJson = gh search prs --repo dotnet/dotnet --author "dotnet-maestro[bot]" --state open "Source code updates from" --base $vmrBranch --json number,title --limit 20 2>$null
                    $pendingForwardPRs = @()
                    if ($LASTEXITCODE -eq 0 -and $forwardPRsJson) {
                        try {
                            $allForward = ($forwardPRsJson -join "`n") | ConvertFrom-Json
                            # Filter to forward flow PRs (not backflow) targeting this VMR branch
                            $pendingForwardPRs = $allForward | Where-Object {
                                $_.title -match "Source code updates from (dotnet/\S+)" -and
                                $Matches[1] -ne "dotnet/dotnet"
                            }
                        }
                        catch {
                            Write-Warning "Failed to parse forward flow PR search results. Skipping forward flow analysis."
                        }
                    }

                    if ($pendingForwardPRs.Count -gt 0) {
                        Write-Host ""
                        Write-Host "  Pending forward flow PRs into VMR ($vmrBranch):" -ForegroundColor Cyan

                        $coveredRepos = @()
                        foreach ($fpr in $pendingForwardPRs) {
                            $fprSourceRepo = $null
                            if ($fpr.title -match "Source code updates from (dotnet/\S+)") {
                                $fprSourceRepo = $Matches[1]
                            }
                            $coveredLabel = ""
                            if ($fprSourceRepo -and $uniqueRepos -contains $fprSourceRepo) {
                                $coveredRepos += $fprSourceRepo
                                $coveredLabel = " ← covers missing updates"
                            }
                            Write-Host "    dotnet/dotnet#$($fpr.number): $($fpr.title)$coveredLabel" -ForegroundColor DarkGray
                        }

                        if ($coveredRepos.Count -gt 0) {
                            $uncoveredRepos = $uniqueRepos | Where-Object { $_ -notin $coveredRepos }
                            $coveredCount = $coveredRepos.Count
                            $totalMissing = $uniqueRepos.Count
                            Write-Host ""
                            Write-Host "  📊 Forward flow coverage: $coveredCount of $totalMissing missing repo(s) have pending forward flow PRs" -ForegroundColor Cyan
                            if ($uncoveredRepos.Count -gt 0) {
                                Write-Host "  Still waiting on: $($uncoveredRepos -join ', ')" -ForegroundColor Yellow
                            }
                            else {
                                Write-Host "  ✅ All missing repos have pending forward flow — gap should close once they merge + new backflow triggers" -ForegroundColor Green
                            }
                        }
                    }
                }
            }
        }
    }
}
else {
    Write-Warning "Cannot check freshness without source commit and branch info"
}

# --- Step 4: Check staleness and conflict warnings (using comments from gh pr view) ---
Write-Section "Staleness & Conflict Check"

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

$conflictWarnings = @()
$lastConflictComment = $null

if ($pr.comments) {
    foreach ($comment in $pr.comments) {
        $commentAuthor = $comment.author.login
        if ($commentAuthor -eq "dotnet-maestro[bot]" -or $commentAuthor -eq "dotnet-maestro") {
            if ($comment.body -match "Conflict detected") {
                $conflictWarnings += $comment
                $lastConflictComment = $comment
            }
        }
    }
}

if ($stalenessWarnings.Count -gt 0 -or $conflictWarnings.Count -gt 0) {
    if ($conflictWarnings.Count -gt 0) {
        Write-Host "  🔴 Conflict detected ($($conflictWarnings.Count) conflict warning(s))" -ForegroundColor Red
        Write-Status "Latest conflict" $lastConflictComment.createdAt

        # Extract conflicting files
        $conflictFiles = @()
        $fileMatches = [regex]::Matches($lastConflictComment.body, '-\s+`([^`]+)`\s*\r?\n')
        foreach ($fm in $fileMatches) {
            $conflictFiles += $fm.Groups[1].Value
        }
        if ($conflictFiles.Count -gt 0) {
            Write-Host "  Conflicting files:" -ForegroundColor Yellow
            foreach ($f in $conflictFiles) {
                Write-Host "    - $f" -ForegroundColor Yellow
            }
        }

        # Extract VMR commit from the conflict comment
        if ($lastConflictComment.body -match 'sources from \[`([a-fA-F0-9]+)`\]') {
            Write-Host "  Conflicting VMR commit: $($Matches[1])" -ForegroundColor DarkGray
        }

        # Extract resolve command
        if ($lastConflictComment.body -match '(darc vmr resolve-conflict --subscription [a-fA-F0-9-]+(?:\s+--build [a-fA-F0-9-]+)?)') {
            Write-Host ""
            Write-Host "  Resolve command:" -ForegroundColor White
            Write-Host "    $($Matches[1])" -ForegroundColor DarkGray
        }
    }

    if ($stalenessWarnings.Count -gt 0) {
        if ($conflictWarnings.Count -gt 0) { Write-Host "" }
        Write-Host "  ⚠️  Staleness warning detected ($($stalenessWarnings.Count) warning(s))" -ForegroundColor Yellow
        Write-Status "Latest warning" $lastStalenessComment.createdAt
        $oppositeFlow = if ($isForwardFlow) { "backflow from VMR merged into $sourceRepo" } else { "forward flow merged into VMR" }
        Write-Host "  Opposite codeflow ($oppositeFlow) while this PR was open." -ForegroundColor Yellow
        Write-Host "  Maestro has blocked further codeflow updates to this PR." -ForegroundColor Yellow

        # Extract darc commands from the warning
        if ($lastStalenessComment.body -match 'darc trigger-subscriptions --id ([a-fA-F0-9-]+)(?:\s+--force)?') {
            Write-Host ""
            Write-Host "  Suggested commands from Maestro:" -ForegroundColor White
            if ($lastStalenessComment.body -match '(darc trigger-subscriptions --id [a-fA-F0-9-]+)\s*\r?\n') {
                Write-Host "    Normal trigger: $($Matches[1])"
            }
            if ($lastStalenessComment.body -match '(darc trigger-subscriptions --id [a-fA-F0-9-]+ --force)') {
                Write-Host "    Force trigger:  $($Matches[1])"
            }
        }
    }
}
else {
    Write-Host "  ✅ No staleness or conflict warnings found" -ForegroundColor Green
}

# --- Step 5: Analyze PR branch commits (using commits from gh pr view) ---
Write-Section "PR Branch Analysis"

if ($prCommits) {
    $maestroCommits = @()
    $manualCommits = @()
    $mergeCommits = @()

    foreach ($c in $prCommits) {
        $msg = $c.messageHeadline
        $authorLogin = if ($c.authors -and $c.authors.Count -gt 0) { $c.authors[0].login } else { $null }
        $authorName = if ($c.authors -and $c.authors.Count -gt 0) { $c.authors[0].name } else { "unknown" }
        $author = if ($authorLogin) { $authorLogin } else { $authorName }

        if ($msg -match "^Merge branch") {
            $mergeCommits += $c
        }
        elseif ($author -in @("dotnet-maestro[bot]", "dotnet-maestro") -or $msg -eq "Update dependencies") {
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

    # Detect manual commits that look like codeflow-like changes (someone manually
    # doing what Maestro would do while flow is paused)
    $codeflowLikeManualCommits = @()
    foreach ($c in $manualCommits) {
        $msg = $c.messageHeadline
        if ($msg -match 'Update dependencies' -or
            $msg -match 'Version\.Details\.xml' -or
            $msg -match 'Versions\.props' -or
            $msg -match '[Bb]ackflow' -or
            $msg -match '[Ff]orward flow' -or
            $msg -match 'from dotnet/' -or
            $msg -match '[a-f0-9]{7,40}' -or
            $msg -match 'src/SourceBuild') {
            $codeflowLikeManualCommits += $c
        }
    }

    if ($codeflowLikeManualCommits.Count -gt 0 -and $stalenessWarnings.Count -gt 0) {
        Write-Host ""
        Write-Host "  ⚠️  $($codeflowLikeManualCommits.Count) manual commit(s) appear to contain codeflow-like changes while flow is paused" -ForegroundColor Yellow
        Write-Host "     The freshness gap reported above may be partially covered by these manual updates" -ForegroundColor DarkGray
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
                try {
                    $manifest = $manifestJson | ConvertFrom-Json
                }
                catch {
                    Write-Warning "Could not parse VMR source-manifest.json: $_"
                    $manifest = $null
                }

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
                    # For backflow: $vmrCommit is a VMR SHA, use it directly
                    # For forward flow: $vmrCommit is a source repo SHA, use PR head commit in dotnet/dotnet instead
                    $snapshotRef = $vmrCommit
                    if ($isForwardFlow -and $pr.commits -and $pr.commits.Count -gt 0) {
                        $snapshotRef = $pr.commits[-1].oid
                    }
                    if ($snapshotRef) {
                        Write-Host ""
                        Write-Host "  Checking if fix is in the PR's snapshot..." -ForegroundColor White

                        $snapshotManifestUrl = "/repos/dotnet/dotnet/contents/src/source-manifest.json?ref=$snapshotRef"
                        $snapshotJson = Invoke-GitHubApi $snapshotManifestUrl -Raw
                        if ($snapshotJson) {
                            try {
                                $snapshotData = $snapshotJson | ConvertFrom-Json
                            }
                            catch {
                                Write-Warning "Could not parse snapshot manifest: $_"
                                $snapshotData = $null
                            }

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
if ($conflictWarnings.Count -gt 0) {
    $fileHint = if ($conflictFiles -and $conflictFiles.Count -gt 0) { " in $($conflictFiles -join ', ')" } else { "" }
    $issues += "Conflict detected$fileHint — manual resolution required"
}

if ($stalenessWarnings.Count -gt 0) {
    $issues += "Staleness warning active — codeflow is blocked"
}

if ($vmrCommit -and $sourceHeadSha -and $vmrCommit -ne $sourceHeadSha -and $compareStatus -ne 'identical') {
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

    if ($conflictWarnings.Count -gt 0) {
        Write-Host "    1. Resolve conflicts — follow the darc vmr resolve-conflict instructions above" -ForegroundColor White
        if ($subscriptionId) {
            Write-Host "       darc vmr resolve-conflict --subscription $subscriptionId" -ForegroundColor DarkGray
        }
        Write-Host "    2. Close & reopen — abandon this PR and let Maestro create a fresh one" -ForegroundColor White
    }
    elseif ($stalenessWarnings.Count -gt 0 -and $manualCommits.Count -gt 0) {
        if ($codeflowLikeManualCommits -and $codeflowLikeManualCommits.Count -gt 0) {
            Write-Host "    ℹ️  Note: Some manual commits appear to contain codeflow-like changes —" -ForegroundColor DarkGray
            Write-Host "       the reported freshness gap may already be partially addressed" -ForegroundColor DarkGray
            Write-Host ""
        }
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
