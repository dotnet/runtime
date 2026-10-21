#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Predeclared interleaved arms; every process and failure is retained, never retried."""

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


def collector_environment(source):
    environment = dict(source)
    for key in tuple(environment):
        canonical = key.upper()
        if canonical.startswith(("DOTNET_LXR", "COMPLUS_LXR", "DOTNET_GCHEAPHARDLIMIT", "COMPLUS_GCHEAPHARDLIMIT")):
            del environment[key]
        elif canonical in (
            "DOTNET_GCPATH", "COMPLUS_GCPATH", "DOTNET_GCNAME", "COMPLUS_GCNAME",
            "DOTNET_GCSERVER", "COMPLUS_GCSERVER", "DOTNET_GCSTRESS", "COMPLUS_GCSTRESS",
        ):
            del environment[key]
    return environment


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--changed", type=Path, required=True)
    parser.add_argument("--test", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--suite", choices=("throughput", "growth"), default="throughput")
    args = parser.parse_args()
    windows = os.name == "nt"
    names = {
        "corerun": "corerun.exe" if windows else "corerun",
        "coreclr": "coreclr.dll" if windows else "libcoreclr.so",
        "jit": "clrjit.dll" if windows else "libclrjit.so",
        "corelib": "System.Private.CoreLib.dll",
        "lxr": "clrlxrgc.dll" if windows else "libclrlxrgc.so",
    }
    args.baseline = args.baseline.resolve(strict=True)
    args.changed = args.changed.resolve(strict=True)
    args.test = args.test.resolve(strict=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    identities = {"test_sha256": digest(args.test)}
    for key in ("corerun", "coreclr", "jit", "corelib"):
        before = digest(args.baseline / names[key])
        after = digest(args.changed / names[key])
        if before != after:
            raise RuntimeError(f"This collector-only study requires unchanged {key}: {before} != {after}")
        identities[key + "_sha256"] = before
    environment = collector_environment(os.environ)
    environment.update({
        "DOTNET_ReadyToRun": "0", "DOTNET_TieredCompilation": "0",
        "DOTNET_gcConcurrent": "1", "DOTNET_GCHeapCount": "2",
    })
    arms = [
        ("P5-LXR", "LXR", args.baseline, {}),
        ("P8-LXR-legacy", "LXR", args.changed, {"DOTNET_LXRLegacyCapacity": "1"}),
        ("P8-LXR-fast-off", "LXR", args.changed, {
            "DOTNET_LXRLegacyCapacity": "1", "DOTNET_LXRFastAllocation": "0",
        }),
        ("WKS", "WKS", args.changed, {}),
        ("Server", "Server", args.changed, {}),
    ]
    scenarios = ("allocation", "mutation", "automatic", "forced")
    if args.suite == "growth":
        arms = [
            ("P5-capacity-control", "LXR", args.baseline, {}),
            ("P8-LXR-growth", "LXR", args.changed, {}),
            ("WKS", "WKS", args.changed, {}),
            ("Server", "Server", args.changed, {}),
        ]
        scenarios = ("growth",)
    schedule = []
    for scenario in scenarios:
        for round_index in range(3):
            order = arms[round_index:] + arms[:round_index]
            for replicate in ("A", "B", "injected"):
                selected = list(reversed(order)) if replicate == "B" else order
                for arm in selected:
                    schedule.append((scenario, round_index, replicate, arm))
    args.output.with_suffix(".schedule.json").write_text(json.dumps({
        "identities": identities,
        "schedule": [
            [scenario, round_index, replicate, arm[0], arm[3]]
            for scenario, round_index, replicate, arm in schedule
        ],
    }, indent=2), encoding="utf-8")
    rows = []
    with args.output.open("w", newline="", encoding="utf-8") as output:
        writer = None
        for index, (scenario, round_index, replicate, arm) in enumerate(schedule):
            label, collector, framework, settings = arm
            child = dict(environment)
            child.update(settings)
            child["DOTNET_gcServer"] = "1" if collector == "Server" else "0"
            module = framework / names["lxr" if collector == "LXR" else "coreclr"]
            if collector == "LXR":
                child["DOTNET_GCPath"] = str(module)
            control = "injected" if replicate == "injected" else "normal"
            command = [str(framework / names["corerun"]), str(args.test),
                       "--vertical", scenario, collector, str(module), control]
            raw = args.output.with_name(f"{args.output.stem}-{index:03d}-{scenario}-{label}-{replicate}.log")
            chosen = {key: value for key, value in child.items() if key.upper().startswith("DOTNET_LXR") or key in (
                "DOTNET_GCPath", "DOTNET_gcServer", "DOTNET_GCHeapCount",
                "DOTNET_gcConcurrent", "DOTNET_ReadyToRun", "DOTNET_TieredCompilation",
            )}
            try:
                result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
            except subprocess.TimeoutExpired as error:
                raw.write_text(json.dumps({"command": command, "settings": chosen}) + "\n" +
                               str(error.stdout) + "\n" + str(error.stderr) + "\nexit=timeout\n", encoding="utf-8")
                raise RuntimeError(f"Timeout; retained {raw}") from error
            raw.write_text(json.dumps({"command": command, "settings": chosen}) + "\n" +
                           result.stdout + result.stderr + f"\nexit={result.returncode}\n", encoding="utf-8")
            lines = [line for line in result.stdout.splitlines() if line.startswith("vertical=")]
            if len(lines) != 1:
                raise RuntimeError(f"Expected one result row; retained {raw}")
            values = dict(item.split("=", 1) for item in lines[0].split())
            expected_capacity = label == "P5-capacity-control"
            expected_exit = 2 if expected_capacity else 0
            if result.returncode != expected_exit or values["exhausted"] != str(expected_capacity):
                raise RuntimeError(f"Unexpected result {result.returncode}; retained {raw}")
            if values["collector"] != collector or values["control"] != control:
                raise RuntimeError(f"Input/collector identity mismatch; retained {raw}")
            row = {
                "arm": label, "round": round_index, "replicate": replicate,
                "exit_code": result.returncode, "capacity_control": expected_capacity,
                **values, **identities,
            }
            if writer is None:
                writer = csv.DictWriter(output, fieldnames=list(row))
                writer.writeheader()
            writer.writerow(row)
            output.flush()
            rows.append(row)
    summaries = []
    for scenario in scenarios:
        for label, _, _, _ in arms:
            selected = [row for row in rows if row["arm"] == label and row["vertical"] == scenario]
            normal = [row for row in selected if row["replicate"] != "injected"]
            a = statistics.median(float(row["elapsed_ms"]) for row in normal if row["replicate"] == "A")
            b = statistics.median(float(row["elapsed_ms"]) for row in normal if row["replicate"] == "B")
            times = [float(row["elapsed_ms"]) for row in normal]
            injected = [float(row["elapsed_ms"]) for row in selected if row["replicate"] == "injected"]
            summary = {
                "scenario": scenario, "arm": label, "n": len(normal),
                "median_ms": statistics.median(times), "AA_delta_pct": (b / a - 1) * 100,
                "injected_median_ms": statistics.median(injected),
                "sensitivity": min(injected) > max(times),
            }
            for key in (
                "working_set_bytes", "private_bytes", "owned_resident_bytes", "non_owned_resident_bytes",
                "heap_bytes", "fragmentation_bytes", "managed_committed_bytes",
                "reserved_bytes", "admitted_bytes", "native_plane_committed_bytes",
                "live_bytes", "reclaimed_bytes", "fast_hits", "refills", "growth_count",
                "hard_limit_bytes", "collections", "post_full_ms", "allocated_bytes",
            ):
                summary[key] = statistics.median(float(row[key]) for row in normal)
            summaries.append(summary)
            print(f"{scenario} {label}: median_ms={summary['median_ms']:.3f} "
                  f"AA_delta_pct={summary['AA_delta_pct']:.2f} "
                  f"sensitivity={'detected' if summary['sensitivity'] else 'inconclusive'}")
    with args.output.with_suffix(".summary.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=list(summaries[0]))
        writer.writeheader()
        writer.writerows(summaries)


if __name__ == "__main__":
    main()
