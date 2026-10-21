#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Paired comparison of one build against the base build, per cell and over all cells."""

import argparse
import json
import math
import statistics
import sys
from collections import defaultdict


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runs", nargs="+")
    parser.add_argument("--base", default="base")
    parser.add_argument("--new", required=True)
    parser.add_argument("--metric", default="elapsed_ms")
    args = parser.parse_args()
    rows = []
    for path in args.runs:
        rows += [json.loads(line) for line in open(path)]
    cells = defaultdict(lambda: defaultdict(list))
    for row in rows:
        if row.get(args.metric) is not None:
            cells[(row["workload"], row["factor"], row["mode"])][row["build"]].append(row[args.metric])
    ratios = []
    print(f"| workload | heap x | mode | base median | {args.new} median | ratio | per-repetition ratios |")
    print("|---|---|---|---|---|---|---|")
    for key in sorted(cells):
        base, new = cells[key].get(args.base), cells[key].get(args.new)
        if not base or not new:
            continue
        ratio = statistics.median(new) / statistics.median(base)
        ratios.append(ratio)
        paired = ", ".join(f"{n / b:.2f}" for b, n in zip(base, new))
        print(f"| {key[0]} | {key[1]:g} | {key[2]} | {statistics.median(base):.0f} | {statistics.median(new):.0f} | {ratio:.3f} | {paired} |")
    if ratios:
        geo = math.exp(sum(math.log(r) for r in ratios) / len(ratios))
        print(f"\ncells={len(ratios)} geometric mean ratio={geo:.3f} min={min(ratios):.3f} max={max(ratios):.3f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
