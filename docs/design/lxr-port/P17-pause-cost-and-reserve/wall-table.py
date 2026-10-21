#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Markdown table of the S2 wall-time comparison: accepted build, layer-1 tip and S2, per cell.

usage: wall-table.py <matrix.jsonl> <base-label> <old-label> <new-label>
A gain is marked "all 3 below 0.95" when every paired repetition ratio (new / old) is below 0.95. Other cells are marked
"mixed": at least one repetition is 0.95 or higher, so the host noise can explain the median.
"""

import json
import math
import statistics
import sys
from collections import defaultdict


def main():
    path, base, old, new = sys.argv[1:5]
    cells = defaultdict(lambda: defaultdict(dict))
    for line in open(path):
        row = json.loads(line)
        if row.get("elapsed_ms") is not None:
            cells[(row["workload"], row["factor"], row["mode"])][row["build"]][row["repetition"]] = row["elapsed_ms"]
    print(f"| workload | heap x | mode | {base} ms | {old} ms | {new} ms | {new}/{old} | {new}/{base} | paired repetitions {new}/{old} | reading |")
    print("|---|---|---|---|---|---|---|---|---|---|")
    ratios_old, ratios_base = [], []
    consistent = 0
    for key in sorted(cells, key=lambda k: (("binarytrees", "requests", "listsuffix").index(k[0]), k[1], ("nonmoving", "nursery", "mature").index(k[2]))):
        builds = cells[key]
        if not all(len(builds.get(b, {})) for b in (base, old, new)):
            continue
        med = {b: statistics.median(builds[b].values()) for b in (base, old, new)}
        reps = sorted(set(builds[old]) & set(builds[new]))
        paired = [builds[new][r] / builds[old][r] for r in reps]
        ratio_old = med[new] / med[old]
        ratio_base = med[new] / med[base]
        ratios_old.append(ratio_old)
        ratios_base.append(ratio_base)
        good = all(round(p, 2) < 0.95 for p in paired) and len(paired) == 3
        consistent += 1 if good else 0
        print(f"| {key[0]} | {key[1]:g} | {key[2]} | {med[base]:.0f} | {med[old]:.0f} | {med[new]:.0f} | {ratio_old:.2f} | {ratio_base:.2f} | "
              f"{', '.join(f'{p:.2f}' for p in paired)} | {'all 3 below 0.95' if good else 'mixed'} |")
    geo = lambda values: math.exp(sum(math.log(v) for v in values) / len(values))
    print(f"\ncells={len(ratios_old)}; geometric mean {new}/{old}={geo(ratios_old):.3f} (min {min(ratios_old):.3f}, max {max(ratios_old):.3f}); "
          f"{new}/{base}={geo(ratios_base):.3f} (min {min(ratios_base):.3f}, max {max(ratios_base):.3f}); cells with all 3 repetitions below 0.95: {consistent}")


if __name__ == "__main__":
    main()
