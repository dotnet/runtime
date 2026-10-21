#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Write the natural-collection results as roadmap benchmark checkpoints (.NET arms; Java ratios go in the notes)."""

import argparse
import json
from pathlib import Path

HERE = Path(__file__).parent
NAMES = {"binarytrees": "Binary trees", "requests": "Requests", "listsuffix": "List suffix"}
UNITS = {"binarytrees": 1, "requests": 600000, "listsuffix": 800000}
ARMS = {"nonmoving": "net-lxr-nonmoving", "nursery": "net-lxr-nursery", "mature": "net-lxr-mature"}
COMMIT = "e8d2d23ef60e44ba7efd35a7cb3f4a76271adc6a"
MODULE = "0909a5b1ce5e6383e944d45ffd17791050acfb62a26b048c4955fb0917b71e14"
CORECLR = "e0f2e01f19eeee42aa510bc969fc196d7c3e0973396962fd9e04f5914d1ba56f"
MACHINE = {"cpu": "Intel Xeon Platinum 8370C", "logicalCores": 16, "virtualized": True, "os": "Linux under WSL2"}
NOTE = ("NATURAL COLLECTIONS, NO INDUCED COLLECTIONS: three matched programs (binary trees, requests, list suffix) written in C# and Java, "
        "same random numbers and checksums, no explicit collection while measured. Heap = 2/4/8 times the smallest heap in which "
        ".NET Workstation finishes. Operations/s = requests, list steps or complete binary-trees runs per second. Stall percentiles come "
        "from a 1 ms sleeper thread, not a pause log, and are not coordinated-omission-free. Linux only, 3 runs per case, median. "
        "Java ratios (Java LXR / Java G1 wall): {java}. Accepted port e8d2d23, not a candidate.")


def cell(data, workload, factor, collector):
    for item in data["cells"]:
        if (item["workload"], item["factor"], item["collector"]) == (workload, factor, collector):
            return item
    raise KeyError((workload, factor, collector))


def record(data, workload, factor, collector, mode):
    item = cell(data, workload, factor, collector)
    kind = {"net-wks": "wks", "net-server": "svr"}.get(collector, "lxr")
    base = {
        "scenario": f"P14 natural {NAMES[workload]} heap {factor:g}x / linux-x64", "collector": kind,
        "heapFactor": factor, "heapLimitMb": item["heap_mib"], "managedAdmissionMb": item["heap_mib"] if kind == "lxr" else None,
        "latencyMethod": "1ms-sleeper-thread-stall", "inducedCollections": 0, "invocations": item["runs"],
        "studyArm": {"lxr": f"LXR-{mode}", "wks": "Workstation", "svr": "Server"}[kind],
        "recordTip": COMMIT, "sourceStage": "natural-collection matrix", "coreclrSha256": CORECLR,
        "moduleSha256": MODULE if kind == "lxr" else None,
        "runtimeBuildId": f"accepted e8d2d23 Linux Release; Java/.NET matched programs",
        "rawSamplesPath": "docs/design/lxr-port/P14-natural-gc/results/runs.jsonl", "machine": MACHINE,
    }
    if "elapsed_ms" not in item:
        base.update(valid=False, status="crashed", invalidReason=f"{item['ok']} of {item['runs']} runs finished: Out of memory",
                    collectorConfirmed=True, operationsPerSecond=None, notes="Our port stops with Out of memory in this heap; the standard .NET collectors finish.")
        return base
    pauses = item["gc_count"]
    base.update(
        valid=True, status="ok", collectorConfirmed=True, operationsPerSecond=UNITS[workload] / (item["elapsed_ms"] / 1000.0),
        workingSetMb=item["peak_rss_mb"], committedMb=None, privateMb=None, regionWallMs=item["elapsed_ms"], processCpuMs=item["cpu_ms"],
        pauseAverageMs=item["gc_ms"] / pauses if pauses else None, pauseP99Ms=None, pauseMaxMs=None,
        latencyP99Ms=item["hiccup_p99_us"] / 1000.0, latencyMaxMs=item["hiccup_max_us"] / 1000.0,
        notes=f"{pauses:.0f} collections, {item['gc_ms']:.0f} ms of pauses in the measured part; wall range {item['elapsed_min']:.0f} to {item['elapsed_max']:.0f} ms.")
    if kind == "lxr":
        base["notes"] += f" Full collections: {item['lxr_full']:.0f}."
    return base


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--summary", default=str(HERE / "results" / "summary.json"))
    parser.add_argument("--output-directory", required=True)
    args = parser.parse_args()
    data = json.loads(Path(args.summary).read_text(encoding="utf-8"))
    java = []
    for workload in NAMES:
        for factor in (2.0, 4.0, 8.0):
            ratio = cell(data, workload, factor, "java-lxr")["elapsed"]
            java.append(f"{NAMES[workload]} {factor:g}x = {ratio:.2f}")
    for index, (mode, collector) in enumerate(ARMS.items()):
        results = []
        for workload in NAMES:
            for factor in (2.0, 4.0, 8.0):
                for arm in (collector, "net-wks", "net-server"):
                    results.append(record(data, workload, factor, arm, mode))
        checkpoint = {
            "id": f"p14-natural-gc-{index}-linux-{mode}", "date": "2026-10-19", "stepId": "P9.3",
            "notes": NOTE.format(java="; ".join(java)) + f" Port mode: {mode}.", "results": results,
        }
        path = Path(args.output_directory) / f"p14-natural-gc-{index}-linux-{mode}.json"
        path.write_text(json.dumps({"schemaVersion": 2, "checkpoints": [checkpoint]}, indent=2) + "\n", encoding="utf-8", newline="\n")
        print("wrote", path.name, len(results), "records")


if __name__ == "__main__":
    main()
