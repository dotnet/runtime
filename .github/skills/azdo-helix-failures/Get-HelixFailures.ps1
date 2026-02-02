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

    [string]$Repository = "dotnet/runtime",
    [string]$Organization = "dnceng-public",
    [string]$Project = "cbb18261-c48f-4abb-8651-8cdcb5474649",
    [switch]$ShowLogs,
    [int]$MaxJobs = 5,
    [int]$MaxFailureLines = 50,
    [int]$TimeoutSec = 30,
    [int]$ContextLines = 0
)

$ErrorActionPreference = "Stop"

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

function Get-AzDOTimeline {
    param([int]$Build)
    
    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/$Build/timeline?api-version=7.0"
    Write-Host "Fetching build timeline..." -ForegroundColor Cyan
    Write-Verbose "GET $url"
    
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec $TimeoutSec
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
    Write-Verbose "GET $url"
    
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec $TimeoutSec
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

function Get-HelixJobDetails {
    param([string]$JobId)
    
    $url = "https://helix.dot.net/api/2019-06-17/jobs/$JobId"
    Write-Verbose "GET $url"
    
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec $TimeoutSec
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
    Write-Verbose "GET $url"
    
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec $TimeoutSec
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
    Write-Verbose "GET $url"
    
    try {
        $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec $TimeoutSec
        return $response
    }
    catch {
        Write-Warning "Failed to fetch work item ${WorkItemName}: $_"
        return $null
    }
}

function Get-HelixConsoleLog {
    param([string]$Url)
    
    Write-Verbose "GET $Url"
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec $TimeoutSec
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
                        
                        # Classify the failure
                        $classification = Get-FailureClassification -Errors @($failureInfo)
                        if ($classification) {
                            Write-Host "`n  Classification: $($classification.Summary)" -ForegroundColor Yellow
                            if ($classification.Action) {
                                Write-Host "  Suggested action: $($classification.Action)" -ForegroundColor Green
                            }
                            if ($classification.Transient) {
                                Write-Host "  (This failure appears to be transient - retry may help)" -ForegroundColor Cyan
                            }
                        }
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
    
    # Get timeline
    $timeline = Get-AzDOTimeline -Build $BuildId
    
    # Get failed jobs
    $failedJobs = Get-FailedJobs -Timeline $timeline
    
    if (-not $failedJobs -or $failedJobs.Count -eq 0) {
        Write-Host "`nNo failed jobs found in build $BuildId" -ForegroundColor Green
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
                                
                                $helixLog = Get-HelixConsoleLog -Url $url
                                if ($helixLog) {
                                    $failureInfo = Format-TestFailure -LogContent $helixLog
                                    if ($failureInfo) {
                                        Write-Host $failureInfo -ForegroundColor White
                                        
                                        # Classify the Helix failure
                                        $classification = Get-FailureClassification -Errors @($failureInfo)
                                        if ($classification -and $classification.Type -ne 'Unknown') {
                                            Write-Host "`n  Classification: $($classification.Summary)" -ForegroundColor Yellow
                                            if ($classification.Action) {
                                                Write-Host "  Suggested action: $($classification.Action)" -ForegroundColor Green
                                            }
                                            if ($classification.Transient) {
                                                Write-Host "  (This failure appears to be transient - retry may help)" -ForegroundColor Cyan
                                            }
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
                            Write-Host "  Build errors:" -ForegroundColor Red
                            foreach ($err in $buildErrors) {
                                Write-Host "    $err" -ForegroundColor White
                            }
                            
                            # Classify the failure
                            $classification = Get-FailureClassification -Errors $buildErrors
                            if ($classification) {
                                Write-Host "`n  Classification: $($classification.Summary)" -ForegroundColor Yellow
                                if ($classification.Action) {
                                    Write-Host "  Suggested action: $($classification.Action)" -ForegroundColor Green
                                }
                                if ($classification.Transient) {
                                    Write-Host "  (This failure appears to be transient - retry may help)" -ForegroundColor Cyan
                                }
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
    Write-Host "Build URL: https://dev.azure.com/$Organization/$Project/_build/results?buildId=$BuildId" -ForegroundColor Cyan
    
}
catch {
    Write-Error "Error: $_"
    exit 1
}
