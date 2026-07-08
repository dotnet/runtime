# Get-VersionInfo.ps1
# Determines the .NET version context for a merged PR using the GitHub CLI (gh).
#
# The reliable signal is the existence of release branches named
#   release/<major>.<minor>-preview<N>   (and later -rc<N> / GA release/<major>.<minor>)
# Once release/<M>.<m>-preview<N> exists, that preview is branched and locked, so a
# change merged to main lands in the *next* milestone -- following the standard .NET
# cadence of $PreviewCount previews, then $RcCount RCs, then GA (so Preview 7 rolls
# over to RC 1, and RC 2 to GA). Exceptions:
#   * If the change was backported into an already-branched milestone, it ships there.
#     A merged backport is found via commit containment (compare API); an unmerged one
#     is found via the backport PR and reported as tentative.
#   * For changes that already shipped in a past major (whose preview/RC branches were
#     pruned), containment resolves only to the GA line, so release *tags* (which are
#     never pruned) are scanned to recover the exact first preview/RC.
#
# Usage:
#   pwsh .github/skills/breaking-change-doc/Get-VersionInfo.ps1 -PrNumber 114929
#
# Output: JSON object with EstimatedVersion, Tentative, DetectionMethod, HighestBranch,
#         ContainedInBranch, FirstShippedTag, Backports, MergeCommit, MergedAt, BaseRef.

param(
    [Parameter(Mandatory = $true)]
    [string]$PrNumber,

    [string]$SourceRepo = "dotnet/runtime",

    [string]$BaseRef = "",

    # .NET release cadence: this many previews, then this many RCs, then GA.
    # Used to predict the next milestone across Preview->RC and RC->GA transitions.
    [int]$PreviewCount = 7,

    [int]$RcCount = 2
)

$ErrorActionPreference = "Stop"

# Parse a release branch name into a structured milestone.
# Recognizes:
#   release/11.0                -> GA
#   release/11.0-preview6       -> Preview 6
#   release/11.0-rc1            -> RC 1
#   release/11.0-staging        -> GA (staging variant, treated as GA milestone)
# Returns $null for non-release or unrecognized branch names.
function ConvertFrom-ReleaseBranch {
    param([string]$branchName)

    if ($branchName -notmatch '^release/(\d+)\.(\d+)(?:-([a-zA-Z]+)(\d+)?)?$') {
        return $null
    }

    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $label = if ($matches[3]) { $matches[3].ToLower() } else { $null }
    $number = if ($matches[4]) { [int]$matches[4] } else { $null }

    # Milestone ordering rank within a major.minor: previews < rc < GA.
    $kind = "ga"
    $rank = 3000
    switch ($label) {
        "preview" { $kind = "preview"; $rank = 1000 + ($number ?? 0) }
        "rc"      { $kind = "rc";      $rank = 2000 + ($number ?? 0) }
        "staging" { $kind = "ga";      $rank = 3000 }
        $null     { $kind = "ga";      $rank = 3000 }
        default   { $kind = $label;    $rank = 0 }
    }

    return [pscustomobject]@{
        Major  = $major
        Minor  = $minor
        Kind   = $kind
        Number = $number
        Rank   = $rank
        Branch = $branchName
    }
}

# Format a milestone (kind + number) for a given major.minor as a docs-template
# version string, e.g. ".NET 11 Preview 7", ".NET 11 RC 1", ".NET 11".
function Format-Milestone {
    param([int]$major, [int]$minor, [string]$kind, [Nullable[int]]$number)

    $baseVersion = ".NET $major"
    if ($minor -ne 0) {
        $baseVersion = ".NET $major.$minor"
    }

    switch ($kind) {
        "preview" { return "$baseVersion Preview $number" }
        "rc"      { return "$baseVersion RC $number" }
        default   { return $baseVersion }
    }
}

