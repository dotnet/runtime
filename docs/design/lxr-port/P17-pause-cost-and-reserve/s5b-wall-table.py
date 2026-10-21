#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Wall time, collections, hiccups and peak RSS per cell for any set of builds in a run-matrix.py output.
The ratio new/base is shown for each paired repetition: a cell whose repetitions are on both sides of 1.0 is inside the
noise. Cells where a build has no finished run are listed with the status.

usage: s5b-wall-table.py <matrix.jsonl> --builds acc,s4,off,s5b --base s4 --new s5b
"""
import argparse
import json
import math
import statistics
import sys
from collections import defaultdict

parser = argparse.ArgumentParser()
parser.add_argument("matrix")
parser.add_argument("--builds", default="s4,s5b")
parser.add_argument("--base", default="s4")
parser.add_argument("--new", default="s5b")
args = parser.parse_args()
builds = args.builds.split(",")

cells = defaultdict(lambda: defaultdict(list))
for line in open(args.matrix, encoding="utf-8"):
    r = json.loads(line)
    cells[(r["workload"], float(r["factor"]), r["mode"])][r["build"]].append(r)

order = {"binarytrees": 0, "requests": 1, "listsuffix": 2}
modes = {"nonmoving": 0, "nursery": 1, "mature": 2}


def ok_runs(runs):
    return [r for r in runs if r.get("elapsed_ms")]


def med(runs, field):
    values = [r[field] for r in runs if r.get(field) is not None]
    return statistics.median(values) if values else float("nan")


def fmt(runs, field, scale=1.0, digits=0):
    ok = ok_runs(runs)
    if not ok:
        return "fail"
    return f"{med(ok, field) / scale:.{digits}f}"


print("| workload | heap x | mode | " + " | ".join(f"wall ms {b}" for b in builds) +
      f" | {args.new}/{args.base} wall (repetitions) | " + " | ".join(f"coll. {b}" for b in builds) +
      " | " + " | ".join(f"gc ms {b}" for b in builds) + " | " + " | ".join(f"RSS MB {b}" for b in builds) +
      " | " + " | ".join(f"hiccup max ms {b}" for b in builds) + " |")
print("|" + "---|" * (3 + len(builds) + 1 + 4 * len(builds)))
ratios, gcr, rssr = [], [], []
faster, slower, mixed = 0, 0, 0
failures = []
for key in sorted(cells, key=lambda k: (order[k[0]], k[1], modes[k[2]])):
    b = cells[key]
    row = f"| {key[0]} | {key[1]:g} | {key[2]} | "
    row += " | ".join(f"{fmt(b[x], 'elapsed_ms')} ({len(ok_runs(b[x]))}/{len(b[x])})" for x in builds)
    base = {r["repetition"]: r["elapsed_ms"] for r in ok_runs(b[args.base])}
    new = {r["repetition"]: r["elapsed_ms"] for r in ok_runs(b[args.new])}
    pairs = [new[i] / base[i] for i in sorted(base) if i in new]
    if pairs and len(ok_runs(b[args.base])) and len(ok_runs(b[args.new])):
        mb, mn = med(ok_runs(b[args.base]), "elapsed_ms"), med(ok_runs(b[args.new]), "elapsed_ms")
        ratios.append(mn / mb)
        gcr.append(med(ok_runs(b[args.new]), "gc_ms") / max(med(ok_runs(b[args.base]), "gc_ms"), 1))
        rssr.append(med(ok_runs(b[args.new]), "peak_rss_kb") / med(ok_runs(b[args.base]), "peak_rss_kb"))
        if all(p < 1 for p in pairs):
            faster += 1
        elif all(p > 1 for p in pairs):
            slower += 1
        else:
            mixed += 1
        row += f" | {mn / mb:.2f} ({', '.join(f'{p:.2f}' for p in pairs)})"
    else:
        row += " | -"
    row += " | " + " | ".join(fmt(b[x], "lxr_pauses") + "/" + fmt(b[x], "lxr_full") for x in builds)
    row += " | " + " | ".join(fmt(b[x], "gc_ms") for x in builds)
    row += " | " + " | ".join(fmt(b[x], "peak_rss_kb", 1024) for x in builds)
    row += " | " + " | ".join(fmt(b[x], "hiccup_max_us", 1000) for x in builds)
    print(row + " |")
    for x in builds:
        for r in b[x]:
            if not r.get("elapsed_ms"):
                failures.append(f"{x} {key[0]} x{key[1]:g} {key[2]} rep{r['repetition']}: exit={r['exit']} "
                                f"{r.get('output_tail', '')[-90:]!r}")


def geo(values):
    return math.exp(sum(math.log(v) for v in values) / len(values)) if values else float("nan")


print()
print(f"cells compared={len(ratios)}; {args.new}/{args.base} geometric mean: wall {geo(ratios):.3f}"
      f" (min {min(ratios, default=float('nan')):.3f}, max {max(ratios, default=float('nan')):.3f}), "
      f"gc ms {geo(gcr):.3f}, peak RSS {geo(rssr):.3f} (min {min(rssr, default=float('nan')):.3f}, max {max(rssr, default=float('nan')):.3f}); "
      f"all repetitions faster in {faster} cells, all slower in {slower}, mixed (noise) in {mixed}")
print(f"\nfailed or timed-out runs: {len(failures)}")
for f in failures:
    print("-", f)
