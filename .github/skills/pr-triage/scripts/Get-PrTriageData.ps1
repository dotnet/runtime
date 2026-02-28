<#
.SYNOPSIS
    Fetches and scores open PRs in dotnet/runtime for merge readiness.
.DESCRIPTION
    Uses batched GraphQL to fetch reviews, review threads, and Build Analysis.
    Outputs scored JSON for the AI skill to format and annotate.
.PARAMETER Label
    Area label to filter by (e.g., "area-CodeGen-coreclr")
.PARAMETER Limit
    Maximum PRs to return from gh pr list (default 100)
.PARAMETER Repo
    Repository (default "dotnet/runtime")
.EXAMPLE
    .\Get-PrTriageData.ps1 -Label "area-CodeGen-coreclr"
#>
param(
    [string]$Label,
    [string]$Author,
    [string]$Assignee,
    [switch]$Community,
    [int]$MinAge,
    [int]$MaxAge,
    [int]$UpdatedWithin,
    [int]$MinApprovals,
    [double]$MinScore,
    [string]$HasLabel,
    [string]$ExcludeLabel,
    [switch]$IncludeDrafts,
    [switch]$ExcludeCopilot,
    [switch]$IncludeNeedsAuthor,
    [switch]$IncludeStale,
    [string]$MyActions,
    [string]$NextAction,
    [string]$PrNumber,
    [int]$Top = 0,
    [int]$Limit = 500,
    [string]$Repo = "dotnet/runtime",
    [switch]$OutputCsv
)

$ErrorActionPreference = "Stop"
$scriptStart = Get-Date

# --- Area owners lookup (parsed from docs/area-owners.md) ---
$areaOwners = @{}
$repoParts = $Repo -split '/'
$areaOwnersUrl = "repos/$($repoParts[0])/$($repoParts[1])/contents/docs/area-owners.md"
try {
    $areaOwnersMd = gh api -H "Accept: application/vnd.github.raw" $areaOwnersUrl 2>$null
    foreach ($line in $areaOwnersMd -split "`n") {
        if ($line -match '^\|\s*(area-\S+)\s*\|\s*@(\S+)\s*\|\s*(.+?)\s*\|') {
            $areaName = $matches[1].Trim()
            $lead = $matches[2].Trim()
            $ownerField = $matches[3].Trim()
            $people = @([regex]::Matches($ownerField, '@(\S+)') | ForEach-Object { $_.Groups[1].Value } |
                Where-Object { $_ -notmatch '^dotnet/' })
            if ($people.Count -eq 0) { $people = @($lead) }
            $areaOwners[$areaName] = $people
        }
    }
    Write-Host "Loaded $($areaOwners.Count) area owners from docs/area-owners.md" -ForegroundColor Cyan
} catch {
    Write-Host "Warning: could not fetch area-owners.md, using empty owner table" -ForegroundColor Yellow
}

$communityTriagers = @("a74nh","am11","clamp03","Clockwork-Muse","filipnavara",
    "huoyaoyuan","martincostello","omajid","Sergio0694","shushanhf",
    "SingleAccretion","teo-tsirpanis","tmds","vcsjones","xoofx")

# --- Step 1: List PRs ---
Write-Host "Fetching PR list..." -ForegroundColor Cyan
$listArgs = @("pr","list","--repo",$Repo,"--state","open","--limit",$Limit,
    "--json","number,title,author,labels,mergeable,isDraft,createdAt,updatedAt,changedFiles,additions,deletions,assignees")
if ($Label) { $listArgs += @("--label",$Label) }
if ($Author) { $listArgs += @("--author",$Author) }
if ($Assignee) { $listArgs += @("--assignee",$Assignee) }

$prsRaw = & gh @listArgs | ConvertFrom-Json

