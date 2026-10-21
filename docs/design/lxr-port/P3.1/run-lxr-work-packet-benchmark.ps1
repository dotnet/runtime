# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(0.1, 60.0)]
    [double]$WarmupSeconds = 1.0,
    [ValidateRange(0.5, 120.0)]
    [double]$DurationSeconds = 3.0
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\P3.1\benchmark'
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$workerProject = Join-Path $RepositoryRoot (
    'docs\design\lxr-port\harness\src\Lxr.Harness.Worker\Lxr.Harness.Worker.csproj')
$worker = Join-Path $RepositoryRoot (
    'artifacts\lxr-harness\build\bin\Lxr.Harness.Worker\release\Lxr.Harness.Worker.dll')
$vcvars = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview\VC\Auxiliary\Build\vcvars64.bat'
foreach ($path in @($dotnet, $workerProject, $vcvars)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Benchmark input is missing: '$path'."
    }
}

$nativeOutput = Join-Path $OutputDirectory 'native'
$runs = Join-Path $OutputDirectory 'runs'
New-Item -ItemType Directory -Path $nativeOutput | Out-Null
New-Item -ItemType Directory -Path $runs | Out-Null

$packetSource = Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.cpp'
$schedulerSource = Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp'
$benchmarkSource = Join-Path $scriptRoot 'lxr-work-packet-benchmark-native.cpp'
$library = Join-Path $nativeOutput 'lxr-work-packet-benchmark.dll'
$compile = @(
    'cl.exe /nologo /std:c++17 /EHsc /O2 /LD',
    "`"$packetSource`"",
    "`"$schedulerSource`"",
    "`"$benchmarkSource`"",
    "/Fe:`"$library`""
) -join ' '
& $env:ComSpec /d /s /c "call `"$vcvars`" >nul && $compile" `
    *> (Join-Path $nativeOutput 'build.log')
if ($LASTEXITCODE -ne 0) {
    throw "Native benchmark bridge build failed with exit code $LASTEXITCODE."
}

