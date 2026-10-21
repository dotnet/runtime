# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\p15-reference-enumeration-benchmark')
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$source = Join-Path $scriptRoot (
    'reference-benchmark\reference-enumeration-benchmark.cpp')
$project = Join-Path $scriptRoot (
    'reference-benchmark\reference-benchmark.csproj')
$nativeLibrary = Join-Path $OutputDirectory (
    'reference-enumeration-benchmark.dll')
$nativeObject = Join-Path $OutputDirectory (
    'reference-enumeration-benchmark.obj')
$benchmarkArtifacts = Join-Path $OutputDirectory 'BenchmarkDotNet.Artifacts'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$command = @(
    "call `"$initializeVisualStudio`" x64",
    "cl /nologo /std:c++17 /EHsc /LD /O2 /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 /DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$RepositoryRoot\src\native`" /I`"$RepositoryRoot\src\native\inc`" `"$source`" /Fo:`"$nativeObject`" /Fe:`"$nativeLibrary`""
) -join ' && '

& $env:ComSpec /d /s /c $command
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$env:P15_REFERENCE_BENCHMARK_LIBRARY = $nativeLibrary
try {
    & $dotnet run --project $project -c Release -- `
        --filter '*ReferenceEnumerationBenchmarks*' `
        --job short `
        --launchCount 3 `
        --warmupCount 3 `
        --iterationCount 8 `
        --artifacts $benchmarkArtifacts
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $report = Join-Path $benchmarkArtifacts (
        'results\P15.ReferenceEnumerationBenchmarks-report.csv')
    if (-not (Test-Path -LiteralPath $report -PathType Leaf)) {
        throw "Benchmark report was not produced: $report"
    }
    $reportText = Get-Content -LiteralPath $report -Raw
    if (($reportText -match '(?m),NA(?:,|$)') -or
        ($reportText -notmatch 'PerSlotCallback') -or
        ($reportText -notmatch 'ReferenceRanges')) {
        throw "Benchmark report contains no usable measurements: $report"
    }
} finally {
    Remove-Item Env:\P15_REFERENCE_BENCHMARK_LIBRARY `
        -ErrorAction SilentlyContinue
}
