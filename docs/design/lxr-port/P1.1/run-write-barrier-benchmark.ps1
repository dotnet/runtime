# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [Parameter(Mandatory)]
    [string]$ProductionRuntimeRoot,
    [Parameter(Mandatory)]
    [string]$ValidationRuntimeRoot,
    [Parameter(Mandatory)]
    [string]$ValidationStandaloneGC,
    [string]$DotNet,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(1, 100)]
    [int]$WarmupCount = 8,
    [ValidateRange(1, 100)]
    [int]$IterationCount = 20
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $DotNet) {
    $DotNet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\p11-write-barrier-benchmark'
}

$project = Join-Path $scriptRoot 'write-barrier-benchmark\write-barrier-benchmark.csproj'
$assembly = Join-Path $RepositoryRoot (
    'artifacts\bin\write-barrier-benchmark\Release\net11.0\write-barrier-benchmark.dll')
$productionRuntime = Join-Path $ProductionRuntimeRoot 'corerun.exe'
$validationRuntime = Join-Path $ValidationRuntimeRoot 'corerun.exe'

foreach ($path in @(
    $DotNet,
    $project,
    $productionRuntime,
    $validationRuntime,
    $ValidationStandaloneGC
)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
& $DotNet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

function Run-Benchmark(
    [string]$Name,
    [string]$Runtime,
    [string[]]$EnvironmentVariables
) {
    $arguments = @(
        'run',
        '--no-build',
        '-c', 'Release',
        '--project', $project,
        '--',
        '--filter', '*',
        '--coreRun', $Runtime,
        '--launchCount', $LaunchCount,
        '--warmupCount', $WarmupCount,
        '--iterationCount', $IterationCount,
        '--artifacts', (Join-Path $OutputDirectory $Name)
    )
    if ($EnvironmentVariables.Count -ne 0) {
        $arguments += '--envvars'
        $arguments += $EnvironmentVariables
    }

    $log = Join-Path $OutputDirectory "$Name.log"
    & $DotNet @arguments *> $log
    if ($LASTEXITCODE -ne 0) {
        throw "Benchmark failed: $Name. See $log."
    }

    Write-Host "PASS: $Name"
}

function Capture-Codegen(
    [string]$Name,
    [string]$Runtime,
    [bool]$UseStandardAbi,
    [bool]$UseUncountedCallback,
    [bool]$ClaimBits
) {
    if ($UseStandardAbi) {
        if ($ClaimBits) {
            Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
        } else {
            $env:DOTNET_GCPath = $ValidationStandaloneGC
        }
        $env:DOTNET_GCWriteBarrierTestClobber = '0'
        if ($UseUncountedCallback) {
            $env:DOTNET_GCWriteBarrierTestUncounted = '1'
        } else {
            $env:DOTNET_GCWriteBarrierTestUncounted = '0'
        }
        $env:DOTNET_GCWriteBarrierTestClaimBits = if ($ClaimBits) { '1' } else { '0' }
    } else {
        Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
        Remove-Item Env:\DOTNET_GCWriteBarrierTestClobber -ErrorAction SilentlyContinue
        Remove-Item Env:\DOTNET_GCWriteBarrierTestUncounted -ErrorAction SilentlyContinue
        Remove-Item Env:\DOTNET_GCWriteBarrierTestClaimBits -ErrorAction SilentlyContinue
    }

    $env:DOTNET_JitDisasm = 'P11.WriteBarrierBenchmarks.WriteBarrierBenchmark:*'
    $env:DOTNET_JitStdOutFile = Join-Path $OutputDirectory "$Name.dasm"
    & $Runtime $assembly --direct
    if ($LASTEXITCODE -ne 0) {
        throw "Codegen capture failed: $Name."
    }
}

try {
    $env:DOTNET_ReadyToRun = '0'
    $env:DOTNET_TieredCompilation = '0'
    $env:DOTNET_gcServer = '0'
    $env:DOTNET_gcConcurrent = '0'
    Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCWriteBarrierTestClobber -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCWriteBarrierTestUncounted -ErrorAction SilentlyContinue

    Run-Benchmark 'stock' $productionRuntime @()
    Run-Benchmark 'standard-empty' $validationRuntime @(
        "DOTNET_GCPath:$ValidationStandaloneGC",
        'DOTNET_GCWriteBarrierTestClobber:0',
        'DOTNET_GCWriteBarrierTestUncounted:1',
        'DOTNET_GCWriteBarrierTestClaimBits:0'
    )
    Run-Benchmark 'standard-counting' $validationRuntime @(
        "DOTNET_GCPath:$ValidationStandaloneGC",
        'DOTNET_GCWriteBarrierTestClobber:0',
        'DOTNET_GCWriteBarrierTestUncounted:0',
        'DOTNET_GCWriteBarrierTestClaimBits:0'
    )
    Run-Benchmark 'slot-fast' $validationRuntime @(
        'DOTNET_GCWriteBarrierTestClobber:0',
        'DOTNET_GCWriteBarrierTestUncounted:0',
        'DOTNET_GCWriteBarrierTestClaimBits:1'
    )

    Capture-Codegen 'stock' $productionRuntime $false $false $false
    Capture-Codegen 'standard-empty' $validationRuntime $true $true $false
    Capture-Codegen 'standard-counting' $validationRuntime $true $false $false
    Capture-Codegen 'slot-fast' $validationRuntime $true $false $true
}
finally {
    Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCWriteBarrierTestClobber -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCWriteBarrierTestUncounted -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_GCWriteBarrierTestClaimBits -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_JitDisasm -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_JitStdOutFile -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_ReadyToRun -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_TieredCompilation -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_gcServer -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_gcConcurrent -ErrorAction SilentlyContinue
}

Write-Host '4/4 write-barrier benchmark configurations passed'
