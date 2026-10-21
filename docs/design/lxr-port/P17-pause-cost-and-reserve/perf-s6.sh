#!/bin/bash
# Profile the three workloads at a 4x heap in nursery-copy mode (hard limit as in the matrices) for one build.
# usage: perf-s6.sh <frame> <label>     output: /root/p17s6/results/perf/<label>-<workload><heap>.{data,self.txt,incl.txt}
FRAME=$1
LABEL=$2
S=/root/p17s6/scripts
for c in binarytrees:40 requests:84 listsuffix:40; do
  IFS=: read w h <<< "$c"
  TOP=5 bash $S/perf-run.sh "$FRAME" $w $h 1 0 "$LABEL-$w$h" > /dev/null 2>&1
  tail -1 /root/p17s6/results/perf/$LABEL-$w$h.run.txt | cut -c1-200
  bash $S/perf-report.sh /root/p17s6/results/perf/$LABEL-$w$h.data 22 > /root/p17s6/results/perf/$LABEL-$w$h.report.txt 2>&1
done
