#!/usr/bin/env bash

## This is a helper script for non-containerized local build and test execution.
## Usage:
## ./build-local.sh [Configuration]

# Note that this script does much less than it's counterpart in HttpStress.
# In SslStress it's a thin utility to generate a runscript for running the app with the live-built testhost.
# The main reason to use an equivalent solution in SslStress is consistency with HttpStress.

version=7.0
repo_root=$(git rev-parse --show-toplevel)

stress_configuration="Release"
if [ "$1" != "" ]
then
    stress_configuration=${1,,}                   # Lowercase all characters in $1
    stress_configuration=${stress_configuration^} # Uppercase first character
fi

libraries_configuration="Release"
if [ "$2" != "" ]
then
    libraries_configuration=${2,,}                      # Lowercase all characters in $1
    libraries_configuration=${libraries_configuration^} # Uppercase first character
fi

echo "StressConfiguration: $stress_configuration, LibrariesConfiguration: $libraries_configuration"

echo "Building solution."
dotnet build -c $stress_configuration

testhost_root=$repo_root/artifacts/bin/testhost/net$version-Linux-$libraries_configuration-x64

runscript=./run-stress-${stress_configuration,,}-${libraries_configuration,,}.sh
if [ ! -f $runscript ]
then
    echo "Generating runscript."
    echo "$testhost_root/dotnet exec ./bin/$stress_configuration/net$version/SslStress.dll \$@" > $runscript
    chmod +x $runscript
fi

echo "To run tests type:"
echo "$runscript [stress test args]"