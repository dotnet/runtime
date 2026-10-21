#!/bin/bash
# Research-only: does the polling thread change what our port does? Same program, with and without GCTRACE.
# usage: observer-effect.sh <workload> <size> <ops> <heapMiB>
W=$1; S=$2; O=$3; H=$4
NET=/root/lxr-batching-d2eccaea/after-Release-linux
cd /root/gcbench
los=$(( H / 8 )); [ $los -lt 8 ] && los=8
for trace in 0 1 0 1; do
  out=$(env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
    DOTNET_GCPath=$NET/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=0 DOTNET_LXREvacuateMature=0 \
    DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H GCTRACE=$trace \
    timeout 300 $NET/corerun trace/gctrace.dll $W $S $O 2>&1 | grep '^{')
  echo "GCTRACE=$trace $(echo "$out" | grep -o -E '"elapsed_ms":[0-9.]+|"gc_count":[0-9]+|"gc_ms":[0-9]+|"lxr_pauses":[0-9]+|"lxr_initial_mark":[0-9]+|"lxr_final_mark":[0-9]+|"lxr_full":[0-9]+' | tr '\n' ' ')"
done
