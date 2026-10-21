#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Summarize a P17 matrix: medians per build, ratio to the accepted build, checksum check, all failures listed."""

import argparse
import json
import statistics
import sys
from collections import defaultdict


def median(values):
    values = [v for v in values if v is not None]
    return statistics.median(values) if values else None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runs", nargs="+")
    parser.add_argument("--base", default="base")
    args = parser.parse_args()
    rows = []
    for path in args.runs:
        rows += [json.loads(line) for line in open(path, encoding="utf-8")]
    cells = defaultdict(list)
    for row in rows:
        cells[(row["workload"], row["factor"], row["mode"], row["build"])].append(row)
    checksums = defaultdict(set)
    failures = []
    for row in rows:
        if row.get("elapsed_ms") is None:
            failures.append(row)
        else:
            checksums[row["workload"]].add(row["checksum"])
    builds = sorted({k[3] for k in cells}, key=lambda b: (b != args.base, b))
    print("## Checksums")
    for workload, values in sorted(checksums.items()):
        print(f"- {workload}: {'one checksum ' + str(next(iter(values))) if len(values) == 1 else 'MISMATCH ' + str(sorted(values))}")
    print(f"\n## Failed or excluded runs: {len(failures)} of {len(rows)}")
    for row in failures:
        print(f"- {row['build']} {row['mode']} {row['workload']} x{row['factor']} rep{row['repetition']}: "
              f"exit={row['exit']} {row.get('output_tail', '')[-120:]!r}")
    print("\n## Medians (ok runs / all runs)")
    print("| workload | heap x | mode | build | ok | wall ms | vs base | cpu ms | gc ms | pauses | full | peak RSS MB |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|")
    for workload in ("binarytrees", "requests", "listsuffix"):
        for factor in sorted({k[1] for k in cells if k[0] == workload}):
            for mode in ("nonmoving", "nursery", "mature"):
                base = cells.get((workload, factor, mode, args.base), [])
                base_wall = median([r.get("elapsed_ms") for r in base])
                for build in builds:
                    runs = cells.get((workload, factor, mode, build))
                    if not runs:
                        continue
                    ok = [r for r in runs if r.get("elapsed_ms") is not None]
                    wall = median([r["elapsed_ms"] for r in ok])
                    ratio = f"{wall / base_wall:.2f}" if wall and base_wall else "-"
                    rss = median([r["peak_rss_kb"] / 1024 for r in ok])
                    print(f"| {workload} | {factor:g} | {mode} | {build} | {len(ok)}/{len(runs)} | "
                          f"{wall or 0:.0f} | {ratio} | {median([r['cpu_ms'] for r in ok]) or 0:.0f} | "
                          f"{median([r['gc_ms'] for r in ok]) or 0:.0f} | {median([r.get('lxr_pauses') for r in ok]) or 0:.0f} | "
                          f"{median([r.get('lxr_full') for r in ok]) or 0:.0f} | {rss or 0:.0f} |")
    return 1 if any(len(v) != 1 for v in checksums.values()) else 0


if __name__ == "__main__":
    sys.exit(main())
