<#
.SYNOPSIS
    Runs the P0.5 Workstation/Server GC baseline measurement.

.DESCRIPTION
    One script, so that the primary session and the reproducibility session run the same
    experiment rather than two similar ones. The reproducibility claim in P0.5 is
    session-to-session agreement on a declared subset; that claim is only meaningful if the
    second session's configuration is the first session's configuration, and the cheapest way
    to guarantee that is to have a single committed artefact produce both.

    Phases, each independently selectable so a failed phase can be resumed without repeating
    the ones before it:

      calibrate  - bisect the minimum viable heap per (scenario, arm) and write calibration.json.
      controls   - re-run the seven controls on this session's build, establishing this
                   session's measurement resolution floor. Control 7 also carries the
                   blind-band assertion.
      throughput - closed-loop throughput across the heap-factor axis.
      latency    - open-loop latency at arrival rates derived from the throughput pass.
      publish    - merge run directories into a committed schema-v2 checkpoint plus raw CSV.

    Nothing here averages a repeated measurement away: every invocation's metrics reach the
    raw CSV, and the aggregate carries a bootstrap interval over them.

.PARAMETER RepoRoot
    Repository root. Defaults to the tree this script ships in, never the working directory.

.PARAMETER Phases
    Which phases to run, in the order given. Default: all five.

.PARAMETER SessionId
    Identifies this measurement session. The primary session and any reproducibility session
    must differ here and agree everywhere else.

.PARAMETER Subset
    Restrict to the declared reproducibility subset rather than the full ten scenarios.

.PARAMETER MeasurementHost
    Which host to measure on. 'testhost' is the locally built runtime and is the primary.

.EXAMPLE
    pwsh docs/design/lxr-port/P0.5-baselines/scripts/run-p05-baselines.ps1 -SessionId s1
    The full primary session.

