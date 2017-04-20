#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [switch]$Update,
    [string[]]$EnvVars=@(),
    [switch]$Help)

if($Help)
{
    Write-Host "Usage: .\update-dependencies.ps1"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Update                            Update dependencies (but don't open a PR)"
    Write-Host "  -EnvVars <'V1=val1','V2=val2'...>  Comma separated list of environment variable name-value pairs"
    Write-Host "  -Help                              Display this help message"
    exit 0
}

$Architecture='x64'

$RepoRoot = "$PSScriptRoot\..\.."
$ProjectArgs = ""

if ($Update)
{
    $ProjectArgs = "--update"
}

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
if (!$env:DOTNET_INSTALL_DIR)
{
    $env:DOTNET_INSTALL_DIR="$RepoRoot\.dotnet_stage0\Windows\$Architecture"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR))
{
    mkdir $env:DOTNET_INSTALL_DIR | Out-Null
}

if (!(Test-Path "$RepoRoot\artifacts"))
{
    mkdir "$RepoRoot\artifacts" | Out-Null
}

# Install a stage 0
$DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1"
Invoke-WebRequest $DOTNET_INSTALL_SCRIPT_URL -OutFile "$RepoRoot\artifacts\dotnet-install.ps1"

& "$RepoRoot\artifacts\dotnet-install.ps1" -Version 1.0.0-preview3-003886 -Architecture $Architecture -Verbose
if($LASTEXITCODE -ne 0) { throw "Failed to install stage0" }

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"

$appPath = "$PSScriptRoot"

# Figure out the RID of the current platform, based on what stage 0 thinks.
$HOST_RID=(dotnet --info | Select-String -Pattern "\s*RID:\s*(?<rid>.*)").Matches[0].Groups['rid'].Value

# Restore the build scripts
Write-Host "Restoring Build Script projects..."
pushd "$PSScriptRoot\.."

(Get-Content "dotnet-host-build\project.json.template").Replace("{RID}", $HOST_RID) | Set-Content "dotnet-host-build\project.json"
(Get-Content "update-dependencies\project.json.template").Replace("{RID}", $HOST_RID) | Set-Content "update-dependencies\project.json"

dotnet restore
if($LASTEXITCODE -ne 0) { throw "Failed to restore" }
popd

# Publish the app
Write-Host "Compiling App $appPath..."
dotnet publish "$appPath" -o "$appPath\bin" --framework netcoreapp1.0
if($LASTEXITCODE -ne 0) { throw "Failed to compile build scripts" }

# Run the app
Write-Host "Invoking App $appPath..."
Write-Host " Configuration: $env:CONFIGURATION"
& "$appPath\bin\update-dependencies.exe" $ProjectArgs -e @EnvVars
if($LASTEXITCODE -ne 0) { throw "Build failed" }
