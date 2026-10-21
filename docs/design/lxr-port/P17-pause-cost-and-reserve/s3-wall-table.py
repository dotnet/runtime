#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Wall time and peak RSS per cell for the accepted build, the S2 tip and the S3 tip, from a run-matrix.py output."""
import json, statistics, sys
from collections import defaultdict

rows = defaultdict(lambda: defaultdict(list))
for line in open(sys.argv[1], encoding="utf-8"):
    r = json.loads(line)
    if r.get("exit") not in (0, None) or not r.get("elapsed_ms"):
        continue
    key = (r["workload"], float(r["factor"]) if "factor" in r else r.get("heap_x"), r["mode"])
    rows[key][r["build"]].append(r)

print("| workload | heap x | mode | wall ms acc / S2 / S3 | S3 / S2 (repetitions) | peak RSS MB acc / S2 / S3 |")
print("|---|---|---|---|---|---|")
order = {"binarytrees": 0, "requests": 1, "listsuffix": 2}
modes = {"nonmoving": 0, "nursery": 1, "mature": 2}
for key in sorted(rows, key=lambda k: (order[k[0]], k[1], modes[k[2]])):
    b = rows[key]
    if not all(len(b[x]) == 3 for x in ("acc", "s2", "s3")):
        continue
    med = lambda x, f: statistics.median(r[f] for r in b[x])
    by_rep = lambda x: {r["repetition"]: r["elapsed_ms"] for r in b[x]}
    s2, s3 = by_rep("s2"), by_rep("s3")
    reps = ", ".join(f"{s3[i] / s2[i]:.2f}" for i in sorted(s2) if i in s3)
    print(f"| {key[0]} | {key[1]:g} | {key[2]} | {med('acc', 'elapsed_ms'):.0f} / {med('s2', 'elapsed_ms'):.0f} / {med('s3', 'elapsed_ms'):.0f} | "
          f"{med('s3', 'elapsed_ms') / med('s2', 'elapsed_ms'):.2f} ({reps}) | "
          f"{med('acc', 'peak_rss_kb') / 1024:.0f} / {med('s2', 'peak_rss_kb') / 1024:.0f} / {med('s3', 'peak_rss_kb') / 1024:.0f} |")
