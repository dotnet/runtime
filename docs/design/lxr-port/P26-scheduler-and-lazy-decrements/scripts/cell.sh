#!/bin/bash
# P26: one run per cell and setting, prints wall s, CPU s, GC s, pauses, Full count. No tracing.
# usage: cell.sh <frame> "<workloads>" "<arms>" [NAME=VALUE ...]
FRAME=$1; WL=$2; ARMS=$3
shift 3
for w in $WL; do
  for a in $ARMS; do
    out=$(/root/p24/scripts/run1.sh $FRAME /root/p24/tracer-bin/gctrace.dll $w $a "$@" 2>&1)
    echo "$out" | python3 -c "
import json, sys
w = a = None
summ = None
for line in sys.stdin:
    if line.startswith('{\"runtime\"'):
        summ = json.loads(line)
    if line.startswith('wall_s='):
        w = float(line.split('=')[1])
if summ is None:
    print('$w $a $* FAILED')
else:
    print('%s %s %s wall=%.2f cpu=%.2f gc=%.2f pauses=%s full=%s checksum=%s' % ('$w', '$a', '$*', w, summ['cpu_ms']/1000, summ['gc_ms']/1000, summ.get('lxr_pauses','-'), summ.get('lxr_full','-'), summ['checksum']))
"
  done
done