.EXAMPLE
    pwsh docs/design/lxr-port/P0.5-baselines/scripts/run-p05-baselines.ps1 `
        -SessionId s2 -Subset -Phases throughput,latency
    The reproducibility session: the declared subset, reusing session 1's calibration.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [ValidateSet('calibrate', 'controls', 'throughput', 'latency', 'publish')]
    [string[]]$Phases = @('calibrate', 'controls', 'throughput', 'latency', 'publish'),
    [string]$SessionId = 's1',
    [switch]$Subset,
    [ValidateSet('sdk', 'testhost', 'corerun')]
    [string]$MeasurementHost = 'testhost',
    [ValidateSet('wks', 'srv', 'srv-datas')]
    [string[]]$Arms = @('wks', 'srv'),
    [double[]]$HeapFactors = @(1.3, 2.0, 6.0),
    [ValidateRange(3, 1000)]
    [int]$Invocations = 5,
    [int]$WarmupSeconds = 3,
    [int]$DurationSeconds = 10,
    # 4 MiB, not the runner's 16 MiB default. At 16 the first calibration bottomed out for six of
    # the ten scenarios under wks: the probe succeeded at the floor, so the recorded 'minimum' was
    # the search bound rather than the scenario's requirement, and every heap factor derived from it
    # was an upper bound on a denominator nobody had measured. A floor low enough that the runtime
    # itself refuses to start is the right bound, because that refusal is a real answer.
    [int]$CalibrationFloorMb = 4,
    # Upper bound on any derived arrival rate. The open-loop path costs more per operation than the
    # closed-loop one it derives its rate from, so 50% of measured throughput capacity is not 50%
    # utilisation: above roughly a million operations a second the single dispatcher thread cannot
    # release work on time and the reported latency becomes its backlog. The cap is bracketed by
    # direct measurement rather than chosen for roundness - the ladder in raw/dispatcher delivered
    # 779,158 op/s at 0.24 ms of unexplained lag and failed at 1,290,265 op/s with 3,892 ms - and it
    # binds only the two fastest scenarios. Ladder and reasoning in P0.5-baselines.md section 6.
    [double]$RateCapPerSecond = 750000,
    # aspnet-request-load is latency-primary and runs open-loop in both phases, so it has no
    # closed-loop capacity to derive a rate from. Before this was declared the runner fell back to
    # its global 1000 op/s default, which looks measured and is not; the runner now refuses instead
    # (F18). 8,000 op/s is the highest rung the ladder delivered - achieved 7,999.9 against 8,000
    # requested, no unexplained lag - while 16,000 achieved only 10,466 and ran 1,256 ms behind.
    [string]$AspNetRatePerSecond = '8000',
    # A committed table of arrival rates, one row per scenario. When given, the latency pass offers
    # exactly these rates instead of deriving them from this session's own throughput measurement.
    # Deriving is right the first time and wrong every time after: it makes the offered load track
    # machine state, so two sessions measure different configurations under the same cell heading
    # (F23). raw/scenario-rates-s2.csv is the table this step measured.
    [string]$RateTable = '',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

# Derive the root from the artefact this script ships in: P0.3's gate defaulted its root to an
# absolute path inside one worktree and silently audited a different checkout.
# scripts -> P0.5-baselines -> lxr-port -> design -> docs -> root
if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\..')).Path
}
$harnessRoot = Join-Path $RepoRoot 'docs\design\lxr-port\harness'
if (-not (Test-Path (Join-Path $harnessRoot 'src\Lxr.Harness.Runner\Lxr.Harness.Runner.csproj'))) {
    throw "Not a harness tree: '$harnessRoot' has no Lxr.Harness.Runner.csproj. Pass -RepoRoot explicitly."
}

# The repo-local SDK, not the machine-wide one: global.json pins an 11.0 preview runtime that is
# not installed machine-wide, so the machine-wide dotnet cannot launch the harness at all.
$dotnet = Join-Path $RepoRoot '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { throw "Repo-local SDK missing: '$dotnet'. Run build.cmd first." }

$runnerDll = Join-Path $RepoRoot 'artifacts\lxr-harness\build\bin\Lxr.Harness.Runner\release\Lxr.Harness.Runner.dll'
$publishRoot = Join-Path $RepoRoot 'docs\design\lxr-port\P0.5-baselines'
$calibrationPath = Join-Path $publishRoot 'calibration.json'

# The declared reproducibility subset. Chosen before any results were seen, spanning the axes
# that matter: a compute-bound scenario with almost no GC, a churn scenario, a scenario with a
# stable live set, and one dominated by the large object heap.
$subsetScenarios = @('low-allocation-compute', 'allocation-churn', 'long-lived-cache', 'large-object-pressure')

# A subset name that matches nothing is indistinguishable from a scenario that was measured and
# produced nothing, and it reports as the latter. Checked against the catalog the runner itself
# uses, so a rename upstream fails here rather than quietly shrinking the subset.
if ($Subset) {
    $catalog = Join-Path $harnessRoot 'src\Lxr.Harness.Core\ScenarioCatalog.cs'
    $known = (Select-String -Path $catalog -Pattern 'Id = "([a-z0-9-]+)"').Matches.Groups |
        Where-Object { $_.Name -eq '1' } | ForEach-Object { $_.Value }
    $missing = $subsetScenarios | Where-Object { $known -notcontains $_ }
    if ($missing) { throw "subset names not in the scenario catalog: $($missing -join ', ')" }
}

# Facts the worker cannot read in-process. Measured, not assumed: see P0.5-baselines.md §3.
$machineArgs = @(
    '--machine-power-plan', 'High performance',
    '--machine-physical-cores', '8',
    '--machine-model', 'Virtual Machine'
)

function Invoke-Runner {
    param([string[]]$RunnerArgs, [string]$What, [int[]]$AllowExit = @(0))

    Write-Host ">> $What" -ForegroundColor Cyan
    Write-Host "   $dotnet $runnerDll $($RunnerArgs -join ' ')" -ForegroundColor DarkGray
    & $dotnet $runnerDll @RunnerArgs
    $code = $LASTEXITCODE
    if ($AllowExit -notcontains $code) { throw "$What failed with exit $code" }
    return $code
}

if (-not $SkipBuild) {
    Write-Host '== building harness ==' -ForegroundColor Cyan
    foreach ($project in 'Lxr.Harness.Worker', 'Lxr.Harness.Worker.AspNet', 'Lxr.Harness.Runner', 'Lxr.Harness.Tests') {
        $csproj = Join-Path $harnessRoot "src\$project\$project.csproj"
        & $dotnet build $csproj -c Release --nologo -v q | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "build failed: $project" }
    }
    # The unit tests gate the measurement: a publisher that writes 0 where it means "not
    # measured" produces a plausible-looking file, and the failure is invisible in the output.
    & $dotnet (Join-Path $RepoRoot 'artifacts\lxr-harness\build\bin\Lxr.Harness.Tests\release\Lxr.Harness.Tests.dll')
    if ($LASTEXITCODE -ne 0) { throw 'harness unit tests failed; refusing to measure' }
}
if (-not (Test-Path $runnerDll)) { throw "Runner not built: $runnerDll" }

$scenarioArgs = @()
if ($Subset) { foreach ($s in $subsetScenarios) { $scenarioArgs += @('--scenario', $s) } }
$armArgs = @()
foreach ($a in $Arms) { $armArgs += @('--arm', $a) }
$heapArgs = @()
foreach ($h in $HeapFactors) { $heapArgs += @('--heap-factor', $h.ToString([cultureinfo]::InvariantCulture)) }

$throughputRun = "p05.$SessionId.$MeasurementHost.throughput"
$latencyRun = "p05.$SessionId.$MeasurementHost.latency"

if ($Phases -contains 'calibrate') {
    # Calibration runs unpinned by heap factor - it is looking for the minimum, so a factor
    # applied to a minimum that does not exist yet would be circular. It uses the measurement's
    # own warmup and steady state, because a heap that survives a shorter probe than the run
    # is not a heap the run can use.
    Invoke-Runner -What 'calibrate minimum heaps' -RunnerArgs (@(
        'calibrate',
        '--repo-root', $RepoRoot,
        '--host', $MeasurementHost,
        '--run-id', "p05.$SessionId.calibrate",
        '--warmup-seconds', $WarmupSeconds,
        '--duration-seconds', $DurationSeconds,
        '--calibration-floor-mb', $CalibrationFloorMb,
        '--calibration', $calibrationPath
    ) + $scenarioArgs + $armArgs + $machineArgs) -AllowExit @(0, 1)
}

if ($Phases -contains 'controls') {
    # 15 invocations: P0.4 saw the resolution ladder reach its target at n=8, at n=15, and not
    # at all across three runs of the same binary. The conservative figure is the one this
    # session observes, so the ladder is run out rather than stopped at the first success.
    Invoke-Runner -What 'controls (resolution floor + blind band)' -RunnerArgs @(
        'controls',
        '--repo-root', $RepoRoot,
        '--host', $MeasurementHost,
        '--run-id', "p05.$SessionId.controls",
        '--invocations', 15,
        '--warmup-seconds', $WarmupSeconds,
        '--duration-seconds', $DurationSeconds
    )
}

if ($Phases -contains 'throughput') {
    Invoke-Runner -What 'throughput matrix' -RunnerArgs (@(
        'matrix',
        '--repo-root', $RepoRoot,
        '--host', $MeasurementHost,
        '--run-id', $throughputRun,
        '--mode', 'throughput',
        '--invocations', $Invocations,
        '--warmup-seconds', $WarmupSeconds,
        '--duration-seconds', $DurationSeconds,
        '--calibration', $calibrationPath,
        '--step-id', 'P0.5'
    ) + $scenarioArgs + $armArgs + $heapArgs + $machineArgs) -AllowExit @(0, 1)
}

if ($Phases -contains 'latency') {
    $rateArgs = @()
    if ($RateTable) {
        # Pinning the rates rather than re-deriving them is the fix for F23. Deriving each session's
        # arrival rate from that session's own throughput pass makes the offered load a function of
        # machine state: s3 measured allocation-churn 17.7% faster than s2 and therefore offered it
        # 67,883 op/s against s2's 49,845, so the two sessions' latency numbers for that cell are not
        # measurements of the same configuration and cannot be compared. A committed table makes the
        # load a constant of the experiment instead of an output of it.
        if (-not (Test-Path $RateTable)) {
            throw "the pinned rate table '$RateTable' does not exist. Passing -RateTable is a claim " +
                  'that the offered load is fixed across sessions; a missing file would silently ' +
                  'return to deriving it, which is the defect the flag exists to avoid.'
        }
        $pinned = Import-Csv -Path $RateTable
        foreach ($entry in $pinned) {
            $rateArgs += @('--scenario-rate', "$($entry.scenario)=$($entry.ratePerSecond)")
        }
        Write-Host "  pinned arrival rates: $($pinned.Count) scenarios from $RateTable"
    }
    else {
        $rateSource = Join-Path $RepoRoot "artifacts\lxr-harness\runs\$throughputRun\results.json"
        if (-not (Test-Path $rateSource)) {
            throw "latency needs the throughput pass first: '$rateSource' does not exist. " +
                  'An open-loop arrival rate that is not derived from measured capacity produces ' +
                  'either an overloaded run or an idle one, and both measure something other than latency.'
        }
        $rateArgs = @('--rate-from', $rateSource, '--rate-cap', $RateCapPerSecond)
        $rateArgs += @('--scenario-rate', "aspnet-request-load=$AspNetRatePerSecond")
    }

    Invoke-Runner -What 'latency matrix' -RunnerArgs (@(
        'matrix',
        '--repo-root', $RepoRoot,
        '--host', $MeasurementHost,
        '--run-id', $latencyRun,
        '--mode', 'latency',
        '--invocations', $Invocations,
        '--warmup-seconds', $WarmupSeconds,
        '--duration-seconds', $DurationSeconds,
        '--calibration', $calibrationPath,
        '--step-id', 'P0.5'
    ) + $rateArgs + $scenarioArgs + $armArgs + $heapArgs + $machineArgs) -AllowExit @(0, 1)
}

if ($Phases -contains 'publish') {
    $inputs = @()
    foreach ($run in @($throughputRun, $latencyRun)) {
        $dir = Join-Path $RepoRoot "artifacts\lxr-harness\runs\$run"
        if (Test-Path $dir) { $inputs += @('--publish-input', $dir) }
        else { Write-Host "   no run directory for '$run'; it will not be published" -ForegroundColor Yellow }
    }
    if ($inputs.Count -eq 0) { throw 'publish has no inputs; nothing was measured' }

    $checkpointId = if ($Subset) { "p0-5-baselines-$SessionId-subset" } else { "p0-5-baselines-$SessionId" }
    Invoke-Runner -What 'publish checkpoint' -RunnerArgs (@(
        'publish',
        '--repo-root', $RepoRoot,
        '--output', $publishRoot,
        '--checkpoint-id', $checkpointId,
        '--checkpoint-date', (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd'),
        '--step-id', 'P0.5'
    ) + $inputs)
}

Write-Host ''
Write-Host "P0.5 phases complete: $($Phases -join ', ')" -ForegroundColor Green
exit 0
