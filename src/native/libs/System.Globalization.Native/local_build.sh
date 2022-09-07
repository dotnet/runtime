#!/bin/sh

# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#

# This script is used only for building libSystem.Globalization.Native.a
# in the end-user's computer for NativeAOT purposes (static linking).
# This file is not used during the dotnet runtime build.

# Currently, only Linux is supported

SHIM_SOURCE_DIR=$1/native/src
INTERMEDIATE_OUTPUT_PATH=$2

if [ -d "$SHIM_SOURCE_DIR" ]; then
    LOCAL_SHIM_DIR="$INTERMEDIATE_OUTPUT_PATH"/libs/System.Globalization.Native/build
    mkdir -p "$LOCAL_SHIM_DIR" && cd "$LOCAL_SHIM_DIR"
    if [ $? -ne 0 ]; then echo "local_build.sh::ERROR: Cannot use local build directory"; exit 1; fi
    cmake -S "$SHIM_SOURCE_DIR/libs/System.Globalization.Native/" -DLOCAL_BUILD:STRING=1 -DCLR_CMAKE_TARGET_UNIX:STRING=1
    if [ $? -ne 0 ]; then echo "local_build.sh::ERROR: cmake failed"; exit 1; fi
    make -j
    if [ $? -ne 0 ]; then echo "local_build.sh::ERROR: Build failed"; exit 1; fi
fi

exit 0
