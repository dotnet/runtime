#!/bin/sh
set -e

# Usage: build_test_osx.cmd <Architecture> <Configuration>
#
#   Architecture is one of arm64 or x64 - x64 is the default if not specified
#   Configuration is one of Debug or Release - Release is the default is not specified
#
# To specify Configuration, Architecture must be specified as well.

if [ "$1" = "" ]; then
    architecture=x64
else
    architecture=$1
fi

if [ "$2" = "" ]; then
    configuration=Release
else
    configuration=$2
fi

.yamato/scripts/build_osx.sh $architecture $configuration
.yamato/scripts/test_osx.sh $architecture $configuration
