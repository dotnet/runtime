#!/bin/bash
# P25: per-thread CPU (user, system in ms) of one run. usage: threadcpu.sh <frame> <workload> <arm> [NAME=VALUE ...]
F=$1; W=$2; A=$3; shift 3
/root/p24/scripts/run1.sh $F /root/p24/tracer-bin/gctrace.dll $W $A "$@" | grep '^{' | python3 -c '
import json, sys
d = json.loads(sys.stdin.read())
t = [(x["u"], x["s"]) for x in d["threads"]]
print("cpu_ms", d["cpu_ms"], "elapsed_ms", d["elapsed_ms"], "gc_ms", d["gc_ms"])
print("user+sys per thread:", t)
print("sum user", sum(a for a, b in t), "sum sys", sum(b for a, b in t))
'
