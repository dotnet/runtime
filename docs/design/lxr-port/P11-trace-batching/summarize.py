#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Summarize retained observations, including goal misses, without retries."""

import argparse
import csv
import json
from pathlib import Path


def read_csv(path):
    with path.open(newline="") as stream:
        return list(csv.DictReader(stream))


def write_csv(path, rows):
    if not rows:
        raise RuntimeError(f"No rows for {path}")
    with path.open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve()
    timing = read_csv(root / "timing" / "summary.csv")
    comparisons = []
    goals = []
    for count, input_name in sorted({(row["count"], row["input"]) for row in timing}):
        for mode in ("nonmoving", "nursery", "mature"):
            before = next(row for row in timing if row["count"] == count and row["input"] == input_name and row["arm"] == "before-" + mode)
            after = next(row for row in timing if row["count"] == count and row["input"] == input_name and row["arm"] == "after-" + mode)
            workstation = next(row for row in timing if row["count"] == count and row["input"] == input_name and row["arm"] == "WKS")
            row = dict(count=int(count), input=input_name, mode=mode)
            for key in ("elapsed_ms", "cpu_ms", "full_sum_ms", "call_p95_ms", "rss_bytes", "private_bytes"):
                old, new = float(before[key]), float(after[key])
                row["before_" + key] = old
                row["after_" + key] = new
                row[key + "_ratio"] = new / old if old else None
            row["remaining_workstation_wall_ratio"] = float(after["elapsed_ms"]) / float(workstation["elapsed_ms"])
            comparisons.append(row)
            if int(count) == 262144:
                limit = 0.85 if input_name == "references" else 1.10
                for key in ("elapsed_ms", "cpu_ms"):
                    ratio = row[key + "_ratio"]
                    goals.append(dict(count=int(count), input=input_name, mode=mode, goal=key + "_ratio",
                                      value=ratio, limit=limit, passed=ratio is not None and ratio <= limit))
    write_csv(root / "comparisons.csv", comparisons)
    diagnostics = []
    for row in read_csv(root / "diagnostic" / "results.csv"):
        record = json.loads(row["record"])
        initial, live = record["before"]["packet_counters"], record["live"]["packet_counters"]
        result = dict(count=int(row["count"]), input=row["input"], arm=row["arm"], checks=record["checks"])
        for key in live:
            if live[key] is None or initial[key] is None:
                result[key] = None
            elif key.endswith("_peak"):
                result[key] = live[key]
            else:
                result[key] = live[key] - initial[key]
        result["combined_packet_pending_peak_bytes"] = live["work_peak"]
        result["descriptor_peak_bytes"] = live["scan_peak"]
        diagnostics.append(result)
        if row["count"] == "262144" and row["input"] == "references" and row["arm"].startswith("after-"):
            for key, limit in (("work_allocations", 50000), ("combined_packet_pending_peak_bytes", 16 * 2**20)):
                goals.append(dict(count=262144, input="references", mode=row["arm"][6:], goal=key,
                                  value=result[key], limit=limit, passed=result[key] <= limit))
    write_csv(root / "diagnostic-deltas.csv", diagnostics)
    write_csv(root / "goals.csv", goals)
    pressure = read_csv(root / "pressure" / "summary.csv")
    pressure_comparisons = []
    for mode in ("nonmoving", "nursery", "mature"):
        old = next(row for row in pressure if row["arm"] == "before-" + mode)
        new = next(row for row in pressure if row["arm"] == "after-" + mode)
        result = dict(mode=mode)
        for key in ("elapsed_ms", "cpu_ms", "collections", "rss_bytes", "private_bytes"):
            result["before_" + key], result["after_" + key] = float(old[key]), float(new[key])
            result[key + "_ratio"] = float(new[key]) / float(old[key]) if float(old[key]) else None
        pressure_comparisons.append(result)
    write_csv(root / "pressure-comparisons.csv", pressure_comparisons)
    print("Goal results:", sum(row["passed"] for row in goals), "/", len(goals))
    for row in goals:
        if not row["passed"]:
            print("GOAL MISS:", row)


if __name__ == "__main__":
    main()
