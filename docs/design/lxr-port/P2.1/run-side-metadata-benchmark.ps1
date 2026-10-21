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
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.1\benchmark\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

$gcRoot = Join-Path $RepositoryRoot 'src\coreclr\gc'
$nativeRoot = Join-Path $RepositoryRoot 'src\native'
$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$initializeVisualStudio = Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'
$project = Join-Path $scriptRoot 'side-metadata-benchmark\side-metadata-benchmark.csproj'
$nativeLibrary = Join-Path $OutputDirectory 'side-metadata-benchmark-native.dll'
$nativeLog = Join-Path $OutputDirectory 'native-build.log'
$benchmarkLog = Join-Path $OutputDirectory 'benchmark.log'
$results = Join-Path $OutputDirectory 'BenchmarkDotNet'

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$nativeCommand = @(
    "call `"$initializeVisualStudio`" x64",
    "cd /d `"$OutputDirectory`"",
    "cl /nologo /LD /std:c++17 /EHsc /O2 /DBUILD_AS_STANDALONE /DTARGET_WINDOWS /DHOST_WINDOWS /DWIN32 /DTARGET_AMD64 /DHOST_AMD64 /DTARGET_64BIT /DHOST_64BIT /I`"$gcRoot`" /I`"$gcRoot\env`" /I`"$nativeRoot`" /I`"$nativeRoot\inc`" `"$scriptRoot\side-metadata-benchmark-native.cpp`" `"$scriptRoot\side-metadata-test-platform.cpp`" `"$gcRoot\side_metadata.cpp`" /Fe:`"$nativeLibrary`""
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

$managedBuildLog = Join-Path $OutputDirectory 'managed-build.log'
try {
    $ErrorActionPreference = 'Continue'
    & $dotnet build $project -c Release --nologo *> $managedBuildLog
    $managedBuildExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
if ($managedBuildExitCode -ne 0) {
    throw 'Managed benchmark build failed.'
}

$savedPath = $env:PATH
$savedRoot = $env:DOTNET_ROOT
$savedLocation = Get-Location
try {
    Set-Location -LiteralPath (Split-Path -Parent $project)
    $env:PATH = "$(Join-Path $RepositoryRoot '.dotnet');$env:PATH"
    $env:DOTNET_ROOT = Join-Path $RepositoryRoot '.dotnet'
    $env:P21_NATIVE_BENCHMARK = $nativeLibrary
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
    Set-Location -LiteralPath $savedLocation.Path
    $env:PATH = $savedPath
    $env:DOTNET_ROOT = $savedRoot
    Remove-Item Env:\P21_NATIVE_BENCHMARK -ErrorAction SilentlyContinue
}

if (Select-String -LiteralPath $benchmarkLog -Pattern 'There are not any results runs|Benchmarks with issues') {
    throw "Benchmark run produced incomplete results. See $benchmarkLog."
}

$manifest = Get-Content (Join-Path $scriptRoot 'metadata-specs.json') -Raw | ConvertFrom-Json
$rcSpec = @($manifest.specs | Where-Object id -eq 'ReferenceCount')
$widthCount = @($rcSpec.logBitsPerValue -split '\|').Count
$methodCount = @(
    Select-String -LiteralPath (
        Join-Path $scriptRoot 'side-metadata-benchmark\Program.cs') `
        -Pattern '^\s*\[Benchmark').Count
$expectedRows = $widthCount * $methodCount
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
    $pattern = '^([\d.]+)\s*(ns|' + [regex]::Escape($microseconds) + '|ms)$'
    $match = [regex]::Match($value, $pattern)
    if (-not $match.Success) {
        throw "Unsupported benchmark time '$value'."
    }
    $number = [double]$match.Groups[1].Value
    $unit = $match.Groups[2].Value
    if ($unit -eq 'ns') {
        return $number
    }
    if ($unit -eq $microseconds) {
        return $number * 1000
    }
    return $number * 1000 * 1000
}

$checks = [Collections.Generic.List[object]]::new()
foreach ($width in 1..3) {
    $map = @($rows | Where-Object {
        ($_.Method -eq 'MapAndLoad') -and ([int]$_.LogReferenceCountBits -eq $width)
    })
    $control = @($rows | Where-Object {
        ($_.Method -eq 'MapAndLoadControl') -and ([int]$_.LogReferenceCountBits -eq $width)
    })
    $bit = @($rows | Where-Object {
        ($_.Method -eq 'BitStore') -and ([int]$_.LogReferenceCountBits -eq $width)
    })
    $sensitivity = @($rows | Where-Object {
        ($_.Method -eq 'ExtraCasSensitivity') -and ([int]$_.LogReferenceCountBits -eq $width)
    })
    if (($map.Count -ne 1) -or ($control.Count -ne 1) -or
        ($bit.Count -ne 1) -or ($sensitivity.Count -ne 1)) {
        throw "Benchmark controls are incomplete for RC log width $width."
    }

    $noiseRatio = (Convert-ToNanoseconds $control[0].Mean) / (Convert-ToNanoseconds $map[0].Mean)
    $sensitivityRatio =
        (Convert-ToNanoseconds $sensitivity[0].Mean) / (Convert-ToNanoseconds $bit[0].Mean)
    if (($noiseRatio -lt 0.90) -or ($noiseRatio -gt 1.10)) {
        throw "A/A noise ratio $noiseRatio is outside [0.90, 1.10] for width $width."
    }
    if ($sensitivityRatio -lt 1.5) {
        throw "Extra-CAS sensitivity ratio $sensitivityRatio is below 1.5 for width $width."
    }
    $checks.Add([pscustomobject][ordered]@{
        log_rc_bits = $width
        aa_ratio = $noiseRatio
        extra_cas_ratio = $sensitivityRatio
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
    methods = $methodCount
    rc_widths = $widthCount
    rows = $rows.Count
    processor = $env:PROCESSOR_IDENTIFIER
    os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
    launcher_framework = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    benchmark_runtime = (($rows.Runtime | Sort-Object -Unique) -join ';')
    benchmark_concurrent_gc = (($rows.Concurrent | Sort-Object -Unique) -join ';')
    benchmark_server_gc = (($rows.Server | Sort-Object -Unique) -join ';')
    native_sha256 = (Get-FileHash $nativeLibrary -Algorithm SHA256).Hash
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'benchmark-identity.csv') -NoTypeInformation

Write-Host "PASS: $($rows.Count) metadata benchmark rows and $($checks.Count) controls"
Write-Host "Output: $OutputDirectory"
