#!/bin/bash
# Traced runs for the S6 before/after tables: accepted build (P15 tracer), the S5b tip and the S6 build (S5b tracer),
# heap x4 with the hard limit as in the matrices, three modes, repetitions in an interleaved order.
# usage: trace-s6.sh <s5b-frame> <s6-frame> [repetitions] [output-name]
S5=$1
S6=$2
REPS=${3:-3}
NAME=${4:-trace-s6}
ACC=/root/lxr-batching-d2eccaea/after-Release-linux
OUT=/root/p17s6/results/$NAME
mkdir -p $OUT
CASES="binarytrees:40 requests:84 listsuffix:40"
MODES="0:0 1:0 1:1"
EXTRA="stamp_ns=243,native_planes=38,gc_committed=26"
EXTRA6="$EXTRA,reg_counted=450,reg_registered=451,reg_retained=452,reg_released=453,reg_decided=454,reg_dropped=455,reg_taken=456,reg_walked=457,touched_cleared=458,reg_capacity_bytes=459,reg_pending=460,reg_duplicates=461"
for rep in $(seq 1 $REPS); do
  for c in $CASES; do
    IFS=: read w h <<< "$c"
    for m in $MODES; do
      IFS=: read n mm <<< "$m"
      order="acc s5 s6"
      [ $((rep % 2)) -eq 0 ] && order="s6 s5 acc"
      [ $((rep % 3)) -eq 0 ] && order="s5 s6 acc"
      for b in $order; do
        case $b in
          acc) PROG=/root/gcbench/trace/gctrace.dll; FRAME=$ACC; X="";;
          s5) PROG=/root/gcbench/trace19/gctrace.dll; FRAME=$S5; X=$EXTRA;;
          s6) PROG=/root/gcbench/trace19/gctrace.dll; FRAME=$S6; X=$EXTRA6;;
        esac
        PROG=$PROG bash /root/p17s6/scripts/run-one.sh $FRAME $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$X" \
          > $OUT/$b-$w-$h-$n$mm-r$rep.txt 2>&1
      done
      echo "rep $rep $w $h $n$mm done"
    done
  done
done
