# Get-VersionInfo.ps1
# Determines the .NET version context for a merged PR using the GitHub CLI (gh).
#
# Usage:
#   pwsh .github/skills/breaking-change-doc/Get-VersionInfo.ps1 -PrNumber 114929
#
# Output: JSON object with LastTagBeforeMerge, FirstTagWithChange, EstimatedVersion

param(
    [Parameter(Mandatory = $true)]
    [string]$PrNumber,

    [string]$SourceRepo = "dotnet/runtime",

    [string]$BaseRef = ""
)

$ErrorActionPreference = "Stop"

function ConvertFrom-DotNetTag {
    param([string]$tagName)

    if (-not $tagName -or $tagName -eq "Unknown") {
        return $null
    }

    if ($tagName -match '^v(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2]
        $build = [int]$matches[3]
        $prerelease = if ($matches[4]) { $matches[4] } else { $null }

        $prereleaseType = $null
        $prereleaseNumber = $null

        if ($prerelease -and $prerelease -match '^([a-zA-Z]+)\.(\d+)') {
            $rawType = $matches[1]
            $prereleaseNumber = [int]$matches[2]

            if ($rawType -ieq "rc") {
                $prereleaseType = "RC"
            } else {
                $prereleaseType = $rawType.Substring(0, 1).ToUpper() + $rawType.Substring(1).ToLower()
            }
        }

        return @{
            Major            = $major
            Minor            = $minor
            Build            = $build
            Prerelease       = $prerelease
            PrereleaseType   = $prereleaseType
            PrereleaseNumber = $prereleaseNumber
            IsRelease        = $null -eq $prerelease
        }
    }

    return $null
}

function Format-DotNetVersion {
    param($parsedTag)

    if (-not $parsedTag) {
        return "Next release"
    }

    $baseVersion = ".NET $($parsedTag.Major).$($parsedTag.Minor)"

    if ($parsedTag.IsRelease) {
        return $baseVersion
    }

    if ($parsedTag.PrereleaseType -and $parsedTag.PrereleaseNumber) {
        return "$baseVersion $($parsedTag.PrereleaseType) $($parsedTag.PrereleaseNumber)"
    }

    return "$baseVersion ($($parsedTag.Prerelease))"
}

function Get-EstimatedNextVersion {
    param($parsedTag, [string]$baseRef)

    if (-not $parsedTag) {
        return "Next release"
    }

    $isMainBranch = $baseRef -eq "main"

    if ($parsedTag.IsRelease) {
        if ($isMainBranch) {
            $nextMajor = $parsedTag.Major + 1
            return ".NET $nextMajor.0 Preview 1"
        } else {
            return ".NET $($parsedTag.Major).$($parsedTag.Minor)"
        }
    }

    if ($isMainBranch -and $parsedTag.PrereleaseType -eq "RC") {
        $nextMajor = $parsedTag.Major + 1
        return ".NET $nextMajor.0 Preview 1"
    } else {
        $nextPreview = $parsedTag.PrereleaseNumber + 1
        return ".NET $($parsedTag.Major).$($parsedTag.Minor) $($parsedTag.PrereleaseType) $nextPreview"
    }
}

try {
    # Step 1: Get PR merge info via GitHub CLI
    $prJson = gh pr view $PrNumber --repo $SourceRepo --json mergeCommit,mergedAt,baseRefName 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to fetch PR #$PrNumber from $SourceRepo"
    }
    $prData = $prJson | ConvertFrom-Json

    $targetCommit = $prData.mergeCommit.oid
    $mergedAt = $prData.mergedAt

    if (-not $BaseRef) {
        $BaseRef = $prData.baseRefName
    }

    # Step 2: Get recent releases (tags with published dates) in a single API call
    $releasesJson = gh release list --repo $SourceRepo --limit 100 --json tagName,publishedAt 2>$null
    $releases = @()
    if ($LASTEXITCODE -eq 0 -and $releasesJson) {
        $releases = @($releasesJson | ConvertFrom-Json)
    }

    # Filter to .NET version tags (v{major}.{minor}.{patch}[-prerelease]) with a valid publishedAt
    $versionReleases = @($releases | Where-Object { $_.tagName -match '^v\d+\.\d+\.\d+' -and $_.publishedAt })

    $lastTagBefore = "Unknown"
    $firstTagWith = "Not yet released"

    if ($mergedAt -and $versionReleases.Count -gt 0) {
        $mergedAtDate = [DateTimeOffset]::Parse($mergedAt)

        # Find the most recent release published before the merge
        $beforeMerge = @($versionReleases |
            Where-Object { [DateTimeOffset]::Parse($_.publishedAt) -lt $mergedAtDate } |
            Sort-Object { [DateTimeOffset]::Parse($_.publishedAt) } -Descending)

        if ($beforeMerge.Count -gt 0) {
            $lastTagBefore = $beforeMerge[0].tagName
        }

        # Find candidate releases published at or after the merge, oldest first
        $afterMerge = @($versionReleases |
            Where-Object { [DateTimeOffset]::Parse($_.publishedAt) -ge $mergedAtDate } |
            Sort-Object { [DateTimeOffset]::Parse($_.publishedAt) })

        # Verify containment via the compare API: behind_by == 0 means the tag
        # includes every commit reachable from the merge commit.
        if ($targetCommit -and $afterMerge.Count -gt 0) {
            foreach ($release in $afterMerge) {
                $tag = $release.tagName
                $behindBy = gh api "repos/$SourceRepo/compare/${targetCommit}...${tag}" --jq '.behind_by' 2>$null
                if ($LASTEXITCODE -eq 0 -and $behindBy -match '^\d+$' -and [int]$behindBy -eq 0) {
                    $firstTagWith = $tag
                    break
                }
            }
        }
    }

    # Step 3: Estimate version
    $estimatedVersion = "Next release"

    if ($firstTagWith -ne "Not yet released") {
        $parsedFirstTag = ConvertFrom-DotNetTag $firstTagWith
        $estimatedVersion = Format-DotNetVersion $parsedFirstTag
    } else {
        $parsedLastTag = ConvertFrom-DotNetTag $lastTagBefore
        $estimatedVersion = Get-EstimatedNextVersion $parsedLastTag $BaseRef
    }

    # Output as JSON
    @{
        LastTagBeforeMerge = $lastTagBefore
        FirstTagWithChange = $firstTagWith
        EstimatedVersion   = $estimatedVersion
        MergeCommit        = $targetCommit
        MergedAt           = $mergedAt
        BaseRef            = $BaseRef
    } | ConvertTo-Json

} catch {
    # Return error info as JSON so the agent can handle it
    @{
        LastTagBeforeMerge = "Unknown"
        FirstTagWithChange = "Not yet released"
        EstimatedVersion   = "Next release"
        Error              = $_.Exception.Message
    } | ConvertTo-Json
}
