#!/bin/bash
# This script is a bridge that allows .cmd files of individual tests to run the respective test executables
# in an unloadable AssemblyLoadContext.
#
# To use this script, set the CLRCustomTestLauncher environment variable to the full path of this script.
#
# Additional command line arguments can be passed to the runincontext tool by setting the RunInContextExtraArgs
# environment variable
#
# The .cmd files of the individual tests will call this script to launch the test.
# This script gets the following arguments
# 1. Full path to the directory of the test binaries (the test .sh file is in there)
# 2. Filename of the test executable
# 3. - n. Additional arguments that were passed to the test .sh

export CORE_LIBRARIES=$1
$_DebuggerFullPath "$CORE_ROOT/corerun" "$CORE_ROOT/runincontext.dll" $RunInContextExtraArgs /referencespath:$CORE_ROOT/ $1$2 "${@:3}"
