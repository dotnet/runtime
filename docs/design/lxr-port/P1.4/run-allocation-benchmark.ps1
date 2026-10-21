# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$RuntimeRoot,
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [string]$EvidenceDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 1,
    [ValidateRange(1, 100)]
    [int]$WarmupCount = 3,
    [ValidateRange(1, 100)]
    [int]$IterationCount = 10
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\p14-allocation-benchmark'
}
if (-not $EvidenceDirectory) {
    $EvidenceDirectory = Join-Path $scriptRoot 'raw'
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$project = Join-Path $scriptRoot 'allocation-benchmark\allocation-benchmark.csproj'
$corerun = Join-Path $RuntimeRoot 'corerun.exe'
$hookLibrary = Join-Path $RuntimeRoot 'coreclr.dll'
$runtimeFiles = @(
    $corerun,
    $hookLibrary,
    (Join-Path $RuntimeRoot 'clrgc.dll'),
    (Join-Path $RuntimeRoot 'clrjit.dll'),
    (Join-Path $RuntimeRoot 'System.Private.CoreLib.dll')
)
foreach ($path in @($dotnet, $project) + $runtimeFiles) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file not found: $path"
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$runtimeFiles |
    ForEach-Object {
        $item = Get-Item -LiteralPath $_
        [pscustomobject][ordered]@{
            Name = $item.Name
            Sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
            Length = $item.Length
        }
    } |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'benchmark-runtime-identities.csv') -NoTypeInformation
& $dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$countingObservations = [Collections.Generic.List[object]]::new()
$countingControls = [Collections.Generic.List[object]]::new()
$variants = @(
    @{ Name = 'unregistered'; Environment = @(); ExpectCountOnly = $false },
    @{
        Name = 'registered-empty'
        Environment = @(
            'DOTNET_GCAllocationNotificationTest:1',
            'DOTNET_GCAllocationNotificationTestUncounted:1')
        ExpectCountOnly = $false
    },
    @{
        Name = 'registered-counting'
        Environment = @(
            'DOTNET_GCAllocationNotificationTest:1',
            'DOTNET_GCAllocationNotificationTestCountOnly:1',
            'P14_EXPECT_COUNT_ONLY:1',
            'P14_NATIVE_HOOK_LIBRARY:coreclr.dll')
        ExpectCountOnly = $true
    }
)

