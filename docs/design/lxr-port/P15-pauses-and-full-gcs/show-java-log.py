#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Summarize the Java LXR's own collection log: kinds, pauses, causes and the numbers behind its next-collection hints."""

import re
import sys
from collections import Counter

text = open(sys.argv[1], encoding="utf-8", errors="replace").read()
starts = re.findall(r"GC\((\d+)\) (\w+) start\. cause=(\w+) incs=(\d+) young-alloc=(\d+)M", text)
finish = re.findall(r"GC\((\d+)\) (\w+) finished\. (\d+)M->(\d+)M\((\d+)M\) used=(\d+)M pause-time=([0-9.]+)ms", text)
pages = re.findall(r"total_pages=(\d+) stop_pages=(\d+) mature_space_pages=(\d+) available_pages=(\d+)", text)
hints = re.findall(r"Hint: cycle = (\w+), emergency = (\w+)", text)
print("collections:", len(finish), "kinds:", dict(Counter(k for _, k, *_ in finish)), "causes:", dict(Counter(c for _, _, c, *_ in starts)))
for kind in sorted({k for _, k, *_ in finish}):
    pauses = [float(p) for _, k, *_, p in finish if k == kind]
    print(f"  {kind:10} n={len(pauses):4} mean pause {sum(pauses) / len(pauses):7.2f} ms  max {max(pauses):7.2f} ms")
if pages:
    total = int(pages[0][0])
    mature = [int(p[2]) for p in pages]
    print(f"total_pages={total} (={total * 4 / 1024:.0f} MiB)  emergency threshold = {int(total * 0.85)} mature pages")
    print(f"mature_space_pages min {min(mature)} max {max(mature)} ({max(mature) * 4 / 1024:.1f} MiB = {100 * max(mature) / total:.0f}% of total)")
print("emergency hints true:", sum(1 for _, e in hints if e == "true"), "of", len(hints), "; cycle hints true:", sum(1 for c, _ in hints if c == "true"))
young = [int(s[4]) for s in starts]
if young:
    print(f"young allocation per collection: mean {sum(young) / len(young):.0f} MiB, min {min(young)}, max {max(young)}")
live = [int(f[3]) for f in finish]
print(f"heap after collection (MiB): min {min(live)} max {max(live)}")
