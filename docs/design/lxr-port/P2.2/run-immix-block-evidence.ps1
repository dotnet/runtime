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

if (-not $RepositoryRoot) {
    $RepositoryRoot = Resolve-PhysicalWindowsPath (Join-Path $scriptRoot '..\..\..\..')
} else {
    $RepositoryRoot = Resolve-PhysicalWindowsPath $RepositoryRoot
}
$WindowsRuntimeRoot = Resolve-PhysicalWindowsPath $WindowsRuntimeRoot
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.2\evidence\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

function Assert-WindowsAmd64Binary([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    try {
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            if ($reader.PEHeaders.CoffHeader.Machine.ToString() -ne 'Amd64') {
                throw "Windows runtime binary is not AMD64: '$Path'."
            }
        } finally {
            $reader.Dispose()
        }
    } finally {
        $stream.Dispose()
    }
}

function Convert-ToWslPath([string]$Path) {
    $resolved = Resolve-PhysicalWindowsPath $Path
    if ($resolved -notmatch '^([A-Za-z]):\\(.*)$') {
        throw "Only absolute Windows drive paths can be converted to WSL: '$resolved'."
    }

    return '/mnt/' + $Matches[1].ToLowerInvariant() + '/' + $Matches[2].Replace('\', '/')
}

$requiredWindowsPaths = @(
    (Join-Path $RepositoryRoot '.dotnet\dotnet.exe'),
    (Join-Path $WindowsRuntimeRoot 'corerun.exe'),
    (Join-Path $WindowsRuntimeRoot 'coreclr.dll'),
    (Join-Path $WindowsRuntimeRoot 'clrgc.dll'),
    (Join-Path $scriptRoot 'run-immix-block-validation.ps1'),
    (Join-Path $scriptRoot 'run-immix-block-validation.sh'),
    (Join-Path $scriptRoot 'run-immix-block-runtime-smoke.ps1'),
    (Join-Path $scriptRoot 'run-immix-block-runtime-smoke.sh'),
    (Join-Path $scriptRoot 'run-immix-block-combined-exports.sh'),
    (Join-Path $scriptRoot 'run-immix-block-benchmark.ps1')
)
foreach ($path in $requiredWindowsPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required evidence input is missing: '$path'."
    }
}
Assert-WindowsAmd64Binary (Join-Path $WindowsRuntimeRoot 'coreclr.dll')
Assert-WindowsAmd64Binary (Join-Path $WindowsRuntimeRoot 'clrgc.dll')
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists; caller-owned output is not modified: '$OutputDirectory'."
}

