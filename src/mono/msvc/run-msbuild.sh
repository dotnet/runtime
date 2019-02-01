#!/bin/bash

# Arguments:
#-------------------------------------------------------
# $1 Visual Studio target, build|clean, default build
# $2 Host CPU architecture, x86_64|i686, default x86_64
# $3 Visual Studio configuration, debug|release, default release
# $4 Additional arguments passed to msbuild, needs to be quoted if multiple.
#-------------------------------------------------------

RUN_MSBUILD_SCRIPT_PATH=$(cd "$(dirname "$0")"; pwd)
RUN_MSBUILD_SCRIPT_PATH=$(cygpath -w "$RUN_MSBUILD_SCRIPT_PATH/run-msbuild.bat")

"$WINDIR/System32/cmd.exe" /c "$RUN_MSBUILD_SCRIPT_PATH" "$@"
