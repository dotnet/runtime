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
$runtime = @(Import-Csv (Join-Path $raw 'runtime-summary.csv'))
$reuse = @(Import-Csv (Join-Path $raw 'reuse-summary.csv'))
$fullTests = @(Import-Csv (Join-Path $raw 'full-test-summary.csv'))
$native = @(Import-Csv (Join-Path $raw 'native-validation-summary.csv'))
$compatibility = @(Import-Csv (Join-Path $raw 'compatibility-summary.csv'))
$malformed = @(Import-Csv (Join-Path $raw 'malformed-summary.csv'))
$controls = @(Import-Csv (Join-Path $raw 'control-summary.csv'))
$x86 = @(Import-Csv (Join-Path $raw 'x86-negotiation.csv'))
$platform = @(Import-Csv (Join-Path $raw 'platform-summary.csv'))
$identities = @(Import-Csv (Join-Path $raw 'source-identities.csv'))
$benchmarks = @(Import-Csv (Join-Path $raw 'benchmark-summary.csv'))
$stateBenchmarks = @(Import-Csv (Join-Path $raw 'state-lock-hash-benchmark.csv'))
$codegen = @(Import-Csv (Join-Path $raw 'hot-function-codegen.csv'))

$expectedIdentitySources = @(
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
$actualIdentitySources = @($identities | ForEach-Object name)
$duplicateIdentitySources = @(
    $actualIdentitySources |
        Group-Object |
        Where-Object Count -ne 1)
$identityDifferences = @(
    Compare-Object `
        ($expectedIdentitySources | Sort-Object) `
        ($actualIdentitySources | Sort-Object))
if (($identities.Count -ne $expectedIdentitySources.Count) -or
    ($duplicateIdentitySources.Count -ne 0) -or
    ($identityDifferences.Count -ne 0)) {
    throw 'Identity manifest path set mismatch.'
}

if (($runtime.Count -ne 4) -or
    (@($runtime | Where-Object {
        ($_.result -ne 'PASS') -or ([int]$_.checks -ne 295)
    }).Count -ne 0)) {
    throw 'Runtime scenario evidence is incomplete.'
}

if (($reuse.Count -ne 12) -or
    (@($reuse | Where-Object {
        (($_.heap -eq 'Large') -or ($_.heap -eq 'Pinned')) -and
        ([int]$_.observed -ne 1)
    }).Count -ne 0) -or
    (@($reuse | Where-Object {
        $row = $_
        $expectedBound = switch ($row.heap) {
            'Small' {
                if ($row.gc_stress -eq '0xC') { 1024 } else { 160000 }
            }
            'Large' { 1024 }
            'Pinned' { 8192 }
            default { -1 }
        }
        [int]$row.bound -ne $expectedBound
    }).Count -ne 0)) {
    throw 'Recycled-allocation evidence is incomplete.'
}
foreach ($configuration in $reuse | Group-Object gc_mode, gc_stress, gc_linkage) {
    if (($configuration.Count -ne 3) -or
        (($configuration.Group.heap | Sort-Object -Unique) -join ',') -ne
            'Large,Pinned,Small') {
        throw "Recycled-allocation heap coverage is incomplete: $($configuration.Name)"
    }
}

if (($fullTests.Count -ne 1) -or
    ([int]$fullTests[0].total -ne 4286) -or
    ([int]$fullTests[0].passed -ne 4130) -or
    ([int]$fullTests[0].failed -ne 7) -or
    ($fullTests[0].result -ne 'PASS_WITH_INFRASTRUCTURE_FAILURES')) {
    throw 'Full-suite accounting is incomplete.'
}

$x64Native = @($native | Where-Object architecture -eq 'x64')
$x86Native = @($native | Where-Object architecture -eq 'x86')
$linuxNative = @($native | Where-Object architecture -eq 'linux-x64')
$nativeAot = @($native | Where-Object architecture -eq 'nativeaot-x64')
if (($native.Count -ne 4) -or
    ($x64Native.Count -ne 1) -or
    ([int]$x64Native[0].passed -ne 28) -or
    ([int]$x64Native[0].total -ne 28) -or
    ($x86Native.Count -ne 1) -or
    ([int]$x86Native[0].passed -ne 6) -or
    ([int]$x86Native[0].total -ne 6) -or
    ($linuxNative.Count -ne 1) -or
    ([int]$linuxNative[0].passed -ne 28) -or
    ([int]$linuxNative[0].total -ne 28) -or
    ($nativeAot.Count -ne 1) -or
    ([int]$nativeAot[0].passed -ne 7) -or
    ([int]$nativeAot[0].total -ne 7)) {
    throw 'Native architecture evidence is incomplete.'
}

if (($compatibility.Count -ne 7) -or
    (@($compatibility | Where-Object Result -ne 'PASS').Count -ne 0)) {
    throw 'Compatibility evidence is incomplete.'
}

if (($malformed.Count -ne 12) -or
    (@($malformed | Where-Object passed -ne 'True').Count -ne 0)) {
    throw 'Malformed startup evidence is incomplete.'
}

if (($controls.Count -ne 1) -or
    ([int]$controls[0].passed -ne 2) -or
    ([int]$controls[0].total -ne 2) -or
    ($controls[0].result -ne 'PASS')) {
    throw 'Negative-control evidence is incomplete.'
}

if (($x86.Count -ne 1) -or
    ($x86[0].passed -ne 'True') -or
    ([int]$x86[0].disabled_exit -ne 100) -or
    ([int]$x86[0].enabled_exit -eq 100)) {
    throw 'x86 fail-closed evidence is incomplete.'
}

if (($platform.Count -ne 7) -or
    (@($platform | Where-Object result -ne 'PASS').Count -ne 0)) {
    throw 'Platform accounting is incomplete.'
}

foreach ($identity in $identities) {
    $path = Join-Path $RepositoryRoot $identity.name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Identity source is missing: $($identity.name)"
    }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $length = (Get-Item -LiteralPath $path).Length
    if (($hash -ne $identity.sha256) -or ($length -ne [int64]$identity.length)) {
        throw "Identity mismatch: $($identity.name)"
    }
}

if (($benchmarks.Count -ne 14) -or
    (@($benchmarks | Where-Object {
        ([double]$_.ratio -lt 0.80) -or ([double]$_.ratio -gt 1.20)
    }).Count -ne 0)) {
    throw 'Benchmark evidence is incomplete or outside its noise guard.'
}

if (($stateBenchmarks.Count -ne 12) -or
    (($stateBenchmarks.State | Sort-Object -Unique) -join ',') -ne '0,2,3') {
    throw 'Stress-state benchmark evidence is incomplete.'
}

if ($codegen.Count -ne 6) {
    throw 'Hot-function codegen evidence is incomplete.'
}
foreach ($symbol in $codegen | Group-Object symbol) {
    if (($symbol.Count -ne 2) -or
        (($symbol.Group.normalized_sha256 | Select-Object -Unique).Count -ne 1) -or
        (($symbol.Group.instruction_count | Select-Object -Unique).Count -ne 1)) {
        throw "Hot-function codegen changed: $($symbol.Name)"
    }
}

Write-Host 'PASS: P1.6 object-header bit evidence'