$linuxRequired = @(
    "$LinuxRuntimeRoot/corerun",
    "$LinuxRuntimeRoot/libcoreclr.so",
    "$LinuxRuntimeRoot/libclrgc.so",
    "$LinuxRuntimeRoot/libclrjit.so",
    "$LinuxRuntimeRoot/System.Private.CoreLib.dll",
    "$LinuxRuntimeRoot/System.Runtime.dll",
    "$LinuxRuntimeRoot/System.Console.dll"
)
foreach ($path in $linuxRequired) {
    & wsl.exe --cd /root test -f $path
    if ($LASTEXITCODE -ne 0) {
        throw "Linux RuntimeRoot is incomplete; missing '$path'."
    }
    & wsl.exe --cd /root test -f "$LinuxCombinedRuntimeRoot/libcoreclr.so"
    if ($LASTEXITCODE -ne 0) {
        throw "Combined-feature Linux RuntimeRoot is incomplete: '$LinuxCombinedRuntimeRoot'."
    }
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$windowsValidation = Join-Path $OutputDirectory 'windows-validation'
$linuxValidation = Join-Path $OutputDirectory 'linux-validation'
$windowsRuntime = Join-Path $OutputDirectory 'windows-runtime'
$linuxRuntime = Join-Path $OutputDirectory 'linux-runtime'
$combinedExports = Join-Path $OutputDirectory 'combined-exports'
$benchmark = Join-Path $OutputDirectory 'benchmark'

& (Join-Path $scriptRoot 'run-immix-block-validation.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $windowsValidation

$repositoryRootLinux = Convert-ToWslPath $RepositoryRoot
$linuxValidationScript =
    "$repositoryRootLinux/docs/design/lxr-port/P2.2/run-immix-block-validation.sh"
$linuxValidationOutput = Convert-ToWslPath $linuxValidation
& wsl.exe --cd /root bash $linuxValidationScript $repositoryRootLinux $linuxValidationOutput
if ($LASTEXITCODE -ne 0) {
    throw 'Linux Immix block validation failed.'
}

& (Join-Path $scriptRoot 'run-immix-block-runtime-smoke.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -RuntimeRoot $WindowsRuntimeRoot `
    -OutputDirectory $windowsRuntime

$managedAssembly = Join-Path $windowsRuntime 'managed\runtime-smoke.dll'
$managedAssemblyLinux = Convert-ToWslPath $managedAssembly
$linuxRuntimeScript =
    "$repositoryRootLinux/docs/design/lxr-port/P2.2/run-immix-block-runtime-smoke.sh"
$linuxRuntimeOutput = Convert-ToWslPath $linuxRuntime
& wsl.exe --cd /root bash `
    $linuxRuntimeScript `
    $LinuxRuntimeRoot `
    $managedAssemblyLinux `
    $linuxRuntimeOutput
if ($LASTEXITCODE -ne 0) {
    throw 'Linux Immix block runtime smoke failed.'
}

$combinedExportScript =
    "$repositoryRootLinux/docs/design/lxr-port/P2.2/run-immix-block-combined-exports.sh"
$combinedExportOutput = Convert-ToWslPath $combinedExports
& wsl.exe --cd /root bash `
    $combinedExportScript `
    "$LinuxCombinedRuntimeRoot/libcoreclr.so" `
    $combinedExportOutput
if ($LASTEXITCODE -ne 0) {
    throw 'Linux combined-feature export validation failed.'
}

& (Join-Path $scriptRoot 'run-immix-block-benchmark.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $benchmark `
    -LaunchCount $LaunchCount `
    -WarmupCount $WarmupCount `
    -IterationCount $IterationCount

$validationRows = @(
    Import-Csv (Join-Path $windowsValidation 'validation-summary.csv')
    Import-Csv (Join-Path $linuxValidation 'validation-summary.csv')
)
$validationRows | Export-Csv `
    (Join-Path $OutputDirectory 'validation-summary.csv') `
    -NoTypeInformation

$windowsRuntimeRows = @(
    Import-Csv (Join-Path $windowsRuntime 'runtime-smoke-summary.csv') |
        ForEach-Object {
            [pscustomobject][ordered]@{
                platform = 'windows-x64'
                linkage = $_.linkage
                gc_mode = $_.gc_mode
                exit_code = $_.exit_code
                result = $_.result
                log = $_.log
            }
        }
)
$linuxRuntimeRows = @(
    Import-Csv (Join-Path $linuxRuntime 'runtime-smoke-summary.csv')
)
@($windowsRuntimeRows + $linuxRuntimeRows) | Export-Csv `
    (Join-Path $OutputDirectory 'runtime-smoke-summary.csv') `
    -NoTypeInformation
Copy-Item `
    (Join-Path $combinedExports 'combined-export-summary.csv') `
    (Join-Path $OutputDirectory 'combined-export-summary.csv')

[pscustomobject][ordered]@{
    repository_root = $RepositoryRoot
    windows_runtime_root = $WindowsRuntimeRoot
    linux_runtime_root = $LinuxRuntimeRoot
    linux_combined_runtime_root = $LinuxCombinedRuntimeRoot
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'invocation.csv') -NoTypeInformation

Write-Host 'PASS: P2.2 full evidence wrapper'
Write-Host "Output: $OutputDirectory"
