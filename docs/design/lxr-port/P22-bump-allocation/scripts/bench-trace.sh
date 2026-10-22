#!/bin/bash
# P22 (D2): traced runs for the pause table (S0 timers of the P17 tracer). Heap x4, 3 workloads, nonmoving and nursery,
# 2 repetitions, two parallel streams on pinned cores, the two builds in alternating order.
# usage: bench-trace.sh <out-dir>     (labels: d1 = D1 tip 74dc9e84445, d2 = this branch)
set -u
. "$(dirname "$0")/env.sh"
P17=$W/docs/design/lxr-port/P17-pause-cost-and-reserve
OUT=${1:-$D/trace}
D1=/root/d1/frames/d1-Release-linux; D2=$D/frames/d2-Release-linux
mkdir -p "$OUT"
CASES="binarytrees:40 requests:84 listsuffix:40"
MODES="0:0 1:0"
EXTRA="stamp_ns=243,native_planes=38,gc_committed=26"
stream() {
  rep=$1
  for c in $CASES; do
    IFS=: read w h <<< "$c"
    for m in $MODES; do
      IFS=: read n mm <<< "$m"
      order="d1 d2"
      [ $((rep % 2)) -eq 1 ] && order="d2 d1"
      for b in $order; do
        case $b in d1) FRAME=$D1;; d2) FRAME=$D2;; esac
        PROG=/root/gcbench/trace19/gctrace.dll bash "$P17/run-one.sh" "$FRAME" $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$EXTRA" DOTNET_ReadyToRun=1 > "$OUT/$b-$w-$h-$n$mm-r$rep.txt" 2>&1
      done
    done
  done
}
export -f stream
export P17 OUT D1 D2 CASES MODES EXTRA
taskset -c 0-4 bash -c "stream 0" &
taskset -c 5-9 bash -c "stream 1" &
wait
echo "trace runs finished: $(ls "$OUT" | wc -l) files"
python3 $W/docs/design/lxr-port/P19-integration/scripts/pause-pool.py "$OUT" d1 d2
