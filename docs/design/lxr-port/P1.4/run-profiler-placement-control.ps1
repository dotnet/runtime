# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$CoreRoot,
    [Parameter(Mandatory)]
    [string]$TestDirectory,
    [Parameter(Mandatory)]
    [string]$HookLibrary,
    [string]$OutputDirectory,
    [string]$SummaryPath
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$customOutputDirectory = $PSBoundParameters.ContainsKey('OutputDirectory')
if (-not $OutputDirectory) {
    $repositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
    $OutputDirectory = Join-Path $repositoryRoot 'artifacts\p14-profiler-placement-control'
}
if (-not $SummaryPath) {
    $SummaryPath = if ($customOutputDirectory) {
        Join-Path $OutputDirectory 'profiler-placement-control.csv'
    } else {
        Join-Path $scriptRoot 'raw\profiler-placement-control.csv'
    }
}

$corerun = Join-Path $CoreRoot 'corerun.exe'
$testAssembly = Join-Path $TestDirectory 'gcallocate.dll'
foreach ($path in @($corerun, $testAssembly, $HookLibrary)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

$summary = [Collections.Generic.List[object]]::new()

function Invoke-Scenario(
    [string]$Name,
    [string]$Perturbation,
    [bool]$ExpectSuccess
) {
    $log = Join-Path $OutputDirectory "$Name.log"
    if ($Perturbation) {
        $env:P14_PROFILER_PLACEMENT_PERTURBATION = $Perturbation
    } else {
        Remove-Item Env:\P14_PROFILER_PLACEMENT_PERTURBATION -ErrorAction SilentlyContinue
    }

    Push-Location $TestDirectory
    try {
        & $corerun $testAssembly *> $log
        $exitCode = $LASTEXITCODE
    } finally {
        Pop-Location
    }
    $output = Get-Content -LiteralPath $log -Raw

    if ($ExpectSuccess) {
        if (($exitCode -ne 100) -or
            ($output -notmatch '(?m)^Profilee STDOUT: PROFILER TEST PASSES\r?$') -or
            ($output -match 'placement mismatch')) {
            throw "Profiler scenario failed: $Name. See $log."
        }
    } elseif (($exitCode -eq 100) -or
              ($output -notmatch 'Allocation-complete callback placement mismatch') -or
              ($output -match '(?m)^Profilee STDOUT: PROFILER TEST PASSES\r?$')) {
        throw "Profiler perturbation was not rejected: $Name. See $log."
    }

    $summary.Add([pscustomobject][ordered]@{
        Name = $Name
        Perturbation = if ($Perturbation) { $Perturbation } else { 'none' }
        Expected = if ($ExpectSuccess) { 'success' } else { 'placement mismatch' }
        Result = 'PASS'
        ExitCode = $exitCode
        Evidence = [IO.Path]::GetFileName($log)
    })
    Write-Host "PASS: $Name"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$env:CORE_ROOT = $CoreRoot
$env:DOTNET_ReadyToRun = '0'
$env:DOTNET_GCAllocationNotificationTest = '1'
$env:P14_NATIVE_HOOK_LIBRARY = $HookLibrary
try {
    Invoke-Scenario 'profiler-placement-positive' '' $true
    Invoke-Scenario 'profiler-placement-drop-control' 'drop' $false
    Invoke-Scenario 'profiler-placement-swap-control' 'swap' $false

    New-Item -ItemType Directory -Path (Split-Path -Parent $SummaryPath) -Force | Out-Null
    $summary |
        Export-Csv -LiteralPath $SummaryPath -NoTypeInformation
    Write-Host "RESULT: PASS ($($summary.Count) profiler placement controls)"
} finally {
    Remove-Item Env:\CORE_ROOT -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_ReadyToRun -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCAllocationNotificationTest -ErrorAction SilentlyContinue
    Remove-Item Env:\P14_NATIVE_HOOK_LIBRARY -ErrorAction SilentlyContinue
    Remove-Item Env:\P14_PROFILER_PLACEMENT_PERTURBATION -ErrorAction SilentlyContinue
}
