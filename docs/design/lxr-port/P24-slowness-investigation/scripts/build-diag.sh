#!/bin/bash
# Build an unstripped Release libclrlxrgc.so from the worktree sources (unchanged product code) and make a frame
# next to the d2 frame (the d2 frame with only libclrlxrgc.so replaced). The frame d2-Release-linux stays untouched.
# Only src/coreclr/gc is copied from the worktree (the rest of the tree is the d2 build tree; the P24 check
# chk-source.sh shows that the collector sources equal the worktree).
# usage: build-diag.sh <worktree-wsl-path>
set -eu
export PATH=/usr/local/lib/python3.10/dist-packages/cmake/data/bin:$PATH
W=$1
R=/root/p24
pkill -x rsync 2>/dev/null || true
mkdir -p $R/logs $R/frames
[ -d $R/main/src/coreclr ] || { mkdir -p $R/main; rsync -a /root/d2/main/src/ $R/main/src/; }
rsync -a --delete $W/src/coreclr/gc/ $R/main/src/coreclr/gc/
find $R/main/src/coreclr/gc -type f \( -name '*.h' -o -name '*.cpp' -o -name 'CMakeLists.txt' -o -name '*.cmake' -o -name '*.inc' \) -exec sed -i 's/\r$//' {} +
for d in eng; do [ -d $R/main/$d ] || rsync -a /root/d2/main/$d/ $R/main/$d/; done
for f in Directory.Build.props Directory.Build.targets global.json; do cp -n /root/d2/main/$f $R/main/ 2>/dev/null || true; done
mkdir -p $R/main/artifacts/obj
cp -n /root/d2/main/artifacts/obj/_version.c /root/d2/main/artifacts/obj/_version.h /root/d2/main/artifacts/obj/runtime_version.h $R/main/artifacts/obj/ 2>/dev/null || true
cp -n /root/d2/main/artifacts/obj/NativeVersion.rc $R/main/artifacts/obj/ 2>/dev/null || true
OBJ=$R/obj-Release
if [ ! -f $OBJ/build.ninja ]; then
  cmake -S $R/main/src/coreclr -B $OBJ -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=linux \
    -DCMAKE_C_COMPILER=/usr/bin/clang-14 -DCMAKE_CXX_COMPILER=/usr/bin/clang++-14 \
    -DCLR_CMAKE_ENABLE_LXR_GC=1 > $R/logs/configure.log 2>&1
fi
ninja -C $OBJ clrlxrgc > $R/logs/build.log 2>&1
echo "build exit=$?"
FRAME=$R/frames/p24-Release-linux
rm -rf $FRAME
cp -al /root/d2/frames/d2-Release-linux $FRAME
cp --remove-destination $OBJ/gc/libclrlxrgc.so $FRAME/libclrlxrgc.so
cp --remove-destination $OBJ/gc/libclrlxrgc.so.dbg $FRAME/libclrlxrgc.so.dbg
ls -la $FRAME/libclrlxrgc.so*
