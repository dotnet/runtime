# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Builds and runs the P2.3 reference-count BenchmarkDotNet harness.

.DESCRIPTION
Compiles reference-count-benchmark-native.cpp (together with the exact
product sources src/coreclr/gc/side_metadata.cpp, immix_block.cpp, and
reference_count.cpp, plus the P2.1 test platform shim) into a standalone
native DLL, builds the managed reference-count-benchmark BenchmarkDotNet
project against the repository's own .dotnet SDK, runs it, and records
source/native/managed identity alongside the raw results.

This harness benchmarks the ReferenceCountTable API in isolation. It does
not allocate real CoreCLR objects and does not exercise LXR collection,
tracing, or block reclamation.

.PARAMETER Quick
Runs a fast validation pass using BenchmarkDotNet's built-in "Dry" job
(minimal warmup/iterations) instead of -LaunchCount/-WarmupCount/
-IterationCount. Intended to validate that the harness builds and executes
end to end; the resulting timings are not statistically meaningful, so the
A/A and injected-mode ratio checks are skipped in this mode.

.PARAMETER CoreRoot
Optional path to a Core_Root test layout. This harness does not execute
against a full CoreCLR test layout (it only runs the native DLL under the
repository's .dotnet SDK), so CoreRoot is not required; when supplied, the
identity of corerun.exe (or corerun) found there is recorded alongside the
managed runtime identity purely for provenance.
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [string]$CoreRoot,
    [switch]$Quick,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(1, 100)]
    [int]$WarmupCount = 8,
    [ValidateRange(1, 100)]
    [int]$IterationCount = 20
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
$savedLocation = Get-Location
try {
    if (-not $RepositoryRoot) {
        $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
    } else {
        $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
    }
    if (-not $OutputDirectory) {
        $OutputDirectory = Join-Path $RepositoryRoot (
            'artifacts\P2.3\benchmark\' + [guid]::NewGuid().ToString('N'))
    } else {
        $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
    }
    if (Test-Path -LiteralPath $OutputDirectory) {
        throw "Output directory already exists: $OutputDirectory"
    }

    $resolvedCoreRoot = $null
    if ($CoreRoot) {
        $resolvedCoreRoot = (Resolve-Path -LiteralPath $CoreRoot).ProviderPath
        if (-not (Test-Path -LiteralPath $resolvedCoreRoot -PathType Container)) {
            throw "CoreRoot is not a directory: $resolvedCoreRoot"
        }
    }

    $gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
    $nativeRoot = Join-Path $RepositoryRoot 'src\native'
    $dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
    $initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
    $project = Join-Path $scriptRoot 'reference-count-benchmark\reference-count-benchmark.csproj'
    $native = Join-Path $scriptRoot 'reference-count-benchmark-native.cpp'
    $platform = Join-Path $scriptRoot '..\P2.1\side-metadata-test-platform.cpp'
    $sideMetadata = Join-Path $gcRoot 'side_metadata.cpp'
    $immixBlock = Join-Path $gcRoot 'immix_block.cpp'
    $referenceCount = Join-Path $gcRoot 'reference_count.cpp'
    $nativeLibrary = Join-Path $OutputDirectory 'reference-count-benchmark-native.dll'
    $nativeLog = Join-Path $OutputDirectory 'native-build.log'
    $managedBuildLog = Join-Path $OutputDirectory 'managed-build.log'
    $benchmarkLog = Join-Path $OutputDirectory 'benchmark.log'
    $results = Join-Path $OutputDirectory 'BenchmarkDotNet'

    $requiredInputs = @(
        $dotnet,
        $initializeVisualStudio,
        $project,
        $native,
        $platform,
        $sideMetadata,
        $immixBlock,
        $referenceCount
    )
    foreach ($inputPath in $requiredInputs) {
        if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) {
            throw "Required benchmark input is missing: $inputPath"
        }
    }

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

    $sourceHashes = [Collections.Generic.List[object]]::new()
    foreach ($sourcePath in @($native, $platform, $sideMetadata, $immixBlock, $referenceCount)) {
        $sourceHashes.Add([pscustomobject][ordered]@{
            file = $sourcePath
            sha256 = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
        })
    }

    $nativeCommand = @(
        "call `"$initializeVisualStudio`" x64",
        "cd /d `"$OutputDirectory`"",
        "cl /nologo /LD /std:c++17 /EHsc /O2 /W4 /WX /wd4100 /wd4324 /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 /DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$nativeRoot`" /I`"$nativeRoot\inc`" `"$native`" `"$platform`" `"$sideMetadata`" `"$immixBlock`" `"$referenceCount`" /Fe:`"$nativeLibrary`""
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
        throw "Native benchmark build failed. See $nativeLog."
    }
    if (-not (Test-Path -LiteralPath $nativeLibrary -PathType Leaf)) {
        throw "Native benchmark build did not produce $nativeLibrary. See $nativeLog."
    }

    try {
        $ErrorActionPreference = 'Continue'
        & $dotnet build $project -c Release --nologo *> $managedBuildLog
        $managedBuildExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    if ($managedBuildExitCode -ne 0) {
        throw "Managed benchmark build failed. See $managedBuildLog."
    }

    # InvocationCount and UnrollFactor are both one so every IterationSetup
    # precedes exactly one measured native batch. They are passed here rather
    # than baked into an in-code Job because layering an in-code Job on top of
    # a CLI "--job" selection produces two jobs and silently doubles every run.
    $benchmarkArgs = @(
        '--filter', '*',
        '--artifacts', $results,
        '--invocationCount', '1',
        '--unrollFactor', '1')
    if ($Quick) {
        $benchmarkArgs += @('--job', 'Dry')
    } else {
        $benchmarkArgs += @(
            '--launchCount', $LaunchCount,
            '--warmupCount', $WarmupCount,
            '--iterationCount', $IterationCount)
    }

    $savedPath = $env:PATH
    $savedRoot = $env:DOTNET_ROOT
    $savedNativeBenchmark = $env:P23_NATIVE_BENCHMARK
    try {
        Set-Location -LiteralPath (Split-Path -Parent $project)
        $env:PATH = "$(Join-Path $RepositoryRoot '.dotnet');$env:PATH"
        $env:DOTNET_ROOT = Join-Path $RepositoryRoot '.dotnet'
        $env:P23_NATIVE_BENCHMARK = $nativeLibrary
        try {
            $ErrorActionPreference = 'Continue'
            & $dotnet run --no-build -c Release --project $project -- @benchmarkArgs *> $benchmarkLog
            $benchmarkExitCode = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = $savedErrorActionPreference
        }
        if ($benchmarkExitCode -ne 0) {
            throw "Benchmark run failed. See $benchmarkLog."
        }
    } finally {
        Set-Location -LiteralPath (Split-Path -Parent $project)
        $env:PATH = $savedPath
        $env:DOTNET_ROOT = $savedRoot
        if ($null -eq $savedNativeBenchmark) {
            Remove-Item Env:\P23_NATIVE_BENCHMARK -ErrorAction SilentlyContinue
        } else {
            $env:P23_NATIVE_BENCHMARK = $savedNativeBenchmark
        }
    }

    if (Select-String -LiteralPath $benchmarkLog -Pattern 'There are not any results runs|Benchmarks with issues') {
        throw "Benchmark run produced incomplete results. See $benchmarkLog."
    }

    $programCs = Join-Path $scriptRoot 'reference-count-benchmark\Program.cs'
    $widthClassLine = (Select-String -LiteralPath $programCs `
        -Pattern 'class ReferenceCountWidthBenchmarks').LineNumber | Select-Object -First 1
    $contentionClassLine = (Select-String -LiteralPath $programCs `
        -Pattern 'class ReferenceCountContentionBenchmarks').LineNumber | Select-Object -First 1
    if ((-not $widthClassLine) -or (-not $contentionClassLine)) {
        throw 'Could not locate benchmark class boundaries in Program.cs.'
    }
    $benchmarkLineNumbers = @(
        Select-String -LiteralPath $programCs -Pattern '^\s*\[Benchmark').LineNumber
    $widthMethodCount = @($benchmarkLineNumbers | Where-Object {
        ($_ -gt $widthClassLine) -and ($_ -lt $contentionClassLine)
    }).Count
    $contentionMethodCount = @($benchmarkLineNumbers | Where-Object {
        $_ -gt $contentionClassLine
    }).Count
    $expectedRows = ($widthMethodCount * 3) + ($contentionMethodCount * 3)

    $report = Get-ChildItem -LiteralPath (Join-Path $results 'results') `
        -Filter '*-report.csv' -File -Recurse
    if ($report.Count -ne 2) {
        throw "Expected two benchmark reports (one per benchmark class), found $($report.Count)."
    }
    $rows = @($report | ForEach-Object { Import-Csv $_.FullName })
    if ((-not $Quick) -and
        (($rows.Count -ne $expectedRows) -or (@($rows | Where-Object { -not $_.Mean }).Count -ne 0))) {
        throw "Benchmark reports have $($rows.Count)/$expectedRows complete rows."
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
    if (-not $Quick) {
        foreach ($workerCount in @(1, 4, 16)) {
            $sameSlot = @($rows | Where-Object {
                ($_.Method -eq 'SameSlotContention') -and ([int]$_.WorkerCount -eq $workerCount)
            })
            $control = @($rows | Where-Object {
                ($_.Method -eq 'SameSlotContentionControl') -and ([int]$_.WorkerCount -eq $workerCount)
            })
            $extraCas = @($rows | Where-Object {
                ($_.Method -eq 'ExtraCasContention') -and ([int]$_.WorkerCount -eq $workerCount)
            })
            $delay = @($rows | Where-Object {
                ($_.Method -eq 'DelayContention') -and ([int]$_.WorkerCount -eq $workerCount)
            })
            if (($sameSlot.Count -ne 1) -or ($control.Count -ne 1) -or
                ($extraCas.Count -ne 1) -or ($delay.Count -ne 1)) {
                throw "Contention benchmark controls are incomplete for worker count $workerCount."
            }

            $aaRatio =
                (Convert-ToNanoseconds $control[0].Mean) / (Convert-ToNanoseconds $sameSlot[0].Mean)
            $extraCasRatio =
                (Convert-ToNanoseconds $extraCas[0].Mean) / (Convert-ToNanoseconds $sameSlot[0].Mean)
            $delayRatio =
                (Convert-ToNanoseconds $delay[0].Mean) / (Convert-ToNanoseconds $sameSlot[0].Mean)
            if (($aaRatio -lt 0.90) -or ($aaRatio -gt 1.10)) {
                throw "A/A ratio $aaRatio is outside [0.90, 1.10] for $workerCount workers."
            }
            if ($extraCasRatio -lt 1.5) {
                throw "Extra-CAS ratio $extraCasRatio is below 1.5 for $workerCount workers."
            }
            if ($delayRatio -lt 1.25) {
                throw "Injected-delay ratio $delayRatio is below 1.25 for $workerCount workers."
            }

            $checks.Add([pscustomobject][ordered]@{
                worker_count = $workerCount
                aa_ratio = $aaRatio
                extra_cas_ratio = $extraCasRatio
                delay_ratio = $delayRatio
                result = 'PASS'
            })
        }
    }

    $dotnetVersionOutput = & $dotnet --version
    $dotnetHash = (Get-FileHash -LiteralPath $dotnet -Algorithm SHA256).Hash
    $corerunIdentity = $null
    if ($resolvedCoreRoot) {
        $corerun = Get-ChildItem -LiteralPath $resolvedCoreRoot -Filter 'corerun*' -File -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($corerun) {
            $corerunIdentity = [pscustomobject][ordered]@{
                path = $corerun.FullName
                sha256 = (Get-FileHash -LiteralPath $corerun.FullName -Algorithm SHA256).Hash
            }
        }
    }

    foreach ($index in 0..($report.Count - 1)) {
        Copy-Item -LiteralPath $report[$index].FullName `
            -Destination (Join-Path $OutputDirectory "benchmark-raw-$index.csv")
    }
    $sourceHashes | Export-Csv (Join-Path $OutputDirectory 'source-manifest.csv') -NoTypeInformation
    if ($checks.Count -gt 0) {
        $checks | Export-Csv (Join-Path $OutputDirectory 'benchmark-controls.csv') -NoTypeInformation
    }
    [pscustomobject][ordered]@{
        quick_mode = [bool]$Quick
        launch_count = $LaunchCount
        warmup_count = $WarmupCount
        iteration_count = $IterationCount
        invocation_count = 1
        unroll_factor = 1
        width_methods = $widthMethodCount
        contention_methods = $contentionMethodCount
        rows = $rows.Count
        expected_rows = $expectedRows
        processor = $env:PROCESSOR_IDENTIFIER
        logical_processors = [Environment]::ProcessorCount
        os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
        launcher_framework = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
        benchmark_runtime = (($rows.Runtime | Sort-Object -Unique) -join ';')
        native_sha256 = (Get-FileHash -LiteralPath $nativeLibrary -Algorithm SHA256).Hash
        dotnet_path = $dotnet
        dotnet_version = $dotnetVersionOutput
        dotnet_sha256 = $dotnetHash
        corerun_path = $(if ($corerunIdentity) { $corerunIdentity.path } else { '' })
        corerun_sha256 = $(if ($corerunIdentity) { $corerunIdentity.sha256 } else { '' })
        result = 'PASS'
    } | Export-Csv (Join-Path $OutputDirectory 'benchmark-identity.csv') -NoTypeInformation

    Write-Host "PASS: $($rows.Count) reference-count benchmark rows and $($checks.Count) controls"
    Write-Host "Output: $OutputDirectory"
} finally {
    Set-Location -LiteralPath $savedLocation.ProviderPath
}
