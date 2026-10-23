#!/bin/bash
# P28: copy the results from the WSL work directories into the repo. usage: copyres28.sh <worktree-wsl-path>
W=$1
D=$W/docs/design/lxr-port/P28-refill-and-rc-loop-parity/results
mkdir -p $D/perf
cp /root/p28/results/*.txt /root/p28/results/*.jsonl $D/
cp /root/p28/perf/*.share.txt /root/p28/perf/*.top.txt $D/perf/
for t in s28-f10e s28-base; do
  for e in bench.out table.txt checksums.txt pauses.txt matrix.jsonl matrix.log; do
    cp /root/p25/results/$t-$e $D/
  done
done
cp /root/p28/out/perf-base2.txt /root/p28/out/perf-fin.txt $D/perf/ 2>/dev/null
cp /root/p25/results/s26-final-bench.out $D/p26-final-bench.out
ls $D | wc -l
