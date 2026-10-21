#!/bin/bash
# Traced runs for the S2 before/after pause tables: accepted build (P15 tracer) and the layer-1 and S2 builds
# (P17 tracer), heap x4, non-moving and nursery-copy modes, three repetitions in an interleaved order.
# usage: trace-s2.sh <layer1-frame> <s2-frame> [repetitions]
L1=$1
S2=$2
REPS=${3:-3}
ACC=/root/lxr-batching-d2eccaea/after-Release-linux
OUT=/root/p17s2/results/trace-s2
mkdir -p $OUT
CASES="binarytrees:40 requests:84 listsuffix:40"
MODES="0:0 1:0"
for rep in $(seq 1 $REPS); do
  for c in $CASES; do
    IFS=: read w h <<< "$c"
    for m in $MODES; do
      IFS=: read n mm <<< "$m"
      order="acc l1 s2"
      [ $((rep % 2)) -eq 0 ] && order="s2 l1 acc"
      [ $((rep % 3)) -eq 0 ] && order="l1 s2 acc"
      for b in $order; do
        case $b in
          acc) PROG=/root/gcbench/trace/gctrace.dll; FRAME=$ACC; EXTRA="";;
          l1) PROG=/root/gcbench/trace17/gctrace.dll; FRAME=$L1; EXTRA="stamp_ns=243";;
          s2) PROG=/root/gcbench/trace17/gctrace.dll; FRAME=$S2; EXTRA="stamp_ns=243";;
        esac
        PROG=$PROG bash /root/p17s2/scripts/run-one.sh $FRAME $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$EXTRA" \
          > $OUT/$b-$w-$h-$n$mm-r$rep.txt 2>&1
      done
      echo "rep $rep $w $h $n$mm done"
    done
  done
done
