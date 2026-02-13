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
    Target repository (required). Format: owner/repo.

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

    [Parameter(Mandatory=$true)]
    [string]$Repository,

    [string]$TraceFix,

    [switch]$ShowCommits,

    [switch]$CheckMissing,

    [string]$Branch
)

# Use Continue for native command compat (PS 5.1 treats gh stderr as terminating with Stop)
$ErrorActionPreference = "Continue"

# --- Parallel support detection ---
$script:canParallel = $null -ne (Get-Command Start-ThreadJob -ErrorAction SilentlyContinue)

# --- Darc support detection ---
$script:hasDarc = (-not $env:CODEFLOW_NO_DARC) -and ($null -ne (Get-Command darc -ErrorAction SilentlyContinue))

# Start a background gh api call (returns Job if parallel, $null if not).
# Caller collects result with Complete-AsyncJob.
function Start-AsyncGitHubApi {
    param([string]$Endpoint, [switch]$Raw)
    if (-not $script:canParallel) { return $null }
    return Start-ThreadJob -ScriptBlock {
        param($ep, $isRaw)
        $ghArgs = @($ep)
        if ($isRaw) { $ghArgs += '-H'; $ghArgs += 'Accept: application/vnd.github.raw' }
        $result = gh api @ghArgs 2>$null
        if ($LASTEXITCODE -ne 0) { return $null }
        return ($result -join "`n")
    } -ArgumentList $Endpoint, [bool]$Raw
}

# Start a background gh CLI call with arbitrary arguments.
function Start-AsyncGh {
    param([string[]]$GhArgs)
    if (-not $script:canParallel) { return $null }
    return Start-ThreadJob -ScriptBlock {
        param($args_)
        $result = gh @args_ 2>$null
        if ($LASTEXITCODE -ne 0) { return $null }
        return ($result -join "`n")
    } -ArgumentList (,@($GhArgs))
}

# Collect result from an async job, or run synchronously if parallel isn't available.
# $FallbackBlock is invoked when $Job is $null (sequential fallback).
function Complete-AsyncJob {
    param($Job, [scriptblock]$FallbackBlock)
    if ($Job) { return Receive-Job -Job $Job -Wait -AutoRemoveJob }
    return & $FallbackBlock
}

# --- Helpers ---

function Invoke-GitHubApi {
    param(
        [string]$Endpoint,
        [switch]$Raw
    )
    try {
        $ghArgs = @($Endpoint)
        if ($Raw) {
            $ghArgs += '-H'
            $ghArgs += 'Accept: application/vnd.github.raw'
        }
        $result = gh api @ghArgs 2>$null
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

# Check an open codeflow PR for staleness/conflict warnings
# Returns a hashtable with: Status, Color, HasConflict, HasStaleness, WasResolved
function Get-CodeflowPRHealth {
    param([int]$PRNumber, [string]$Repo = "dotnet/dotnet")

    $result = @{ Status = "⚠️  Unknown"; Color = "Yellow"; HasConflict = $false; HasStaleness = $false; WasResolved = $false; Details = @() }

    $prJson = gh pr view $PRNumber -R $Repo --json body,comments,updatedAt,mergeable 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $prJson) { return $result }

    try { $prDetail = ($prJson -join "`n") | ConvertFrom-Json } catch { return $result }

    # If we got here, we can determine health
    $result.Status = "✅ Healthy"
    $result.Color = "Green"

    $hasConflict = $false
    $hasStaleness = $false
    if ($prDetail.comments) {
        foreach ($comment in $prDetail.comments) {
            if ($comment.author.login -match '^dotnet-maestro') {
                if ($comment.body -match 'codeflow cannot continue|the source repository has received code changes') { $hasStaleness = $true }
                if ($comment.body -match 'Conflict detected') { $hasConflict = $true }
            }
        }
    }

    $wasConflict = $hasConflict
    $wasStaleness = $hasStaleness

    # If issues detected, check if they were resolved
    # Two signals: (1) PR is mergeable (no git conflict), (2) Codeflow verification SUCCESS
    # Either one clears the conflict flag. Staleness needs a newer commit after the warning.
    if ($hasConflict -or $hasStaleness) {
        # Check mergeable status — if PR has no git conflicts, clear the conflict flag
        $isMergeable = $false
        if ($prDetail.PSObject.Properties.Name -contains 'mergeable' -and $prDetail.mergeable -eq 'MERGEABLE') {
            $isMergeable = $true
        }
        if ($isMergeable -and $hasConflict) {
            $hasConflict = $false
        }

        $checksJson = gh pr checks $PRNumber -R $Repo --json name,state 2>$null
        if ($LASTEXITCODE -eq 0 -and $checksJson) {
            try {
                $checks = ($checksJson -join "`n") | ConvertFrom-Json
                $codeflowCheck = @($checks | Where-Object { $_.name -match 'Codeflow verification' }) | Select-Object -First 1
                if (($codeflowCheck -and $codeflowCheck.state -eq 'SUCCESS') -or $isMergeable) {
                    # No merge conflict — either Codeflow verification passes or PR is mergeable
                    $hasConflict = $false
                    # For staleness, check if there are commits after the last staleness warning
                    if ($hasStaleness) {
                        $commitsJson = gh pr view $PRNumber -R $Repo --json commits --jq '.commits[-1].committedDate' 2>$null
                        if ($LASTEXITCODE -eq 0 -and $commitsJson) {
                            $lastCommitTime = ($commitsJson -join "").Trim()
                            $lastWarnTime = $null
                            foreach ($comment in $prDetail.comments) {
                                if ($comment.author.login -match '^dotnet-maestro' -and $comment.body -match 'codeflow cannot continue|the source repository has received code changes') {
                                    $warnDt = [DateTimeOffset]::Parse($comment.createdAt).UtcDateTime
                                    if (-not $lastWarnTime -or $warnDt -gt $lastWarnTime) {
                                        $lastWarnTime = $warnDt
                                    }
                                }
                            }
                            $commitDt = if ($lastCommitTime) { [DateTimeOffset]::Parse($lastCommitTime).UtcDateTime } else { $null }
                            if ($lastWarnTime -and $commitDt -and $commitDt -gt $lastWarnTime) {
                                $hasStaleness = $false
                            }
                        }
                    }
                }
            } catch { }
        }
    }

    if ($hasConflict) {
        $result.Status = "🔴 Conflict"
        $result.Color = "Red"
        $result.HasConflict = $true
    }
    elseif ($hasStaleness) {
        $result.Status = "⚠️  Stale"
        $result.Color = "Yellow"
        $result.HasStaleness = $true
    }
    else {
        if ($wasConflict) { $result.Status = "✅ Conflict resolved"; $result.WasResolved = $true }
        elseif ($wasStaleness) { $result.Status = "✅ Updated since staleness warning"; $result.WasResolved = $true }
    }

    return $result
}

function Get-VMRBuildFreshness {
    param([string]$VMRBranch)

    # Try darc first — gives exact commit, build date, build link
    if ($script:hasDarc) {
        $result = Get-VMRBuildFreshnessDarc -VMRBranch $VMRBranch
        if ($result) { return $result }
    }
    return Get-VMRBuildFreshnessAkaMs -VMRBranch $VMRBranch
}

function Get-VMRBuildFreshnessDarc {
    param([string]$VMRBranch)

    # Map VMR branch to channel name
    # Channels: ".NET 11.0.1xx SDK", ".NET 11.0.1xx SDK Preview 1"
    $channelName = $null
    if ($VMRBranch -eq "main") {
        $currentMajor = [DateTime]::UtcNow.Year - 2015
        $channelName = ".NET $currentMajor.0.1xx SDK"
    }
    elseif ($VMRBranch -match 'release/(\d+\.\d+\.\d+xx)-preview\.?(\d+)') {
        $channelName = ".NET $($Matches[1]) SDK Preview $($Matches[2])"
    }
    elseif ($VMRBranch -match 'release/(\d+\.\d+\.\d+xx)') {
        $channelName = ".NET $($Matches[1]) SDK"
    }
    if (-not $channelName) { return $null }

    try {
        $output = darc get-latest-build --repo dotnet/dotnet --channel $channelName 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $output) { return $null }
        $text = $output -join "`n"

        $commit = if ($text -match 'Commit:\s+(\S+)') { $Matches[1] } else { $null }
        $buildNum = if ($text -match 'Build Number:\s+(\S+)') { $Matches[1] } else { $null }
        $buildLink = if ($text -match 'Build Link:\s+(\S+)') { $Matches[1] } else { $null }
        $dateStr = if ($text -match 'Date Produced:\s+(.+)') { $Matches[1].Trim() } else { $null }

        if (-not $dateStr) { return $null }
        $published = [DateTime]::Parse($dateStr, [System.Globalization.CultureInfo]::InvariantCulture).ToUniversalTime()

        # Extract channel short name for display (e.g., "11.0.1xx" or "11.0.1xx-preview1")
        $channelShort = if ($VMRBranch -eq "main") { "$([DateTime]::UtcNow.Year - 2015).0.1xx" }
                        elseif ($VMRBranch -match 'release/(\d+\.\d+\.\d+xx(?:-preview\.?\d+)?)') { $Matches[1] }
                        else { $channelName }

        $released = if ($text -match 'Released:\s+(True|False)') { $Matches[1] -eq 'True' } else { $false }

        return @{
            Channel   = $channelShort
            Version   = $buildNum
            Published = $published
            Age       = [DateTime]::UtcNow - $published
            Commit    = $commit
            BuildLink = $buildLink
            Released  = $released
            Source    = "darc"
        }
    } catch { return $null }
}

