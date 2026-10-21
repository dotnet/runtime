# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(0, 100)]
    [int]$WarmupCycles = 1,
    [ValidateRange(1, 1024)]
    [int]$MeasuredCycles = 8,
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
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\P3.4\benchmark'
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
$binary = Join-Path $OutputDirectory 'lxr-completion-counter-benchmark.exe'
$sources = @(
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_completion_counter.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_packet.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\lxr_work_scheduler.cpp'),
    (Join-Path $scriptRoot 'lxr-completion-counter-benchmark-native.cpp')
)
$quotedSources = ($sources | ForEach-Object { "`"$_`"" }) -join ' '
$objectDirectory = $OutputDirectory.Replace('\', '/') + '/'
$compile = "cl.exe /nologo /std:c++17 /EHsc /O2 /W4 /WX " +
    "/DLXR_ENABLE_LAZY_COMPLETION_COUNTER $quotedSources " +
    "/Fo`"$objectDirectory`" /Fe:`"$binary`""
$buildLog = Join-Path $OutputDirectory 'build.log'
& $env:ComSpec /d /s /c "call `"$vcvars`" >nul && $compile" *> $buildLog
if ($LASTEXITCODE -ne 0) {
    throw "P3.4 benchmark build failed with exit code $LASTEXITCODE."
}

$policies = [ordered]@{
    tracked = [pscustomobject]@{ Arm = 'tracked'; Passes = 1 }
    'same-code' = [pscustomobject]@{ Arm = 'untracked'; Passes = 1 }
    null = [pscustomobject]@{ Arm = 'tracked'; Passes = 0 }
    injected = [pscustomobject]@{ Arm = 'tracked'; Passes = 65 }
    aa = [pscustomobject]@{ Arm = 'aa'; Passes = 1 }
}
$workers = @(1, 2, 4, 8, 16)
$depths = @(0, 1, 4, 16, 32)
$capacity = 1024
$rows = [Collections.Generic.List[object]]::new()
foreach ($launch in 1..$LaunchCount) {
    $policyOrder = if (($launch % 2) -eq 1) {
        @('tracked', 'aa', 'same-code', 'null', 'injected')
    } else {
        @('injected', 'null', 'same-code', 'aa', 'tracked')
    }
    $depthOrder = if (($launch % 2) -eq 1) { $depths } else { @($depths[($depths.Count - 1)..0]) }
    foreach ($worker in $workers) {
        foreach ($depth in $depthOrder) {
            foreach ($policyName in $policyOrder) {
                $policy = $policies[$policyName]
                $id = "w$worker-d$depth-$policyName-$launch"
                $log = Join-Path $runs "$id.log"
                $output = @(& $binary $policy.Arm $worker $PacketCount $capacity `
                    $depth $policy.Passes $WarmupCycles $MeasuredCycles 2>&1)
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
                $cycles = $MeasuredCycles
                $rows.Add([pscustomobject][ordered]@{
                    workers = $worker
                    backlog = $depth
                    policy = $policyName
                    arm = $policy.Arm
                    callback_passes = $policy.Passes
                    launch = $launch
                    packet_count = $PacketCount
                    reference_capacity = $capacity
                    warmup_cycles = $WarmupCycles
                    measured_cycles = $MeasuredCycles
                    exit_code = $exitCode
                    mean_drain_a_ns = if ($valid) { [uint64]$values.drain_a_ns / [double]$cycles } else { [double]::NaN }
                    mean_drain_b_ns = if ($valid -and $policy.Arm -eq 'aa') { [uint64]$values.drain_b_ns / [double]$cycles } else { [double]::NaN }
                    mean_next_pause_a_ns = if ($valid) { [uint64]$values.pause_a_ns / [double]$cycles } else { [double]::NaN }
                    mean_next_pause_b_ns = if ($valid -and $policy.Arm -eq 'aa') { [uint64]$values.pause_b_ns / [double]$cycles } else { [double]::NaN }
                    mean_combined_next_pause_a_ns = if ($valid) { ([uint64]$values.drain_a_ns + [uint64]$values.pause_a_ns) / [double]$cycles } else { [double]::NaN }
                    mean_combined_next_pause_b_ns = if ($valid -and $policy.Arm -eq 'aa') { ([uint64]$values.drain_b_ns + [uint64]$values.pause_b_ns) / [double]$cycles } else { [double]::NaN }
                    observed_min = if ($valid) { [uint64]$values.observed_min } else { 0 }
                    observed_max = if ($valid) { [uint64]$values.observed_max } else { 0 }
                    callbacks = if ($valid) { [uint64]$values.callbacks } else { 0 }
                    checksum = if ($valid) { [uint64]$values.checksum } else { 0 }
                    submitted = if ($valid) { [uint64]$values.submitted } else { 0 }
                    executed = if ($valid) { [uint64]$values.executed } else { 0 }
                    stolen = if ($valid) { [uint64]$values.stolen } else { 0 }
                    decrement_callbacks = if ($valid) { [uint64]$values.decrement_callbacks } else { 0 }
                    overall_callbacks = if ($valid) { [uint64]$values.overall_callbacks } else { 0 }
                    ownership_violations = if ($valid) { [uint64]$values.ownership_violations } else { 0 }
                    result = if ($valid) { 'PASS' } else { 'FAIL' }
                    exclusion_reason = if ($valid) { '' } else { "exit=$exitCode result_lines=$($line.Count)" }
                    log = [IO.Path]::GetRelativePath($OutputDirectory, $log)
                })
            }
        }
    }
}
$rows | Export-Csv (Join-Path $OutputDirectory 'benchmark-raw.csv') -NoTypeInformation

