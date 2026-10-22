#!/bin/bash
# P22 (D2): build Satori (opt-in) from the D2 sources and run binary trees with it, then the default GC, on the D2 Release runtime.
set -u
. "$(dirname "$0")/env.sh"
OBJ=$D/obj-satori
if [ ! -f "$OBJ/build.ninja" ]; then
  cmake -S "$T/src/coreclr" -B "$OBJ" -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=linux \
    -DCMAKE_C_COMPILER=/usr/bin/clang-14 -DCMAKE_CXX_COMPILER=/usr/bin/clang++-14 \
    -DCLR_CMAKE_ENABLE_SATORI_GC=1 > $D/configure-satori.log 2>&1
fi
ninja -C "$OBJ" satorigc > $D/build-satori.log 2>&1
echo "satori build exit=$?"
tail -2 $D/build-satori.log | cut -c1-200
F=$D/frames/d2-Release-linux
S=$(find "$OBJ" -name 'libsatorigc.so' | head -1); cp -f "$S" $F/libsatorigc.so
echo "satori module: $S"
echo "== Satori, binary trees"
DOTNET_GCName=libsatorigc.so $F/corerun /root/gcbench/net/gcbench.dll binarytrees 16 0 run 2>&1 | tail -2
echo "== default GC, binary trees"
$F/corerun /root/gcbench/net/gcbench.dll binarytrees 16 0 run 2>&1 | tail -2
