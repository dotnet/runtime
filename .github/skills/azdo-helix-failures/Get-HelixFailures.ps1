<#
.SYNOPSIS
    Retrieves test failures from Azure DevOps builds and Helix test runs.

.DESCRIPTION
    This script queries Azure DevOps for failed jobs in a build and retrieves
    the corresponding Helix console logs to show detailed test failure information.
    It can also directly query a specific Helix job and work item.

.PARAMETER BuildId
    The Azure DevOps build ID to query.

.PARAMETER PRNumber
    The GitHub PR number to find the associated build.

.PARAMETER HelixJob
    The Helix job ID (GUID) to query directly.

.PARAMETER WorkItem
    The Helix work item name to query (requires -HelixJob).

.PARAMETER Repository
    The GitHub repository (owner/repo format). Default: dotnet/runtime

.PARAMETER Organization
    The Azure DevOps organization. Default: dnceng-public

.PARAMETER Project
    The Azure DevOps project GUID. Default: cbb18261-c48f-4abb-8651-8cdcb5474649

.PARAMETER ShowLogs
    If specified, fetches and displays the Helix console logs for failed tests.

.PARAMETER MaxJobs
    Maximum number of failed jobs to process. Default: 5

.PARAMETER MaxFailureLines
    Maximum number of lines to capture per test failure. Default: 50

.PARAMETER TimeoutSec
    Timeout in seconds for API calls. Default: 30

.PARAMETER ContextLines
    Number of context lines to show before errors. Default: 0

.EXAMPLE
    .\Get-HelixFailures.ps1 -BuildId 1276327
    
.EXAMPLE
    .\Get-HelixFailures.ps1 -PRNumber 123445 -ShowLogs

.EXAMPLE
    .\Get-HelixFailures.ps1 -PRNumber 123445 -Repository dotnet/aspnetcore

.EXAMPLE
    .\Get-HelixFailures.ps1 -HelixJob "4b24b2c2-ad5a-4c46-8a84-844be03b1d51" -WorkItem "iOS.Device.Aot.Test"
#>

[CmdletBinding(DefaultParameterSetName = 'BuildId')]
param(
    [Parameter(ParameterSetName = 'BuildId', Mandatory = $true)]
    [int]$BuildId,

    [Parameter(ParameterSetName = 'PRNumber', Mandatory = $true)]
    [int]$PRNumber,

    [Parameter(ParameterSetName = 'HelixJob', Mandatory = $true)]
    [string]$HelixJob,

    [Parameter(ParameterSetName = 'HelixJob')]
    [string]$WorkItem,

    [Parameter(ParameterSetName = 'ClearCache', Mandatory = $true)]
    [switch]$ClearCache,

    [string]$Repository = "dotnet/runtime",
    [string]$Organization = "dnceng-public",
    [string]$Project = "cbb18261-c48f-4abb-8651-8cdcb5474649",
    [switch]$ShowLogs,
    [int]$MaxJobs = 5,
    [int]$MaxFailureLines = 50,
    [int]$TimeoutSec = 30,
    [int]$ContextLines = 0,
    [switch]$NoCache,
    [int]$CacheTTLMinutes = 60
)

$ErrorActionPreference = "Stop"

# Handle -ClearCache parameter
if ($ClearCache) {
    $cacheDir = Join-Path $env:TEMP "helix-failures-cache"
    if (Test-Path $cacheDir) {
        $files = Get-ChildItem -Path $cacheDir -File
        $count = $files.Count
        Remove-Item -Path $cacheDir -Recurse -Force
        Write-Host "Cleared $count cached files from $cacheDir" -ForegroundColor Green
    }
    else {
        Write-Host "Cache directory does not exist: $cacheDir" -ForegroundColor Yellow
    }
    exit 0
}

# Setup caching
$script:CacheDir = Join-Path $env:TEMP "helix-failures-cache"
if (-not (Test-Path $script:CacheDir)) {
    New-Item -ItemType Directory -Path $script:CacheDir -Force | Out-Null
}

# Clean up expired cache files on startup (files older than 2x TTL)
function Clear-ExpiredCache {
    param([int]$TTLMinutes = $CacheTTLMinutes)
    
    $maxAge = $TTLMinutes * 2
    $cutoff = (Get-Date).AddMinutes(-$maxAge)
    
    Get-ChildItem -Path $script:CacheDir -File -ErrorAction SilentlyContinue | Where-Object {
        $_.LastWriteTime -lt $cutoff
    } | ForEach-Object {
        Write-Verbose "Removing expired cache file: $($_.Name)"
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
    }
}

# Run cache cleanup at startup (non-blocking)
if (-not $NoCache) {
    Clear-ExpiredCache -TTLMinutes $CacheTTLMinutes
}

function Get-CachedResponse {
    param(
        [string]$Url,
        [int]$TTLMinutes = $CacheTTLMinutes
    )
    
    if ($NoCache) { return $null }
    
    # Create a hash of the URL for the cache filename
    $hash = [System.BitConverter]::ToString(
        [System.Security.Cryptography.SHA256]::Create().ComputeHash(
            [System.Text.Encoding]::UTF8.GetBytes($Url)
        )
    ).Replace("-", "").Substring(0, 16)
    
    $cacheFile = Join-Path $script:CacheDir "$hash.json"
    
    if (Test-Path $cacheFile) {
        $cacheInfo = Get-Item $cacheFile
        $age = (Get-Date) - $cacheInfo.LastWriteTime
        
        if ($age.TotalMinutes -lt $TTLMinutes) {
            Write-Verbose "Cache hit for $Url (age: $([int]$age.TotalMinutes) min)"
            return Get-Content $cacheFile -Raw
        }
        else {
            Write-Verbose "Cache expired for $Url"
        }
    }
    
    return $null
}

