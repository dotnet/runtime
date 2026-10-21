#!/bin/bash
# Traced runs for the pause table: heap x4, 3 workloads, nonmoving and nursery, 2 repetitions, two parallel streams.
# acc = accepted e8d2d23 (the older tracer, pause from the runtime counter), s6 and int use the P17 tracer (S0 timers).
# usage: bench-trace.sh <p17-scripts-dir> <out-dir> <acc-frame> <s6-frame> <int-frame>
set -u
P17=$1; OUT=$2; ACC=$3; S6=$4; INT=$5
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
      order="acc s6 int"
      [ $((rep % 2)) -eq 1 ] && order="int s6 acc"
      for b in $order; do
        case $b in
          acc) PROG=/root/gcbench/trace/gctrace.dll; FRAME=$ACC; X="";;
          s6) PROG=/root/gcbench/trace19/gctrace.dll; FRAME=$S6; X=$EXTRA;;
          int) PROG=/root/gcbench/trace19/gctrace.dll; FRAME=$INT; X=$EXTRA;;
        esac
        PROG=$PROG bash "$P17/run-one.sh" "$FRAME" $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$X" > "$OUT/$b-$w-$h-$n$mm-r$rep.txt" 2>&1
      done
    done
  done
}
taskset -c 0-4 bash -c "$(declare -f stream); P17=$P17 OUT=$OUT ACC=$ACC S6=$S6 INT=$INT CASES='$CASES' MODES='$MODES' EXTRA='$EXTRA'; stream 0" &
taskset -c 5-9 bash -c "$(declare -f stream); P17=$P17 OUT=$OUT ACC=$ACC S6=$S6 INT=$INT CASES='$CASES' MODES='$MODES' EXTRA='$EXTRA'; stream 1" &
wait
echo "trace runs finished: $(ls "$OUT" | wc -l) files"
