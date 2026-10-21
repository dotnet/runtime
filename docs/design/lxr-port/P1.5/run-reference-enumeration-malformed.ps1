# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$RuntimeRoot,
    [string]$SmokeAssembly,
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
if (-not $SmokeAssembly) {
    $SmokeAssembly = Join-Path $RepositoryRoot (
        'artifacts\tests\coreclr\windows.x64.Debug\profiler\' +
        'gcheapenumeration\gcheapenumeration\gcheapenumeration.dll')
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts'
}

$runtime = Join-Path $RuntimeRoot 'corerun.exe'
$standaloneGC = Join-Path $RepositoryRoot (
    'artifacts\bin\coreclr\windows.x64.Debug\clrgc.dll')
$summaryPath = Join-Path $OutputDirectory 'p15-malformed-summary.csv'
$expectedPatterns = @{
    1 = 'descriptor was not initialized to NotProcessed'
    2 = 'resolver must be initialized to null'
    3 = 'descriptor is null'
    4 = 'request is invalid'
}
$rows = [Collections.Generic.List[object]]::new()

foreach ($path in @($runtime, $standaloneGC, $SmokeAssembly)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

$env:DOTNET_GCObjectReferenceEnumerationTest = '1'
$env:DOTNET_ReadyToRun = '0'
try {
    foreach ($mode in @('linked', 'standalone')) {
        if ($mode -eq 'standalone') {
            $env:DOTNET_GCPath = $standaloneGC
        } else {
            Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
        }

        foreach ($malformed in 1..4) {
            $env:DOTNET_GCObjectReferenceEnumerationTestMalformed =
                $malformed.ToString()
            $logName = "p15-malformed-$mode-$malformed.log"
            $log = Join-Path $OutputDirectory $logName
            & $runtime $SmokeAssembly RunTest `
                EnumerateGCHeapObjectsSingleThreadNoPriorSuspension *> $log
            $exitCode = $LASTEXITCODE
            $output = Get-Content -LiteralPath $log -Raw
            $expectedPattern = $expectedPatterns[$malformed]
            if (($exitCode -eq 0) -or
                ($exitCode -eq 100) -or
                ($output -notmatch [regex]::Escape($expectedPattern)) -or
                ($output -notmatch 'coreclr_initialize failed')) {
                throw "Malformed control failed: $mode/$malformed. See $log."
            }

            $rows.Add([pscustomobject][ordered]@{
                Mode = $mode
                Malformed = $malformed
                ExpectedPattern = $expectedPattern
                ExitCode = $exitCode
                Result = 'PASS'
                Evidence = $logName
            })
            Write-Host "PASS: $mode malformed $malformed"
        }
    }
} finally {
    foreach ($name in @(
        'DOTNET_GCObjectReferenceEnumerationTest',
        'DOTNET_GCObjectReferenceEnumerationTestMalformed',
        'DOTNET_GCPath',
        'DOTNET_ReadyToRun'
    )) {
        Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
    }
}

if ($rows.Count -ne 8) {
    throw "Produced $($rows.Count) malformed rows; expected 8."
}
$rows | Export-Csv -LiteralPath $summaryPath -NoTypeInformation
Write-Host 'PASS: 8 malformed request controls'
$global:LASTEXITCODE = 0
