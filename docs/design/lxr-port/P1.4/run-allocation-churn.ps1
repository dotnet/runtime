# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$RuntimeRoot,
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 20)]
    [int]$PairCount = 5,
    [ValidateRange(1, 60)]
    [int]$DurationSeconds = 5
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\p14-allocation-churn'
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$workerProject = Join-Path $RepositoryRoot (
    'docs\design\lxr-port\harness\src\Lxr.Harness.Worker\Lxr.Harness.Worker.csproj')
$workerDirectory = Join-Path $RepositoryRoot (
    'artifacts\lxr-harness\build\bin\Lxr.Harness.Worker\release')
$worker = Join-Path $workerDirectory 'Lxr.Harness.Worker.dll'
$corerun = Join-Path $RuntimeRoot 'corerun.exe'
foreach ($path in @($dotnet, $workerProject, $corerun)) {
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
$rows = [Collections.Generic.List[object]]::new()
$environmentNames = @(
    'CORE_LIBRARIES',
    'DOTNET_GCAllocationNotificationTest',
    'DOTNET_GCAllocationNotificationTestUncounted',
    'DOTNET_ReadyToRun',
    'DOTNET_TieredCompilation',
    'DOTNET_gcConcurrent',
    'DOTNET_gcServer',
    'DOTNET_GCHeapCount'
)

try {
    $env:CORE_LIBRARIES = $workerDirectory
    $env:DOTNET_ReadyToRun = '0'
    $env:DOTNET_TieredCompilation = '0'
    $env:DOTNET_gcConcurrent = '1'

    foreach ($gc in @('wks', 'srv')) {
        if ($gc -eq 'srv') {
            $env:DOTNET_gcServer = '1'
            $env:DOTNET_GCHeapCount = '8'
        } else {
            $env:DOTNET_gcServer = '0'
            Remove-Item Env:\DOTNET_GCHeapCount -ErrorAction SilentlyContinue
        }

        for ($pair = 0; $pair -lt $PairCount; $pair++) {
            $order = if (($pair % 2) -eq 0) {
                @('unregistered', 'registered-empty')
            } else {
                @('registered-empty', 'unregistered')
            }

            for ($orderIndex = 0; $orderIndex -lt $order.Count; $orderIndex++) {
                $variant = $order[$orderIndex]
                if ($variant -eq 'registered-empty') {
                    $env:DOTNET_GCAllocationNotificationTest = '1'
                    $env:DOTNET_GCAllocationNotificationTestUncounted = '1'
                } else {
                    Remove-Item Env:\DOTNET_GCAllocationNotificationTest -ErrorAction SilentlyContinue
                    Remove-Item Env:\DOTNET_GCAllocationNotificationTestUncounted -ErrorAction SilentlyContinue
                }

                $id = "allocation-churn-$gc-$variant-$pair"
                $json = Join-Path $OutputDirectory "$id.json"
                $log = Join-Path $OutputDirectory "$id.log"
                & $corerun $worker `
                    --scenario allocation-churn `
                    --arm $gc `
                    --server-heap-count 8 `
                    --seed (20260901 + $pair) `
                    --workers 8 `
                    --warmup-seconds 1 `
                    --duration-seconds $DurationSeconds `
                    --mode throughput `
                    --output $json *> $log
                $exitCode = $LASTEXITCODE
                $output = Get-Content -LiteralPath $log -Raw
                if (($exitCode -ne 0) -or
                    ($output -notmatch '(?m)^LXR-HARNESS-COMPLETE ') -or
                    -not (Test-Path -LiteralPath $json -PathType Leaf)) {
                    throw "Allocation churn failed: $id, exit $exitCode."
                }

                $report = Get-Content -LiteralPath $json -Raw | ConvertFrom-Json
                $rows.Add([pscustomobject][ordered]@{
                    Pair = "$gc-$pair"
                    Order = $orderIndex
                    Invocation = $pair
                    Seed = $report.seed
                    GC = $gc
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
                    RequestedAllocationNotification = if ($variant -eq 'registered-empty') { 1 } else { 0 }
                    RequestedReadyToRun = 0
                    RequestedTieredCompilation = 0
                    RequestEvidence = 'launcher environment; allocation callback execution proven by the correctness matrix'
                    CoreClrSha256 = $report.runtime.coreClrSha256
                    CoreClrFileVersion = $report.runtime.coreClrFileVersion
                    CompletionMarker = $report.marker
                })
                Write-Host "PASS: $id"
            }
        }
    }

    $rows | Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'allocation-churn-invocations.csv') -NoTypeInformation
} finally {
    foreach ($name in $environmentNames) {
        Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
    }
}

Write-Host "$($rows.Count) allocation-churn invocations passed"