function Get-SubscriptionHealth {
    param(
        [string]$TargetRepo,
        [string]$TargetBranch,
        [hashtable]$BuildFreshness  # optional — reuse if already fetched
    )

    if (-not $script:hasDarc) { return $null }

    try {
        # Get subscriptions for this repo from dotnet/dotnet
        $subOutput = darc get-subscriptions --source-repo dotnet/dotnet --target-repo $TargetRepo 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $subOutput) { return $null }

        # Parse subscription blocks — each starts with the header line
        # "https://...dotnet/dotnet (channel) ==> 'target' ('branch')"
        $subText = $subOutput -join "`n"
        $blocks = $subText -split '(?=https://github\.com/dotnet/dotnet\s+\()'
        $blocks = $blocks | Where-Object { $_.Trim() }

        # Find the subscription matching our target branch
        $matchedBlock = $null
        foreach ($block in $blocks) {
            if ($block -match "==>\s+'[^']+'\s+\('([^']+)'\)") {
                $subBranch = $Matches[1]
                if ($subBranch -eq $TargetBranch) {
                    $matchedBlock = $block
                    break
                }
            }
        }

        if (-not $matchedBlock) {
            return @{
                Diagnostic = "subscription-missing"
                Message    = "No subscription found for $TargetRepo/$TargetBranch"
            }
        }

        # Parse subscription fields
        $subId = if ($matchedBlock -match 'Id:\s+(\S+)') { $Matches[1] } else { $null }
        $enabled = if ($matchedBlock -match 'Enabled:\s+(True|False)') { $Matches[1] -eq 'True' } else { $true }
        $updateFreq = if ($matchedBlock -match 'Update Frequency:\s+(\S+)') { $Matches[1] } else { "Unknown" }
        $lastBuildNum = $null; $lastBuildCommit = $null
        if ($matchedBlock -match 'Last Build:\s+(\S+)\s+\((\S+)\)') {
            $lastBuildNum = $Matches[1]
            $lastBuildCommit = $Matches[2]
        }

        # Extract channel name from header
        $channelName = $null
        if ($matchedBlock -match 'dotnet/dotnet\s+\(([^)]+)\)') {
            $channelName = $Matches[1]
        }

        if (-not $enabled) {
            return @{
                Diagnostic     = "subscription-disabled"
                SubscriptionId = $subId
                Channel        = $channelName
                UpdateFrequency = $updateFreq
                Message        = "Subscription $subId is disabled"
            }
        }

        # Get latest build for comparison (reuse if provided)
        $latestBuild = $BuildFreshness
        if (-not $latestBuild -and $channelName) {
            $latestBuild = try {
                $bOutput = darc get-latest-build --repo dotnet/dotnet --channel $channelName 2>$null
                if ($LASTEXITCODE -eq 0 -and $bOutput) {
                    $bText = $bOutput -join "`n"
                    $bCommit = if ($bText -match 'Commit:\s+(\S+)') { $Matches[1] } else { $null }
                    $bNum = if ($bText -match 'Build Number:\s+(\S+)') { $Matches[1] } else { $null }
                    $bReleased = if ($bText -match 'Released:\s+(True|False)') { $Matches[1] -eq 'True' } else { $false }
                    $bDateStr = if ($bText -match 'Date Produced:\s+(.+)') { $Matches[1].Trim() } else { $null }
                    $bDate = if ($bDateStr) { [DateTime]::Parse($bDateStr, [System.Globalization.CultureInfo]::InvariantCulture).ToUniversalTime() } else { $null }
                    @{ Commit = $bCommit; Version = $bNum; Released = $bReleased; Published = $bDate }
                }
            } catch { $null }
        }

        if (-not $latestBuild) {
            return @{
                Diagnostic     = "unknown"
                SubscriptionId = $subId
                Channel        = $channelName
                UpdateFrequency = $updateFreq
                LastBuild      = $lastBuildNum
                Message        = "Could not fetch latest build for channel $channelName"
            }
        }

        # Check if channel is frozen (released preview)
        if ($latestBuild.Released -and $lastBuildCommit -and $latestBuild.Commit -and
            $lastBuildCommit -eq $latestBuild.Commit) {
            return @{
                Diagnostic     = "channel-frozen"
                SubscriptionId = $subId
                Channel        = $channelName
                UpdateFrequency = $updateFreq
                LastBuild      = $lastBuildNum
                LatestBuild    = $latestBuild.Version
                Message        = "Channel frozen — latest build is released, subscription is up to date"
            }
        }

        # Check if Maestro is stuck (last-flowed build != latest build)
        if ($lastBuildCommit -and $latestBuild.Commit -and $lastBuildCommit -ne $latestBuild.Commit) {
            $behindMsg = "LastBuild=$lastBuildNum but latest=$($latestBuild.Version)"
            # Check if frequency-limited — only applies when the gap is small
            # Build numbers are date-based (YYYYMMDD.N), so we can compare dates
            if ($updateFreq -eq "EveryDay" -and $latestBuild.Published -and $lastBuildNum) {
                $buildAge = [DateTime]::UtcNow - $latestBuild.Published
                # Only frequency-limited if last-flowed build is from the previous day or two
                $lastBuildDate = $null
                if ($lastBuildNum -match '^(\d{4})(\d{2})(\d{2})\.') {
                    try { $lastBuildDate = [DateTime]::SpecifyKind([DateTime]::new([int]$Matches[1], [int]$Matches[2], [int]$Matches[3]), [DateTimeKind]::Utc) } catch {}
                }
                $lastBuildAge = if ($lastBuildDate) { ([DateTime]::UtcNow - $lastBuildDate).TotalDays } else { 999 }
                if ($buildAge.TotalHours -lt 24 -and $lastBuildAge -lt 3) {
                    return @{
                        Diagnostic     = "frequency-limited"
                        SubscriptionId = $subId
                        Channel        = $channelName
                        UpdateFrequency = $updateFreq
                        LastBuild      = $lastBuildNum
                        LatestBuild    = $latestBuild.Version
                        Message        = "EveryDay subscription — latest build is $([math]::Round($buildAge.TotalHours,1))h old, next update within 24h"
                    }
                }
            }
            return @{
                Diagnostic     = "maestro-stuck"
                SubscriptionId = $subId
                Channel        = $channelName
                UpdateFrequency = $updateFreq
                LastBuild      = $lastBuildNum
                LastBuildCommit = $lastBuildCommit
                LatestBuild    = $latestBuild.Version
                LatestCommit   = $latestBuild.Commit
                Message        = "Maestro stuck — $behindMsg"
            }
        }

        # No Last Build data — can't determine health
        if (-not $lastBuildCommit) {
            return @{
                Diagnostic     = "unknown"
                SubscriptionId = $subId
                Channel        = $channelName
                UpdateFrequency = $updateFreq
                LastBuild      = $lastBuildNum
                Message        = "No Last Build data — subscription health unknown"
            }
        }

        # Subscription is healthy
        return @{
            Diagnostic     = "healthy"
            SubscriptionId = $subId
            Channel        = $channelName
            UpdateFrequency = $updateFreq
            LastBuild      = $lastBuildNum
            LatestBuild    = $latestBuild.Version
            Message        = "Subscription is up to date"
        }
    } catch { return $null }
}

