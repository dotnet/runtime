#!/usr/bin/env python3
"""Phase table of one traced run (the S0 timers of the P17 tracer).

usage: phases.py <trace-file> [--events]
Prints, for each pause kind: number of pauses, and for every phase the median and the sum over the run (ms).
Counters in the events are cumulative; the script shows per-pause differences for the ones that matter.
"""
import json
import statistics
import sys
from collections import defaultdict

NAMES = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
path = sys.argv[1]
events = []
for line in open(path, encoding="utf-8", errors="replace"):
    if line.startswith("trace_event="):
        events.append(json.loads(line[len("trace_event="):]))
if not events:
    sys.exit("no trace events")
if "--events" in sys.argv:
    for e in events:
        print(json.dumps(e))
    sys.exit(0)

CUM = ["incs", "decs", "nursery_copied_objs", "nursery_copied_bytes", "mature_copied_objs", "mature_copied_bytes",
       "traced_objects", "traced_refs", "blocks_rel", "allocations", "allocated_bytes", "nursery_copy_failures"]
prev = defaultdict(int)
rows = []
for e in events:
    r = dict(e)
    for k in CUM:
        if k in e:
            r[k + "_d"] = e[k] - prev[k]
            prev[k] = e[k]
    rows.append(r)

PH = [k for k in events[0] if k.endswith("_us") and k not in ("total_us",)]
by_kind = defaultdict(list)
for r in rows:
    by_kind[NAMES.get(r["kind"], str(r["kind"]))].append(r)
print(f"file {path}: {len(rows)} events")
for kind, rs in by_kind.items():
    print(f"\n== {kind}: {len(rs)} pauses")
    print(f"{'phase':28s} {'median ms':>10s} {'sum ms':>10s}")
    for ph in PH:
        vals = [r[ph] / 1000.0 for r in rs]
        if sum(vals) < 1.0:
            continue
        print(f"{ph:28s} {statistics.median(vals):10.1f} {sum(vals):10.0f}")
    for k in ("incs_d", "decs_d", "nursery_copied_objs_d", "nursery_copied_bytes_d", "traced_objects_d", "blocks_rel_d",
              "inc_packets", "dec_packets", "surv_bytes", "young_bytes", "free_pages", "reserve_pages"):
        if k in rs[0]:
            vals = [r[k] for r in rs]
            print(f"{k:28s} {statistics.median(vals):10.0f} {sum(vals):10.0f}")
