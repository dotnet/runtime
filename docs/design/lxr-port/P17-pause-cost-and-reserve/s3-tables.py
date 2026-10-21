#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""S3 tables from the traced runs (trace-s3.sh).

usage: s3-tables.py <trace-dir>
Prints (1) the pause time per kind for the accepted build, the S2 tip and the S3 build, pooled over the three runs of a
cell, (2) the phase shares of the pause for the S2 tip and the S3 build (RefCount pauses), (3) the native plane commit
size (metric 38) and the managed committed bytes.
"""

import glob
import json
import statistics
import sys

KINDS = {0: "Full", 1: "RefCount", 2: "InitialMark", 3: "FinalMark"}
CELLS = (("binarytrees", 40), ("requests", 84), ("listsuffix", 40))
MODES = (("00", "non-moving"), ("10", "nursery copy"))
MIB = 1048576.0


def load(path):
    events = []
    for line in open(path, encoding="utf-8", errors="replace"):
        if line.startswith("trace_event="):
            events.append(json.loads(line[len("trace_event="):]))
    return events


def pooled(directory, build, workload, heap, mode):
    events = []
    for path in sorted(glob.glob(f"{directory}/{build}-{workload}-{heap}-{mode}-r?.txt")):
        events.extend(load(path))
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
    return f"{len(values)}, {statistics.median(values):.0f}, {max(values):.0f}" if values else "-"


def pause_table(directory):
    print("| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S2: n, median, max ms | S3: n, median, max ms |")
    print("|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, name in MODES:
            rows = {b: pauses(pooled(directory, b, workload, heap, mode)) for b in ("acc", "s2", "s3")}
            for kind in ("RefCount", "InitialMark", "FinalMark", "Full"):
                values = [[v for k, v in rows[b] if k == kind] for b in ("acc", "s2", "s3")]
                if not any(values):
                    continue
                print(f"| {workload} | {name} | {kind} | " + " | ".join(cell(v) for v in values) + " |")


def share(events, field):
    total = sum(e["stw_pause_us"] for e in events)
    return 100.0 * sum(e.get(field, 0) for e in events) / total if total else 0.0


def phase_table(directory):
    fields = ("reclaim_nursery_us", "release_epoch_us", "sweep_blocks_us", "scan_lease_finalizers_us",
              "scan_heap_statistics_us", "retire_sources_us", "root_capture_us")
    print("| workload | mode | build | RefCount pauses | median pause ms | " +
          " | ".join(f.replace("_us", "") + " % of pause" for f in fields) + " |")
    print("|---|---|---|---|---|" + "---|" * len(fields))
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in ("s2", "s3"):
                events = [e for e in pooled(directory, build, workload, heap, mode) if e["kind"] == 1]
                if not events:
                    continue
                shares = " | ".join(f"{share(events, f):.1f}" for f in fields)
                print(f"| {workload} | {name} | {build} | {len(events)} | "
                      f"{statistics.median(e['stw_pause_us'] for e in events) / 1000.0:.0f} | {shares} |")


def phase_ms_table(directory):
    fields = ("reclaim_nursery_us", "scan_lease_finalizers_us", "scan_heap_statistics_us", "release_epoch_us")
    print("| workload | mode | build | " + " | ".join(f.replace("_us", "") + " median ms" for f in fields) + " |")
    print("|---|---|---|" + "---|" * len(fields))
    for workload, heap in CELLS:
        for mode, name in MODES:
            for build in ("s2", "s3"):
                events = [e for e in pooled(directory, build, workload, heap, mode) if e["kind"] == 1]
                if not events:
                    continue
                medians = " | ".join(f"{statistics.median(e.get(f, 0) for e in events) / 1000.0:.1f}" for f in fields)
                print(f"| {workload} | {name} | {build} | {medians} |")


def plane_table(directory):
    print("| workload | heap MiB | mode | S2 native planes max (MiB) | S3 native planes max (MiB) | saved (MiB) | S2 managed committed max (MiB) | S3 managed committed max (MiB) |")
    print("|---|---|---|---|---|---|---|---|")
    for workload, heap in CELLS:
        for mode, name in MODES:
            values = {}
            for build in ("s2", "s3"):
                runs = [load(p) for p in sorted(glob.glob(f"{directory}/{build}-{workload}-{heap}-{mode}-r?.txt"))]
                planes = statistics.median(max(e.get("native_planes", 0) for e in r) for r in runs)
                committed = statistics.median(max(e.get("gc_committed", 0) for e in r) for r in runs)
                values[build] = (planes, committed)
            print(f"| {workload} | {heap} | {name} | {values['s2'][0] / MIB:.1f} | {values['s3'][0] / MIB:.1f} | "
                  f"{(values['s2'][0] - values['s3'][0]) / MIB:.1f} | {values['s2'][1] / MIB:.1f} | {values['s3'][1] / MIB:.1f} |")


def main():
    directory = sys.argv[1]
    print("## Pause time per kind\n")
    pause_table(directory)
    print("\n## Phase shares of the RefCount pause\n")
    phase_table(directory)
    print("\n## Median time of single phases in RefCount pauses (ms)\n")
    phase_ms_table(directory)
    print("\n## Native plane commit size and managed committed bytes\n")
    plane_table(directory)


if __name__ == "__main__":
    main()
