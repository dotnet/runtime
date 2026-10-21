#!/bin/bash
# Traced runs for the S4 before/after tables: accepted build (P15 tracer), the S3 tip and the S4 build (P17 tracer),
# heap x4, non-moving and nursery-copy modes, repetitions in an interleaved order.
# usage: trace-s4.sh <s3-frame> <s4-frame> [repetitions] [output-name]
S3=$1
S4=$2
REPS=${3:-3}
NAME=${4:-trace-s4}
ACC=/root/lxr-batching-d2eccaea/after-Release-linux
OUT=/root/p17s4/results/$NAME
mkdir -p $OUT
CASES="binarytrees:40 requests:84 listsuffix:40"
MODES="0:0 1:0"
EXTRA="stamp_ns=243,native_planes=38,gc_committed=26"
for rep in $(seq 1 $REPS); do
  for c in $CASES; do
    IFS=: read w h <<< "$c"
    for m in $MODES; do
      IFS=: read n mm <<< "$m"
      order="acc s3 s4"
      [ $((rep % 2)) -eq 0 ] && order="s4 s3 acc"
      [ $((rep % 3)) -eq 0 ] && order="s3 s4 acc"
      for b in $order; do
        case $b in
          acc) PROG=/root/gcbench/trace/gctrace.dll; FRAME=$ACC; X="";;
          s3) PROG=/root/gcbench/trace17/gctrace.dll; FRAME=$S3; X=$EXTRA;;
          s4) PROG=/root/gcbench/trace18/gctrace.dll; FRAME=$S4; X=$EXTRA;;
        esac
        PROG=$PROG bash /root/p17s4/scripts/run-one.sh $FRAME $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$X" \
          > $OUT/$b-$w-$h-$n$mm-r$rep.txt 2>&1
      done
      echo "rep $rep $w $h $n$mm done"
    done
  done
done
