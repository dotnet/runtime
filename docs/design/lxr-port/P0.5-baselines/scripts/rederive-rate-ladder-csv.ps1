<#
.SYNOPSIS
    Rebuilds the open-loop rate ladder CSV from the probe reports the ladder run retained.

.DESCRIPTION
    The ladder was measured under a selection rule that was rejected after its output was read: it
    selected on raw dispatch lag, so on the arms where the collector pauses it kept halving the rate
    until the pauses stopped happening. The shipped rule subtracts the longest observed pause first,
    because an in-process dispatcher is suspended by the collector it measures and that slip is the
    measurement rather than an artefact.

    Changing the rule does not require re-measuring: the verdict is a function of two numbers each
    probe already recorded. This script re-derives the CSV from the retained reports so that the
    published file carries the pause beside the lag and the arithmetic between them - the first CSV
    published only the lag, so a reader could not check the subtraction that decided each row.

    It measures nothing. Every column comes from a report on disk, and the run identifiers of the
    probes it read are printed so the set can be checked against the run directory.

.PARAMETER RunsRoot
    Directory holding the retained p05.rateladder.* run directories.

.PARAMETER OutputCsv
    Destination CSV.
#>
[CmdletBinding()]
param(
    [string]$RunsRoot,
    [string]$OutputCsv
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$baselineRoot = Split-Path -Parent $scriptRoot
if (-not $RunsRoot) {
    $repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $baselineRoot))
    $RunsRoot = Join-Path $repoRoot 'artifacts\lxr-harness\runs'
}
if (-not $OutputCsv) { $OutputCsv = Join-Path $baselineRoot 'raw\dispatcher\open-loop-rate-ladder.csv' }

# The bound and the selection threshold are read from the gate rather than restated, so a divergence
# between what this file publishes and what the harness enforces cannot be silent.
$harnessRoot = Join-Path (Split-Path -Parent $baselineRoot) 'harness'
$aggregator = Join-Path $harnessRoot 'src\Lxr.Harness.Runner\Aggregator.cs'
$boundMatch = Select-String -Path $aggregator -Pattern 'MaximumUnexplainedDispatchLagMs\s*=\s*([0-9.]+)'
if (-not $boundMatch) { throw "could not read MaximumUnexplainedDispatchLagMs from $aggregator" }
$gateBound = [double]$boundMatch.Matches[0].Groups[1].Value
$selectionThreshold = $gateBound / 2
Write-Host ("gate bound = {0} ms (read from Aggregator.cs); selection threshold = {1} ms" -f $gateBound, $selectionThreshold)

$dirs = @(Get-ChildItem $RunsRoot -Directory | Where-Object { $_.Name -like 'p05.rateladder.*' } | Sort-Object Name)
Write-Host ("probe run directories found: {0}" -f $dirs.Count)
if ($dirs.Count -eq 0) { throw "no p05.rateladder.* run directories under $RunsRoot" }

$rows = New-Object System.Collections.Generic.List[object]
$reportCount = 0
foreach ($d in $dirs) {
    $reports = Join-Path $d.FullName 'reports'
    if (-not (Test-Path $reports)) {
        Write-Host ("  {0}: NO REPORTS" -f $d.Name) -ForegroundColor Yellow
        continue
    }

    foreach ($f in (Get-ChildItem $reports -Filter '*.json' | Sort-Object Name)) {
        $r = Get-Content $f.FullName -Raw | ConvertFrom-Json
        $m = $r.metrics
        foreach ($field in 'dispatchLagP50Ms', 'dispatchLagP99Ms', 'arrivalRatePerSecond', 'achievedRatePerSecond') {
            if ($m.PSObject.Properties.Name -notcontains $field) {
                throw "report $($f.FullName) has no '$field'; a missing field must not be read as a passing zero"
            }
        }

        $pauseMax = if ($null -ne $r.gc.pauseMaxMs) { [double]$r.gc.pauseMaxMs } else { 0.0 }
        $unexplained = [math]::Max(0.0, $m.dispatchLagP99Ms - $pauseMax)
        $reportCount++

        $rows.Add([pscustomobject]@{
                scenario         = $r.scenario
                arm              = $r.arm
                requestedRate    = [math]::Round($m.arrivalRatePerSecond, 0)
                achievedRate     = [math]::Round($m.achievedRatePerSecond, 1)
                dispatchLagP50Ms = [math]::Round($m.dispatchLagP50Ms, 4)
                dispatchLagP99Ms = [math]::Round($m.dispatchLagP99Ms, 4)
                pauseMaxMs       = [math]::Round($pauseMax, 4)
                collections      = [int]$r.gc.gen0Collections
                unexplainedLagMs = [math]::Round($unexplained, 4)
                withinBound      = ($unexplained -le $gateBound).ToString().ToLowerInvariant()
                withinSelection  = ($unexplained -lt $selectionThreshold).ToString().ToLowerInvariant()
                workerValid      = $r.valid.ToString().ToLowerInvariant()
            })
    }
}

$rows = $rows | Sort-Object scenario, @{Expression = 'requestedRate'; Descending = $true }, arm
$dir = Split-Path -Parent $OutputCsv
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
$rows | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding UTF8

# Print the two counts and their difference rather than a verdict: a bare "all rows within bound"
# reads identically whether the comparison ran or the collection was empty.
$overBound = @($rows | Where-Object { $_.withinBound -eq 'false' })
Write-Host ""
Write-Host ("reports read: {0}   rows written: {1}   rows over the {2} ms bound: {3}   rows within: {4}" -f `
        $reportCount, $rows.Count, $gateBound, $overBound.Count, ($rows.Count - $overBound.Count))
Write-Host "probes whose lag no collector pause accounts for:"
foreach ($row in ($overBound | Sort-Object { [double]$_.unexplainedLagMs } -Descending)) {
    Write-Host ("  {0,-26} {1,-4} rate={2,10:N0}  lag p99={3,10:F2} - pause max={4,7:F2} = {5,10:F2} ms unexplained, {6} collections" -f `
            $row.scenario, $row.arm, $row.requestedRate, $row.dispatchLagP99Ms, $row.pauseMaxMs, $row.unexplainedLagMs, $row.collections)
}
Write-Host ("largest unexplained lag among the rest: {0:F4} ms" -f `
    (($rows | Where-Object { $_.withinBound -eq 'true' } | Measure-Object -Property unexplainedLagMs -Maximum).Maximum))
Write-Host ("wrote {0}" -f $OutputCsv)
