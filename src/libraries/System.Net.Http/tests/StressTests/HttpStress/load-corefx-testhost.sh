#!/usr/bin/env bash

## *** Usage ***
## source ./load-corefx-teshost.ps1

if [ ! -d "./.dotnet-daily" ]
then
    mkdir ./.dotnet-daily
    wget https://dot.net/v1/dotnet-install.sh -O ./.dotnet-daily/dotnet-install.sh

    # Unfortunately "--runtime aspnetcore" cannot be downloaded with "--quality daily",
    # so we need to acquire the full SDK:
    bash ./.dotnet-daily/dotnet-install.sh --no-path --channel 7.0.1xx --quality daily --install-dir ./.dotnet-daily
fi

repo_root=$(git rev-parse --show-toplevel)
testhost_root=$repo_root/artifacts/bin/testhost/net7.0-Linux-Release-x64

export DOTNET_ROOT=./.dotnet-daily
export PATH=$DOTNET_ROOT:$PATH
# export DOTNET_MULTILEVEL_LOOKUP=0

if [ ! -d "$testhost_root/shared/Microsoft.AspNetCore.App" ]
then
    # mkdir "$DOTNET_ROOT/shared/Microsoft.AspNetCore.App"
    cp -r ./.dotnet-daily/shared/Microsoft.AspNetCore.App "$testhost_root/shared/Microsoft.AspNetCore.App"
fi

# $env:DOTNET_ROOT=$candidate_path
# $env:DOTNET_CLI_HOME=$candidate_path
# $env:PATH=($candidate_path + $pathSeparator + $env:PATH)
# $env:DOTNET_MULTILEVEL_LOOKUP=0
# $env:DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2