function Get-VMRBuildFreshnessAkaMs {
    param([string]$VMRBranch)

    # Map VMR branch to aka.ms channel
    $channel = $null
    $blobUrl = $null

    Add-Type -AssemblyName System.Net.Http -ErrorAction SilentlyContinue
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(15)

    try {
        if ($VMRBranch -eq "main") {
            # Dynamically generate candidate channels. Try current major first,
            # then next (for early-year transitions), then previous as fallback.
            # .NET version = current year - 2015 (e.g., 2026 → 11, 2027 → 12)
            $currentMajor = [DateTime]::UtcNow.Year - 2015
            $tryChannels = @("$currentMajor.0.1xx", "$($currentMajor+1).0.1xx", "$($currentMajor-1).0.1xx")
            foreach ($ch in $tryChannels) {
                try {
                    $resp = $client.GetAsync("https://aka.ms/dotnet/$ch/daily/dotnet-sdk-win-x64.zip").Result
                    if ([int]$resp.StatusCode -eq 301 -and $resp.Headers.Location) {
                        $channel = $ch
                        $blobUrl = $resp.Headers.Location.ToString()
                        $resp.Dispose()
                        break
                    }
                    $resp.Dispose()
                } catch { }
            }
        }
        elseif ($VMRBranch -match 'release/(\d+\.\d+\.\d+xx-preview\.?\d+)') {
            # aka.ms uses "preview1" not "preview.1"
            $channel = $Matches[1] -replace 'preview\.', 'preview'
        }
        elseif ($VMRBranch -match 'release/(\d+\.\d+)\.(\d)xx') {
            $channel = "$($Matches[1]).$($Matches[2])xx"
        }

        if (-not $channel) { return $null }

        if (-not $blobUrl) {
            $resp = $client.GetAsync("https://aka.ms/dotnet/$channel/daily/dotnet-sdk-win-x64.zip").Result
            if ([int]$resp.StatusCode -ne 301 -or -not $resp.Headers.Location) {
                $resp.Dispose()
                return $null
            }
            $blobUrl = $resp.Headers.Location.ToString()
            $resp.Dispose()
        }

        $version = if ($blobUrl -match '/Sdk/([^/]+)/') { $Matches[1] } else { $null }

        # Use HttpClient HEAD (consistent with above, avoids mixing Invoke-WebRequest)
        # Need a separate client with auto-redirect enabled for the blob URL
        $blobHandler = [System.Net.Http.HttpClientHandler]::new()
        $blobClient = [System.Net.Http.HttpClient]::new($blobHandler)
        $blobClient.Timeout = [TimeSpan]::FromSeconds(15)
        $published = $null
        try {
            $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Head, $blobUrl)
            $headResp = $blobClient.SendAsync($request, [System.Net.Http.HttpCompletionOption]::ResponseHeadersRead).Result
            # PowerShell unwraps Nullable<DateTimeOffset> — use cast, not .Value
            $lastMod = $headResp.Content.Headers.LastModified
            if ($null -eq $lastMod) { $lastMod = $headResp.Headers.LastModified }
            if ($null -ne $lastMod) { $published = ([DateTimeOffset]$lastMod).UtcDateTime }
        }
        finally {
            if ($headResp) { $headResp.Dispose() }
            if ($request) { $request.Dispose() }
            $blobClient.Dispose()
            $blobHandler.Dispose()
        }

        if (-not $published) { return $null }
        return @{
            Channel   = $channel
            Version   = $version
            Published = $published
            Age       = [DateTime]::UtcNow - $published
        }
    }
    catch {
        return $null
    }
    finally {
        if ($client) { $client.Dispose() }
    }
}

