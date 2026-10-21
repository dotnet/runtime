<#
.SYNOPSIS
    Measures, per scenario, the highest arrival rate the open-loop dispatcher actually delivers.

.DESCRIPTION
    P0.5 finding F15: the latency pass derives its arrival rate as a fraction of *closed-loop*
    throughput capacity, but the open-loop path costs more per operation than the closed-loop one,
    so that fraction is not a utilisation. F17: the shortfall is per scenario, not global, so a
    single --rate-cap cannot express it. In P0.5's first latency matrix, allocation-churn at
    63,771 op/s ran a 5.4 ms p99 dispatch lag while pointer-chasing at 74,541 op/s ran 0.009 ms.
    A cap chosen to fix the first would needlessly idle the second.

    So each scenario's rate is measured rather than assumed. The ladder descends from the rate the
    runner would derive on its own, halving until the dispatcher's p99 scheduling error that no
    collector pause accounts for falls below half of Aggregator.MaximumUnexplainedDispatchLagMs on
    *both* arms. Half, not all, of the bound: the selected rate has to survive five invocations in
    the matrix afterwards, and selecting at the bound would put the matrix's own validity on a coin
    flip.

    The subtraction matters and was learned from this ladder's own output. Selecting on raw lag drove
    allocation-churn to 1,993 op/s - 1.6% of its capacity - because on wks the lag appeared only when
    the collector paused, and shrank as the rate fell. That rule tunes the offered load until the
    collector stops mattering. See P0.5-baselines.md section 6: the selections this script made under
    the raw-lag rule were rejected after inspection, and the rates the matrix uses are the runner's
    own 50%-of-capacity derivation. The ladder is published as the evidence for that decision and for
    the dispatcher's own capacity ceiling, not as the source of the rates.

    Every probe prints what it compared - requested rate, achieved rate, lag - and the CSV keeps
    every rung, including the rejected ones. A ladder that published only its answer would be
    indistinguishable from one that had guessed.

.PARAMETER RateSource
    The throughput results.json the rates are derived from. Capacity is a closed-loop measurement
    and does not involve the dispatcher, so a throughput pass from a previous build is a valid
    source; the value is printed so a reader can see which one was used.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$RateSource,
    [string]$OutputCsv,
    [ValidateSet('sdk', 'testhost', 'corerun')]
    [string]$MeasurementHost = 'testhost',
    [double]$HeapFactor = 6.0,
    [double[]]$Fractions = @(1.0, 0.5, 0.25, 0.125, 0.0625, 0.03125),
    [int]$WarmupSeconds = 3,
    [int]$DurationSeconds = 10,
    # Half of Aggregator.MaximumDispatchLagMs. Asserted against the source below rather than
    # restated from memory: a threshold that drifts from the gate it feeds selects rates the gate
    # then rejects.
    [double]$SelectionThresholdMs = 0.5,
    # Scenarios with no closed-loop capacity to derive from. aspnet-request-load is latency-primary:
    # it runs open-loop in both phases, so its "throughput" row is its achieved arrival rate and
    # carries no information about how fast it could go. Its ladder therefore starts from a declared
    # rate rather than a measured one, and the difference is published rather than hidden - the
    # runner's own fallback for this case is a silent 1000 op/s that looks measured.
    [string[]]$StartRate = @('aspnet-request-load=16000')
)

$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\..')).Path
}
$harnessRoot = Join-Path $RepoRoot 'docs\design\lxr-port\harness'
$dotnet = Join-Path $RepoRoot '.dotnet\dotnet.exe'
$runnerDll = Join-Path $RepoRoot 'artifacts\lxr-harness\build\bin\Lxr.Harness.Runner\release\Lxr.Harness.Runner.dll'
$calibrationPath = Join-Path $RepoRoot 'docs\design\lxr-port\P0.5-baselines\calibration.json'
if (-not $OutputCsv) {
    $OutputCsv = Join-Path $RepoRoot 'docs\design\lxr-port\P0.5-baselines\raw\dispatcher\open-loop-rate-ladder.csv'
}
if (-not $RateSource) {
    throw 'RateSource is required: the ladder descends from the rate the runner itself would derive.'
}

