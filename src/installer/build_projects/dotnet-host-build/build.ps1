#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [string]$Architecture="x64",
    [string[]]$Targets=@("Default"),
    [switch]$NoPackage,
    [switch]$Help)

if($Help)
{
    Write-Host "Usage: .\build.ps1 [-Configuration <CONFIGURATION>] [-NoPackage] [-Help] [-Targets <TARGETS...>]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <CONFIGURATION>     Build the specified Configuration (Debug or Release, default: Debug)"
    Write-Host "  -Architecture  <ARCHITECTURE>      Build the specified architecture (x64 or x86 (supported only on Windows), default: x64)"
    Write-Host "  -Targets <TARGETS...>              Comma separated build targets to run (Init, Compile, Publish, etc.; Default is a full build and publish)"
    Write-Host "  -NoPackage                         Skip packaging targets"
    Write-Host "  -Help                              Display this help message"
    exit 0
}

$env:CONFIGURATION = $Configuration;
$RepoRoot = "$PSScriptRoot\..\.."

if($NoPackage)
{
    $env:DOTNET_BUILD_SKIP_PACKAGING=1
}
else
{
    $env:DOTNET_BUILD_SKIP_PACKAGING=0
}

# Load Branch Info
cat "$RepoRoot\branchinfo.txt" | ForEach-Object {
    if(!$_.StartsWith("#") -and ![String]::IsNullOrWhiteSpace($_)) {
        $splat = $_.Split([char[]]@("="), 2)
        Set-Content "env:\$($splat[0])" -Value $splat[1]
    }
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

# Install a stage 0
Write-Host "Installing .NET Core CLI Stage 0 from branchinfo channel"
& "$RepoRoot\scripts\obtain\dotnet-install.ps1" -Channel $env:CHANNEL -Architecture $Architecture -Verbose
if($LASTEXITCODE -ne 0) { throw "Failed to install stage0" }

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"

# Restore the build scripts
Write-Host "Restoring Build Script projects..."
pushd "$PSScriptRoot\.."
dotnet restore --infer-runtimes
if($LASTEXITCODE -ne 0) { throw "Failed to restore" }
popd

# Publish the builder
Write-Host "Compiling Build Scripts..."
dotnet publish "$PSScriptRoot" -o "$PSScriptRoot\bin" --framework netcoreapp1.0
if($LASTEXITCODE -ne 0) { throw "Failed to compile build scripts" }

# Run the builder
Write-Host "Invoking Build Scripts..."
Write-Host " Configuration: $env:CONFIGURATION"
& "$PSScriptRoot\bin\dotnet-host-build.exe" @Targets
if($LASTEXITCODE -ne 0) { throw "Build failed" }
