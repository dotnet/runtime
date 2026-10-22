#!/bin/bash
# Make the text reports of the raw results and copy everything (without the perf .data files) to the repository.
# usage: collect.sh <worktree-wsl-path>
W=$1
R=/root/p24/results
mkdir -p $R/reports
for f in $R/trace*.txt; do
  [ -f "$f" ] && python3 /root/p24/scripts/phases.py "$f" > $R/reports/phases-$(basename "$f" .txt).txt 2>&1
done
/root/p24/scripts/perfalloc.sh > $R/reports/perf-alloc.txt 2>&1
/root/p24/scripts/startup.sh > $R/startup.txt 2>&1
for f in $R/rep-ls-nur/run*.txt; do :; done
python3 /root/p24/scripts/matrix-table.py $R/matrix1.jsonl $R/matrix2-batch.jsonl > $R/reports/matrix-table.txt
python3 /root/p24/scripts/split-table.py $R/matrix1.jsonl $R/matrix2-batch.jsonl > $R/reports/split-table.txt
for p in $R/policy-*.json; do python3 /root/p24/scripts/policy-table.py $p > $R/reports/$(basename $p .json).txt 2>&1; done
python3 /root/p24/scripts/java-summary.py $R/java/lxr-binarytrees.txt $R/java/lxr-requests.txt $R/java/lxr-listsuffix.txt > $R/reports/java-summary.txt
for d in $R/perf/*.data; do
  n=$(basename $d .data)
  /root/p24/scripts/perfrep.sh $n > $R/reports/perf-$n.txt 2>&1
done
cat $R/cells/*.txt 2>/dev/null | grep -h '^{"runtime"' | wc -l > /dev/null
# one summary line per cell run (e2..e6)
python3 - <<'EOF' > $R/reports/cells-summary.txt
import glob, json, os
for p in sorted(glob.glob("/root/p24/results/cells/*.txt")):
    summ = None; wall = None
    for line in open(p, errors="replace"):
        if line.startswith('{"runtime"'):
            summ = json.loads(line)
        if line.startswith("wall_s="):
            wall = float(line.split("=")[1])
    name = os.path.basename(p)[:-4]
    if not summ:
        print(name, "FAILED"); continue
    th = sorted(summ.get("threads", []), key=lambda t: -(t["u"] + t["s"]))[:6]
    print(f'{name}: wall={wall:.2f}s elapsed={summ["elapsed_ms"]/1000:.2f}s cpu={summ["cpu_ms"]/1000:.2f}s gc={summ["gc_ms"]/1000:.2f}s '
          f'pauses={summ.get("lxr_pauses","-")} im={summ.get("lxr_initial_mark","-")} fm={summ.get("lxr_final_mark","-")} full={summ.get("lxr_full","-")} '
          f'rss={summ["peak_rss_kb"]//1024}MiB threads(u+s ms)=' + " ".join(f'{t["n"][:8]}:{t["u"]}+{t["s"]}' for t in th))
EOF
D=$W/docs/design/lxr-port/P24-slowness-investigation/results
mkdir -p $D
rsync -a --exclude '*.data' --exclude '*.data.old' --exclude 'cells' $R/ $D/
mkdir -p $D/cells
cp $R/cells/*.txt $D/cells/ 2>/dev/null
find $D -name '*.txt' -o -name '*.json' -o -name '*.jsonl' | head -3 > /dev/null
du -sh $D
