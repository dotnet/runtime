#!/bin/bash
# Sample where one port run spends user CPU. usage: perf-run.sh <frame> <workload> <heapMiB> <nursery> <mature> <name>
FRAME=$1; W=$2; H=$3; NUR=$4; MAT=$5; NAME=$6
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
case $W in
  binarytrees) S=16; O=0;;
  requests) S=32768; O=600000;;
  listsuffix) S=4096; O=800000;;
esac
los=$(( H / 8 )); [ $los -lt 8 ] && los=8
OUT=/root/p17/results/perf
mkdir -p $OUT
cd /root/gcbench
env -i PATH=/usr/bin:/bin DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=0 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0 \
  DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$NUR DOTNET_LXREvacuateMature=$MAT \
  DOTNET_LXRImmixInitialMB=$(( H - los )) DOTNET_LXRLOSInitialMB=$los DOTNET_LXRManagedHeapHardLimitMB=$H \
  $PERF record -o $OUT/$NAME.data -F 499 -e cpu-clock:u -g -- $FRAME/corerun /root/gcbench/net/gcbench.dll $W $S $O run > $OUT/$NAME.run.txt 2>&1
$PERF report -i $OUT/$NAME.data --no-children --sort dso,symbol --stdio 2>/dev/null | grep -v '^#' | grep -v '^$' | grep -E '^ +[0-9]' | head -${TOP:-25} > $OUT/$NAME.top.txt
cat $OUT/$NAME.top.txt
