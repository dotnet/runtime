#!/bin/bash
# P26: copy the result files of /root/p25 into the P26 results directory of the worktree.
# usage: collect26.sh <worktree-wsl-path>
W=$1
D=$W/docs/design/lxr-port/P26-scheduler-and-lazy-decrements/results
mkdir -p $D
cd /root/p25
cp results/s26-* results/p26-cpucmp-f2-f4b.txt results/b26-trace-ls-nur-c3.txt $D/ 2>/dev/null
for t in f2a f2b f4b f8 final; do
  cp out/$t-nat-Debug.txt out/$t-nat-Release.txt out/$t-legacy.txt $D/ 2>/dev/null
done
cp out/managed-final.summary $D/managed-final.summary
cp out/managed-final.txt $D/managed-final.txt 2>/dev/null
cp out/managed-f8.summary $D/managed-f8-first-run-9-failures.summary 2>/dev/null
cp out/managed-f8b.summary $D/managed-f8-after-test-hook-fix.summary 2>/dev/null
cp out/managed-f4b.summary $D/managed-f4b.summary 2>/dev/null
cp out/flake/p34-old.fails out/flake/p34-new.fails $D/ 2>/dev/null
cp out/one-Release-tsan/run3.log $D/tsan-f4b-selector.log 2>/dev/null
cp out/tsan26/*.run $D/ 2>/dev/null
ls $D | wc -l
