#!/usr/bin/env bash

# This script is a bridge that allows running NativeAOT compiled executables instead of using corerun.
#
# To use this script, set the CLRCustomTestLauncher environment variable to the full path of this script.
#
# The .cmd files of the individual tests will call this script to launch the test.
# This script gets the following arguments
# 1. Full path to the directory of the test binaries (the test .sh file is in there)
# 2. Filename of the test executable
# 3. - n. Additional arguments that were passed to the test .sh

exename=$(basename $2 .dll)
$_DebuggerFullPath $1/native/$exename "${@:3}"
