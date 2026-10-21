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
    [int]$IterationCount = 20
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
        'artifacts\P2.5\benchmark\' + [guid]::NewGuid().ToString('N'))
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$nativeRoot = Join-Path $RepositoryRoot 'src\native'
$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$project = Join-Path $scriptRoot 'large-object-space-benchmark\large-object-space-benchmark.csproj'
$platform = Join-Path $scriptRoot 'large-object-space-test-platform.cpp'
$nativeLibrary = Join-Path $OutputDirectory 'large-object-space-benchmark-native.dll'
$results = Join-Path $OutputDirectory 'BenchmarkDotNet'

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$nativeCommand = @(
    "call `"$initializeVisualStudio`" x64",
    "cd /d `"$OutputDirectory`"",
    "cl /nologo /LD /std:c++17 /EHsc /O2 /W4 /WX /wd4100 /wd4324 /DBUILD_AS_STANDALONE /DLXR_SIDE_METADATA_TEST_PLATFORM /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 /DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$nativeRoot`" /I`"$nativeRoot\inc`" `"$scriptRoot\large-object-space-benchmark-native.cpp`" `"$platform`" `"$gcRoot\side_metadata.cpp`" `"$gcRoot\immix_block.cpp`" `"$gcRoot\large_object_space.cpp`" /Fe:`"$nativeLibrary`""
) -join ' && '

$nativeLog = Join-Path $OutputDirectory 'native-build.log'
$savedPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & $env:ComSpec /d /s /c $nativeCommand *> $nativeLog
    $nativeExit = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedPreference
}
if ($nativeExit -ne 0) {
    throw "Native benchmark build failed. See $nativeLog."
}

$managedLog = Join-Path $OutputDirectory 'managed-build.log'
try {
    $ErrorActionPreference = 'Continue'
    & $dotnet build $project -c Release --nologo *> $managedLog
    $managedExit = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedPreference
}
if ($managedExit -ne 0) {
    throw "Managed benchmark build failed. See $managedLog."
}

$savedPath = $env:PATH
$savedRoot = $env:DOTNET_ROOT
$savedNative = $env:P25_NATIVE_BENCHMARK
$savedLocation = Get-Location
try {
    Set-Location -LiteralPath (Split-Path -Parent $project)
    $env:PATH = "$(Join-Path $RepositoryRoot '.dotnet');$env:PATH"
    $env:DOTNET_ROOT = Join-Path $RepositoryRoot '.dotnet'
    $env:P25_NATIVE_BENCHMARK = $nativeLibrary
    $benchmarkLog = Join-Path $OutputDirectory 'benchmark.log'
    try {
        $ErrorActionPreference = 'Continue'
        & $dotnet run --no-build -c Release --project $project -- `
            --filter '*' `
            --launchCount $LaunchCount `
            --warmupCount $WarmupCount `
            --iterationCount $IterationCount `
            --artifacts $results *> $benchmarkLog
        $benchmarkExit = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedPreference
    }
    if ($benchmarkExit -ne 0) {
        throw "Benchmark run failed. See $benchmarkLog."
    }
} finally {
    Set-Location -LiteralPath $savedLocation.ProviderPath
    $env:PATH = $savedPath
    $env:DOTNET_ROOT = $savedRoot
    if ($null -eq $savedNative) {
        Remove-Item Env:\P25_NATIVE_BENCHMARK -ErrorAction SilentlyContinue
    } else {
        $env:P25_NATIVE_BENCHMARK = $savedNative
    }
}

$report = Get-ChildItem -LiteralPath (Join-Path $results 'results') `
    -Filter '*-report.csv' -File -Recurse
if ($report.Count -ne 1) {
    throw "Expected one benchmark report, found $($report.Count)."
}
$rows = @(Import-Csv $report.FullName)
if (($rows.Count -ne 21) -or (@($rows | Where-Object { -not $_.Mean }).Count -ne 0)) {
    throw "Benchmark report has $($rows.Count)/21 complete rows."
}

function Convert-ToNanoseconds([string]$value) {
    $value = $value.Replace(',', '')
    $microseconds = ([char]0x03BC) + 's'
    $match = [regex]::Match(
        $value,
        '^([\d.]+)\s*(ns|' + [regex]::Escape($microseconds) + '|ms|s)$')
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

function Get-MeanNanoseconds([string]$method, [int]$workerCount) {
    $match = @($rows | Where-Object {
        ($_.Method -eq $method) -and
        ([int]$_.WorkerCount -eq $workerCount)
    })
    if ($match.Count -ne 1) {
        throw "Benchmark row differs for $method/$workerCount."
    }
    return Convert-ToNanoseconds $match[0].Mean
}

$spec = Get-Content (Join-Path $scriptRoot 'mechanism-spec.json') -Raw | ConvertFrom-Json
$controls = [Collections.Generic.List[object]]::new()
foreach ($workerCount in @(1, 4, 16)) {
    $aa =
        (Get-MeanNanoseconds 'GeometryControl' $workerCount) /
        (Get-MeanNanoseconds 'Geometry' $workerCount)
    $extra =
        (Get-MeanNanoseconds 'ExtraReuseUpdates' $workerCount) /
        (Get-MeanNanoseconds 'ReuseCounterAllocation' $workerCount)
    $injected =
        (Get-MeanNanoseconds 'InjectedOwnerDelay' $workerCount) /
        (Get-MeanNanoseconds 'RecycledAllocateRelease' $workerCount)
    if (($aa -lt [double]$spec.benchmark.aaMinimum) -or
        ($aa -gt [double]$spec.benchmark.aaMaximum)) {
        throw "A/A ratio $aa is outside the declared interval for $workerCount workers."
    }
    if ($extra -lt [double]$spec.benchmark.extraUpdateMinimum) {
        throw "Extra-update ratio $extra is below its declared minimum for $workerCount workers."
    }
    if ($injected -lt [double]$spec.benchmark.injectedCostMinimum) {
        throw "Injected-cost ratio $injected is below its declared minimum for $workerCount workers."
    }
    $controls.Add([pscustomobject][ordered]@{
        worker_count = $workerCount
        aa_ratio = $aa
        extra_update_ratio = $extra
        injected_cost_ratio = $injected
        result = 'PASS'
    })
}

Copy-Item -LiteralPath $report.FullName `
    -Destination (Join-Path $OutputDirectory 'benchmark-raw.csv')
$controls | Export-Csv (Join-Path $OutputDirectory 'benchmark-controls.csv') -NoTypeInformation
[pscustomobject][ordered]@{
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    operations_per_invoke = $spec.benchmark.operationsPerInvoke
    rows = $rows.Count
    native_sha256 = (Get-FileHash $nativeLibrary -Algorithm SHA256).Hash
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'benchmark-identity.csv') -NoTypeInformation

Write-Host "PASS: $($rows.Count) large object space benchmark rows"
