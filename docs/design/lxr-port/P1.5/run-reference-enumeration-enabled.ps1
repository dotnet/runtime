# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$RuntimeRoot,
    [string]$TestDirectory,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $RuntimeRoot) {
    $RuntimeRoot = Join-Path $RepositoryRoot (
        'artifacts\tests\coreclr\windows.x64.Debug\Tests\Core_Root')
}
if (-not $TestDirectory) {
    $TestDirectory = Join-Path $RepositoryRoot (
        'artifacts\tests\coreclr\windows.x64.Debug\profiler\' +
        'gcheapenumeration\gcheapenumeration')
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts'
}

$wrapper = Join-Path $TestDirectory 'gcheapenumeration.cmd'
$standaloneGC = Join-Path $RepositoryRoot (
    'artifacts\bin\coreclr\windows.x64.Debug\clrgc.dll')
$summaryPath = Join-Path $OutputDirectory 'p15-enabled-summary.csv'
$rows = [Collections.Generic.List[object]]::new()

foreach ($path in @($wrapper, $standaloneGC, (Join-Path $RuntimeRoot 'corerun.exe'))) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

$env:CORE_ROOT = $RuntimeRoot
$env:DOTNET_GCObjectReferenceEnumerationTest = '1'
$env:DOTNET_gcServer = '0'
try {
    foreach ($mode in @('linked', 'standalone')) {
        if ($mode -eq 'standalone') {
            $env:DOTNET_GCPath = $standaloneGC
        } else {
            Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
        }

        $logName = "p15-enabled-$mode.log"
        $log = Join-Path $OutputDirectory $logName
        Push-Location $TestDirectory
        try {
            & $wrapper *> $log
            $exitCode = $LASTEXITCODE
        } finally {
            Pop-Location
        }

        $output = Get-Content -LiteralPath $log -Raw
        $passMarkers = [regex]::Matches(
            $output,
            '(?m)^Profilee STDOUT: PROFILER TEST PASSES\r?$').Count
        if (($exitCode -notin @(0, 100)) -or
            ($passMarkers -ne 3) -or
            ($output -notmatch '(?m)^END EXECUTION - PASSED\r?$')) {
            throw "Enabled current-runtime run failed: $mode. See $log."
        }

        $rows.Add([pscustomobject][ordered]@{
            Mode = $mode
            Request = 'Enabled'
            ExitCode = $exitCode
            ProfilerPassMarkers = $passMarkers
            Result = 'PASS'
            Evidence = $logName
        })
        Write-Host "PASS: enabled $mode current runtime"
    }
} finally {
    foreach ($name in @(
        'CORE_ROOT',
        'DOTNET_GCObjectReferenceEnumerationTest',
        'DOTNET_gcServer',
        'DOTNET_GCPath'
    )) {
        Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
    }
}

if ($rows.Count -ne 2) {
    throw "Produced $($rows.Count) enabled rows; expected 2."
}
$rows | Export-Csv -LiteralPath $summaryPath -NoTypeInformation
Write-Host 'PASS: 2 enabled current-runtime controls'
$global:LASTEXITCODE = 0
