#!/bin/bash
# Traced runs for the S3 before/after tables: accepted build (P15 tracer), the S2 tip and the S3 build (P17 tracer),
# heap x4, non-moving and nursery-copy modes, three repetitions in an interleaved order. The P17 tracer also records
# the native plane commit size (metric 38) and the managed committed bytes (26) after each collection. The extra
# counters 7, 8, 9 and 10 are NOT plane sizes (blocks released, root increments, deaths, initial marks): the names
# extent_commit, live_commit, lock_commit and scan_commit in the raw traces are wrong and are not used.
# usage: trace-s3.sh <s2-frame> <s3-frame> [repetitions]
S2=$1
S3=$2
REPS=${3:-3}
ACC=/root/lxr-batching-d2eccaea/after-Release-linux
OUT=/root/p17s3/results/trace-s3
mkdir -p $OUT
CASES="binarytrees:40 requests:84 listsuffix:40"
MODES="0:0 1:0"
EXTRA="stamp_ns=243,native_planes=38,extent_commit=7,live_commit=10,lock_commit=8,scan_commit=9,gc_committed=26"
for rep in $(seq 1 $REPS); do
  for c in $CASES; do
    IFS=: read w h <<< "$c"
    for m in $MODES; do
      IFS=: read n mm <<< "$m"
      order="acc s2 s3"
      [ $((rep % 2)) -eq 0 ] && order="s3 s2 acc"
      [ $((rep % 3)) -eq 0 ] && order="s2 s3 acc"
      for b in $order; do
        case $b in
          acc) PROG=/root/gcbench/trace/gctrace.dll; FRAME=/root/lxr-batching-d2eccaea/after-Release-linux; X="";;
          s2) PROG=/root/gcbench/trace17/gctrace.dll; FRAME=$S2; X=$EXTRA;;
          s3) PROG=/root/gcbench/trace17/gctrace.dll; FRAME=$S3; X=$EXTRA;;
        esac
        PROG=$PROG bash /root/p17s3/scripts/run-one.sh $FRAME $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$X" \
          > $OUT/$b-$w-$h-$n$mm-r$rep.txt 2>&1
      done
      echo "rep $rep $w $h $n$mm done"
    done
  done
done
