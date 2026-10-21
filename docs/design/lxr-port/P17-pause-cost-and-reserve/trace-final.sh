#!/bin/bash
# Traced runs for the before/after tables: base (P15 tracer) and the new builds (P17 tracer), heap x4, two modes.
OUT=/root/p17/results/trace-final
mkdir -p $OUT
CASES="binarytrees:40 requests:84 listsuffix:40"
MODES="0:0 1:0"
for c in $CASES; do
  IFS=: read w h <<< "$c"
  for m in $MODES; do
    IFS=: read n mm <<< "$m"
    # base: P15 tracer
    PROG=/root/gcbench/trace/gctrace.dll bash /root/p17/scripts/run-one.sh /root/lxr-batching-d2eccaea/after-Release-linux $w $h $n $mm GCTRACE=1 > $OUT/base-$w-$h-$n$mm.txt 2>&1
    for b in s0b s5e s1d; do
      EXTRA="stamp_ns=243"
      [ $b != s0b ] && EXTRA="stamp_ns=243,surv_e9=245,surv_bytes=246,reserve_pages=247,free_pages=248,flip_overlaps=249,flip_blocked=250"
      PROG=/root/gcbench/trace17/gctrace.dll bash /root/p17/scripts/run-one.sh /root/p17/frames/$b-Release-linux $w $h $n $mm GCTRACE=1 GCTRACE_EXTRA=$EXTRA > $OUT/$b-$w-$h-$n$mm.txt 2>&1
    done
    echo "done $w $h $n$mm"
  done
done
