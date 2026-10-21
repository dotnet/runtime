#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Per-collection phase table from GcTrace output (trace_event lines). Medians per pause kind, in ms."""

import json
import statistics
import sys
from collections import defaultdict

KINDS = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
COLUMNS = ["total", "stw_pause", "pre_drain", "suspend_prepare", "root_capture", "scan_lease_finalizers",
           "inc_wall", "inc_busy", "dec_busy", "release_epoch", "reclaim_nursery", "sweep_blocks",
           "scan2", "scan_heap_statistics", "scan_queue_finalizers", "prepare_trace", "weak_closure", "post_drain",
           "mature_sweep_busy"]


def value(event, name):
    for key in (name + "_us", name + "_wall_us", name + "_busy_us"):
        if key in event:
            return event[key] / 1000.0
    return float(event.get(name, 0.0))


def load(path):
    events = []
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("trace_event="):
            events.append(json.loads(line[len("trace_event="):]))
    return events


def detail(path, kind):
    events = [e for e in load(path) if e["kind"] == kind]
    print(f"### {path} kind={KINDS.get(kind, kind)} n={len(events)} (median ms or count)")
    for key in events[0]:
        if key.endswith(("_us", "_packets")):
            median = statistics.median(e[key] for e in events)
            if median > 0:
                print(f"{key:34s} {median / 1000.0 if key.endswith('_us') else median:10.2f}")


def main():
    if len(sys.argv) == 4 and sys.argv[1] == "--detail":
        detail(sys.argv[2], int(sys.argv[3]))
        return
    for path in sys.argv[1:]:
        events = load(path)
        print(f"### {path}: {len(events)} collections")
        by_kind = defaultdict(list)
        for event in events:
            by_kind[event["kind"]].append(event)
        print("| kind | n | " + " | ".join(COLUMNS) + " |")
        print("|---|---|" + "---|" * len(COLUMNS))
        for kind, items in sorted(by_kind.items()):
            cells = [f"{statistics.median(value(e, c) for e in items):.1f}" for c in COLUMNS]
            print(f"| {KINDS.get(kind, kind)} | {len(items)} | " + " | ".join(cells) + " |")
        print()


if __name__ == "__main__":
    main()
