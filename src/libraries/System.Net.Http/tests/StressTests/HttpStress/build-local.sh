#!/usr/bin/env bash

## This is a helper script for non-containerized local build and test execution.
## It downloads and uses the daily SDK which contains the compatible AspNetCore bits.
## Usage:
## ./build-local.sh [Configuration]

version=7.0
repo_root=$(git rev-parse --show-toplevel)
daily_dotnet_root=./.dotnet-daily

configuration="Release"
if [ "$1" != "" ]
then
    configuration=${1,,}            # Lowercase all characters in $1
    configuration=${configuration^} # Uppercase first character
fi

echo "Configuration: $configuration"

if [ ! -d $daily_dotnet_root ];
then
    echo "Downloading daily SDK to $daily_dotnet_root"
    mkdir $daily_dotnet_root
    wget https://dot.net/v1/dotnet-install.sh -O $daily_dotnet_root/dotnet-install.sh
    bash $daily_dotnet_root/dotnet-install.sh --no-path --channel $version.1xx --quality daily --install-dir $daily_dotnet_root
else
    echo "Daily SDK found in $daily_dotnet_root"
fi

testhost_root=$repo_root/artifacts/bin/testhost/net$version-Linux-$configuration-x64

export DOTNET_ROOT=$daily_dotnet_root
export PATH=$DOTNET_ROOT:$PATH
export DOTNET_MULTILEVEL_LOOKUP=0

if [ ! -d "$testhost_root/shared/Microsoft.AspNetCore.App" ]
then
    echo "Copying Microsoft.AspNetCore.App bits from daily SDK to testhost: $testhost_root"
    cp -r $daily_dotnet_root/shared/Microsoft.AspNetCore.App $testhost_root/shared/Microsoft.AspNetCore.App
else
    echo "Microsoft.AspNetCore.App found in testhost: $testhost_root"
fi

echo "Building solution."
dotnet build -c $configuration

runscript=./run-stress-${configuration,,}.sh
if [ ! -f $runscript ]
then
    echo "Generating runscript."
    echo "$testhost_root/dotnet exec ./bin/$configuration/net$version/HttpStress.dll \$@" > $runscript
    chmod +x $runscript
fi

echo "To run tests type:"
echo "$runscript [stress test args]"