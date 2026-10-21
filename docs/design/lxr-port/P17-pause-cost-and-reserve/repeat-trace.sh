#!/bin/bash
# Repeat the traced listsuffix run (4x heap, nursery copy) many times, three builds interleaved, two parallel streams.
# usage: repeat-trace.sh <reps-per-stream> <layer1-frame> <s2-frame>
N=$1; L1=$2; S2=$3
ACC=/root/lxr-batching-d2eccaea/after-Release-linux
OUT=/root/p17s2/results/repeat-trace
rm -rf $OUT; mkdir -p $OUT
CORES=("0-4" "5-9")
for s in 0 1; do
  (
    for i in $(seq 1 $N); do
      for b in acc l1 s2; do
        case $b in
          acc) PROG=/root/gcbench/trace/gctrace.dll; FRAME=$ACC; EXTRA="";;
          l1) PROG=/root/gcbench/trace17/gctrace.dll; FRAME=$L1; EXTRA="stamp_ns=243";;
          s2) PROG=/root/gcbench/trace17/gctrace.dll; FRAME=$S2; EXTRA="stamp_ns=243";;
        esac
        PROG=$PROG taskset -c "${CORES[$s]}" bash /root/p17s2/scripts/run-one.sh $FRAME listsuffix 40 1 0 GCTRACE=1 GCTRACE_EXTRA="$EXTRA" > $OUT/$b-s$s-r$i.txt 2>&1
      done
    done
  ) &
done
wait
for b in acc l1 s2; do
  total=$(ls $OUT/$b-*.txt | wc -l)
  failed=$(grep -l '^Fatal error' $OUT/$b-*.txt | wc -l)
  echo "$b traced listsuffix 4x nursery: failed $failed of $total"
done
