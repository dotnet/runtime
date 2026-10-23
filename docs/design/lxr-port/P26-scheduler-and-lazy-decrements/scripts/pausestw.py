#!/usr/bin/env python3
"""P26: pause medians of the traced runs, two ways: total_us (the whole collection, with the waits for lazy work before
and after the pause) and stw_pause_us (the time that the program threads are stopped).
usage: pausestw.py <results-dir> <prefix> <step> [<step> ...]"""
import json
import statistics
import sys
from pathlib import Path

root = Path(sys.argv[1])
prefix = sys.argv[2]
NAMES = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
print("| step | program | arm | kind | n | median total ms | median stop ms | max stop ms |")
print("|---|---|---|---|---|---|---|---|")
for step in sys.argv[3:]:
    for w in ("binarytrees", "requests", "listsuffix"):
        for a in ("lxr-nonmoving", "lxr-nursery"):
            path = root / f"{prefix}-{step}-trace-{w}-{a}.txt"
            if not path.exists():
                continue
            events = []
            for line in path.read_text(errors="replace").splitlines():
                if line.startswith("trace_event="):
                    events.append(json.loads(line[len("trace_event="):]))
            for kind in sorted({e["kind"] for e in events}):
                ev = [e for e in events if e["kind"] == kind]
                total = [e["total_us"] / 1000 for e in ev]
                stw = [e["stw_pause_us"] / 1000 for e in ev]
                print(f"| {step} | {w} | {a} | {NAMES.get(kind, kind)} | {len(ev)} | {statistics.median(total):.1f} | "
                      f"{statistics.median(stw):.1f} | {max(stw):.1f} |")