$summary = foreach ($group in $rows | Group-Object workers, backlog, policy) {
    $valid = @($group.Group | Where-Object result -eq 'PASS')
    [pscustomobject][ordered]@{
        workers = $group.Group[0].workers
        backlog = $group.Group[0].backlog
        policy = $group.Group[0].policy
        expected_launches = $LaunchCount
        valid_launches = $valid.Count
        mean_drain_a_ns = ($valid | Measure-Object mean_drain_a_ns -Average).Average
        mean_drain_b_ns = if ($group.Group[0].policy -eq 'aa') { ($valid | Measure-Object mean_drain_b_ns -Average).Average } else { [double]::NaN }
        mean_next_pause_a_ns = ($valid | Measure-Object mean_next_pause_a_ns -Average).Average
        mean_next_pause_b_ns = if ($group.Group[0].policy -eq 'aa') { ($valid | Measure-Object mean_next_pause_b_ns -Average).Average } else { [double]::NaN }
        mean_combined_next_pause_a_ns = ($valid | Measure-Object mean_combined_next_pause_a_ns -Average).Average
        mean_combined_next_pause_b_ns = if ($group.Group[0].policy -eq 'aa') { ($valid | Measure-Object mean_combined_next_pause_b_ns -Average).Average } else { [double]::NaN }
        result = if ($valid.Count -eq $LaunchCount) { 'PASS' } else { 'FAIL' }
    }
}
$summary | Sort-Object workers, backlog, policy |
    Export-Csv (Join-Path $OutputDirectory 'benchmark-summary.csv') -NoTypeInformation

$controls = foreach ($worker in $workers) {
    $aaRows = @($summary | Where-Object { $_.workers -eq $worker -and $_.policy -eq 'aa' -and $_.backlog -eq 32 })
    $baseRows = @($summary | Where-Object { $_.workers -eq $worker -and $_.policy -eq 'tracked' })
    $injectedRows = @($summary | Where-Object { $_.workers -eq $worker -and $_.policy -eq 'injected' })
    $aaRatios = @($aaRows | ForEach-Object { [double]$_.mean_drain_b_ns / [double]$_.mean_drain_a_ns })
    $injectedDepth32 = $injectedRows | Where-Object backlog -eq 32
    $injectedDepth0 = $injectedRows | Where-Object backlog -eq 0
    $baseDepth32 = $baseRows | Where-Object backlog -eq 32
    $injectedRatio = [double]$injectedDepth32.mean_drain_a_ns / [double]$baseDepth32.mean_drain_a_ns
    $backlogRatio = [double]$injectedDepth32.mean_drain_a_ns / [Math]::Max(1.0, [double]$injectedDepth0.mean_drain_a_ns)
    $aaMin = ($aaRatios | Measure-Object -Minimum).Minimum
    $aaMax = ($aaRatios | Measure-Object -Maximum).Maximum
    [pscustomobject][ordered]@{
        workers = $worker
        aa_min = $aaMin
        aa_max = $aaMax
        injected_to_base_depth32 = $injectedRatio
        injected_depth32_to_depth0 = $backlogRatio
        result = if ($aaMin -ge 0.25 -and $aaMax -le 4.0 -and $injectedRatio -ge 1.25 -and $backlogRatio -ge 1.25) { 'PASS' } else { 'FAIL' }
    }
}
$controls | Export-Csv (Join-Path $OutputDirectory 'benchmark-controls.csv') -NoTypeInformation

[pscustomobject][ordered]@{
    launches = $LaunchCount
    workers = $workers -join ';'
    backlogs = $depths -join ';'
    policies = $policies.Keys -join ';'
    packet_count = $PacketCount
    reference_capacity = $capacity
    warmup_cycles = $WarmupCycles
    measured_cycles = $MeasuredCycles
    aa_lower = 0.25
    aa_upper = 4.0
    injected_minimum = 1.25
    backlog_sensitivity_minimum = 1.25
    result = if (@($rows | Where-Object result -ne 'PASS').Count -eq 0 -and @($controls | Where-Object result -ne 'PASS').Count -eq 0) { 'PASS' } else { 'FAIL' }
} | Export-Csv (Join-Path $OutputDirectory 'benchmark-invocation.csv') -NoTypeInformation

if (@($rows | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more P3.4 benchmark rows failed; all rows were retained.'
}
if (@($controls | Where-Object result -ne 'PASS').Count -ne 0) {
    throw 'One or more P3.4 benchmark controls failed; all rows were retained.'
}
Write-Host "PASS: $($rows.Count) P3.4 benchmark rows and $($controls.Count) controls"
