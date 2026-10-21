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
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(0.1, 60.0)]
    [double]$WarmupSeconds = 1.0,
    [ValidateRange(0.5, 120.0)]
    [double]$DurationSeconds = 3.0
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
        'artifacts\P3.1\evidence\' + [guid]::NewGuid().ToString('N'))
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
foreach ($path in @(
    "$LinuxRuntimeRoot/corerun",
    "$LinuxRuntimeRoot/libcoreclr.so",
    "$LinuxRuntimeRoot/libclrgc.so",
    "$LinuxRuntimeRoot/libclrjit.so",
    "$LinuxRuntimeRoot/System.Private.CoreLib.dll",
    "$LinuxRuntimeRoot/System.Runtime.dll",
    "$LinuxRuntimeRoot/System.Console.dll")) {
    & wsl.exe --cd /root test -f $path
    if ($LASTEXITCODE -ne 0) {
        throw "Linux runtime root is incomplete: '$path'."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$windowsValidation = Join-Path $OutputDirectory 'windows-validation'
$linuxValidation = Join-Path $OutputDirectory 'linux-validation'
$windowsSmoke = Join-Path $OutputDirectory 'windows-runtime'
$linuxSmoke = Join-Path $OutputDirectory 'linux-runtime'
$benchmark = Join-Path $OutputDirectory 'benchmark'

& (Join-Path $scriptRoot 'run-lxr-work-packet-validation.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $windowsValidation

$linuxValidationOutput = Convert-ToWslPath $linuxValidation
& wsl.exe --cd /root bash `
    "$repoLinux/docs/design/lxr-port/P3.1/run-lxr-work-packet-validation.sh" `
    $repoLinux `
    $linuxValidationOutput
if ($LASTEXITCODE -ne 0) {
    throw 'Linux native scheduler validation failed.'
}

& (Join-Path $scriptRoot 'run-lxr-work-packet-runtime-smoke.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -RuntimeRoot $WindowsRuntimeRoot `
    -OutputDirectory $windowsSmoke

$managedAssembly = Convert-ToWslPath (
    Join-Path $windowsSmoke 'managed\runtime-smoke.dll')
$linuxSmokeOutput = Convert-ToWslPath $linuxSmoke
& wsl.exe --cd /root bash `
    "$repoLinux/docs/design/lxr-port/P3.1/run-lxr-work-packet-runtime-smoke.sh" `
    $LinuxRuntimeRoot `
    $managedAssembly `
    $linuxSmokeOutput
if ($LASTEXITCODE -ne 0) {
    throw 'Linux runtime scheduler smoke failed.'
}

& (Join-Path $scriptRoot 'run-lxr-work-packet-benchmark.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $benchmark `
    -LaunchCount $LaunchCount `
    -WarmupSeconds $WarmupSeconds `
    -DurationSeconds $DurationSeconds

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
    warmup_seconds = $WarmupSeconds
    duration_seconds = $DurationSeconds
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'invocation.csv') -NoTypeInformation

Write-Host 'PASS: P3.1 full evidence wrapper'
Write-Host "Output: $OutputDirectory"
