#!/bin/bash
# Make the S2 result tables from the matrix and the traced runs, and copy the raw files into the repository folder.
R=/mnt/c/Users/konradkokosa/.copilot/repos/copilot-worktrees/runtime-fork/kkokosa-scaling-fiesta/docs/design/lxr-port/P17-pause-cost-and-reserve/results
S=/root/p17s2/scripts
M=/root/p17s2/results/matrix-m-s2.jsonl
cp $M $R/matrix-s2-acc-l1-s2.jsonl
python3 $S/summarize.py $M --base acc > $R/summary-s2.md
for pair in "l1 acc" "s2 acc" "s2 l1"; do
  set -- $pair
  python3 $S/compare.py $M --base $2 --new $1 > $R/compare-wall-$1-vs-$2.md
done
python3 $S/compare.py $M --base l1 --new s2 --metric gc_ms > $R/compare-gcms-s2-vs-l1.md
python3 $S/compare.py $M --base l1 --new s2 --metric cpu_ms > $R/compare-cpums-s2-vs-l1.md
# pause tables: pool the three repetitions of each cell
T=/root/p17s2/results/trace-s2
args=()
for b in acc l1 s2; do
  for c in binarytrees-40 requests-84 listsuffix-40; do
    for m in 00 10; do
      f=/root/p17s2/results/trace-s2/pool-$b-$c-$m.txt
      cat $T/$b-$c-$m-r?.txt > $f
      args+=("$b/$c/$m=$f")
    done
  done
done
python3 $S/pause-table.py "${args[@]}" > $R/pause-tables-s2-x4.md
for b in acc l1 s2; do for c in binarytrees-40 requests-84 listsuffix-40; do for m in 00 10; do
  for r in 1 2 3; do echo "$b $c $m r$r $(grep -c trace_event= $T/$b-$c-$m-r$r.txt) events $(grep '^{' $T/$b-$c-$m-r$r.txt | tail -1 | python3 -c 'import sys,json; j=json.loads(sys.stdin.read()); print(j.get("checksum"), j.get("elapsed_ms"))')"; done
done; done; done > $R/trace-runs-s2.txt
echo done
