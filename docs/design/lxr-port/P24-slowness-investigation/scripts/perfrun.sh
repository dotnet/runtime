#!/bin/bash
# Sample where one run spends CPU (user and kernel samples if the kernel allows it).
# usage: perfrun.sh <workload> <arm> <name> [NAME=VALUE ...]
# Output: /root/p24/results/perf/<name>.{data,run.txt,threads.txt,top.txt}. Uses the unstripped p24 frame.
W=$1; ARM=$2; NAME=$3; shift 3
OUT=/root/p24/results/perf; mkdir -p $OUT
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
EVENT=${PERF_EVENT:-cpu-clock:u}
for a in "$@"; do case $a in PERF_EVENT=*) EVENT=${a#PERF_EVENT=} ;; esac; done
FRAME=${FRAME:-/root/p24/frames/p24-Release-linux}
declare -A SIZE=( [binarytrees]="16 0" [requests]="32768 600000" [listsuffix]="4096 800000" )
declare -A MIN=( [binarytrees]=10 [requests]=21 [listsuffix]=10 )
heap=$(( ${MIN[$W]} * 4 )); los=$(( heap / 8 )); [ $los -lt 8 ] && los=8
nursery=0; [ "$ARM" = "lxr-nursery" ] && nursery=1
envs=(DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=1 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0)
case $ARM in
  lxr-*) envs+=(DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$nursery
                DOTNET_LXREvacuateMature=0 DOTNET_LXRImmixInitialMB=$(( heap - los )) DOTNET_LXRLOSInitialMB=$los
                DOTNET_LXRManagedHeapHardLimitMB=$heap) ;;
  wks) envs+=(DOTNET_GCHeapHardLimit=$(printf '%x' $(( heap * 1048576 )))) ;;
esac
env -i PATH=/usr/bin:/bin "${envs[@]}" "$@" $PERF record -o $OUT/$NAME.data -F 499 -e $EVENT --call-graph fp -- \
  $FRAME/corerun /root/p24/tracer-bin/gctrace.dll $W ${SIZE[$W]} run > $OUT/$NAME.run.txt 2>&1
tail -1 $OUT/$NAME.run.txt | cut -c1-300
$PERF report -i $OUT/$NAME.data --stdio --no-children --sort tid -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | head -20 > $OUT/$NAME.threads.txt
$PERF report -i $OUT/$NAME.data --stdio --no-children --sort dso,symbol -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | head -40 | cut -c1-200 > $OUT/$NAME.top.txt
cat $OUT/$NAME.threads.txt; echo; cat $OUT/$NAME.top.txt
