#!/bin/bash
# Build the LXR collector (target clrlxrgc) on Linux and make a runnable frame.
# The frame is a hard-linked copy of the accepted e8d2d23 layout with only libclrlxrgc.so replaced.
# usage: build-linux.sh <repo-root-as-WSL-path> <Release|Debug> <label>
set -eu
REPO="$1"
CFG="$2"
LABEL="$3"
ROOT=${P17_ROOT:-/root/p17}
OBJ="$ROOT/obj-$LABEL-$CFG"
ACCEPTED=/root/lxr-batching-d2eccaea/after-$CFG-linux
FRAME="$ROOT/frames/$LABEL-$CFG-linux"
mkdir -p "$ROOT/frames" "$ROOT/logs"
if [ ! -f "$OBJ/build.ninja" ]; then
  cmake -S "$REPO/src/coreclr" -B "$OBJ" -G Ninja -DCMAKE_BUILD_TYPE="$CFG" \
    -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=linux \
    -DCMAKE_C_COMPILER=/usr/bin/clang-14 -DCMAKE_CXX_COMPILER=/usr/bin/clang++-14 \
    -DCLR_CMAKE_ENABLE_LXR_GC=1 > "$ROOT/logs/configure-$CFG.log" 2>&1
fi
ninja -C "$OBJ" clrlxrgc > "$ROOT/logs/build-$LABEL-$CFG.log" 2>&1
echo "build exit=$?"
rm -rf "$FRAME"
cp -al "$ACCEPTED" "$FRAME"
cp --remove-destination "$OBJ/gc/libclrlxrgc.so" "$FRAME/libclrlxrgc.so"
[ -f "$OBJ/gc/libclrlxrgc.so.dbg" ] && cp --remove-destination "$OBJ/gc/libclrlxrgc.so.dbg" "$FRAME/libclrlxrgc.so.dbg"
sha256sum "$FRAME/libclrlxrgc.so"
