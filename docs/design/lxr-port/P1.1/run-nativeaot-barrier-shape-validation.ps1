# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [ValidateSet('Debug', 'Checked', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

$build = Join-Path $RepositoryRoot 'build.cmd'
$testBuild = Join-Path $RepositoryRoot 'src\tests\build.cmd'
$testProject = 'nativeaot\SmokeTests\AttributeTrimming\AttributeTrimming.csproj'
$testExecutable = Join-Path $RepositoryRoot (
    "artifacts\tests\coreclr\windows.x64.$Configuration\" +
    'nativeaot\SmokeTests\AttributeTrimming\AttributeTrimming\native\AttributeTrimming.exe')

function Build-NativeAotRuntime([bool]$EnableRejectionTest) {
    $setting = if ($EnableRejectionTest) { '1' } else { '0' }
    & $build clr.nativeaotruntime -rc $Configuration `
        -cmakeargs "-DCLR_CMAKE_ENABLE_WRITE_BARRIER_STANDARD_ABI_TEST=$setting"
    if ($LASTEXITCODE -ne 0) {
        throw "NativeAOT runtime build failed with validation setting $setting."
    }
}

function Build-NativeAotTest {
    if (Test-Path -LiteralPath $testExecutable) {
        Remove-Item -LiteralPath $testExecutable -Force
    }

    & $testBuild nativeaot $Configuration test $testProject
    if ($LASTEXITCODE -ne 0) {
        throw 'NativeAOT test build failed.'
    }
    if (-not (Test-Path -LiteralPath $testExecutable -PathType Leaf)) {
        throw "NativeAOT test executable was not produced: $testExecutable"
    }
}

try {
    Build-NativeAotRuntime -EnableRejectionTest $true
    Build-NativeAotTest

    $rejectionOutput = & $testExecutable 2>&1 | Out-String
    $rejectionExitCode = $LASTEXITCODE
    if (($rejectionExitCode -ne -1) -or $rejectionOutput) {
        throw "NativeAOT did not reject the non-default shape before managed execution. Exit: $rejectionExitCode. Output: $rejectionOutput"
    }

    Write-Host 'PASS: NativeAOT rejected the non-default shape before managed execution'
}
finally {
    Build-NativeAotRuntime -EnableRejectionTest $false
}

Build-NativeAotTest
& $testExecutable
if ($LASTEXITCODE -ne 100) {
    throw "NativeAOT card-table execution failed with exit code $LASTEXITCODE."
}

Write-Host 'PASS: NativeAOT card-table application returned 100'
Write-Host '2/2 NativeAOT barrier-shape scenarios passed'
exit 0
