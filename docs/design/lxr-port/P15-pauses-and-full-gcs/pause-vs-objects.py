#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Does the pause of a non-full collection of our port scale with the number of objects allocated since the last one?"""

import json
import sys

for path in sys.argv[1:]:
    events = [json.loads(line[len("trace_event="):]) for line in open(path, encoding="utf-8", errors="replace")
              if line.startswith("trace_event=")]
    rows = []
    for index in range(1, len(events)):
        e, p = events[index], events[index - 1]
        if e["new_collections"] != 1 or e["full"] != p["full"]:
            continue
        objects = e["allocations"] - p["allocations"]
        survivors = e["incs"] - p["incs"]
        rows.append((objects, survivors, e["pause_ms"]))
    if len(rows) < 3:
        print(path, "too few non-full collections", len(rows))
        continue
    n = len(rows)
    mean_x = sum(r[0] for r in rows) / n
    mean_y = sum(r[2] for r in rows) / n
    sxx = sum((r[0] - mean_x) ** 2 for r in rows)
    sxy = sum((r[0] - mean_x) * (r[2] - mean_y) for r in rows)
    slope = sxy / sxx if sxx else float("nan")
    ss_tot = sum((r[2] - mean_y) ** 2 for r in rows)
    ss_res = sum((r[2] - (mean_y + slope * (r[0] - mean_x))) ** 2 for r in rows)
    r2 = 1 - ss_res / ss_tot if ss_tot else float("nan")
    print(f"{path.split(chr(92))[-1]}: {n} non-full collections; objects allocated per collection {min(r[0] for r in rows):.0f}..{max(r[0] for r in rows):.0f} "
          f"(mean {mean_x:.0f}); pause {min(r[2] for r in rows):.0f}..{max(r[2] for r in rows):.0f} ms (mean {mean_y:.0f}); "
          f"slope {slope * 1e6:.0f} ns per allocated object, R^2 {r2:.2f}; mean pause/mean objects = {mean_y * 1e6 / mean_x:.0f} ns per object")