$passed = 0
foreach ($gc in @('wks', 'srv')) {
    foreach ($variant in $variants) {
        $name = "$gc-$($variant.Name)"
        $environment = @(
            'DOTNET_ReadyToRun:0',
            'DOTNET_TieredCompilation:0',
            "DOTNET_gcServer:$(if ($gc -eq 'srv') { '1' } else { '0' })"
        ) + $variant.Environment
        $arguments = @(
            'run',
            '--no-build',
            '-c', 'Release',
            '--project', $project,
            '--',
            '--filter', '*',
            '--coreRun', $corerun,
            '--launchCount', $LaunchCount,
            '--warmupCount', $WarmupCount,
            '--iterationCount', $IterationCount,
            '--artifacts', (Join-Path $OutputDirectory $name),
            '--envvars'
        ) + $environment

        $log = Join-Path $OutputDirectory "$name.log"
        & $dotnet @arguments *> $log
        if ($LASTEXITCODE -ne 0) {
            throw "Benchmark failed: $name"
        }
        $expectedBenchmarkMatch = Select-String -LiteralPath $log `
            -Pattern '^// \*\*\*\*\* Found (\d+) benchmark\(s\) in total \*\*\*\*\*$'
        if (@($expectedBenchmarkMatch).Count -ne 1) {
            throw "$name did not report one total benchmark cardinality."
        }
        $expectedBenchmarks = [int]$expectedBenchmarkMatch.Matches[0].Groups[1].Value
        if ($expectedBenchmarks -le 0) {
            throw "$name found no benchmarks."
        }
        $reportRows = @(
            Get-ChildItem -LiteralPath (Join-Path $OutputDirectory $name) `
                -Filter '*-report.csv' -File -Recurse |
                ForEach-Object { Import-Csv -LiteralPath $_.FullName })
        if (($reportRows.Count -ne $expectedBenchmarks) -or
            @($reportRows | Where-Object { -not $_.Mean }).Count -ne 0) {
            throw (
                "$name produced $($reportRows.Count)/$expectedBenchmarks complete reports. " +
                "See $log.")
        }
        if ($variant.ExpectCountOnly) {
            $observations = @(
                Select-String -LiteralPath $log `
                    -Pattern '^P14_COUNT_ONLY_DELTA=(\d+);MINIMUM=(\d+)$')
            $expectedObservations = $reportRows.Count * $LaunchCount
            if ($observations.Count -ne $expectedObservations) {
                throw (
                    "$name recorded $($observations.Count) count-only probes; " +
                    "expected $expectedObservations.")
            }
            foreach ($observation in $observations) {
                $count = [int64]$observation.Matches[0].Groups[1].Value
                $minimum = [int64]$observation.Matches[0].Groups[2].Value
                if ($count -lt $minimum) {
                    throw "$name observed count-only delta $count below $minimum."
                }
                $countingObservations.Add([pscustomobject][ordered]@{
                    GC = $gc
                    Variant = $variant.Name
                    Observation = $countingObservations.Count + 1
                    Count = $count
                    Minimum = $minimum
                    ExpectedObservations = $expectedObservations
                })
            }
            $countingControls.Add([pscustomobject][ordered]@{
                Name = "$name-observability"
                Expected = "$expectedObservations scaled callback deltas"
                Result = 'PASS'
                Evidence = [IO.Path]::GetFileName($log)
            })
        }
        $passed++
        Write-Host "PASS: $name"
    }
}

$negativeName = 'registered-counting-missing-callback-control'
$negativeLog = Join-Path $OutputDirectory "$negativeName.log"
$negativeArguments = @(
    'run',
    '--no-build',
    '-c', 'Release',
    '--project', $project,
    '--',
    '--filter', '*FixedAllocationBenchmarks.Object*',
    '--coreRun', $corerun,
    '--launchCount', '1',
    '--warmupCount', '1',
    '--iterationCount', '1',
    '--artifacts', (Join-Path $OutputDirectory $negativeName),
    '--envvars',
    'DOTNET_ReadyToRun:0',
    'DOTNET_TieredCompilation:0',
    'DOTNET_gcServer:0',
    'DOTNET_GCAllocationNotificationTest:1',
    'DOTNET_GCAllocationNotificationTestCountOnly:1',
    'DOTNET_GCAllocationNotificationTestUncounted:1',
    'P14_EXPECT_COUNT_ONLY:1',
    'P14_NATIVE_HOOK_LIBRARY:coreclr.dll'
)
& $dotnet @negativeArguments *> $negativeLog
$negativeOutput = Get-Content -LiteralPath $negativeLog -Raw
if (($negativeOutput -notmatch 'Count-only allocation notification callback delivered') -or
    ($negativeOutput -match 'P14_COUNT_ONLY_DELTA=')) {
    throw "Missing-callback negative control did not fail observably. See $negativeLog."
}
$countingControls.Add([pscustomobject][ordered]@{
    Name = $negativeName
    Expected = 'missing callback rejected'
    Result = 'PASS'
    Evidence = [IO.Path]::GetFileName($negativeLog)
})

$countingObservations |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'counting-callback-observations.csv') -NoTypeInformation
$countingControls |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'counting-callback-controls.csv') -NoTypeInformation

Write-Host "PASS: $negativeName"
Write-Host "$passed/6 allocation benchmark configurations and 1 negative control passed"

$collector = Join-Path $scriptRoot 'collect-allocation-benchmark.ps1'
& pwsh -NoProfile -File $collector `
    -InputDirectory $OutputDirectory `
    -OutputDirectory $EvidenceDirectory
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to collect allocation benchmark evidence.'
}
