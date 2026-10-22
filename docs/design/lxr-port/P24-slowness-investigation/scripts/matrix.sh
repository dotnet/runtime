#!/bin/bash
# Direct comparison on one frame: arms x workloads x repetitions, one run after the other, rotated order (as compare.sh of P23).
# usage: matrix.sh <out.jsonl> <reps> "<arms>" "<workloads>" [NAME=VALUE ...]
# Arms: wks server lxr-nonmoving lxr-nursery lxr-mature (nursery copy + mature evacuation = the defaults of the collector).
# Extra NAME=VALUE pairs go to every run (for example GCBENCH_LATENCY=batch). Program: the P24 tracer binary without tracing.
OUT=$1; REPS=$2; ARMS=($3); WORKLOADS=($4); shift 4
: > $OUT
for rep in $(seq 0 $((REPS - 1))); do
  for w in "${WORKLOADS[@]}"; do
    n=${#ARMS[@]}
    for i in $(seq 0 $((n - 1))); do
      arm=${ARMS[$(( (i + rep) % n ))]}
      /root/p24/scripts/run1.sh ${FRAME:-/root/p24/frames/p24-Release-linux} /root/p24/tracer-bin/gctrace.dll $w $arm "$@" > /tmp/p24-run.txt 2>&1
      python3 - /tmp/p24-run.txt $rep $w $arm "$*" >> $OUT <<'EOF'
import json, sys
path, rep, w, arm, extra = sys.argv[1:6]
summ = None; wall = None
for line in open(path, errors="replace"):
    if line.startswith('{"runtime"'):
        summ = json.loads(line)
    if line.startswith("wall_s="):
        wall = float(line.split("=")[1])
print(json.dumps({"rep": int(rep), "workload": w, "arm": arm, "extra": extra, "wall_s": wall, "prog": summ}))
EOF
      tail -1 $OUT | python3 -c "import json,sys; r=json.loads(sys.stdin.read()); p=r['prog']; print(r['rep'], r['workload'], r['arm'], 'FAILED' if not p else '%.1fs gc=%.1fs pauses=%s full=%s' % (r['wall_s'], p['gc_ms']/1000, p.get('lxr_pauses','-'), p.get('lxr_full','-')))" >&2
    done
  done
done
