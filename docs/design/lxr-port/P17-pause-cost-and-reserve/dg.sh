#!/bin/bash
# usage: dg.sh <frame> <workload> <heapMiB> <nursery> <mature> <timeout> <name> [NAME=VALUE ...]   (stderr DIAG lines -> /root/p17/dg/<name>.err)
FRAME=$1; W=$2; H=$3; NUR=$4; MAT=$5; TMO=$6; NAME=$7
shift 7
case $W in
  binarytrees) S=16; O=0;;
  requests) S=32768; O=${OPS:-600000};;
  listsuffix) S=4096; O=800000;;
esac
los=$(( H / 8 )); [ $los -lt 8 ] && los=8
mkdir -p /root/p17/dg
cd /root/gcbench
env -i PATH=/usr/bin:/bin LXR_DIAG=1 DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT \
  DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H "$@" \
  timeout $TMO taskset -c ${CORES:-15} $FRAME/corerun /root/gcbench/net/gcbench.dll $W $S $O run > /root/p17/dg/$NAME.out 2> /root/p17/dg/$NAME.err
echo "$NAME exit=$? pauses=$(grep -c '^DIAG' /root/p17/dg/$NAME.err) $(grep -o -E '"elapsed_ms":[0-9.]+' /root/p17/dg/$NAME.out)"
