# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

<#
.SYNOPSIS
Runs the complete P2.3 reference-count evidence set.

.DESCRIPTION
Orchestrates the existing P2.3 harnesses through explicit paths and
parameters:

  run-reference-count-validation.ps1      Windows x64 and x86 validators
  run-reference-count-validation.sh       Linux x64 validator (real WSL Unix)
  run-reference-count-runtime-smoke.ps1   Windows linked/standalone WKS/SVR
  run-reference-count-runtime-smoke.sh    Linux linked/standalone WKS/SVR
  run-reference-count-combined-exports.sh Combined opt-in exports and the
                                          default-build absence control
  run-reference-count-benchmark.ps1       Native + BenchmarkDotNet harness
  verify-reference-count-cross-cwd.ps1    Unrelated-working-directory control
  run-incomplete-coreroot-control.ps1     Incomplete Core_Root rejection
                                          control

It also records the SHA-256 identity of every Windows and Linux runtime
binary the evidence executed against, and copies the caller-supplied build,
platform, and attempt summaries into the output unchanged.

Every input must be complete before anything runs; the wrapper refuses an
existing output directory so caller-owned output is never modified, and all
child logs and output directories are preserved under -OutputDirectory.

Required parameters are validated in the body instead of being declared
Mandatory, so an incomplete invocation fails immediately with one explicit
message and never prompts.

This wrapper does not build the runtime. -WindowsRuntimeRoot,
-LinuxRuntimeRoot, -LinuxCombinedRuntimeRoot, and -LinuxDefaultRuntimeRoot
must already point at complete, separately built layouts.

.PARAMETER LinuxCombinedRuntimeRoot
A Linux runtime root built with the reference-count option together with the
other GC validation options, used for the combined-export check.

.PARAMETER LinuxDefaultRuntimeRoot
A Linux runtime root built with every GC validation option off, used for the
default-build export-absence control.

.PARAMETER InvalidWindowsRuntimeRoot
A Windows runtime root that is deliberately missing a required Core_Root file,
used by the incomplete-Core_Root rejection control.

.PARAMETER BuildSummary
Caller-supplied build-summary.csv. The build matrix spans machines, drives,
and architectures that this wrapper does not run, so its rows are recorded
externally and copied in unchanged.

.PARAMETER PlatformSummary
Caller-supplied platform-summary.csv, recorded externally for the same reason.

