#!/usr/bin/env python3
"""Summarize P0.1 HEAD-LXR and stock-collector DaCapo/Hiccup TSV files."""

from __future__ import annotations

import argparse
import csv
import statistics
from collections import Counter, defaultdict
from pathlib import Path


BENCHMARKS = ("antlr", "fop", "luindex", "lusearch", "pmd", "hsqldb", "eclipse", "xalan")
HEAPS = (500, 2000)
COLLECTORS = ("lxr-head", "g1", "parallel", "serial", "zgc")
INVOCATIONS = 3


def parse_key_values(values: list[str]) -> dict[str, str]:
    result: dict[str, str] = {}
    for value in values:
        if "=" in value:
            key, item = value.split("=", 1)
            result[key] = item
    return result


def read_lxr(path: Path) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    with path.open(newline="", encoding="utf-8") as stream:
        for raw in csv.reader(stream, delimiter="\t"):
            if not raw or raw[0] != "head":
                continue
            hiccup = parse_key_values(raw[7:])
            rows.append(
                {
                    "collector": "lxr-head",
                    "bench": raw[2],
                    "heapMB": raw[3],
                    "invocation": raw[4],
                    "status": raw[5],
                    "msec": raw[6],
                    **hiccup,
                }
            )
    return rows


def read_stock(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8") as stream:
        reader = csv.DictReader(stream, delimiter="\t")
        expected = (
            "collector",
            "bench",
            "heapMB",
            "invocation",
            "status",
            "msec",
            "samples",
            "mean",
            "p50",
            "p90",
            "p99",
            "p999",
            "max",
            "exit",
            "wallSeconds",
        )
        if tuple(reader.fieldnames or ()) != expected:
            raise ValueError(f"stock header differs: {reader.fieldnames}")
        rows = list(reader)
    for row in rows:
        if None in row:
            raise ValueError("stock row contains undeclared columns")
        for key in ("samples", "mean", "p50", "p90", "p99", "p999", "max"):
            value = row[key]
            if "=" in value:
                label, item = value.split("=", 1)
                if label != key:
                    raise ValueError(f"stock column label differs: expected {key}, found {label}")
                row[key] = item
    return rows


def validate(rows: list[dict[str, str]]) -> None:
    counts = Counter((row["collector"], row["bench"], int(row["heapMB"])) for row in rows)
    expected = {(collector, bench, heap) for collector in COLLECTORS for bench in BENCHMARKS for heap in HEAPS}
    if set(counts) != expected:
        missing = sorted(expected - set(counts))
        extra = sorted(set(counts) - expected)
        raise ValueError(f"cell set differs: missing={missing}, extra={extra}")
    bad_counts = {cell: count for cell, count in counts.items() if count != INVOCATIONS}
    if bad_counts:
        raise ValueError(f"cell invocation counts differ: {bad_counts}")


def format_float(value: float) -> str:
    return f"{value:.3f}".rstrip("0").rstrip(".")


def summarize(rows: list[dict[str, str]], output: Path) -> None:
    grouped: dict[tuple[str, str, int], list[dict[str, str]]] = defaultdict(list)
    for row in rows:
        grouped[(row["collector"], row["bench"], int(row["heapMB"]))].append(row)

    output.mkdir(parents=True, exist_ok=True)
    with (output / "pass-summary.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream, lineterminator="\n")
        writer.writerow(("collector", "passed", "total", "result"))
        for collector in COLLECTORS:
            collector_rows = [row for row in rows if row["collector"] == collector]
            passed = sum(row["status"] == "PASSED" for row in collector_rows)
            writer.writerow((collector, passed, len(collector_rows), "PASS" if passed == len(collector_rows) else "FAIL"))

    with (output / "throughput-summary.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream, lineterminator="\n")
        writer.writerow(("benchmark", "heap_mb", *COLLECTORS))
        for bench in BENCHMARKS:
            for heap in HEAPS:
                values: list[str] = []
                for collector in COLLECTORS:
                    cell = grouped[(collector, bench, heap)]
                    if any(row["status"] != "PASSED" for row in cell):
                        values.append("FAIL")
                    else:
                        values.append(format_float(statistics.median(float(row["msec"]) for row in cell)))
                writer.writerow((bench, heap, *values))

    with (output / "pause-summary.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream, lineterminator="\n")
        writer.writerow(
            (
                "collector",
                "benchmark",
                "heap_mb",
                "p99_min_ms",
                "p99_max_ms",
                "p999_min_ms",
                "p999_max_ms",
                "max_min_ms",
                "max_max_ms",
            )
        )
        for collector in COLLECTORS:
            for bench in BENCHMARKS:
                for heap in HEAPS:
                    cell = grouped[(collector, bench, heap)]
                    if any(row["status"] != "PASSED" for row in cell):
                        writer.writerow((collector, bench, heap, "FAIL", "FAIL", "FAIL", "FAIL", "FAIL", "FAIL"))
                        continue
                    p99 = [float(row["p99"]) for row in cell]
                    p999 = [float(row["p999"]) for row in cell]
                    maximum = [float(row["max"]) for row in cell]
                    writer.writerow(
                        (
                            collector,
                            bench,
                            heap,
                            format_float(min(p99)),
                            format_float(max(p99)),
                            format_float(min(p999)),
                            format_float(max(p999)),
                            format_float(min(maximum)),
                            format_float(max(maximum)),
                        )
                    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("lxr_tsv", type=Path)
    parser.add_argument("stock_tsv", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    rows = read_lxr(args.lxr_tsv) + read_stock(args.stock_tsv)
    validate(rows)
    summarize(rows, args.output)
    print(f"PASS: summarized {len(rows)} rows into {args.output}")


if __name__ == "__main__":
    main()
