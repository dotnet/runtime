#!/bin/bash
# P28: perf recording of the cells of P24 (user and kernel samples, call graph by frame pointer, 499 Hz) and the share of the
# symbols that P24 named. One run per cell, nothing else running.
# usage: perf28.sh <frame> <tag> [cells...]      cells: rq-nursery ls-nursery bt-nursery ls-nonmoving rq-nonmoving (default: first three)
# output: /root/p28/perf/<tag>-<cell>.{data,run.txt,top.txt,share.txt}
FRAME=$1; TAG=$2; shift 2
CELLS=${*:-rq-nursery ls-nursery bt-nursery}
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
OUT=/root/p28/perf
mkdir -p $OUT
declare -A SIZE=( [binarytrees]="16 0" [requests]="32768 600000" [listsuffix]="4096 800000" )
declare -A MIN=( [binarytrees]=10 [requests]=21 [listsuffix]=10 )
for cell in $CELLS; do
  case $cell in
    rq-*) W=requests ;; ls-*) W=listsuffix ;; bt-*) W=binarytrees ;;
  esac
  nursery=0; [ "${cell#*-}" = "nursery" ] && nursery=1
  heap=$(( ${MIN[$W]} * 4 )); los=$(( heap / 8 )); [ $los -lt 8 ] && los=8
  envs=(DOTNET_TieredCompilation=0 DOTNET_ReadyToRun=1 DOTNET_gcConcurrent=1 DOTNET_GCHeapCount=2 DOTNET_gcServer=0
        DOTNET_GCPath=$FRAME/libclrlxrgc.so DOTNET_LXRLegacyCapacity=1 DOTNET_LXREvacuateNursery=$nursery
        DOTNET_LXREvacuateMature=0 DOTNET_LXRImmixInitialMB=$(( heap - los )) DOTNET_LXRLOSInitialMB=$los
        DOTNET_LXRManagedHeapHardLimitMB=$heap)
  N=$TAG-$cell
  env -i PATH=/usr/bin:/bin "${envs[@]}" $PERF record -o $OUT/$N.data -F 499 -e cpu-clock --call-graph fp -- \
    $FRAME/corerun /root/p24/tracer-bin/gctrace.dll $W ${SIZE[$W]} run > $OUT/$N.run.txt 2>&1
  tail -1 $OUT/$N.run.txt | cut -c1-200
  $PERF report -i $OUT/$N.data --stdio --no-children --sort dso,symbol -g none 2>/dev/null | grep -v '^#' | grep -v '^$' | head -40 | cut -c1-200 > $OUT/$N.top.txt
  python3 /root/p28/scripts/perfshare.py $PERF $OUT/$N.data > $OUT/$N.share.txt
  echo "== $N"; cat $OUT/$N.share.txt
done
