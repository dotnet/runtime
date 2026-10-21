#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Bounded, predeclared correctness-fixed lifecycle/full-cycle comparisons."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import statistics
import subprocess
import time


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--changed", type=Path, required=True)
    parser.add_argument("--fixture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--phase", choices=("attribution", "convergence"), required=True)
    args = parser.parse_args()
    baseline = args.baseline.resolve(strict=True)
    changed = args.changed.resolve(strict=True)
    fixture = args.fixture.resolve(strict=True)
    args.output.mkdir(parents=True, exist_ok=False)
    windows = os.name == "nt"
    corerun = "corerun.exe" if windows else "corerun"
    coreclr = "coreclr.dll" if windows else "libcoreclr.so"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    jit = "clrjit.dll" if windows else "libclrjit.so"
    identities = {"fixture": digest(fixture)}
    for name in (corerun, coreclr, jit, "System.Private.CoreLib.dll"):
        identities[name] = digest(baseline / name)
        if identities[name] != digest(changed / name):
            raise RuntimeError(f"Runtime input differs: {name}")
    modes = (("nonmoving", "0", "0"), ("nursery", "1", "0"), ("mature", "1", "1"))
    arms = [(f"{stage}-{mode}", root, "LXR", nursery, mature)
            for stage, root in (("fixed", baseline), ("current", changed))
            for mode, nursery, mature in modes]
    arms += [("WKS", changed, "WKS", None, None), ("Server", changed, "Server", None, None)]
    scenarios = ["empty", "roots", "lifetimes"]
    if args.phase == "convergence":
        scenarios += ["fragmented", "dense", "pinned", "high-survival"]
    schedule = []
    if args.phase == "attribution":
        for scenario in scenarios:
            for mode, _, _ in modes:
                fixed = next(arm for arm in arms if arm[0] == f"fixed-{mode}")
                current = next(arm for arm in arms if arm[0] == f"current-{mode}")
                schedule.extend((scenario, label, diagnostic, arm) for label, diagnostic, arm in (
                    ("A", False, current), ("fixed", False, fixed), ("B", False, current),
                    ("diagnostic", True, current), ("injected", False, current)))
            for arm in arms[-2:]:
                schedule.extend((scenario, label, False, arm) for label in ("A", "B", "injected"))
    else:
        for scenario in scenarios:
            for pair in range(2):
                order = arms[pair:] + arms[:pair]
                for label, order in ((f"A{pair}", order), (f"B{pair}", list(reversed(order)))):
                    schedule.extend((scenario, label, False, arm) for arm in order)
            for label, diagnostic in (("injected", False), ("diagnostic", True)):
                schedule.extend((scenario, label, diagnostic, arm) for arm in reversed(arms))
    declaration = {
        "phase": args.phase, "identities": identities,
        "scope": "Correctness-fixed denominator only. Exactly eight explicit blocking full requests; "
                 "fixed retained inputs/checksums. Diagnostic wall/current-thread CPU scopes overlap; "
                 "do not sum nested scopes or infer exclusive total-GC CPU. CPU clocks may be quantized. "
                 "Parent process wall includes setup/startup/teardown and is not region wall. "
                 "Native commitment, requested memory and RSS are different observations. "
                 "No retries, excluded outliers, tuning or widened targets. Built-ins characterize "
                 "same runtime, not equal-heap fairness. Injected arm adds 8x25ms sleeps.",
        "goal": "Attribution has no optimization target. Convergence target must be frozen in README "
                "after attribution and before running changed measurements.",
        "arms": [[label, str(root), category, nursery, mature,
                  digest(root / (collector if category == "LXR" else coreclr))]
                 for label, root, category, nursery, mature in arms],
        "schedule": [[scenario, label, diagnostic, arm[0]] for scenario, label, diagnostic, arm in schedule],
    }
    (args.output / "schedule.json").write_text(json.dumps(declaration, indent=2))
    base = {key: value for key, value in os.environ.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    rows = []
    for index, (scenario, label, diagnostic, arm) in enumerate(schedule):
        name, root, category, nursery, mature = arm
        child = dict(base)
        child["DOTNET_gcServer"] = "1" if category == "Server" else "0"
        module = root / (collector if category == "LXR" else coreclr)
        if category == "LXR":
            child.update(DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery,
                         DOTNET_LXREvacuateMature=mature)
        control = "injected" if label == "injected" else "normal"
        instrumentation = "diagnostic" if diagnostic else "off"
        lifecycle = scenario in ("empty", "roots", "lifetimes")
        command = [str(root / corerun), str(fixture),
                   "--p7-cost" if lifecycle else "--mature-study", scenario, category, str(module),
                   *([instrumentation, control] if lifecycle else [control, instrumentation])]
        raw = args.output / f"{index:03d}-{scenario}-{name}-{label}.log"
        binding = {"command": command, "settings": {key: value for key, value in child.items()
                                                  if key.startswith("DOTNET_")},
                   "module_sha256": digest(module), "identities": identities}
        start = time.perf_counter()
        try:
            result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
        except subprocess.TimeoutExpired as error:
            raw.write_text(json.dumps(binding) + "\n" + str(error.stdout) + "\n" + str(error.stderr) +
                           "\nexit=timeout\n")
            raise RuntimeError(f"Timeout retained: {raw}") from error
        process_wall = (time.perf_counter() - start) * 1000
        raw.write_text(json.dumps(binding) + "\n" + result.stdout + result.stderr +
                       f"\nexit={result.returncode}\nprocess_wall_ms={process_wall}\n")
        prefix = "lifecycle_cost=" if lifecycle else "mature_study="
        values = [json.loads(line[len(prefix):]) for line in result.stdout.splitlines() if line.startswith(prefix)]
        if result.returncode != 0 or len(values) != 1:
            raise RuntimeError(f"Failed process retained: {raw}")
        value = values[0]
        valid = value["module_sha256"].lower() == binding["module_sha256"] and \
            value["collector"] == category and value["server"] == (category == "Server") and \
            value["primary_full_calls"] == 8
        if category == "LXR":
            valid = valid and value["nursery" if lifecycle else "nursery_enabled"] == int(nursery) and \
                value["mature" if lifecycle else "mature_enabled"] == int(mature)
        row = {"index": index, "arm": name, "replicate": label, "semantic_valid": valid,
               "exit_code": result.returncode, "process_wall_ms": process_wall, **value}
        rows.append(row)
        (args.output / "rows.json").write_text(json.dumps(rows, indent=2))
        print(index, scenario, name, label, value["elapsed_ms"], flush=True)
        if not valid:
            raise RuntimeError(f"Input/category/module/mode mismatch retained: {raw}")
    summaries = []
    for scenario in scenarios:
        for arm in arms:
            selected = [row for row in rows if row["scenario"] == scenario and row["arm"] == arm[0]]
            normal = [row for row in selected if row["replicate"].startswith(("A", "B")) or row["replicate"] == "fixed"]
            if not normal:
                continue
            summary = {"scenario": scenario, "arm": arm[0], "normal_count": len(normal)}
            for key in ("elapsed_ms", "cpu_ms", "pause_ms", "process_wall_ms", "rss_bytes", "private_bytes"):
                summary[key] = statistics.median(row[key] for row in normal)
            summaries.append(summary)
    with (args.output / "summary.csv").open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(summaries[0]))
        writer.writeheader()
        writer.writerows(summaries)


if __name__ == "__main__":
    main()
