#!/bin/sh
set -e

# Usage: build_test_linux.cmd <Configuration>
#
#   Configuration is one of Debug or Release - Release is the default is not specified

if [ "$1" = "" ]; then
    configuration=Release
else
    configuration=$1
fi

.yamato/scripts/build_linux.sh $configuration
.yamato/scripts/test_linux.sh $configuration
