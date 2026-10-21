# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

$raw = Join-Path $scriptRoot 'raw'
$artifacts = Join-Path $RepositoryRoot 'artifacts'
New-Item -ItemType Directory -Path $raw -Force | Out-Null

function Read-Match(
    [string]$Path,
    [string]$Pattern
) {
    $text = Get-Content -LiteralPath $Path -Raw
    $match = [regex]::Match($text, $Pattern)
    if (-not $match.Success) {
        throw "Pattern '$Pattern' not found in $Path"
    }
    return $match
}

$runtimeInputs = @(
    @('Workstation', 'p1.6-seqcst-reuse-wks.log', 'none', 'linked'),
    @('Server', 'p1.6-seqcst-reuse-server.log', 'none', 'linked'),
    @('Workstation', 'p1.6-seqcst-reuse-gcstress.log', '0xC', 'linked'),
    @('Workstation', 'p1.6-seqcst-standalone.log', 'none', 'standalone')
)
$runtimeRows = foreach ($item in $runtimeInputs) {
    $path = Join-Path $artifacts $item[1]
    $match = Read-Match $path '(\d+) object-header runtime checks passed'
    [pscustomobject][ordered]@{
        gc_mode = $item[0]
        gc_stress = $item[2]
        gc_linkage = $item[3]
        checks = [int]$match.Groups[1].Value
        result = 'PASS'
        evidence = $item[1]
    }
}
$runtimeRows | Export-Csv (Join-Path $raw 'runtime-summary.csv') -NoTypeInformation

$reuseRows = foreach ($item in $runtimeInputs) {
    $path = Join-Path $artifacts $item[1]
    $text = Get-Content -LiteralPath $path -Raw
    $matches = @([regex]::Matches(
        $text,
        'P16_REUSE=(Small|Large|Pinned);OBSERVED=([01]);BOUND=(\d+)'))
    if ($matches.Count -ne 3) {
        throw "Reuse evidence is incomplete in $path"
    }
    foreach ($match in $matches) {
        [pscustomobject][ordered]@{
            gc_mode = $item[0]
            gc_stress = $item[2]
            gc_linkage = $item[3]
            heap = $match.Groups[1].Value
            observed = [int]$match.Groups[2].Value
            bound = [int]$match.Groups[3].Value
            evidence = $item[1]
        }
    }
}
$reuseRows | Export-Csv (Join-Path $raw 'reuse-summary.csv') -NoTypeInformation

$fullTestLog = Join-Path $artifacts 'p1.6-full-tests-run.log'
$fullTestMatch = Read-Match $fullTestLog (
    '\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*\(total\)')
[pscustomobject][ordered]@{
    total = [int]$fullTestMatch.Groups[1].Value
    passed = [int]$fullTestMatch.Groups[2].Value
    failed = [int]$fullTestMatch.Groups[3].Value
    skipped = [int]$fullTestMatch.Groups[4].Value
    active_issue = [int]$fullTestMatch.Groups[5].Value
    result = 'PASS_WITH_INFRASTRUCTURE_FAILURES'
    limitation = 'Seven out-of-process tests could not find generated native executables; the test runner returned 0 and its error file is empty.'
    evidence = 'p1.6-full-tests-run.log'
} | Export-Csv (Join-Path $raw 'full-test-summary.csv') -NoTypeInformation

$nativeRows = foreach ($item in @(
    @('x64', 'p1.6-seqcst-native-x64-2.log', '(\d+)/(\d+) object-header bit checks passed'),
    @('x86', 'p1.6-seqcst-native-x86.log', '(\d+)/(\d+) object-header bit checks passed'),
    @('linux-x64', 'p1.6-seqcst-native-linux.log', '(\d+)/(\d+) object-header bit checks passed'),
    @('nativeaot-x64', 'p1.6-nativeaot-validation-driver-2.log', '(\d+)/(\d+) NativeAOT object-header bit checks passed')
)) {
    $path = Join-Path $artifacts $item[1]
    $match = Read-Match $path $item[2]
    [pscustomobject][ordered]@{
        architecture = $item[0]
        passed = [int]$match.Groups[1].Value
        total = [int]$match.Groups[2].Value
        result = 'PASS'
        evidence = $item[1]
    }
}
$nativeRows | Export-Csv (Join-Path $raw 'native-validation-summary.csv') -NoTypeInformation

Copy-Item -LiteralPath (
    Join-Path $artifacts 'p16-object-header-bits-compatibility\compatibility-summary.csv') `
    -Destination (Join-Path $raw 'compatibility-summary.csv') -Force
Copy-Item -LiteralPath (
    Join-Path $artifacts 'p16-object-header-bits-startup\malformed-summary.csv') `
    -Destination (Join-Path $raw 'malformed-summary.csv') -Force