# --- Step 2: Quick-screen ---
$drafts = @($prsRaw | Where-Object { $_.isDraft })
$bots = @($prsRaw | Where-Object { -not $_.isDraft -and $_.author.login -match "^(app/)?dotnet-maestro|^(app/)?github-actions" })
$copilotAgent = @($prsRaw | Where-Object { -not $_.isDraft -and $_.author.login -match "copilot-swe-agent" })
$needsAuthor = @($prsRaw | Where-Object { -not $_.isDraft -and ($_.labels.name -contains "needs-author-action") })
$stale = @($prsRaw | Where-Object { -not $_.isDraft -and ($_.labels.name -contains "no-recent-activity") })
$candidates = @($prsRaw | Where-Object {
    ($IncludeDrafts -or -not $_.isDraft) -and
    $_.author.login -notmatch "^(app/)?dotnet-maestro|^(app/)?github-actions" -and
    (-not $ExcludeCopilot -or $_.author.login -notmatch "copilot-swe-agent") -and
    ($IncludeNeedsAuthor -or -not ($_.labels.name -contains "needs-author-action")) -and
    ($IncludeStale -or -not ($_.labels.name -contains "no-recent-activity"))
})

# Apply additional label filters
if ($Community) {
    $candidates = @($candidates | Where-Object { $_.labels.name -contains "community-contribution" })
}
if ($HasLabel) {
    $candidates = @($candidates | Where-Object { $_.labels.name -contains $HasLabel })
}
if ($ExcludeLabel) {
    $candidates = @($candidates | Where-Object { $_.labels.name -notcontains $ExcludeLabel })
}

# Apply age filters
$now = Get-Date
if ($MinAge -gt 0) {
    $candidates = @($candidates | Where-Object { ($now - [DateTime]::Parse($_.createdAt)).TotalDays -ge $MinAge })
}
if ($MaxAge -gt 0) {
    $candidates = @($candidates | Where-Object { ($now - [DateTime]::Parse($_.createdAt)).TotalDays -le $MaxAge })
}
if ($UpdatedWithin -gt 0) {
    $candidates = @($candidates | Where-Object { ($now - [DateTime]::Parse($_.updatedAt)).TotalDays -le $UpdatedWithin })
}

# Single PR mode
if ($PrNumber) {
    $candidates = @($candidates | Where-Object { $_.number -eq [long]$PrNumber })
    if ($candidates.Count -eq 0) {
        # PR wasn't in filtered set - fetch it directly
        $singlePr = & gh pr view $PrNumber --repo $Repo --json number,title,author,labels,mergeable,isDraft,createdAt,updatedAt,changedFiles,additions,deletions,assignees | ConvertFrom-Json
        $candidates = @($singlePr)
    }
}

$excludedDrafts = if ($IncludeDrafts) { 0 } else { $drafts.Count }
$excludedBots = $bots.Count
Write-Host "Scanned $($prsRaw.Count) -> $($candidates.Count) candidates ($excludedDrafts drafts, $excludedBots bots, $($needsAuthor.Count) needs-author, $($stale.Count) stale excluded)" -ForegroundColor Cyan

if ($candidates.Count -eq 0) {
    Write-Host "No candidates to analyze." -ForegroundColor Yellow
    @{ scanned = $prsRaw.Count; analyzed = 0; prs = @() } | ConvertTo-Json -Depth 5
    return
}

# --- Step 3: Batched GraphQL (reviews, threads, Build Analysis, thread authors) ---
$fragment = 'number comments{totalCount} reviews(last:10){nodes{author{login}state}} reviewThreads(first:50){nodes{isResolved comments(first:5){nodes{author{login}}}}} commits(last:1){nodes{commit{statusCheckRollup{contexts(first:100){nodes{...on CheckRun{name conclusion status}}}}}}}'

$graphqlData = @{}
$batches = [System.Collections.ArrayList]@()
$batch = [System.Collections.ArrayList]@()
foreach ($pr in $candidates) {
    [void]$batch.Add($pr.number)
    if ($batch.Count -eq 10) {
        [void]$batches.Add([long[]]$batch.ToArray())
        $batch = [System.Collections.ArrayList]@()
    }
}
if ($batch.Count -gt 0) { [void]$batches.Add([long[]]$batch.ToArray()) }

