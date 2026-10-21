# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 1,
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
        'artifacts\P2.1\cross-cwd\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

$caller = Join-Path ([IO.Path]::GetTempPath()) (
    'p21-unrelated-caller-' + [guid]::NewGuid().ToString('N'))
$benchmark = Join-Path $OutputDirectory 'benchmark'
$log = Join-Path $OutputDirectory 'cross-cwd-benchmark.log'
New-Item -ItemType Directory -Path $caller -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$savedLocation = Get-Location
try {
    Set-Location -LiteralPath $caller
    & (Join-Path $scriptRoot 'run-side-metadata-benchmark.ps1') `
        -RepositoryRoot $RepositoryRoot `
        -OutputDirectory $benchmark `
        -LaunchCount $LaunchCount `
        -WarmupCount $WarmupCount `
        -IterationCount $IterationCount *> $log
    if ($LASTEXITCODE -ne 0) {
        throw "Cross-CWD benchmark failed. See $log."
    }
} finally {
    Set-Location -LiteralPath $savedLocation.Path
}

$identity = @(Import-Csv (Join-Path $benchmark 'benchmark-identity.csv'))
$controls = @(Import-Csv (Join-Path $benchmark 'benchmark-controls.csv'))
if (($identity.Count -ne 1) -or
    ([int]$identity[0].rows -ne 45) -or
    ($identity[0].result -ne 'PASS') -or
    ($controls.Count -ne 3) -or
    (@($controls | Where-Object result -ne 'PASS').Count -ne 0)) {
    throw 'Cross-CWD benchmark evidence is incomplete.'
}

[pscustomobject][ordered]@{
    caller = $caller
    repository_root = (Resolve-Path $RepositoryRoot).ProviderPath
    benchmark_rows = [int]$identity[0].rows
    controls = $controls.Count
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    result = 'PASS'
    log = [IO.Path]::GetFileName($log)
} | Export-Csv (Join-Path $OutputDirectory 'cross-cwd-summary.csv') -NoTypeInformation

Write-Host 'PASS: cross-CWD benchmark executed 45 rows and 3 controls'
Write-Host "Caller: $caller"
Write-Host "Output: $OutputDirectory"
