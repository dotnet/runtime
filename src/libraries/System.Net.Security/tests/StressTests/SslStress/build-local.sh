#!/usr/bin/env bash

## This is a helper script for non-containerized local build and test execution.
## Usage:
## ./build-local.sh [Configuration]

# Note that this script does much less than it's counterpart in HttpStress.
# In SslStress it's a thin utility to generate a runscript for running the app with the live-built testhost.
# The main reason to use an equivalent solution in SslStress is consistency with HttpStress.

version=7.0
repo_root=$(git rev-parse --show-toplevel)

configuration="Release"
if [ "$1" != "" ]
then
    configuration=${1,,}            # Lowercase all characters in $1
    configuration=${configuration^} # Uppercase first character
fi

echo "Building solution."
dotnet build -c $configuration

testhost_root=$repo_root/artifacts/bin/testhost/net$version-Linux-$configuration-x64

runscript=./run-stress-${configuration,,}.sh
if [ ! -f $runscript ]
then
    echo "Generating runscript."
    echo "$testhost_root/dotnet exec ./bin/$configuration/net$version/SslStress.dll \$@" > $runscript
    chmod +x $runscript
fi

echo "To run tests type:"
echo "$runscript [stress test args]"