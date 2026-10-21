# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InputDirectory,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $scriptRoot 'raw'
}

$summary = [Collections.Generic.List[object]]::new()
$invocations = [Collections.Generic.List[object]]::new()
$countingObservationsPath = Join-Path $InputDirectory 'counting-callback-observations.csv'
$countingControlsPath = Join-Path $InputDirectory 'counting-callback-controls.csv'
$runtimeIdentitiesPath = Join-Path $InputDirectory 'benchmark-runtime-identities.csv'
foreach ($path in @(
    $countingObservationsPath,
    $countingControlsPath,
    $runtimeIdentitiesPath
)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Benchmark evidence is missing: $path"
    }
}

foreach ($directory in Get-ChildItem -LiteralPath $InputDirectory -Directory) {
    if ($directory.Name -notmatch '^(wks|srv)-(.+)$') {
        continue
    }

    $gc = $Matches[1]
    $variant = $Matches[2]
    $results = Join-Path $directory.FullName 'results'
    foreach ($csv in Get-ChildItem -LiteralPath $results -Filter '*-report.csv') {
        foreach ($row in Import-Csv -LiteralPath $csv.FullName) {
            $summary.Add([pscustomobject][ordered]@{
                GC = $gc
                Variant = $variant
                Method = $row.Method
                Length = if ($row.PSObject.Properties.Name -contains 'Length') {
                    $row.Length
                } else {
                    ''
                }
                IterationCount = $row.IterationCount
                MeanNanoseconds = ([double](($row.Mean -replace ' ns', '').Trim())).ToString(
                    'F6',
                    [Globalization.CultureInfo]::InvariantCulture)
                ErrorNanoseconds = ([double](($row.Error -replace ' ns', '').Trim())).ToString(
                    'F6',
                    [Globalization.CultureInfo]::InvariantCulture)
                StdDevNanoseconds = ([double](($row.StdDev -replace ' ns', '').Trim())).ToString(
                    'F6',
                    [Globalization.CultureInfo]::InvariantCulture)
                AllocatedBytes = [int64](($row.Allocated -replace ' B', '').Trim())
                Gen0 = $row.Gen0
            })
        }
    }

    $log = Get-ChildItem -LiteralPath $directory.FullName -Filter 'BenchmarkRun-*.log' |
        Select-Object -First 1
    if (-not $log) {
        throw "Benchmark log is missing under $($directory.FullName)."
    }

    $method = ''
    $length = ''
    foreach ($line in Get-Content -LiteralPath $log.FullName) {
        if ($line -match '^// Benchmark: (?:P14\.)?([A-Za-z]+AllocationBenchmarks)\.([A-Za-z]+):') {
            $method = "$($Matches[1]).$($Matches[2])"
            $length = if ($line -match '\[Length=(\d+)\]') { $Matches[1] } else { '' }
            continue
        }

        if ($line -match '^WorkloadResult\s+(\d+):\s+(\d+) op, ([0-9.]+) ns, ([0-9.]+) (ns|us)/op$') {
            $nanoseconds = [double]$Matches[4]
            if ($Matches[5] -eq 'us') {
                $nanoseconds *= 1000
            }
            $invocations.Add([pscustomobject][ordered]@{
                GC = $gc
                Variant = $variant
                Method = $method
                Length = $length
                Iteration = [int]$Matches[1]
                Operations = [int64]$Matches[2]
                TotalNanoseconds = $Matches[3]
                NanosecondsPerOperation = $nanoseconds.ToString(
                    'F4',
                    [Globalization.CultureInfo]::InvariantCulture)
            })
        }
    }
}

$countingObservations = @(Import-Csv -LiteralPath $countingObservationsPath)
$countingControls = @(Import-Csv -LiteralPath $countingControlsPath)
$benchmarkRuntimeIdentities = @(Import-Csv -LiteralPath $runtimeIdentitiesPath)

$expectedInvocations = (
    $summary |
        ForEach-Object { [int]$_.IterationCount } |
        Measure-Object -Sum).Sum
if ($invocations.Count -ne $expectedInvocations) {
    throw "Collected $($invocations.Count) invocation rows; expected $expectedInvocations."
}
foreach ($group in @($countingObservations | Group-Object GC)) {
    $expectedObservations = @($group.Group.ExpectedObservations | Sort-Object -Unique)
    if (($expectedObservations.Count -ne 1) -or
        ($group.Count -ne [int]$expectedObservations[0])) {
        throw "Counting callback observation cardinality is invalid for $($group.Name)."
    }
    foreach ($observation in $group.Group) {
        if ([int64]$observation.Count -lt [int64]$observation.Minimum) {
            throw "Counting callback delta is below its minimum for $($group.Name)."
        }
    }
}
if (($countingControls.Count -ne 3) -or
    @($countingControls | Where-Object Result -ne 'PASS').Count -ne 0) {
    throw 'Counting callback controls are incomplete or failed.'
}
if (($benchmarkRuntimeIdentities.Count -ne 5) -or
    @($benchmarkRuntimeIdentities | Where-Object {
        ($_.Sha256 -notmatch '^[0-9A-F]{64}$') -or ([int64]$_.Length -le 0)
    }).Count -ne 0) {
    throw 'Benchmark runtime identities are incomplete or malformed.'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$summary |
    Sort-Object GC, Variant, Method, Length |
    Export-Csv -LiteralPath (Join-Path $OutputDirectory 'benchmark-summary.csv') -NoTypeInformation
$invocations |
    Export-Csv -LiteralPath (Join-Path $OutputDirectory 'benchmark-invocations.csv') -NoTypeInformation
$countingObservations |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'counting-callback-observations.csv') -NoTypeInformation
$countingControls |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'counting-callback-controls.csv') -NoTypeInformation
$benchmarkRuntimeIdentities |
    Export-Csv -LiteralPath (
        Join-Path $OutputDirectory 'benchmark-runtime-identities.csv') -NoTypeInformation

Write-Host (
    "Collected $($summary.Count) summaries, $($invocations.Count) invocation rows, " +
    "$($countingObservations.Count) counting observations, and 3 counting controls.")
