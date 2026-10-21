<#
.SYNOPSIS
    Runs the P0.4 harness smoke matrix: proof of function, not baselines.

.DESCRIPTION
    P0.4 builds and proves the harness; P0.5 collects baselines. This script runs
    short invocations across the requested hosts and collector arms so that every
    scenario is demonstrated to execute end to end with its collector identity
    confirmed. The durations here are deliberately too short to be a measurement.

    Each host is run separately because the host set is not uniform:

      sdk       - the repo's .dotnet, or any dotnet root. All ten scenarios; this
                  is the only stock host with Microsoft.AspNetCore.App.
      testhost  - artifacts/bin/testhost/..., the locally built runtime. All ten
                  scenarios once compose-testhost-aspnet.ps1 has layered the
                  ASP.NET shared framework over it.
      corerun   - artifacts/bin/coreclr/..., the locally built runtime with no
                  shared framework at all. Nine scenarios; aspnet-request-load is
                  recorded as a declared skip, never a silent absence.

.PARAMETER RepoRoot
    Repository root. Defaults to the tree this script ships in.

.PARAMETER Hosts
    Which hosts to smoke. Default: all three.

.PARAMETER Arms
    Which collector arms. Default: wks and srv, the two that exist today.

.PARAMETER RunId
    Output directory name under artifacts/lxr-harness/runs. Default: smoke-<utc>.

.PARAMETER Controls
    Also run the seven controls (adds several minutes).

.EXAMPLE
    pwsh docs/design/lxr-port/harness/scripts/run-smoke.ps1
    Full smoke across all three hosts and both arms.

.EXAMPLE
    pwsh docs/design/lxr-port/harness/scripts/run-smoke.ps1 -Hosts sdk -Controls
    Fast path: one host plus the control demonstrations.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [ValidateSet('sdk', 'testhost', 'corerun')]
    [string[]]$Hosts = @('sdk', 'testhost', 'corerun'),
    [ValidateSet('wks', 'srv', 'srv-datas')]
    [string[]]$Arms = @('wks', 'srv'),
    [string]$RunId,
    [int]$WarmupSeconds = 1,
    [int]$DurationSeconds = 3,
    # Three is Aggregator.MinimumInvocationsForBootstrap: below it the bootstrap has nothing to
    # resample and a ratio is refused rather than fabricated. A smoke run is proof of function, not a
    # measurement, but it should still exercise the path that publishes a ratio.
    [ValidateRange(3, 1000)]
    [int]$Invocations = 3,
    [switch]$Controls,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

# Derive the repo root from the artifact this script ships in, never from the
# working directory: scripts -> harness -> lxr-port -> design -> docs -> root.
if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..\..')).Path
}
$harnessRoot = Join-Path $RepoRoot 'docs\design\lxr-port\harness'
if (-not (Test-Path (Join-Path $harnessRoot 'src\Lxr.Harness.Runner\Lxr.Harness.Runner.csproj'))) {
    throw "Not a harness tree: '$harnessRoot' has no Lxr.Harness.Runner.csproj. Pass -RepoRoot explicitly."
}

$dotnet = Join-Path $RepoRoot '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
    Write-Host "Repo .dotnet absent; using '$dotnet'." -ForegroundColor Yellow
}

if (-not $RunId) {
    $RunId = 'smoke-' + (Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss')
}

$runnerDll = Join-Path $RepoRoot 'artifacts\lxr-harness\build\bin\Lxr.Harness.Runner\release\Lxr.Harness.Runner.dll'

if (-not $SkipBuild) {
    Write-Host '== building harness ==' -ForegroundColor Cyan
    foreach ($project in 'Lxr.Harness.Worker', 'Lxr.Harness.Worker.AspNet', 'Lxr.Harness.Runner', 'Lxr.Harness.Tests') {
        $csproj = Join-Path $harnessRoot "src\$project\$project.csproj"
        & $dotnet build $csproj -c Release --nologo | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "build failed: $project" }
    }
}
if (-not (Test-Path $runnerDll)) { throw "Runner not built: $runnerDll" }

Write-Host "== host inventory ==" -ForegroundColor Cyan
& $dotnet $runnerDll hosts --repo-root $RepoRoot
if ($LASTEXITCODE -ne 0) { throw 'host discovery failed' }

$failures = @()

foreach ($smokeHost in $Hosts) {
    Write-Host ''
    Write-Host "== smoke: host=$smokeHost arms=$($Arms -join ',') ==" -ForegroundColor Cyan
    $hostRunId = "$RunId.$smokeHost"
    $matrixArgs = @(
        'matrix',
        '--repo-root', $RepoRoot,
        '--host', $smokeHost,
        '--run-id', $hostRunId,
        '--warmup-seconds', $WarmupSeconds,
        '--duration-seconds', $DurationSeconds,
        '--invocations', $Invocations
    )
    foreach ($arm in $Arms) { $matrixArgs += @('--arm', $arm) }

    & $dotnet $runnerDll @matrixArgs
    $matrixExit = $LASTEXITCODE
    if ($matrixExit -ne 0) {
        $failures += "matrix host=$smokeHost exit=$matrixExit"
        Write-Host "matrix on '$smokeHost' exited $matrixExit" -ForegroundColor Red
        continue
    }

    $resultsPath = Join-Path $RepoRoot "artifacts\lxr-harness\runs\$hostRunId\results.json"
    & $dotnet $runnerDll conformance --input $resultsPath
    if ($LASTEXITCODE -ne 0) { $failures += "conformance host=$smokeHost" }
}

if ($Controls) {
    Write-Host ''
    Write-Host '== controls ==' -ForegroundColor Cyan
    & $dotnet $runnerDll controls `
        --repo-root $RepoRoot `
        --host sdk `
        --run-id "$RunId.controls" `
        --invocations 15 `
        --warmup-seconds 1 `
        --duration-seconds 3
    if ($LASTEXITCODE -ne 0) { $failures += "controls exit=$LASTEXITCODE" }
}

Write-Host ''
if ($failures.Count -gt 0) {
    Write-Host "SMOKE FAILED:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host "SMOKE OK - artifacts/lxr-harness/runs/$RunId.*" -ForegroundColor Green
Write-Host 'These are proof of function, not baselines. Do not publish them to benchmark-results/.'
exit 0
