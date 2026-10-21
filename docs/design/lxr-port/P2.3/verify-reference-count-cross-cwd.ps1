# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Runs the P2.3 benchmark harness from an unrelated working directory.

.DESCRIPTION
The P2.3 harnesses resolve the repository, their own inputs, and their own
output locations from the script location and explicit parameters. This
control proves that by launching run-reference-count-benchmark.ps1 with the
current directory set to a directory that is not inside the repository, and
then requiring the resulting benchmark evidence to be complete.

.PARAMETER CallerDirectory
Optional unrelated working directory. When omitted a unique directory is
created under the user's temporary directory. It is never deleted, so the
caller can inspect it.
#>
[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [string]$CallerDirectory,
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
        'artifacts\P2.3\cross-cwd\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists; caller-owned output is not modified: '$OutputDirectory'."
}
if (-not $CallerDirectory) {
    $CallerDirectory = Join-Path ([IO.Path]::GetTempPath()) (
        'p23-unrelated-caller-' + [guid]::NewGuid().ToString('N'))
} else {
    $CallerDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($CallerDirectory)
}

$expectedRows = 57
$expectedControls = 3
$benchmark = Join-Path $OutputDirectory 'benchmark'
$log = Join-Path $OutputDirectory 'cross-cwd-benchmark.log'
New-Item -ItemType Directory -Path $CallerDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$savedLocation = Get-Location
try {
    Set-Location -LiteralPath $CallerDirectory
    & (Join-Path $scriptRoot 'run-reference-count-benchmark.ps1') `
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
    ([int]$identity[0].rows -ne $expectedRows) -or
    ([int]$identity[0].expected_rows -ne $expectedRows) -or
    ($identity[0].result -ne 'PASS') -or
    ($controls.Count -ne $expectedControls) -or
    (@($controls | Where-Object result -ne 'PASS').Count -ne 0)) {
    throw 'Cross-CWD benchmark evidence is incomplete.'
}

[pscustomobject][ordered]@{
    caller = $CallerDirectory
    repository_root = $RepositoryRoot
    benchmark_rows = [int]$identity[0].rows
    controls = $controls.Count
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    result = 'PASS'
    log = [IO.Path]::GetFileName($log)
} | Export-Csv (Join-Path $OutputDirectory 'cross-cwd-summary.csv') -NoTypeInformation

Write-Host "PASS: cross-CWD benchmark executed $expectedRows rows and $expectedControls controls"
Write-Host "Caller: $CallerDirectory"
Write-Host "Output: $OutputDirectory"
