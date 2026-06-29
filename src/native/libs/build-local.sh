#!/bin/sh

# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#

# This script is used only for building native libraries
# in the end-user's computer for NativeAOT purposes (static linking).
# This file is not used during the dotnet runtime build.

# Currently, only Linux is supported

SHIM_SOURCE_DIR="$1"/native/src
INTERMEDIATE_OUTPUT_PATH="$2"
TARGET_LIBRARY="$3"
EMBED_ICU_DATA_PATH="$4"

if [ -n "$EMBED_ICU_DATA_PATH" ]; then
    header="${INTERMEDIATE_OUTPUT_PATH}embedded_icu_data.h"

    cat <<EOF > "$header"
#ifndef EMBEDDED_ICU_DATA
#define EMBEDDED_ICU_DATA

static const unsigned char icu_data[] __attribute__((aligned(16))) = {
$(od -An -vtx1 "$EMBED_ICU_DATA_PATH" | tr -d ' \n' | sed 's/\(..\)/0x\1, /g')
};

#endif // EMBEDDED_ICU_DATA
EOF

    CMAKE_EXTRA_ARGS=-DCMAKE_C_FLAGS="-DEMBEDDED_ICU_DATA_HEADER='\"$header\"'"
fi

if [ -d "$SHIM_SOURCE_DIR" ]; then
    LOCAL_SHIM_DIR="$INTERMEDIATE_OUTPUT_PATH"/libs/$TARGET_LIBRARY/build

    if ! { mkdir -p "$LOCAL_SHIM_DIR" && cd "$LOCAL_SHIM_DIR"; }; then
        echo "local_build.sh::ERROR: Cannot use local build directory"
        exit 1
    fi

    if ! cmake -S "$SHIM_SOURCE_DIR/libs/$TARGET_LIBRARY/" -DLOCAL_BUILD:STRING=1 -DCLR_CMAKE_TARGET_UNIX:STRING=1 $CMAKE_EXTRA_ARGS; then
        echo "local_build.sh::ERROR: cmake failed"
        exit 1
    fi

    if ! make -j; then
        echo "local_build.sh::ERROR: Build failed"
        exit 1
    fi
fi
