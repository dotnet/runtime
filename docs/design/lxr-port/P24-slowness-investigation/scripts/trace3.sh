#!/bin/bash
# Traced run with the P24 tracer (supports GCBENCH_LATENCY=batch). usage: trace3.sh <workload> <arm> <out> [NAME=VALUE ...]
W=$1; ARM=$2; OUT=$3; shift 3
EXTRA="blocks_rel=7,free_pages=248,reserve_pages=247,surv_bytes=246,young_bytes=86,cause=43"
/root/p24/scripts/run1.sh /root/p24/frames/p24-Release-linux /root/p24/tracer-bin/gctrace.dll $W $ARM GCTRACE=1 GCTRACE_EXTRA="$EXTRA" "$@" > "$OUT" 2>&1
tail -2 "$OUT" | head -1 | cut -c1-300
