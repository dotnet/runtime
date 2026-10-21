#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Fixed default-recycling aged graphs; preserve every process without retries or tuning."""

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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--changed", type=Path, required=True)
    parser.add_argument("--fixture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    baseline = args.baseline.resolve(strict=True)
    changed = args.changed.resolve(strict=True)
    fixture = args.fixture.resolve(strict=True)
    args.output = args.output.resolve()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    if args.output.exists():
        raise RuntimeError("A previous measurement cannot be overwritten.")
    windows = os.name == "nt"
    corerun = "corerun.exe" if windows else "corerun"
    coreclr = "coreclr.dll" if windows else "libcoreclr.so"
    jit = "clrjit.dll" if windows else "libclrjit.so"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    identities = {"fixture_sha256": digest(fixture)}
    for name in (corerun, coreclr, jit, "System.Private.CoreLib.dll"):
        value = digest(baseline / name)
        if value != digest(changed / name):
            raise RuntimeError(f"Runtime input differs: {name}")
        identities[name] = value
    arms = [
        ("accepted-off", baseline, "LXR", "0", "0"),
        ("accepted-nursery", baseline, "LXR", "1", "0"),
        ("nonmoving", changed, "LXR", "0", "0"),
        ("nursery-only", changed, "LXR", "1", "0"),
        ("nursery-mature", changed, "LXR", "1", "1"),
        ("WKS", changed, "WKS", None, None),
        ("Server", changed, "Server", None, None),
    ]
    scenarios = ("fragmented", "dense", "pinned", "high-survival")
    schedule = []
    for scenario in scenarios:
        for pair in range(2):
            order = arms[pair:] + arms[:pair]
            for replicate in ("A", "B", "injected"):
                for arm in reversed(order) if replicate == "B" else order:
                    schedule.append((scenario, pair, replicate, "off", arm))
        for arm in arms:
            schedule.append((scenario, 0, "diagnostic", "diagnostic", arm))
    declaration = {
        "protocol": "v2: reviewed source; placement is an observation, not a per-process allocator invariant",
        "amendment": "Preserve the stopped candidate14 matrix and its zero-hit diagnostic. "
                     "Persist semantic/activity/placement outcomes separately. No input sizes, "
                     "recovery count, heap settings or effect thresholds are tuned.",
        "identities": identities,
        "scope": "DEFAULT mutator/copy recycling. 512 aged 4096-byte payload nodes; "
                 "retain a 64-node cycle, or 512 nodes in dense. Eight explicit full calls. "
                 "High-survival adds eight 128-node bursts, retaining four cohorts. "
                 "Pin input holds eight real pinned payloads. Subsequent 256 x4096 allocations "
                 "measure real old-block address reuse separately. Four normal and two injected "
                 "observations per arm/scenario; 8x25ms nominal injection. One separately intrusive "
                 "diagnostic process per group. CPU/RSS/private are whole-process observations, "
                 "not exclusive GC CPU or production request latency. Nested cost timers are not additive. "
                 "No retries, effect target, equal-built-in-heap claim or RSS-release inference.",
        "arms": [[label, str(root), category, nursery, mature, digest(root / collector)]
                 for label, root, category, nursery, mature in arms],
        "schedule": [[scenario, pair, replicate, instrumentation, arm[0]]
                     for scenario, pair, replicate, instrumentation, arm in schedule],
    }
    args.output.with_suffix(".schedule.json").write_text(json.dumps(declaration, indent=2), encoding="utf-8")
    base = {key: value for key, value in os.environ.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    rows = []
    with args.output.open("w", newline="", encoding="utf-8") as output:
        writer = None
        for index, (scenario, pair, replicate, instrumentation, arm) in enumerate(schedule):
            label, root, category, nursery, mature = arm
            child = dict(base)
            child["DOTNET_gcServer"] = "1" if category == "Server" else "0"
            module = root / (collector if category == "LXR" else coreclr)
            if category == "LXR":
                child.update(DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery,
                             DOTNET_LXREvacuateMature=mature)
            command = [str(root / corerun), str(fixture), "--mature-study", scenario,
                       category, str(module), "injected" if replicate == "injected" else "normal",
                       instrumentation]
            raw = args.output.with_name(f"{args.output.stem}-{index:03d}-{scenario}-{label}-{replicate}.log")
            binding = {"command": command, "settings": {key: value for key, value in child.items()
                                                       if key.upper().startswith("DOTNET_")},
                       "identities": identities, "module_sha256": digest(module)}
            try:
                result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
            except subprocess.TimeoutExpired as error:
                raw.write_text(json.dumps(binding) + "\n" + str(error.stdout) + "\n" +
                               str(error.stderr) + "\nexit=timeout\n", encoding="utf-8")
                raise RuntimeError(f"Timeout retained: {raw}") from error
            raw.write_text(json.dumps(binding) + "\n" + result.stdout + result.stderr +
                           f"\nexit={result.returncode}\n", encoding="utf-8")
            matches = [line.removeprefix("mature_study=") for line in result.stdout.splitlines()
                       if line.startswith("mature_study=")]
            if result.returncode != 0 or len(matches) != 1:
                raise RuntimeError(f"Failed process retained: {raw}")
            values = json.loads(matches[0])
            semantic_valid = values["collector"] == category and \
                values["server"] == (category == "Server") and \
                values["module_sha256"].lower() == binding["module_sha256"] and \
                values["primary_full_calls"] == 8 and \
                values["checksum"] == (1046528 if scenario == "dense" else 129024)
            activity = values["mature_copied_bytes"] is not None and \
                values["mature_copied_bytes"] > 0 and values["mature_released_blocks"] > 0
            placement = values["old_payload_blocks_reused_by_actual_allocations"] is not None and \
                values["old_payload_blocks_reused_by_actual_allocations"] > 0
            if category == "LXR":
                semantic_valid = semantic_valid and values["nursery_enabled"] == int(nursery)
                if label.startswith("accepted-"):
                    semantic_valid = semantic_valid and values["mature_enabled"] is None and \
                        values["mature_copied_bytes"] is None
                else:
                    semantic_valid = semantic_valid and values["mature_enabled"] == int(mature)
                semantic_valid = semantic_valid and \
                    (mature != "0" or values["mature_copied_bytes"] in (None, 0))
            row = {"arm": label, "pair": pair, "replicate": replicate, "exit_code": result.returncode,
                   "semantic_valid": semantic_valid, "mature_copy_release_activity": activity,
                   "original_block_address_hits_observed": placement, **values, **identities}
            for key, value in list(row.items()):
                if isinstance(value, list):
                    row[key] = json.dumps(value, separators=(",", ":"))
            if writer is None:
                writer = csv.DictWriter(output, fieldnames=list(row))
                writer.writeheader()
            writer.writerow(row)
            output.flush()
            rows.append(row)
            print(index, scenario, label, replicate, values["elapsed_ms"], flush=True)
            if not semantic_valid:
                raise RuntimeError(f"Actual category/input/module/mode mismatch; row retained: {raw}")
            if label == "nursery-mature" and scenario == "fragmented" and not activity:
                raise RuntimeError(f"No actual mature copy/release activity; row retained: {raw}")
    observed = [row for row in rows if row["scenario"] == "fragmented" and row["arm"] == "nursery-mature" and
                row["replicate"] in ("A", "B")]
    if not any(row["original_block_address_hits_observed"] for row in observed):
        raise RuntimeError("No original-block allocation hits were observed in the fixed normal cohort; all rows retained.")
    summaries = []
    for scenario in scenarios:
        for label, *_ in arms:
            selected = [row for row in rows if row["arm"] == label and row["scenario"] == scenario]
            normal = [row for row in selected if row["replicate"] in ("A", "B")]
            summary = {"scenario": scenario, "arm": label, "normal_count": len(normal)}
            for key in ("elapsed_ms", "cpu_ms", "pause_ms", "collections", "mature_copied_bytes",
                        "nursery_copied_bytes", "mature_copy_failures", "nursery_copy_failures",
                        "mature_released_blocks", "old_payload_blocks_reused_by_actual_allocations",
                        "fragmentation_bytes", "managed_admit", "managed_commit", "managed_live",
                        "native_planes", "rss_bytes", "private_bytes", "recovery_elapsed_ms"):
                samples = [row[key] for row in normal]
                summary[key] = None if any(value is None for value in samples) else statistics.median(samples)
            for replicate in ("A", "B", "injected"):
                summary[replicate + "_ms"] = statistics.median(
                    row["elapsed_ms"] for row in selected if row["replicate"] == replicate)
            summaries.append(summary)
    with args.output.with_suffix(".summary.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=list(summaries[0]))
        writer.writeheader()
        writer.writerows(summaries)


if __name__ == "__main__":
    main()