.PARAMETER AttemptSummary
Caller-supplied attempt-summary.csv recording the attempts, including failed
ones, behind the evidence.
#>
[CmdletBinding()]
param(
    [string]$WindowsRuntimeRoot,
    [string]$LinuxRuntimeRoot,
    [string]$LinuxCombinedRuntimeRoot,
    [string]$LinuxDefaultRuntimeRoot,
    [string]$InvalidWindowsRuntimeRoot,
    [string]$BuildSummary,
    [string]$PlatformSummary,
    [string]$AttemptSummary,
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

# The required inputs are validated here rather than declared Mandatory so an
# incomplete invocation always fails with one explicit message. A Mandatory
# parameter prompts an interactive host instead, which would stall an evidence
# run that is meant to be non-interactive and reproducible.
$requiredParameters = [ordered]@{
    WindowsRuntimeRoot = $WindowsRuntimeRoot
    LinuxRuntimeRoot = $LinuxRuntimeRoot
    LinuxCombinedRuntimeRoot = $LinuxCombinedRuntimeRoot
    LinuxDefaultRuntimeRoot = $LinuxDefaultRuntimeRoot
    InvalidWindowsRuntimeRoot = $InvalidWindowsRuntimeRoot
    BuildSummary = $BuildSummary
    PlatformSummary = $PlatformSummary
    AttemptSummary = $AttemptSummary
}
$missingParameters = @(
    $requiredParameters.GetEnumerator() |
        Where-Object { [string]::IsNullOrWhiteSpace($_.Value) } |
        ForEach-Object { $_.Key }
)
if ($missingParameters.Count -ne 0) {
    throw "Required evidence parameters are missing: $($missingParameters -join ', ')."
}

function Get-LinuxSha256([string]$Path) {
    $output = (& wsl.exe --cd /root sha256sum -- $Path | Out-String).Trim()
    if (($LASTEXITCODE -ne 0) -or ($output -notmatch '^([0-9a-f]{64})\s')) {
        throw "Unable to hash Linux evidence input: '$Path'."
    }

    return $Matches[1].ToUpperInvariant()
}

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
        throw "Only absolute Windows drive paths can be converted to WSL: '$resolved'."
    }

    return '/mnt/' + $Matches[1].ToLowerInvariant() + '/' + $Matches[2].Replace('\', '/')
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

function Assert-LinuxFile([string]$Path) {
    & wsl.exe --cd /root test -f $Path
    if ($LASTEXITCODE -ne 0) {
        throw "Required evidence input is missing: '$Path'."
    }
}

if (-not $RepositoryRoot) {
    $RepositoryRoot = Resolve-PhysicalWindowsPath (Join-Path $scriptRoot '..\..\..\..')
} else {
    $RepositoryRoot = Resolve-PhysicalWindowsPath $RepositoryRoot
}
$WindowsRuntimeRoot = Resolve-PhysicalWindowsPath $WindowsRuntimeRoot
$InvalidWindowsRuntimeRoot = Resolve-PhysicalWindowsPath $InvalidWindowsRuntimeRoot
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.3\evidence\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

$requiredWindowsPaths = @(
    (Join-Path $RepositoryRoot '.dotnet\dotnet.exe'),
    (Join-Path $RepositoryRoot 'eng\native\init-vs-env.cmd'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\reference_count.cpp'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\reference_count.h'),
    (Join-Path $RepositoryRoot 'src\coreclr\gc\referencecounttest.cpp'),
    (Join-Path $WindowsRuntimeRoot 'corerun.exe'),
    (Join-Path $WindowsRuntimeRoot 'coreclr.dll'),
    (Join-Path $WindowsRuntimeRoot 'clrgc.dll'),
    (Join-Path $WindowsRuntimeRoot 'clrjit.dll'),
    (Join-Path $WindowsRuntimeRoot 'System.Private.CoreLib.dll'),
    (Join-Path $WindowsRuntimeRoot 'System.Runtime.dll'),
    (Join-Path $WindowsRuntimeRoot 'System.Console.dll'),
    (Join-Path $scriptRoot 'reference-count-validation.cpp'),
    (Join-Path $scriptRoot 'reference-count-benchmark-native.cpp'),
    (Join-Path $scriptRoot 'run-reference-count-validation.ps1'),
    (Join-Path $scriptRoot 'run-reference-count-validation.sh'),
    (Join-Path $scriptRoot 'run-reference-count-runtime-smoke.ps1'),
    (Join-Path $scriptRoot 'run-reference-count-runtime-smoke.sh'),
    (Join-Path $scriptRoot 'run-reference-count-combined-exports.sh'),
    (Join-Path $scriptRoot 'run-reference-count-benchmark.ps1')
)
foreach ($path in $requiredWindowsPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required evidence input is missing: '$path'."
    }
}
$summaryInputs = @($BuildSummary, $PlatformSummary, $AttemptSummary)
foreach ($path in $summaryInputs) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required evidence summary is missing: '$path'."
    }
}
Assert-WindowsAmd64Binary (Join-Path $WindowsRuntimeRoot 'coreclr.dll')
Assert-WindowsAmd64Binary (Join-Path $WindowsRuntimeRoot 'clrgc.dll')

foreach ($name in @(
    'corerun',
    'libcoreclr.so',
    'libclrgc.so',
    'libclrjit.so',
    'System.Private.CoreLib.dll',
    'System.Runtime.dll',
    'System.Console.dll')) {
    Assert-LinuxFile "$LinuxRuntimeRoot/$name"
}
Assert-LinuxFile "$LinuxCombinedRuntimeRoot/libcoreclr.so"
Assert-LinuxFile "$LinuxDefaultRuntimeRoot/libcoreclr.so"

if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists; caller-owned output is not modified: '$OutputDirectory'."
}

