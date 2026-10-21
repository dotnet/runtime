# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [Parameter(Mandatory)]
    [string]$RuntimeRoot,
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
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts\P3.1\runtime-smoke\windows-x64'
} else {
    $OutputDirectory = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
}
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "OutputDirectory already exists: '$OutputDirectory'."
}

$required = @(
    (Join-Path $RepositoryRoot '.dotnet\dotnet.exe'),
    (Join-Path $RuntimeRoot 'corerun.exe'),
    (Join-Path $RuntimeRoot 'coreclr.dll'),
    (Join-Path $RuntimeRoot 'clrgc.dll'),
    (Join-Path $RuntimeRoot 'System.Private.CoreLib.dll'),
    (Join-Path $RuntimeRoot 'System.Runtime.dll'),
    (Join-Path $RuntimeRoot 'System.Console.dll')
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Runtime smoke input is missing: '$path'."
    }
}

$managedOutput = Join-Path $OutputDirectory 'managed'
New-Item -ItemType Directory -Path $managedOutput | Out-Null
& (Join-Path $RepositoryRoot '.dotnet\dotnet.exe') build `
    (Join-Path $scriptRoot 'runtime-smoke\runtime-smoke.csproj') `
    -c Release `
    --nologo `
    --output $managedOutput `
    *> (Join-Path $OutputDirectory 'managed-build.log')
if ($LASTEXITCODE -ne 0) {
    throw "Managed runtime smoke build failed with exit code $LASTEXITCODE."
}

$rows = [Collections.Generic.List[object]]::new()
$corerun = Join-Path $RuntimeRoot 'corerun.exe'
$assembly = Join-Path $managedOutput 'runtime-smoke.dll'
$savedGcPath = $env:DOTNET_GCPath
$savedGcServer = $env:DOTNET_gcServer
try {
    foreach ($linkage in @('linked', 'standalone')) {
        $libraryName = if ($linkage -eq 'linked') { 'coreclr.dll' } else { 'clrgc.dll' }
        $library = Join-Path $RuntimeRoot $libraryName
        if ($linkage -eq 'standalone') {
            $env:DOTNET_GCPath = $library
        } else {
            Remove-Item Env:\DOTNET_GCPath -ErrorAction SilentlyContinue
        }

        foreach ($gcMode in @('Workstation', 'Server')) {
            $env:DOTNET_gcServer = if ($gcMode -eq 'Server') { '1' } else { '0' }
            $label = "$linkage-$gcMode"
            $log = Join-Path $OutputDirectory "$label.log"

            & $corerun $assembly $library $label *> $log
            $exitCode = $LASTEXITCODE
            $marker = "P3.1-SCHEDULER-SMOKE $label PASS"
            $markerSeen = (Get-Content -LiteralPath $log -Raw).Contains($marker)
            if ($exitCode -ne 0 -or -not $markerSeen) {
                throw "Runtime smoke '$label' failed with exit code $exitCode."
            }
            $rows.Add([pscustomobject][ordered]@{
                platform = 'windows-x64'
                linkage = $linkage
                gc_mode = $gcMode
                exit_code = $exitCode
                marker = $marker
                result = 'PASS'
            })
        }
    }
} finally {
    $env:DOTNET_GCPath = $savedGcPath
    $env:DOTNET_gcServer = $savedGcServer
}

$rows | Export-Csv (Join-Path $OutputDirectory 'runtime-smoke-summary.csv') -NoTypeInformation
Write-Host "PASS: $($rows.Count) P3.1 Windows runtime smoke rows"
