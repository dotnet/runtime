#!/bin/bash
# P22 (D2): the allocation micro benchmark on a frame, with the LXR collector (or the default collector with "default").
# usage: alloc-bench.sh <frame> [nonmoving|nursery|default] [objects] [reps]
. "$(dirname "$0")/env.sh"
FRAME=$1; MODE=${2:-nonmoving}; N=${3:-20000000}; R=${4:-3}
S=$D/allocbench-src
if [ ! -f $D/allocbench/AllocBench.dll ]; then
  rm -rf $S; mkdir -p $S
  cp $W/docs/design/lxr-port/P22-bump-allocation/AllocBench/* $S/
  sed -i 's/\r$//' $S/*
  $T/.dotnet/dotnet build $S/AllocBench.csproj -c Release -o $D/allocbench 2>&1 | tail -2
fi
NUR=0; [ "$MODE" = nursery ] && NUR=1
if [ "$MODE" = default ]; then
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=1 $FRAME/corerun $D/allocbench/AllocBench.dll $N $R
else
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=1 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
    DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=0 \
    DOTNET_LXRImmixInitialMB=192 DOTNET_LXRLOSInitialMB=64 DOTNET_LXRManagedHeapHardLimitMB=256 $EXTRA \
    $FRAME/corerun $D/allocbench/AllocBench.dll $N $R
fi
