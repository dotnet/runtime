#!/usr/bin/env python3
"""Compare two published checkpoints cell by cell, and set the difference between them against the
spread *within* each of them.

The correctness criterion for P0.5 is that baselines are reproducible across repeated sessions on the
same machine state, "with variance recorded rather than averaged away". Averaging the two sessions
would satisfy the letter of a baseline and destroy exactly the evidence the criterion asks for, so
this script never combines them: it prints both, their difference, and the within-session spread that
difference has to be read against.

It deliberately does not perform a significance test. This host reached a 8.35% half-width at n=15 in
its own control ladder (section 4 of the baselines document), and n here is 5; a p-value computed on
five points from a machine whose resolution ladder is non-monotone would be a more confident claim
than the data supports. Two means, two coefficients of variation, and their subtraction is what the
data can carry.

Rule 26: every comparison prints the numbers it compared.
"""

from __future__ import annotations

import argparse
import collections
import csv
import json
import pathlib
import statistics
import sys

# The primary metric of a cell decides which column is the one to compare; comparing latency across
# sessions for a throughput-primary cell would be comparing two incidental numbers.
LATENCY_PRIMARY = {"aspnet-request-load"}


def phase_of(run_id: str) -> str:
    return "latency" if run_id.endswith(".latency") else "throughput"


def primary_field(scenario: str, phase: str) -> str:
    if phase == "latency" or scenario in LATENCY_PRIMARY:
        return "latencyP99Ms"
    return "operationsPerSecond"


def load(checkpoint: pathlib.Path, csv_path: pathlib.Path):
    document = json.loads(checkpoint.read_text(encoding="utf-8"))
    records = [r for c in document.get("checkpoints", []) for r in c.get("results", [])]
    with csv_path.open(newline="", encoding="utf-8-sig") as handle:
        rows = list(csv.DictReader(handle))

    samples: dict[tuple, list[float]] = collections.defaultdict(list)
    rates: dict[tuple, set[str]] = collections.defaultdict(set)
    for row in rows:
        if row["valid"].strip().lower() != "true":
            continue
        phase = phase_of(row["runId"])
        key = (row["scenario"], row["collector"], round(float(row["heapFactor"]), 4), phase)
        field = primary_field(row["scenario"], phase)
        value = row.get(field, "")
        if value:
            samples[key].append(float(value))
        rate = row.get("arrivalRatePerSecond", "")
        if rate:
            rates[key].add(rate)

    return records, samples, rates


