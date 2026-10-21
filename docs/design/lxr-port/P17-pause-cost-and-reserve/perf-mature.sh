#!/bin/bash
# Profile the three workloads at a 4x heap in the mature-copy mode (nursery and mature evacuation on) for one build.
# usage: perf-mature.sh <frame> <label>
FRAME=$1
LABEL=$2
S=/root/p17s2/scripts
for c in binarytrees:40 requests:84 listsuffix:40; do
  IFS=: read w h <<< "$c"
  TOP=3 bash $S/perf-run.sh "$FRAME" $w $h 1 1 "$LABEL-$w$h" > /dev/null 2>&1
  tail -1 /root/p17s2/results/perf/$LABEL-$w$h.run.txt | cut -c1-160
  bash $S/perf-report.sh /root/p17s2/results/perf/$LABEL-$w$h.data 14 > /root/p17s2/results/perf/$LABEL-$w$h.report.txt 2>&1
done
python3 $S/perf-shares.py bt=/root/p17s2/results/perf/$LABEL-binarytrees40 req=/root/p17s2/results/perf/$LABEL-requests84 list=/root/p17s2/results/perf/$LABEL-listsuffix40
