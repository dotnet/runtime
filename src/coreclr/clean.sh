#!/usr/bin/env bash

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo Cleaning previous output for the selected configuration

rm -rf "$__ProjectRoot/bin"

rm -rf "$__ProjectRoot/Tools"

exit 0