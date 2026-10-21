#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Print one line per collection from a P15 port trace (events from the polling thread)."""

import json
import sys

CAUSE = {0: "None", 1: "HeapFull", 2: "FinalMark", 3: "Survival", 4: "ImmixSpaceFull", 5: "Increments", 6: "FixedNursery", 7: "YoungAlloc"}
limit = int(sys.argv[2]) if len(sys.argv) > 2 else 40
events = [json.loads(line[len("trace_event="):]) for line in open(sys.argv[1], encoding="utf-8", errors="replace")
          if line.startswith("trace_event=")]
print(f"{len(events)} events")
prev = None
print(f"{'t_ms':>8} {'n':>2} {'pause':>8} {'kind':>6} {'cause':>14} {'cyc':>3} {'emg':>3} {'live MB':>8} {'comm MB':>8} {'adm MB':>7} {'objs traced':>11} {'incs':>9} {'decs':>9} {'allocMB':>8}")
for e in events[:limit]:
    if prev is None:
        d = {k: e[k] for k in ("initial", "final", "full", "traced_objects", "incs", "decs", "allocated_bytes")}
    else:
        d = {k: e[k] - prev[k] for k in ("initial", "final", "full", "traced_objects", "incs", "decs", "allocated_bytes")}
    kind = "full" if d["full"] else "init" if d["initial"] else "final" if d["final"] else "rc"
    print(f"{e['t_ms']:8.0f} {e['new_collections']:>2} {e['pause_ms']:8.1f} {kind:>6} {CAUSE.get(e['cause'], e['cause']):>14} "
          f"{e['cycle_hint']:>3} {e['emergency_hint']:>3} {e['live_bytes'] / 1048576:8.1f} {e['committed_bytes'] / 1048576:8.1f} "
          f"{e['admitted_bytes'] / 1048576:7.1f} {d['traced_objects']:11} {d['incs']:9} {d['decs']:9} {d['allocated_bytes'] / 1048576:8.1f}")
    prev = e
kinds = {}
for index, e in enumerate(events):
    p = events[index - 1] if index else None
    full = e["full"] - (p["full"] if p else 0)
    kinds.setdefault("full" if full else "other", []).append(e["pause_ms"])
for kind, pauses in kinds.items():
    print(f"{kind}: {len(pauses)} events, mean pause {sum(pauses) / len(pauses):.1f} ms, max {max(pauses):.1f} ms")
