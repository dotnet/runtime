#!/bin/bash
# Record the policy state at every lazy-sweep end under gdb (research only). The run is slow because of gdb.
# usage: gdb-policy.sh <workload> <arm> <out-json> [NAME=VALUE ...]
W=$1; ARM=$2; OUT=$3; shift 3
FRAME=/root/p24/frames/p24-Release-linux
declare -A SIZE=( [binarytrees]="16 0" [requests]="32768 600000" [listsuffix]="4096 800000" )
declare -A MIN=( [binarytrees]=10 [requests]=21 [listsuffix]=10 )
heap=$(( ${MIN[$W]} * 4 )); los=$(( heap / 8 )); [ $los -lt 8 ] && los=8
nursery=0; [ "$ARM" = "lxr-nursery" ] && nursery=1
export LXR_GDB_OUTPUT=$OUT
env -i PATH=/usr/bin:/bin LXR_GDB_OUTPUT=$OUT DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=1 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 \
  DOTNET_gcServer=0 DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$nursery \
  DOTNET_LXREvacuateMature=0 DOTNET_LXRImmixInitialMB=$(( heap - los )) DOTNET_LXRLOSInitialMB=$los \
  DOTNET_LXRManagedHeapHardLimitMB=$heap "$@" \
  gdb -batch -x /root/p24/scripts/policy-states.py --args $FRAME/corerun /root/gcbench/net/gcbench.dll $W ${SIZE[$W]} run > $OUT.gdb.log 2>&1
echo "gdb exit=$?"; tail -3 $OUT.gdb.log
