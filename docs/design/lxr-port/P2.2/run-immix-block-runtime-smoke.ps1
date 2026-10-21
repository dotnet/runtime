# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$RuntimeRoot,
    [string]$RepositoryRoot,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot
if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).ProviderPath
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
}
$RuntimeRoot = (Resolve-Path -LiteralPath $RuntimeRoot).ProviderPath
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $RepositoryRoot (
        'artifacts\P2.2\runtime-smoke\' + [guid]::NewGuid().ToString('N'))
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}

$dotnet = Join-Path $RepositoryRoot '.dotnet\dotnet.exe'
$corerun = Join-Path $RuntimeRoot 'corerun.exe'
$coreclr = Join-Path $RuntimeRoot 'coreclr.dll'
$standalone = Join-Path $RuntimeRoot 'clrgc.dll'
$project = Join-Path $scriptRoot 'runtime-smoke\runtime-smoke.csproj'
$managed = Join-Path $OutputDirectory 'managed'
$assembly = Join-Path $managed 'runtime-smoke.dll'
$requiredPaths = @(
    $dotnet,
    $corerun,
    $coreclr,
    $standalone,
    (Join-Path $RuntimeRoot 'clrjit.dll'),
    (Join-Path $RuntimeRoot 'System.Private.CoreLib.dll'),
    (Join-Path $RuntimeRoot 'System.Runtime.dll'),
    (Join-Path $RuntimeRoot 'System.Console.dll'),
    $project
)
foreach ($path in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw (
            "RuntimeRoot must be a complete CoreRoot and the smoke project must exist; " +
            "missing '$path'.")
    }
}

New-Item -ItemType Directory -Path $managed -Force | Out-Null
$managedBuildLog = Join-Path $OutputDirectory 'managed-build.log'
$savedErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & $dotnet build $project -c Release -o $managed --nologo *> $managedBuildLog
    $managedBuildExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
if ($managedBuildExitCode -ne 0) {
    throw "Unable to build the P2.2 runtime smoke. See $managedBuildLog."
}

$environmentNames = @(
    'DOTNET_gcServer',
    'DOTNET_ReadyToRun',
    'DOTNET_TieredCompilation',
    'P22_NATIVE_HOOK_LIBRARY',
    'DOTNET_GCPath'
)
$saved = @{}
foreach ($name in $environmentNames) {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}

$rows = [Collections.Generic.List[object]]::new()
try {
    $env:DOTNET_ReadyToRun = '0'
    $env:DOTNET_TieredCompilation = '0'
    foreach ($linkage in @('linked', 'standalone')) {
        if ($linkage -eq 'standalone') {
            $env:DOTNET_GCPath = $standalone
            $env:P22_NATIVE_HOOK_LIBRARY = $standalone
        } else {
            Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
            $env:P22_NATIVE_HOOK_LIBRARY = $coreclr
        }

        foreach ($gcMode in @('Workstation', 'Server')) {
            $env:DOTNET_gcServer = if ($gcMode -eq 'Server') { '1' } else { '0' }
            $id = "$linkage-$gcMode"
            $log = Join-Path $OutputDirectory "$id.log"
            try {
                $ErrorActionPreference = 'Continue'
                & $corerun $assembly *> $log
                $exitCode = $LASTEXITCODE
            } finally {
                $ErrorActionPreference = $savedErrorActionPreference
            }
            $output = Get-Content -LiteralPath $log -Raw
            if (($exitCode -ne 0) -or ($output -notmatch '(?m)^PASS:')) {
                throw "Runtime smoke failed: $id. See $log."
            }
            $rows.Add([pscustomobject][ordered]@{
                linkage = $linkage
                gc_mode = $gcMode
                exit_code = $exitCode
                result = 'PASS'
                log = [IO.Path]::GetFileName($log)
            })
        }
    }
} finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name])
    }
}

$rows | Export-Csv (Join-Path $OutputDirectory 'runtime-smoke-summary.csv') -NoTypeInformation
Write-Host "PASS: $($rows.Count) linked/standalone and GC-mode scenarios"
Write-Host "Output: $OutputDirectory"
