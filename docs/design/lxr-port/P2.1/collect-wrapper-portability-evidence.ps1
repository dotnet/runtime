# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$CrossCwdRoot,
    [Parameter(Mandatory)]
    [string]$PublicWrapperRoot,
    [Parameter(Mandatory)]
    [string]$PublicWrapperLog,
    [Parameter(Mandatory)]
    [string]$CoreRootControlRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $scriptRoot 'raw'
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
$logs = Join-Path $OutputDirectory 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$CrossCwdRoot = (Resolve-Path -LiteralPath $CrossCwdRoot).ProviderPath
$PublicWrapperRoot = (Resolve-Path -LiteralPath $PublicWrapperRoot).ProviderPath
$PublicWrapperLog = (Resolve-Path -LiteralPath $PublicWrapperLog).ProviderPath
$CoreRootControlRoot = (Resolve-Path -LiteralPath $CoreRootControlRoot).ProviderPath

$crossSummary = @(Import-Csv (Join-Path $CrossCwdRoot 'cross-cwd-summary.csv'))
$crossIdentity = @(Import-Csv (Join-Path $CrossCwdRoot 'benchmark\benchmark-identity.csv'))
$crossControls = @(Import-Csv (Join-Path $CrossCwdRoot 'benchmark\benchmark-controls.csv'))
if (($crossSummary.Count -ne 1) -or
    ($crossIdentity.Count -ne 1) -or
    ([int]$crossIdentity[0].rows -ne 45) -or
    ($crossControls.Count -ne 3) -or
    (@($crossControls | Where-Object result -ne 'PASS').Count -ne 0)) {
    throw 'Cross-CWD source evidence is incomplete.'
}
$crossSummary | Export-Csv (Join-Path $OutputDirectory 'cross-cwd-summary.csv') -NoTypeInformation
Copy-Item -LiteralPath (Join-Path $CrossCwdRoot $crossSummary[0].log) `
    -Destination (Join-Path $logs 'cross-cwd-benchmark.log') -Force

$publicSteps = @(Import-Csv (Join-Path $PublicWrapperRoot 'full-evidence-summary.csv'))
$publicIdentity = Get-Content (Join-Path $PublicWrapperRoot 'identity.json') -Raw |
    ConvertFrom-Json
$publicBenchmark = @(Import-Csv (
    Join-Path $PublicWrapperRoot 'benchmark\benchmark-identity.csv'))
$publicControls = @(Import-Csv (
    Join-Path $PublicWrapperRoot 'benchmark\benchmark-controls.csv'))
$publicLogText = Get-Content -LiteralPath $PublicWrapperLog -Raw
$callerMatch = [regex]::Match($publicLogText, '(?m)^OUTER_CALLER=(.+)$')
if (($publicSteps.Count -ne 4) -or
    (@($publicSteps | Where-Object result -ne 'PASS').Count -ne 0) -or
    ($publicBenchmark.Count -ne 1) -or
    ([int]$publicBenchmark[0].rows -ne 45) -or
    ($publicControls.Count -ne 3) -or
    (@($publicControls | Where-Object result -ne 'PASS').Count -ne 0) -or
    (-not $callerMatch.Success) -or
    ($publicLogText -notmatch 'PASS: full P2.1 evidence path \(4 steps\)')) {
    throw 'Public wrapper source evidence is incomplete.'
}
$publicSteps |
    Export-Csv (Join-Path $OutputDirectory 'public-wrapper-smoke-summary.csv') -NoTypeInformation
Copy-Item -LiteralPath $PublicWrapperLog `
    -Destination (Join-Path $logs 'public-full-evidence-wrapper.log') -Force
[pscustomobject][ordered]@{
    caller = $callerMatch.Groups[1].Value.Trim()
    repository_root = $publicIdentity.repository_root
    runtime_root = $publicIdentity.runtime_root
    benchmark_rows = [int]$publicBenchmark[0].rows
    controls = $publicControls.Count
    launch_count = [int]$publicBenchmark[0].launch_count
    warmup_count = [int]$publicBenchmark[0].warmup_count
    iteration_count = [int]$publicBenchmark[0].iteration_count
    result = 'PASS'
    log = 'public-full-evidence-wrapper.log'
} | Export-Csv (Join-Path $OutputDirectory 'public-wrapper-invocation.csv') -NoTypeInformation

$coreRootSummary = @(Import-Csv (
    Join-Path $CoreRootControlRoot 'core-root-control-summary.csv'))
$coreRootControlLog = Join-Path $CoreRootControlRoot 'invalid-coreroot-control.log'
$coreRootLogText = Get-Content -LiteralPath $coreRootControlLog -Raw
if (($coreRootSummary.Count -ne 1) -or
    ([int]$coreRootSummary[0].exit_code -eq 0) -or
    ($coreRootLogText -notmatch 'RuntimeRoot must be a complete CoreRoot') -or
    ($coreRootLogText -notmatch 'System\.Runtime\.dll')) {
    throw 'Incomplete CoreRoot source control did not fail as expected.'
}
Copy-Item -LiteralPath $coreRootControlLog `
    -Destination (Join-Path $logs 'invalid-coreroot-control.log') -Force
[pscustomobject][ordered]@{
    missing_file = $coreRootSummary[0].missing_file
    exit_code = $coreRootSummary[0].exit_code
    result = $coreRootSummary[0].result
    log = 'invalid-coreroot-control.log'
} | Export-Csv (Join-Path $OutputDirectory 'core-root-control-summary.csv') -NoTypeInformation

Write-Host 'Collected cross-CWD and CoreRoot portability evidence.'