function Set-CachedResponse {
    param(
        [string]$Url,
        [string]$Content
    )
    
    if ($NoCache) { return }
    
    $hash = [System.BitConverter]::ToString(
        [System.Security.Cryptography.SHA256]::Create().ComputeHash(
            [System.Text.Encoding]::UTF8.GetBytes($Url)
        )
    ).Replace("-", "").Substring(0, 16)
    
    $cacheFile = Join-Path $script:CacheDir "$hash.json"
    $Content | Set-Content $cacheFile -Force
    Write-Verbose "Cached response for $Url"
}

function Invoke-CachedRestMethod {
    param(
        [string]$Uri,
        [int]$TimeoutSec = 30,
        [switch]$AsJson
    )
    
    # Check cache first
    $cached = Get-CachedResponse -Url $Uri
    if ($cached) {
        if ($AsJson) {
            return $cached | ConvertFrom-Json
        }
        return $cached
    }
    
    # Make the actual request
    Write-Verbose "GET $Uri"
    $response = Invoke-RestMethod -Uri $Uri -Method Get -TimeoutSec $TimeoutSec
    
    # Cache the response
    if ($AsJson -or $response -is [PSCustomObject]) {
        $content = $response | ConvertTo-Json -Depth 10 -Compress
        Set-CachedResponse -Url $Uri -Content $content
    }
    else {
        Set-CachedResponse -Url $Uri -Content $response
    }
    
    return $response
}

function Get-AzDOBuildIdFromPR {
    param([int]$PR)
    
    # Check for gh CLI dependency
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI (gh) is required for PR lookup. Install from https://cli.github.com/ or use -BuildId instead."
    }
    
    Write-Host "Finding build for PR #$PR in $Repository..." -ForegroundColor Cyan
    Write-Verbose "Running: gh pr checks $PR --repo $Repository"
    
    # Use gh cli to get the checks
    $checksOutput = gh pr checks $PR --repo $Repository 2>&1
    
    # Find the runtime build URL
    $runtimeCheck = $checksOutput | Select-String -Pattern "runtime\s+fail.*buildId=(\d+)" | Select-Object -First 1
    if ($runtimeCheck) {
        if ($runtimeCheck -match "buildId=(\d+)") {
            return [int]$Matches[1]
        }
    }
    
    # Try to find any failing build
    $anyBuild = $checksOutput | Select-String -Pattern "buildId=(\d+)" | Select-Object -First 1
    if ($anyBuild -and $anyBuild -match "buildId=(\d+)") {
        return [int]$Matches[1]
    }
    
    throw "Could not find Azure DevOps build for PR #$PR in $Repository"
}

function Get-AzDOBuildStatus {
    param([int]$Build)
    
    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/${Build}?api-version=7.0"
    
    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return @{
            Status = $response.status        # notStarted, inProgress, completed
            Result = $response.result        # succeeded, failed, canceled (only set when completed)
            StartTime = $response.startTime
            FinishTime = $response.finishTime
        }
    }
    catch {
        Write-Verbose "Failed to fetch build status: $_"
        return $null
    }
}

function Get-AzDOTimeline {
    param([int]$Build)
    
    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/$Build/timeline?api-version=7.0"
    Write-Host "Fetching build timeline..." -ForegroundColor Cyan
    
    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return $response
    }
    catch {
        throw "Failed to fetch build timeline: $_"
    }
}

function Get-FailedJobs {
    param($Timeline)
    
    $failedJobs = $Timeline.records | Where-Object { 
        $_.type -eq "Job" -and $_.result -eq "failed" 
    }
    
    return $failedJobs
}

function Get-HelixJobInfo {
    param($Timeline, $JobId)
    
    # Find tasks in this job that mention Helix
    $helixTasks = $Timeline.records | Where-Object { 
        $_.parentId -eq $JobId -and 
        $_.name -like "*Helix*" -and 
        $_.result -eq "failed"
    }
    
    return $helixTasks
}

function Get-BuildLog {
    param([int]$Build, [int]$LogId)
    
    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/$Build/logs/${LogId}?api-version=7.0"
    
    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec
        return $response
    }
    catch {
        Write-Warning "Failed to fetch log ${LogId}: $_"
        return $null
    }
}

function Extract-HelixUrls {
    param([string]$LogContent)
    
    $urls = @()
    
    # First, normalize the content by removing line breaks that might split URLs
    $normalizedContent = $LogContent -replace "`r`n", "" -replace "`n", ""
    
    # Match Helix console log URLs - workitem names can contain dots, dashes, and other chars
    $urlMatches = [regex]::Matches($normalizedContent, 'https://helix\.dot\.net/api/[^/]+/jobs/[a-f0-9-]+/workitems/[^/\s\]]+/console')
    foreach ($match in $urlMatches) {
        $urls += $match.Value
    }
    
    Write-Verbose "Found $($urls.Count) Helix URLs"
    return $urls | Select-Object -Unique
}

