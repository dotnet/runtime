#!/bin/bash
# P21 (D1): build Satori (opt-in) from the D1 tree and run binary trees with it on the D1 Release runtime.
set -u
R=/root/d1/main
OBJ=/root/d1/obj-satori
if [ ! -f "$OBJ/build.ninja" ]; then
  cmake -S "$R/src/coreclr" -B "$OBJ" -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=linux \
    -DCMAKE_C_COMPILER=/usr/bin/clang-14 -DCMAKE_CXX_COMPILER=/usr/bin/clang++-14 \
    -DCLR_CMAKE_ENABLE_SATORI_GC=1 > /root/d1/configure-satori.log 2>&1
fi
ninja -C "$OBJ" satorigc > /root/d1/build-satori.log 2>&1
echo "satori build exit=$?"
tail -2 /root/d1/build-satori.log | cut -c1-200
F=/root/d1/frames/d1-Release-linux
cp -f "$OBJ/gc_satori/libsatorigc.so" /root/d1/frames/satorigc-copy.so 2>/dev/null || find "$OBJ" -name 'libsatorigc.so' | head -2
