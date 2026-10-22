#!/bin/bash
# P25: build the LXR collector library (libclrlxrgc.so) from the Windows worktree and make a frame.
# The sources of the collector and the P2-P4 test sources are copied to /root/p25/main with LF line endings.
# The first call makes /root/p25/main from the P24 build tree (only src/coreclr/gc is replaced later).
# usage: build.sh <worktree-wsl-path> <Release|Debug> [frame-name]
#   Release: frame = Release runtime of D2 with the new libclrlxrgc.so        (default name p25-Release-linux)
#   Debug:   frame = the same Release runtime with the Debug libclrlxrgc.so   (default name p25-dbgcol-linux)
set -eu
export PATH=/usr/local/lib/python3.10/dist-packages/cmake/data/bin:$PATH
W=$1
CFG=$2
R=/root/p25
mkdir -p $R/logs $R/frames
if [ ! -d $R/main/src/coreclr ]; then
  mkdir -p $R/main
  rsync -a /root/p24/main/ $R/main/
fi
rsync -a --delete $W/src/coreclr/gc/ $R/main/src/coreclr/gc/
mkdir -p $R/main/docs/design/lxr-port
rsync -a --delete $W/docs/design/lxr-port/ $R/main/docs/design/lxr-port/ --exclude='*/results' --exclude='*/benchmark-results' --exclude='*.html' --exclude='*.png'
find $R/main/src/coreclr/gc $R/main/docs/design/lxr-port -type f \( -name '*.h' -o -name '*.cpp' -o -name '*.sh' -o -name '*.py' -o -name 'CMakeLists.txt' -o -name '*.cmake' -o -name '*.inc' \) -exec sed -i 's/\r$//' {} +
OBJ=$R/obj-$CFG
if [ ! -f $OBJ/build.ninja ]; then
  cmake -S $R/main/src/coreclr -B $OBJ -G Ninja -DCMAKE_BUILD_TYPE=$CFG \
    -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=linux \
    -DCMAKE_C_COMPILER=/usr/bin/clang-14 -DCMAKE_CXX_COMPILER=/usr/bin/clang++-14 \
    -DCLR_CMAKE_ENABLE_LXR_GC=1 > $R/logs/configure-$CFG.log 2>&1
fi
set +e
ninja -C $OBJ clrlxrgc > $R/logs/build-$CFG.log 2>&1
code=$?
set -e
echo "build $CFG exit=$code"
[ $code -eq 0 ] || { grep -E 'error' $R/logs/build-$CFG.log | head -20; exit 1; }
if [ "$CFG" = Release ]; then NAME=${3:-p25-Release-linux}; else NAME=${3:-p25-dbgcol-linux}; fi
FRAME=$R/frames/$NAME
rm -rf $FRAME
cp -al /root/d2/frames/d2-Release-linux $FRAME
cp --remove-destination $OBJ/gc/libclrlxrgc.so $FRAME/libclrlxrgc.so
[ -f $OBJ/gc/libclrlxrgc.so.dbg ] && cp --remove-destination $OBJ/gc/libclrlxrgc.so.dbg $FRAME/libclrlxrgc.so.dbg || true
ls -la $FRAME/libclrlxrgc.so
