#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Side-by-side pause table (accepted build, layer-1 tip, S2) from the output of pause-table.py.

usage: pause-compare.py <pause-tables-s2-x4.md>
Prints one row per cell and pause kind: count and median of each build (milliseconds, pooled over the repetitions).
"""

import sys
from collections import defaultdict

CELL_NAMES = {"00": "non-moving", "10": "nursery copy"}


def main():
    table = defaultdict(dict)
    for line in open(sys.argv[1], encoding="utf-8"):
        parts = [p.strip() for p in line.strip().strip("|").split("|")]
        if len(parts) != 7 or "/" not in parts[0]:
            continue
        build, cell, mode = parts[0].split("/")
        table[(cell, mode, parts[1])][build] = (int(parts[2]), int(parts[3]), int(parts[5]))
    print("| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | layer 1: n, median, max ms | S2: n, median, max ms |")
    print("|---|---|---|---|---|---|")
    order = {"binarytrees-40": 0, "requests-84": 1, "listsuffix-40": 2}
    kinds = {"RefCount": 0, "InitialMark": 1, "FinalMark": 2, "Full": 3}
    for key in sorted(table, key=lambda k: (order[k[0]], k[1], kinds[k[2]])):
        cells = []
        for build in ("acc", "l1", "s2"):
            value = table[key].get(build)
            cells.append(f"{value[0]}, {value[1]}, {value[2]}" if value else "-")
        print(f"| {key[0].split('-')[0]} | {CELL_NAMES[key[1]]} | {key[2]} | {cells[0]} | {cells[1]} | {cells[2]} |")


if __name__ == "__main__":
    main()
