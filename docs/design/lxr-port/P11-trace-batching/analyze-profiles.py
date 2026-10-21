#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Analyze actual profile denominators; never convert them into body GC CPU."""

import argparse
from collections import Counter, defaultdict
import importlib.util
import json
from pathlib import Path
import re


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--os", choices=("windows", "linux"), required=True)
    args = parser.parse_args()
    root = args.root.resolve()
    spec = importlib.util.spec_from_file_location("profile_parser", root / "profile-parser.py")
    helpers = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helpers)
    directory = root / ("profile-linux-fp" if args.os == "linux" else "profile-windows-census")
    result = {}
    paths = sorted(directory.glob("*.data.stacks.txt" if args.os == "linux" else "*.samples.json"))
    for path in paths:
        top = Counter()
        categories = Counter()
        ancestors = Counter()
        threads = defaultdict(Counter)
        coverage = Counter()
        if args.os == "linux":
            records = helpers.linux_stacks(path)
        else:
            data = json.loads(path.read_text())
            records = [{"tid": row["tid"], "stack": [{"symbol": re.sub(r"\+0x[0-9a-f]+$", "", item)}
                                                     for item in row["stack"]]} for row in data["samples"]]
        if not records:
            raise RuntimeError(f"No records parsed from {path}")
        for record in records:
            stack = [frame["symbol"] for frame in record["stack"]]
            if not stack:
                coverage["empty"] += 1
                continue
            leaf = stack[0]
            top[leaf] += 1
            threads[record["tid"]][leaf] += 1
            if leaf.startswith(("SideMetadataManager::", "ReferenceCountTable::")):
                categories["metadata_rc_leaf"] += 1
            elif leaf in ("LxrReferenceCounting::NewWork", "LxrReferenceCounting::DeleteWork"):
                categories["packet_create_delete_leaf"] += 1
            elif any(word in leaf for word in ("malloc", "free", "operator new", "operator delete", "_int_")):
                categories["native_allocation_leaf"] += 1
            elif any(word in leaf for word in ("TryCopy", "FixForwardingReaders", "ResolveForwarded", "GetObjectExtent", "RememberMature", "ReplayMature")):
                categories["movement_reader_extent_leaf"] += 1
            elif leaf == "[unknown]" or leaf.startswith("0x"):
                categories["unresolved_leaf"] += 1
            else:
                categories["other_leaf"] += 1
            if "LxrWorkScheduler::Impl::WorkerLoop" in stack:
                coverage["native_worker_stack"] += 1
            if "LxrGcHeap::ExecuteScan" in stack or "LxrGcHeap::ScanOnGcThread" in stack:
                coverage["ee_executor_stack"] += 1
            for symbol in set(stack):
                ancestors[symbol] += 1
        denominator = len(records)
        entry = dict(n=denominator, nonempty=sum(top.values()), coverage=coverage,
                     self=helpers.percentage(top, denominator),
                     categories=helpers.percentage(categories, denominator),
                     inclusive_not_additive=helpers.percentage(ancestors, denominator),
                     threads={str(tid): dict(n=sum(values.values()), top=helpers.percentage(values, sum(values.values()))[:8])
                              for tid, values in threads.items()})
        if args.os == "windows":
            entry.update(limits=data["limits"], duration_s=data["duration_s"], failures=data["failures"],
                         cpu_ms_by_tid=data["cpu_ms_by_tid"])
        else:
            report = Path(str(path).replace(".stacks.txt", ".report.txt")).read_text(errors="replace")
            lost = re.search(r"Total Lost Samples:\s*(\d+)", report)
            if lost is None:
                raise RuntimeError("Missing explicit lost-sample count")
            entry["lost"] = int(lost[1])
            entry["limits"] = "Whole-process user CPU, native frame-pointer call chains; not the eight-call-only CPU interval. Missing managed/VM/JIT/libc chains are not filled or reassigned."
        result[path.name] = entry
    if len(result) != 6:
        raise RuntimeError("Expected all six matched native profiles")
    (root / f"profile-analysis-{args.os}.json").write_text(json.dumps(result, indent=2) + "\n")
    print("Analyzed", len(result), args.os, "profiles")


if __name__ == "__main__":
    main()
