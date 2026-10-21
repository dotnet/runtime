#!/bin/bash
# Summarize a DIAG file: per block of N pauses: pauses/s, median young MiB, clean MiB, start/end free pages, mature pages.
# usage: dgsum.sh <file> [block=100]
python3 - "$1" "${2:-100}" <<'EOF'
import re, sys, statistics as st
rows = []
for l in open(sys.argv[1]):
    if not l.startswith('DIAG'):
        continue
    d = dict(re.findall(r'(\w+)=([-\d.]+)', l))
    rows.append({k: float(v) for k, v in d.items()})
n = int(sys.argv[2])
print(f"total pauses {len(rows)}; kinds {dict((k, sum(1 for r in rows if r.get('kind') == k)) for k in (0, 1, 2, 3))}")
print("   from   to  pauses/s  young_MiB clean_MiB  startfree endfree  resv  mature  surv_KiB  pause_ms  full")
for i in range(0, len(rows), n):
    b = rows[i:i + n]
    dt = (b[-1]['t'] - b[0]['t']) / 1000.0 if len(b) > 1 else 0
    g = lambda k: st.median(r.get(k, 0) for r in b)
    print(f"{i + 1:7d} {i + len(b):6d} {((len(b) - 1) / dt if dt > 0 else 0):8.1f} {g('young') / 1048576:9.2f} {g('clean') / 1048576:9.2f} {g('startfree'):9.0f} {g('endfree'):7.0f} {g('resv'):5.0f} {g('mature'):7.0f} {g('surv') / 1024:9.1f} {g('pausems'):9.1f} {sum(1 for r in b if r.get('kind') == 0):5d}")
EOF