# The selection threshold must stay tied to the gate it feeds. Read the bound out of the source
# instead of trusting this script's default.
$aggregator = Join-Path $harnessRoot 'src\Lxr.Harness.Runner\Aggregator.cs'
$boundMatch = Select-String -Path $aggregator -Pattern 'MaximumUnexplainedDispatchLagMs\s*=\s*([0-9.]+)'
if (-not $boundMatch) { throw "could not read MaximumUnexplainedDispatchLagMs from $aggregator" }
$gateBound = [double]$boundMatch.Matches[0].Groups[1].Value
Write-Host ("gate bound (Aggregator.MaximumUnexplainedDispatchLagMs) = {0} ms; selecting below {1} ms" -f $gateBound, $SelectionThresholdMs)
if ($SelectionThresholdMs -ge $gateBound) {
    throw "selection threshold $SelectionThresholdMs ms must be below the gate bound $gateBound ms"
}

# Recompute the runner's own derivation - 50% of the lowest per-arm capacity - and print it beside
# the rate the last latency pass actually used, so a silent divergence in this script's arithmetic
# shows up as a mismatch rather than as a plausible number.
$throughput = Get-Content $RateSource -Raw | ConvertFrom-Json
$results = $throughput.checkpoints[0].results
$capacity = @{}
foreach ($r in $results) {
    if (-not $r.valid) { continue }
    if ($null -eq $r.operationsPerSecond) { continue }
    if (-not $capacity.ContainsKey($r.scenario) -or $r.operationsPerSecond -lt $capacity[$r.scenario]) {
        $capacity[$r.scenario] = $r.operationsPerSecond
    }
}

Write-Host ''
Write-Host 'derived rates (0.5 x lowest valid per-arm throughput):'
$derived = @{}
foreach ($s in ($capacity.Keys | Sort-Object)) {
    $derived[$s] = [math]::Max(1.0, [math]::Round($capacity[$s] * 0.5))
    Write-Host ("  {0,-26} capacity={1,12:N0}  derived={2,12:N0}" -f $s, $capacity[$s], $derived[$s])
}

$missing = @()
foreach ($r in $results) { if (-not $capacity.ContainsKey($r.scenario)) { $missing += $r.scenario } }
$missing = $missing | Sort-Object -Unique
foreach ($s in $missing) {
    Write-Host ("  {0,-26} NO VALID THROUGHPUT CELL - the runner has no capacity to derive from" -f $s) -ForegroundColor Yellow
}

foreach ($entry in $StartRate) {
    $parts = $entry.Split('=')
    if ($parts.Count -ne 2) { throw "malformed -StartRate '$entry'; expected scenario=rate" }
    $name = $parts[0]
    $value = [double]::Parse($parts[1], [cultureinfo]::InvariantCulture)
    if ($derived.ContainsKey($name)) {
        Write-Host ("  {0,-26} declared start {1:N0} REPLACES the derived {2:N0}" -f $name, $value, $derived[$name]) -ForegroundColor Yellow
    }
    else {
        Write-Host ("  {0,-26} declared start {1:N0} (no measured capacity exists)" -f $name, $value) -ForegroundColor Yellow
    }
    $derived[$name] = $value
}

$rows = New-Object System.Collections.Generic.List[object]
$selected = @{}

