#!/bin/sh

# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#

# This script is used only for building libSystem.Globalization.Native.a
# in the end-user's computer for NativeAOT purposes (static linking).
# It is not used during the dotnet runtime build.

# Currently, only Linux is supported

SHIM_SOURCE_DIR=$1/shim_source_code
INTERMEDIATE_OUTPUT_PATH=$2

if [ -d "$SHIM_SOURCE_DIR" ]; then
    cp -r "$SHIM_SOURCE_DIR" "$INTERMEDIATE_OUTPUT_PATH"
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: Cannot copy into $INTERMEDIATE_OUTPUT_PATH"; exit 1; fi
    LOCAL_SHIM_DIR="$INTERMEDIATE_OUTPUT_PATH"/shim_source_code/libs/System.Globalization.Native
    cd "$LOCAL_SHIM_DIR"
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: Cannot cd into $LOCAL_SHIM_DIR"; exit 1; fi
    cp CMakeLists-standalone.txt CMakeLists.txt
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: Cannot copy CMakeLists.txt"; exit 1; fi
    cp config-standalone.h config.h
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: Cannot copy config.h"; exit 1; fi
    mkdir -p build
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: Cannot mkdir build directory"; exit 1; fi
    cd build
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: Build directory does not exist"; exit 1; fi
    cmake ../
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: cmake failed"; exit 1; fi
    make -j;
    if [ $? -ne 0 ]; then echo "build-standalone.sh::ERROR: Build failed"; exit 1; fi
fi

exit 0