function Extract-TestFailures {
    param([string]$LogContent)
    
    $failures = @()
    
    # Match test failure patterns from MSBuild output (use $failureMatches to avoid shadowing automatic $Matches variable)
    $pattern = 'error\s*:\s*.*Test\s+(\S+)\s+has failed'
    $failureMatches = [regex]::Matches($LogContent, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    
    foreach ($match in $failureMatches) {
        $failures += @{
            TestName = $match.Groups[1].Value
            FullMatch = $match.Value
        }
    }
    
    Write-Verbose "Found $($failures.Count) test failures"
    return $failures
}

function Extract-BuildErrors {
    param(
        [string]$LogContent,
        [int]$Context = $ContextLines
    )
    
    $errors = @()
    $lines = $LogContent -split "`n"
    
    # Patterns for common build errors - ordered from most specific to least specific
    $errorPatterns = @(
        'error\s+CS\d+:.*',                        # C# compiler errors
        'error\s+MSB\d+:.*',                       # MSBuild errors
        'error\s+NU\d+:.*',                        # NuGet errors
        '\.pcm: No such file or directory',        # Clang module cache
        'EXEC\s*:\s*error\s*:.*',                  # Exec task errors
        'fatal error:.*',                          # Fatal errors (clang, etc)
        '##\[error\].*'                            # AzDO error annotations (last - catch-all)
    )
    
    $combinedPattern = ($errorPatterns -join '|')
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $combinedPattern) {
            # Clean up the line (remove timestamps, etc)
            $cleanLine = $lines[$i] -replace '^\d{4}-\d{2}-\d{2}T[\d:\.]+Z\s*', ''
            $cleanLine = $cleanLine -replace '##\[error\]', 'ERROR: '
            
            # Add context lines if requested
            if ($Context -gt 0) {
                $contextStart = [Math]::Max(0, $i - $Context)
                $contextLines = @()
                for ($j = $contextStart; $j -lt $i; $j++) {
                    $contextLines += "  " + $lines[$j].Trim()
                }
                if ($contextLines.Count -gt 0) {
                    $errors += ($contextLines -join "`n")
                }
            }
            
            $errors += $cleanLine.Trim()
        }
    }
    
    return $errors | Select-Object -First 20 | Select-Object -Unique
}

function Extract-HelixLogUrls {
    param([string]$LogContent)
    
    $urls = @()
    
    # Match Helix console log URLs from log content
    # Pattern: https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{workItemName}/console
    $pattern = 'https://helix\.dot\.net/api/[^/]+/jobs/([a-f0-9-]+)/workitems/([^/\s\]]+)/console'
    $urlMatches = [regex]::Matches($LogContent, $pattern)
    
    foreach ($match in $urlMatches) {
        $urls += @{
            Url = $match.Value
            JobId = $match.Groups[1].Value
            WorkItem = $match.Groups[2].Value
        }
    }
    
    # Deduplicate by URL
    $uniqueUrls = @{}
    foreach ($url in $urls) {
        if (-not $uniqueUrls.ContainsKey($url.Url)) {
            $uniqueUrls[$url.Url] = $url
        }
    }
    
    return $uniqueUrls.Values
}

function Get-FailureClassification {
    param([string[]]$Errors)
    
    $errorText = $Errors -join "`n"
    
    # Known failure patterns with classifications - ordered from most specific to general
    $knownPatterns = @(
        @{
            Pattern = '\.pcm: No such file or directory|clang/ModuleCache'
            Type = 'Infrastructure'
            Summary = '[!] macOS clang module cache issue (dsymutil failure)'
            Action = 'Apply StripSymbols=false workaround or wait for SDK fix'
            Transient = $false
        },
        @{
            Pattern = 'File size is not in the expected range|Size of the executable'
            Type = 'SizeRegression'
            Summary = '[Size] NativeAOT binary size regression'
            Action = 'Investigate size increase or update thresholds'
            Transient = $false
        },
        @{
            Pattern = 'error NU1102: Unable to find package'
            Type = 'Infrastructure'
            Summary = '[Pkg] Missing NuGet package'
            Action = 'Check if package is published to feeds; may need to wait for upstream build'
            Transient = $true
        },
        @{
            Pattern = 'DEVICE_NOT_FOUND|exit code 81|device unauthorized'
            Type = 'Infrastructure'
            Summary = '[Device] Android/iOS device infrastructure issue'
            Action = 'Retry the build - transient device connection issue'
            Transient = $true
        },
        @{
            Pattern = 'Helix work item timed out|timed out after'
            Type = 'Infrastructure'
            Summary = '[Timeout] Helix timeout'
            Action = 'Retry or investigate slow test; may need timeout increase'
            Transient = $true
        },
        @{
            Pattern = 'error CS\d+:'
            Type = 'Build'
            Summary = '[Build] C# compilation error'
            Action = 'Fix the code - this is a real build failure'
            Transient = $false
        },
        @{
            Pattern = 'error MSB\d+:'
            Type = 'Build'
            Summary = '[Build] MSBuild error'
            Action = 'Check build configuration and dependencies'
            Transient = $false
        },
        @{
            Pattern = 'OutOfMemoryException|out of memory'
            Type = 'Infrastructure'
            Summary = '[OOM] Out of memory failure'
            Action = 'Retry - may be transient memory pressure on Helix machine'
            Transient = $true
        },
        @{
            Pattern = 'StackOverflowException'
            Type = 'Test'
            Summary = '[Test] Stack overflow in test'
            Action = 'Investigate infinite recursion or deep call stack'
            Transient = $false
        },
        @{
            Pattern = 'Assert\.\w+\(\)\s+Failure|Expected:.*but was:'
            Type = 'Test'
            Summary = '[Test] Assertion failure'
            Action = 'Fix the test or the code under test'
            Transient = $false
        },
        @{
            Pattern = 'System\.TimeoutException|did not complete within'
            Type = 'Test'
            Summary = '[Test] Test timeout'
            Action = 'Retry or increase test timeout; may indicate perf regression'
            Transient = $true
        },
        @{
            Pattern = 'Connection refused|ECONNREFUSED|network.+unreachable'
            Type = 'Infrastructure'
            Summary = '[Network] Network connectivity issue'
            Action = 'Retry - transient network issue on Helix machine'
            Transient = $true
        },
        @{
            Pattern = 'Unable to pull image|docker.+pull.+failed|Exit Code:-4'
            Type = 'Infrastructure'
            Summary = '[Docker] Container image pull failure'
            Action = 'Retry - transient container registry connectivity issue'
            Transient = $true
        },
        @{
            Pattern = 'XUnit.*error.*Tests failed:'
            Type = 'Test'
            Summary = '[Test] Local xUnit test failure'
            Action = 'Check test run URL for specific failed test details'
            Transient = $false
        },
        @{
            Pattern = '\[FAIL\]'
            Type = 'Test'
            Summary = '[Test] Helix test failure'
            Action = 'Check console log for failure details'
            Transient = $false
        }
    )
    
    foreach ($known in $knownPatterns) {
        if ($errorText -match $known.Pattern) {
            return @{
                Type = $known.Type
                Summary = $known.Summary
                Action = $known.Action
                Transient = $known.Transient
            }
        }
    }
    
    # Unknown failure
    return @{
        Type = 'Unknown'
        Summary = '[?] Unknown failure type'
        Action = 'Manual investigation required'
        Transient = $false
    }
}

