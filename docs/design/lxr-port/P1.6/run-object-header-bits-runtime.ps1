# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [ValidateSet('Workstation', 'Server')]
    [string]$GCMode = 'Workstation'
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $RepositoryRoot) {
    $RepositoryRoot = (Resolve-Path (Join-Path $scriptRoot '..\..\..\..')).Path
}

$configuration = 'Checked'
$coreRoot = Join-Path $RepositoryRoot (
    "artifacts\tests\coreclr\windows.x64.$configuration\Tests\Core_Root")
$project = Join-Path $scriptRoot 'runtime-smoke\runtime-smoke.csproj'
$output = Join-Path $RepositoryRoot "artifacts\p16-object-header-bits-runtime\$GCMode"
$runtime = Join-Path $coreRoot 'coreclr.dll'

if (-not (Test-Path $runtime)) {
    throw "Core_Root runtime not found: $runtime"
}

& (Join-Path $RepositoryRoot 'dotnet.cmd') publish $project -c Release -o $output
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$environment = @{
    DOTNET_GCObjectHeaderBitsTest = '1'
    DOTNET_gcServer = if ($GCMode -eq 'Server') { '1' } else { '0' }
    P16_NATIVE_HOOK_LIBRARY = $runtime
}

$saved = @{}
try {
    foreach ($entry in $environment.GetEnumerator()) {
        $saved[$entry.Key] = [Environment]::GetEnvironmentVariable($entry.Key)
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value)
    }

    & (Join-Path $coreRoot 'corerun.exe') (Join-Path $output 'runtime-smoke.dll')
    if ($LASTEXITCODE -ne 100) {
        throw "Runtime smoke returned $LASTEXITCODE"
    }
}
finally {
    foreach ($entry in $saved.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value)
    }
}

exit 0
