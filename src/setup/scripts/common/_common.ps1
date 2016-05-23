#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

. $PSScriptRoot\_utility.ps1

# Copy things from environment variables that were sent by the build scripts
$Rid = $env:Rid
$Tfm = $env:Tfm
$OutputDir = $env:OutputDir
$Stage1Dir = $env:Stage1Dir
$Stage1CompilationDir = $env:Stage1CompilationDir
$Stage2Dir = $env:Stage2Dir
$Stage2CompilationDir = $env:Stage2CompilationDir
$PackageDir = $env:PackageDir
$TestBinRoot = $env:TestBinRoot
$TestPackageDir = $env:TestPackageDir

$env:Channel = "$env:RELEASE_SUFFIX"

# Set reasonable defaults for unset variables
setEnvIfDefault "DOTNET_INSTALL_DIR"  "$RepoRoot\.dotnet_stage0\win7-x64"
setEnvIfDefault "DOTNET_CLI_VERSION" "0.1.0.0"
setPathAndHomeIfDefault "$Stage2Dir"
setVarIfDefault "CONFIGURATION" "Debug"