function Search-KnownIssues {
    param(
        [string]$TestName,
        [string]$ErrorMessage,
        [string]$Repository = "dotnet/runtime"
    )
    
    # Search for known issues using the "Known Build Error" label
    # This label is used by Build Analysis across dotnet repositories
    
    $knownIssues = @()
    
    # Check if gh CLI is available
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Verbose "GitHub CLI not available for searching known issues"
        return $knownIssues
    }
    
    try {
        # Extract search terms from test name and error message
        $searchTerms = @()
        
        # First priority: Look for [FAIL] test names in the error message
        # Pattern: "TestName [FAIL]" - the test name comes BEFORE [FAIL]
        if ($ErrorMessage -match '(\S+)\s+\[FAIL\]') {
            $failedTest = $Matches[1]
            # Extract just the method name (after last .)
            if ($failedTest -match '\.([^.]+)$') {
                $searchTerms += $Matches[1]
            }
            # Also add the full test name
            $searchTerms += $failedTest
        }
        
        # Second priority: Extract test class/method from stack traces
        if ($ErrorMessage -match 'at\s+(\w+\.\w+)\(' -and $searchTerms.Count -eq 0) {
            $searchTerms += $Matches[1]
        }
        
        if ($TestName) {
            # Try to get the test method name from the work item
            if ($TestName -match '\.([^.]+)$') {
                $methodName = $Matches[1]
                # Only add if it looks like a test name (not just "Tests")
                if ($methodName -ne "Tests" -and $methodName.Length -gt 5) {
                    $searchTerms += $methodName
                }
            }
            # Also try the full test name if it's not too long and looks specific
            if ($TestName.Length -lt 100 -and $TestName -notmatch '^System\.\w+\.Tests$') {
                $searchTerms += $TestName
            }
        }
        
        # Third priority: Extract specific exception patterns (but not generic TimeoutException)
        if ($ErrorMessage -and $searchTerms.Count -eq 0) {
            # Look for specific exception types
            if ($ErrorMessage -match '(System\.(?:InvalidOperation|ArgumentNull|FormatProvider)\w*Exception)') {
                $searchTerms += $Matches[1]
            }
        }
        
        # Deduplicate and limit search terms
        $searchTerms = $searchTerms | Select-Object -Unique | Select-Object -First 3
        
        foreach ($term in $searchTerms) {
            if (-not $term) { continue }
            
            Write-Verbose "Searching for known issues with term: $term"
            
            # Search for open issues with the "Known Build Error" label
            $results = gh issue list `
                --repo $Repository `
                --label "Known Build Error" `
                --state open `
                --search "$term" `
                --limit 3 `
                --json number,title,url 2>$null | ConvertFrom-Json
            
            if ($results) {
                foreach ($issue in $results) {
                    # Check if the title actually contains our search term (avoid false positives)
                    if ($issue.title -match [regex]::Escape($term) -or $term.Length -gt 20) {
                        $knownIssues += @{
                            Number = $issue.number
                            Title = $issue.title
                            Url = $issue.url
                            SearchTerm = $term
                        }
                    }
                }
            }
            
            # If we found issues, stop searching
            if ($knownIssues.Count -gt 0) {
                break
            }
        }
        
        # Deduplicate by issue number
        $unique = @{}
        foreach ($issue in $knownIssues) {
            if (-not $unique.ContainsKey($issue.Number)) {
                $unique[$issue.Number] = $issue
            }
        }
        
        return $unique.Values
    }
    catch {
        Write-Verbose "Failed to search for known issues: $_"
        return @()
    }
}

function Show-ClassificationWithKnownIssues {
    param(
        [hashtable]$Classification,
        [string]$TestName = "",
        [string]$ErrorMessage = "",
        [string]$Repository = $script:Repository
    )
    
    if (-not $Classification) { return }
    
    # Show classification
    Write-Host "`n  Classification: $($Classification.Summary)" -ForegroundColor Yellow
    if ($Classification.Action) {
        Write-Host "  Suggested action: $($Classification.Action)" -ForegroundColor Green
    }
    if ($Classification.Transient) {
        Write-Host "  (This failure appears to be transient - retry may help)" -ForegroundColor Cyan
    }
    
    # Search for known issues if we have a test name or error
    if ($TestName -or $ErrorMessage) {
        $knownIssues = Search-KnownIssues -TestName $TestName -ErrorMessage $ErrorMessage -Repository $Repository
        if ($knownIssues -and $knownIssues.Count -gt 0) {
            Write-Host "  Known Issues:" -ForegroundColor Magenta
            foreach ($issue in $knownIssues) {
                Write-Host "    #$($issue.Number): $($issue.Title)" -ForegroundColor Magenta
                Write-Host "    $($issue.Url)" -ForegroundColor Gray
            }
        }
    }
}

