#!/usr/bin/env python3
"""One line per collection of a traced run: the columns that matter for the pause cost.
usage: perpause.py <trace-file> [column ...]"""
import json
import sys

path = sys.argv[1]
cols = sys.argv[2:] or ["kind", "stw_pause_us", "suspend_prepare_us", "publish_trace_us", "inc_wall_us", "inc_busy_us",
                        "inc_packets", "incs_d", "decs_d", "nursery_copied_objs_d", "nursery_copied_bytes_d",
                        "blocks_rel_d", "surv_bytes", "young_bytes", "cause", "trace_phase", "post_drain_us"]
prev = {}
print(" ".join(f"{c[:14]:>14s}" for c in cols))
for line in open(path, encoding="utf-8", errors="replace"):
    if not line.startswith("trace_event="):
        continue
    e = json.loads(line[len("trace_event="):])
    for k in ("incs", "decs", "nursery_copied_objs", "nursery_copied_bytes", "blocks_rel", "mature_copied_objs", "traced_objects"):
        if k in e:
            e[k + "_d"] = e[k] - prev.get(k, 0)
            prev[k] = e[k]
    out = []
    for c in cols:
        v = e.get(c, "")
        if isinstance(v, float):
            v = f"{v:.0f}" if c.endswith("_us") else f"{v}"
        out.append(f"{str(v):>14s}")
    print(" ".join(out))
