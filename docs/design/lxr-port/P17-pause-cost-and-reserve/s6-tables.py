#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""S6 tables from the traced runs (trace-s6.sh).

usage: s6-tables.py <trace-dir>
Prints (1) the pause time per kind for the accepted build, the S5b tip and the S6 build, pooled over the runs of a cell,
(2) the median time of the phases that S6 changes (root capture, the scan of kind 0, the three finalizer steps) in
RefCount pauses, (3) their share of the RefCount pause time, (4) the same phases in tracing pauses, (5) per-run totals.
In the S6 build the three finalizer steps are: scan_lease_finalizers = registry capture (new entries, releases, ready
roots), scan_queue_finalizers = decision of a tracing pause (registry walk and tracing of the candidates),
scan_trace_finalizers = the tracing of the candidates alone (a part of scan_queue_finalizers).
"""

import glob
import json
import statistics
import sys

KINDS = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
CELLS = (("binarytrees", 40), ("requests", 84), ("listsuffix", 40))
MODES = (("00", "non-moving"), ("10", "nursery copy"), ("11", "mature copy"))
BUILDS = ("acc", "s5", "s6")
NAMES = {"acc": "accepted", "s5": "S5b tip", "s6": "S6"}


def load(path):
    events = []
    summary = {}
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("trace_event="):
            events.append(json.loads(line[len("trace_event="):]))
        elif line.startswith("{") and '"checksum"' in line:
            summary = json.loads(line)
    return events, summary


def pooled(directory, build, workload, heap, mode):
    events = []
    for path in sorted(glob.glob(f"{directory}/{build}-{workload}-{heap}-{mode}-r?.txt")):
        events.extend(load(path)[0])
    return events


def old_format_pauses(events):
    out = []
    previous = {"full": 0, "initial": 0, "final": 0}
    for e in events:
        kind = "RefCount"
        if e["full"] > previous["full"]:
            kind = "Full"
        elif e["initial"] > previous["initial"]:
            kind = "InitialMark"
        elif e["final"] > previous["final"]:
            kind = "FinalMark"
        previous = {key: e[key] for key in previous}
        out.append((kind, e["pause_ms"]))
    return out


def pauses(events):
    if events and "stw_pause_us" in events[0]:
        return [(KINDS.get(e["kind"], str(e["kind"])), e["stw_pause_us"] / 1000.0) for e in events]
    return old_format_pauses(events)


def cell(values):
    return f"{len(values)}, {statistics.median(values):.1f}, {max(values):.0f}" if values else "-"


def pause_table(directory):
    print("| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S5b: n, median, max ms | S6: n, median, max ms |")
    print("|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, name in MODES:
            rows = {b: pauses(pooled(directory, b, workload, heap, mode)) for b in BUILDS}
            for kind in ("RefCount", "InitialMark", "FinalMark", "Full"):
                values = [[v for k, v in rows[b] if k == kind] for b in BUILDS]
                if not any(values):
                    continue
                print(f"| {workload} | {name} | {kind} | " + " | ".join(cell(v) for v in values) + " |")


FIELDS = ("stw_pause_us", "root_capture_us", "scan0_us", "scan_lease_finalizers_us", "scan_queue_finalizers_us",
          "scan_trace_finalizers_us")


def phase_table(directory, kinds, title):
    print(f"| workload | mode | build | {title} pauses | " + " | ".join(f.replace("_us", "") + " median ms" for f in FIELDS) + " |")
    print("|---|---|---|---|" + "---|" * len(FIELDS))
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in BUILDS:
                events = [e for e in pooled(directory, build, workload, heap, mode) if e.get("kind") in kinds]
                if not events:
                    continue
                medians = " | ".join(f"{statistics.median(e.get(f, 0) for e in events) / 1000.0:.2f}" for f in FIELDS)
                print(f"| {workload} | {name} | {NAMES[build]} | {len(events)} | {medians} |")


def share_table(directory):
    fields = ("root_capture_us", "scan0_us", "scan_lease_finalizers_us")
    print("| workload | mode | build | " + " | ".join(f.replace("_us", "") + " % of all RefCount pause time" for f in fields) + " |")
    print("|---|---|---|" + "---|" * len(fields))
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in BUILDS:
                events = [e for e in pooled(directory, build, workload, heap, mode) if e.get("kind") == 1]
                total = sum(e["stw_pause_us"] for e in events)
                if not total:
                    continue
                shares = " | ".join(f"{100.0 * sum(e.get(f, 0) for e in events) / total:.1f}" for f in fields)
                print(f"| {workload} | {name} | {NAMES[build]} | {shares} |")


def run_table(directory):
    print("| workload | mode | build | runs | wall ms (median) | gc ms (median) | pauses (median) | tracing pauses (median) | checksums |")
    print("|---|---|---|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in BUILDS:
                summaries = []
                tracing = []
                for p in sorted(glob.glob(f"{directory}/{build}-{workload}-{heap}-{mode}-r?.txt")):
                    events, summary = load(p)
                    if "elapsed_ms" in summary:
                        summaries.append(summary)
                        tracing.append(sum(1 for k, _ in pauses(events) if k in ("InitialMark", "Full")))
                if not summaries:
                    print(f"| {workload} | {name} | {NAMES[build]} | 0 | - | - | - | - | - |")
                    continue
                print(f"| {workload} | {name} | {NAMES[build]} | {len(summaries)} | "
                      f"{statistics.median(s['elapsed_ms'] for s in summaries):.0f} | "
                      f"{statistics.median(s['gc_ms'] for s in summaries):.0f} | "
                      f"{statistics.median(s['gc_count'] for s in summaries):.0f} | "
                      f"{statistics.median(tracing):.0f} | "
                      f"{len({s['checksum'] for s in summaries})} |")


def main():
    directory = sys.argv[1]
    print("## Pause time per kind\n")
    pause_table(directory)
    print("\n## Median time of the phases that S6 changes, RefCount pauses (ms)\n")
    phase_table(directory, (1,), "RefCount")
    print("\n## Share of the changed phases in all RefCount pause time (%)\n")
    share_table(directory)
    print("\n## The same phases in tracing pauses (Full and FinalMark), median ms\n")
    phase_table(directory, (0, 3), "tracing")
    print("\n## Per-run totals\n")
    run_table(directory)


if __name__ == "__main__":
    main()
