#!/bin/bash
# Traced run (S0 phase timers of the P17 tracer, one event per collection).
# usage: trace1.sh <workload> <arm> <out-file> [NAME=VALUE ...]
# The tracer polls the metrics in one extra thread, so the wall time of a traced run is not a result.
W=$1; ARM=$2; OUT=$3; shift 3
EXTRA="stamp_ns=243,native_planes=38,gc_committed=26,blocks_rel=7,admitted=36,used_est=3,reserve_pages=247,free_pages=248,surv_bytes=246,young_bytes=86,cause=43,mature_live=40"
/root/p24/scripts/run1.sh ${FRAME:-/root/p24/frames/p24-Release-linux} /root/gcbench/trace19/gctrace.dll $W $ARM GCTRACE=1 GCTRACE_EXTRA="$EXTRA" "$@" > "$OUT" 2>&1
tail -2 "$OUT" | cut -c1-600