def cv_percent(values: list[float]) -> float | None:
    if len(values) < 2:
        return None
    mean = statistics.fmean(values)
    if mean == 0:
        return None
    return 100.0 * statistics.stdev(values) / mean


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--baseline", required=True, help="checkpoint json of the reference session")
    parser.add_argument("--baseline-csv", required=True)
    parser.add_argument("--other", required=True, help="checkpoint json of the session being compared")
    parser.add_argument("--other-csv", required=True)
    parser.add_argument("--label", required=True, help="what the comparison is, e.g. 'reproducibility'")
    parser.add_argument("--out", required=True, help="csv written with one row per shared cell")
    parser.add_argument(
        "--resolution-floor-percent",
        type=float,
        default=8.35,
        help="this session's own measured resolution half-width; differences below it are inside the noise",
    )
    args = parser.parse_args()

    base_records, base_samples, base_rates = load(pathlib.Path(args.baseline), pathlib.Path(args.baseline_csv))
    other_records, other_samples, other_rates = load(pathlib.Path(args.other), pathlib.Path(args.other_csv))

    print(f"comparison : {args.label}")
    print(f"baseline   : {args.baseline}  ({len(base_records)} records, {len(base_samples)} cells with samples)")
    print(f"other      : {args.other}  ({len(other_records)} records, {len(other_samples)} cells with samples)")

    shared = sorted(set(base_samples) & set(other_samples))
    only_base = len(set(base_samples) - set(other_samples))
    only_other = len(set(other_samples) - set(base_samples))
    print(f"cells in both : {len(shared)}   only in baseline : {only_base}   only in other : {only_other}")
    if not shared:
        print("FAIL: the two checkpoints share no cell, so nothing was compared")
        return 1

    rows = []
    exceeding = 0
    differing_load = 0
    for key in shared:
        scenario, collector, heap_factor, phase = key
        base_values = base_samples[key]
        other_values = other_samples[key]
        # An open-loop cell is only comparable across sessions if both sessions offered the same load.
        # They need not have: the arrival rate is derived from each session's *own* throughput pass, so
        # a session that measured higher capacity then offered a higher load, and the latency
        # difference that follows is a difference of configuration, not of the collector.
        base_rate = ",".join(sorted(base_rates.get(key, set()))) or "none"
        other_rate = ",".join(sorted(other_rates.get(key, set()))) or "none"
        same_load = base_rate == other_rate
        differing_load += 0 if same_load else 1
        base_mean = statistics.fmean(base_values)
        other_mean = statistics.fmean(other_values)
        base_cv = cv_percent(base_values)
        other_cv = cv_percent(other_values)
        ratio = other_mean / base_mean if base_mean else None
        difference = 100.0 * abs(ratio - 1.0) if ratio is not None else None
        widest = max(v for v in (base_cv, other_cv, 0.0) if v is not None)
        # "Exceeds" means the two sessions differ by more than either session's own invocation spread
        # *and* by more than this host's demonstrated resolution. Either alone would over-report.
        exceeds = (
            difference is not None
            and difference > widest
            and difference > args.resolution_floor_percent
        )
        exceeding += 1 if exceeds else 0
        rows.append(
            {
                "scenario": scenario,
                "collector": collector,
                "heapFactor": heap_factor,
                "phase": phase,
                "metric": primary_field(scenario, phase),
                "baselineMean": round(base_mean, 6),
                "baselineInvocations": len(base_values),
                "baselineCvPercent": None if base_cv is None else round(base_cv, 3),
                "otherMean": round(other_mean, 6),
                "otherInvocations": len(other_values),
                "otherCvPercent": None if other_cv is None else round(other_cv, 3),
                "ratioOtherOverBaseline": None if ratio is None else round(ratio, 6),
                "differencePercent": None if difference is None else round(difference, 3),
                "widestWithinSessionCvPercent": round(widest, 3),
                "baselineArrivalRate": base_rate,
                "otherArrivalRate": other_rate,
                "sameOfferedLoad": "true" if same_load else "false",
                "exceedsBothSpreadAndFloor": "true" if exceeds else "false",
            }
        )

    out_path = pathlib.Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with out_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0].keys()), lineterminator="\n")
        writer.writeheader()
        writer.writerows(rows)

    differences = [r["differencePercent"] for r in rows if r["differencePercent"] is not None]
    print(f"\nsession-to-session difference, percent: "
          f"min {min(differences):.2f}  median {statistics.median(differences):.2f}  max {max(differences):.2f}")
    print(f"cells whose difference exceeds both the within-session spread and the "
          f"{args.resolution_floor_percent}% resolution floor : {exceeding} of {len(rows)}")
    print(f"cells within one or the other                                                        "
          f"       : {len(rows) - exceeding} of {len(rows)}")
    # Two counts and a subtraction: how many of the disagreements are comparisons of different things.
    comparable = [r for r in rows if r["sameOfferedLoad"] == "true"]
    comparable_exceeding = sum(1 for r in comparable if r["exceedsBothSpreadAndFloor"] == "true")
    print(f"\ncells where both sessions offered the same load : {len(comparable)} of {len(rows)}"
          f"   (differing load: {differing_load})")
    print(f"  of those, exceeding spread and floor : {comparable_exceeding}")
    print(f"  of the {differing_load} differing-load cells, exceeding : {exceeding - comparable_exceeding}")

    print("\nevery shared cell, printed rather than summarised:")
    print(f"  {'scenario':26s} {'arm':4s} {'hf':>4s} {'phase':10s} {'baseline':>14s} {'other':>14s} "
          f"{'diff%':>7s} {'cv%':>6s} {'sameLoad':>9s} {'exceeds':>7s}")
    for row in rows:
        print(
            f"  {row['scenario']:26s} {row['collector']:4s} {row['heapFactor']:>4} {row['phase']:10s} "
            f"{row['baselineMean']:>14.4f} {row['otherMean']:>14.4f} "
            f"{row['differencePercent']:>7.2f} {row['widestWithinSessionCvPercent']:>6.2f} "
            f"{row['sameOfferedLoad']:>9s} {row['exceedsBothSpreadAndFloor']:>7s}"
        )

    print(f"\nwritten: {out_path}")
    # Disagreement is a finding to publish, not a failure to fix: the exit code reports whether the
    # comparison ran, not whether the two sessions agreed.
    return 0


if __name__ == "__main__":
    sys.exit(main())
