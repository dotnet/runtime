#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Fixed inputs, interleaved A/B and injected controls; every process is retained without retries."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import statistics
import subprocess


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def environment(source):
    return {key: value for key, value in source.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--changed", type=Path, required=True)
    parser.add_argument("--test", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--suite", choices=("survival", "growth"), default="survival")
    args = parser.parse_args()
    baseline = args.baseline.resolve(strict=True)
    changed = args.changed.resolve(strict=True)
    fixture = args.test.resolve(strict=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    if args.output.exists():
        raise RuntimeError("A study output cannot overwrite a previous process record.")
    windows = os.name == "nt"
    corerun = "corerun.exe" if windows else "corerun"
    coreclr = "coreclr.dll" if windows else "libcoreclr.so"
    jit = "clrjit.dll" if windows else "libclrjit.so"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    identities = {"fixture_sha256": digest(fixture)}
    for name in (corerun, coreclr, jit, "System.Private.CoreLib.dll"):
        before, after = digest(baseline / name), digest(changed / name)
        if before != after:
            raise RuntimeError(f"Runtime input mismatch: {name}")
        identities[name] = before
    arms = [
        ("compact", baseline, "LXR", {}),
        ("nursery-off", changed, "LXR", {"DOTNET_LXREvacuateNursery": "0"}),
        ("nursery-on", changed, "LXR", {"DOTNET_LXREvacuateNursery": "1"}),
        ("WKS", changed, "WKS", {}),
        ("Server", changed, "Server", {}),
    ]
    base = environment(os.environ)
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    schedule = []
    scenarios = ("growth",) if args.suite == "growth" else (
        "low-ref", "high-ref", "low-byte", "high-byte", "pinned")
    for scenario in scenarios:
        for round_index in range(3):
            order = arms[round_index:] + arms[:round_index]
            for replicate in ("A", "B", "injected"):
                for arm in reversed(order) if replicate == "B" else order:
                    schedule.append((scenario, round_index, replicate, arm))
    args.output.with_suffix(".schedule.json").write_text(json.dumps({
        "identities": identities,
        "arms": [[label, str(root), category, settings, digest(root / collector)]
                 for label, root, category, settings in arms],
        "scope": ("Default-recycling growth characterization; fixed1240-array retention/reclaim input. "
                  if args.suite == "growth" else
                  "Fresh-source characterization; NoMutatorLineRecycling=1 for all LXR arms. "
                 "Eight measured full requests, 32 allocation rounds, 200ms nominal injected sleeps. "
                 ) +
                 "Whole-process CPU/RSS/private, not exclusive GC CPU or production tail latency. "
                 "No retries or effect-size threshold. Six normal samples per arm/scenario.",
        "schedule": [[scenario, round_index, replicate, arm[0]]
                     for scenario, round_index, replicate, arm in schedule],
    }, indent=2), encoding="utf-8")
    rows = []
    with args.output.open("w", newline="", encoding="utf-8") as output:
        writer = None
        for index, (scenario, round_index, replicate, arm) in enumerate(schedule):
            label, root, category, settings = arm
            child = dict(base)
            child.update(settings)
            child["DOTNET_gcServer"] = "1" if category == "Server" else "0"
            module = root / (collector if category == "LXR" else coreclr)
            if category == "LXR":
                child["DOTNET_GCPath"] = str(module)
                if args.suite == "survival":
                    child["DOTNET_LXRNoMutatorLineRecycling"] = "1"
            command = [str(root / corerun), str(fixture),
                       "--vertical" if args.suite == "growth" else "--nursery-study", scenario,
                       category, str(module), "injected" if replicate == "injected" else "normal"]
            raw = args.output.with_name(f"{args.output.stem}-{index:03d}-{scenario}-{label}-{replicate}.log")
            record = {"command": command, "settings": {key: value for key, value in child.items()
                                                      if key.upper().startswith("DOTNET_")},
                      "identities": identities, "module_sha256": digest(module)}
            try:
                result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
            except subprocess.TimeoutExpired as error:
                raw.write_text(json.dumps(record) + "\n" + str(error.stdout) + "\n" +
                               str(error.stderr) + "\nexit=timeout\n", encoding="utf-8")
                raise RuntimeError(f"Timeout retained: {raw}") from error
            raw.write_text(json.dumps(record) + "\n" + result.stdout + result.stderr +
                           f"\nexit={result.returncode}\n", encoding="utf-8")
            marker = "vertical=" if args.suite == "growth" else "nursery_study="
            lines = [line for line in result.stdout.splitlines() if line.startswith(marker)]
            if result.returncode != 0 or len(lines) != 1:
                raise RuntimeError(f"Failed process retained: {raw}")
            values = dict(part.split("=", 1) for part in lines[0].split())
            if values["module_sha256"].lower() != record["module_sha256"] or \
                    values["collector"] != category or \
                    (args.suite == "survival" and values["primary_full_calls"] != "8"):
                raise RuntimeError(f"Input/module/call-count mismatch retained: {raw}")
            if args.suite == "survival" and label == "nursery-on" and int(values["copied_bytes"]) == 0:
                raise RuntimeError(f"The moving arm did no measured copying: {raw}")
            if args.suite == "survival" and label in ("compact", "nursery-off") and int(values["copied_bytes"]) != 0:
                raise RuntimeError(f"The nonmoving arm copied objects: {raw}")
            row = {"arm": label, "round": round_index, "replicate": replicate,
                   "exit_code": result.returncode, **values, **identities}
            if writer is None:
                writer = csv.DictWriter(output, fieldnames=list(row))
                writer.writeheader()
            writer.writerow(row)
            output.flush()
            rows.append(row)
    summaries = []
    for scenario in scenarios:
        for label, _, _, _ in arms:
            selected = [row for row in rows if row["arm"] == label and
                        row["vertical" if args.suite == "growth" else "nursery_study"] == scenario]
            normal = [row for row in selected if row["replicate"] != "injected"]
            summary = {"scenario": scenario, "arm": label, "normal_count": len(normal)}
            for name in ("elapsed_ms", "cpu_ms", "pause_ms", "rss_bytes", "private_bytes",
                         "managed_admit", "managed_commit", "native_planes", "copied_bytes",
                         "pinned_promotion_bytes", "copy_failures", "collections", "full_completions"):
                if name in normal[0]:
                    summary[name] = statistics.median(float(row[name]) for row in normal)
            summary["A_ms"] = statistics.median(float(row["elapsed_ms"]) for row in normal if row["replicate"] == "A")
            summary["B_ms"] = statistics.median(float(row["elapsed_ms"]) for row in normal if row["replicate"] == "B")
            summary["injected_ms"] = statistics.median(float(row["elapsed_ms"]) for row in selected if row["replicate"] == "injected")
            summaries.append(summary)
    with args.output.with_suffix(".summary.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=list(summaries[0]))
        writer.writeheader()
        writer.writerows(summaries)


if __name__ == "__main__":
    main()
