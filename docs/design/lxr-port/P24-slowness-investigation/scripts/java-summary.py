#!/usr/bin/env python3
"""Summary of Java LXR verbose logs (MMTK_VERBOSE=3). usage: java-summary.py <log> [<log> ...]
Per pause kind: count, median and maximum pause-time (ms), median young allocation (MiB) and the causes."""
import re
import statistics
import sys
from collections import defaultdict

start = re.compile(r"GC\((\d+)\) (\w+) start\. cause=(\w+) incs=(\d+) young-alloc=(\d+)M")
fin = re.compile(r"GC\((\d+)\) (\w+) finished\. (\d+)M->(\d+)M\((\d+)M\) used=(\d+)M pause-time=([0-9.]+)ms")
for path in sys.argv[1:]:
    text = open(path, errors="replace").read()
    b = text.find("BENCH_MARK start")
    e = text.find("BENCH_MARK end")
    if b >= 0 and e > b:
        text = text[b:e]
    info = {}
    kinds = defaultdict(list)
    causes = defaultdict(lambda: defaultdict(int))
    for m in start.finditer(text):
        info[m.group(1)] = (m.group(3), int(m.group(4)), int(m.group(5)))
    for m in fin.finditer(text):
        k = m.group(2)
        cause, incs, young = info.get(m.group(1), ("?", 0, 0))
        kinds[k].append((float(m.group(7)), young, incs, int(m.group(3)), int(m.group(4))))
        causes[k][cause] += 1
    print(f"== {path}")
    total = sum(p[0] for v in kinds.values() for p in v)
    print(f"   pauses={sum(len(v) for v in kinds.values())} total pause={total:.0f} ms")
    for k, v in kinds.items():
        pa = [p[0] for p in v]
        print(f"   {k:12s} n={len(v):3d} median={statistics.median(pa):6.2f} ms max={max(pa):6.2f} ms median young-alloc={statistics.median(p[1] for p in v):4.0f} MiB "
              f"median incs(logged)={statistics.median(p[2] for p in v):6.0f} heap before->after median {statistics.median(p[3] for p in v):.0f}->{statistics.median(p[4] for p in v):.0f} MiB causes={dict(causes[k])}")