New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$windowsValidation = Join-Path $OutputDirectory 'windows-validation'
$linuxValidation = Join-Path $OutputDirectory 'linux-validation'
$windowsRuntime = Join-Path $OutputDirectory 'windows-runtime'
$linuxRuntime = Join-Path $OutputDirectory 'linux-runtime'
$combinedExports = Join-Path $OutputDirectory 'combined-exports'
$benchmark = Join-Path $OutputDirectory 'benchmark'
$crossCwd = Join-Path $OutputDirectory 'cross-cwd'
$coreRootControl = Join-Path $OutputDirectory 'core-root-control'
$repositoryRootLinux = Convert-ToWslPath $RepositoryRoot
$runtimeIdentities = [Collections.Generic.List[object]]::new()
foreach ($item in @(
    @('windows-enabled-corerun', (Join-Path $WindowsRuntimeRoot 'corerun.exe')),
    @('windows-enabled-coreclr', (Join-Path $WindowsRuntimeRoot 'coreclr.dll')),
    @('windows-enabled-clrgc', (Join-Path $WindowsRuntimeRoot 'clrgc.dll')),
    @('windows-enabled-clrjit', (Join-Path $WindowsRuntimeRoot 'clrjit.dll')),
    @('windows-enabled-corelib', (Join-Path $WindowsRuntimeRoot 'System.Private.CoreLib.dll')))) {
    $runtimeIdentities.Add([pscustomobject][ordered]@{
        role = $item[0]
        path = $item[1]
        sha256 = (Get-FileHash -LiteralPath $item[1] -Algorithm SHA256).Hash
    })
}
foreach ($item in @(
    @('linux-enabled-corerun', "$LinuxRuntimeRoot/corerun"),
    @('linux-enabled-coreclr', "$LinuxRuntimeRoot/libcoreclr.so"),
    @('linux-enabled-clrgc', "$LinuxRuntimeRoot/libclrgc.so"),
    @('linux-enabled-clrjit', "$LinuxRuntimeRoot/libclrjit.so"),
    @('linux-enabled-corelib', "$LinuxRuntimeRoot/System.Private.CoreLib.dll"),
    @('linux-combined-coreclr', "$LinuxCombinedRuntimeRoot/libcoreclr.so"),
    @('linux-default-coreclr', "$LinuxDefaultRuntimeRoot/libcoreclr.so"),
    @('linux-default-clrgc', "$LinuxDefaultRuntimeRoot/libclrgc.so"),
    @('linux-default-coreclr-symbols', "$LinuxDefaultRuntimeRoot/libcoreclr.so.dbg"),
    @('linux-default-clrgc-symbols', "$LinuxDefaultRuntimeRoot/libclrgc.so.dbg"))) {
    $runtimeIdentities.Add([pscustomobject][ordered]@{
        role = $item[0]
        path = $item[1]
        sha256 = Get-LinuxSha256 $item[1]
    })
}
$runtimeIdentities |
    Export-Csv (Join-Path $OutputDirectory 'runtime-identities.csv') -NoTypeInformation

