#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""S5b tables from the traced runs without a hard limit (trace-s5b.sh).

usage: s5b-tables.py <trace-dir>
Prints (1) per cell and build: wall time, collection time, collections, full collections, pause time (RefCount kind),
peak RSS, pooled over the repetitions as medians; (2) for the S5b build: how much capacity a pause borrows, how often
the give-back succeeds, how much stays borrowed after the pause, and the cost of admission and of the give-back.
"""

import glob
import json
import statistics
import sys

CELLS = (("binarytrees", 40), ("requests", 84), ("listsuffix", 40))
MODES = (("10", "nursery"), ("11", "mature"))
BUILDS = (("acc", "accepted"), ("s4", "S4"), ("off", "S5b, overshoot off"), ("s5", "S5b"))
MIB = 1024 * 1024


def load(path):
    events = []
    summary = {}
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("trace_event="):
            events.append(json.loads(line[len("trace_event="):]))
        elif line.startswith("{") and '"checksum"' in line:
            summary = json.loads(line)
    return events, summary


def med(values):
    values = list(values)
    return statistics.median(values) if values else float("nan")


def mx(values):
    values = list(values)
    return max(values) if values else float("nan")


def runs(directory, build, workload, heap, mode):
    out = []
    for path in sorted(glob.glob(f"{directory}/{build}-{workload}-{heap}-{mode}-r?.txt")):
        events, summary = load(path)
        out.append((events, summary))
    return out


def main():
    directory = sys.argv[1]
    print("## Wall time, collections, pauses and peak RSS (medians of the runs that finished; ok = finished / all)\n")
    print("| workload | mode | build | ok | wall ms | gc ms | collections | full | pause ms (median of all pauses) | pause ms (max) | peak RSS MB |")
    print("|---|---|---|---|---|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, mode_name in MODES:
            for build, build_name in BUILDS:
                rs = runs(directory, build, workload, heap, mode)
                if not rs:
                    continue
                ok = [(e, s) for e, s in rs if s.get("elapsed_ms")]
                if not ok:
                    print(f"| {workload} | {mode_name} | {build_name} | 0/{len(rs)} | - | - | - | - | - | - | - |")
                    continue
                pauses = [e["stw_pause_us"] / 1000.0 if "stw_pause_us" in e else e.get("pause_ms", 0.0)
                          for events, _ in ok for e in events]
                print(f"| {workload} | {mode_name} | {build_name} | {len(ok)}/{len(rs)} | "
                      f"{med(s['elapsed_ms'] for _, s in ok):.0f} | {med(s['gc_ms'] for _, s in ok):.0f} | "
                      f"{med(s['gc_count'] for _, s in ok):.0f} | {med(s['lxr_full'] for _, s in ok):.0f} | "
                      f"{med(pauses):.1f} | {mx(pauses):.1f} | {med(s['peak_rss_kb'] / 1024 for _, s in ok):.0f} |")
    print("\n## Borrowed capacity (S5b build; pooled over the pauses of all repetitions)\n")
    print("| workload | mode | pauses | pauses that borrow | borrow MiB median / max | request pages median | free pages median | "
          "refused | give-back attempts | succeeded | complete (back to the kept capacity) | "
          "borrowed after the pause MiB median / max | peak capacity MiB (max of runs) |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, mode_name in MODES:
            rs = [(e, s) for e, s in runs(directory, "s5", workload, heap, mode) if s.get("elapsed_ms")]
            if not rs:
                continue
            events = [e for ev, _ in rs for e in ev]
            borrow = [e["borrow_bytes_last"] / MIB for e in events if e["borrow_bytes_last"] > 0]
            finals = [ev[-1] for ev, _ in rs if ev]
            print(f"| {workload} | {mode_name} | {len(events)} | {len(borrow)} | "
                  f"{med(borrow):.1f} / {mx(borrow):.1f} | {med(e['borrow_request_pages'] for e in events):.0f} | "
                  f"{med(e['borrow_free_pages'] for e in events):.0f} | {sum(f['borrow_refused'] for f in finals)} | "
                  f"{sum(f['giveback_attempts'] for f in finals)} | {sum(f['giveback_successes'] for f in finals)} | "
                  f"{sum(f['giveback_complete'] for f in finals)} | "
                  f"{med(e['borrowed_now'] / MIB for e in events):.1f} / {mx(e['borrowed_now'] / MIB for e in events):.1f} | "
                  f"{mx(f['peak_capacity'] / MIB for f in finals):.0f} |")
    print("\n## Cost of admission and give-back per pause (S5b build, microseconds)\n")
    print("| workload | mode | borrow us median / max | give-back us median / max (pauses with an attempt) | "
          "borrow phase us median (timer) | give_back phase us median (timer) | stw pause us median |")
    print("|---|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, mode_name in MODES:
            rs = [(e, s) for e, s in runs(directory, "s5", workload, heap, mode) if s.get("elapsed_ms")]
            if not rs:
                continue
            events = [e for ev, _ in rs for e in ev]
            b = [e["borrow_ns_last"] / 1000.0 for e in events if e["borrow_ns_last"] > 0]
            g = [e["giveback_ns_last"] / 1000.0 for e in events if e["giveback_ns_last"] > 0]
            bt = [e["borrow_us"] for e in events if e["borrow_us"] > 0]
            gt = [e["give_back_us"] for e in events if e["give_back_us"] > 0]
            print(f"| {workload} | {mode_name} | {med(b):.0f} / {mx(b):.0f} | {med(g):.0f} / {mx(g):.0f} | "
                  f"{med(bt):.0f} | {med(gt):.0f} | {med(e['stw_pause_us'] for e in events):.0f} |")


if __name__ == "__main__":
    main()
