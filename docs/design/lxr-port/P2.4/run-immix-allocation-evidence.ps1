# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$WindowsRuntimeRoot,
    [Parameter(Mandatory)]
    [string]$LinuxRuntimeRoot,
    [Parameter(Mandatory)]
    [string]$LinuxCombinedRuntimeRoot,
    [string]$RepositoryRoot,
    [string]$OutputDirectory,
    [ValidateRange(1, 10)]
    [int]$LaunchCount = 3,
    [ValidateRange(1, 100)]
    [int]$WarmupCount = 8,
    [ValidateRange(1, 100)]
    [int]$IterationCount = 20
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot

function Resolve-PhysicalWindowsPath([string]$Path) {
    $resolved = if (Test-Path -LiteralPath $Path) {
        (Resolve-Path -LiteralPath $Path).ProviderPath
    } else {
        $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
    }
    $drive = [IO.Path]::GetPathRoot($resolved).TrimEnd('\')
    $mapping = @(subst.exe | Where-Object {
        $_ -match "^$([regex]::Escape($drive))\\: => "
    })
    if ($mapping.Count -gt 1) {
        throw "Multiple SUBST mappings were reported for '$drive'."
    }
    if ($mapping.Count -eq 1) {
        $target = (($mapping[0] -split '=>', 2)[1]).Trim().TrimEnd('\')
        $relative = $resolved.Substring([IO.Path]::GetPathRoot($resolved).Length)
        return [IO.Path]::GetFullPath((Join-Path $target $relative))
    }
    return $resolved
}

function Convert-ToWslPath([string]$Path) {
    $resolved = Resolve-PhysicalWindowsPath $Path
    if ($resolved -notmatch '^([A-Za-z]):\\(.*)$') {
        throw "Only absolute Windows drive paths can be converted: '$resolved'."
    }
    return '/mnt/' + $Matches[1].ToLowerInvariant() + '/' +
        $Matches[2].Replace('\', '/')
}

if (-not $RepositoryRoot) {
    $RepositoryRoot = Resolve-PhysicalWindowsPath (
        Join-Path $scriptRoot '..\..\..\..')
} else {
    $RepositoryRoot = Resolve-PhysicalWindowsPath $RepositoryRoot
}
$WindowsRuntimeRoot = Resolve-PhysicalWindowsPath $WindowsRuntimeRoot
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.4\evidence\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$required = @(
    (Join-Path $WindowsRuntimeRoot 'corerun.exe'),
    (Join-Path $WindowsRuntimeRoot 'coreclr.dll'),
    (Join-Path $WindowsRuntimeRoot 'clrgc.dll'),
    (Join-Path $WindowsRuntimeRoot 'clrjit.dll'),
    (Join-Path $WindowsRuntimeRoot 'System.Private.CoreLib.dll'),
    (Join-Path $scriptRoot 'run-immix-allocation-validation.ps1'),
    (Join-Path $scriptRoot 'run-immix-allocation-validation.sh'),
    (Join-Path $scriptRoot 'run-immix-allocation-runtime-smoke.ps1'),
    (Join-Path $scriptRoot 'run-immix-allocation-runtime-smoke.sh'),
    (Join-Path $scriptRoot 'run-immix-allocation-combined-exports.sh'),
    (Join-Path $scriptRoot 'run-immix-allocation-benchmark.ps1')
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required evidence input is missing: '$path'."
    }
}
foreach ($path in @(
    "$LinuxRuntimeRoot/corerun",
    "$LinuxRuntimeRoot/libcoreclr.so",
    "$LinuxRuntimeRoot/libclrgc.so",
    "$LinuxRuntimeRoot/libclrjit.so",
    "$LinuxRuntimeRoot/System.Private.CoreLib.dll",
    "$LinuxRuntimeRoot/System.Runtime.dll",
    "$LinuxRuntimeRoot/System.Console.dll",
    "$LinuxCombinedRuntimeRoot/libcoreclr.so")) {
    & wsl.exe --cd /root test -f $path
    if ($LASTEXITCODE -ne 0) {
        throw "Linux evidence input is missing: '$path'."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$windowsValidation = Join-Path $OutputDirectory 'windows-validation'
$linuxValidation = Join-Path $OutputDirectory 'linux-validation'
$windowsRuntime = Join-Path $OutputDirectory 'windows-runtime'
$linuxRuntime = Join-Path $OutputDirectory 'linux-runtime'
$combined = Join-Path $OutputDirectory 'combined-exports'
$benchmark = Join-Path $OutputDirectory 'benchmark'

& (Join-Path $scriptRoot 'run-immix-allocation-validation.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $windowsValidation

$repositoryLinux = Convert-ToWslPath $RepositoryRoot
$linuxValidationOutput = Convert-ToWslPath $linuxValidation
& wsl.exe --cd /root bash `
    "$repositoryLinux/docs/design/lxr-port/P2.4/run-immix-allocation-validation.sh" `
    $repositoryLinux `
    $linuxValidationOutput
if ($LASTEXITCODE -ne 0) {
    throw 'Linux Immix allocation validation failed.'
}

& (Join-Path $scriptRoot 'run-immix-allocation-runtime-smoke.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -RuntimeRoot $WindowsRuntimeRoot `
    -OutputDirectory $windowsRuntime

$managedAssembly = Join-Path $windowsRuntime 'managed\runtime-smoke.dll'
& wsl.exe --cd /root bash `
    "$repositoryLinux/docs/design/lxr-port/P2.4/run-immix-allocation-runtime-smoke.sh" `
    $LinuxRuntimeRoot `
    (Convert-ToWslPath $managedAssembly) `
    (Convert-ToWslPath $linuxRuntime)
if ($LASTEXITCODE -ne 0) {
    throw 'Linux Immix allocation runtime smoke failed.'
}

& wsl.exe --cd /root bash `
    "$repositoryLinux/docs/design/lxr-port/P2.4/run-immix-allocation-combined-exports.sh" `
    "$LinuxCombinedRuntimeRoot/libcoreclr.so" `
    (Convert-ToWslPath $combined)
if ($LASTEXITCODE -ne 0) {
    throw 'Linux combined-export validation failed.'
}

& (Join-Path $scriptRoot 'run-immix-allocation-benchmark.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $benchmark `
    -LaunchCount $LaunchCount `
    -WarmupCount $WarmupCount `
    -IterationCount $IterationCount

@(
    Import-Csv (Join-Path $windowsValidation 'validation-summary.csv')
    Import-Csv (Join-Path $linuxValidation 'validation-summary.csv')
) | Export-Csv (
    Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation
@(
    Import-Csv (Join-Path $windowsRuntime 'runtime-smoke-summary.csv')
    Import-Csv (Join-Path $linuxRuntime 'runtime-smoke-summary.csv')
) | Export-Csv (
    Join-Path $OutputDirectory 'runtime-smoke-summary.csv') -NoTypeInformation
Copy-Item (Join-Path $combined 'combined-export-summary.csv') $OutputDirectory
foreach ($name in @(
    'benchmark-controls.csv',
    'benchmark-identity.csv',
    'benchmark-raw.csv')) {
    Copy-Item (Join-Path $benchmark $name) $OutputDirectory
}

[pscustomobject][ordered]@{
    repository_root = $RepositoryRoot
    windows_runtime_root = $WindowsRuntimeRoot
    linux_runtime_root = $LinuxRuntimeRoot
    linux_combined_runtime_root = $LinuxCombinedRuntimeRoot
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    result = 'PASS'
} | Export-Csv (
    Join-Path $OutputDirectory 'invocation.csv') -NoTypeInformation

Write-Host 'PASS: P2.4 full evidence wrapper'
Write-Host "Output: $OutputDirectory"
