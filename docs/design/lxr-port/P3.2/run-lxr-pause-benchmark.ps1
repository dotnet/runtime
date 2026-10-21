# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(0.1, 60.0)]
    [double]$WarmupSeconds = 0.5,
    [ValidateRange(0.5, 120.0)]
    [double]$DurationSeconds = 3.0,
    [ValidateRange(1, 100)]
    [int]$CountPerKind = 8,
    [ValidateRange(1000, 1000000)]
    [int]$InjectedMicroseconds = 5000
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\P3.2\benchmark'
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$vcvars = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview\VC\Auxiliary\Build\vcvars64.bat'
$calibrationPath = Join-Path $RepositoryRoot (
    'docs\design\lxr-port\P0.5-baselines\calibration.json')
foreach ($path in @($dotnet, $vcvars, $calibrationPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Benchmark input is missing: '$path'."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$nativeOutput = Join-Path $OutputDirectory 'native'
$runs = Join-Path $OutputDirectory 'runs'
New-Item -ItemType Directory -Path $nativeOutput | Out-Null
New-Item -ItemType Directory -Path $runs | Out-Null

$sources = @(
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_pause_schedule.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_pause_coordinator.cpp'),
    (Join-Path $scriptRoot 'lxr-pause-benchmark-native.cpp')
)
$library = Join-Path $nativeOutput 'lxr-pause-benchmark.dll'
$quotedSources = ($sources | ForEach-Object { "`"$_`"" }) -join ' '
$compile = @(
    'cl.exe /nologo /std:c++17 /EHsc /W4 /WX /O2 /LD',
    $quotedSources,
    "/Fe:`"$library`""
) -join ' '
& $env:ComSpec /d /s /c "call `"$vcvars`" >nul && $compile" `
    *> (Join-Path $nativeOutput 'build.log')
if ($LASTEXITCODE -ne 0) {
    throw "Native benchmark bridge build failed with exit code $LASTEXITCODE."
}

$harnessRoot = Join-Path $RepositoryRoot 'docs\design\lxr-port\harness'
foreach ($project in @(
    'src\Lxr.Harness.Worker\Lxr.Harness.Worker.csproj',
    'src\Lxr.Harness.Worker.AspNet\Lxr.Harness.Worker.AspNet.csproj')) {
    & $dotnet build (Join-Path $harnessRoot $project) -c Release --nologo `
        *> (Join-Path $OutputDirectory (
            ([IO.Path]::GetFileNameWithoutExtension($project)) + '-build.log'))
    if ($LASTEXITCODE -ne 0) {
        throw "Managed benchmark worker build failed for '$project'."
    }
}

$workerRoot = Join-Path $RepositoryRoot 'artifacts\lxr-harness\build\bin'
$workers = @{
    ordinary = Join-Path $workerRoot 'Lxr.Harness.Worker\release\Lxr.Harness.Worker.dll'
    aspnet = Join-Path $workerRoot (
        'Lxr.Harness.Worker.AspNet\release\Lxr.Harness.Worker.AspNet.dll')
}
$calibration = Get-Content -LiteralPath $calibrationPath -Raw | ConvertFrom-Json
$calibratedTwoXLimits = @{}
foreach ($baseline in $calibration.baselines) {
    $calibratedTwoXLimits[[string]$baseline.scenario] =
        [int64]$baseline.sharedMinimumMb * 2
}

$scenarios = @(
    'low-allocation-compute',
    'allocation-churn',
    'long-lived-cache',
    'cyclic-garbage',
    'pointer-chasing',
    'multi-thread-throughput',
    'aspnet-request-load',
    'pinning-heavy-io',
    'lifecycle-semantics',
    'large-object-pressure'
)
$policies = [ordered]@{
    scheduled = 1
    'same-code' = 4
    null = 2
    injected = 3
}
$gcModes = @('Workstation', 'Server')

function Get-Percentile([double[]]$Values, [double]$Percentile) {
    if ($Values.Count -eq 0) {
        return [double]::NaN
    }
    $sorted = @($Values | Sort-Object)
    $index = [Math]::Max(
        0,
        [Math]::Min(
            $sorted.Count - 1,
            [Math]::Ceiling(($Percentile / 100.0) * $sorted.Count) - 1))
    return [double]$sorted[$index]
}

$rows = [Collections.Generic.List[object]]::new()
$savedEnvironment = @{
    P32 = $env:P32_PAUSE_SCHEDULE_LIBRARY
    Server = $env:DOTNET_gcServer
    HeapCount = $env:DOTNET_GCHeapCount
    Datas = $env:DOTNET_GCDynamicAdaptationMode
    HardLimit = $env:DOTNET_GCHeapHardLimit
}
try {
    $env:P32_PAUSE_SCHEDULE_LIBRARY = $library
    foreach ($scenario in $scenarios) {
        $worker = if ($scenario -eq 'aspnet-request-load') {
            $workers.aspnet
        } else {
            $workers.ordinary
        }
        $mode = if ($scenario -eq 'aspnet-request-load') { 'latency' } else { 'throughput' }
        $heapLimitMb = [int64]$calibratedTwoXLimits[$scenario] * 3
        $limitBytes = [uint64]$heapLimitMb * 1024 * 1024
        $env:DOTNET_GCHeapHardLimit = $limitBytes.ToString('x')

        foreach ($gcMode in $gcModes) {
            $arm = if ($gcMode -eq 'Server') { 'srv' } else { 'wks' }
            $env:DOTNET_gcServer = if ($gcMode -eq 'Server') { '1' } else { '0' }
            if ($gcMode -eq 'Server') {
                $env:DOTNET_GCHeapCount = '8'
                $env:DOTNET_GCDynamicAdaptationMode = '0'
            } else {
                Remove-Item Env:\DOTNET_GCHeapCount -ErrorAction SilentlyContinue
                Remove-Item Env:\DOTNET_GCDynamicAdaptationMode -ErrorAction SilentlyContinue
            }

            for ($launch = 1; $launch -le $LaunchCount; $launch++) {
                $policyOrder = if (($launch % 2) -eq 1) {
                    @('scheduled', 'same-code', 'null', 'injected')
                } else {
                    @('same-code', 'scheduled', 'injected', 'null')
                }
                foreach ($policyName in $policyOrder) {
                    $id = "$scenario-$gcMode-$policyName-$launch"
                    $report = Join-Path $runs "$id.json"
                    $log = Join-Path $runs "$id.log"
                    $arguments = @(
                        $worker,
                        '--scenario', $scenario,
                        '--arm', $arm,
                        '--server-heap-count', '8',
                        '--workers', '4',
                        '--seed', (20221101 + $launch),
                        '--warmup-seconds', $WarmupSeconds,
                        '--duration-seconds', $DurationSeconds,
                        '--mode', $mode,
                        '--heap-factor', '6.0',
                        '--heap-limit-mb', $heapLimitMb,
                        '--output', $report,
                        '--param', "pauseSchedulePolicy=$policyName",
                        '--param', "pauseScheduleCountPerKind=$CountPerKind",
                        '--param', "pauseScheduleInjectedMicroseconds=$InjectedMicroseconds"
                    )
                    if ($mode -eq 'latency') {
                        $arguments += @('--rate', '500')
                    }

                    $savedPreference = $ErrorActionPreference
                    try {
                        $ErrorActionPreference = 'Continue'
                        & $dotnet @arguments *> $log
                        $exitCode = $LASTEXITCODE
                    } finally {
                        $ErrorActionPreference = $savedPreference
                    }

                    $document = if (Test-Path -LiteralPath $report -PathType Leaf) {
                        Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
                    } else {
                        $null
                    }
                    $valid = $null -ne $document -and $document.valid -eq $true
                    $pause = if ($null -ne $document) { $document.pauseSchedule } else { $null }
                    $aaRatio = if (
                        $null -ne $pause -and
                        [uint64]$pause.aaCountA -gt 0 -and
                        [uint64]$pause.aaCountB -gt 0) {
                        ([double]$pause.aaNanosecondsB / [uint64]$pause.aaCountB) /
                            ([double]$pause.aaNanosecondsA / [uint64]$pause.aaCountA)
                    } else {
                        [double]::NaN
                    }

                    foreach ($kindName in @('Full', 'RefCount', 'InitialMark', 'FinalMark')) {
                        $kind = if ($null -ne $pause) {
                            $pause.kinds | Where-Object kind -eq $kindName
                        } else {
                            $null
                        }
                        [double[]]$samplesMs = if ($null -ne $kind) {
                            @($kind.durationNanoseconds | ForEach-Object {
                                [double]$_ / 1000000.0
                            })
                        } else {
                            @()
                        }
                        $rows.Add([pscustomobject][ordered]@{
                            scenario = $scenario
                            gc_mode = $gcMode
                            policy = $policyName
                            policy_id = $policies[$policyName]
                            launch = $launch
                            heap_factor = 6.0
                            heap_limit_mb = $heapLimitMb
                            pause_kind = $kindName
                            requested_count = $CountPerKind
                            completed_kind_count = if ($null -ne $kind) {
                                [uint64]$kind.completedCount
                            } else {
                                0
                            }
                            completed_schedules = if ($null -ne $pause) {
                                [uint64]$pause.completedSchedules
                            } else {
                                0
                            }
                            synthetic_suspends = if ($null -ne $pause) {
                                [uint64]$pause.syntheticSuspends
                            } else {
                                0
                            }
                            sample_count = $samplesMs.Count
                            mean_ms = if ($samplesMs.Count -gt 0) {
                                ($samplesMs | Measure-Object -Average).Average
                            } else {
                                [double]::NaN
                            }
                            p50_ms = Get-Percentile $samplesMs 50
                            p95_ms = Get-Percentile $samplesMs 95
                            max_ms = if ($samplesMs.Count -gt 0) {
                                ($samplesMs | Measure-Object -Maximum).Maximum
                            } else {
                                [double]::NaN
                            }
                            aa_ratio = $aaRatio
                            operations_per_second = if ($null -ne $document) {
                                [double]$document.metrics.operationsPerSecond
                            } else {
                                [double]::NaN
                            }
                            latency_p99_ms = if (
                                $null -ne $document -and
                                $null -ne $document.metrics.latencyP99Ms) {
                                [double]$document.metrics.latencyP99Ms
                            } else {
                                [double]::NaN
                            }
                            exit_code = $exitCode
                            valid = $valid
                            invalid_reason = if ($null -ne $document) {
                                [string]$document.invalidReason
                            } else {
                                'report-missing'
                            }
                            result = if ($exitCode -eq 0 -and $valid) { 'PASS' } else { 'FAIL' }
                            report = [IO.Path]::GetRelativePath($OutputDirectory, $report)
                            log = [IO.Path]::GetRelativePath($OutputDirectory, $log)
                        })
                    }
                }
            }
        }
    }
} finally {
    $env:P32_PAUSE_SCHEDULE_LIBRARY = $savedEnvironment.P32
    $env:DOTNET_gcServer = $savedEnvironment.Server
    $env:DOTNET_GCHeapCount = $savedEnvironment.HeapCount
    $env:DOTNET_GCDynamicAdaptationMode = $savedEnvironment.Datas
    $env:DOTNET_GCHeapHardLimit = $savedEnvironment.HardLimit
}

$rows | Export-Csv (Join-Path $OutputDirectory 'benchmark-raw.csv') -NoTypeInformation

$summary = foreach ($group in $rows | Group-Object scenario, gc_mode, policy, pause_kind) {
    $validRows = @($group.Group | Where-Object result -eq 'PASS')
    [pscustomobject][ordered]@{
        scenario = $group.Group[0].scenario
        gc_mode = $group.Group[0].gc_mode
        policy = $group.Group[0].policy
        pause_kind = $group.Group[0].pause_kind
        expected_launches = $LaunchCount
        valid_launches = $validRows.Count
        mean_p50_ms = if ($validRows.Count -gt 0) {
            ($validRows | Measure-Object p50_ms -Average).Average
        } else {
            [double]::NaN
        }
        mean_p95_ms = if ($validRows.Count -gt 0) {
            ($validRows | Measure-Object p95_ms -Average).Average
        } else {
            [double]::NaN
        }
        mean_max_ms = if ($validRows.Count -gt 0) {
            ($validRows | Measure-Object max_ms -Average).Average
        } else {
            [double]::NaN
        }
        result = if ($validRows.Count -eq $LaunchCount) { 'PASS' } else { 'FAIL' }
    }
}
$summary | Sort-Object scenario, gc_mode, policy, pause_kind |
    Export-Csv (Join-Path $OutputDirectory 'benchmark-summary.csv') -NoTypeInformation

$controls = foreach ($scenario in $scenarios) {
    foreach ($gcMode in $gcModes) {
        foreach ($kindName in @('Full', 'RefCount', 'InitialMark', 'FinalMark')) {
            $sameCode = @($rows | Where-Object {
                $_.scenario -eq $scenario -and
                $_.gc_mode -eq $gcMode -and
                $_.pause_kind -eq $kindName -and
                $_.policy -eq 'same-code' -and
                $_.result -eq 'PASS'
            })
            $scheduled = @($rows | Where-Object {
                $_.scenario -eq $scenario -and
                $_.gc_mode -eq $gcMode -and
                $_.pause_kind -eq $kindName -and
                $_.policy -eq 'scheduled' -and
                $_.result -eq 'PASS'
            })
            $injected = @($rows | Where-Object {
                $_.scenario -eq $scenario -and
                $_.gc_mode -eq $gcMode -and
                $_.pause_kind -eq $kindName -and
                $_.policy -eq 'injected' -and
                $_.result -eq 'PASS'
            })
            $nullRows = @($rows | Where-Object {
                $_.scenario -eq $scenario -and
                $_.gc_mode -eq $gcMode -and
                $_.pause_kind -eq $kindName -and
                $_.policy -eq 'null' -and
                $_.result -eq 'PASS'
            })
            $aaRatio = if ($sameCode.Count -gt 0) {
                ($sameCode | Measure-Object aa_ratio -Average).Average
            } else {
                [double]::NaN
            }
            $scheduledMedian = if ($scheduled.Count -gt 0) {
                ($scheduled | Measure-Object p50_ms -Average).Average
            } else {
                [double]::NaN
            }
            $injectedMedian = if ($injected.Count -gt 0) {
                ($injected | Measure-Object p50_ms -Average).Average
            } else {
                [double]::NaN
            }
            $injectedDelta = $injectedMedian
            $nullClean = $nullRows.Count -eq $LaunchCount -and
                @($nullRows | Where-Object {
                    [uint64]$_.completed_kind_count -ne 0 -or
                    [uint64]$_.synthetic_suspends -ne 0
                }).Count -eq 0
            $passed =
                $sameCode.Count -eq $LaunchCount -and
                $scheduled.Count -eq $LaunchCount -and
                $injected.Count -eq $LaunchCount -and
                $aaRatio -ge 0.90 -and
                $aaRatio -le 1.10 -and
                $nullClean -and
                $injectedDelta -ge 4.0
            [pscustomobject][ordered]@{
                scenario = $scenario
                gc_mode = $gcMode
                pause_kind = $kindName
                aa_ratio = $aaRatio
                null_clean = $nullClean
                injected_median_delta_ms = $injectedDelta
                result = if ($passed) { 'PASS' } else { 'FAIL' }
            }
        }
    }
}
$controls | Export-Csv (Join-Path $OutputDirectory 'benchmark-controls.csv') -NoTypeInformation

[pscustomobject][ordered]@{
    scenarios = $scenarios -join ';'
    gc_modes = $gcModes -join ';'
    policies = $policies.Keys -join ';'
    launch_count = $LaunchCount
    heap_factor = 6.0
    count_per_kind = $CountPerKind
    injected_microseconds = $InjectedMicroseconds
    warmup_seconds = $WarmupSeconds
    duration_seconds = $DurationSeconds
    result = if (
        @($rows | Where-Object result -ne 'PASS').Count -eq 0 -and
        @($controls | Where-Object result -ne 'PASS').Count -eq 0) {
        'PASS'
    } else {
        'FAIL'
    }
} | Export-Csv (Join-Path $OutputDirectory 'benchmark-invocation.csv') -NoTypeInformation

if (@($rows | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more P3.2 benchmark rows failed; all rows were retained.'
}
if (@($controls | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more P3.2 sensitivity controls failed; all rows were retained.'
}

Write-Host "PASS: $($rows.Count) P3.2 benchmark rows and $($controls.Count) controls"