& (Join-Path $scriptRoot 'run-reference-count-validation.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $windowsValidation

$linuxValidationScript =
    "$repositoryRootLinux/docs/design/lxr-port/P2.3/run-reference-count-validation.sh"
& wsl.exe --cd /root bash `
    $linuxValidationScript `
    $repositoryRootLinux `
    (Convert-ToWslPath $linuxValidation)
if ($LASTEXITCODE -ne 0) {
    throw 'Linux reference-count validation failed.'
}

& (Join-Path $scriptRoot 'run-reference-count-runtime-smoke.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -RuntimeRoot $WindowsRuntimeRoot `
    -OutputDirectory $windowsRuntime

$managedAssembly = Join-Path $windowsRuntime 'managed\runtime-smoke.dll'
if (-not (Test-Path -LiteralPath $managedAssembly -PathType Leaf)) {
    throw "Required evidence input is missing: '$managedAssembly'."
}
$linuxRuntimeScript =
    "$repositoryRootLinux/docs/design/lxr-port/P2.3/run-reference-count-runtime-smoke.sh"
& wsl.exe --cd /root bash `
    $linuxRuntimeScript `
    $LinuxRuntimeRoot `
    (Convert-ToWslPath $managedAssembly) `
    (Convert-ToWslPath $linuxRuntime)
if ($LASTEXITCODE -ne 0) {
    throw 'Linux reference-count runtime smoke failed.'
}

$combinedExportScript =
    "$repositoryRootLinux/docs/design/lxr-port/P2.3/run-reference-count-combined-exports.sh"
& wsl.exe --cd /root bash `
    $combinedExportScript `
    "$LinuxCombinedRuntimeRoot/libcoreclr.so" `
    (Convert-ToWslPath $combinedExports) `
    "$LinuxDefaultRuntimeRoot/libcoreclr.so" `
    "$LinuxDefaultRuntimeRoot/libclrgc.so" `
    $repositoryRootLinux
if ($LASTEXITCODE -ne 0) {
    throw 'Linux combined-feature export validation failed.'
}

& (Join-Path $scriptRoot 'run-reference-count-benchmark.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $benchmark `
    -LaunchCount $LaunchCount `
    -WarmupCount $WarmupCount `
    -IterationCount $IterationCount

& (Join-Path $scriptRoot 'verify-reference-count-cross-cwd.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -OutputDirectory $crossCwd `
    -LaunchCount 1 `
    -WarmupCount $WarmupCount `
    -IterationCount $IterationCount

& (Join-Path $scriptRoot 'run-incomplete-coreroot-control.ps1') `
    -RepositoryRoot $RepositoryRoot `
    -InvalidRuntimeRoot $InvalidWindowsRuntimeRoot `
    -OutputDirectory $coreRootControl

$validationRows = @(
    Import-Csv (Join-Path $windowsValidation 'validation-summary.csv')
    Import-Csv (Join-Path $linuxValidation 'validation-summary.csv')
)
$validationRows |
    Select-Object platform, passed, total, checks, result |
    Export-Csv (Join-Path $OutputDirectory 'validation-summary.csv') -NoTypeInformation

$windowsRuntimeRows = @(
    Import-Csv (Join-Path $windowsRuntime 'runtime-smoke-summary.csv')
)
$linuxRuntimeRows = @(
    Import-Csv (Join-Path $linuxRuntime 'runtime-smoke-summary.csv')
)
@($windowsRuntimeRows + $linuxRuntimeRows) |
    Select-Object platform, linkage, gc_mode, exit_code, result, log |
    Export-Csv (Join-Path $OutputDirectory 'runtime-smoke-summary.csv') -NoTypeInformation

foreach ($name in @('combined-export-summary.csv', 'default-path-summary.csv')) {
    Copy-Item `
        (Join-Path $combinedExports $name) `
        (Join-Path $OutputDirectory $name)
}

# The benchmark harness emits one report per BenchmarkDotNet class. The width
# class is parameterized by LogReferenceCountBits and the contention class by
# WorkerCount, so the reports are classified by their own columns rather than
# by an ordering assumption.
$reports = @(
    Get-ChildItem -LiteralPath $benchmark -Filter 'benchmark-raw-*.csv' -File
)
if ($reports.Count -ne 2) {
    throw "Expected two benchmark reports, found $($reports.Count)."
}
$widthReport = $null
$contentionReport = $null
foreach ($report in $reports) {
    $columns = @(
        (Import-Csv -LiteralPath $report.FullName |
            Select-Object -First 1).PSObject.Properties.Name
    )
    if ($columns -contains 'LogReferenceCountBits') {
        $widthReport = $report.FullName
    } elseif ($columns -contains 'WorkerCount') {
        $contentionReport = $report.FullName
    }
}
if ((-not $widthReport) -or (-not $contentionReport)) {
    throw 'Benchmark reports could not be classified by their parameter columns.'
}
Copy-Item $widthReport (Join-Path $OutputDirectory 'benchmark-width-raw.csv')
Copy-Item $contentionReport (Join-Path $OutputDirectory 'benchmark-contention-raw.csv')
foreach ($name in @('benchmark-controls.csv', 'benchmark-identity.csv')) {
    Copy-Item `
        (Join-Path $benchmark $name) `
        (Join-Path $OutputDirectory $name)
}
Copy-Item $BuildSummary (Join-Path $OutputDirectory 'build-summary.csv')
Copy-Item $PlatformSummary (Join-Path $OutputDirectory 'platform-summary.csv')
Copy-Item $AttemptSummary (Join-Path $OutputDirectory 'attempt-summary.csv')
Copy-Item `
    (Join-Path $crossCwd 'cross-cwd-summary.csv') `
    (Join-Path $OutputDirectory 'cross-cwd-summary.csv')
Copy-Item `
    (Join-Path $coreRootControl 'core-root-control-summary.csv') `
    (Join-Path $OutputDirectory 'core-root-control-summary.csv')

[pscustomobject][ordered]@{
    repository_root = $RepositoryRoot
    windows_runtime_root = $WindowsRuntimeRoot
    linux_runtime_root = $LinuxRuntimeRoot
    linux_combined_runtime_root = $LinuxCombinedRuntimeRoot
    linux_default_runtime_root = $LinuxDefaultRuntimeRoot
    invalid_windows_runtime_root = $InvalidWindowsRuntimeRoot
    launch_count = $LaunchCount
    warmup_count = $WarmupCount
    iteration_count = $IterationCount
    validation_rows = $validationRows.Count
    runtime_rows = ($windowsRuntimeRows.Count + $linuxRuntimeRows.Count)
    result = 'PASS'
} | Export-Csv (Join-Path $OutputDirectory 'invocation.csv') -NoTypeInformation

Write-Host 'PASS: P2.3 full evidence wrapper'
Write-Host "Output: $OutputDirectory"
