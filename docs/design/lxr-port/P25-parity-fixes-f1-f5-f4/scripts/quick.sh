#!/bin/bash
# P25: run some cells once and print one summary line each.
# usage: quick.sh <frame> "<workloads>" "<arms>" [NAME=VALUE ...]
FRAME=$1; WL=($2); ARMS=($3); shift 3
for w in "${WL[@]}"; do
  for a in "${ARMS[@]}"; do
    /root/p24/scripts/run1.sh $FRAME /root/p24/tracer-bin/gctrace.dll $w $a "$@" > /tmp/p25-quick.txt 2>&1
    python3 - /tmp/p25-quick.txt $w $a <<'EOF'
import json, sys
path, w, a = sys.argv[1:4]
summ = None; wall = None
for line in open(path, errors="replace"):
    if line.startswith('{"runtime"'):
        summ = json.loads(line)
    if line.startswith("wall_s="):
        wall = float(line.split("=")[1])
if not summ:
    print(w, a, "FAILED", open(path, errors="replace").read()[-300:].replace("\n", " | "))
else:
    print(w, a, "wall=%.2f gc=%.2f cpu=%.2f pauses=%s full=%s checksum=%s" % (wall, summ['gc_ms']/1000, summ['cpu_ms']/1000, summ.get('lxr_pauses','-'), summ.get('lxr_full','-'), summ.get('checksum')))
EOF
  done
done