Write-Host "Fetching details in $($batches.Count) GraphQL batch(es)..." -ForegroundColor Cyan
foreach ($b in $batches) {
    $parts = @()
    for ($i = 0; $i -lt $b.Count; $i++) {
        $parts += "pr$($i): pullRequest(number:$($b[$i])) { $fragment }"
    }
    $repoParts = $Repo -split '/'
    $repoOwner = $repoParts[0]
    $repoName = $repoParts[1]
    $query = "{ repository(owner:`"$repoOwner`",name:`"$repoName`") { $($parts -join ' ') } }"
    $result = gh api graphql -f query="$query" 2>&1 | ConvertFrom-Json
    for ($i = 0; $i -lt $b.Count; $i++) {
        $prData = $result.data.repository."pr$i"
        if ($prData) { $graphqlData[$b[$i]] = $prData }
    }
}

# --- Step 4: Determine area owners for label ---
$owners = @()
if ($Label -and $areaOwners.ContainsKey($Label)) {
    $owners = $areaOwners[$Label]
}
# Also try matching each PR's area labels
function Get-OwnersForPr($labelNames) {
    foreach ($lbl in $labelNames) {
        if ($areaOwners.ContainsKey($lbl)) { return $areaOwners[$lbl] }
    }
    return @()
}

# --- Step 5: Score each PR ---
$now = Get-Date
$results = @()

