#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Table of the FinalizerLag runs (lag-matrix.sh): per configuration, mode and build, medians over the finished runs.

usage: lag-table.py <lag.jsonl>
Columns: finished runs of all runs, finalizers that had run when the producer ended, delay between the last use of an
object and its finalizer (mean, p50 and p95 upper bucket edge, max; ms), peak RSS, tracing pauses (initial marks plus full
pauses), natural collections, elapsed time, and the finalizers that were lost (must be 0).
"""
import json
import statistics
import sys
from collections import defaultdict

cells = defaultdict(list)
for line in open(sys.argv[1], encoding="utf-8"):
    row = json.loads(line)
    cells[(row["config"], row["mode"], row["build"])].append(row["result"])

configs = []
for config, _, _ in cells:
    if config not in configs:
        configs.append(config)
modes = ("nonmoving", "nursery", "mature")
builds = ("acc", "s5b", "s6")
names = {"acc": "accepted", "s5b": "S5b tip", "s6": "S6"}


def med(runs, field):
    values = [r[field] for r in runs if field in r]
    return statistics.median(values) if values else float("nan")


print("| configuration | mode | build | finished | finalized at end | delay mean ms | p50 ms | p95 ms | max ms | peak RSS MiB | tracing pauses | collections | elapsed ms | lost |")
print("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
for config in configs:
    for mode in modes:
        for build in builds:
            runs = cells.get((config, mode, build), [])
            if not runs:
                continue
            ok = [r for r in runs if "elapsed_ms" in r]
            if not ok:
                print(f"| {config} | {mode} | {names[build]} | 0 of {len(runs)} | fail | | | | | | | | | |")
                continue
            lost = "n/a" if ok[0].get("plain") else max(r.get("lost", 0) for r in ok)
            print(f"| {config} | {mode} | {names[build]} | {len(ok)} of {len(runs)} | {med(ok, 'finalized_at_end'):.0f} | "
                  f"{med(ok, 'delay_mean_ms'):.0f} | {med(ok, 'delay_p50_ms'):.0f} | {med(ok, 'delay_p95_ms'):.0f} | "
                  f"{max(r['delay_max_ms'] for r in ok):.0f} | {med(ok, 'peak_rss_mib'):.0f} | {med(ok, 'tracing_pauses'):.0f} | "
                  f"{med(ok, 'collections_gen0'):.0f} | {med(ok, 'elapsed_ms'):.0f} | {lost} |")
