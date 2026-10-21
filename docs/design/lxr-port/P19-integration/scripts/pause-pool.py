#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Pause table from the traced runs of bench-trace.sh: pooled over the repetitions, per build, cell and pause kind.

usage: pause-pool.py <trace-dir> <label>...   (labels in column order, for example acc s6 int)
Each cell shows: number of pauses, median ms, maximum ms. The stop-the-world pause of the trace event is used
(stw_pause_us with the P17 tracer, pause_ms with the older tracer that the accepted build needs).
"""
import json
import re
import statistics
import sys
from collections import defaultdict
from pathlib import Path

NAMES = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
directory, labels = Path(sys.argv[1]), sys.argv[2:]


def events(path):
    out = []
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        if line.startswith("trace_event="):
            out.append(json.loads(line[len("trace_event="):]))
    return out


def pauses(evs):
    rows = []
    if evs and "stw_pause_us" in evs[0]:
        return [(NAMES.get(e["kind"], str(e["kind"])), e["stw_pause_us"] / 1000.0) for e in evs]
    previous = {"full": 0, "initial": 0, "final": 0}
    for e in evs:
        kind = "RefCount"
        if e["full"] > previous["full"]:
            kind = "Full"
        elif e["initial"] > previous["initial"]:
            kind = "InitialMark"
        elif e["final"] > previous["final"]:
            kind = "FinalMark"
        previous = {key: e[key] for key in previous}
        rows.append((kind, e["pause_ms"]))
    return rows


table = defaultdict(lambda: defaultdict(list))
for path in sorted(directory.glob("*.txt")):
    m = re.match(r"(\w+)-(\w+)-(\d+)-(\d\d)-r(\d)\.txt", path.name)
    if not m:
        continue
    build, workload, heap, mode, _rep = m.groups()
    for kind, value in pauses(events(path)):
        table[(workload, mode, kind)][build].append(value)

print("| workload (heap x4) | mode | pause kind | " + " | ".join("%s: n, median, max ms" % l for l in labels) + " |")
print("|---|---|---|" + "---|" * len(labels))
order = {"binarytrees": 0, "requests": 1, "listsuffix": 2}
kinds = {"RefCount": 0, "InitialMark": 1, "FinalMark": 2, "Full": 3}
names = {"00": "non-moving", "10": "nursery copy"}
for key in sorted(table, key=lambda k: (order[k[0]], k[1], kinds[k[2]])):
    cells = []
    for label in labels:
        v = sorted(table[key].get(label, []))
        cells.append("%d, %.0f, %.0f" % (len(v), statistics.median(v), v[-1]) if v else "-")
    print("| %s | %s | %s | %s |" % (key[0], names[key[1]], key[2], " | ".join(cells)))
