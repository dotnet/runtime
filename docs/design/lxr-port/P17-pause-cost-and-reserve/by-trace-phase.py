#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Pause time of RefCount pauses split by the trace phase at the end of the pause, with the part of the pause that no
timer covers (stop-the-world time minus increments, prepare-trace, suspend-prepare and root capture wall times).

usage: by-trace-phase.py <trace-file> ...
"""
import json
import statistics
import sys

for path in sys.argv[1:]:
    rows = {}
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("trace_event="):
            e = json.loads(line[len("trace_event="):])
            if e.get("kind") != 1:
                continue
            rows.setdefault(e.get("trace_phase"), []).append(e)
    print(path)
    for phase, events in sorted(rows.items(), key=lambda kv: str(kv[0])):
        def med(field):
            return statistics.median(e.get(field, 0) for e in events) / 1000.0
        stw = med("stw_pause_us")
        inc = med("inc_wall_us")
        prep = med("prepare_trace_wall_us")
        susp = med("suspend_prepare_us")
        cap = med("root_capture_us")
        print(f"  trace_phase={phase} n={len(events)} stw={stw:.1f} inc_wall={inc:.1f} prepare_trace={prep:.1f} "
              f"suspend_prepare={susp:.1f} capture={cap:.1f} unattributed={stw - inc - prep - susp - cap:.1f} "
              f"publish_trace={med('publish_trace_us'):.1f} resume_gap={med('resume_gap_us'):.1f}")
