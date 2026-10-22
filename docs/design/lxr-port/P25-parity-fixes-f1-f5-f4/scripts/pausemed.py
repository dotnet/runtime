#!/usr/bin/env python3
"""Pause medians of one traced run: per pause kind the count, the median and the maximum of the stop-the-world time (ms).
usage: pausemed.py <trace-file>"""
import json
import statistics
import sys

NAMES = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
events = []
for line in open(sys.argv[1], encoding="utf-8", errors="replace"):
    if line.startswith("trace_event="):
        events.append(json.loads(line[len("trace_event="):]))
if not events:
    print("no trace events")
    sys.exit(0)
for kind in sorted({e["kind"] for e in events}):
    v = [e["total_us"] / 1000 for e in events if e["kind"] == kind]
    print(f"  {NAMES.get(kind, kind):12s} n={len(v):3d} median={statistics.median(v):8.2f} ms  max={max(v):8.2f} ms")
