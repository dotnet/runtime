#!/bin/bash
# usage: cell1.sh <frame-dir> <nonmoving|nursery|mature> <case|full> [Release|Debug fixture] [fixtures dir]
FRAME=$1; MODE=$2; CASE=$3; FIX=${4:-Release}; FD=${5:-/root/lxr-batching-d2eccaea}
case $MODE in nonmoving) N=0; M=0;; nursery) N=1; M=0;; mature) N=1; M=1;; esac
MOD=$FRAME/libclrlxrgc.so
if [ "$CASE" = full ]; then ARGS="$MOD"; else ARGS="--p7-lifecycle $CASE $MOD"; fi
env -i PATH=/usr/bin:/bin DOTNET_ReadyToRun=0 DOTNET_TieredCompilation=0 DOTNET_gcServer=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 \
  DOTNET_LXRLegacyCapacity=1 $EXTRA_ENV DOTNET_GCPath=$MOD DOTNET_LXREvacuateNursery=$N DOTNET_LXREvacuateMature=$M \
  timeout ${TMO:-300} $FRAME/corerun $FD/fixture-scope5-$FIX/runtime-tests.dll $ARGS 2>&1 | tail -${TAILN:-3}
echo "exit=${PIPESTATUS[0]}"
