# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(1, 100)]
    [int]$WarmupCount = 8,
    [ValidateRange(1, 100)]
    [int]$IterationCount = 20,
    [switch]$SkipControlThresholds
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.4\benchmark\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$nativeRoot = Join-Path $RepositoryRoot 'src\native'
$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$project = Join-Path $scriptRoot (
    'immix-allocation-benchmark\immix-allocation-benchmark.csproj')
$evidenceManifestPath = Join-Path $scriptRoot 'evidence-manifest.json'
$mechanismSpecPath = Join-Path $scriptRoot 'mechanism-spec.json'
$platform = Join-Path $scriptRoot '..\P2.1\side-metadata-test-platform.cpp'
$nativeLibrary = Join-Path $OutputDirectory 'immix-allocation-benchmark-native.dll'
$nativeLog = Join-Path $OutputDirectory 'native-build.log'
$managedBuildLog = Join-Path $OutputDirectory 'managed-build.log'
$benchmarkLog = Join-Path $OutputDirectory 'benchmark.log'
$results = Join-Path $OutputDirectory 'BenchmarkDotNet'
$sources = @(
    (Join-Path $scriptRoot 'immix-allocation-benchmark-native.cpp'),
    $platform,
    (Join-Path $gcRoot 'side_metadata.cpp'),
    (Join-Path $gcRoot 'immix_block.cpp'),
    (Join-Path $gcRoot 'immix_allocation.cpp')
)
foreach ($path in @(
        $initializeVisualStudio,
        $dotnet,
        $project,
        $evidenceManifestPath,
        $mechanismSpecPath) + $sources) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required benchmark input is missing: '$path'."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$quotedSources = $sources | ForEach-Object { "`"$_`"" }
