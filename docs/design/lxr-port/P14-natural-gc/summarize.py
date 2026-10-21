#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Aggregate a natural-collection matrix: medians, ratios to the stock collector, validity checks."""

import argparse
import json
import statistics
import sys
from collections import defaultdict

STOCK = {"java": "java-g1", "net": "net-wks"}
ORDER = ["java-lxr", "java-g1", "java-parallel", "java-serial", "java-zgc",
         "net-lxr-nonmoving", "net-lxr-nursery", "net-lxr-mature", "net-wks", "net-server"]
LABEL = {
    "java-lxr": "Java LXR", "java-g1": "Java G1", "java-parallel": "Java Parallel", "java-serial": "Java Serial",
    "java-zgc": "Java ZGC", "net-lxr-nonmoving": "Our port, no moving", "net-lxr-nursery": "Our port, move young",
    "net-lxr-mature": "Our port, move all", "net-wks": ".NET Workstation", "net-server": ".NET Server",
}


def load(path):
    return [json.loads(line) for line in open(path, encoding="utf-8")]


def runtime(collector):
    return "java" if collector.startswith("java-") else "net"


def aggregate(rows):
    cells = defaultdict(list)
    for row in rows:
        cells[(row["workload"], row["factor"], row["collector"])].append(row)
    result = {}
    for key, runs in cells.items():
        ok = [r for r in runs if r.get("elapsed_ms") is not None]
        item = {"runs": len(runs), "ok": len(ok), "heap_mib": runs[0]["heap_mib"]}
        if ok:
            def med(name):
                return statistics.median(r[name] for r in ok if name in r) if any(name in r for r in ok) else None
            item.update(
                elapsed_ms=med("elapsed_ms"), cpu_ms=med("cpu_ms"), gc_count=med("gc_count"), gc_ms=med("gc_ms"),
                hiccup_p99_us=med("hiccup_p99_us"), hiccup_max_us=max(r["hiccup_max_us"] for r in ok),
                req_p99_us=med("req_p99_us"), peak_rss_mb=med("peak_rss_kb") / 1024.0,
                lxr_full=med("lxr_full"), lxr_pauses=med("lxr_pauses"),
                lxr_initial_mark=med("lxr_initial_mark"), lxr_final_mark=med("lxr_final_mark"),
                elapsed_min=min(r["elapsed_ms"] for r in ok), elapsed_max=max(r["elapsed_ms"] for r in ok),
                checksum=sorted({r["checksum"] for r in ok}))
        else:
            item["failure"] = (runs[0].get("stderr_tail") or "")[-160:]
        result[key] = item
    return result


def validate(result):
    """Every successful run of one workload must compute the same checksum."""
    problems = []
    seen = {}
    for (workload, factor, collector), item in result.items():
        for checksum in item.get("checksum", []):
            seen.setdefault(workload, set()).add(checksum)
    for workload, values in seen.items():
        if len(values) != 1:
            problems.append(f"{workload}: checksums differ {sorted(values)}")
    return problems


def ratios(result):
    out = {}
    for (workload, factor, collector), item in result.items():
        if "elapsed_ms" not in item:
            continue
        stock = result.get((workload, factor, STOCK[runtime(collector)]))
        if stock and "elapsed_ms" in stock:
            out[(workload, factor, collector)] = {
                "elapsed": item["elapsed_ms"] / stock["elapsed_ms"], "cpu": item["cpu_ms"] / stock["cpu_ms"]}
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("runs")
    parser.add_argument("--json")
    args = parser.parse_args()
    rows = load(args.runs)
    result = aggregate(rows)
    ratio = ratios(result)
    problems = validate(result)
    workloads = sorted({k[0] for k in result})
    factors = sorted({k[1] for k in result})
    for workload in workloads:
        for factor in factors:
            print(f"\n== {workload} heap x{factor} (min-heap multiple)")
            print(f"{'collector':22} {'heap':>5} {'ok':>4} {'wall ms':>9} {'x stock':>8} {'cpu ms':>9} {'x stock':>8} "
                  f"{'gcs':>5} {'pause ms':>9} {'hic99 us':>9} {'hicMax ms':>10} {'rss MB':>7}  LXR pauses (full/init/final)")
            for collector in ORDER:
                item = result.get((workload, factor, collector))
                if item is None:
                    continue
                if "elapsed_ms" not in item:
                    print(f"{LABEL[collector]:22} {item['heap_mib']:>4}M {item['ok']}/{item['runs']}  FAILED: {item['failure']!r}")
                    continue
                r = ratio.get((workload, factor, collector), {})
                kinds = ""
                if item.get("lxr_pauses") is not None:
                    kinds = f"{item['lxr_pauses']:.0f} ({item['lxr_full']:.0f}/{item['lxr_initial_mark']:.0f}/{item['lxr_final_mark']:.0f})"
                print(f"{LABEL[collector]:22} {item['heap_mib']:>4}M {item['ok']}/{item['runs']:<2} {item['elapsed_ms']:9.0f} "
                      f"{r.get('elapsed', float('nan')):8.2f} {item['cpu_ms']:9.0f} {r.get('cpu', float('nan')):8.2f} "
                      f"{item['gc_count']:5.0f} {item['gc_ms']:9.0f} {item['hiccup_p99_us']:9.0f} "
                      f"{item['hiccup_max_us'] / 1000:10.1f} {item['peak_rss_mb']:7.0f}  {kinds}")
    print("\nvalidity:", "OK, one checksum per workload" if not problems else problems)
    if args.json:
        flat = [dict(workload=k[0], factor=k[1], collector=k[2], **v) for k, v in sorted(result.items())]
        for entry in flat:
            entry.update(ratio.get((entry["workload"], entry["factor"], entry["collector"]), {}))
        with open(args.json, "w", encoding="utf-8") as stream:
            json.dump({"cells": flat, "problems": problems}, stream, indent=1)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
