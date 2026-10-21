#!/bin/bash
# Managed controls for S6: build the collector with one S6 control define (the product code has the same switches as the
# native test), run the managed cells that depend on the rule, and print the result. Each control must make a cell fail.
# usage: controls-s6-managed.sh <repo-wsl-path>
S=${1:?repo path}
R=/root/p17s6
cd $R
[ -z "${ONLY:-}" ] && bash scripts/build-linux.sh $S Release ctl > logs/ctl-first.log 2>&1
OBJ=$R/obj-ctl-Release
FRAME=$R/frames/ctl-Release-linux
export EXTRA_ENV="DOTNET_LXRImmixInitialMB=192 DOTNET_LXRLOSInitialMB=32 DOTNET_LXRManagedHeapHardLimitMB=224"
run() { # control cells modes
  c=$1
  [ -n "${ONLY:-}" ] && [[ " $ONLY " != *" $c "* ]] && return
  cmake -DCMAKE_CXX_FLAGS="-DLXR_S6_${c}_CONTROL" $OBJ > logs/ctl-$c-configure.log 2>&1
  ninja -C $OBJ clrlxrgc > logs/ctl-$c-build.log 2>&1 || { echo "control $c: BUILD FAILED"; return; }
  cp --remove-destination $OBJ/gc/libclrlxrgc.so $FRAME/libclrlxrgc.so
  for m in $3; do
    for cell in $2; do
      out=$(TAILN=30 TMO=300 bash cell1.sh $FRAME $m $cell Release /root/p17s6/fixtures 2>&1)
      res=$(echo "$out" | grep -E '^PASS|Fatal error|Out of memory|InvalidOperationException|Unhandled|Segmentation|ASSERT|dumped core' | head -1 | cut -c1-140)
      echo "control $c | $m | $cell :: ${res:-no result line}"
    done
  done
}
run NO_RETAIN "finalizer-churn resurrection" "nursery"
run NO_DECIDE "prompt-collect finalizer-churn" "nursery"
run MARKED_READY "resurrection pooled-reregister" "nursery"
run RC_DECIDE "pooled-reregister" "nursery"
run NO_DEDUPE "pooled-reregister" "nursery"
run UNSAFE_APPEND "finalizer-churn-concurrent" "nursery"
run NO_RELOCATE "pooled-reregister resurrection finalizer-churn" "nursery mature"
run NO_TRACE "resurrection finalizer-churn" "nursery"
# restore the plain build in the control frame so that nobody uses a broken module by accident
cmake -DCMAKE_CXX_FLAGS="" $OBJ > /dev/null 2>&1
