# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(1, 1024)]
    [int]$WarmupCycles = 4,
    [ValidateRange(1, 4096)]
    [int]$MeasuredCycles = 64,
    [ValidateRange(1, 4096)]
    [int]$PacketCount = 32
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\P3.3\benchmark'
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$vcvars = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview\VC\Auxiliary\Build\vcvars64.bat'
if (-not (Test-Path -LiteralPath $vcvars -PathType Leaf)) {
    throw "Visual C++ environment script not found: '$vcvars'."
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$runs = Join-Path $OutputDirectory 'runs'
New-Item -ItemType Directory -Path $runs | Out-Null
$binary = Join-Path $OutputDirectory 'lxr-deferred-bucket-benchmark.exe'
$packetSource = Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.cpp'
$schedulerSource = Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp'
$benchmarkSource = Join-Path $scriptRoot 'lxr-deferred-bucket-benchmark-native.cpp'
$compile = @(
    'cl.exe /nologo /std:c++17 /EHsc /O2',
    "`"$packetSource`"",
    "`"$schedulerSource`"",
    "`"$benchmarkSource`"",
    "/Fe:`"$binary`""
) -join ' '
& $env:ComSpec /d /s /c "call `"$vcvars`" >nul && $compile" `
    *> (Join-Path $OutputDirectory 'build.log')
if ($LASTEXITCODE -ne 0) {
    throw "P3.3 benchmark build failed with exit code $LASTEXITCODE."
}

$policies = [ordered]@{
    lazy = [pscustomobject]@{ Arm = 'lazy'; Passes = 1 }
    eager = [pscustomobject]@{ Arm = 'eager'; Passes = 1 }
    'lazy-null' = [pscustomobject]@{ Arm = 'lazy'; Passes = 0 }
    'eager-null' = [pscustomobject]@{ Arm = 'eager'; Passes = 0 }
    'lazy-injected' = [pscustomobject]@{ Arm = 'lazy'; Passes = 65 }
    'eager-injected' = [pscustomobject]@{ Arm = 'eager'; Passes = 65 }
    aa = [pscustomobject]@{ Arm = 'aa'; Passes = 1 }
}
$workerCounts = @(1, 2, 4, 8, 16)
$referenceCapacity = 1024
$rows = [Collections.Generic.List[object]]::new()
$savedPasses = $env:P33_CALLBACK_PASSES
try {
    foreach ($workers in $workerCounts) {
        for ($launch = 1; $launch -le $LaunchCount; $launch++) {
            $policyOrder = if (($launch % 2) -eq 1) {
                @('lazy', 'aa', 'eager', 'lazy-null', 'eager-injected', 'eager-null', 'lazy-injected')
            } else {
                @('eager', 'aa', 'lazy', 'eager-null', 'lazy-injected', 'lazy-null', 'eager-injected')
            }
            foreach ($policyName in $policyOrder) {
                $policy = $policies[$policyName]
                $env:P33_CALLBACK_PASSES = [string]$policy.Passes
                $id = "w$workers-$policyName-$launch"
                $log = Join-Path $runs "$id.log"
                $output = @(
                    & $binary `
                        $policy.Arm `
                        $workers `
                        $PacketCount `
                        $referenceCapacity `
                        $WarmupCycles `
                        $MeasuredCycles 2>&1
                )
                $exitCode = $LASTEXITCODE
                $output | Out-File $log -Encoding ascii
                $line = @($output | Where-Object { $_ -like 'RESULT *' } | Select-Object -Last 1)
                $values = @{}
                if ($line.Count -eq 1) {
                    foreach ($match in [regex]::Matches($line[0], '(?<key>[a-z_]+)=(?<value>[^ ]+)')) {
                        $values[$match.Groups['key'].Value] = $match.Groups['value'].Value
                    }
                }
                $valid = $exitCode -eq 0 -and $line.Count -eq 1
                $pauseA = if ($valid) { [uint64]$values.pause_a_ns } else { 0 }
                $pauseB = if ($valid) { [uint64]$values.pause_b_ns } else { 0 }
                $totalA = if ($valid) { [uint64]$values.total_a_ns } else { 0 }
                $totalB = if ($valid) { [uint64]$values.total_b_ns } else { 0 }
                $cyclesPerLabel = $MeasuredCycles
                $rows.Add([pscustomobject][ordered]@{
                    workers = $workers
                    policy = $policyName
                    arm = $policy.Arm
                    callback_passes = $policy.Passes
                    launch = $launch
                    packet_count = $PacketCount
                    reference_capacity = $referenceCapacity
                    warmup_cycles = $WarmupCycles
                    measured_cycles = $MeasuredCycles
                    exit_code = $exitCode
                    pause_a_ns = $pauseA
                    pause_b_ns = $pauseB
                    mean_pause_a_ns = if ($valid) { $pauseA / [double]$cyclesPerLabel } else { [double]::NaN }
                    mean_pause_b_ns = if ($valid -and $policy.Arm -eq 'aa') {
                        $pauseB / [double]$cyclesPerLabel
                    } else {
                        [double]::NaN
                    }
                    total_a_ns = $totalA
                    total_b_ns = $totalB
                    callbacks = if ($valid) { [uint64]$values.callbacks } else { 0 }
                    checksum = if ($valid) { [uint64]$values.checksum } else { 0 }
                    submitted = if ($valid) { [uint64]$values.submitted } else { 0 }
                    executed = if ($valid) { [uint64]$values.executed } else { 0 }
                    deferred_submitted = if ($valid) { [uint64]$values.deferred_submitted } else { 0 }
                    deferred_published = if ($valid) { [uint64]$values.deferred_published } else { 0 }
                    deferred_executed = if ($valid) { [uint64]$values.deferred_executed } else { 0 }
                    flips = if ($valid) { [uint64]$values.flips } else { 0 }
                    empty_flips = if ($valid) { [uint64]$values.empty_flips } else { 0 }
                    ownership_violations = if ($valid) { [uint64]$values.ownership_violations } else { 0 }
                    result = if ($valid) { 'PASS' } else { 'FAIL' }
                    log = [IO.Path]::GetRelativePath($OutputDirectory, $log)
                })
            }
        }
    }
} finally {
    $env:P33_CALLBACK_PASSES = $savedPasses
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
        mean_pause_a_ns = if ($validRows.Count -gt 0) {
            ($validRows | Measure-Object mean_pause_a_ns -Average).Average
        } else {
            [double]::NaN
        }
        mean_pause_b_ns = if ($validRows.Count -gt 0 -and $group.Group[0].policy -eq 'aa') {
            ($validRows | Measure-Object mean_pause_b_ns -Average).Average
        } else {
            [double]::NaN
        }
        callbacks = ($validRows | Measure-Object callbacks -Sum).Sum
        ownership_violations = ($validRows | Measure-Object ownership_violations -Sum).Sum
        result = if ($validRows.Count -eq $LaunchCount) { 'PASS' } else { 'FAIL' }
    })
}
$summary | Sort-Object workers, policy |
    Export-Csv (Join-Path $OutputDirectory 'benchmark-summary.csv') -NoTypeInformation

