#!/usr/bin/env python3
"""Test whether the collector *ratio* reproduces across sessions where the absolute numbers do not.

The reproducibility comparison found that this host's absolute throughput moved by up to 28.5%
between two sessions of the same binary, in the same direction in all eight cells, tracking a 3.7
percentage-point difference in background load. That is a property of the machine, and it will not be
fixed by measuring more carefully.

But the two collector arms are interleaved *within* a session, so a machine-state shift that lifts one
arm lifts the other at nearly the same moment. If so, the srv/wks ratio survives what the absolute
numbers do not - and the ratio, not the absolute rate, is what a future LXR column has to be compared
against. This script measures that rather than assuming it, and prints both quantities side by side so
the claim can be checked instead of believed.
"""

from __future__ import annotations

import argparse
import collections
import csv
import json
import pathlib
import statistics
import sys

LATENCY_PRIMARY = {"aspnet-request-load"}


def phase_of(run_id: str) -> str:
    return "latency" if run_id.endswith(".latency") else "throughput"


def primary_field(scenario: str, phase: str) -> str:
    if phase == "latency" or scenario in LATENCY_PRIMARY:
        return "latencyP99Ms"
    return "operationsPerSecond"


def arm_means(csv_path: pathlib.Path) -> dict[tuple, float]:
    with csv_path.open(newline="", encoding="utf-8-sig") as handle:
        rows = list(csv.DictReader(handle))

    samples: dict[tuple, list[float]] = collections.defaultdict(list)
    for row in rows:
        if row["valid"].strip().lower() != "true":
            continue
        phase = phase_of(row["runId"])
        value = row.get(primary_field(row["scenario"], phase), "")
        if value:
            key = (row["scenario"], round(float(row["heapFactor"]), 4), phase, row["collector"])
            samples[key].append(float(value))

    return {key: statistics.fmean(values) for key, values in samples.items()}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--baseline-csv", required=True)
    parser.add_argument("--other-csv", required=True)
    parser.add_argument("--baseline-label", default="s2")
    parser.add_argument("--other-label", default="s3")
    parser.add_argument("--out", required=True)
    args = parser.parse_args()

    base = arm_means(pathlib.Path(args.baseline_csv))
    other = arm_means(pathlib.Path(args.other_csv))

    def ratios(means: dict[tuple, float]) -> dict[tuple, float]:
        out = {}
        for (scenario, heap_factor, phase, collector), value in means.items():
            if collector != "srv":
                continue
            baseline_value = means.get((scenario, heap_factor, phase, "wks"))
            if baseline_value:
                out[(scenario, heap_factor, phase)] = value / baseline_value
        return out

    base_ratios = ratios(base)
    other_ratios = ratios(other)
    shared = sorted(set(base_ratios) & set(other_ratios))

    print(f"cells with a srv/wks ratio in {args.baseline_label} : {len(base_ratios)}")
    print(f"cells with a srv/wks ratio in {args.other_label} : {len(other_ratios)}")
    print(f"cells in both : {len(shared)}")
    if not shared:
        print("FAIL: no cell has a ratio in both sessions; nothing was compared")
        return 1

    rows = []
    print(f"\n  {'scenario':26s} {'hf':>4s} {'phase':10s} "
          f"{'absolute drift%':>15s} {'ratio ' + args.baseline_label:>12s} "
          f"{'ratio ' + args.other_label:>12s} {'ratio drift%':>12s}")
    for key in shared:
        scenario, heap_factor, phase = key
        base_ratio = base_ratios[key]
        other_ratio = other_ratios[key]
        ratio_drift = 100.0 * abs(other_ratio / base_ratio - 1.0)

        # The absolute drift the ratio has to survive: the larger of the two arms' own drifts.
        absolute_drifts = []
        for collector in ("wks", "srv"):
            a = base.get((scenario, heap_factor, phase, collector))
            b = other.get((scenario, heap_factor, phase, collector))
            if a and b:
                absolute_drifts.append(100.0 * abs(b / a - 1.0))
        absolute_drift = max(absolute_drifts) if absolute_drifts else 0.0

        rows.append(
            {
                "scenario": scenario,
                "heapFactor": heap_factor,
                "phase": phase,
                "metric": primary_field(scenario, phase),
                f"ratio_{args.baseline_label}": round(base_ratio, 6),
                f"ratio_{args.other_label}": round(other_ratio, 6),
                "ratioDriftPercent": round(ratio_drift, 3),
                "largestAbsoluteDriftPercent": round(absolute_drift, 3),
                "ratioMoreStableThanAbsolute": "true" if ratio_drift < absolute_drift else "false",
            }
        )
        print(f"  {scenario:26s} {heap_factor:>4} {phase:10s} {absolute_drift:>15.2f} "
              f"{base_ratio:>12.4f} {other_ratio:>12.4f} {ratio_drift:>12.2f}")

    stabler = sum(1 for r in rows if r["ratioMoreStableThanAbsolute"] == "true")
    ratio_drifts = [r["ratioDriftPercent"] for r in rows]
    absolute = [r["largestAbsoluteDriftPercent"] for r in rows]
    print(f"\ncells where the ratio drifted less than the absolute numbers : {stabler} of {len(rows)}")
    print(f"  median ratio drift    : {statistics.median(ratio_drifts):.2f}%")
    print(f"  median absolute drift : {statistics.median(absolute):.2f}%")

    out_path = pathlib.Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()), lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)
    print(f"\nwritten: {out_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
