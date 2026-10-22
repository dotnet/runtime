#!/usr/bin/env python3
"""Table of the policy states recorded by policy-states.py. usage: policy-table.py <json>"""
import json
import sys

d = json.load(open(sys.argv[1]))
print(f"{len(d)} records")
cols = ["n", "pause_kind", "mature_pages", "admitted_pages", "used_pages", "non_free_blocks", "immix_capacity_blocks",
        "live_bytes", "mature_live_pages_before", "mature_live_pages_after", "cycle_hint_after", "emergency_hint_after", "cause"]
print(" ".join(f"{c[:12]:>12s}" for c in cols), " block_states")
for r in d:
    if "error" in r:
        print(r["n"], "ERROR", r["error"])
        continue
    cells = []
    for c in cols:
        v = r.get(c, "")
        if isinstance(v, float):
            v = f"{v:.0f}"
        cells.append(f"{str(v):>12s}")
    print(" ".join(cells), " ", r.get("block_states", ""))
