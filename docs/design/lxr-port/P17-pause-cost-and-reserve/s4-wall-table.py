#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Wall time, collection time and peak RSS per cell for the accepted build, the S3 tip and the S4 tip, from a
run-matrix.py output. The S4/S3 ratio is shown for each paired repetition: a cell whose repetitions include 1.0 is
inside the noise.

usage: s4-wall-table.py <matrix.jsonl>
"""
import json
import math
import statistics
import sys
from collections import defaultdict

rows = defaultdict(lambda: defaultdict(list))
for line in open(sys.argv[1], encoding="utf-8"):
    r = json.loads(line)
    if r.get("exit") not in (0, None) or not r.get("elapsed_ms"):
        continue
    rows[(r["workload"], float(r["factor"]), r["mode"])][r["build"]].append(r)

print("| workload | heap x | mode | wall ms acc / S3 / S4 | S4 / S3 (repetitions) | gc ms S3 / S4 | pauses S3 / S4 | peak RSS MB acc / S3 / S4 |")
print("|---|---|---|---|---|---|---|---|")
order = {"binarytrees": 0, "requests": 1, "listsuffix": 2}
modes = {"nonmoving": 0, "nursery": 1, "mature": 2}
wall, gc, rss = [], [], []
for key in sorted(rows, key=lambda k: (order[k[0]], k[1], modes[k[2]])):
    b = rows[key]
    if not all(len(b[x]) == 3 for x in ("acc", "s3", "s4")):
        continue

    def med(x, f):
        return statistics.median(r[f] for r in b[x])

    def by_rep(x, f="elapsed_ms"):
        return {r["repetition"]: r[f] for r in b[x]}

    s3, s4 = by_rep("s3"), by_rep("s4")
    reps = ", ".join(f"{s4[i] / s3[i]:.2f}" for i in sorted(s3) if i in s4)
    wall.append(med("s4", "elapsed_ms") / med("s3", "elapsed_ms"))
    gc.append(med("s4", "gc_ms") / med("s3", "gc_ms"))
    rss.append(med("s4", "peak_rss_kb") / med("s3", "peak_rss_kb"))
    print(f"| {key[0]} | {key[1]:g} | {key[2]} | {med('acc', 'elapsed_ms'):.0f} / {med('s3', 'elapsed_ms'):.0f} / "
          f"{med('s4', 'elapsed_ms'):.0f} | {wall[-1]:.2f} ({reps}) | {med('s3', 'gc_ms'):.0f} / {med('s4', 'gc_ms'):.0f} | "
          f"{med('s3', 'lxr_pauses'):.0f} / {med('s4', 'lxr_pauses'):.0f} | "
          f"{med('acc', 'peak_rss_kb') / 1024:.0f} / {med('s3', 'peak_rss_kb') / 1024:.0f} / {med('s4', 'peak_rss_kb') / 1024:.0f} |")


def geo(values):
    return math.exp(sum(math.log(v) for v in values) / len(values))


print()
print(f"cells={len(wall)}; S4/S3 geometric mean: wall {geo(wall):.3f} (min {min(wall):.3f}, max {max(wall):.3f}), "
      f"gc {geo(gc):.3f} (min {min(gc):.3f}, max {max(gc):.3f}), peak RSS {geo(rss):.3f} (min {min(rss):.3f}, max {max(rss):.3f})")