function Get-AzDOTestResults {
    param(
        [string]$RunId,
        [string]$Org = "https://dev.azure.com/$Organization"
    )
    
    # Check if az devops CLI is available
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
        Write-Verbose "Azure CLI not available for fetching test results"
        return $null
    }
    
    try {
        Write-Verbose "Fetching test results for run $RunId via az devops CLI..."
        $results = az devops invoke `
            --org $Org `
            --area test `
            --resource Results `
            --route-parameters project=$Project runId=$RunId `
            --api-version 7.0 `
            --query "value[?outcome=='Failed'].{name:testCaseTitle, outcome:outcome, error:errorMessage}" `
            -o json 2>$null | ConvertFrom-Json
        
        return $results
    }
    catch {
        Write-Verbose "Failed to fetch test results via az devops: $_"
        return $null
    }
}

function Extract-TestRunUrls {
    param([string]$LogContent)
    
    $testRuns = @()
    
    # Match Azure DevOps Test Run URLs
    # Pattern: Published Test Run : https://dev.azure.com/dnceng-public/public/_TestManagement/Runs?runId=35626550&_a=runCharts
    $pattern = 'Published Test Run\s*:\s*(https://dev\.azure\.com/[^/]+/[^/]+/_TestManagement/Runs\?runId=(\d+)[^\s]*)'
    $matches = [regex]::Matches($LogContent, $pattern)
    
    foreach ($match in $matches) {
        $testRuns += @{
            Url = $match.Groups[1].Value
            RunId = $match.Groups[2].Value
        }
    }
    
    Write-Verbose "Found $($testRuns.Count) test run URLs"
    return $testRuns
}

function Get-LocalTestFailures {
    param(
        [object]$Timeline,
        [int]$BuildId
    )
    
    $localFailures = @()
    
    # Find failed test tasks (non-Helix)
    # Look for tasks with "Test" in name that have issues but no Helix URLs
    $testTasks = $Timeline.records | Where-Object { 
        ($_.name -match 'Test|xUnit' -or $_.type -eq 'Task') -and 
        $_.issues -and 
        $_.issues.Count -gt 0 
    }
    
    foreach ($task in $testTasks) {
        # Check if this task has test failures (XUnit errors)
        $testErrors = $task.issues | Where-Object { 
            $_.message -match 'Tests failed:' -or 
            $_.message -match 'error\s*:.*Test.*failed' 
        }
        
        if ($testErrors.Count -gt 0) {
            # This is a local test failure
            $failure = @{
                TaskName = $task.name
                TaskId = $task.id
                LogId = $task.log.id
                Issues = $testErrors
                TestRunUrls = @()
            }
            
            # Try to get test run URLs from the publish task
            $publishTask = $Timeline.records | Where-Object { 
                $_.parentId -eq $task.parentId -and 
                $_.name -match 'Publish.*Test.*Results' -and
                $_.log
            } | Select-Object -First 1
            
            if ($publishTask -and $publishTask.log) {
                $logContent = Get-BuildLog -Build $BuildId -LogId $publishTask.log.id
                if ($logContent) {
                    $testRunUrls = Extract-TestRunUrls -LogContent $logContent
                    $failure.TestRunUrls = $testRunUrls
                }
            }
            
            $localFailures += $failure
        }
    }
    
    return $localFailures
}

function Get-HelixJobDetails {
    param([string]$JobId)
    
    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId"
    
    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return $response
    }
    catch {
        Write-Warning "Failed to fetch Helix job ${JobId}: $_"
        return $null
    }
}

function Get-HelixWorkItems {
    param([string]$JobId)
    
    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId/workitems"
    
    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return $response
    }
    catch {
        Write-Warning "Failed to fetch work items for job ${JobId}: $_"
        return $null
    }
}

function Get-HelixWorkItemDetails {
    param([string]$JobId, [string]$WorkItemName)
    
    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId/workitems/$WorkItemName"
    
    try {
        $response = Invoke-CachedRestMethod -Uri $url -TimeoutSec $TimeoutSec -AsJson
        return $response
    }
    catch {
        Write-Warning "Failed to fetch work item ${WorkItemName}: $_"
        return $null
    }
}

function Get-HelixConsoleLog {
    param([string]$Url)
    
    try {
        $response = Invoke-CachedRestMethod -Uri $Url -TimeoutSec $TimeoutSec
        return $response
    }
    catch {
        Write-Warning "Failed to fetch Helix log from ${Url}: $_"
        return $null
    }
}

function Format-TestFailure {
    param(
        [string]$LogContent,
        [int]$MaxLines = $MaxFailureLines,
        [int]$MaxFailures = 3
    )
    
    $lines = $LogContent -split "`n"
    $allFailures = @()
    $currentFailure = @()
    $inFailure = $false
    $emptyLineCount = 0
    $failureCount = 0
    
    # Expanded failure detection patterns
    $failureStartPatterns = @(
        '\[FAIL\]',
        'Assert\.\w+\(\)\s+Failure',
        'Expected:.*but was:',
        'BUG:',
        'FAILED\s*$',
        'END EXECUTION - FAILED',
        'System\.\w+Exception:'
    )
    $combinedPattern = ($failureStartPatterns -join '|')
    
    foreach ($line in $lines) {
        # Check for new failure start
        if ($line -match $combinedPattern) {
            # Save previous failure if exists
            if ($currentFailure.Count -gt 0) {
                $allFailures += ($currentFailure -join "`n")
                $failureCount++
                if ($failureCount -ge $MaxFailures) {
                    break
                }
            }
            # Start new failure
            $currentFailure = @($line)
            $inFailure = $true
            $emptyLineCount = 0
            continue
        }
        
        if ($inFailure) {
            $currentFailure += $line
            
            # Track consecutive empty lines to detect end of stack trace
            if ($line -match '^\s*$') {
                $emptyLineCount++
            }
            else {
                $emptyLineCount = 0
            }
            
            # Stop this failure after stack trace ends (2+ consecutive empty lines) or max lines reached
            if ($emptyLineCount -ge 2 -or $currentFailure.Count -ge $MaxLines) {
                $allFailures += ($currentFailure -join "`n")
                $currentFailure = @()
                $inFailure = $false
                $failureCount++
                if ($failureCount -ge $MaxFailures) {
                    break
                }
            }
        }
    }
    
    # Don't forget last failure
    if ($currentFailure.Count -gt 0 -and $failureCount -lt $MaxFailures) {
        $allFailures += ($currentFailure -join "`n")
    }
    
    if ($allFailures.Count -eq 0) {
        return $null
    }
    
    $result = $allFailures -join "`n`n--- Next Failure ---`n`n"
    
    if ($failureCount -ge $MaxFailures) {
        $result += "`n`n... (more failures exist, showing first $MaxFailures)"
    }
    
    return $result
}

