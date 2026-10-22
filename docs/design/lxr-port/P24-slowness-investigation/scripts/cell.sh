#!/bin/bash
# Untraced runs with the P24 tracer binary (no polling thread unless GCTRACE=1): prints wall, cpu, gc ms, pause kinds and the
# CPU of the busiest threads. usage: cell.sh <workload> <arm> <label> [NAME=VALUE ...]
W=$1; ARM=$2; LABEL=$3; shift 3
OUT=/root/p24/results/cells; mkdir -p $OUT
F=${FRAME:-/root/p24/frames/p24-Release-linux}
/root/p24/scripts/run1.sh $F /root/p24/tracer-bin/gctrace.dll $W $ARM "$@" > $OUT/$LABEL.txt 2>&1
python3 - $OUT/$LABEL.txt "$LABEL" <<'EOF'
import json, sys
label = sys.argv[2]
summ = None; wall = None
for line in open(sys.argv[1], errors="replace"):
    if line.startswith('{"runtime"'):
        summ = json.loads(line)
    if line.startswith("wall_s="):
        wall = float(line.split("=")[1])
if not summ:
    print(label, "FAILED"); sys.exit()
th = sorted(summ.get("threads", []), key=lambda t: -(t["u"] + t["s"]))[:7]
ths = " ".join(f'{t["n"][:10]}:{t["u"]}+{t["s"]}' for t in th)
print(f'{label}: wall={wall:.2f}s elapsed={summ["elapsed_ms"]/1000:.2f}s cpu={summ["cpu_ms"]/1000:.2f}s gc={summ["gc_ms"]/1000:.2f}s '
      f'pauses={summ.get("lxr_pauses","-")} im={summ.get("lxr_initial_mark","-")} fm={summ.get("lxr_final_mark","-")} full={summ.get("lxr_full","-")} '
      f'rss={summ["peak_rss_kb"]//1024}MiB  threads(u+s ms): {ths}')
EOF
