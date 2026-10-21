#!/bin/bash
# One natural-collection run of a port collector with no hard limit (P17 S5b): the heap size is the initial admitted
# size, and the reservation is larger (Immix 1024 MiB, large objects 256 MiB unless IMMIX_RESERVE / LOS_RESERVE is set).
# usage: [PROG=<dll>] run-nolimit.sh <frame-dir> <workload> <heapMiB> <nursery 0|1> <mature 0|1> [NAME=VALUE ...]
FRAME=$1; W=$2; H=$3; NUR=$4; MAT=$5; PROG=${PROG:-/root/gcbench/net/gcbench.dll}
shift 5
case $W in
  binarytrees) S=16; O=0;;
  requests) S=32768; O=600000;;
  listsuffix) S=4096; O=800000;;
esac
los=$(( H / 8 )); [ $los -lt 8 ] && los=8
cd /root/gcbench
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=0 DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT \
  DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los \
  DOTNET_LXRImmixReserveMB=${IMMIX_RESERVE:-1024} DOTNET_LXRLOSReserveMB=${LOS_RESERVE:-256} "$@" \
  timeout 400 $FRAME/corerun $PROG $W $S $O run 2>&1
