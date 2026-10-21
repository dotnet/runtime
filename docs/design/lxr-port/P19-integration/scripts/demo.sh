#!/bin/bash
# Run one demo program once, with the default GC ("default") or with LXR ("lxr").
# usage: demo.sh <frame> [default|lxr] [workload size ops]
#   workload size ops: binarytrees 16 0 | requests 32768 600000 | listsuffix 4096 800000
# GCBENCH is the path of gcbench.dll (default: $HOME/gcbench/net/gcbench.dll; build it from
# docs/design/lxr-port/P14-natural-gc/GcBench.csproj).
FRAME=$1
MODE=${2:-default}
W=${3:-binarytrees}; S=${4:-16}; O=${5:-0}
GCBENCH=${GCBENCH:-$HOME/gcbench/net/gcbench.dll}
if [ "$MODE" = lxr ]; then
  env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
    DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=0 DOTNET_LXREvacuateMature=0 \
    DOTNET_LXRImmixInitialMB=80 DOTNET_LXRLOSInitialMB=20 DOTNET_LXRManagedHeapHardLimitMB=100 \
    timeout 300 "$FRAME/corerun" "$GCBENCH" "$W" "$S" "$O" run 2>&1 | tail -5
else
  env -i PATH=/usr/bin:/bin timeout 300 "$FRAME/corerun" "$GCBENCH" "$W" "$S" "$O" run 2>&1 | tail -5
fi
