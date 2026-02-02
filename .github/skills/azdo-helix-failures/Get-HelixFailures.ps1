<#
.SYNOPSIS
    Retrieves test failures from Azure DevOps builds and Helix test runs.

.DESCRIPTION
    This script queries Azure DevOps for failed jobs in a build and retrieves
    the corresponding Helix console logs to show detailed test failure information.

.PARAMETER BuildId
    The Azure DevOps build ID to query.

.PARAMETER PRNumber
    The GitHub PR number to find the associated build.

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

.EXAMPLE
    .\Get-HelixFailures.ps1 -BuildId 1276327
    
.EXAMPLE
    .\Get-HelixFailures.ps1 -PRNumber 123445 -ShowLogs
#>

[CmdletBinding(DefaultParameterSetName = 'BuildId')]
param(
    [Parameter(ParameterSetName = 'BuildId', Mandatory = $true)]
    [int]$BuildId,

    [Parameter(ParameterSetName = 'PRNumber', Mandatory = $true)]
    [int]$PRNumber,

    [string]$Organization = "dnceng-public",
    [string]$Project = "cbb18261-c48f-4abb-8651-8cdcb5474649",
    [switch]$ShowLogs,
    [int]$MaxJobs = 5,
    [int]$MaxFailureLines = 50,
    [int]$TimeoutSec = 30
)

$ErrorActionPreference = "Stop"

function Get-AzDOBuildIdFromPR {
    param([int]$PR)
    
    # Check for gh CLI dependency
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        throw "GitHub CLI (gh) is required for PR lookup. Install from https://cli.github.com/ or use -BuildId instead."
    }
    
    Write-Host "Finding build for PR #$PR..." -ForegroundColor Cyan
    Write-Verbose "Running: gh pr checks $PR --repo dotnet/runtime"
    
    # Use gh cli to get the checks
    $checksOutput = gh pr checks $PR --repo dotnet/runtime 2>&1
    
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
    
    throw "Could not find Azure DevOps build for PR #$PR"
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
    
    $url = "https://dev.azure.com/$Organization/$Project/_apis/build/builds/$Build/logs/$LogId?api-version=7.0"
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
    
    # Match Helix console log URLs (use $urlMatches to avoid shadowing automatic $Matches variable)
    $urlMatches = [regex]::Matches($LogContent, 'https://helix\.dot\.net/api/[^/]+/jobs/[a-f0-9-]+/workitems/[^/\s]+/console')
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
    param([string]$LogContent)
    
    $errors = @()
    $lines = $LogContent -split "`n"
    
    # Patterns for common build errors
    $errorPatterns = @(
        '##\[error\].*',
        'error\s*:\s*.+',
        'error\s+CS\d+:.*',
        'error\s+MSB\d+:.*',
        'error\s+NU\d+:.*',
        'fatal error:.*',
        '\.pcm: No such file or directory',
        'EXEC\s*:\s*error\s*:.*'
    )
    
    $combinedPattern = ($errorPatterns -join '|')
    
    foreach ($line in $lines) {
        if ($line -match $combinedPattern) {
            # Clean up the line (remove timestamps, etc)
            $cleanLine = $line -replace '^\d{4}-\d{2}-\d{2}T[\d:\.]+Z\s*', ''
            $cleanLine = $cleanLine -replace '##\[error\]', 'ERROR: '
            $errors += $cleanLine.Trim()
        }
    }
    
    return $errors | Select-Object -Unique -First 10
}

function Get-FailureClassification {
    param([string[]]$Errors)
    
    $errorText = $Errors -join "`n"
    
    # Known failure patterns with classifications
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
        [int]$MaxLines = $MaxFailureLines
    )
    
    $lines = $LogContent -split "`n"
    $inFailure = $false
    $failureLines = @()
    $emptyLineCount = 0
    
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
        if (-not $inFailure -and $line -match $combinedPattern) {
            $inFailure = $true
            $emptyLineCount = 0
        }
        
        if ($inFailure) {
            $failureLines += $line
            
            # Track consecutive empty lines to detect end of stack trace
            if ($line -match '^\s*$') {
                $emptyLineCount++
            }
            else {
                $emptyLineCount = 0
            }
            
            # Stop after stack trace ends (2+ consecutive empty lines) or max lines reached
            if ($emptyLineCount -ge 2 -or $failureLines.Count -ge $MaxLines) {
                break
            }
        }
    }
    
    if ($failureLines.Count -ge $MaxLines) {
        $failureLines += "... (truncated, use -MaxFailureLines to see more)"
    }
    
    return $failureLines -join "`n"
}

# Main execution
try {
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
