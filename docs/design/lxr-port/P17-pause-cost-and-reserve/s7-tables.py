#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""P17 S7 tables from traced runs: wall, GC time, peak RSS, native plane bytes, committed bytes, pause medians, pins.

usage: s7-tables.py <label> <label> <dir> [<dir>...]
The two labels are the builds (for example int s7). A file name is <label>-<workload>-<heap>-<nm>-r<rep>.txt, where
nm is the nursery flag and the mature flag (00 non-moving, 10 nursery copy). A run without a final result line
(the process ended abnormally) is counted as failed and left out of the numbers.
"""
import json
import re
import statistics
import sys
from collections import defaultdict
from pathlib import Path

NAMES = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
labels = sys.argv[1:3]
dirs = [Path(d) for d in sys.argv[3:]]
MODE = {"00": "non-moving", "10": "nursery copy"}
ORDER = {"binarytrees": 0, "requests": 1, "listsuffix": 2, "pinheavy": 3}

runs = defaultdict(list)
failed = defaultdict(int)
for directory in dirs:
    for path in sorted(directory.glob("*.txt")):
        m = re.match(r"(\w+?)-(\w+)-(\d+)-(\d\d)-r(\d)\.txt", path.name)
        if not m:
            continue
        build, workload, heap, mode, _rep = m.groups()
        events, final = [], None
        for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
            if line.startswith("trace_event="):
                events.append(json.loads(line[len("trace_event="):]))
            elif line.startswith("{") and '"checksum"' in line:
                final = json.loads(line)
        key = (workload, mode)
        if final is None:
            failed[(key, build)] += 1
            continue
        runs[(key, build)].append((events, final))


def med(values):
    return statistics.median(values) if values else float("nan")


def cell(key, build, fn, fmt="%.0f"):
    rs = runs.get((key, build), [])
    values = [fn(r) for r in rs]
    values = [v for v in values if v is not None]
    if not values:
        return "failed"
    return (fmt % statistics.mean(values)) + " (" + ", ".join(fmt % v for v in values) + ")"


keys = sorted({k for (k, _b) in list(runs) + list(failed)}, key=lambda k: (ORDER[k[0]], k[1]))
print("| workload | mode | " + " | ".join("%s wall ms | %s GC ms | %s peak RSS MiB" % (l, l, l) for l in labels) + " | checksums | failed runs |")
print("|---|---|" + "---|" * (3 * len(labels) + 2))
for key in keys:
    parts = []
    for b in labels:
        parts.append(cell(key, b, lambda r: r[1]["elapsed_ms"]))
        parts.append(cell(key, b, lambda r: r[1]["gc_ms"]))
        parts.append(cell(key, b, lambda r: r[1]["peak_rss_kb"] / 1024.0))
    sums = {r[1]["checksum"] for b in labels for r in runs.get((key, b), [])}
    fails = ", ".join("%s %d" % (b, failed[(key, b)]) for b in labels if failed[(key, b)]) or "none"
    print("| %s | %s | %s | %s | %s |" % (key[0], MODE[key[1]], " | ".join(parts), "equal" if len(sums) == 1 else "DIFFER", fails))

print("\nNative planes and committed bytes (maximum over the run, MiB; median of the runs)\n")
print("| workload | mode | " + " | ".join("%s native planes | %s managed committed" % (l, l) for l in labels) + " |")
print("|---|---|" + "---|" * (2 * len(labels)))
for key in keys:
    parts = []
    for b in labels:
        rs = runs.get((key, b), [])
        planes = [max((e.get("native_planes", 0) for e in ev), default=0) / 1048576.0 for ev, _f in rs]
        comm = [max((e.get("gc_committed", 0) for e in ev), default=0) / 1048576.0 for ev, _f in rs]
        parts += ["%.1f" % med(planes) if planes else "failed", "%.1f" % med(comm) if comm else "failed"]
    print("| %s | %s | %s |" % (key[0], MODE[key[1]], " | ".join(parts)))

print("\nPause table (stop-the-world time of the S0 timers, pooled over the runs): n, median ms, maximum ms\n")
print("| workload | mode | pause kind | " + " | ".join(labels) + " |")
print("|---|---|---|" + "---|" * len(labels))
pauses = defaultdict(lambda: defaultdict(list))
for (key, build), rs in runs.items():
    for ev, _f in rs:
        for e in ev:
            if "stw_pause_us" in e:
                pauses[(key, NAMES.get(e["kind"], str(e["kind"])))][build].append(e["stw_pause_us"] / 1000.0)
kind_order = {"RefCount": 0, "InitialMark": 1, "FinalMark": 2, "Full": 3}
for (key, kind) in sorted(pauses, key=lambda x: (ORDER[x[0][0]], x[0][1], kind_order.get(x[1], 9))):
    cells = []
    for b in labels:
        v = sorted(pauses[(key, kind)].get(b, []))
        cells.append("%d, %.1f, %.1f" % (len(v), statistics.median(v), v[-1]) if v else "-")
    print("| %s | %s | %s | %s |" % (key[0], MODE[key[1]], kind, " | ".join(cells)))

print("\nPins (pin-heavy program): pinned roots per pause and bytes of pinned objects promoted in place (last event of the run)\n")
print("The counter that the S7 build calls pinned roots (id 458) is the touched-list count in the build before S7, so the")
print("column of the earlier build shows n/a.\n")
print("| mode | " + " | ".join("%s mean pinned roots per pause | %s in-place pinned bytes" % (l, l) for l in labels) + " |")
print("|---|" + "---|" * (2 * len(labels)))
for key in keys:
    if key[0] != "pinheavy":
        continue
    parts = []
    for b in labels:
        rs = runs.get((key, b), [])
        roots = [statistics.mean([e.get("pinned_roots", 0) for e in ev]) for ev, _f in rs if ev]
        inplace = [ev[-1].get("pins_inplace", 0) for ev, _f in rs if ev]
        parts += ["n/a" if b == "int" else ("%.1f" % med(roots) if roots else "-"), "%.0f" % med(inplace) if inplace else "-"]
    print("| %s | %s |" % (MODE[key[1]], " | ".join(parts)))
