#!/bin/bash

# Arguments:
#-------------------------------------------------------
# $1 Visual Studio target, build|clean, default build
# $2 Host CPU architecture, x86_64|i686, default x86_64
#-------------------------------------------------------

VS_TARGET="build"
VS_PLATFORM="x64"

if [[ $1 = "clean" ]]; then
    VS_TARGET="clean"
fi

if [[ $2 = "i686" ]]; then
    VS_PLATFORM="Win32"
fi

VS_BUILD_ARGS="run-msbuild.bat"
VS_BUILD_ARGS+=" /p:Configuration=Release"
VS_BUILD_ARGS+=" /p:Platform=$VS_PLATFORM"
VS_BUILD_ARGS+=" /p:MONO_TARGET_GC=sgen"
VS_BUILD_ARGS+=" /t:Build"

if [[ -z "$ORIGINAL_PATH" ]]; then
    echo "Warning, run-msbuild.sh executed without ORIGINAL_PATH environment variable set. \
    Windows environment can not be properly restored before running command."
fi

export PATH=$ORIGINAL_PATH
$WINDIR/System32/cmd.exe /c "$VS_BUILD_ARGS"