& $dotnet build $workerProject -c Release --nologo `
    *> (Join-Path $OutputDirectory 'managed-build.log')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $worker -PathType Leaf)) {
    throw "Managed benchmark worker build failed with exit code $LASTEXITCODE."
}

$policies = [ordered]@{
    direct = 0
    scheduled = 1
    null = 2
    injected = 3
    replicate = 4
}
$workerCounts = @(1, 2, 4, 8, 16)
$rows = [Collections.Generic.List[object]]::new()
$savedLibrary = $env:P31_SCHEDULER_BENCHMARK_LIBRARY
try {
    $env:P31_SCHEDULER_BENCHMARK_LIBRARY = $library
    foreach ($workers in $workerCounts) {
        for ($launch = 1; $launch -le $LaunchCount; $launch++) {
            $policyOrder = if (($launch % 2) -eq 1) {
                @('scheduled', 'replicate', 'direct', 'null', 'injected')
            } else {
                @('replicate', 'scheduled', 'injected', 'null', 'direct')
            }
            foreach ($policyName in $policyOrder) {
                $policy = [pscustomobject]@{
                    Key = $policyName
                    Value = $policies[$policyName]
                }
                $id = "w$workers-$($policy.Key)-$launch"
                $report = Join-Path $runs "$id.json"
                $samples = Join-Path $runs "$id.bin.gz"
                $log = Join-Path $runs "$id.log"
                $savedPreference = $ErrorActionPreference
                try {
                    $ErrorActionPreference = 'Continue'
                    & $dotnet $worker `
                        --scenario multi-thread-throughput `
                        --workers $workers `
                        --seed (20221101 + $launch) `
                        --warmup-seconds $WarmupSeconds `
                        --duration-seconds $DurationSeconds `
                        --mode throughput `
                        --output $report `
                        --samples $samples `
                        --param schedulerBenchmark=true `
                        --param "schedulerBenchmarkMode=$($policy.Value)" `
                        --param schedulerReferenceCapacity=1024 `
                        --param allocationsPerOperation=0 `
                        --param sharedTouchesPerOperation=0 `
                        *> $log
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
                $detail = if ($null -ne $document) { [string]$document.verificationDetail } else { '' }
                $steals = if ($detail -match 'steals=(\d+)') { [uint64]$Matches[1] } else { 0 }
                $internalAaRatio = if ($detail -match 'aaRatio=([0-9.Ee+-]+)') {
                    [double]$Matches[1]
                } else {
                    [double]::NaN
                }
                $rows.Add([pscustomobject][ordered]@{
                    workers = $workers
                    policy = $policy.Key
                    policy_id = $policy.Value
                    launch = $launch
                    reference_capacity = 1024
                    warmup_seconds = $WarmupSeconds
                    duration_seconds = $DurationSeconds
                    exit_code = $exitCode
                    valid = $valid
                    operations_per_second = if ($valid) {
                        [double]$document.metrics.operationsPerSecond
                    } else {
                        [double]::NaN
                    }
                    operations_completed = if ($null -ne $document) {
                        [uint64]$document.metrics.operationsCompleted
                    } else {
                        0
                    }
                    steals = $steals
                    internal_aa_ratio = $internalAaRatio
                    verification_detail = $detail
                    invalid_reason = if ($null -ne $document) {
                        [string]$document.invalidReason
                    } else {
                        'report-missing'
                    }
                    result = if ($exitCode -eq 0 -and $valid) { 'PASS' } else { 'FAIL' }
                    report = [IO.Path]::GetRelativePath($OutputDirectory, $report)
                    samples = [IO.Path]::GetRelativePath($OutputDirectory, $samples)
                    log = [IO.Path]::GetRelativePath($OutputDirectory, $log)
                })
            }
        }
    }
} finally {
    $env:P31_SCHEDULER_BENCHMARK_LIBRARY = $savedLibrary
}

$rows | Export-Csv (Join-Path $OutputDirectory 'benchmark-raw.csv') -NoTypeInformation
$summary = [Collections.Generic.List[object]]::new()
foreach ($group in $rows | Group-Object workers, policy) {
    $validRows = @($group.Group | Where-Object result -eq 'PASS')
    $summary.Add([pscustomobject][ordered]@{
        workers = $group.Group[0].workers
        policy = $group.Group[0].policy
        expected_launches = $LaunchCount
        valid_launches = $validRows.Count
        mean_operations_per_second = if ($validRows.Count -gt 0) {
            ($validRows | Measure-Object operations_per_second -Average).Average
        } else {
            [double]::NaN
        }
        total_steals = ($validRows | Measure-Object steals -Sum).Sum
        result = if ($validRows.Count -eq $LaunchCount) { 'PASS' } else { 'FAIL' }
    })
}
$summary | Sort-Object workers, policy |
    Export-Csv (Join-Path $OutputDirectory 'benchmark-summary.csv') -NoTypeInformation

$controls = [Collections.Generic.List[object]]::new()
foreach ($workers in $workerCounts) {
    $byPolicy = @{}
    foreach ($policy in $policies.Keys) {
        $entry = $summary | Where-Object {
            $_.workers -eq $workers -and $_.policy -eq $policy
        }
        $byPolicy[$policy] = [double]$entry.mean_operations_per_second
    }
    $aaRows = @($rows | Where-Object {
        $_.workers -eq $workers -and
        $_.policy -eq 'replicate' -and
        $_.result -eq 'PASS'
    })
    $aaRatio = ($aaRows | Measure-Object internal_aa_ratio -Average).Average
    $injectedRatio = $byPolicy['scheduled'] / $byPolicy['injected']
    $directRatio = $byPolicy['scheduled'] / $byPolicy['direct']
    $steals = [uint64](($summary | Where-Object {
        $_.workers -eq $workers -and $_.policy -eq 'scheduled'
    }).total_steals)
    $controlResult =
        $aaRatio -ge 0.90 -and
        $aaRatio -le 1.10 -and
        $injectedRatio -ge 1.25 -and
        ($workers -eq 1 -or $steals -gt 0)
    $controls.Add([pscustomobject][ordered]@{
        workers = $workers
        aa_ratio = $aaRatio
        injected_cost_ratio = $injectedRatio
        scheduled_vs_direct_ratio = $directRatio
        scheduled_steals = $steals
        result = if ($controlResult) { 'PASS' } else { 'FAIL' }
    })
}
$controls | Export-Csv (Join-Path $OutputDirectory 'benchmark-controls.csv') -NoTypeInformation

[pscustomobject][ordered]@{
    launch_count = $LaunchCount
    worker_counts = $workerCounts -join ';'
    policies = $policies.Keys -join ';'
    reference_capacity = 1024
    warmup_seconds = $WarmupSeconds
    duration_seconds = $DurationSeconds
    machine_logical_processors = [Environment]::ProcessorCount
    result = if (
        @($rows | Where-Object result -ne 'PASS').Count -eq 0 -and
        @($controls | Where-Object result -ne 'PASS').Count -eq 0) {
        'PASS'
    } else {
        'FAIL'
    }
} | Export-Csv (Join-Path $OutputDirectory 'benchmark-invocation.csv') -NoTypeInformation

if (@($rows | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more benchmark rows failed; all rows were retained.'
}
if (@($controls | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more benchmark sensitivity controls failed; all rows were retained.'
}

Write-Host "PASS: $($rows.Count) P3.1 benchmark rows and $($controls.Count) controls"