# Compute the version a change merged to main would ship in, given the release branches
# already cut for its major.minor. Follows the .NET cadence: $previewCount previews,
# then $rcCount RCs, then GA -- so the milestone *after* the highest branched one, with
# Preview N -> RC 1 and RC N -> GA rollovers. Returns the version string, the highest
# branch name, and a human-readable detection note.
function Get-NextMilestoneVersion {
    param(
        [pscustomobject[]]$milestones,
        [pscustomobject]$devVersion,
        [string]$baseRef,
        [int]$previewCount,
        [int]$rcCount
    )

    if (-not $milestones -or $milestones.Count -eq 0) {
        return [pscustomobject]@{
            Version       = Format-Milestone $devVersion.Major $devVersion.Minor "preview" 1
            HighestBranch = "(none)"
            Detection     = "Merged to '$baseRef'; no milestone branched yet for this version"
        }
    }

    $highest = $milestones | Sort-Object Rank -Descending | Select-Object -First 1

    switch ($highest.Kind) {
        "preview" {
            $version = if ($highest.Number -lt $previewCount) {
                Format-Milestone $devVersion.Major $devVersion.Minor "preview" ($highest.Number + 1)
            } else {
                # Final preview branched -> next milestone is RC 1.
                Format-Milestone $devVersion.Major $devVersion.Minor "rc" 1
            }
        }
        "rc" {
            $version = if ($highest.Number -lt $rcCount) {
                Format-Milestone $devVersion.Major $devVersion.Minor "rc" ($highest.Number + 1)
            } else {
                # Final RC branched -> next milestone is GA.
                Format-Milestone $devVersion.Major $devVersion.Minor "ga" $null
            }
        }
        default { $version = Format-Milestone $devVersion.Major $devVersion.Minor "ga" $null }
    }

    return [pscustomobject]@{
        Version       = $version
        HighestBranch = $highest.Branch
        Detection     = "Merged to '$baseRef'; ships after highest branched milestone '$($highest.Branch)' (assumes $previewCount previews + $rcCount RCs before GA)"
    }
}

# Parse a .NET release tag (e.g. v11.0.0-preview.7.25380.108, v10.0.0-rc.1.xxx,
# v10.0.0, v10.0.3) into a structured milestone. Unlike release branches, tags are
# never pruned, so they preserve preview/RC granularity for already-shipped versions.
function ConvertFrom-DotNetTag {
    param([string]$tagName)

    if ($tagName -notmatch '^v(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
        return $null
    }

    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $build = [int]$matches[3]
    $prerelease = if ($matches[4]) { $matches[4] } else { $null }

    $kind = "ga"
    $number = $null
    if ($prerelease) {
        if ($prerelease -match '^([a-zA-Z]+)\.(\d+)') {
            $raw = $matches[1]
            $number = [int]$matches[2]
            $kind = if ($raw -ieq "rc") { "rc" } elseif ($raw -ieq "preview") { "preview" } else { $raw.ToLower() }
        } else {
            $kind = "prerelease"
        }
    }

    return [pscustomobject]@{
        Major     = $major
        Minor     = $minor
        Build     = $build
        Kind      = $kind
        Number    = $number
        IsRelease = ($null -eq $prerelease)
        Tag       = $tagName
    }
}

# Ordering key so preview < rc < GA within a patch, and lower patch first.
function Get-TagSortKey {
    param($t)

    $sub = switch ($t.Kind) {
        "preview" { 1000 + ($t.Number ?? 0) }
        "rc"      { 2000 + ($t.Number ?? 0) }
        "ga"      { 3000 }
        default   { 500 }
    }
    return ($t.Build * 100000) + $sub
}

# Find the earliest published tag for a given major.minor that already contains the
# merge commit. Used to recover exact preview/RC granularity when branch containment
# only resolved to a (pruned) GA line. Returns the parsed tag, or $null.
function Get-EarliestContainingTag {
    param([string]$repo, [string]$commit, [int]$major, [int]$minor)

    if (-not $commit) { return $null }

    $tagNames = @(gh api "repos/$repo/tags" --paginate --jq '.[].name' 2>$null)
    if ($LASTEXITCODE -ne 0) { return $null }

    $candidates = @(
        $tagNames |
        ForEach-Object { ConvertFrom-DotNetTag $_ } |
        Where-Object { $_ -and $_.Major -eq $major -and $_.Minor -eq $minor } |
        Sort-Object { Get-TagSortKey $_ }
    )

    foreach ($t in $candidates) {
        if (Test-CommitInBranch -repo $repo -commit $commit -branch $t.Tag) {
            return $t
        }
    }
    return $null
}

