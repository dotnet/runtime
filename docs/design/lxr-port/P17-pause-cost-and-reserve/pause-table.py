#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Pause time per collection kind from GcTrace output (old P15 format and new P17 format).

usage: pause-table.py <label=file> ...   (prints one row per label and kind: count, median, p95, max, total in ms)
The old format reports the kind only through counter deltas, and the pause as the runtime pause counter delta.
"""

import json
import statistics
import sys

NAMES = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}


def load(path):
    events = []
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("trace_event="):
            events.append(json.loads(line[len("trace_event="):]))
    return events


def pauses(events):
    out = []
    if events and "stw_pause_us" in events[0]:
        for e in events:
            out.append((NAMES.get(e["kind"], str(e["kind"])), e["stw_pause_us"] / 1000.0))
        return out
    previous = {"full": 0, "initial": 0, "final": 0}
    for e in events:
        kind = "RefCount"
        if e["full"] > previous["full"]:
            kind = "Full"
        elif e["initial"] > previous["initial"]:
            kind = "InitialMark"
        elif e["final"] > previous["final"]:
            kind = "FinalMark"
        previous = {key: e[key] for key in previous}
        out.append((kind, e["pause_ms"]))
    return out


def main():
    print("| run | kind | n | median ms | p95 ms | max ms | total ms |")
    print("|---|---|---|---|---|---|---|")
    for item in sys.argv[1:]:
        label, path = item.split("=", 1)
        rows = pauses(load(path))
        for kind in ("RefCount", "InitialMark", "FinalMark", "Full"):
            values = sorted(v for k, v in rows if k == kind)
            if not values:
                continue
            p95 = values[min(len(values) - 1, int(0.95 * len(values)))]
            print(f"| {label} | {kind} | {len(values)} | {statistics.median(values):.0f} | {p95:.0f} | {values[-1]:.0f} | {sum(values):.0f} |")


if __name__ == "__main__":
    main()
