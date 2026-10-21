#!/bin/bash
# Short requests run with a chosen operation count. usage: short-run.sh <frame> <heapMiB> <nursery> <mature> <ops> [NAME=VALUE ...]
FRAME=$1; H=$2; NUR=$3; MAT=$4; OPS=$5
shift 5
los=$(( H / 8 )); [ $los -lt 8 ] && los=8
cd /root/gcbench
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT \
  DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H "$@" \
  timeout 300 $FRAME/corerun /root/gcbench/net/gcbench.dll requests 32768 $OPS run 2>&1 | grep '^{' | grep -o -E '"elapsed_ms":[0-9.]+|"gc_count":[0-9]+|"gc_ms":[0-9]+|"lxr_full":[0-9]+|"peak_rss_kb":[0-9]+'  | tr '\n' ' '
echo
