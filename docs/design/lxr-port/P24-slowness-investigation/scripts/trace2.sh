#!/bin/bash
# Traced run with the trace counters added. usage: trace2.sh <workload> <arm> <out-file> [NAME=VALUE ...]
W=$1; ARM=$2; OUT=$3; shift 3
EXTRA="satb=15,del_marks=16,forced=17,split=18,cycle_obj=19,slices=29,ckpt=31,blocks_rel=7,reserve_pages=247,free_pages=248,surv_bytes=246,young_bytes=86,mature_live=40,root_inc=8,deaths=9,flushes=22"
/root/p24/scripts/run1.sh ${FRAME:-/root/p24/frames/p24-Release-linux} /root/gcbench/trace19/gctrace.dll $W $ARM GCTRACE=1 GCTRACE_EXTRA="$EXTRA" "$@" > "$OUT" 2>&1
tail -2 "$OUT" | cut -c1-400