# Read MajorVersion/MinorVersion from eng/Versions.props at a specific ref.
# This is the authoritative source for the in-development major.minor on main.
function Get-DevMajorMinor {
    param([string]$repo, [string]$ref)

    try {
        $content = gh api "repos/$repo/contents/eng/Versions.props?ref=$ref" -H "Accept: application/vnd.github.raw" 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $content) {
            return $null
        }
        $text = ($content | Out-String)
        if ($text -match '<MajorVersion>(\d+)</MajorVersion>' ) {
            $maj = [int]$matches[1]
            $min = 0
            if ($text -match '<MinorVersion>(\d+)</MinorVersion>') {
                $min = [int]$matches[1]
            }
            return [pscustomobject]@{ Major = $maj; Minor = $min }
        }
    } catch { }

    return $null
}

# Is the merge commit already contained in the given branch?
# True when the milestone branched after the PR merged (normal case), or when a
# backport was already merged into the branch.
function Test-CommitInBranch {
    param([string]$repo, [string]$commit, [string]$branch)

    if (-not $commit) { return $false }
    $behind = gh api "repos/$repo/compare/$commit...$branch" --jq '.behind_by' 2>$null
    return ($LASTEXITCODE -eq 0 -and $behind -match '^\d+$' -and [int]$behind -eq 0)
}

# Find *potential* backport PRs for a source PR via GitHub's cross-reference graph.
# A backport PR is almost always cross-referenced from the primary PR (through its
# body, a comment, or the backport tooling), regardless of its head-branch name --
# which makes this far more robust than matching a branch naming convention (real
# backports use ad-hoc names, and branches are deleted after merge). The primary PR's
# timeline yields candidate PR numbers; a single GraphQL batch resolves each one's
# base branch and state; and we keep only those targeting a release branch.
#
# NOTE: these are *candidates only*. A cross-reference does not prove a PR is a
# backport of this change, so the caller (skill) should confirm via the PR's
# title/description/diff before trusting it.
function Get-BackportPRs {
    param([string]$repo, [string]$prNumber)

    # Cross-references can point at PRs in *other* repos (e.g. VMR codeflow PRs from
    # dotnet/dotnet). Filter to same-repo PRs up front (via source.issue.repository)
    # so the GraphQL batch only asks for numbers that exist in this repo.
    $candidateNumbers = @(
        gh api "repos/$repo/issues/$prNumber/timeline" --paginate `
            --jq ".[] | select(.event==`"cross-referenced`") | select(.source.issue.pull_request != null) | select(.source.issue.repository.full_name == `"$repo`") | .source.issue.number" 2>$null |
        Sort-Object -Unique
    )
    if ($candidateNumbers.Count -eq 0) { return @() }

    $parts = $repo -split '/'
    $owner = $parts[0]
    $name = $parts[1]

    # Build a single batched GraphQL query with one alias per candidate.
    $aliases = for ($i = 0; $i -lt $candidateNumbers.Count; $i++) {
        "p${i}: pullRequest(number: $($candidateNumbers[$i])) { number state merged baseRefName title }"
    }
    $query = 'query($owner:String!,$name:String!){ repository(owner:$owner,name:$name){ ' + ($aliases -join ' ') + ' } }'

    $resp = gh api graphql -f owner=$owner -f name=$name -f query=$query 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $resp) { return @() }
    $repository = ($resp | ConvertFrom-Json).data.repository
    if (-not $repository) { return @() }

    $results = @()
    foreach ($prop in $repository.PSObject.Properties) {
        $pr = $prop.Value
        if (-not $pr) { continue }
        # Only PRs targeting a release branch are plausible backports.
        $milestone = ConvertFrom-ReleaseBranch $pr.baseRefName
        if (-not $milestone) { continue }
        $results += [pscustomobject]@{
            Number       = $pr.number
            State        = $pr.state    # OPEN | CLOSED | MERGED
            Merged       = [bool]$pr.merged
            TargetBranch = $pr.baseRefName
            Title        = $pr.title
            Milestone    = $milestone
        }
    }

    return $results
}