# Main execution
try {
    # Handle direct Helix job query
    if ($PSCmdlet.ParameterSetName -eq 'HelixJob') {
        Write-Host "`n=== Helix Job $HelixJob ===" -ForegroundColor Yellow
        Write-Host "URL: https://helix.dot.net/api/jobs/$HelixJob" -ForegroundColor Gray
        
        # Get job details
        $jobDetails = Get-HelixJobDetails -JobId $HelixJob
        if ($jobDetails) {
            Write-Host "`nQueue: $($jobDetails.QueueId)" -ForegroundColor Cyan
            Write-Host "Source: $($jobDetails.Source)" -ForegroundColor Cyan
        }
        
        if ($WorkItem) {
            # Query specific work item
            Write-Host "`n--- Work Item: $WorkItem ---" -ForegroundColor Cyan
            
            $workItemDetails = Get-HelixWorkItemDetails -JobId $HelixJob -WorkItemName $WorkItem
            if ($workItemDetails) {
                Write-Host "  State: $($workItemDetails.State)" -ForegroundColor $(if ($workItemDetails.State -eq 'Passed') { 'Green' } else { 'Red' })
                Write-Host "  Exit Code: $($workItemDetails.ExitCode)" -ForegroundColor White
                Write-Host "  Machine: $($workItemDetails.MachineName)" -ForegroundColor Gray
                Write-Host "  Duration: $($workItemDetails.Duration)" -ForegroundColor Gray
                
                # Show artifacts
                if ($workItemDetails.Files -and $workItemDetails.Files.Count -gt 0) {
                    Write-Host "`n  Artifacts:" -ForegroundColor Yellow
                    foreach ($file in $workItemDetails.Files | Select-Object -First 10) {
                        Write-Host "    $($file.Name): $($file.Uri)" -ForegroundColor Gray
                    }
                }
                
                # Fetch console log
                $consoleUrl = "https://helix.dot.net/api/2019-06-17/jobs/$HelixJob/workitems/$WorkItem/console"
                Write-Host "`n  Console Log: $consoleUrl" -ForegroundColor Yellow
                
                $consoleLog = Get-HelixConsoleLog -Url $consoleUrl
                if ($consoleLog) {
                    $failureInfo = Format-TestFailure -LogContent $consoleLog
                    if ($failureInfo) {
                        Write-Host $failureInfo -ForegroundColor White
                        
                        # Classify the failure and search for known issues
                        $classification = Get-FailureClassification -Errors @($failureInfo)
                        Show-ClassificationWithKnownIssues -Classification $classification -TestName $WorkItem -ErrorMessage $failureInfo
                    }
                    else {
                        # Show last 50 lines if no failure pattern detected
                        $lines = $consoleLog -split "`n"
                        $lastLines = $lines | Select-Object -Last 50
                        Write-Host ($lastLines -join "`n") -ForegroundColor White
                    }
                }
            }
        }
        else {
            # List all work items in the job
            Write-Host "`nWork Items:" -ForegroundColor Yellow
            $workItems = Get-HelixWorkItems -JobId $HelixJob
            if ($workItems) {
                Write-Host "  Total: $($workItems.Count)" -ForegroundColor Cyan
                Write-Host "  Checking for failures..." -ForegroundColor Gray
                
                # Need to fetch details for each to find failures (list API only shows 'Finished')
                $failedItems = @()
                foreach ($wi in $workItems | Select-Object -First 20) {
                    $details = Get-HelixWorkItemDetails -JobId $HelixJob -WorkItemName $wi.Name
                    if ($details -and $details.ExitCode -ne 0) {
                        $failedItems += @{
                            Name = $wi.Name
                            ExitCode = $details.ExitCode
                            State = $details.State
                        }
                    }
                }
                
                if ($failedItems.Count -gt 0) {
                    Write-Host "`n  Failed Work Items:" -ForegroundColor Red
                    foreach ($wi in $failedItems | Select-Object -First $MaxJobs) {
                        Write-Host "    - $($wi.Name) (Exit: $($wi.ExitCode))" -ForegroundColor White
                    }
                    Write-Host "`n  Use -WorkItem '<name>' to see details" -ForegroundColor Gray
                }
                else {
                    Write-Host "  No failures found in first 20 work items" -ForegroundColor Green
                }
                
                Write-Host "`n  All work items:" -ForegroundColor Yellow
                foreach ($wi in $workItems | Select-Object -First 10) {
                    Write-Host "    - $($wi.Name)" -ForegroundColor White
                }
                if ($workItems.Count -gt 10) {
                    Write-Host "    ... and $($workItems.Count - 10) more" -ForegroundColor Gray
                }
            }
        }
        
        exit 0
    }
    
    # Get build ID if using PR number
    if ($PSCmdlet.ParameterSetName -eq 'PRNumber') {
        $BuildId = Get-AzDOBuildIdFromPR -PR $PRNumber
        Write-Host "Found build ID: $BuildId" -ForegroundColor Green
    }
    
    Write-Host "`n=== Azure DevOps Build $BuildId ===" -ForegroundColor Yellow
    Write-Host "URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$BuildId" -ForegroundColor Gray
    
    # Get and display build status
    $buildStatus = Get-AzDOBuildStatus -Build $BuildId
    if ($buildStatus) {
        $statusColor = switch ($buildStatus.Status) {
            "inProgress" { "Cyan" }
            "completed" { if ($buildStatus.Result -eq "succeeded") { "Green" } else { "Red" } }
            default { "Gray" }
        }
        $statusText = $buildStatus.Status
        if ($buildStatus.Status -eq "completed" -and $buildStatus.Result) {
            $statusText = "$($buildStatus.Status) ($($buildStatus.Result))"
        }
        elseif ($buildStatus.Status -eq "inProgress") {
            $statusText = "IN PROGRESS - showing failures so far"
        }
        Write-Host "Status: $statusText" -ForegroundColor $statusColor
    }
    
    # Get timeline
    $timeline = Get-AzDOTimeline -Build $BuildId
    
    # Get failed jobs
    $failedJobs = Get-FailedJobs -Timeline $timeline
    
    # Also check for local test failures (non-Helix)
    $localTestFailures = Get-LocalTestFailures -Timeline $timeline -BuildId $BuildId
    
    if ((-not $failedJobs -or $failedJobs.Count -eq 0) -and $localTestFailures.Count -eq 0) {
        if ($buildStatus -and $buildStatus.Status -eq "inProgress") {
            Write-Host "`nNo failures yet - build still in progress" -ForegroundColor Cyan
            Write-Host "Run again later to check for failures, or use -NoCache to get fresh data" -ForegroundColor Gray
        }
        else {
            Write-Host "`nNo failed jobs found in build $BuildId" -ForegroundColor Green
        }
        exit 0
    }
    
    # Report local test failures first (these may exist even without failed jobs)
    if ($localTestFailures.Count -gt 0) {
        Write-Host "`n=== Local Test Failures (non-Helix) ===" -ForegroundColor Yellow
        Write-Host "Build: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$BuildId" -ForegroundColor Gray
        
        foreach ($failure in $localTestFailures) {
            Write-Host "`n--- $($failure.TaskName) ---" -ForegroundColor Cyan
            
            # Show issues
            foreach ($issue in $failure.Issues) {
                Write-Host "  $($issue.message)" -ForegroundColor Red
            }
            
            # Show test run URLs if available
            if ($failure.TestRunUrls.Count -gt 0) {
                Write-Host "`n  Test Results:" -ForegroundColor Yellow
                foreach ($testRun in $failure.TestRunUrls) {
                    Write-Host "    Run $($testRun.RunId): $($testRun.Url)" -ForegroundColor Gray
                    
                    # Try to get actual failed test names via az devops CLI
                    $testResults = Get-AzDOTestResults -RunId $testRun.RunId -Org "https://dev.azure.com/$Organization"
                    if ($testResults -and $testResults.Count -gt 0) {
                        Write-Host "`n    Failed tests ($($testResults.Count)):" -ForegroundColor Red
                        foreach ($result in $testResults | Select-Object -First 10) {
                            Write-Host "      - $($result.name)" -ForegroundColor White
                        }
                        if ($testResults.Count -gt 10) {
                            Write-Host "      ... and $($testResults.Count - 10) more" -ForegroundColor Gray
                        }
                    }
                }
            }
            
            # Try to get more details from the task log
            if ($failure.LogId) {
                $logContent = Get-BuildLog -Build $BuildId -LogId $failure.LogId
                if ($logContent) {
                    # Extract test run URLs from this log too
                    $additionalRuns = Extract-TestRunUrls -LogContent $logContent
                    if ($additionalRuns.Count -gt 0 -and $failure.TestRunUrls.Count -eq 0) {
                        Write-Host "`n  Test Results:" -ForegroundColor Yellow
                        foreach ($testRun in $additionalRuns) {
                            Write-Host "    Run $($testRun.RunId): $($testRun.Url)" -ForegroundColor Gray
                            
                            # Try to get actual failed test names via az devops CLI
                            $testResults = Get-AzDOTestResults -RunId $testRun.RunId -Org "https://dev.azure.com/$Organization"
                            if ($testResults -and $testResults.Count -gt 0) {
                                Write-Host "`n    Failed tests ($($testResults.Count)):" -ForegroundColor Red
                                foreach ($result in $testResults | Select-Object -First 10) {
                                    Write-Host "      - $($result.name)" -ForegroundColor White
                                }
                                if ($testResults.Count -gt 10) {
                                    Write-Host "      ... and $($testResults.Count - 10) more" -ForegroundColor Gray
                                }
                            }
                        }
                    }
                    
                    # Classify the failure
                    $buildErrors = Extract-BuildErrors -LogContent $logContent
                    if ($buildErrors.Count -gt 0) {
                        $classification = Get-FailureClassification -Errors $buildErrors
                        if ($classification -and $classification.Type -ne 'Unknown') {
                            Show-ClassificationWithKnownIssues -Classification $classification -ErrorMessage ($buildErrors -join "`n")
                        }
                    }
                }
            }
        }
    }
    
    if (-not $failedJobs -or $failedJobs.Count -eq 0) {
        Write-Host "`n=== Summary ===" -ForegroundColor Yellow
        Write-Host "Local test failures: $($localTestFailures.Count)" -ForegroundColor Red
        Write-Host "Build URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$BuildId" -ForegroundColor Cyan
        exit 0
    }
    
    Write-Host "`nFound $($failedJobs.Count) failed job(s):" -ForegroundColor Red
    
    $processedJobs = 0
    foreach ($job in $failedJobs) {
        if ($processedJobs -ge $MaxJobs) {
            Write-Host "`n... and $($failedJobs.Count - $MaxJobs) more failed jobs (use -MaxJobs to see more)" -ForegroundColor Yellow
            break
        }
        
        Write-Host "`n--- $($job.name) ---" -ForegroundColor Cyan
        Write-Host "  Build: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$BuildId&view=logs&j=$($job.id)" -ForegroundColor Gray
        
        # Get Helix tasks for this job
        $helixTasks = Get-HelixJobInfo -Timeline $timeline -JobId $job.id
        
        if ($helixTasks) {
            foreach ($task in $helixTasks) {
                if ($task.log) {
                    Write-Host "  Fetching Helix task log..." -ForegroundColor Gray
                    $logContent = Get-BuildLog -Build $BuildId -LogId $task.log.id
                    
                    if ($logContent) {
                        # Extract test failures
                        $failures = Extract-TestFailures -LogContent $logContent
                        
                        if ($failures.Count -gt 0) {
                            Write-Host "  Failed tests:" -ForegroundColor Red
                            foreach ($failure in $failures) {
                                Write-Host "    - $($failure.TestName)" -ForegroundColor White
                            }
                        }
                        
                        # Extract and optionally fetch Helix URLs
                        $helixUrls = Extract-HelixUrls -LogContent $logContent
                        
                        if ($helixUrls.Count -gt 0 -and $ShowLogs) {
                            Write-Host "`n  Helix Console Logs:" -ForegroundColor Yellow
                            
                            foreach ($url in $helixUrls | Select-Object -First 3) {
                                Write-Host "`n  $url" -ForegroundColor Gray
                                
                                # Extract work item name from URL for known issue search
                                $workItemName = ""
                                if ($url -match '/workitems/([^/]+)/console') {
                                    $workItemName = $Matches[1]
                                }
                                
                                $helixLog = Get-HelixConsoleLog -Url $url
                                if ($helixLog) {
                                    $failureInfo = Format-TestFailure -LogContent $helixLog
                                    if ($failureInfo) {
                                        Write-Host $failureInfo -ForegroundColor White
                                        
                                        # Classify the Helix failure and search for known issues
                                        $classification = Get-FailureClassification -Errors @($failureInfo)
                                        if ($classification -and $classification.Type -ne 'Unknown') {
                                            Show-ClassificationWithKnownIssues -Classification $classification -TestName $workItemName -ErrorMessage $failureInfo
                                        }
                                    }
                                }
                            }
                        }
                        elseif ($helixUrls.Count -gt 0) {
                            Write-Host "`n  Helix logs available (use -ShowLogs to fetch):" -ForegroundColor Yellow
                            foreach ($url in $helixUrls | Select-Object -First 3) {
                                Write-Host "    $url" -ForegroundColor Gray
                            }
                        }
                    }
                }
            }
        }
        else {
            # No Helix tasks - this is a build failure, extract actual errors
            $buildTasks = $timeline.records | Where-Object { 
                $_.parentId -eq $job.id -and $_.result -eq "failed" 
            }
            
            foreach ($task in $buildTasks | Select-Object -First 3) {
                Write-Host "  Failed task: $($task.name)" -ForegroundColor Red
                
                # Fetch and parse the build log for actual errors
                if ($task.log) {
                    Write-Host "  Fetching build log..." -ForegroundColor Gray
                    $logContent = Get-BuildLog -Build $BuildId -LogId $task.log.id
                    
                    if ($logContent) {
                        $buildErrors = Extract-BuildErrors -LogContent $logContent
                        
                        if ($buildErrors.Count -gt 0) {
                            # Extract Helix log URLs from the full log content
                            $helixLogUrls = Extract-HelixLogUrls -LogContent $logContent
                            
                            if ($helixLogUrls.Count -gt 0) {
                                Write-Host "  Helix failures ($($helixLogUrls.Count)):" -ForegroundColor Red
                                foreach ($helixLog in $helixLogUrls | Select-Object -First 5) {
                                    Write-Host "    - $($helixLog.WorkItem)" -ForegroundColor White
                                    Write-Host "      Log: $($helixLog.Url)" -ForegroundColor Gray
                                }
                                if ($helixLogUrls.Count -gt 5) {
                                    Write-Host "    ... and $($helixLogUrls.Count - 5) more" -ForegroundColor Gray
                                }
                            }
                            else {
                                Write-Host "  Build errors:" -ForegroundColor Red
                                foreach ($err in $buildErrors | Select-Object -First 5) {
                                    Write-Host "    $err" -ForegroundColor White
                                }
                                if ($buildErrors.Count -gt 5) {
                                    Write-Host "    ... and $($buildErrors.Count - 5) more errors" -ForegroundColor Gray
                                }
                            }
                            
                            # Classify the failure and search for known issues
                            $classification = Get-FailureClassification -Errors $buildErrors
                            if ($classification) {
                                Show-ClassificationWithKnownIssues -Classification $classification -ErrorMessage ($buildErrors -join "`n")
                            }
                        }
                        else {
                            Write-Host "  (No specific errors extracted from log)" -ForegroundColor Gray
                        }
                    }
                }
            }
        }
        
        $processedJobs++
    }
    
    Write-Host "`n=== Summary ===" -ForegroundColor Yellow
    Write-Host "Total failed jobs: $($failedJobs.Count)" -ForegroundColor Red
    if ($localTestFailures.Count -gt 0) {
        Write-Host "Local test failures: $($localTestFailures.Count)" -ForegroundColor Red
    }
    Write-Host "Build URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$BuildId" -ForegroundColor Cyan
    
}
catch {
    Write-Error "Error: $_"
    exit 1
}