$controls = [Collections.Generic.List[object]]::new()
foreach ($workers in $workerCounts) {
    $byPolicy = @{}
    foreach ($policyName in $policies.Keys) {
        $byPolicy[$policyName] = $summary | Where-Object {
            $_.workers -eq $workers -and $_.policy -eq $policyName
        }
    }
    $aaRatio = [double]$byPolicy.aa.mean_pause_b_ns /
        [double]$byPolicy.aa.mean_pause_a_ns
    $eagerInjectedRatio = [double]$byPolicy.'eager-injected'.mean_pause_a_ns /
        [double]$byPolicy.eager.mean_pause_a_ns
    $lazyInjectedRatio = [double]$byPolicy.'lazy-injected'.mean_pause_a_ns /
        [double]$byPolicy.lazy.mean_pause_a_ns
    $injectedSeparationRatio = $eagerInjectedRatio / $lazyInjectedRatio
    $deferralRatio = [double]$byPolicy.eager.mean_pause_a_ns /
        [double]$byPolicy.lazy.mean_pause_a_ns
    $eagerNullRatio = [double]$byPolicy.eager.mean_pause_a_ns /
        [double]$byPolicy.'eager-null'.mean_pause_a_ns
    $controlResult =
        $aaRatio -ge 0.75 -and
        $aaRatio -le 1.25 -and
        $eagerInjectedRatio -ge 1.25 -and
        $injectedSeparationRatio -ge 2.0 -and
        $deferralRatio -gt 1.0
    $controls.Add([pscustomobject][ordered]@{
        workers = $workers
        aa_ratio = $aaRatio
        eager_injected_ratio = $eagerInjectedRatio
        lazy_injected_ratio = $lazyInjectedRatio
        injected_separation_ratio = $injectedSeparationRatio
        eager_to_lazy_pause_ratio = $deferralRatio
        eager_to_eager_null_ratio = $eagerNullRatio
        result = if ($controlResult) { 'PASS' } else { 'FAIL' }
    })
}
$controls | Export-Csv (Join-Path $OutputDirectory 'benchmark-controls.csv') -NoTypeInformation

[pscustomobject][ordered]@{
    launch_count = $LaunchCount
    worker_counts = $workerCounts -join ';'
    policies = $policies.Keys -join ';'
    packet_count = $PacketCount
    reference_capacity = $referenceCapacity
    warmup_cycles = $WarmupCycles
    measured_cycles = $MeasuredCycles
    aa_lower = 0.75
    aa_upper = 1.25
    eager_injected_minimum = 1.25
    injected_separation_minimum = 2.0
    result = if (
        @($rows | Where-Object result -ne 'PASS').Count -eq 0 -and
        @($controls | Where-Object result -ne 'PASS').Count -eq 0) {
        'PASS'
    } else {
        'FAIL'
    }
} | Export-Csv (Join-Path $OutputDirectory 'benchmark-invocation.csv') -NoTypeInformation

if (@($rows | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more P3.3 benchmark rows failed; all rows were retained.'
}
if (@($controls | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more P3.3 benchmark sensitivity controls failed; all rows were retained.'
}

Write-Host "PASS: $($rows.Count) P3.3 benchmark rows and $($controls.Count) controls"