try {
    # Step 1: PR merge info
    $prJson = gh pr view $PrNumber --repo $SourceRepo --json mergeCommit,mergedAt,baseRefName 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to fetch PR #$PrNumber from $SourceRepo"
    }
    $prData = $prJson | ConvertFrom-Json

    $mergeCommit = $prData.mergeCommit.oid
    $mergedAt = $prData.mergedAt
    if (-not $BaseRef) {
        $BaseRef = $prData.baseRefName
    }

    # Step 2: If the PR merged directly into a release branch, the milestone is that
    # branch's milestone -- no further estimation needed.
    $baseMilestone = if ($BaseRef -match '^release/') { ConvertFrom-ReleaseBranch $BaseRef } else { $null }

    if ($baseMilestone) {
        $estimated = Format-Milestone $baseMilestone.Major $baseMilestone.Minor $baseMilestone.Kind $baseMilestone.Number
        @{
            EstimatedVersion = $estimated
            Tentative        = $false
            DetectionMethod  = "PR merged directly into release branch '$BaseRef'"
            MergeCommit      = $mergeCommit
            MergedAt         = $mergedAt
            BaseRef          = $BaseRef
        } | ConvertTo-Json
        return
    }

    # Step 3: Determine the in-development major.minor for main.
    $devVersion = Get-DevMajorMinor -repo $SourceRepo -ref $mergeCommit
    if (-not $devVersion) {
        $devVersion = Get-DevMajorMinor -repo $SourceRepo -ref $BaseRef
    }

    # Step 4: Enumerate release branches for that major.minor.
    $branchNames = @(gh api "repos/$SourceRepo/branches" --paginate --jq '.[].name' 2>$null)
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to list branches for $SourceRepo"
    }

    $allMilestones = @($branchNames | ForEach-Object { ConvertFrom-ReleaseBranch $_ } | Where-Object { $_ -ne $null })

    # If we could not read the dev version, fall back to the highest major.minor
    # that has any release branch.
    if (-not $devVersion -and $allMilestones.Count -gt 0) {
        $top = $allMilestones | Sort-Object Major, Minor -Descending | Select-Object -First 1
        $devVersion = [pscustomobject]@{ Major = $top.Major; Minor = $top.Minor }
    }

    if (-not $devVersion) {
        throw "Could not determine the in-development major.minor version."
    }

    $milestones = @($allMilestones | Where-Object { $_.Major -eq $devVersion.Major -and $_.Minor -eq $devVersion.Minor })

    # Collect *potential* backport PRs (cross-referenced from this PR and targeting a
    # release branch). Surface all of them -- including cross-major servicing backports
    # -- as informational output; they are candidates the skill should confirm.
    $backports = @(Get-BackportPRs -repo $SourceRepo -prNumber $PrNumber)
    $backportInfo = @($backports | ForEach-Object {
        [pscustomobject]@{ Number = $_.Number; State = $_.State; Merged = $_.Merged; Target = $_.TargetBranch; Title = $_.Title }
    })

    # Step 5a: If *this PR's merge commit itself* is contained in an existing milestone
    # branch, the change definitively ships in that milestone (lowest such milestone).
    # This is the normal "milestone branched after merge" case. Note it does NOT catch
    # cherry-pick backports, whose commit differs from this PR's -- those are handled
    # (tentatively) in Step 5b.
    $containing = $null
    foreach ($m in ($milestones | Sort-Object Rank)) {
        if (Test-CommitInBranch -repo $SourceRepo -commit $mergeCommit -branch $m.Branch) {
            $containing = $m
            break
        }
    }

    if ($containing) {
        $detection = "Merge commit is already contained in branched milestone '$($containing.Branch)'"
        $firstShippedTag = $null
        $estimated = Format-Milestone $containing.Major $containing.Minor $containing.Kind $containing.Number

        # Refinement: preview/RC branches are pruned after a major ships, so containment
        # on a GA branch loses granularity. Recover the exact first preview/RC from the
        # persistent release tags.
        if ($containing.Kind -eq "ga") {
            $tag = Get-EarliestContainingTag -repo $SourceRepo -commit $mergeCommit -major $containing.Major -minor $containing.Minor
            if ($tag) {
                $estimated = Format-Milestone $tag.Major $tag.Minor $tag.Kind $tag.Number
                $firstShippedTag = $tag.Tag
                $detection = "First shipped in tag '$($tag.Tag)' (refined from GA branch '$($containing.Branch)')"
            }
        }

        @{
            EstimatedVersion    = $estimated
            Tentative           = $false
            DetectionMethod     = $detection
            HighestBranch       = ($milestones | Sort-Object Rank -Descending | Select-Object -First 1).Branch
            ContainedInBranch   = $containing.Branch
            FirstShippedTag     = $firstShippedTag
            Backports           = $backportInfo
            MergeCommit         = $mergeCommit
            MergedAt            = $mergedAt
            BaseRef             = $BaseRef
        } | ConvertTo-Json -Depth 5
        return
    }

    # Step 5b: Not contained by commit. A *linked* PR targeting an earlier branched
    # milestone of the same major.minor suggests the change may also ship there (e.g.
    # via a cherry-pick backport, whose commit differs from this PR's so containment
    # can't see it). But a "backport" here is only "a linked PR to a release branch" and
    # is NOT verified, so it is never definitive: report that milestone but mark the
    # version **tentative** for the skill to confirm. Consider OPEN and MERGED linked
    # PRs (a merged one may already be shipped there); ignore CLOSED (abandoned) ones.
    $tentativeBackport = $backports |
        Where-Object { ($_.State -eq 'OPEN' -or $_.State -eq 'MERGED') -and $_.Milestone -and $_.Milestone.Major -eq $devVersion.Major -and $_.Milestone.Minor -eq $devVersion.Minor } |
        Sort-Object { $_.Milestone.Rank } |
        Select-Object -First 1

    if ($tentativeBackport) {
        $bm = $tentativeBackport.Milestone
        $estimated = Format-Milestone $bm.Major $bm.Minor $bm.Kind $bm.Number
        $stateNote = if ($tentativeBackport.State -eq 'MERGED') {
            "merged into '$($tentativeBackport.TargetBranch)' (may already ship there)"
        } else {
            "open against '$($tentativeBackport.TargetBranch)' (would ship there if it merges)"
        }

        # The version if the linked PR turns out NOT to be a genuine backport: the
        # normal "ships after highest branched milestone" result. Provided so the skill
        # doesn't have to re-derive the cadence rollover itself.
        $fallback = Get-NextMilestoneVersion -milestones $milestones -devVersion $devVersion -baseRef $BaseRef -previewCount $PreviewCount -rcCount $RcCount

        @{
            EstimatedVersion = $estimated
            Tentative        = $true
            DetectionMethod  = "Potential backport PR #$($tentativeBackport.Number) $stateNote; version is tentative -- confirm the PR is a genuine backport of this change (title/description/diff). If it is NOT, use FallbackVersion instead."
            FallbackVersion  = $fallback.Version
            HighestBranch    = $fallback.HighestBranch
            Backports        = $backportInfo
            MergeCommit      = $mergeCommit
            MergedAt         = $mergedAt
            BaseRef          = $BaseRef
        } | ConvertTo-Json -Depth 5
        return
    }

    # Step 6: Not contained and no relevant linked PR -> ships in the milestone *after*
    # the highest branched one (cadence-aware; see Get-NextMilestoneVersion).
    $next = Get-NextMilestoneVersion -milestones $milestones -devVersion $devVersion -baseRef $BaseRef -previewCount $PreviewCount -rcCount $RcCount

    @{
        EstimatedVersion = $next.Version
        Tentative        = $false
        DetectionMethod  = $next.Detection
        HighestBranch    = $next.HighestBranch
        Backports        = $backportInfo
        MergeCommit      = $mergeCommit
        MergedAt         = $mergedAt
        BaseRef          = $BaseRef
    } | ConvertTo-Json -Depth 5

} catch {
    @{
        EstimatedVersion = "Next release"
        DetectionMethod  = "error"
        Error            = $_.Exception.Message
    } | ConvertTo-Json
}
