#!/bin/bash
# Traced runs: per-collection phase output of one build.
# usage: [TRACE_EXTRA=name=id,...] trace-runs.sh <frame> <label> [workload:heap:nursery:mature ...]
FRAME=$1; LABEL=$2; shift 2
CASES=${@:-"requests:84:0:0 requests:84:1:0 binarytrees:40:0:0 listsuffix:40:0:0"}
OUT=/root/p17/results/trace-$LABEL
mkdir -p $OUT
for c in $CASES; do
  IFS=: read w h n m <<< "$c"
  PROG=/root/gcbench/trace17/gctrace.dll bash /root/p17/scripts/run-one.sh $FRAME $w $h $n $m GCTRACE=1 \
    GCTRACE_EXTRA="${TRACE_EXTRA:-stamp_ns=243}" > $OUT/$w-$h-$n$m.txt 2>&1
  echo "$w $h $n$m: $(tail -1 $OUT/$w-$h-$n$m.txt | cut -c1-200)"
done
