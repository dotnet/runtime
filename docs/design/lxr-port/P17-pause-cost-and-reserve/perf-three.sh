#!/bin/bash
# Profile the three workloads at a 4x heap in nursery-copy mode for one build.
# usage: perf-three.sh <frame> <label>
FRAME=$1
LABEL=$2
S=/root/p17s2/scripts
for c in binarytrees:40 requests:84 listsuffix:40; do
  IFS=: read w h <<< "$c"
  TOP=5 bash $S/perf-run.sh "$FRAME" $w $h 1 0 "$LABEL-$w$h" > /dev/null 2>&1
  tail -1 /root/p17s2/results/perf/$LABEL-$w$h.run.txt | cut -c1-200
  bash $S/perf-report.sh /root/p17s2/results/perf/$LABEL-$w$h.data 22 > /root/p17s2/results/perf/$LABEL-$w$h.report.txt 2>&1
done
