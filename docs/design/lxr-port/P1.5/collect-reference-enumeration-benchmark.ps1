# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$InputDirectory,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}
if (-not $InputDirectory) {
    $InputDirectory = Join-Path $RepositoryRoot (
        'artifacts\p15-reference-enumeration-benchmark')
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $scriptRoot 'raw'
}

$artifactDirectory = Join-Path $InputDirectory 'BenchmarkDotNet.Artifacts'
$resultDirectory = Join-Path $artifactDirectory 'results'
$report = Join-Path $resultDirectory (
    'P15.ReferenceEnumerationBenchmarks-report.csv')
$nativeLibrary = Join-Path $InputDirectory (
    'reference-enumeration-benchmark.dll')
$logs = @(Get-ChildItem -LiteralPath $artifactDirectory -File -Filter '*.log' |
    Sort-Object LastWriteTimeUtc -Descending)
if ($logs.Count -eq 0) {
    throw "No BenchmarkDotNet log found in $artifactDirectory."
}
$log = $logs[0].FullName

foreach ($path in @($report, $nativeLibrary, $log)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required benchmark artifact not found: $path"
    }
}

function Convert-ToNanoseconds([string]$value) {
    if ($value -notmatch '^(?<number>[0-9.]+) (?<unit>ns|μs|ms)$') {
        throw "Unrecognized benchmark duration: $value"
    }
    $number = [double]::Parse(
        $Matches.number,
        [Globalization.CultureInfo]::InvariantCulture)
    switch ($Matches.unit) {
        'ns' { return $number }
        'μs' { return $number * 1000 }
        'ms' { return $number * 1000000 }
    }
}

$rows = [Collections.Generic.List[object]]::new()
$method = $null
$scenario = -1
$launch = 0
foreach ($line in Get-Content -LiteralPath $log) {
    if ($line -match (
        '^// Benchmark: ReferenceEnumerationBenchmarks\.' +
        '(?<method>[A-Za-z]+): .*\[Scenario=(?<scenario>[0-9]+)\]$')) {
        $method = $Matches.method
        $scenario = [int]$Matches.scenario
        $launch = 0
        continue
    }
    if ($line -match '^// Launch: (?<launch>[0-9]+) / [0-9]+$') {
        $launch = [int]$Matches.launch
        continue
    }
    if ($line -match (
        '^WorkloadActual\s+(?<iteration>[0-9]+): ' +
        '(?<operations>[0-9]+) op, (?<total>[0-9.]+) ns, ' +
        '(?<value>[0-9.]+) (?<unit>ns|us|μs|ms)/op$')) {
        if (($null -eq $method) -or ($scenario -lt 0) -or ($launch -le 0)) {
            throw "Workload row has no benchmark context: $line"
        }
        $duration = "$($Matches.value) $($Matches.unit -replace '^us$', 'μs')"
        $rows.Add([pscustomobject][ordered]@{
            Method = $method
            Scenario = $scenario
            Launch = $launch
            Iteration = [int]$Matches.iteration
            Operations = [int64]$Matches.operations
            TotalNanoseconds = [double]::Parse(
                $Matches.total,
                [Globalization.CultureInfo]::InvariantCulture)
            NanosecondsPerOperation = Convert-ToNanoseconds $duration
        })
    }
}

$expectedMethods = @(
    'PerSlotCallback',
    'ReferenceRanges',
    'ReferenceRangeVisitor')
$expectedRows = $expectedMethods.Count * 3 * 3 * 8
if ($rows.Count -ne $expectedRows) {
    throw "Found $($rows.Count) raw rows; expected $expectedRows."
}
foreach ($methodName in $expectedMethods) {
    foreach ($scenarioValue in 0..2) {
        $group = @($rows | Where-Object {
            ($_.Method -eq $methodName) -and
            ($_.Scenario -eq $scenarioValue)
        })
        if ($group.Count -ne 24) {
            throw "$methodName/$scenarioValue has $($group.Count) rows; expected 24."
        }
    }
}

$summaryRows = [Collections.Generic.List[object]]::new()
foreach ($row in Import-Csv -LiteralPath $report) {
    if ($row.Method -notin $expectedMethods) {
        continue
    }
    $hasMedian = (
        ($row.PSObject.Properties.Name -contains 'Median') -and
        $row.Median
    )
    $summaryRows.Add([pscustomobject][ordered]@{
        Method = $row.Method
        Scenario = [int]$row.Scenario
        MeanNanoseconds = Convert-ToNanoseconds $row.Mean
        ErrorNanoseconds = Convert-ToNanoseconds $row.Error
        StdDevNanoseconds = Convert-ToNanoseconds $row.StdDev
        MedianNanoseconds = if ($hasMedian) {
            Convert-ToNanoseconds $row.Median
        } else {
            ''
        }
        MedianSource = if ($hasMedian) {
            'BenchmarkDotNet'
        } else {
            'not-reported'
        }
        Ratio = [double]::Parse(
            $row.Ratio,
            [Globalization.CultureInfo]::InvariantCulture)
        AllocatedBytes = 0
        RawSampleCount = 24
    })
}
if ($summaryRows.Count -ne 9) {
    throw "Found $($summaryRows.Count) summary rows; expected 9."
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$rows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'benchmark-invocations.csv') -NoTypeInformation
$summaryRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'benchmark-summary.csv') -NoTypeInformation

$identityRows = @(
    [pscustomobject][ordered]@{
        Name = 'reference-enumeration-benchmark.dll'
        Sha256 = (Get-FileHash -LiteralPath $nativeLibrary -Algorithm SHA256).Hash
        Length = (Get-Item -LiteralPath $nativeLibrary).Length
    },
    [pscustomobject][ordered]@{
        Name = 'gcref.h'
        Sha256 = (Get-FileHash -LiteralPath (
            Join-Path $RepositoryRoot 'src\coreclr\gc\gcref.h') -Algorithm SHA256).Hash
        Length = (Get-Item -LiteralPath (
            Join-Path $RepositoryRoot 'src\coreclr\gc\gcref.h')).Length
    }
)
$identityRows | Export-Csv -LiteralPath (
    Join-Path $OutputDirectory 'benchmark-identities.csv') -NoTypeInformation

Write-Host (
    "PASS: {0} raw rows, {1} summaries, {2} identities from {3}" -f
    $rows.Count,
    $summaryRows.Count,
    $identityRows.Count,
    ([IO.Path]::GetFileName($log)))
