#!/bin/bash
# P17 S7 managed controls. Build the collector with one control define (the product code has the same switches as the
# native test), run the managed cells that depend on the rule, and print the result. Each control must make a cell fail.
# The cells need the managed fixture (docs/design/lxr-port/P5/runtime-tests) built into <fixtures>/fixture-scope5-Release.
# usage: controls-s7-managed.sh <repo-wsl-path> <work-dir> <frame-with-s7-collector> <fixtures-dir> <cell1.sh>
S=${1:?repo path}
R=${2:?work dir}
BASE=${3:?frame}
FIXTURES=${4:?fixtures dir}
CELL1=${5:?cell1.sh}
mkdir -p $R
OBJ=$R/obj-ctl-Release
FRAME=$R/frame-ctl
if [ ! -f "$OBJ/build.ninja" ]; then
  cmake -S "$S/src/coreclr" -B "$OBJ" -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DCLR_CMAKE_TARGET_ARCH=x64 -DCLR_CMAKE_HOST_ARCH=x64 -DCLR_CMAKE_TARGET_OS=linux \
    -DCMAKE_C_COMPILER=/usr/bin/clang-14 -DCMAKE_CXX_COMPILER=/usr/bin/clang++-14 \
    -DCLR_CMAKE_ENABLE_LXR_GC=1 > $R/configure.log 2>&1
fi
rm -rf $FRAME
cp -al $BASE $FRAME
export EXTRA_ENV="DOTNET_LXRImmixInitialMB=96 DOTNET_LXRLOSInitialMB=32 DOTNET_LXRManagedHeapHardLimitMB=128"
run() { # control cells modes
  c=$1
  [ -n "${ONLY:-}" ] && [[ " $ONLY " != *" $c "* ]] && return
  cmake -DCMAKE_CXX_FLAGS="-DLXR_S7_${c}_CONTROL" $OBJ > $R/ctl-$c-configure.log 2>&1
  ninja -C $OBJ clrlxrgc > $R/ctl-$c-build.log 2>&1 || { echo "control $c: BUILD FAILED"; return; }
  cp --remove-destination $OBJ/gc/libclrlxrgc.so $FRAME/libclrlxrgc.so
  for m in $3; do
    for cell in $2; do
      out=$(TAILN=30 TMO=300 bash $CELL1 $FRAME $m $cell Release $FIXTURES 2>&1)
      res=$(echo "$out" | grep -E '^PASS|Fatal error|Out of memory|InvalidOperationException|Unhandled|Segmentation|ASSERT|dumped core' | head -1 | cut -c1-160)
      echo "control $c | $m | $cell :: ${res:-no result line}"
    done
  done
}
run IGNORE_PIN "pin-handles-churn fixed-pressure" "nursery"
run NO_BLOCK_BYTE "pin-handles-churn fixed-pressure" "nursery"
run POH_NORMAL "poh-churn" "nursery"
run NO_RETAIN "pin-concurrent-trace pin-handles-churn" "mature"
# restore the plain build in the control tree
cmake -DCMAKE_CXX_FLAGS="" $OBJ > /dev/null 2>&1
