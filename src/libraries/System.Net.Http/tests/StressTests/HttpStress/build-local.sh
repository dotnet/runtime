#!/usr/bin/env bash

## This is a helper script for non-containerized local build and test execution.
## It downloads and uses the daily SDK which contains the compatible AspNetCore bits.
## Usage:
## ./build-local.sh [configuration]

version=7.0
repo_root=$(git rev-parse --show-toplevel)
daily_dotnet_root=$repo_root/.dotnet-daily

configuration="Release"
if [ "$1" != "" ]
then
    configuration=${1,,}            # Lowercase all characters in $1
    configuration=${configuration^} # Uppercase first character
fi

echo $configuration

if [ ! -d $daily_dotnet_root ]
then
    mkdir $daily_dotnet_root
    wget https://dot.net/v1/dotnet-install.sh -O $daily_dotnet_root/dotnet-install.sh

    # Unfortunately "--runtime aspnetcore" cannot be downloaded with "--quality daily",
    # so we need to acquire the full SDK:
    bash $daily_dotnet_root/dotnet-install.sh --no-path --channel $version.1xx --quality daily --install-dir $daily_dotnet_root
fi

testhost_root=$repo_root/artifacts/bin/testhost/net$version-Linux-$configuration-x64

export DOTNET_ROOT=$daily_dotnet_root
export PATH=$DOTNET_ROOT:$PATH
export DOTNET_MULTILEVEL_LOOKUP=0

if [ ! -d "$testhost_root/shared/Microsoft.AspNetCore.App" ]
then
    cp -r $daily_dotnet_root/shared/Microsoft.AspNetCore.App $testhost_root/shared/Microsoft.AspNetCore.App
fi

dotnet build -c $configuration



# $env:DOTNET_ROOT=$candidate_path
# $env:DOTNET_CLI_HOME=$candidate_path
# $env:PATH=($candidate_path + $pathSeparator + $env:PATH)
# $env:DOTNET_MULTILEVEL_LOOKUP=0
# $env:DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2