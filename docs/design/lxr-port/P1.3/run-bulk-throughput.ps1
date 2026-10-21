# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$StockRuntimeRoot,
    [Parameter(Mandatory)]
    [string]$FamilyRuntimeRoot,
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\p13-bulk-throughput'
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$workerProject = Join-Path $RepositoryRoot (
    'docs\design\lxr-port\harness\src\Lxr.Harness.Worker\Lxr.Harness.Worker.csproj')
$workerDirectory = Join-Path $RepositoryRoot (
    'artifacts\lxr-harness\build\bin\Lxr.Harness.Worker\release')
$worker = Join-Path $workerDirectory 'Lxr.Harness.Worker.dll'
$stockRuntime = Join-Path $StockRuntimeRoot 'corerun.exe'
$familyRuntime = Join-Path $FamilyRuntimeRoot 'corerun.exe'

foreach ($path in @($dotnet, $workerProject, $stockRuntime, $familyRuntime)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

& $dotnet build $workerProject -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
if (-not (Test-Path -LiteralPath $worker -PathType Leaf)) {
    throw "Harness worker was not produced: $worker"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$environmentNames = @(
    'CORE_LIBRARIES',
    'DOTNET_ReadyToRun',
    'DOTNET_TieredCompilation',
    'DOTNET_gcConcurrent',
    'DOTNET_gcServer',
    'DOTNET_GCHeapCount',
    'DOTNET_GCWriteBarrierTestShape',
    'DOTNET_GCWriteBarrierTestClobber',
    'DOTNET_GCWriteBarrierTestUncounted',
    'DOTNET_GCWriteBarrierTestClaimBits'
)
$passed = 0
$invocationRows = [Collections.Generic.List[object]]::new()

try {
    $env:CORE_LIBRARIES = $workerDirectory
    $env:DOTNET_ReadyToRun = '0'
    $env:DOTNET_TieredCompilation = '0'
    $env:DOTNET_gcConcurrent = '1'

    foreach ($gcMode in @('wks', 'srv')) {
        if ($gcMode -eq 'srv') {
            $env:DOTNET_gcServer = '1'
            $env:DOTNET_GCHeapCount = '8'
        } else {
            $env:DOTNET_gcServer = '0'
            Remove-Item Env:\DOTNET_GCHeapCount -ErrorAction SilentlyContinue
        }

        for ($invocation = 0; $invocation -lt 5; $invocation++) {
            $order = if (($invocation % 2) -eq 0) {
                @('stock', 'family')
            } else {
                @('family', 'stock')
            }

            for ($orderIndex = 0; $orderIndex -lt $order.Count; $orderIndex++) {
                $variant = $order[$orderIndex]
                if ($variant -eq 'family') {
                    $runtime = $familyRuntime
                    $env:DOTNET_GCWriteBarrierTestShape = '1'
                    $env:DOTNET_GCWriteBarrierTestClobber = '0'
                    $env:DOTNET_GCWriteBarrierTestUncounted = '0'
                    $env:DOTNET_GCWriteBarrierTestClaimBits = '1'
                } else {
                    $runtime = $stockRuntime
                    foreach ($name in @(
                        'DOTNET_GCWriteBarrierTestShape',
                        'DOTNET_GCWriteBarrierTestClobber',
                        'DOTNET_GCWriteBarrierTestUncounted',
                        'DOTNET_GCWriteBarrierTestClaimBits'
                    )) {
                        Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
                    }
                }

                $id = "low-allocation-compute-$gcMode-$variant-$invocation"
                $json = Join-Path $OutputDirectory "$id.json"
                $log = Join-Path $OutputDirectory "$id.log"
                $arguments = @(
                    $worker,
                    '--scenario', 'low-allocation-compute',
                    '--arm', $gcMode,
                    '--server-heap-count', '8',
                    '--seed', (20260830 + $invocation).ToString(),
                    '--workers', '8',
                    '--warmup-seconds', '1',
                    '--duration-seconds', '5',
                    '--mode', 'throughput',
                    '--output', $json
                )

                & $runtime @arguments *> $log
                $exitCode = $LASTEXITCODE
                $output = Get-Content -LiteralPath $log -Raw
                if (($exitCode -ne 0) -or
                    ($output -notmatch '(?m)^LXR-HARNESS-COMPLETE ') -or
                    -not (Test-Path -LiteralPath $json -PathType Leaf)) {
                    throw "Throughput run failed: $id, exit $exitCode."
                }

                $report = Get-Content -LiteralPath $json -Raw | ConvertFrom-Json
                if (($report.gc.gen0Collections -ne 0) -or
                    ($report.gc.gen1Collections -ne 0) -or
                    ($report.gc.gen2Collections -ne 0)) {
                    throw "Claimed-bit throughput run collected: $id."
                }

                $invocationRows.Add([pscustomobject][ordered]@{
                    Pair = "$gcMode-$invocation"
                    Order = $orderIndex
                    Invocation = $invocation
                    Seed = $report.seed
                    GC = $gcMode
                    Arm = $report.arm
                    Variant = $variant
                    Workers = $report.workerCount
                    OpsPerSecond = ([double]$report.metrics.operationsPerSecond).ToString(
                        'F6',
                        [Globalization.CultureInfo]::InvariantCulture)
                    Gen0 = $report.gc.gen0Collections
                    Gen1 = $report.gc.gen1Collections
                    Gen2 = $report.gc.gen2Collections
                    ObservedServerGC = $report.observedGcConfig.ServerGC
                    ObservedConcurrentGC = $report.observedGcConfig.ConcurrentGC
                    CollectorConfirmed = $report.collectorConfirmed
                    ReportValid = $report.valid
                    VerificationSuccess = $report.verificationSuccess
                    RequestedClaimBits = if ($variant -eq 'family') { 1 } else { 0 }
                    RequestedReadyToRun = 0
                    RequestedTieredCompilation = 0
                    RequestEvidence = 'launcher environment; not independently confirmed by the worker'
                    CoreClrSha256 = $report.runtime.coreClrSha256
                    CoreClrFileVersion = $report.runtime.coreClrFileVersion
                    Processor = $report.machine.processorName
                    LogicalCores = $report.machine.logicalCores
                    OS = $report.machine.osDescription
                    CompletionMarker = $report.marker
                })
                $passed++
                Write-Host "PASS: $id"
            }
        }
    }

    $invocationRows |
        Export-Csv -LiteralPath (Join-Path $OutputDirectory 'bulk-throughput-invocations.csv') -NoTypeInformation
} finally {
    foreach ($name in $environmentNames) {
        Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
    }
}

Write-Host "$passed multi-thread throughput runs passed"
