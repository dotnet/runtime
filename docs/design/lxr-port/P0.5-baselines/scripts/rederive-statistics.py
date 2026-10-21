#!/usr/bin/env python3
"""Recompute every published statistic in the P0.5 checkpoint from the raw per-invocation CSV.

The point of publishing the raw vector is that a reader does not have to trust the harness. This
script is that reader: it reads the CSV, recomputes each cell's statistics without importing
anything from the harness, and compares its answer to the published one.

It prints the comparison rather than a verdict. A script that printed only "all statistics
re-derive" would read identically whether it had checked 120 cells or zero, and one of P0.5's own
probes did exactly that - it reported "0 cells published valid over the bound" because the field it
was reading was absent from 108 of 120 rows, and the absence looked like a pass.

Two classes of published field are distinguished, because they are produced differently:

  mean fields   - averaged across the cell's valid invocations by the aggregator. These must
                  re-derive to within floating-point tolerance, and a mismatch fails the run.
  copy fields   - taken from a single invocation's report. These are checked against every
                  invocation in the cell: the assertion is that the published value equals one of
                  them, not their mean. See finding F20 in P0.5-baselines.md - the pause statistics
                  and collection counts are single samples even though five were measured, and a
                  reader who averages them from the CSV will get a different (better) number than
                  the checkpoint publishes.

Usage:
  python rederive-statistics.py [--checkpoint <json>] [--csv <csv>]
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import pathlib
import sys

# Averaged across valid invocations by Aggregator.MeanAcross / Stats.Mean.
MEAN_FIELDS = [
    "operationsPerSecond",
    "latencyP50Ms",
    "latencyP99Ms",
    "latencyP999Ms",
    "latencyP9999Ms",
    "latencyMaxMs",
    "serviceTimeP99Ms",
    "lateFraction",
    "dispatchLagP99Ms",
]

# Copied from one invocation's report by Aggregator.PopulateFromReport.
COPY_FIELDS = [
    "pauseAverageMs",
    "pauseP99Ms",
    "pauseMaxMs",
    "gen0Collections",
    "gen1Collections",
    "gen2Collections",
    "inducedCollections",
    "workingSetMb",
    "committedMb",
]

TOLERANCE = 1e-6


def close(a: float, b: float) -> bool:
    """Whether two figures agree once publication rounding is allowed for.

    The CSV carries values rounded for publication - four decimals for latencies, six for rates - so
    the mean of the CSV column and the mean the aggregator computed from unrounded reports differ in
    the last place. The tolerance is wide enough for that and nothing else: the defect this script
    found published 0.3498 ms where the mean was 0.7668 ms.
    """
    return abs(a - b) <= max(1e-4, TOLERANCE * max(abs(a), abs(b)))


def relative_difference(a: float, b: float) -> float:
    scale = max(abs(a), abs(b), 1e-12)
    return abs(a - b) / scale


def cell_key(scenario: str, collector: str, heap_factor, phase: str) -> tuple:
    """Identity of a cell, with the heap factor as a number.

    Joining on the *text* of the heap factor is how the first version of this script lost 83 of 120
    cells: the checkpoint writes 2.0 and the CSV writes 2, which are the same cell and different
    strings. It reported the loss rather than hiding it, which is the only reason it was noticed.

    The fourth component is the measurement *phase*, and it is deliberately not the CSV's ``mode``
    column. ``mode`` is the discipline the invocation was driven with, and for a latency-primary
    scenario that is "latency" in both phases - so joining on it silently merged
    ``aspnet-request-load``'s five throughput-phase invocations with its five latency-phase ones and
    compared published values against the mean of ten. The phase is carried by ``runId``.
    """
    return (scenario, collector, round(float(heap_factor), 4), phase)


def phase_of(run_id: str) -> str:
    return "latency" if run_id.endswith(".latency") else "throughput"


def parse(value: str) -> float | None:
    if value is None or value == "":
        return None
    return float(value)


def main() -> int:
    here = pathlib.Path(__file__).resolve().parent
    root = here.parent
    parser = argparse.ArgumentParser()
    parser.add_argument("--checkpoint", default=str(root / "p0-5-baselines-s2.json"))
    parser.add_argument("--csv", default=str(root / "raw" / "p0-5-baselines-s2-invocations.csv"))
    args = parser.parse_args()

    checkpoint_path = pathlib.Path(args.checkpoint)
    csv_path = pathlib.Path(args.csv)
    print(f"checkpoint : {checkpoint_path}")
    print(f"raw csv    : {csv_path}")
    if not checkpoint_path.exists() or not csv_path.exists():
        print("FAIL: an input is missing; nothing was compared")
        return 1

    document = json.loads(checkpoint_path.read_text(encoding="utf-8"))
    published = [r for c in document.get("checkpoints", []) for r in c.get("results", [])]

    with csv_path.open(newline="", encoding="utf-8-sig") as handle:
        rows = list(csv.DictReader(handle))

    # A cell is identified the same way in both files. Building the key from the CSV's own columns
    # rather than from the record's notes keeps the join independent of how the runner labels runs.
    by_cell: dict[tuple, list[dict]] = {}
    for row in rows:
        key = cell_key(row["scenario"], row["collector"], row["heapFactor"], phase_of(row["runId"]))
        by_cell.setdefault(key, []).append(row)

    print(f"\npublished records : {len(published)}")
    print(f"csv rows          : {len(rows)}")
    print(f"csv cells         : {len(by_cell)}")

    # The record's own phase is recoverable only from the run id the runner embedded in its notes.
    mode_of = {}
    for record in published:
        notes = record.get("notes") or ""
        mode_of[id(record)] = "latency" if ".latency" in notes else "throughput"

    checked_mean = 0
    checked_copy = 0
    mismatches: list[str] = []
    unmatched_cells = 0
    unmatched_explained: list[str] = []
    mean_differs_from_copy = 0

    for record in published:
        key = cell_key(record["scenario"], record["collector"], record["heapFactor"], mode_of[id(record)])
        candidates = by_cell.get(key)
        if candidates is None:
            # A cell with no valid invocation writes no invocation rows, so its absence from the raw
            # data is what the record already says rather than a join that failed. Counting the two
            # separately is the difference between "this cell crashed" and "this cell is unverifiable".
            if not record.get("valid", True) and record.get("invocations", 0) == 0:
                unmatched_explained.append(f"{key}: {record.get('invalidReason')}")
            else:
                unmatched_cells += 1
            continue

        valid_rows = [r for r in candidates if r["valid"].strip().lower() == "true"]
        if not valid_rows:
            continue

        for field in MEAN_FIELDS:
            values = [parse(r.get(field, "")) for r in valid_rows]
            values = [v for v in values if v is not None]
            expected = record.get(field)
            if expected is None:
                if values:
                    mismatches.append(
                        f"{key}: {field} published null but {len(values)} invocation(s) reported it"
                    )
                continue
            if not values:
                mismatches.append(f"{key}: {field} published {expected} but no invocation reported it")
                continue
            recomputed = sum(values) / len(values)
            checked_mean += 1
            if not close(recomputed, float(expected)):
                mismatches.append(
                    f"{key}: {field} published {expected!r} but the mean of "
                    f"{len(values)} invocation(s) is {recomputed!r}"
                )

        for field in COPY_FIELDS:
            expected = record.get(field)
            if expected is None:
                continue
            values = [parse(r.get(field, "")) for r in valid_rows]
            values = [v for v in values if v is not None]
            if not values:
                continue
            checked_copy += 1
            if not any(close(v, float(expected)) for v in values):
                mismatches.append(
                    f"{key}: {field} published {expected!r} which is none of the "
                    f"{len(values)} invocation value(s) {values!r}"
                )
            mean_value = sum(values) / len(values)
            if len(values) > 1 and not close(mean_value, float(expected)):
                mean_differs_from_copy += 1

    print(f"\nmean-field comparisons  : {checked_mean}")
    print(f"copy-field comparisons  : {checked_copy}")
    print(f"cells with no csv rows, explained by the record itself : {len(unmatched_explained)}")
    for message in unmatched_explained:
        print(f"    {message}")
    print(f"cells in json with no matching csv rows and no explanation : {unmatched_cells}")
    print(
        f"copy fields whose published value differs from the mean of the same cell's "
        f"invocations : {mean_differs_from_copy}   <- F20, expected to be large and non-zero"
    )
    print(f"mismatches : {len(mismatches)}")
    for message in mismatches[:20]:
        print(f"  {message}")
    if len(mismatches) > 20:
        print(f"  ... and {len(mismatches) - 20} more")

    if checked_mean == 0:
        print("\nFAIL: no mean field was compared; the join produced nothing")
        return 1
    if unmatched_cells:
        print(f"\nFAIL: {unmatched_cells} published cell(s) could not be joined to the raw CSV")
        return 1
    if mismatches:
        print("\nFAIL: the published statistics do not re-derive from the raw data")
        return 1

    print("\nPASS: every published statistic re-derives from the per-invocation CSV")
    return 0


if __name__ == "__main__":
    sys.exit(main())
