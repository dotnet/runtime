#!/usr/bin/env python3
"""Median table of a matrix.sh result. usage: matrix-table.py <jsonl> [<jsonl> ...]"""
import json
import statistics
import sys

rows = []
for path in sys.argv[1:]:
    rows += [json.loads(l) for l in open(path) if l.strip()]
wl = ["binarytrees", "requests", "listsuffix"]
arms = []
for r in rows:
    key = (r["arm"], r["extra"])
    if key not in arms:
        arms.append(key)
base = {}
print(f"{'workload':12s} {'arm':14s} {'extra':22s} {'n':>2s} {'wall s':>7s} {'cpu s':>7s} {'gc s':>6s} {'pauses':>6s} {'full':>4s} {'rss MiB':>7s} {'wall xWKS':>9s} {'cpu xWKS':>8s}")
for w in wl:
    for arm, extra in arms:
        ok = [r for r in rows if r["workload"] == w and r["arm"] == arm and r["extra"] == extra and r["prog"]]
        if not ok:
            continue
        wall = statistics.median(r["wall_s"] for r in ok)
        cpu = statistics.median(r["prog"]["cpu_ms"] for r in ok) / 1000
        gc = statistics.median(r["prog"]["gc_ms"] for r in ok) / 1000
        pa = statistics.median(r["prog"].get("lxr_pauses", r["prog"].get("gc_count", 0)) for r in ok)
        fu = statistics.median(r["prog"].get("lxr_full", 0) for r in ok)
        rss = statistics.median(r["prog"]["peak_rss_kb"] for r in ok) / 1024
        if arm == "wks" and not extra:
            base[w] = (wall, cpu)
        b = base.get(w)
        ratio = f"{wall / b[0]:9.1f} {cpu / b[1]:8.1f}" if b else ""
        print(f"{w:12s} {arm:14s} {extra[:22]:22s} {len(ok):2d} {wall:7.2f} {cpu:7.2f} {gc:6.2f} {pa:6.0f} {fu:4.0f} {rss:7.0f} {ratio}")
