# Demonstrates the control 7 blind-band assertion FAILING.
#
# An assertion nobody has watched fail is indistinguishable from an absent one. P0.4 declined to ship
# this bound precisely because it could not watch it fire.
#
# The degradation is a genuinely loaded machine, not a tightened threshold. That matters: the bound
# exists to catch a catastrophic loss of measurement resolution, and CPU contention is exactly how
# that happens in practice. Moving the threshold until it trips would demonstrate arithmetic, not the
# assertion. The threshold here is the shipped default.
#
# Control 7 floors its own invocation count at 9 and its steady state at 8 s, so it cannot be degraded
# by running it briefly - the control refuses the configuration in which it could not work. Load is
# the remaining honest lever.

[CmdletBinding()]
param(
    [string]$RepoRoot = 'M:\',
    [int]$Burners = 14,
    [string]$RunId = 'p05.s1.control7-blindband-firing'
)

$ErrorActionPreference = 'Stop'
$dotnet = Join-Path $RepoRoot '.dotnet\dotnet.exe'
$runner = Join-Path $RepoRoot 'artifacts\lxr-harness\build\bin\Lxr.Harness.Runner\release\Lxr.Harness.Runner.dll'
$log = Join-Path $RepoRoot "artifacts\$RunId.log"

$jobs = @()
try {
    Write-Host "starting $Burners CPU burners to degrade measurement resolution" -ForegroundColor Yellow
    for ($i = 0; $i -lt $Burners; $i++) {
        $jobs += Start-Process -FilePath 'powershell.exe' -PassThru -WindowStyle Hidden `
            -ArgumentList '-NoProfile', '-Command', '$x=0.0; while ($true) { $x = [math]::Sqrt($x + 1.0) * 1.0000001 }'
    }
    Start-Sleep -Seconds 3
    $load = (Get-Counter '\Processor(_Total)\% Processor Time' -SampleInterval 1 -MaxSamples 3).CounterSamples.CookedValue
    Write-Host ("background load during the run: " + (($load | ForEach-Object { [math]::Round($_, 1) }) -join ', ') + " %")

    & $dotnet $runner controls --repo-root $RepoRoot --host testhost --control 7 `
        --run-id $RunId --invocations 9 --warmup-seconds 3 --duration-seconds 10 *> $log
    $code = $LASTEXITCODE
    Write-Host "controls exit=$code (a non-zero exit is the demonstration, not a failure of this script)"
}
finally {
    foreach ($job in $jobs) {
        Stop-Process -Id $job.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host 'burners stopped'
}

Get-Content $log
exit 0