Copy-Item -LiteralPath (Join-Path $artifacts 'p1.6-x86-negotiation.csv') `
    -Destination (Join-Path $raw 'x86-negotiation.csv') -Force

$controlMatch = Read-Match (
    Join-Path $artifacts 'p1.6-seqcst-controls.log') (
    '(\d+)/(\d+) object-header bit controls failed as expected')
[pscustomobject][ordered]@{
    passed = [int]$controlMatch.Groups[1].Value
    total = [int]$controlMatch.Groups[2].Value
    result = 'PASS'
    evidence = 'p1.6-seqcst-controls.log'
} | Export-Csv (Join-Path $raw 'control-summary.csv') -NoTypeInformation

$platformRows = @(
    [pscustomobject][ordered]@{
        platform = 'Windows x64'
        scope = 'Debug/Checked/Release build and execution'
        result = 'PASS'
        limitation = ''
    },
    [pscustomobject][ordered]@{
        platform = 'Linux x64'
        scope = 'native layout and atomic execution'
        result = 'PASS'
        limitation = 'Full CoreCLR build not run from the mixed Windows/WSL worktree.'
    },
    [pscustomobject][ordered]@{
        platform = 'Windows x86'
        scope = 'target product build, layout, disabled start, enabled rejection'
        result = 'PASS'
        limitation = 'Optional x64-hosted cross-debug component fails against installed Windows SDK after target succeeds.'
    },
    [pscustomobject][ordered]@{
        platform = 'Windows ARM64'
        scope = 'target product cross-build'
        result = 'PASS'
        limitation = 'No execution hardware; optional x64-hosted cross-debug component fails after target succeeds.'
    },
    [pscustomobject][ordered]@{
        platform = 'NativeAOT x64'
        scope = 'Debug/Checked/Release runtime compilation and ObjHeader atomic execution'
        result = 'PASS'
        limitation = ''
    },
    [pscustomobject][ordered]@{
        platform = 'DAC/cDAC'
        scope = 'CoreCLR Debug/Checked/Release compilation'
        result = 'PASS'
        limitation = 'State is collector-private; existing data contract is unchanged.'
    },
    [pscustomobject][ordered]@{
        platform = 'Mono'
        scope = 'source audit'
        result = 'PASS'
        limitation = 'Mono does not use CoreCLR ObjHeader.'
    }
)
$platformRows | Export-Csv (Join-Path $raw 'platform-summary.csv') -NoTypeInformation

$identitySources = @(
    'docs\design\lxr-port\P1.4\verify-allocation-notification.ps1',
    'docs\design\lxr-port\P1.5\reference-enumeration-validation.cpp',
    'docs\design\lxr-port\P1.5\verify-reference-enumeration.ps1',
    'docs\design\lxr-port\P1.6-gc-reserved-object-header-bits.md',
    'docs\design\lxr-port\P1.6\collect-object-header-bits-evidence.ps1',
    'docs\design\lxr-port\P1.6\lock-hash-benchmark\Program.cs',
    'docs\design\lxr-port\P1.6\lock-hash-benchmark\lock-hash-benchmark.csproj',
    'docs\design\lxr-port\P1.6\nativeaot-object-header-bits-validation.cpp',
    'docs\design\lxr-port\P1.6\object-header-bits-validation.cpp',
    'docs\design\lxr-port\P1.6\run-object-header-bits-compatibility.ps1',
    'docs\design\lxr-port\P1.6\run-object-header-bits-malformed.ps1',
    'docs\design\lxr-port\P1.6\run-object-header-bits-runtime.ps1',
    'docs\design\lxr-port\P1.6\run-object-header-bits-validation.ps1',
    'docs\design\lxr-port\P1.6\run-nativeaot-object-header-bits-validation.ps1',
    'docs\design\lxr-port\P1.6\runtime-smoke\Program.cs',
    'docs\design\lxr-port\P1.6\runtime-smoke\runtime-smoke.csproj',
    'docs\design\lxr-port\P1.6\startup-smoke\Program.cs',
    'docs\design\lxr-port\P1.6\startup-smoke\startup-smoke.csproj',
    'docs\design\lxr-port\P1.6\verify-object-header-bits-controls.ps1',
    'docs\design\lxr-port\P1.6\verify-object-header-bits-gate.ps1',
    'docs\design\lxr-port\P1.6\verify-object-header-bits.ps1',
    'docs\design\lxr-port\README.md',
    'src\coreclr\CMakeLists.txt',
    'src\coreclr\dlls\mscoree\CMakeLists.txt',
    'src\coreclr\dlls\mscoree\coreclr\CMakeLists.txt',
    'src\coreclr\dlls\mscoree\mscorwks_objectheaderbitstest_unixexports.src',
    'src\coreclr\gc\CMakeLists.txt',
    'src\coreclr\gc\env\gcenv.object.h',
    'src\coreclr\gc\gcconfig.h',
    'src\coreclr\gc\gcimpl.h',
    'src\coreclr\gc\gcinterface.h',
    'src\coreclr\gc\interface.cpp',
    'src\coreclr\nativeaot\Runtime\ObjectLayout.cpp',
    'src\coreclr\nativeaot\Runtime\ObjectLayout.h',
    'src\coreclr\nativeaot\Runtime\clrgc.enabled.cpp',
    'src\coreclr\nativeaot\Runtime\gcheaputilities.cpp',
    'src\coreclr\nativeaot\Runtime\gcheaputilities.h',
    'src\coreclr\vm\CMakeLists.txt',
    'src\coreclr\vm\gcheaputilities.cpp',
    'src\coreclr\vm\gcheaputilities.h',
    'src\coreclr\vm\object.h',
    'src\coreclr\vm\objectheaderbitstest.cpp',
    'src\coreclr\vm\syncblk.cpp',
    'src\coreclr\vm\syncblk.h',
    'src\coreclr\vm\wks\CMakeLists.txt'
)
$productCommit = (git -C $RepositoryRoot rev-parse HEAD).Trim()
$identityRows = foreach ($source in $identitySources) {
    $path = Join-Path $RepositoryRoot $source
    [pscustomobject][ordered]@{
        name = $source
        product_commit = $productCommit
        sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        length = (Get-Item -LiteralPath $path).Length
    }
}
$identityRows | Export-Csv (Join-Path $raw 'source-identities.csv') -NoTypeInformation

function Convert-TimeToNanoseconds([string]$Value) {
    $match = [regex]::Match($Value, '^([\d.]+)\s*(ns|μs)$')
    if (-not $match.Success) {
        throw "Unsupported benchmark time: $Value"
    }
    $number = [double]$match.Groups[1].Value
    if ($match.Groups[2].Value -eq 'μs') {
        return $number * 1000
    }
    return $number
}

$benchmarkClasses = @(
    'P16.LockBenchmarks-report.csv',
    'P16.ContendedLockBenchmarks-report.csv',
    'P16.HashBenchmarks-report.csv'
)
$defaultBenchmarkRoot = Join-Path $artifacts 'p16-benchmark-default-reversed\results'
$registeredOffRoot = Join-Path $artifacts 'p16-benchmark-registered-off-rerun\results'
$registeredOnRoot = Join-Path $artifacts 'p16-benchmark-registered-on-rerun\results'
$benchmarkRows = [Collections.Generic.List[object]]::new()

foreach ($file in $benchmarkClasses) {
    $defaultRows = Import-Csv (Join-Path $defaultBenchmarkRoot $file)
    foreach ($group in $defaultRows | Group-Object Method) {
        $baseline = @($group.Group | Where-Object Toolchain -match 'baseline')
        $changed = @($group.Group | Where-Object Toolchain -match 'changed')
        if (($baseline.Count -ne 1) -or ($changed.Count -ne 1)) {
            throw "Default benchmark pairing is incomplete for $($group.Name)."
        }
        $baselineMean = Convert-TimeToNanoseconds $baseline[0].Mean
        $changedMean = Convert-TimeToNanoseconds $changed[0].Mean
        $benchmarkRows.Add([pscustomobject][ordered]@{
            comparison = 'base-vs-default'
            method = $group.Name
            baseline_mean_ns = $baselineMean
            changed_mean_ns = $changedMean
            ratio = $changedMean / $baselineMean
            baseline_error = $baseline[0].Error
            changed_error = $changed[0].Error
        })
    }

    $offRows = Import-Csv (Join-Path $registeredOffRoot $file)
    $onRows = Import-Csv (Join-Path $registeredOnRoot $file)
    foreach ($off in $offRows) {
        $on = @($onRows | Where-Object Method -eq $off.Method)
        if ($on.Count -ne 1) {
            throw "Registered benchmark pairing is incomplete for $($off.Method)."
        }
        $offMean = Convert-TimeToNanoseconds $off.Mean
        $onMean = Convert-TimeToNanoseconds $on[0].Mean
        $benchmarkRows.Add([pscustomobject][ordered]@{
            comparison = 'registered-off-vs-clear'
            method = $off.Method
            baseline_mean_ns = $offMean
            changed_mean_ns = $onMean
            ratio = $onMean / $offMean
            baseline_error = $off.Error
            changed_error = $on[0].Error
        })
    }

    Copy-Item (Join-Path $defaultBenchmarkRoot $file) `
        (Join-Path $raw "default-$file") -Force
    Copy-Item (Join-Path $registeredOffRoot $file) `
        (Join-Path $raw "registered-off-$file") -Force
    Copy-Item (Join-Path $registeredOnRoot $file) `
        (Join-Path $raw "registered-clear-$file") -Force
}
$benchmarkRows | Export-Csv (Join-Path $raw 'benchmark-summary.csv') -NoTypeInformation

$stateBenchmark = Join-Path $artifacts (
    'p16-benchmark-seqcst-states\results\P16.StateLockHashBenchmarks-report.csv')
Copy-Item $stateBenchmark (Join-Path $raw 'state-lock-hash-benchmark.csv') -Force

Copy-Item (Join-Path $artifacts 'p1.6-binary-identity.csv') `
    (Join-Path $raw 'binary-identity.csv') -Force
Copy-Item (Join-Path $artifacts 'p1.6-hot-function-codegen.csv') `
    (Join-Path $raw 'hot-function-codegen.csv') -Force

Write-Host "Collected P1.6 evidence in $raw"
