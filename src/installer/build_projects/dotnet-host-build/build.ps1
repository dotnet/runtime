#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [string]$Architecture="x64",
    [string]$TargetArch="",
    [string]$ToolsetDir="",
    [string]$Framework="netcoreapp2.0",
    [string[]]$Targets=@("Default"),
    [string[]]$EnvVars=@(),
    [switch]$NoPackage,
    [switch]$Help)

if($Help)
{
    Write-Host "Usage: .\build.ps1 [-Configuration <CONFIGURATION>] [-NoPackage] [-Help] [-Targets <TARGETS...>]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <CONFIGURATION>     Build the specified Configuration (Debug or Release, default: Debug)"
    Write-Host "  -Architecture  <ARCHITECTURE>      Build on the specified architecture (x64 or x86 (supported only on Windows), default: x64)"
    Write-Host "  -TargetArch  <ARCHITECTURE>        Build for the specified architecture (x64, x86 (supported only on Windows), arm, or arm64, default: x64)"
    Write-Host "  -ToolsetDir  <TOOLSETDIR>          Temporary variable specifying a path to a toolset to use when building the native host for ARM64. To be removed when the toolset is publicly available. )"
    Write-Host "  -Framework  <FRAMEWORK>            Build the specified framework (netcoreapp1.0 or netcoreapp1.1, default: netcoreapp1.0)"
    Write-Host "  -Targets <TARGETS...>              Comma separated build targets to run (Init, Compile, Publish, etc.; Default is a full build and publish)"
    Write-Host "  -EnvVars <'V1=val1','V2=val2'...>  Comma separated list of environment variable name-value pairs"
    Write-Host "  -NoPackage                         Skip packaging targets"
    Write-Host "  -Help                              Display this help message"
    exit 0
}

$env:CONFIGURATION = $Configuration;
if (!$TargetArch)
{
    $TargetArch = $Architecture
}
$env:TARGETPLATFORM = $TargetArch;
$env:TARGETFRAMEWORK = $Framework;
$RepoRoot = "$PSScriptRoot\..\.."
$env:NUGET_PACKAGES = "$RepoRoot\.nuget\packages"

if($TargetArch -eq "arm64")
{
    if ($Framework -eq "netcoreapp1.0")
    {
        throw "ARM64 is not available on netcoreapp1.0. Pass in '-Framework netcoreapp1.1' to enable ARM64"
    }
    $env:__ToolsetDir = $ToolsetDir;
    $env:TARGETRID = "win10-arm64";
    $env:PATH="$ToolsetDir\VC_sdk\bin;$env:PATH";
    $env:LIB="$ToolsetDir\VC_sdk\lib\arm64;$ToolsetDir\sdpublic\sdk\lib\arm64";
    $env:INCLUDE="$ToolsetDir\VC_sdk\inc;$ToolsetDir\sdpublic\sdk\inc;$ToolsetDir\sdpublic\shared\inc;$ToolsetDir\sdpublic\shared\inc\minwin;$ToolsetDir\sdpublic\sdk\inc\ucrt;$ToolsetDir\sdpublic\sdk\inc\minwin;$ToolsetDir\sdpublic\sdk\inc\mincore;$ToolsetDir\sdpublic\sdk\inc\abi;$ToolsetDir\sdpublic\sdk\inc\clientcore;$ToolsetDir\diasdk\include";
}

# No use in specifying a RID if the current and target architecture are equivalent.
if($TargetArch -eq "x86" -and $Architecture -ne "x86")
{
    $env:TARGETRID = "win7-x86";
}
if($TargetArch -eq "x64" -and $Architecture -ne "x64")
{
    $env:TARGETRID = "win7-x64";
}
if($TargetArch -eq "arm")
{
    $env:TARGETRID = "win8-arm";
}

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

# Disable first run since we want to control all package sources
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

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
& "$PSScriptRoot\bin\dotnet-host-build.exe" -t @Targets -e @EnvVars
if($LASTEXITCODE -ne 0) { throw "Build failed" }