# --- Parse repo owner/name ---
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
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

    # dotnet/dotnet doesn't have backflow from itself — skip to forward flow + build freshness
    if ($Repository -eq "dotnet/dotnet") {
        Write-Host "  ℹ️  VMR (dotnet/dotnet) does not have backflow from itself" -ForegroundColor DarkGray

        # Still show build freshness for the VMR
        $vmrBranches = @{}
        if ($Branch -eq "main" -or -not $Branch) { $vmrBranches["main"] = "main" }
        if ($Branch -match 'release/' -or -not $Branch) {
            # Try to detect release branches
            $branchesJson = gh api "/repos/dotnet/dotnet/branches?per_page=30" --jq '.[].name' 2>$null
            if ($LASTEXITCODE -eq 0 -and $branchesJson) {
                foreach ($b in ($branchesJson -split "`n")) {
                    if ($b -match '^release/') { $vmrBranches[$b] = $b }
                }
            }
        }
        $checkedChannels = @{}
        $anyVeryStale = $false
        if ($vmrBranches.Count -gt 0) {
            $freshnessSource = if ($script:hasDarc) { "darc" } else { "aka.ms" }
            Write-Section "Official Build Freshness (via $freshnessSource)"
            foreach ($entry in $vmrBranches.GetEnumerator()) {
                $freshness = Get-VMRBuildFreshness -VMRBranch $entry.Value
                if ($freshness -and -not $checkedChannels.ContainsKey($freshness.Channel)) {
                    $checkedChannels[$freshness.Channel] = $freshness
                    $ageDays = $freshness.Age.TotalDays
                    $ageStr = if ($ageDays -ge 1) { "$([math]::Round($ageDays, 1))d" } else { "$([math]::Round($freshness.Age.TotalHours, 1))h" }
                    $color = if ($ageDays -gt 3) { 'Red' } elseif ($ageDays -gt 1) { 'Yellow' } else { 'Green' }
                    $versionStr = if ($freshness.Version) { $freshness.Version } else { "unknown" }
                    $branchLabel = "$($entry.Key) → $($freshness.Channel)"
                    Write-Host "  $($branchLabel.PadRight(40)) $($versionStr.PadRight(48)) $($freshness.Published.ToString('yyyy-MM-dd HH:mm')) UTC  ($ageStr ago)" -ForegroundColor $color
                    if ($ageDays -gt 3) { $anyVeryStale = $true }
                }
            }
            if ($anyVeryStale) {
                Write-Host ""
                Write-Host "  ⚠️  Official builds appear stale — VMR may be failing to build" -ForegroundColor Yellow
                Write-Host "    Check https://dev.azure.com/dnceng-public/public/_build?definitionId=278 for public CI failures" -ForegroundColor DarkGray
                Write-Host "    See also: https://github.com/dotnet/dotnet/issues?q=is:issue+is:open+%22Operational+Issue%22" -ForegroundColor DarkGray
            }
        }

        # Emit structured summary for dotnet/dotnet
        $vmrSummary = [ordered]@{
            mode        = "flow-health"
            repository  = $Repository
            backflow    = [ordered]@{ covered = 0; upToDate = 0; blocked = 0; missing = 0 }
            forwardFlow = [ordered]@{ healthy = 0; stale = 0; conflicted = 0 }
            buildFreshness = @($checkedChannels.Values | ForEach-Object {
                $entry = [ordered]@{ channel = $_.Channel; version = $_.Version; ageDays = [math]::Round($_.Age.TotalDays, 1); published = $_.Published.ToString("o") }
                if ($_.Commit) { $entry.commit = $_.Commit }
                if ($_.BuildLink) { $entry.buildLink = $_.BuildLink }
                if ($_.Source) { $entry.source = $_.Source }
                $entry
            })
            buildsAreStale = $anyVeryStale
        }
        Write-Host ""
        Write-Host "[CODEFLOW_SUMMARY]"
        Write-Host ($vmrSummary | ConvertTo-Json -Depth 4 -Compress)
        Write-Host "[/CODEFLOW_SUMMARY]"

        return
    }

    # Find open backflow PRs (to know which branches are already covered)
    $openPRsJson = gh search prs --repo $Repository --author "dotnet-maestro[bot]" --state open "Source code updates from dotnet/dotnet" --json number,title --limit 50 2>$null
    $openPRs = @()
    $ghSearchFailed = $false
    if ($LASTEXITCODE -eq 0 -and $openPRsJson) {
        try { $openPRs = ($openPRsJson -join "`n") | ConvertFrom-Json } catch { $openPRs = @() }
    }
    elseif ($LASTEXITCODE -ne 0) {
        Write-Warning "gh search failed (exit code $LASTEXITCODE). Check authentication with 'gh auth status'."
        $ghSearchFailed = $true
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
    elseif ($LASTEXITCODE -ne 0 -and -not $ghSearchFailed) {
        Write-Warning "gh search for merged PRs failed (exit code $LASTEXITCODE). Results may be incomplete."
    }

    if ($mergedPRs.Count -eq 0 -and $openPRs.Count -eq 0) {
        if ($ghSearchFailed) {
            Write-Host "  ❌ Could not query GitHub. Check 'gh auth status' and rate limits." -ForegroundColor Red
        }
        else {
            Write-Host "  No backflow PRs found (open or recently merged). This repo may not have backflow subscriptions." -ForegroundColor Yellow
        }
        return
    }

    # Group merged PRs by branch, keeping only the most recently merged per branch
    $branchLastMerged = @{}
    foreach ($mpr in $mergedPRs) {
        if ($mpr.title -match '^\[([^\]]+)\]') {
            $branchName = $Matches[1]
            if ($Branch -and $branchName -ne $Branch) { continue }
            if (-not $branchLastMerged.ContainsKey($branchName)) {
                $branchLastMerged[$branchName] = $mpr
            }
            else {
                # Keep the one with the later closedAt (actual merge time)
                $existing = $branchLastMerged[$branchName]
                if ($mpr.closedAt -and $existing.closedAt -and $mpr.closedAt -gt $existing.closedAt) {
                    $branchLastMerged[$branchName] = $mpr
                }
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
    $blockedCount = 0
    $vmrBranchesFound = @{}
    $cachedPRBodies = @{}
    $subscriptionDiagnostics = @()

    # First pass: collect VMR branch mappings from merged PRs (needed for build freshness)
    # Fetch all PR bodies in parallel (falls back to sequential on PS 5.1)
    $prBodyJobs = @{}
    foreach ($branchName in ($branchLastMerged.Keys | Sort-Object)) {
        if ($openBranches.ContainsKey($branchName)) { continue }
        $lastPR = $branchLastMerged[$branchName]
        $prBodyJobs[$branchName] = Start-AsyncGh @('pr', 'view', $lastPR.number.ToString(), '-R', $Repository, '--json', 'body')
    }

    foreach ($branchName in ($branchLastMerged.Keys | Sort-Object)) {
        if ($openBranches.ContainsKey($branchName)) { continue }
        $lastPR = $branchLastMerged[$branchName]
        $result = Complete-AsyncJob $prBodyJobs[$branchName] {
            try {
                $json = gh pr view $lastPR.number -R $Repository --json body 2>$null
                if ($LASTEXITCODE -ne 0) { return $null }
                return ($json -join "`n")
            } catch { return $null }
        }
        if (-not $result) { continue }
        try { $prDetail = $result | ConvertFrom-Json } catch { continue }
        $cachedPRBodies[$branchName] = $prDetail
        $vmrBranchFromPR = $null
        if ($prDetail.body -match '\*\*Branch\*\*:\s*\[([^\]]+)\]') { $vmrBranchFromPR = $Matches[1] }
        if ($vmrBranchFromPR) { $vmrBranchesFound[$branchName] = $vmrBranchFromPR }
    }

    # --- Official build freshness check (shown first for context) ---
    $buildsAreStale = $false
    if ($vmrBranchesFound.Count -gt 0) {
        $freshnessSource = if ($script:hasDarc) { "darc" } else { "aka.ms" }
        Write-Section "Official Build Freshness (via $freshnessSource)"
        $checkedChannels = @{}
        foreach ($entry in $vmrBranchesFound.GetEnumerator()) {
            $freshness = Get-VMRBuildFreshness -VMRBranch $entry.Value
            if ($freshness -and -not $checkedChannels.ContainsKey($freshness.Channel)) {
                $checkedChannels[$freshness.Channel] = $freshness
                $ageDays = $freshness.Age.TotalDays
                $ageStr = if ($ageDays -ge 1) { "$([math]::Round($ageDays, 1))d" } else { "$([math]::Round($freshness.Age.TotalHours, 1))h" }
                $color = if ($ageDays -gt 3) { 'Red' } elseif ($ageDays -gt 1) { 'Yellow' } else { 'Green' }
                $isPreviewChannel = $freshness.Channel -match 'preview'
                # Preview channels with old builds are expected post-release — don't flag as stale
                if ($isPreviewChannel -and $ageDays -gt 14) {
                    $color = 'DarkGray'
                    $ageStr = "$ageStr (preview likely released)"
                }
                elseif (-not $isPreviewChannel -and $ageDays -gt 3) {
                    $buildsAreStale = $true
                }
                $versionStr = if ($freshness.Version) { $freshness.Version } else { "unknown" }
                $branchLabel = "$($entry.Key) → $($freshness.Channel)"
                Write-Host "  $($branchLabel.PadRight(40)) $($versionStr.PadRight(48)) $($freshness.Published.ToString('yyyy-MM-dd HH:mm')) UTC  ($ageStr ago)" -ForegroundColor $color
            }
        }
        if ($buildsAreStale) {
            Write-Host ""
            Write-Host "  ⚠️  Official builds appear stale — VMR may be failing to build" -ForegroundColor Yellow
            Write-Host "    Missing backflow PRs below are likely caused by this, not a Maestro issue" -ForegroundColor DarkGray
            Write-Host "    Check https://dev.azure.com/dnceng-public/public/_build?definitionId=278 for public CI failures" -ForegroundColor DarkGray
            Write-Host "    See also: https://github.com/dotnet/dotnet/issues?q=is:issue+is:open+%22Operational+Issue%22" -ForegroundColor DarkGray
        }
    }

    # --- Per-branch backflow analysis ---
    # Pre-fetch VMR branch HEADs in parallel for branches without open PRs
    $vmrHeadJobs = @{}
    $vmrHeadBranches = @{}  # Track which branches we need to fetch
    foreach ($branchName in ($branchLastMerged.Keys | Sort-Object)) {
        if ($openBranches.ContainsKey($branchName)) { continue }
        $vmrBranchFromPR = $vmrBranchesFound[$branchName]
        if (-not $vmrBranchFromPR) { continue }
        $prDetail = $cachedPRBodies[$branchName]
        if (-not $prDetail) { continue }
        if ($prDetail.body -notmatch '\*\*Commit\*\*:\s*\[([a-fA-F0-9]+)\]') { continue }
        $encodedVmrBranch = [uri]::EscapeDataString($vmrBranchFromPR)
        $vmrHeadBranches[$branchName] = $encodedVmrBranch
        $vmrHeadJobs[$branchName] = Start-AsyncGitHubApi -Endpoint "/repos/dotnet/dotnet/commits/$encodedVmrBranch"
    }

    # Also pre-fetch health for open backflow PRs in parallel
    $healthJobs = @{}
    foreach ($branchName in ($branchLastMerged.Keys | Sort-Object)) {
        if ($openBranches.ContainsKey($branchName)) {
            $healthJobs[$branchName] = Start-AsyncGh @('pr', 'view', $openBranches[$branchName].ToString(), '-R', $Repository, '--json', 'body,comments,updatedAt,mergeable')
        }
    }

    # Collect VMR HEAD results
    $cachedVmrHeads = @{}
    foreach ($branchName in $vmrHeadBranches.Keys) {
        $result = Complete-AsyncJob $vmrHeadJobs[$branchName] {
            try {
                $json = gh api "/repos/dotnet/dotnet/commits/$($vmrHeadBranches[$branchName])" 2>$null
                if ($LASTEXITCODE -ne 0) { return $null }
                return ($json -join "`n")
            } catch { return $null }
        }
        if ($result) {
            try { $cachedVmrHeads[$branchName] = $result | ConvertFrom-Json } catch { }
        }
    }

    # Collect health results
    $cachedHealthData = @{}
    foreach ($branchName in $healthJobs.Keys) {
        $result = Complete-AsyncJob $healthJobs[$branchName] {
            try {
                $json = gh pr view $openBranches[$branchName] -R $Repository --json body,comments,updatedAt,mergeable 2>$null
                if ($LASTEXITCODE -ne 0) { return $null }
                return ($json -join "`n")
            } catch { return $null }
        }
        if ($result) { $cachedHealthData[$branchName] = $result }
    }

    Write-Section "Backflow status ($Repository ← dotnet/dotnet)"

    foreach ($branchName in ($branchLastMerged.Keys | Sort-Object)) {
        $lastPR = $branchLastMerged[$branchName]
        Write-Host ""
        Write-Host "  Branch: $branchName" -ForegroundColor White

        if ($openBranches.ContainsKey($branchName)) {
            # Use pre-fetched health data — simplified check (no mergeable/verification probes)
            $bfHealth = @{ Status = "⚠️  Unknown"; Color = "Yellow"; HasConflict = $false; HasStaleness = $false; WasResolved = $false }
            $healthJson = $cachedHealthData[$branchName]
            if ($healthJson) {
                try {
                    $prDetailH = $healthJson | ConvertFrom-Json
                    $bfHealth.Status = "✅ Healthy"; $bfHealth.Color = "Green"
                    # Check mergeable state if available
                    if ($prDetailH.mergeable -eq 'CONFLICTING') { $bfHealth.HasConflict = $true }
                    if ($prDetailH.comments) {
                        foreach ($comment in $prDetailH.comments) {
                            if ($comment.author.login -match '^dotnet-maestro') {
                                if ($comment.body -match 'codeflow cannot continue|the source repository has received code changes') { $bfHealth.HasStaleness = $true }
                                if ($comment.body -match 'Conflict detected') { $bfHealth.HasConflict = $true }
                            }
                        }
                    }
                    if ($bfHealth.HasConflict) { $bfHealth.Status = "🔴 Conflict"; $bfHealth.Color = "Red" }
                    elseif ($bfHealth.HasStaleness) { $bfHealth.Status = "⚠️  Stale"; $bfHealth.Color = "Yellow" }
                } catch { }
            }
            Write-Host "    Open backflow PR #$($openBranches[$branchName]): $($bfHealth.Status)" -ForegroundColor $bfHealth.Color
            if ($bfHealth.HasConflict -or $bfHealth.HasStaleness) { $blockedCount++ }
            elseif ($bfHealth.Status -notlike '*Unknown*') { $coveredCount++ }
            continue
        }

        # Get the PR body to extract VMR commit (branch already collected above)
        $vmrBranchFromPR = $vmrBranchesFound[$branchName]
        if (-not $vmrBranchFromPR) {
            Write-Host "    ⚠️  Could not determine VMR branch from last merged PR" -ForegroundColor Yellow
            continue
        }

        # Use cached PR body from first pass
        $prDetail = $cachedPRBodies[$branchName]
        if (-not $prDetail) {
            Write-Host "    ⚠️  Could not fetch PR details" -ForegroundColor Yellow
            continue
        }

        $vmrCommitFromPR = $null
        if ($prDetail.body -match '\*\*Commit\*\*:\s*\[([a-fA-F0-9]+)\]') {
            $vmrCommitFromPR = $Matches[1]
        }

        if (-not $vmrCommitFromPR) {
            Write-Host "    ⚠️  Could not parse VMR commit from last merged PR #$($lastPR.number)" -ForegroundColor Yellow
            continue
        }

        Write-Host "    Last merged: PR #$($lastPR.number) on $($lastPR.closedAt)" -ForegroundColor DarkGray
        Write-Host "    VMR branch: $vmrBranchFromPR" -ForegroundColor DarkGray
        Write-Host "    VMR commit: $(Get-ShortSha $vmrCommitFromPR)" -ForegroundColor DarkGray

        # Get current VMR branch HEAD (from pre-fetched cache)
        $vmrHead = $cachedVmrHeads[$branchName]
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
            # Check if this is a released preview branch (expected to stop flowing)
            $mergedTime = [DateTimeOffset]::Parse($lastPR.closedAt).UtcDateTime
            $elapsed = [DateTime]::UtcNow - $mergedTime
            $isReleasedPreview = $false
            $vmrBranchFromPR = $vmrBranchesFound[$branchName]
            if (($vmrBranchFromPR -match 'preview') -and ($elapsed.TotalDays -gt 14)) {
                $isReleasedPreview = $true
            }

            if ($isReleasedPreview) {
                Write-Host "    ℹ️  Preview likely released — no further backflow expected" -ForegroundColor DarkGray
                if ($script:hasDarc) {
                    $subHealth = Get-SubscriptionHealth -TargetRepo $Repository -TargetBranch $branchName
                    if ($subHealth) {
                        $subscriptionDiagnostics += [ordered]@{ branch = $branchName; diagnostic = $subHealth.Diagnostic; subscriptionId = $subHealth.SubscriptionId; channel = $subHealth.Channel; lastBuild = $subHealth.LastBuild; latestBuild = $subHealth.LatestBuild; message = $subHealth.Message }
                        Write-Host "    📋 Subscription: $($subHealth.Message)" -ForegroundColor DarkGray
                    }
                }
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

                if ($elapsed.TotalHours -gt 6) {
                    if ($buildsAreStale) {
                        Write-Host "    ℹ️  No new official build available — backflow blocked upstream" -ForegroundColor DarkGray
                    }
                    else {
                        Write-Host "    ⚠️  Last PR merged $([math]::Round($elapsed.TotalHours, 1)) hours ago — Maestro may be stuck" -ForegroundColor Yellow
                        # Cross-reference with darc subscription data
                        if ($script:hasDarc) {
                            $subHealth = Get-SubscriptionHealth -TargetRepo $Repository -TargetBranch $branchName
                            if ($subHealth) {
                                $subscriptionDiagnostics += [ordered]@{ branch = $branchName; diagnostic = $subHealth.Diagnostic; subscriptionId = $subHealth.SubscriptionId; channel = $subHealth.Channel; lastBuild = $subHealth.LastBuild; latestBuild = $subHealth.LatestBuild; message = $subHealth.Message }
                                $diagColor = if ($subHealth.Diagnostic -in @("maestro-stuck","subscription-disabled","subscription-missing")) { "Yellow" } else { "DarkGray" }
                                Write-Host "    📋 Subscription: $($subHealth.Message)" -ForegroundColor $diagColor
                            }
                        }
                    }
                }
                else {
                    Write-Host "    ℹ️  Last PR merged $([math]::Round($elapsed.TotalHours, 1)) hours ago — Maestro may still be processing" -ForegroundColor DarkGray
                }
                $missingCount++
            }
        }
    }

    # Also check open-only branches (that weren't in merged list)
    foreach ($branchName in ($openBranches.Keys | Sort-Object)) {
        if (-not $branchLastMerged.ContainsKey($branchName)) {
            if ($Branch -and $branchName -ne $Branch) { continue }
            Write-Host ""
            Write-Host "  Branch: $branchName" -ForegroundColor White
            $bfHealth = Get-CodeflowPRHealth -PRNumber $openBranches[$branchName] -Repo $Repository
            Write-Host "    Open backflow PR #$($openBranches[$branchName]): $($bfHealth.Status)" -ForegroundColor $bfHealth.Color
            if ($bfHealth.HasConflict -or $bfHealth.HasStaleness) { $blockedCount++ }
            elseif ($bfHealth.Status -notlike '*Unknown*') { $coveredCount++ }
        }
    }

    # --- Forward flow: check PRs from this repo into the VMR ---
    $repoShortName = $Repository -replace '^dotnet/', ''
    Write-Host ""
    Write-Section "Forward flow PRs ($Repository → dotnet/dotnet)"

    $fwdPRsJson = gh search prs --repo dotnet/dotnet --author "dotnet-maestro[bot]" --state open "Source code updates from dotnet/$repoShortName" --json number,title --limit 10 2>$null
    $fwdPRs = @()
    if ($LASTEXITCODE -eq 0 -and $fwdPRsJson) {
        try { $fwdPRs = ($fwdPRsJson -join "`n") | ConvertFrom-Json } catch { $fwdPRs = @() }
    }
    # Filter to exact repo match (avoid dotnet/sdk matching dotnet/sdk-container-builds)
    $fwdPRs = @($fwdPRs | Where-Object { $_.title -match "from dotnet/$([regex]::Escape($repoShortName))$" })

    $fwdHealthy = 0
    $fwdStale = 0
    $fwdConflict = 0

    if ($fwdPRs.Count -eq 0) {
        Write-Host "  No open forward flow PRs found" -ForegroundColor DarkGray
    }
    else {
        foreach ($fpr in $fwdPRs) {
            $fprBranch = if ($fpr.title -match '^\[([^\]]+)\]') { $Matches[1] } else { "unknown" }
            if ($Branch -and $fprBranch -ne $Branch) { continue }

            $fwdHealth = Get-CodeflowPRHealth -PRNumber $fpr.number -Repo "dotnet/dotnet"

            if ($fwdHealth.HasConflict) { $fwdConflict++ }
            elseif ($fwdHealth.HasStaleness) { $fwdStale++ }
            elseif ($fwdHealth.Status -notlike '*Unknown*') { $fwdHealthy++ }

            Write-Host "  PR #$($fpr.number) [$fprBranch]: $($fwdHealth.Status)" -ForegroundColor $fwdHealth.Color
            Write-Host "    https://github.com/dotnet/dotnet/pull/$($fpr.number)" -ForegroundColor DarkGray
        }
    }

    Write-Section "Summary"
    Write-Host "  Backflow ($Repository ← dotnet/dotnet):" -ForegroundColor White
    if ($coveredCount -gt 0) { Write-Host "    Branches with healthy open PRs: $coveredCount" -ForegroundColor Green }
    if ($upToDateCount -gt 0) { Write-Host "    Branches up to date: $upToDateCount" -ForegroundColor Green }
    if ($blockedCount -gt 0) { Write-Host "    Branches with blocked open PRs: $blockedCount" -ForegroundColor Red }
    if ($missingCount -gt 0) {
        Write-Host "    Branches MISSING backflow PRs: $missingCount" -ForegroundColor Red
    }
    if ($missingCount -eq 0 -and $blockedCount -eq 0) {
        Write-Host "    No missing backflow PRs ✅" -ForegroundColor Green
    }
    Write-Host "  Forward flow ($Repository → dotnet/dotnet):" -ForegroundColor White
    if ($fwdPRs.Count -eq 0) {
        Write-Host "    No open forward flow PRs" -ForegroundColor DarkGray
    }
    else {
        if ($fwdHealthy -gt 0) { Write-Host "    Healthy: $fwdHealthy" -ForegroundColor Green }
        if ($fwdStale -gt 0) { Write-Host "    Stale: $fwdStale" -ForegroundColor Yellow }
        if ($fwdConflict -gt 0) { Write-Host "    Conflicted: $fwdConflict" -ForegroundColor Red }
    }

    # Emit structured summary for CheckMissing mode
    $buildFreshnessData = @()
    if ($checkedChannels) {
        $buildFreshnessData = @($checkedChannels.Values | ForEach-Object {
            $entry = [ordered]@{ channel = $_.Channel; version = $_.Version; ageDays = [math]::Round($_.Age.TotalDays, 1); published = $_.Published.ToString("o") }
            if ($_.Commit) { $entry.commit = $_.Commit }
            if ($_.BuildLink) { $entry.buildLink = $_.BuildLink }
            if ($_.Source) { $entry.source = $_.Source }
            $entry
        })
    }
    $flowSummary = [ordered]@{
        mode        = "flow-health"
        repository  = $Repository
        backflow    = [ordered]@{
            covered   = $coveredCount
            upToDate  = $upToDateCount
            blocked   = $blockedCount
            missing   = $missingCount
        }
        forwardFlow = [ordered]@{
            healthy    = $fwdHealthy
            stale      = $fwdStale
            conflicted = $fwdConflict
        }
        buildsAreStale   = $buildsAreStale
        buildFreshness   = $buildFreshnessData
    }
    if ($subscriptionDiagnostics.Count -gt 0) {
        $flowSummary.subscriptionHealth = $subscriptionDiagnostics
    }
    Write-Host ""
    Write-Host "[CODEFLOW_SUMMARY]"
    Write-Host ($flowSummary | ConvertTo-Json -Depth 4 -Compress)
    Write-Host "[/CODEFLOW_SUMMARY]"

    return
}

# --- Validate PRNumber for non-CheckMissing mode ---
if (-not $PRNumber) {
    Write-Error "PRNumber is required unless -CheckMissing is used."
    return
}

# --- Step 1: PR Overview ---
Write-Section "Codeflow PR #$PRNumber in $Repository"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed or not in PATH. Install from https://cli.github.com/"
    return
}

$prJson = gh pr view $PRNumber -R $Repository --json body,title,state,author,headRefName,baseRefName,createdAt,updatedAt,url,comments,commits,additions,deletions,changedFiles
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

# --- Step 2: Current State (independent assessment from primary signals) ---
Write-Section "Current State"

# Check for empty diff (0 changed files)
$isEmptyDiff = ($pr.changedFiles -eq 0 -and $pr.additions -eq 0 -and $pr.deletions -eq 0)
if ($isEmptyDiff) {
    Write-Host "  📭 Empty diff: 0 changed files, 0 additions, 0 deletions" -ForegroundColor Yellow
}

# Kick off parallel fetches for timeline + Version.Details.xml (both independent of each other)
$owner, $repo = $Repository -split '/'
$timelineJob = Start-AsyncGh @('api', "repos/$owner/$repo/issues/$PRNumber/timeline", '--paginate', '--slurp', '--jq', 'map(.[] | select(.event == "head_ref_force_pushed"))')

$vdJob = $null
if (-not $isForwardFlow) {
    $encodedRef = [System.Uri]::EscapeDataString($pr.headRefName)
    $vdJob = Start-AsyncGitHubApi -Endpoint "/repos/$Repository/contents/eng/Version.Details.xml?ref=$encodedRef" -Raw
}

# Collect timeline results
$forcePushEvents = @()
try {
    $timelineJson = Complete-AsyncJob $timelineJob {
        try {
            $json = gh api "repos/$owner/$repo/issues/$PRNumber/timeline" --paginate --slurp --jq 'map(.[] | select(.event == "head_ref_force_pushed"))' 2>$null
            if ($LASTEXITCODE -ne 0 -or -not $json) { return "[]" }
            return ($json -join "`n")
        } catch { return "[]" }
    }
    if ($timelineJson -and $timelineJson -ne "[]") {
        $forcePushEvents = @($timelineJson | ConvertFrom-Json)
    }
}
catch {
    Write-Verbose "Failed to parse timeline JSON for force push events: $($_.Exception.Message)"
    $forcePushEvents = @()
}

if ($forcePushEvents.Count -gt 0) {
    foreach ($fp in $forcePushEvents) {
        $fpActor = if ($fp.actor) { $fp.actor.login } else { "unknown" }
        $fpTime = $fp.created_at
        $fpSha = if ($fp.commit_id) { Get-ShortSha $fp.commit_id } else { "unknown" }
        Write-Host "  🔄 Force push by @$fpActor at $fpTime (→ $fpSha)" -ForegroundColor Cyan
    }
    $lastForcePush = $forcePushEvents[-1]
    $lastForcePushTime = if ($lastForcePush.created_at) {
        [DateTimeOffset]::Parse($lastForcePush.created_at).UtcDateTime
    } else { $null }
    $lastForcePushActor = if ($lastForcePush.actor) { $lastForcePush.actor.login } else { "unknown" }
}

# Synthesize current state assessment
$prUpdatedTime = if ($pr.updatedAt) { [DateTimeOffset]::Parse($pr.updatedAt).UtcDateTime } else { $null }
$prAgeDays = if ($prUpdatedTime) { ([DateTime]::UtcNow - $prUpdatedTime).TotalDays } else { 0 }
$isClosed = $pr.state -eq "CLOSED"
$isMerged = $pr.state -eq "MERGED"
$currentState = if ($isMerged) {
    "MERGED"
} elseif ($isClosed) {
    "CLOSED"
} elseif ($isEmptyDiff) {
    "NO-OP"
} elseif ($forcePushEvents.Count -gt 0 -and $lastForcePushTime -and ([DateTime]::UtcNow - $lastForcePushTime).TotalHours -lt 24) {
    "IN_PROGRESS"
} elseif ($prAgeDays -gt 3) {
    "STALE"
} else {
    "ACTIVE"
}

Write-Host ""
switch ($currentState) {
    "MERGED"      { Write-Host "  ✅ MERGED — PR has been merged" -ForegroundColor Green }
    "CLOSED"      { Write-Host "  ✖️  CLOSED — PR was closed without merging" -ForegroundColor DarkGray }
    "NO-OP"       { Write-Host "  📭 NO-OP — empty diff, likely already resolved" -ForegroundColor Yellow }
    "IN_PROGRESS" { Write-Host "  🔄 IN PROGRESS — recent force push, awaiting update" -ForegroundColor Cyan }
    "STALE"       { Write-Host "  ⏳ STALE — no recent activity" -ForegroundColor Yellow }
    "ACTIVE"      { Write-Host "  ✅ ACTIVE — PR has content" -ForegroundColor Green }
}

# --- Step 3: Codeflow Metadata ---
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

# --- Step 4: Determine actual VMR snapshot on the PR branch ---
# Priority: 1) Version.Details.xml (ground truth), 2) commit messages, 3) PR body
$branchVmrCommit = $null
$commitMsgVmrCommit = $null
$versionDetailsVmrCommit = $null

# First: check eng/Version.Details.xml on the PR branch (authoritative source)
if (-not $isForwardFlow) {
    $vdContent = Complete-AsyncJob $vdJob {
        Invoke-GitHubApi "/repos/$Repository/contents/eng/Version.Details.xml?ref=$([System.Uri]::EscapeDataString($pr.headRefName))" -Raw
    }
    if ($vdContent) {
        try {
            [xml]$vdXml = $vdContent
            $sourceNode = $vdXml.Dependencies.Source
            if ($sourceNode -and $sourceNode.Sha -and $sourceNode.Sha -match '^[a-fA-F0-9]{40}$') {
                $versionDetailsVmrCommit = $sourceNode.Sha
                $branchVmrCommit = $versionDetailsVmrCommit
            }
        }
        catch {
            # Fall back to regex if XML parsing fails
            if ($vdContent -match '<Source\s+[^>]*Sha="([a-fA-F0-9]{40})"') {
                $versionDetailsVmrCommit = $Matches[1]
                $branchVmrCommit = $versionDetailsVmrCommit
            }
        }
    }
}

# Second: scan commit messages for "Backflow from" / "Forward flow from" SHAs
if ($prCommits) {
    $reversedCommits = @($prCommits)
    [Array]::Reverse($reversedCommits)
    foreach ($c in $reversedCommits) {
        $msg = $c.messageHeadline
        if ($msg -match '(?:Backflow|Forward flow) from .+ / ([a-fA-F0-9]+)') {
            $commitMsgVmrCommit = $Matches[1]
            break
        }
    }
    # For forward flow (no Version.Details.xml source), commit messages are primary
    if (-not $branchVmrCommit -and $commitMsgVmrCommit) {
        $branchVmrCommit = $commitMsgVmrCommit
    }
}

if ($branchVmrCommit -or $vmrCommit) {
    Write-Section "Snapshot Validation"
    $usedBranchSnapshot = $false

    if ($branchVmrCommit) {
        # We have a branch-derived snapshot (from Version.Details.xml or commit message)
        $branchShort = Get-ShortSha $branchVmrCommit
        $sourceLabel = if ($versionDetailsVmrCommit -and $branchVmrCommit -eq $versionDetailsVmrCommit) { "Version.Details.xml" } else { "branch commit" }

        if ($vmrCommit) {
            $bodyShort = Get-ShortSha $vmrCommit
            if ($vmrCommit.StartsWith($branchVmrCommit, [StringComparison]::OrdinalIgnoreCase) -or $branchVmrCommit.StartsWith($vmrCommit, [StringComparison]::OrdinalIgnoreCase)) {
                Write-Host "  ✅ $sourceLabel ($branchShort) matches PR body ($bodyShort)" -ForegroundColor Green
            }
            else {
                Write-Host "  ⚠️  MISMATCH: $sourceLabel has $branchShort but PR body claims $bodyShort" -ForegroundColor Red
                Write-Host "  PR body is stale — using $sourceLabel for freshness check" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  ℹ️  PR body has no commit reference — using $sourceLabel ($branchShort)" -ForegroundColor Yellow
        }

        # Resolve to full SHA for accurate comparison (skip API call if already full-length)
        if ($branchVmrCommit.Length -ge 40) {
            $vmrCommit = $branchVmrCommit
            $usedBranchSnapshot = $true
        }
        else {
            $resolvedCommit = Invoke-GitHubApi "/repos/$freshnessRepo/commits/$branchVmrCommit"
            if ($resolvedCommit) {
                $vmrCommit = $resolvedCommit.sha
                $usedBranchSnapshot = $true
            }
            elseif ($vmrCommit) {
                Write-Host "  ⚠️  Could not resolve $sourceLabel SHA $branchShort — falling back to PR body ($(Get-ShortSha $vmrCommit))" -ForegroundColor Yellow
            }
            else {
                Write-Host "  ⚠️  Could not resolve $sourceLabel SHA $branchShort" -ForegroundColor Yellow
            }
        }
    }
    else {
        # No branch-derived snapshot — PR body only
        $commitCount = if ($prCommits) { $prCommits.Count } else { 0 }
        if ($commitCount -eq 1 -and $prCommits[0].messageHeadline -match "^Initial commit for subscription") {
            Write-Host "  ℹ️  PR has only an initial subscription commit — PR body snapshot ($(Get-ShortSha $vmrCommit)) not yet verifiable" -ForegroundColor DarkGray
        }
        else {
            Write-Host "  ⚠️  Could not verify PR body snapshot ($(Get-ShortSha $vmrCommit)) from branch" -ForegroundColor Yellow
        }
    }
}

# --- Step 5: Check source freshness ---
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
        $snapshotSource = if ($usedBranchSnapshot) {
            if ($versionDetailsVmrCommit -and $vmrCommit.StartsWith($versionDetailsVmrCommit, [StringComparison]::OrdinalIgnoreCase)) { "from Version.Details.xml" }
            elseif ($commitMsgVmrCommit) { "from branch commit" }
            else { "from branch" }
        } else { "from PR body" }
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

# Collect Maestro comment data (needed by PR Branch Analysis and Codeflow History)
$stalenessWarnings = @()
$lastStalenessComment = $null
$conflictWarnings = @()
$lastConflictComment = $null

if ($pr.comments) {
    foreach ($comment in $pr.comments) {
        $commentAuthor = $comment.author.login
        if ($commentAuthor -eq "dotnet-maestro[bot]" -or $commentAuthor -eq "dotnet-maestro") {
            if ($comment.body -match "codeflow cannot continue" -or $comment.body -match "darc trigger-subscriptions") {
                $stalenessWarnings += $comment
                $lastStalenessComment = $comment
            }
            if ($comment.body -match "Conflict detected") {
                $conflictWarnings += $comment
                $lastConflictComment = $comment
            }
        }
    }
}

# Extract conflicting files (used in History and Recommendations)
$conflictFiles = @()
if ($lastConflictComment) {
    $fileMatches = [regex]::Matches($lastConflictComment.body, '-\s+`([^`]+)`\s*\r?\n')
    foreach ($fm in $fileMatches) {
        $conflictFiles += $fm.Groups[1].Value
    }
}

# Cross-reference force push against conflict/staleness warnings (data only)
$conflictMayBeResolved = $false
$stalenessMayBeResolved = $false
if ($lastForcePushTime) {
    if ($conflictWarnings.Count -gt 0 -and $lastConflictComment) {
        $lastConflictTime = [DateTimeOffset]::Parse($lastConflictComment.createdAt).UtcDateTime
        if ($lastForcePushTime -gt $lastConflictTime) {
            $conflictMayBeResolved = $true
        }
    }
    if ($stalenessWarnings.Count -gt 0 -and $lastStalenessComment) {
        $lastStalenessTime = [DateTimeOffset]::Parse($lastStalenessComment.createdAt).UtcDateTime
        if ($lastForcePushTime -gt $lastStalenessTime) {
            $stalenessMayBeResolved = $true
        }
    }
}

# --- Step 6: PR Branch Analysis ---
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

# --- Step 7: Codeflow History (Maestro comments as historical context) ---
Write-Section "Codeflow History"
Write-Host "  Maestro warnings (historical — see Current State for present status):" -ForegroundColor DarkGray

if ($stalenessWarnings.Count -gt 0 -or $conflictWarnings.Count -gt 0) {
    if ($conflictWarnings.Count -gt 0) {
        Write-Host "  🔴 Conflict detected ($($conflictWarnings.Count) conflict warning(s))" -ForegroundColor Red
        Write-Status "Latest conflict" $lastConflictComment.createdAt

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

# Cross-reference force push against conflict/staleness warnings (historical context)
if ($lastForcePushTime) {
    if ($conflictMayBeResolved) {
        Write-Host ""
        Write-Host "  ℹ️  Force push by @$lastForcePushActor at $($lastForcePush.created_at) is AFTER the last conflict warning" -ForegroundColor Cyan
        Write-Host "     Conflict may have been resolved via darc vmr resolve-conflict" -ForegroundColor DarkGray
    }
    if ($stalenessMayBeResolved) {
        Write-Host "  ℹ️  Force push is AFTER the staleness warning — someone may have acted on it" -ForegroundColor Cyan
    }
    if ($isEmptyDiff -and ($conflictMayBeResolved -or $stalenessMayBeResolved)) {
        Write-Host ""
        Write-Host "  📭 PR has empty diff after force push — codeflow changes may already be in target branch" -ForegroundColor Yellow
        Write-Host "     This PR is likely a no-op. Consider merging to clear state or closing it." -ForegroundColor DarkGray
    }
}

# --- Step 8: Trace a specific fix (optional) ---
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

# --- Step 9: Structured Summary ---
# Emit a JSON summary for the agent to reason over when generating recommendations.
# The agent should use SKILL.md guidance to synthesize contextual recommendations.

$summary = [ordered]@{
    prNumber        = $PRNumber
    repository      = $Repository
    prState         = $pr.state
    currentState    = $currentState
    isCodeflowPR    = ($isBackflow -or $isForwardFlow)
    isMaestroAuthored = $isMaestroPR
    flowDirection   = if ($isForwardFlow) { "forward" } elseif ($isBackflow) { "backflow" } else { "unknown" }
    isEmptyDiff     = $isEmptyDiff
    changedFiles    = [int]$pr.changedFiles
    additions       = [int]$pr.additions
    deletions       = [int]$pr.deletions
    subscriptionId  = $subscriptionId
    vmrCommit       = if ($vmrCommit) { Get-ShortSha $vmrCommit } else { $null }
    vmrBranch       = $vmrBranch
}

# Freshness
$hasFreshnessData = ($null -ne $vmrCommit -and $null -ne $sourceHeadSha)
$summary.freshness = [ordered]@{
    sourceHeadSha   = if ($sourceHeadSha) { Get-ShortSha $sourceHeadSha } else { $null }
    compareStatus   = $compareStatus
    aheadBy         = $aheadBy
    behindBy        = $behindBy
    isUpToDate      = if ($hasFreshnessData) { ($vmrCommit -eq $sourceHeadSha -or $compareStatus -eq 'identical') } else { $null }
}

# Force pushes
$summary.forcePushes = [ordered]@{
    count           = $forcePushEvents.Count
    lastActor       = if ($lastForcePushActor) { $lastForcePushActor } else { $null }
    lastTime        = if ($lastForcePushTime) { $lastForcePushTime.ToString("o") } else { $null }
}

# Warnings
$summary.warnings = [ordered]@{
    conflictCount           = $conflictWarnings.Count
    conflictFiles           = $conflictFiles
    conflictMayBeResolved   = $conflictMayBeResolved
    stalenessCount          = $stalenessWarnings.Count
    stalenessMayBeResolved  = $stalenessMayBeResolved
}

# Commits
$manualCommitCount = if ($manualCommits) { $manualCommits.Count } else { 0 }
$codeflowLikeCount = if ($codeflowLikeManualCommits) { $codeflowLikeManualCommits.Count } else { 0 }
$summary.commits = [ordered]@{
    total                   = if ($prCommits) { $prCommits.Count } else { 0 }
    manual                  = $manualCommitCount
    codeflowLikeManual      = $codeflowLikeCount
}

# PR age
$summary.age = [ordered]@{
    daysSinceUpdate = [math]::Max(0, [math]::Round($prAgeDays, 1))
    createdAt       = $pr.createdAt
    updatedAt       = $pr.updatedAt
}

Write-Host ""
Write-Host "[CODEFLOW_SUMMARY]"
Write-Host ($summary | ConvertTo-Json -Depth 4 -Compress)
Write-Host "[/CODEFLOW_SUMMARY]"

# Ensure clean exit code (gh api failures may leave $LASTEXITCODE = 1)
exit 0
