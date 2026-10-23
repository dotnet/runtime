#!/bin/bash
# P28: a quick A/B of collector frames in one session. Natural collections, ReadyToRun on, heap 4x (the run1.sh of P24), 3 repetitions,
# the frames are interleaved (rep 1: A B C, rep 2: B C A, rep 3: C A B). One run at a time. Prints the median wall time, CPU time,
# GC time, pause count and Full count of each frame and cell, and the checksums that were seen.
# usage: quick28.sh <out-prefix> <frame> [<frame> ...]        env: CELLS="requests:lxr-nursery ..." REPS=3
# frame = a directory under /root/p25/frames
OUT=/root/p28/results/$1; shift
FRAMES=("$@")
CELLS=${CELLS:-"binarytrees:lxr-nonmoving binarytrees:lxr-nursery requests:lxr-nonmoving requests:lxr-nursery listsuffix:lxr-nonmoving listsuffix:lxr-nursery"}
REPS=${REPS:-3}
mkdir -p /root/p28/results
: > $OUT.jsonl
n=${#FRAMES[@]}
for rep in $(seq 0 $((REPS - 1))); do
  for cell in $CELLS; do
    w=${cell%%:*}; a=${cell##*:}
    for k in $(seq 0 $((n - 1))); do
      f=${FRAMES[$(( (k + rep) % n ))]}
      line=$(/root/p25/scripts26/cell.sh /root/p25/frames/$f "$w" "$a" 2>&1 | tail -1)
      echo "$f $rep $line" >> $OUT.jsonl
    done
  done
done
python3 - $OUT.jsonl > $OUT.txt <<'EOF'
import sys, re, statistics, collections
rows = collections.defaultdict(list)
for l in open(sys.argv[1]):
    m = re.match(r"(\S+) (\d+) (\S+) (\S+) .*wall=([\d.]+) cpu=([\d.]+) gc=([\d.]+) pauses=(\S+) full=(\S+) checksum=(\S+)", l)
    if not m:
        print("FAILED LINE:", l.strip())
        continue
    f, rep, w, a, wall, cpu, gc, pauses, full, cs = m.groups()
    rows[(w, a, f)].append((float(wall), float(cpu), float(gc), pauses, full, cs))
print("%-12s %-14s %-22s %7s %7s %7s %8s %6s  %s" % ("workload", "arm", "frame", "wall", "cpu", "gc", "pauses", "full", "checksums"))
for (w, a, f), v in sorted(rows.items()):
    wall = statistics.median(x[0] for x in v)
    cpu = statistics.median(x[1] for x in v)
    gc = statistics.median(x[2] for x in v)
    pauses = sorted(x[3] for x in v)[len(v) // 2]
    full = sorted(x[4] for x in v)[len(v) // 2]
    cs = sorted({x[5] for x in v})
    print("%-12s %-14s %-22s %7.2f %7.2f %7.2f %8s %6s  %s  (wall runs: %s)" % (
        w, a, f, wall, cpu, gc, pauses, full, ",".join(cs), " ".join("%.2f" % x[0] for x in v)))
EOF
cat $OUT.txt