$nativeCommand = @(
    "call `"$initializeVisualStudio`" x64",
    "cd /d `"$OutputDirectory`"",
    (
        'cl /nologo /LD /std:c++17 /EHsc /O2 /W4 /WX /wd4100 /wd4324 ' +
        '/DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 ' +
        '/DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT ' +
        "/I`"$gcRoot`" /I`"$gcRoot\env`" " +
        "/I`"$nativeRoot`" /I`"$nativeRoot\inc`" " +
        ($quotedSources -join ' ') +
        " /Fe:`"$nativeLibrary`""
    )
) -join ' && '

$savedErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & $env:ComSpec /d /s /c $nativeCommand *> $nativeLog
    $nativeExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
if ($nativeExitCode -ne 0) {
    throw "Native benchmark build failed. See '$nativeLog'."
}

try {
    $ErrorActionPreference = 'Continue'
    & $dotnet build $project -c Release --nologo *> $managedBuildLog
    $managedBuildExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
if ($managedBuildExitCode -ne 0) {
    throw "Managed benchmark build failed. See '$managedBuildLog'."
}

$savedPath = $env:PATH
$savedRoot = $env:DOTNET_ROOT
$savedNativeBenchmark = $env:P24_NATIVE_BENCHMARK
$savedLocation = Get-Location
try {
    Set-Location -LiteralPath (Split-Path -Parent $project)
    $env:PATH = "$(Join-Path $RepositoryRoot '.dotnet');$env:PATH"
    $env:DOTNET_ROOT = Join-Path $RepositoryRoot '.dotnet'
    $env:P24_NATIVE_BENCHMARK = $nativeLibrary
    try {
        $ErrorActionPreference = 'Continue'
        & $dotnet run --no-build -c Release --project $project -- `
            --filter '*ImmixAllocationBenchmarks*' `
            --launchCount $LaunchCount `
            --warmupCount $WarmupCount `
            --iterationCount $IterationCount `
            --artifacts $results *> $benchmarkLog
        $benchmarkExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    if ($benchmarkExitCode -ne 0) {
        throw "Benchmark run failed. See '$benchmarkLog'."
    }
} finally {
    Set-Location -LiteralPath $savedLocation.ProviderPath
    $env:PATH = $savedPath
    $env:DOTNET_ROOT = $savedRoot
    if ($null -eq $savedNativeBenchmark) {
        Remove-Item Env:\P24_NATIVE_BENCHMARK -ErrorAction SilentlyContinue
    } else {
        $env:P24_NATIVE_BENCHMARK = $savedNativeBenchmark
    }
}

if (Select-String -LiteralPath $benchmarkLog `
        -Pattern 'There are not any results runs|Benchmarks with issues') {
    throw "Benchmark run produced incomplete results. See '$benchmarkLog'."
}

$evidenceManifest = Get-Content -LiteralPath $evidenceManifestPath -Raw |
    ConvertFrom-Json
$mechanismSpec = Get-Content -LiteralPath $mechanismSpecPath -Raw |
    ConvertFrom-Json
$benchmarkSpec = $evidenceManifest.benchmark
$benchmarkMethods = @($benchmarkSpec.methods)
$workerCounts = @($benchmarkSpec.workerCounts)
$methodCount = $benchmarkMethods.Count
$expectedRows = [int]$benchmarkSpec.rows
if ($expectedRows -ne ($methodCount * $workerCounts.Count)) {
    throw 'Benchmark row count is inconsistent with the evidence manifest.'
}
$report = Get-ChildItem -LiteralPath (Join-Path $results 'results') `
    -Filter '*-report.csv' -File -Recurse
if ($report.Count -ne 1) {
    throw "Expected one benchmark report, found $($report.Count)."
}
$rows = @(Import-Csv $report.FullName)
if (($rows.Count -ne $expectedRows) -or
    (@($rows | Where-Object { -not $_.Mean }).Count -ne 0)) {
    throw "Benchmark report has $($rows.Count)/$expectedRows complete rows."
}
foreach ($workerCount in $workerCounts) {
    foreach ($method in $benchmarkMethods) {
        $matchingRows = @($rows | Where-Object {
            ($_.Method -eq $method) -and
            ([int]$_.WorkerCount -eq $workerCount)
        })
        if ($matchingRows.Count -ne 1) {
            throw "Benchmark row differs for $method/$workerCount."
        }
    }
}

function Convert-ToNanoseconds([string]$value) {
    $value = $value.Replace(',', '')
    $microseconds = ([char]0x03BC) + 's'
    $pattern = '^([\d.]+)\s*(ns|' + [regex]::Escape($microseconds) + '|ms|s)$'
    $match = [regex]::Match($value, $pattern)
    if (-not $match.Success) {
        throw "Unsupported benchmark time '$value'."
    }
    $number = [double]$match.Groups[1].Value
    switch ($match.Groups[2].Value) {
        'ns' { return $number }
        $microseconds { return $number * 1000 }
        'ms' { return $number * 1000 * 1000 }
        's' { return $number * 1000 * 1000 * 1000 }
    }
}

$checks = [Collections.Generic.List[object]]::new()
foreach ($workerCount in $workerCounts) {
    $values = @{}
    foreach ($method in @(
        'Fresh',
        'FreshControl',
        'Recycled',
        'RecycledControl',
        'HoleScan',
        'ExtraScanSensitivity',
        'InjectedOwnerDelay')) {
        $row = @($rows | Where-Object {
            ($_.Method -eq $method) -and
            ([int]$_.WorkerCount -eq $workerCount)
        })
        if ($row.Count -ne 1) {
            throw "Benchmark row differs for $method/$workerCount."
        }
        $values[$method] = Convert-ToNanoseconds $row[0].Mean
    }

    $freshAa = $values.FreshControl / $values.Fresh
    $recycledAa = $values.RecycledControl / $values.Recycled
    $extraScan = $values.ExtraScanSensitivity / $values.HoleScan
    $ownerDelay = $values.InjectedOwnerDelay / $values.Recycled
    if (-not $SkipControlThresholds -and
        (($freshAa -lt [double]$benchmarkSpec.aaMinimum) -or
         ($freshAa -gt [double]$benchmarkSpec.aaMaximum))) {
        throw "Fresh A/A ratio $freshAa is outside the permitted range."
    }
    if (-not $SkipControlThresholds -and
        (($recycledAa -lt [double]$benchmarkSpec.aaMinimum) -or
         ($recycledAa -gt [double]$benchmarkSpec.aaMaximum))) {
        throw "Recycled A/A ratio $recycledAa is outside the permitted range."
    }
    if (-not $SkipControlThresholds -and
        ($extraScan -lt [double]$benchmarkSpec.extraScanMinimum)) {
        throw "Extra-scan ratio $extraScan is below the required minimum."
    }
    if (-not $SkipControlThresholds -and
        ($ownerDelay -lt [double]$benchmarkSpec.ownerDelayMinimum)) {
        throw "Owner-delay ratio $ownerDelay is below the required minimum."
    }

    $checks.Add([pscustomobject][ordered]@{
        worker_count = $workerCount
        fresh_aa_ratio = $freshAa
        recycled_aa_ratio = $recycledAa
        extra_scan_ratio = $extraScan
        owner_delay_ratio = $ownerDelay
        result = 'PASS'
    })
}

Copy-Item -LiteralPath $report.FullName `
    -Destination (Join-Path $OutputDirectory 'benchmark-raw.csv')
$checks | Export-Csv `
    (Join-Path $OutputDirectory 'benchmark-controls.csv') `
    -NoTypeInformation
[pscustomobject][ordered]@{
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    operations_per_invoke = [int]$mechanismSpec.benchmark.operationsPerInvoke
    methods = $methodCount
    worker_counts = ($workerCounts -join ';')
    rows = $rows.Count
    recycled_cache_percent = 90
    controls_enforced = -not $SkipControlThresholds
    processor = $env:PROCESSOR_IDENTIFIER
    logical_processors = [Environment]::ProcessorCount
    os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    launcher_framework = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    benchmark_runtime = (($rows.Runtime | Sort-Object -Unique) -join ';')
    native_sha256 = (Get-FileHash $nativeLibrary -Algorithm SHA256).Hash
    result = 'PASS'
} | Export-Csv `
    (Join-Path $OutputDirectory 'benchmark-identity.csv') `
    -NoTypeInformation

Write-Host "PASS: $($rows.Count) benchmark rows and $($checks.Count) controls"
Write-Host "Output: $OutputDirectory"
