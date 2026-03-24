# Get-VersionInfo.ps1
# Determines the .NET version context for a merged PR using local git tags.
# Called by the breaking-change-doc skill to provide accurate version information.
#
# Usage:
#   pwsh .github/skills/breaking-change-doc/Get-VersionInfo.ps1 -PrNumber 114929 [-RepoRoot .]
#
# Output: JSON object with LastTagBeforeMerge, FirstTagWithChange, EstimatedVersion

param(
    [Parameter(Mandatory = $true)]
    [string]$PrNumber,

    [string]$RepoRoot = ".",

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

function Find-ClosestTagByDistance {
    param([string]$targetCommit, [int]$maxTags = 10)

    $recentTags = git tag --sort=-version:refname 2>$null | Select-Object -First $maxTags
    $closestTag = $null
    $minDistance = [int]::MaxValue

    foreach ($tag in $recentTags) {
        if ($targetCommit -match '^[a-f0-9]{40}$') {
            git merge-base --is-ancestor $targetCommit $tag 2>$null
            if ($LASTEXITCODE -eq 0) {
                continue
            }
        }

        $distance = git rev-list --count "$tag..$targetCommit" 2>$null
        if ($LASTEXITCODE -eq 0 -and $distance -match '^\d+$') {
            $distanceNum = [int]$distance
            if ($distanceNum -lt $minDistance) {
                $minDistance = $distanceNum
                $closestTag = $tag
            }
        }
    }

    return $closestTag
}

try {
    Push-Location $RepoRoot

    # Unshallow if needed (GitHub Actions default is fetch-depth=1)
    $isShallow = git rev-parse --is-shallow-repository 2>$null
    if ($isShallow -eq "true") {
        Write-Host "Shallow clone detected — fetching full history for tag/ancestry operations"
        git fetch --unshallow --tags 2>$null | Out-Null
    } else {
        git fetch --tags 2>$null | Out-Null
    }

    # Get merge commit and base ref from GitHub CLI
    $prJson = gh pr view $PrNumber --repo $SourceRepo --json mergeCommit,mergedAt,baseRefName 2>$null
    $prData = $prJson | ConvertFrom-Json

    $targetCommit = $prData.mergeCommit.oid
    $mergedAt = $prData.mergedAt

    if (-not $BaseRef) {
        $BaseRef = $prData.baseRefName
    }

    $firstTagWith = "Not yet released"

    if ($targetCommit) {
        $firstTagWith = git describe --tags --contains $targetCommit 2>$null
        if ($firstTagWith -and $firstTagWith -match '^([^~^]+)') {
            $firstTagWith = $matches[1]
        }
    }

    if (-not $targetCommit) {
        $targetCommit = git rev-parse "origin/$BaseRef" 2>$null
    }

    # Find the last tag before this commit
    $lastTagBefore = "Unknown"
    if ($targetCommit) {
        $closestTag = Find-ClosestTagByDistance -targetCommit $targetCommit
        if ($closestTag) {
            $lastTagBefore = $closestTag
        } else {
            if ($BaseRef -eq "main") {
                $lastTagBefore = git describe --tags --abbrev=0 "origin/$BaseRef" 2>$null
                if (-not $lastTagBefore) {
                    $lastTagBefore = git tag --sort=-version:refname | Select-Object -First 1 2>$null
                }
            } else {
                $lastTagBefore = git describe --tags --abbrev=0 "origin/$BaseRef" 2>$null
            }
        }
    }

    $lastTagBefore = if ($lastTagBefore) { $lastTagBefore.Trim() } else { "Unknown" }
    $firstTagWith = if ($firstTagWith -and $firstTagWith -ne "Not yet released") { $firstTagWith.Trim() } else { "Not yet released" }

    # Estimate version
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
} finally {
    try {
        Pop-Location -ErrorAction Stop
    } catch {
        # Ignore failures when restoring location to avoid masking original errors
    }
}
