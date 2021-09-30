#!/bin/bash
# This script is a bridge that allows .cmd files of individual tests to run the respective test executables
# repeatedly so that more methods get rejitted at Tier1
#
# To use this script, set the CLRCustomTestLauncher environment variable to the full path of this script.

export CORE_LIBRARIES=$1
$_DebuggerFullPath "$CORE_ROOT/corerun" "$CORE_ROOT/tieringtest.dll" $1$2 "${@:3}"