foreach ($pr in $candidates) {
    $n = $pr.number
    $gql = $graphqlData[$n]
    $labelNames = @($pr.labels | ForEach-Object { $_.name })

    # Per-PR owners (use label-specific or fallback to filter-level)
    $prOwners = Get-OwnersForPr $labelNames
    if ($prOwners.Count -eq 0) { $prOwners = $owners }

    # For bot-authored PRs, find the human who triggered it (non-Copilot assignee)
    $botTrigger = $null
    if ($pr.author.login -match "copilot-swe-agent|copilot") {
        $botTrigger = $pr.assignees | Where-Object { $_.login -ne "Copilot" -and $_.login -ne "app/copilot-swe-agent" } |
            Select-Object -First 1 -ExpandProperty login -ErrorAction SilentlyContinue
    }

    # Extract Build Analysis
    $checks = @()
    $baConclusion = "ABSENT"
    if ($gql -and $gql.commits.nodes.Count -gt 0) {
        $rollup = $gql.commits.nodes[0].commit.statusCheckRollup
        if ($rollup -and $rollup.contexts.nodes) {
            $checks = @($rollup.contexts.nodes | Where-Object { $_.name })
            $baNode = $checks | Where-Object { $_.name -eq "Build Analysis" } | Select-Object -First 1
            if ($baNode) {
                $baConclusion = if ($baNode.conclusion) { $baNode.conclusion } else { "IN_PROGRESS" }
            }
        }
    }

    # Extract reviews (skip copilot reviewer)
    $reviews = @()
    if ($gql -and $gql.reviews.nodes) {
        $reviews = @($gql.reviews.nodes | Where-Object { $_.author.login -ne "copilot-pull-request-reviewer" })
    }

    # Extract threads, commenters, discussion metrics
    $unresolvedThreads = 0
    $totalThreads = 0
    $threadAuthors = @()
    $allCommenters = @()
    $prCommentCount = 0
    if ($gql) {
        $prCommentCount = if ($gql.comments.totalCount) { $gql.comments.totalCount } else { 0 }
    }
    if ($gql -and $gql.reviewThreads.nodes) {
        $totalThreads = $gql.reviewThreads.nodes.Count
        $unresolved = @($gql.reviewThreads.nodes | Where-Object { -not $_.isResolved })
        $unresolvedThreads = $unresolved.Count
        $threadAuthors = @($unresolved | ForEach-Object {
            if ($_.comments.nodes.Count -gt 0) { $_.comments.nodes[0].author.login }
        } | Where-Object { $_ } | Select-Object -Unique)
        # All distinct commenters across all threads (resolved + unresolved)
        $allCommenters = @($gql.reviewThreads.nodes | ForEach-Object {
            $_.comments.nodes | ForEach-Object { $_.author.login }
        } | Where-Object { $_ } | Select-Object -Unique)
    }
    $totalComments = $prCommentCount + ($gql.reviewThreads.nodes | ForEach-Object { $_.comments.nodes.Count } | Measure-Object -Sum).Sum
    $distinctCommenters = $allCommenters.Count

    # Classify reviewers
    $hasOwnerApproval = $false
    $hasTriagerApproval = $false
    $hasAnyApproval = $false
    $approvalCount = 0
    $reviewerLogins = @()
    $approverLogins = @()
    foreach ($rev in $reviews) {
        $login = $rev.author.login
        $reviewerLogins += $login
        if ($rev.state -eq "APPROVED") {
            $approvalCount++
            $hasAnyApproval = $true
            $approverLogins += $login
            if ($prOwners -contains $login) { $hasOwnerApproval = $true }
            elseif ($communityTriagers -contains $login) { $hasTriagerApproval = $true }
        }
    }
    $reviewerLogins = @($reviewerLogins | Select-Object -Unique)
    $hasAnyReview = $reviews.Count -gt 0

    # Labels
    $isCommunity = $labelNames -contains "community-contribution"
    $hasAreaLabel = ($labelNames | Where-Object { $_ -match "^area-" }).Count -gt 0
    $isUntriaged = $labelNames -contains "untriaged"

    # Dates
    $updatedAt = [DateTime]::Parse($pr.updatedAt)
    $createdAt = [DateTime]::Parse($pr.createdAt)
    $daysSinceUpdate = ($now - $updatedAt).TotalDays
    $ageInDays = ($now - $createdAt).TotalDays

    # Check counts
    $passed = @($checks | Where-Object { $_.conclusion -eq "SUCCESS" }).Count
    $failed = @($checks | Where-Object { $_.conclusion -eq "FAILURE" }).Count
    $running = @($checks | Where-Object { $_.status -eq "IN_PROGRESS" -or $_.status -eq "QUEUED" }).Count

    # --- DIMENSION SCORING ---
    $ciScore = switch ($baConclusion) { "SUCCESS" { 1.0 } "ABSENT" { 0.5 } "IN_PROGRESS" { 0.5 } default { 0.0 } }
    $stalenessScore = if ($daysSinceUpdate -le 3) { 1.0 } elseif ($daysSinceUpdate -le 14) { 0.5 } else { 0.0 }
    $maintScore = if ($hasOwnerApproval) { 1.0 } elseif ($hasTriagerApproval) { 0.75 } elseif ($hasAnyReview) { 0.5 } else { 0.0 }
    $feedbackScore = if ($unresolvedThreads -eq 0) { 1.0 } else { 0.5 }
    $conflictScore = switch ($pr.mergeable) { "MERGEABLE" { 1.0 } "UNKNOWN" { 0.5 } "CONFLICTING" { 0.0 } default { 0.5 } }
    $alignScore = if ($isUntriaged -or -not $hasAreaLabel) { 0.0 } else { 0.5 }
    $freshScore = if ($daysSinceUpdate -le 14) { 1.0 } elseif ($daysSinceUpdate -le 30) { 0.5 } else { 0.0 }
    $totalLines = $pr.additions + $pr.deletions
    $sizeScore = if ($pr.changedFiles -le 5 -and $totalLines -le 200) { 1.0 } elseif ($pr.changedFiles -le 20 -and $totalLines -le 500) { 0.5 } else { 0.0 }
    $communityScore = if ($isCommunity) { 0.5 } else { 1.0 }
    $approvalScore = if ($approvalCount -ge 2 -and $hasOwnerApproval) { 1.0 }
                     elseif ($hasOwnerApproval -or ($hasTriagerApproval -and $approvalCount -ge 2)) { 0.75 }
                     elseif ($hasTriagerApproval -or $approvalCount -ge 2) { 0.5 }
                     elseif ($approvalCount -ge 1) { 0.5 }
                     else { 0.0 }
    $velocityScore = if ($reviews.Count -eq 0) { if ($ageInDays -le 14) { 0.5 } else { 0.0 } }
                     elseif ($daysSinceUpdate -le 7) { 1.0 } elseif ($daysSinceUpdate -le 14) { 0.5 } else { 0.0 }
    # Discussion complexity: many threads/commenters = harder to push forward
    # Light (≤5 threads, ≤2 commenters) = 1.0, moderate = 0.5, heavy (>15 threads or >5 commenters) = 0.0
    $discussionScore = if ($totalThreads -le 5 -and $distinctCommenters -le 2) { 1.0 }
                       elseif ($totalThreads -le 15 -and $distinctCommenters -le 5) { 0.5 }
                       else { 0.0 }

    # Composite: weighted sum normalized to 0-10 scale
    $rawMax = 20.0
    $rawScore = ($ciScore * 3) + ($conflictScore * 3) + ($maintScore * 3) +
        ($feedbackScore * 2) + ($approvalScore * 2) + ($stalenessScore * 1.5) +
        ($discussionScore * 1.5) +
        ($alignScore * 1) + ($freshScore * 1) + ($sizeScore * 1) +
        ($communityScore * 0.5) + ($velocityScore * 0.5)
    $composite = [Math]::Round(($rawScore / $rawMax) * 10, 1)

    # --- WHO OWNS NEXT ACTION ---
    # Identify 1-2 specific people responsible for the next step
    $prNextAction = ""
    $who = @()

    if ($pr.mergeable -eq "CONFLICTING") {
        $prNextAction = "Author: resolve conflicts"
        $who = @($pr.author.login)
    }
    elseif ($unresolvedThreads -gt 0) {
        $prNextAction = "Author: respond to $unresolvedThreads thread(s)"
        # Who left the threads? That's who's waiting.
        $who = @($pr.author.login)
        # But note who's waiting on them (thread authors)
        $waitingOn = @($threadAuthors | Where-Object { $_ -ne $pr.author.login }) | Select-Object -First 2
        if ($waitingOn.Count -gt 0) {
            $prNextAction += " from @$($waitingOn -join ', @')"
        }
        $who = @($pr.author.login)
    }
    elseif (-not $hasAnyReview) {
        $prNextAction = "Maintainer: review needed"
        # Pick specific owners to tag: prefer owners who have reviewed similar PRs
        if ($prOwners.Count -gt 0) {
            $who = @($prOwners | Select-Object -First 2)
        } else {
            $who = @("area owner")
        }
    }
    elseif ($baConclusion -eq "FAILURE") {
        $prNextAction = "Author: fix CI failures"
        $who = @($pr.author.login)
    }
    elseif ($daysSinceUpdate -gt 14) {
        $prNextAction = "Author: merge main (stale $([int]$daysSinceUpdate)d)"
        $who = @($pr.author.login)
    }
    elseif ($ciScore -eq 1 -and $conflictScore -eq 1 -and $maintScore -ge 0.75 -and $feedbackScore -eq 1) {
        $prNextAction = "Ready to merge"
        # Who should click merge? The approving owner or area lead
        if ($approverLogins.Count -gt 0) {
            $who = @($approverLogins | Where-Object { $prOwners -contains $_ } | Select-Object -First 1)
            if ($who.Count -eq 0) { $who = @($approverLogins | Select-Object -First 1) }
        } elseif ($prOwners.Count -gt 0) {
            $who = @($prOwners | Select-Object -First 1)
        }
    }
    elseif (-not $hasOwnerApproval -and -not $hasTriagerApproval) {
        $prNextAction = "Maintainer: review needed"
        # Already have community review but need owner; pick owners not yet reviewing
        $nonReviewingOwners = @($prOwners | Where-Object { $reviewerLogins -notcontains $_ }) | Select-Object -First 2
        if ($nonReviewingOwners.Count -gt 0) { $who = $nonReviewingOwners }
        elseif ($prOwners.Count -gt 0) { $who = @($prOwners | Select-Object -First 2) }
    }
    elseif ($baConclusion -eq "IN_PROGRESS" -or $baConclusion -eq "ABSENT") {
        $prNextAction = "Wait for CI"
        $who = @($pr.author.login)
    }
    else {
        $prNextAction = "Maintainer: review/merge"
        if ($prOwners.Count -gt 0) { $who = @($prOwners | Select-Object -First 2) }
    }

    # For bot-authored PRs, substitute the human trigger person
    if ($botTrigger -and $who.Count -gt 0 -and $who[0] -eq $pr.author.login) {
        $who = @($botTrigger)
    }

    $whoStr = if ($who.Count -gt 0) { "@" + ($who -join ", @") } else { "—" }

    # Blockers
    $blockers = @()
    if ($pr.mergeable -eq "CONFLICTING") { $blockers += "Conflicts" }
    if ($baConclusion -eq "FAILURE") { $blockers += "CI fail ($failed failed)" }
    if ($baConclusion -eq "IN_PROGRESS") { $blockers += "CI running" }
    if ($baConclusion -eq "ABSENT") { $blockers += "No CI" }
    if ($unresolvedThreads -gt 0) { $blockers += "$unresolvedThreads threads" }
    if (-not $hasAnyReview) { $blockers += "No review" }
    elseif (-not $hasOwnerApproval -and -not $hasTriagerApproval) { $blockers += "No owner approval" }
    if ($daysSinceUpdate -gt 14) { $blockers += "Stale $([int]$daysSinceUpdate)d" }
    $blockersStr = if ($blockers.Count -gt 0) { $blockers -join ", " } else { "—" }

    # Why
    $why = @()
    $why += if ($ciScore -eq 1) { "CI:pass" } elseif ($ciScore -eq 0) { "CI:fail" } else { "CI:pending" }
    if ($conflictScore -eq 0) { $why += "Conflicts" }
    if ($hasOwnerApproval) { $why += "OwnerApproved" }
    elseif ($hasTriagerApproval) { $why += "TriagerApproved" }
    elseif ($hasAnyApproval) { $why += "CommunityApproved" }
    elseif ($hasAnyReview) { $why += "Reviewed(noAppr)" }
    else { $why += "NoReview" }
    if ($unresolvedThreads -gt 0) { $why += "${unresolvedThreads}unresolved" }
    if ($totalThreads -gt 15) { $why += "Heavy discussion(${totalThreads}threads/${distinctCommenters}people)" }
    elseif ($totalThreads -gt 5) { $why += "Moderate discussion(${totalThreads}threads)" }
    if ($isCommunity) { $why += "Community" }
    if ($sizeScore -eq 1) { $why += "Small" }
    elseif ($sizeScore -eq 0) { $why += "Large($($pr.changedFiles)f/$($totalLines)L)" }
    if ($daysSinceUpdate -gt 14) { $why += "Stale($([int]$daysSinceUpdate)d)" }
    if ($ageInDays -gt 90) { $why += "Old($([int]$ageInDays)d)" }
    $whyStr = $why -join ", "

    $results += [PSCustomObject]@{
        number = $n
        title = $pr.title.Substring(0, [Math]::Min(70, $pr.title.Length))
        author = $pr.author.login
        score = $composite
        ci = $baConclusion
        ci_detail = "$passed/$failed/$running"
        unresolved_threads = $unresolvedThreads
        total_threads = $totalThreads
        total_comments = $totalComments
        distinct_commenters = $distinctCommenters
        mergeable = $pr.mergeable
        approval_count = $approvalCount
        is_community = $isCommunity
        age_days = [int]$ageInDays
        days_since_update = [int]$daysSinceUpdate
        changed_files = $pr.changedFiles
        lines_changed = $totalLines
        next_action = $prNextAction
        who = $whoStr
        blockers = $blockersStr
        why = $whyStr
    }
}

