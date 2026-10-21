#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Markdown table for the P19 matrix: wall time, GC time, peak RSS and checksums per workload, mode and build.

usage: summarize.py <wall.jsonl> <label>...   (labels in the order of the columns, for example acc s6 int)
Each cell shows the mean of the repetitions, then the single values in parentheses. A checksum column shows
"equal" when every successful run of the workload has the same checksum.
"""
import json
import statistics
import sys
from collections import defaultdict

path, labels = sys.argv[1], sys.argv[2:]
rows = defaultdict(lambda: defaultdict(list))
checksums = defaultdict(set)
for line in open(path):
    r = json.loads(line)
    key = (r["workload"], r["mode"])
    if r.get("elapsed_ms") is None:
        rows[key][r["build"]].append(None)
        continue
    rows[key][r["build"]].append(r)
    checksums[r["workload"]].add(r.get("checksum"))


def cell(runs, field, scale=1.0, fmt="%.0f"):
    values = [r[field] / scale for r in runs if r]
    if not values:
        return "failed"
    text = fmt % statistics.mean(values)
    return text + " (" + ", ".join(fmt % v for v in values) + ")"


for field, title, scale in (("elapsed_ms", "wall ms", 1.0), ("gc_ms", "GC ms (runtime counter)", 1.0), ("peak_rss_kb", "peak RSS MiB", 1024.0)):
    print("\n%s\n" % title)
    print("| workload | mode | " + " | ".join(labels) + " |")
    print("|---|---|" + "---|" * len(labels))
    for key in sorted(rows, key=lambda k: (("binarytrees", "requests", "listsuffix").index(k[0]), k[1])):
        print("| %s | %s | " % key + " | ".join(cell(rows[key][b], field, scale) for b in labels) + " |")
print("\nchecksums per workload: " + "; ".join("%s %s" % (w, "equal" if len(c) == 1 else "DIFFERENT " + str(sorted(c))) for w, c in sorted(checksums.items())))
