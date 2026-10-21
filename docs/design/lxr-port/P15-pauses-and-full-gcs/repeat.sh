#!/bin/bash
# Research-only: repeat one port run several times and report exit status and first error line.
# usage: repeat.sh <runs> <workload> <size> <ops> <heapMiB> [nursery] [mature]
N=$1; W=$2; S=$3; O=$4; H=$5; NUR=${6:-0}; MAT=${7:-0}
NET=/root/lxr-batching-d2eccaea/after-Release-linux
cd /root/gcbench
los=$(( H / 8 )); [ $los -lt 8 ] && los=8
for i in $(seq 1 $N); do
  out=$(env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
    DOTNET_GCPath=$NET/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT \
    DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H \
    timeout 300 $NET/corerun net/gcbench.dll $W $S $O 2>&1)
  rc=$?
  echo "run $i exit=$rc :: $(echo "$out" | head -1 | cut -c1-90) :: $(echo "$out" | grep -o '"elapsed_ms":[0-9.]*' | head -1)"
done
