#!/bin/bash
# P26: CPU of one run split into user and system time (sum over threads), wall, and the pauses. Two frames side by side.
# usage: cpucmp.sh "<frame1> <frame2> ..." "<workloads>" "<arms>" <reps> [NAME=VALUE ...]
FRAMES=($1); WL=$2; ARMS=$3; REPS=$4
shift 4
for w in $WL; do for a in $ARMS; do for r in $(seq 1 $REPS); do for f in "${FRAMES[@]}"; do
  /root/p24/scripts/run1.sh $f /root/p24/tracer-bin/gctrace.dll $w $a "$@" 2>&1 | python3 -c "
import json, sys
wall = None; d = None
for l in sys.stdin:
    if l.startswith('{\"runtime\"'): d = json.loads(l)
    if l.startswith('wall_s='): wall = float(l.split('=')[1])
if d is None:
    print('$(basename $f) $w $a FAILED'); sys.exit()
u = sum(x['u'] for x in d['threads']) / 1000; s = sum(x['s'] for x in d['threads']) / 1000
print('%-24s %-11s %-12s wall=%.2f cpu=%.2f user=%.2f sys=%.2f gc=%.2f pauses=%s' % ('$(basename $f)', '$w', '$a', wall, d['cpu_ms']/1000, u, s, d['gc_ms']/1000, d.get('lxr_pauses','-')))
"
done; done; done; done
