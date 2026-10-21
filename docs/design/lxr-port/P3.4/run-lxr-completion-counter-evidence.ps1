# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$WindowsRuntimeRoot,
    [Parameter(Mandatory)]
    [string]$LinuxRuntimeRoot,
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [int]$LaunchCount = 3,
    [int]$WarmupCycles = 1,
    [int]$MeasuredCycles = 8,
    [int]$PacketCount = 32
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
$WindowsRuntimeRoot = (Resolve-Path -LiteralPath $WindowsRuntimeRoot).ProviderPath
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P3.4\evidence\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

function Convert-ToWslPath([string]$Path) {
    $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
    if ($resolved -notmatch '^([A-Za-z]):\\(.*)$') {
        throw "Only absolute Windows drive paths can be converted to WSL: '$resolved'."
    }
    return '/mnt/' + $Matches[1].ToLowerInvariant() + '/' + $Matches[2].Replace('\', '/')
}

$repoLinux = Convert-ToWslPath $RepositoryRoot
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$windowsValidation = Join-Path $OutputDirectory 'windows-validation'
$linuxValidation = Join-Path $OutputDirectory 'linux-validation'
$windowsSmoke = Join-Path $OutputDirectory 'windows-runtime'
$linuxSmoke = Join-Path $OutputDirectory 'linux-runtime'
$benchmark = Join-Path $OutputDirectory 'benchmark'

& (Join-Path $scriptRoot 'run-lxr-completion-counter-validation.ps1') `
    -RepositoryRoot $RepositoryRoot -OutputDirectory $windowsValidation
$linuxValidationWsl = Convert-ToWslPath $linuxValidation
& wsl.exe --cd /root bash `
    "$repoLinux/docs/design/lxr-port/P3.4/run-lxr-completion-counter-validation.sh" `
    $repoLinux $linuxValidationWsl
if ($LASTEXITCODE -ne 0) {
    throw 'Linux completion-counter validation failed.'
}

& (Join-Path $scriptRoot 'run-lxr-completion-counter-runtime-smoke.ps1') `
    -RepositoryRoot $RepositoryRoot -RuntimeRoot $WindowsRuntimeRoot `
    -OutputDirectory $windowsSmoke
$managedAssembly = Convert-ToWslPath (
    Join-Path $windowsSmoke 'managed\runtime-smoke.dll')
$linuxSmokeWsl = Convert-ToWslPath $linuxSmoke
& wsl.exe --cd /root bash `
    "$repoLinux/docs/design/lxr-port/P3.4/run-lxr-completion-counter-runtime-smoke.sh" `
    $LinuxRuntimeRoot $managedAssembly $linuxSmokeWsl
if ($LASTEXITCODE -ne 0) {
    throw 'Linux completion-counter runtime smoke failed.'
}

& (Join-Path $scriptRoot 'run-lxr-completion-counter-benchmark.ps1') `
    -RepositoryRoot $RepositoryRoot -OutputDirectory $benchmark `
    -LaunchCount $LaunchCount -WarmupCycles $WarmupCycles `
    -MeasuredCycles $MeasuredCycles -PacketCount $PacketCount

@(
    Import-Csv (Join-Path $windowsValidation 'validation-summary.csv')
    Import-Csv (Join-Path $linuxValidation 'validation-summary.csv')
) | Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation
@(
    Import-Csv (Join-Path $windowsSmoke 'runtime-smoke-summary.csv')
    Import-Csv (Join-Path $linuxSmoke 'runtime-smoke-summary.csv')
) | Export-Csv (Join-Path $OutputDirectory 'runtime-smoke-summary.csv') -NoTypeInformation
foreach ($name in @(
    'benchmark-raw.csv',
    'benchmark-summary.csv',
    'benchmark-controls.csv',
    'benchmark-invocation.csv')) {
    Copy-Item -LiteralPath (Join-Path $benchmark $name) -Destination (
        Join-Path $OutputDirectory $name)
}
[pscustomobject][ordered]@{
    repository_root = $RepositoryRoot
    windows_runtime_root = $WindowsRuntimeRoot
    linux_runtime_root = $LinuxRuntimeRoot
    launch_count = $LaunchCount
    warmup_cycles = $WarmupCycles
    measured_cycles = $MeasuredCycles
    packet_count = $PacketCount
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'invocation.csv') -NoTypeInformation
Write-Host 'PASS: P3.4 full evidence wrapper'
Write-Host "Output: $OutputDirectory"
