#!/bin/bash
# Repeat one traced cell N times (one run after the other) and print one summary line per run:
# elapsed ms, gc ms, pauses, InitialMark/FinalMark/Full counts inside the measured part, trace phase at the first event,
# number of pauses with the trace phase active, median RefCount stw ms with and without an active trace.
# usage: repeat-trace.sh <workload> <arm> <n> <tag> [NAME=VALUE ...]
W=$1; ARM=$2; N=$3; TAG=$4; shift 4
OUT=/root/p24/results/rep-$TAG; mkdir -p $OUT
for i in $(seq 1 $N); do
  /root/p24/scripts/trace1.sh $W $ARM $OUT/run$i.txt "$@" > /dev/null
  python3 - $OUT/run$i.txt <<'EOF'
import json, sys, statistics
path = sys.argv[1]
ev = []; summ = None
for line in open(path, errors="replace"):
    if line.startswith("trace_event="):
        ev.append(json.loads(line[12:]))
    elif line.startswith("{\"runtime\""):
        summ = json.loads(line)
if not summ:
    print(path, "no summary"); sys.exit()
first = ev[0]["trace_phase"] if ev else -1
act = [e for e in ev if e["kind"] == 1 and e["trace_phase"] != 0]
non = [e for e in ev if e["kind"] == 1 and e["trace_phase"] == 0]
med = lambda xs: round(statistics.median([x["stw_pause_us"] for x in xs]) / 1000.0, 1) if xs else None
print(f'{path.split("/")[-1]}: elapsed={summ["elapsed_ms"]:.0f} gc={summ["gc_ms"]} pauses={summ["lxr_pauses"]} '
      f'im={summ["lxr_initial_mark"]} fm={summ["lxr_final_mark"]} full={summ["lxr_full"]} first_phase={first} '
      f'rc_with_trace={len(act)} med_ms={med(act)} rc_no_trace={len(non)} med_ms={med(non)}')
EOF
done
