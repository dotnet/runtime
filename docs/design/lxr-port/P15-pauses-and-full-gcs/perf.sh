#!/bin/bash
# Research-only: sample where our port spends CPU while running the requests program (nonmoving, 4x heap).
# usage: perf.sh <name> <workload> <size> <ops> <heapMiB> <nursery> <mature>
set -u
NAME=$1; W=$2; S=$3; O=$4; H=$5; NUR=$6; MAT=$7
NET=/root/lxr-batching-d2eccaea/after-Release-linux
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
OUT=/root/gcbench/results/p15-perf
mkdir -p "$OUT"
cd /root/gcbench
los=$(( H / 8 )); [ $los -lt 8 ] && los=8
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$NET/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT \
  DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H \
  $PERF record -o "$OUT/$NAME.data" -F 499 -e cpu-clock:u --call-graph fp -- $NET/corerun net/gcbench.dll $W $S $O > "$OUT/$NAME.run.txt" 2>&1
echo "perf exit=$?"
tail -1 "$OUT/$NAME.run.txt" | cut -c1-200
