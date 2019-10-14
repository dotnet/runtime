# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# Helper script used for pointing the current powershell environment 
# to the testhost sdk built by the corefx build script.

[CmdletBinding(PositionalBinding=$false)]
Param(
  [string][Alias('f')]$framework = "netcoreapp",
  [string][Alias('c')]$configuration = "Debug",
  [string][Alias('a')]$arch = "x64",
  [string][Alias('o')]$os = ""
)

# script needs to be sourced, detect if running standalone
if ($MyInvocation.InvocationName -ne ".")
{
    write-output "Script must be sourced"
    write-output "USAGE: . $($MyInvocation.InvocationName) <args>"
    exit
}

# find corefx root, assuming script lives in the git repo
$SOURCE_DIR="$(split-path -Parent $MyInvocation.MyCommand.Definition)"
$COREFX_ROOT_DIR=$(git -C "$SOURCE_DIR" rev-parse --show-toplevel)

function Find-Os()
{
    if (!$(test-path variable:IsWindows) -or $IsWindows)
    {
        return "Windows_NT"
    } 
    else
    {
        switch -Wildcard ($(uname -s))
        {
            "Linux*" { return "Linux" }
            "Darwin*" { return "MacOS" }
            "*" { return "Unix" }
        }
    }
}

if ($os -eq "")
{
    $os=$(Find-Os)
}

function Set-Sdk-Environment()
{
    $candidate_path=$([IO.Path]::Combine($COREFX_ROOT_DIR, 'artifacts', 'bin', 'testhost', "$FRAMEWORK-$OS-$CONFIGURATION-$ARCH"))

    if (!$(test-path -PathType container $candidate_path))
    {
        write-output "Could not locate testhost sdk path $candidate_path" 
        return
    }
    elseif (!$(test-path -PathType leaf $([IO.Path]::Combine($candidate_path, "dotnet"))) -and 
            !$(test-path -PathType leaf $([IO.Path]::Combine($candidate_path, "dotnet.exe"))))
    {
        write-output "Could not find dotnet executable in testhost sdk path $candidate_path"
        return
    }

    $pathSeparator=if($os -eq "Windows_NT") { ";" } else { ":" }
    
    $env:DOTNET_ROOT=$candidate_path
    $env:DOTNET_CLI_HOME=$candidate_path
    $env:PATH=($candidate_path + $pathSeparator + $env:PATH)
    $env:DOTNET_MULTILEVEL_LOOKUP=0
    $env:DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2
}

Set-Sdk-Environment
