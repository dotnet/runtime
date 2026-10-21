#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Median of every phase field of the RefCount pauses of one cell, for several builds side by side.

usage: phase-all.py <trace-dir> <workload> <heap> <mode> [build ...]
"""
import glob
import json
import statistics
import sys

directory, workload, heap, mode = sys.argv[1:5]
builds = sys.argv[5:] or ["s3", "s4"]
data = {}
for build in builds:
    events = []
    for path in sorted(glob.glob(f"{directory}/{build}-{workload}-{heap}-{mode}-r?.txt")):
        for line in open(path, encoding="utf-8", errors="replace"):
            if line.startswith("trace_event="):
                e = json.loads(line[len("trace_event="):])
                if e.get("kind") == 1:
                    events.append(e)
    data[build] = events
fields = sorted({k for events in data.values() for e in events for k in e if k.endswith("_us")})
print("field | " + " | ".join(f"{b} n={len(data[b])}" for b in builds))
for f in fields:
    values = [statistics.median(e.get(f, 0) for e in data[b]) / 1000.0 if data[b] else 0 for b in builds]
    if max(values) < 0.05:
        continue
    print(f"{f} | " + " | ".join(f"{v:.2f}" for v in values))
extra = ["nursery_copied_bytes", "mature_copied_bytes", "blocks_released", "nursery_copy_failures", "incs", "decs",
         "live_bytes", "committed_bytes"]
for f in extra:
    values = [statistics.median(e.get(f, 0) for e in data[b]) if data[b] else 0 for b in builds]
    print(f"{f} | " + " | ".join(f"{v:.0f}" for v in values))
