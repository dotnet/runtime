#!/usr/bin/env python3
"""Where does the wall time go? usage: split-table.py <jsonl> [<jsonl> ...]
wall = process wall; elapsed = measured part of the program (after warm-up); gc = pause time reported by the runtime;
mutator = elapsed - gc; other = wall - elapsed (runtime start, warm-up run, shutdown). Medians over the repetitions."""
import json
import statistics
import sys

rows = []
for path in sys.argv[1:]:
    rows += [json.loads(l) for l in open(path) if l.strip()]
arms = []
for r in rows:
    k = (r["arm"], r["extra"])
    if k not in arms:
        arms.append(k)
print(f"{'workload':12s} {'arm':14s} {'extra':20s} {'wall':>6s} {'other':>6s} {'mutator':>8s} {'gc':>6s} {'cpu':>6s}   (seconds)")
for w in ("binarytrees", "requests", "listsuffix"):
    for arm, extra in arms:
        ok = [r for r in rows if r["workload"] == w and r["arm"] == arm and r["extra"] == extra and r["prog"]]
        if not ok:
            continue
        med = lambda f: statistics.median(f(r) for r in ok)
        wall = med(lambda r: r["wall_s"])
        el = med(lambda r: r["prog"]["elapsed_ms"] / 1000)
        gc = med(lambda r: r["prog"]["gc_ms"] / 1000)
        cpu = med(lambda r: r["prog"]["cpu_ms"] / 1000)
        print(f"{w:12s} {arm:14s} {extra[:20]:20s} {wall:6.2f} {wall - el:6.2f} {el - gc:8.2f} {gc:6.2f} {cpu:6.2f}")
