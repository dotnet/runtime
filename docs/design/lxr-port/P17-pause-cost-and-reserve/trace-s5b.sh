#!/bin/bash
# Traced runs without a hard limit (P17 S5b): accepted build (P15 tracer), the S4 tip (S4 tracer), the S5b build and the
# S5b build with LXROvershootToSpace=0 (S5b tracer). Heap x4 (initial admitted size), nursery-copy and mature modes.
# usage: trace-s5b.sh <s4-frame> <s5b-frame> [repetitions] [output-name]
S4=$1
S5=$2
REPS=${3:-3}
NAME=${4:-trace-s5b}
ACC=/root/lxr-batching-d2eccaea/after-Release-linux
OUT=/root/p17s5b/results/$NAME
mkdir -p $OUT
CASES="binarytrees:40 requests:84 listsuffix:40"
MODES="1:0 1:1"
EXTRA="borrow_pauses=410,borrow_bytes_last=412,borrow_refused=414,giveback_attempts=415,giveback_successes=416,giveback_complete=417,giveback_bytes_last=419,borrowed_now=420,borrow_ns_last=423,giveback_ns_last=424,peak_capacity=425,empty_copy_released=426,borrow_request_pages=427,borrow_free_pages=428,native_planes=38,gc_committed=26,admitted=36"
for rep in $(seq 1 $REPS); do
  for c in $CASES; do
    IFS=: read w h <<< "$c"
    for m in $MODES; do
      IFS=: read n mm <<< "$m"
      order="acc s4 s5 off"
      [ $((rep % 2)) -eq 0 ] && order="off s5 s4 acc"
      [ $((rep % 3)) -eq 0 ] && order="s4 off acc s5"
      for b in $order; do
        case $b in
          acc) PROG=/root/gcbench/trace/gctrace.dll; FRAME=$ACC; X=""; E="";;
          s4) PROG=/root/gcbench/trace18/gctrace.dll; FRAME=$S4; X=""; E="";;
          s5) PROG=/root/gcbench/trace19/gctrace.dll; FRAME=$S5; X=$EXTRA; E="";;
          off) PROG=/root/gcbench/trace19/gctrace.dll; FRAME=$S5; X=$EXTRA; E="DOTNET_LXROvershootToSpace=0";;
        esac
        PROG=$PROG bash /root/p17s5b/run-nolimit.sh $FRAME $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA="$X" $E \
          > $OUT/$b-$w-$h-$n$mm-r$rep.txt 2>&1
      done
      echo "rep $rep $w $h $n$mm done"
    done
  done
done
