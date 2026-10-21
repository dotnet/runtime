#!/usr/bin/env python3
"""Verify the committed P0.1 stock-collector comparison evidence."""

from __future__ import annotations

import argparse
import csv
import hashlib
import re
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while chunk := stream.read(1024 * 1024):
            digest.update(chunk)
    return digest.hexdigest()


def compare(left: Path, right: Path) -> None:
    if left.read_bytes() != right.read_bytes():
        raise ValueError(f"derived summary differs: {left.name}")


def zip_files(archive: zipfile.ZipFile) -> set[str]:
    corrupt = archive.testzip()
    if corrupt is not None:
        raise ValueError(f"ZIP member CRC differs: {corrupt}")
    return {name for name in archive.namelist() if not name.endswith("/")}


def markdown_table(text: str, start: str, end: str) -> list[list[str]]:
    if start not in text or end not in text:
        raise ValueError(f"Markdown table boundary is missing: {start!r} / {end!r}")
    section = text.split(start, 1)[1].split(end, 1)[0]
    rows: list[list[str]] = []
    for line in section.splitlines():
        if not line.startswith("|") or line.startswith("|---"):
            continue
        rows.append([cell.strip() for cell in line.strip("|").split("|")])
    if len(rows) < 2:
        raise ValueError(f"Markdown table is empty: {start!r}")
    return rows


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repository-root", type=Path)
    args = parser.parse_args()

    repository = (
        args.repository_root.resolve()
        if args.repository_root
        else Path(__file__).resolve().parents[4]
    )
    root = repository / "docs" / "design" / "lxr-port"
    evidence = root / "P0.1-stock-baselines"
    raw = evidence / "raw"
    manifest = raw / "sha256.csv"
    document = root / "P0.1-stock-baselines.md"
    summarizer = root / "scripts" / "summarize-stock-results.py"

    with manifest.open(newline="", encoding="utf-8") as stream:
        records = list(csv.DictReader(stream))
    listed = {record["path"] for record in records}
    actual = {
        path.relative_to(raw).as_posix()
        for path in raw.rglob("*")
        if path.is_file() and path != manifest
    }
    if listed != actual:
        raise ValueError(
            f"raw file set differs: missing={sorted(listed - actual)}, extra={sorted(actual - listed)}"
        )
    for record in records:
        path = raw / record["path"]
        if int(record["length"]) != path.stat().st_size:
            raise ValueError(f"raw length differs: {record['path']}")
        if record["sha256"].lower() != sha256(path):
            raise ValueError(f"raw SHA-256 differs: {record['path']}")

    with tempfile.TemporaryDirectory(prefix="lxr-stock-") as temp:
        generated = Path(temp)
        subprocess.run(
            [
                sys.executable,
                str(summarizer),
                str(raw / "lxr-head-and-pldi-shipped-release.tsv"),
                str(raw / "stock-release.tsv"),
                str(generated),
            ],
            check=True,
        )
        for name in ("pass-summary.csv", "throughput-summary.csv", "pause-summary.csv"):
            compare(raw / name, generated / name)

    with (raw / "stock-release.tsv").open(newline="", encoding="utf-8") as stream:
        stock_rows = list(csv.DictReader(stream, delimiter="\t"))
    expected_stock_logs = {
        "flogs-final-stock-release/"
        f"{row['collector']}-{row['bench']}-{row['heapMB']}-{row['invocation']}.log"
        for row in stock_rows
    }
    expected_stock_files = expected_stock_logs | {
        "final-stock-release-driver.log",
        "final-stock-release.status",
    }
    with zipfile.ZipFile(raw / "stock-release-logs.zip") as archive:
        stock_files = zip_files(archive)
        if stock_files != expected_stock_files:
            raise ValueError(
                "stock ZIP member set differs: "
                f"missing={sorted(expected_stock_files - stock_files)}, "
                f"extra={sorted(stock_files - expected_stock_files)}"
            )
        status = archive.read("final-stock-release.status").decode("utf-8")
        if status != "exit=0\n":
            raise ValueError(f"stock driver status differs: {status!r}")
        driver = archive.read("final-stock-release-driver.log").decode("utf-8")
        if "===== done -> /root/lxr/results/final-stock-release.tsv =====" not in driver:
            raise ValueError("stock driver completion marker is missing")
        rows_by_log = {
            "flogs-final-stock-release/"
            f"{row['collector']}-{row['bench']}-{row['heapMB']}-{row['invocation']}.log": row
            for row in stock_rows
        }
        for name, row in rows_by_log.items():
            text = archive.read(name).decode("utf-8", errors="replace")
            passed = re.findall(r"PASSED in ([0-9]+) msec", text)
            if not passed or passed[-1] != row["msec"]:
                raise ValueError(f"stock log final iteration differs: {name}")
            hiccup = [line for line in text.splitlines() if line.startswith("HICCUP ")]
            if len(hiccup) != 1:
                raise ValueError(f"stock Hiccup record count differs: {name}")
            values = {}
            for token in hiccup[0].split()[2:]:
                key, value = token.split("=", 1)
                values[key] = value
            for key in ("samples", "mean", "p50", "p90", "p99", "p999", "max"):
                expected_value = row[key].split("=", 1)[-1]
                if values.get(key) != expected_value:
                    raise ValueError(f"stock log {key} differs: {name}")

    expected_lxr_logs: set[str] = set()
    with (raw / "lxr-head-and-pldi-shipped-release.tsv").open(
        newline="", encoding="utf-8"
    ) as stream:
        for row in csv.reader(stream, delimiter="\t"):
            expected_lxr_logs.add(
                f"flogs-release-shipped/{row[0]}-{row[2]}-{row[3]}-{row[4]}.log"
            )
    with zipfile.ZipFile(raw / "lxr-head-and-pldi-shipped-release-logs.zip") as archive:
        lxr_files = zip_files(archive)
    if lxr_files != expected_lxr_logs:
        raise ValueError(
            "LXR/PLDI ZIP member set differs: "
            f"missing={sorted(expected_lxr_logs - lxr_files)}, "
            f"extra={sorted(lxr_files - expected_lxr_logs)}"
        )

    text = document.read_text(encoding="utf-8")
    normalized_text = " ".join(text.split())
    required = (
        "not randomized, interleaved, or run under a quiesced host",
        "No run crashed, timed out, or failed DaCapo validation.",
        "hsqldb",
        "System.gc()",
        "observations of this non-interleaved run, not claimed speedups",
    )
    for phrase in required:
        if phrase not in normalized_text:
            raise ValueError(f"required limitation or result is missing: {phrase}")

    with (raw / "throughput-summary.csv").open(newline="", encoding="utf-8") as stream:
        throughput = list(csv.DictReader(stream))
    for heap, start, end in (
        ("500", "### 4.1 500 MB", "### 4.2 2000 MB"),
        ("2000", "### 4.2 2000 MB", "### 4.3 Descriptive ratios"),
    ):
        observed = markdown_table(text, start, end)
        expected = [["benchmark", "LXR HEAD", "G1", "Parallel", "Serial", "ZGC"]]
        expected.extend(
            [
                row["benchmark"],
                row["lxr-head"],
                row["g1"],
                row["parallel"],
                row["serial"],
                row["zgc"],
            ]
            for row in throughput
            if row["heap_mb"] == heap
        )
        if observed != expected:
            raise ValueError(f"Markdown throughput table differs at {heap} MB")

    with (raw / "pause-summary.csv").open(newline="", encoding="utf-8") as stream:
        pauses = list(csv.DictReader(stream))
    collectors = ("lxr-head", "g1", "parallel", "serial", "zgc")
    headings = ("LXR HEAD", "G1", "Parallel", "Serial", "ZGC")
    for heap, start, end in (
        ("500", "### 5.1 500 MB", "### 5.2 2000 MB"),
        ("2000", "### 5.2 2000 MB", "## 6. Reading the result"),
    ):
        observed = markdown_table(text, start, end)
        expected = [["workload", *headings]]
        for benchmark in ("antlr", "fop", "luindex", "lusearch", "pmd", "hsqldb", "eclipse", "xalan"):
            row = [benchmark]
            for collector in collectors:
                item = next(
                    value
                    for value in pauses
                    if value["collector"] == collector
                    and value["benchmark"] == benchmark
                    and value["heap_mb"] == heap
                )
                row.append(
                    f"{item['p99_min_ms']}–{item['p99_max_ms']} / "
                    f"{item['p999_min_ms']}–{item['p999_max_ms']} / "
                    f"{item['max_min_ms']}–{item['max_max_ms']}"
                )
            expected.append(row)
        if observed != expected:
            raise ValueError(f"Markdown pause table differs at {heap} MB")

    print(
        f"PASS: {len(records)} raw files, 240 summarized rows, "
        f"{len(expected_stock_logs)} stock logs, {len(expected_lxr_logs)} LXR/PLDI logs"
    )


if __name__ == "__main__":
    main()