# Sort by score descending
$results = $results | Sort-Object -Property score -Descending

# --- Post-scoring filters ---
if ($MinApprovals -gt 0) {
    $results = @($results | Where-Object { $_.approval_count -ge $MinApprovals })
}
if ($MinScore -gt 0) {
    $results = @($results | Where-Object { $_.score -ge $MinScore })
}
if ($NextAction) {
    # Filter by next-action type: "ready", "review", "author", "conflicts", "ci"
    $pattern = switch ($NextAction.ToLower()) {
        "ready"     { "Ready to merge" }
        "review"    { "review needed" }
        "author"    { "^Author:" }
        "conflicts" { "resolve conflicts" }
        "ci"        { "fix CI" }
        default     { $NextAction }
    }
    $results = @($results | Where-Object { $_.next_action -match $pattern })
}
if ($MyActions) {
    $me = $MyActions.TrimStart('@')
    $results = @($results | Where-Object {
        $_.who -match $me -or
        ($_.author -eq $me -and $_.next_action -match "^Author:")
    })
}
$totalResults = $results.Count
if ($Top -gt 0) {
    $results = @($results | Select-Object -First $Top)
}

# --- Output JSON ---
$output = @{
    timestamp = $now.ToString("o")
    repo = $Repo
    filters = @{
        label = if ($Label) { $Label } else { $null }
        author = if ($Author) { $Author } else { $null }
        assignee = if ($Assignee) { $Assignee } else { $null }
        community = [bool]$Community
        min_age = $MinAge
        max_age = $MaxAge
        updated_within = $UpdatedWithin
        min_approvals = $MinApprovals
        min_score = $MinScore
        next_action = if ($NextAction) { $NextAction } else { $null }
        my_actions = if ($MyActions) { $MyActions } else { $null }
        top = $Top
    }
    owners = $owners
    scanned = $prsRaw.Count
    analyzed = $candidates.Count
    returned = $results.Count
    total_after_filters = $totalResults
    screened_out = @{
        drafts = @($drafts | ForEach-Object { @{ number = $_.number; author = $_.author.login; title = $_.title.Substring(0, [Math]::Min(60, $_.title.Length)) } })
        bots = @($bots | ForEach-Object { @{ number = $_.number; author = $_.author.login } })
        needs_author_action = @($needsAuthor | ForEach-Object { @{ number = $_.number; author = $_.author.login } })
        stale = @($stale | ForEach-Object { @{ number = $_.number; author = $_.author.login } })
    }
    quick_actions = @{
        ready_to_merge = @($results | Where-Object { $_.next_action -match "Ready to merge" }).Count
        needs_maintainer_review = @($results | Where-Object { $_.next_action -match "review needed" }).Count
        needs_author_action = @($results | Where-Object { $_.next_action -match "^Author:" }).Count
        blocked_conflicts = @($results | Where-Object { $_.mergeable -eq "CONFLICTING" }).Count
    }
    prs = @($results)
    elapsed_seconds = [Math]::Round(((Get-Date) - $scriptStart).TotalSeconds, 1)
}

if ($OutputCsv) {
    # Tab-separated output for easy SQL/spreadsheet import
    $header = "number`ttitle`tauthor`tscore`tci`tci_detail`tunresolved_threads`ttotal_threads`tdistinct_commenters`tmergeable`tapproval_count`tis_community`tage_days`tdays_since_update`tchanged_files`tlines_changed`tnext_action`twho`tblockers`twhy"
    $lines = @($header)
    foreach ($r in $results) {
        $t = ($r.title -replace "`t"," ").Substring(0, [Math]::Min(70, $r.title.Length))
        $lines += "$($r.number)`t$t`t$($r.author)`t$($r.score)`t$($r.ci)`t$($r.ci_detail)`t$($r.unresolved_threads)`t$($r.total_threads)`t$($r.distinct_commenters)`t$($r.mergeable)`t$($r.approval_count)`t$(if ($r.is_community) {1} else {0})`t$($r.age_days)`t$($r.days_since_update)`t$($r.changed_files)`t$($r.lines_changed)`t$($r.next_action)`t$($r.who)`t$($r.blockers)`t$($r.why)"
    }
    $lines -join "`n"
} else {
    $output | ConvertTo-Json -Depth 5
}
