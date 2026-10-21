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
        'artifacts\P2.2\benchmark\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$nativeRoot = Join-Path $RepositoryRoot 'src\native'
$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$project = Join-Path $scriptRoot 'immix-block-benchmark\immix-block-benchmark.csproj'
$platform = Join-Path $scriptRoot '..\P2.1\side-metadata-test-platform.cpp'
$nativeLibrary = Join-Path $OutputDirectory 'immix-block-benchmark-native.dll'
$nativeLog = Join-Path $OutputDirectory 'native-build.log'
$managedBuildLog = Join-Path $OutputDirectory 'managed-build.log'
$benchmarkLog = Join-Path $OutputDirectory 'benchmark.log'
$results = Join-Path $OutputDirectory 'BenchmarkDotNet'

$requiredInputs = @(
    $initializeVisualStudio,
    $project,
    $platform,
    (Join-Path $scriptRoot 'immix-block-benchmark-native.cpp'),
    (Join-Path $gcRoot 'side_metadata.cpp'),
    (Join-Path $gcRoot 'immix_block.cpp')
)
foreach ($inputPath in $requiredInputs) {
    if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) {
        throw "Required benchmark input is missing: $inputPath"
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$nativeCommand = @(
    "call `"$initializeVisualStudio`" x64",
    "cd /d `"$OutputDirectory`"",
    "cl /nologo /LD /std:c++17 /EHsc /O2 /W4 /WX /wd4100 /wd4324 /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 /DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$nativeRoot`" /I`"$nativeRoot\inc`" `"$scriptRoot\immix-block-benchmark-native.cpp`" `"$platform`" `"$gcRoot\side_metadata.cpp`" `"$gcRoot\immix_block.cpp`" /Fe:`"$nativeLibrary`""
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

$savedPath = $env:PATH
$savedRoot = $env:DOTNET_ROOT
$savedNativeBenchmark = $env:P22_NATIVE_BENCHMARK
$savedLocation = Get-Location
try {
    Set-Location -LiteralPath (Split-Path -Parent $project)
    $env:PATH = "$(Join-Path $RepositoryRoot '.dotnet');$env:PATH"
    $env:DOTNET_ROOT = Join-Path $RepositoryRoot '.dotnet'
    $env:P22_NATIVE_BENCHMARK = $nativeLibrary
    try {
        $ErrorActionPreference = 'Continue'
        & $dotnet run --no-build -c Release --project $project -- `
            --filter '*' `
            --launchCount $LaunchCount `
            --warmupCount $WarmupCount `
            --iterationCount $IterationCount `
            --artifacts $results *> $benchmarkLog
        $benchmarkExitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }
    if ($benchmarkExitCode -ne 0) {
        throw "Benchmark run failed. See $benchmarkLog."
    }
} finally {
    Set-Location -LiteralPath $savedLocation.ProviderPath
    $env:PATH = $savedPath
    $env:DOTNET_ROOT = $savedRoot
    if ($null -eq $savedNativeBenchmark) {
        Remove-Item Env:\P22_NATIVE_BENCHMARK -ErrorAction SilentlyContinue
    } else {
        $env:P22_NATIVE_BENCHMARK = $savedNativeBenchmark
    }
}

if (Select-String -LiteralPath $benchmarkLog -Pattern 'There are not any results runs|Benchmarks with issues') {
    throw "Benchmark run produced incomplete results. See $benchmarkLog."
}

$methodCount = @(
    Select-String -LiteralPath (
        Join-Path $scriptRoot 'immix-block-benchmark\Program.cs') `
        -Pattern '^\s*\[Benchmark').Count
$workerCounts = 3
$expectedRows = $methodCount * $workerCounts
$report = Get-ChildItem -LiteralPath (Join-Path $results 'results') `
    -Filter '*-report.csv' -File -Recurse
if ($report.Count -ne 1) {
    throw "Expected one benchmark report, found $($report.Count)."
}
$rows = @(Import-Csv $report.FullName)
if (($rows.Count -ne $expectedRows) -or (@($rows | Where-Object { -not $_.Mean }).Count -ne 0)) {
    throw "Benchmark report has $($rows.Count)/$expectedRows complete rows."
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
foreach ($workerCount in @(1, 4, 16)) {
    $fresh = @($rows | Where-Object {
        ($_.Method -eq 'FreshAcquireRelease') -and
        ([int]$_.WorkerCount -eq $workerCount)
    })
    $control = @($rows | Where-Object {
        ($_.Method -eq 'FreshAcquireReleaseControl') -and
        ([int]$_.WorkerCount -eq $workerCount)
    })
    $extraCas = @($rows | Where-Object {
        ($_.Method -eq 'ExtraCasSensitivity') -and
        ([int]$_.WorkerCount -eq $workerCount)
    })
    $ownerDelay = @($rows | Where-Object {
        ($_.Method -eq 'InjectedOwnerDelay') -and
        ([int]$_.WorkerCount -eq $workerCount)
    })
    if (($fresh.Count -ne 1) -or ($control.Count -ne 1) -or
        ($extraCas.Count -ne 1) -or ($ownerDelay.Count -ne 1)) {
        throw "Benchmark controls are incomplete for worker count $workerCount."
    }

    $aaRatio =
        (Convert-ToNanoseconds $control[0].Mean) /
        (Convert-ToNanoseconds $fresh[0].Mean)
    $extraCasRatio =
        (Convert-ToNanoseconds $extraCas[0].Mean) /
        (Convert-ToNanoseconds $fresh[0].Mean)
    $ownerDelayRatio =
        (Convert-ToNanoseconds $ownerDelay[0].Mean) /
        (Convert-ToNanoseconds $fresh[0].Mean)
    if (($aaRatio -lt 0.90) -or ($aaRatio -gt 1.10)) {
        throw "A/A ratio $aaRatio is outside [0.90, 1.10] for $workerCount workers."
    }
    if ($extraCasRatio -lt 1.5) {
        throw "Extra-CAS ratio $extraCasRatio is below 1.5 for $workerCount workers."
    }
    if ($ownerDelayRatio -lt 1.25) {
        throw "Owner-delay ratio $ownerDelayRatio is below 1.25 for $workerCount workers."
    }

    $checks.Add([pscustomobject][ordered]@{
        worker_count = $workerCount
        aa_ratio = $aaRatio
        extra_cas_ratio = $extraCasRatio
        owner_delay_ratio = $ownerDelayRatio
        result = 'PASS'
    })
}

Copy-Item -LiteralPath $report.FullName `
    -Destination (Join-Path $OutputDirectory 'benchmark-raw.csv')
$checks | Export-Csv (Join-Path $OutputDirectory 'benchmark-controls.csv') -NoTypeInformation
[pscustomobject][ordered]@{
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    operations_per_invoke = 4096
    methods = $methodCount
    worker_counts = '1;4;16'
    rows = $rows.Count
    processor = $env:PROCESSOR_IDENTIFIER
    logical_processors = [Environment]::ProcessorCount
    os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    launcher_framework = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    benchmark_runtime = (($rows.Runtime | Sort-Object -Unique) -join ';')
    native_sha256 = (Get-FileHash $nativeLibrary -Algorithm SHA256).Hash
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'benchmark-identity.csv') -NoTypeInformation

Write-Host "PASS: $($rows.Count) Immix block benchmark rows and $($checks.Count) controls"
Write-Host "Output: $OutputDirectory"
