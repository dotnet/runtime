#!/bin/bash
# Build the Debug LXR collector (target clrlxrgc) and make a frame that has the Release runtime and the Debug collector.
# usage: mkdbgcollector.sh <repo-root> <label> [frames-root]   (the Release frame <label>-Release-linux must exist)
set -eu
R=$1; L=$2
ROOT=${3:-$HOME/lxr-frames}
OBJ=$ROOT/obj-dbg-$L
mkdir -p "$ROOT"
if [ ! -f "$OBJ/build.ninja" ]; then
  cmake -S "$R/src/coreclr" -B "$OBJ" -G Ninja -DCMAKE_BUILD_TYPE=Debug \
    -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=linux \
    -DCMAKE_C_COMPILER=/usr/bin/clang-14 -DCMAKE_CXX_COMPILER=/usr/bin/clang++-14 \
    -DCLR_CMAKE_ENABLE_LXR_GC=1 > "$ROOT/configure-dbg-$L.log" 2>&1
fi
ninja -C "$OBJ" clrlxrgc > "$ROOT/build-dbg-$L.log" 2>&1
echo "build exit=$?"
F=$ROOT/$L-Debug-linux
rm -rf "$F"
cp -al "$ROOT/$L-Release-linux" "$F"
cp --remove-destination "$OBJ/gc/libclrlxrgc.so" "$F/libclrlxrgc.so"
sha256sum "$F/libclrlxrgc.so"
