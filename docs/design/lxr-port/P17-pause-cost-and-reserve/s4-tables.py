#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""S4 tables from the traced runs (trace-s4.sh).

usage: s4-tables.py <trace-dir>
Prints (1) the pause time per kind for the accepted build, the S3 tip and the S4 build, pooled over the runs of a cell,
(2) the median time of the phases that S4 changes in RefCount pauses (S3 against S4), (3) their share of the RefCount
pause time, (4) per-run totals: wall time, collection time, number of pauses.
"""

import glob
import json
import statistics
import sys

KINDS = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
CELLS = (("binarytrees", 40), ("requests", 84), ("listsuffix", 40))
MODES = (("00", "non-moving"), ("10", "nursery copy"))
BUILDS = ("acc", "s3", "s4")


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
    print("| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S3: n, median, max ms | S4: n, median, max ms |")
    print("|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, name in MODES:
            rows = {b: pauses(pooled(directory, b, workload, heap, mode)) for b in BUILDS}
            for kind in ("RefCount", "InitialMark", "FinalMark", "Full"):
                values = [[v for k, v in rows[b] if k == kind] for b in BUILDS]
                if not any(values):
                    continue
                print(f"| {workload} | {name} | {kind} | " + " | ".join(cell(v) for v in values) + " |")


def phase_table(directory):
    fields = ("stw_pause_us", "release_epoch_us", "reclaim_nursery_us", "sweep_blocks_us", "scan_heap_statistics_us",
              "post_drain_us")
    print("| workload | mode | build | RefCount pauses | " + " | ".join(f.replace("_us", "") + " median ms" for f in fields) + " |")
    print("|---|---|---|---|" + "---|" * len(fields))
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in ("s3", "s4"):
                events = [e for e in pooled(directory, build, workload, heap, mode) if e.get("kind") == 1]
                if not events:
                    continue
                medians = " | ".join(f"{statistics.median(e.get(f, 0) for e in events) / 1000.0:.2f}" for f in fields)
                print(f"| {workload} | {name} | {build} | {len(events)} | {medians} |")


def share_table(directory):
    fields = ("release_epoch_us", "reclaim_nursery_us", "sweep_blocks_us", "scan_heap_statistics_us")
    print("| workload | mode | build | " + " | ".join(f.replace("_us", "") + " % of all RefCount pause time" for f in fields) + " |")
    print("|---|---|---|" + "---|" * len(fields))
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in ("s3", "s4"):
                events = [e for e in pooled(directory, build, workload, heap, mode) if e.get("kind") == 1]
                total = sum(e["stw_pause_us"] for e in events)
                if not total:
                    continue
                shares = " | ".join(f"{100.0 * sum(e.get(f, 0) for e in events) / total:.1f}" for f in fields)
                print(f"| {workload} | {name} | {build} | {shares} |")


def run_table(directory):
    print("| workload | mode | build | runs | wall ms (median) | gc ms (median) | pauses (median) | checksums |")
    print("|---|---|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in BUILDS:
                summaries = [load(p)[1] for p in sorted(glob.glob(f"{directory}/{build}-{workload}-{heap}-{mode}-r?.txt"))]
                summaries = [s for s in summaries if "elapsed_ms" in s]
                if not summaries:
                    print(f"| {workload} | {name} | {build} | 0 | - | - | - | - |")
                    continue
                print(f"| {workload} | {name} | {build} | {len(summaries)} | "
                      f"{statistics.median(s['elapsed_ms'] for s in summaries):.0f} | "
                      f"{statistics.median(s['gc_ms'] for s in summaries):.0f} | "
                      f"{statistics.median(s['gc_count'] for s in summaries):.0f} | "
                      f"{len({s['checksum'] for s in summaries})} |")


def main():
    directory = sys.argv[1]
    print("## Pause time per kind\n")
    pause_table(directory)
    print("\n## Median time of the phases that S4 changes, RefCount pauses (ms)\n")
    phase_table(directory)
    print("\n## Share of the changed phases in all RefCount pause time (%)\n")
    share_table(directory)
    print("\n## Per-run totals\n")
    run_table(directory)


if __name__ == "__main__":
    main()
