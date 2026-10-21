#!/usr/bin/env python3
"""Regenerate the section 6.4 within-cell variance table from the raw invocation CSV.

Section 6.4 was published without a generator. It was therefore reproducible only by
reverse-engineering which partition produced it, and the coordinator's independent recomputation
partitioned by the `mode` COLUMN, got 53 throughput cells at 1.792% instead of 59 at 1.597%, and was
one step from reporting the table as unreproducible. The table was right; the probe was wrong.

The partition is by PHASE DERIVED FROM `runId`, not by the `mode` column. Those two disagree for
`aspnet-request-load`, which is latency-primary (finding F22): it runs open-loop in both phases, so
its rows inside the throughput run carry `mode=latency` while belonging to the throughput pass. That
is exactly 30 rows / 6 cells, which is the 53 + 6 = 59 the coordinator observed.

A cell is (scenario, collector, heapFactor, phase). CV is the sample coefficient of variation over
the valid invocations of that cell. Emits the markdown table and, with --check, compares against the
document.
"""
import argparse
import csv
import os
import statistics
import sys

FLOOR_PERCENT = 8.35

# The metric each phase is judged on. Throughput cells are judged on throughput, latency cells on
# tail latency; a throughput row's p99 is not a latency measurement of the same thing.
PHASE_METRIC = {"throughput": "operationsPerSecond", "latency": "latencyP99Ms"}


def phase_of(run_id):
    """Phase comes from the run, not the row's mode column. See module docstring."""
    if run_id.endswith(".throughput"):
        return "throughput"
    if run_id.endswith(".latency"):
        return "latency"
    return None


def load_cells(csv_path):
    cells = {}
    skipped_invalid = 0
    skipped_blank = 0
    total = 0
    with open(csv_path, newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            total += 1
            phase = phase_of(row["runId"])
            if phase is None:
                continue
            if row["valid"].strip().lower() != "true":
                skipped_invalid += 1
                continue
            raw = row[PHASE_METRIC[phase]].strip()
            if not raw:
                skipped_blank += 1
                continue
            key = (row["scenario"], row["collector"], row["heapFactor"], phase)
            cells.setdefault(key, []).append(float(raw))
    return cells, total, skipped_invalid, skipped_blank


def cv_percent(values):
    if len(values) < 2:
        return None
    mean = statistics.fmean(values)
    if mean == 0:
        return None
    return statistics.stdev(values) / mean * 100.0


def summarise(cells, phase):
    cvs = []
    for (_, _, _, cell_phase), values in cells.items():
        if cell_phase != phase:
            continue
        cv = cv_percent(values)
        if cv is not None:
            cvs.append(cv)
    if not cvs:
        return None
    above = [c for c in cvs if c > FLOOR_PERCENT]
    return {
        "metric": PHASE_METRIC[phase],
        "cells": len(cvs),
        "median": statistics.median(cvs),
        "max": max(cvs),
        "above": len(above),
    }


def main():
    parser = argparse.ArgumentParser()
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(here)
    parser.add_argument("--csv", default=os.path.join(root, "raw", "p0-5-baselines-s2-invocations.csv"))
    parser.add_argument("--doc", default=os.path.join(os.path.dirname(root), "P0.5-baselines.md"))
    parser.add_argument("--check", action="store_true",
                        help="compare the derived table against the document and exit non-zero on disagreement")
    args = parser.parse_args()

    if not os.path.isfile(args.csv):
        print("FAIL missing CSV: %s" % args.csv)
        return 1

    cells, total, skipped_invalid, skipped_blank = load_cells(args.csv)
    # Rule 26: print the accounting, not just the verdict.
    print("rows read              : %d" % total)
    print("rows dropped, invalid  : %d" % skipped_invalid)
    print("rows dropped, no value : %d" % skipped_blank)
    print("partition              : (scenario, collector, heapFactor, phase-from-runId)")

    summaries = [summarise(cells, "throughput"), summarise(cells, "latency")]
    if any(s is None for s in summaries):
        print("FAIL a phase produced no cells")
        return 1

    print()
    print("| metric | cells | median within-cell CV | max | cells above the %.2f%% floor |" % FLOOR_PERCENT)
    print("|---|---|---|---|---|")
    for s in summaries:
        print("| `%s` | %d | **%.2f%%** | %.2f%% | **%d of %d** |"
              % (s["metric"], s["cells"], s["median"], s["max"], s["above"], s["cells"]))

    if not args.check:
        return 0

    if not os.path.isfile(args.doc):
        print("FAIL missing document: %s" % args.doc)
        return 1
    with open(args.doc, encoding="utf-8") as handle:
        doc = handle.read()

    print()
    failures = 0
    for s in summaries:
        # Each derived figure must appear in the document's row for that metric. The expected value
        # comes from the CSV, never from a constant here, so this cannot pass by agreeing with itself.
        row = None
        for line in doc.splitlines():
            if line.startswith("| `%s` |" % s["metric"]):
                row = line
                break
        if row is None:
            print("FAIL  no section 6.4 row for `%s` in the document" % s["metric"])
            failures += 1
            continue
        expected = ["| %d |" % s["cells"], "**%.2f%%**" % s["median"],
                    "%.2f%%" % s["max"], "**%d of %d**" % (s["above"], s["cells"])]
        missing = [e for e in expected if e not in row]
        print("  derived %-20s cells=%d median=%.2f%% max=%.2f%% above=%d"
              % (s["metric"], s["cells"], s["median"], s["max"], s["above"]))
        print("  document row         %s" % row.strip())
        if missing:
            print("  FAIL  document row lacks: %s" % " ".join(missing))
            failures += 1
        else:
            print("  ok    all 4 derived figures present in the document row")

    if failures:
        print("RESULT: FAIL (%d of 2 rows disagree with the CSV)" % failures)
        return 1
    print("RESULT: PASS (section 6.4 re-derives from %s)" % os.path.basename(args.csv))
    return 0


if __name__ == "__main__":
    sys.exit(main())