foreach ($scenario in ($derived.Keys | Sort-Object)) {
    $chosen = $null
    foreach ($fraction in $Fractions) {
        $rate = [math]::Max(1.0, [math]::Round($derived[$scenario] * $fraction))
        $runId = "p05.rateladder.$scenario.$($rate.ToString([cultureinfo]::InvariantCulture))"
        $runDir = Join-Path $RepoRoot "artifacts\lxr-harness\runs\$runId"
        if (Test-Path $runDir) { Remove-Item $runDir -Recurse -Force }

        $args = @(
            'matrix',
            '--repo-root', $RepoRoot,
            '--host', $MeasurementHost,
            '--run-id', $runId,
            '--mode', 'latency',
            '--scenario', $scenario,
            '--arm', 'wks', '--arm', 'srv',
            '--heap-factor', $HeapFactor.ToString([cultureinfo]::InvariantCulture),
            '--invocations', 1,
            '--warmup-seconds', $WarmupSeconds,
            '--duration-seconds', $DurationSeconds,
            '--calibration', $calibrationPath,
            '--scenario-rate', "$scenario=$($rate.ToString([cultureinfo]::InvariantCulture))",
            '--no-samples'
        )
        & $dotnet $runnerDll @args | Out-Null

        $reports = Join-Path $runDir 'reports'
        if (-not (Test-Path $reports)) {
            Write-Host ("  {0,-26} rate={1,10:N0}  NO REPORTS - the probe did not produce a run" -f $scenario, $rate) -ForegroundColor Yellow
            $rows.Add([pscustomobject]@{ scenario = $scenario; fraction = $fraction; requestedRate = $rate; arm = ''; achievedRate = ''; dispatchLagP50Ms = ''; dispatchLagP99Ms = ''; pauseMaxMs = ''; unexplainedLagMs = ''; valid = 'false'; selected = 'false' })
            continue
        }

        $worstUnexplained = -1.0
        $armRows = @()
        foreach ($file in (Get-ChildItem $reports -Filter '*.json')) {
            $report = Get-Content $file.FullName -Raw | ConvertFrom-Json
            $m = $report.metrics
            foreach ($field in 'dispatchLagP99Ms', 'arrivalRatePerSecond') {
                if ($m.PSObject.Properties.Name -notcontains $field) {
                    throw "report $($file.Name) has no '$field'; a missing field must not be read as a passing zero"
                }
            }
            if ([math]::Abs($m.arrivalRatePerSecond - $rate) -gt 1) {
                throw "asked for $rate op/s but the worker was told $($m.arrivalRatePerSecond); --scenario-rate did not take effect"
            }

            # A pause the collector took suspends the dispatcher with everything else, so it explains
            # the schedule slipping by that much. No pause reported means no collection occurred in the
            # measured region, and nothing explains the lag - that is the F15 signature, not a zero to
            # pass over.
            $pauseMax = if ($null -ne $report.gc.pauseMaxMs) { [double]$report.gc.pauseMaxMs } else { 0.0 }
            $unexplained = [math]::Max(0.0, $m.dispatchLagP99Ms - $pauseMax)

            $armRows += [pscustomobject]@{
                scenario           = $scenario
                fraction           = $fraction
                requestedRate      = $rate
                arm                = $report.arm
                achievedRate       = [math]::Round($m.achievedRatePerSecond, 1)
                dispatchLagP50Ms   = [math]::Round($m.dispatchLagP50Ms, 4)
                dispatchLagP99Ms   = [math]::Round($m.dispatchLagP99Ms, 4)
                pauseMaxMs         = [math]::Round($pauseMax, 4)
                unexplainedLagMs   = [math]::Round($unexplained, 4)
                valid              = $report.valid.ToString().ToLowerInvariant()
                selected           = 'false'
            }
            if ($unexplained -gt $worstUnexplained) { $worstUnexplained = $unexplained }
        }

        $verdict = if ($worstUnexplained -lt $SelectionThresholdMs) { 'KEEP' } else { 'reject' }
        Write-Host ("  {0,-26} rate={1,10:N0} ({2,7:P2} of derived)  worst unexplained lag p99={3,8:F4} ms  {4}" -f `
                $scenario, $rate, $fraction, $worstUnexplained, $verdict)
        foreach ($ar in $armRows) {
            Write-Host ("      {0}: achieved={1,10:N0}  lag p99={2,8:F4} - pause max={3,8:F4} = unexplained {4,8:F4}  valid={5}" -f `
                    $ar.arm, $ar.achievedRate, $ar.dispatchLagP99Ms, $ar.pauseMaxMs, $ar.unexplainedLagMs, $ar.valid)
        }

        if ($verdict -eq 'KEEP') {
            foreach ($ar in $armRows) { $ar.selected = 'true' }
            $chosen = $rate
        }
        foreach ($ar in $armRows) { $rows.Add($ar) }
        if ($chosen) { break }
    }

    if ($chosen) {
        $selected[$scenario] = $chosen
    }
    else {
        Write-Host ("  {0,-26} NO RATE DELIVERED - every rung exceeded the threshold" -f $scenario) -ForegroundColor Red
    }
}

$dir = Split-Path $OutputCsv -Parent
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
$rows | Export-Csv -Path $OutputCsv -NoTypeInformation
Write-Host ''
Write-Host ("wrote {0} ladder rows to {1}" -f $rows.Count, $OutputCsv)

Write-Host ''
Write-Host 'selected rates:'
foreach ($s in ($selected.Keys | Sort-Object)) {
    Write-Host ("  --scenario-rate {0}={1}" -f $s, $selected[$s])
}
Write-Host ''
Write-Host '--- paste-ready argument list ---'
$flat = @()
foreach ($s in ($selected.Keys | Sort-Object)) { $flat += "$s=$($selected[$s])" }
Write-Host ($flat -join ',')
exit 0
