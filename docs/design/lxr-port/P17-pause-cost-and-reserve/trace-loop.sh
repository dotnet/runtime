#!/bin/bash
# Repeat the traced listsuffix 4x nursery run of one frame, two parallel streams, and report the failed runs.
# usage: trace-loop.sh <frame> <label> <runs-per-stream> [PROG=trace17|trace]
FRAME=$1; LABEL=$2; N=$3; KIND=${4:-trace17}
OUT=/root/p17s2/results/loop-$LABEL
rm -rf $OUT; mkdir -p $OUT
CORES=("0-4" "5-9")
if [ "$KIND" = trace ]; then PROG=/root/gcbench/trace/gctrace.dll; EXTRA=""; else PROG=/root/gcbench/trace17/gctrace.dll; EXTRA="stamp_ns=243"; fi
for s in 0 1; do
  (
    for i in $(seq 1 $N); do
      PROG=$PROG taskset -c "${CORES[$s]}" bash /root/p17s2/scripts/run-one.sh $FRAME listsuffix 40 1 0 GCTRACE=1 GCTRACE_EXTRA="$EXTRA" > $OUT/s$s-r$i.txt 2>&1
    done
  ) &
done
wait
total=$(ls $OUT/*.txt | wc -l)
failed=$(grep -l -E '^Fatal error|DIAG' $OUT/*.txt | wc -l)
completed=$(grep -l '^{"runtime"' $OUT/*.txt | wc -l)
echo "$LABEL: runs=$total fatal_or_diag=$failed completed_with_result=$completed"
