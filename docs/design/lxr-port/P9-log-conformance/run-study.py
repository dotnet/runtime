#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Fixed, source-bound log footprint and completed-cycle study; never retry a child."""

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


def expected_checksum(input_name, count):
    if input_name == "references":
        return 8 * count * (count - 1) // 2
    return sum(2 * ((index + cycle) % 251) for cycle in range(8) for index in range(count // 256))


def validate(record, input_name, count, category, nursery, mature, framework, expected_module, common):
    errors = []
    expected = {
        "input": input_name, "count": count, "collector": category,
        "checksum": expected_checksum(input_name, count), "collections": 8,
        "primary_full_calls": 8, "warmup_full_calls": 2, "cleanup_full_calls": 1,
    }
    if category == "LXR":
        expected.update(nursery=int(nursery), mature=int(mature))
    for key, value in expected.items():
        if record.get(key) != value:
            errors.append(f"{key}: {record.get(key)!r} != {value!r}")
    if len(record.get("full_call_ms", [])) != 8:
        errors.append("not eight actual request durations")
    if record.get("module_sha256", "").lower() != expected_module:
        errors.append("process collector identity differs from executed input")
    loaded = {Path(row["path"]).name.lower(): row
              for row in record.get("loaded_native_modules", [])}
    native = (["coreclr.dll", "clrjit.dll", "corerun.exe"] if os.name == "nt"
              else ["libcoreclr.so", "libclrjit.so", "corerun"])
    if category == "LXR":
        native.append("clrlxrgc.dll" if os.name == "nt" else "libclrlxrgc.so")
    for name in native:
        row = loaded.get(name)
        target = (framework / name).resolve()
        expected_sha = common.get(name, expected_module)
        if row is None or Path(row["path"]).resolve() != target or row["sha256"].lower() != expected_sha:
            errors.append(f"actual loaded native module mismatch: {name}")
    corelib = record.get("framework_corelib_input", {})
    if (corelib.get("sha256", "").lower() != common["System.Private.CoreLib.dll"] or
            Path(corelib.get("path", "")).resolve() != (framework / "System.Private.CoreLib.dll").resolve()):
        errors.append("framework CoreLib input mismatch")
    return errors


def check_inputs(expected):
    errors = []
    for path, sha in expected.items():
        try:
            if digest(path) != sha:
                errors.append(f"frozen input changed: {path}")
        except OSError as error:
            errors.append(f"frozen input unreadable: {path}: {error}")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", required=True, type=Path)
    parser.add_argument("--changed", required=True, type=Path)
    parser.add_argument("--fixture", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--phase", choices=("smoke", "convergence", "overhead"), default="convergence")
    args = parser.parse_args()
    baseline = args.baseline.resolve(strict=True)
    changed = args.changed.resolve(strict=True)
    fixture = args.fixture.resolve(strict=True)
    args.output.mkdir(parents=True, exist_ok=False)
    windows = os.name == "nt"
    core = "corerun.exe" if windows else "corerun"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    coreclr = "coreclr.dll" if windows else "libcoreclr.so"
    jit = "clrjit.dll" if windows else "libclrjit.so"
    common = {name: digest(baseline / name)
              for name in (core, coreclr, jit, "System.Private.CoreLib.dll")}
    for name, sha in common.items():
        if digest(changed / name) != sha:
            raise RuntimeError(f"Collector-only experiment changed common input {name}")
    arms = []
    for stage, framework in (("baseline", baseline), ("changed", changed)):
        for mode, nursery, mature in (("nonmoving", "0", "0"), ("nursery", "1", "0"), ("mature", "1", "1")):
            arms.append((f"{stage}-{mode}", "LXR", framework, nursery, mature))
    if args.phase != "overhead":
        arms.extend((("WKS", "WKS", changed, None, None), ("Server", "Server", changed, None, None)))
    sizes = ((24, 65536),) if args.phase in ("smoke", "overhead") else ((24, 65536), (64, 131072), (128, 262144))
    observations = ("diagnostic",) if args.phase == "smoke" else ("A0", "B0", "A1", "B1", "injected", "diagnostic")
    schedule = []
    for capacity, count in sizes:
        for input_name in ("bytes", "references"):
            for sample, observation in enumerate(observations):
                ordered = arms[sample % len(arms):] + arms[:sample % len(arms)]
                if observation.startswith("B"):
                    ordered = list(reversed(ordered))
                for arm in ordered:
                    schedule.append((capacity, count, input_name, observation, arm))
    identities = {"fixture": str(fixture), "fixture_sha256": digest(fixture), "common": common,
                  "collectors": {str(frame / collector): digest(frame / collector) for frame in (baseline, changed)}}
    (args.output / "schedule.json").write_text(json.dumps({
        "phase": args.phase, "identities": identities,
        "schedule": [[capacity, count, input_name, observation, arm[0]]
                     for capacity, count, input_name, observation, arm in schedule],
    }, indent=2) + "\n", encoding="utf-8")
    base = {key: value for key, value in os.environ.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    rows = []
    with (args.output / "results.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=(
            "index", "capacity_mib", "count", "input", "observation", "arm", "exit_code",
            "semantic_valid", "parent_wall_ms", "fixture_sha256", "module_sha256", "raw", "record"))
        writer.writeheader()
        for index, (capacity, count, input_name, observation, arm) in enumerate(schedule):
            label, category, framework, nursery, mature = arm
            module = framework / (collector if category == "LXR" else coreclr)
            module_sha = identities["collectors"][str(module)] if category == "LXR" else common[coreclr]
            child = dict(base, DOTNET_gcServer="1" if category == "Server" else "0")
            if category == "LXR":
                child.update(DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery,
                             DOTNET_LXREvacuateMature=mature,
                             DOTNET_LXRImmixInitialMB=str(8 if capacity == 24 else capacity // 2),
                             DOTNET_LXRLOSInitialMB=str(16 if capacity == 24 else capacity // 2))
            command = [str(framework / core), str(fixture), "--log-study", input_name,
                       category, str(module), str(count),
                       "injected" if observation == "injected" else "normal",
                       "on" if observation == "diagnostic" and category == "LXR" else "off"]
            binding = {"command": command, "settings": {k: v for k, v in child.items() if k.startswith("DOTNET_")},
                       "module_sha256": module_sha, "fixture_sha256": identities["fixture_sha256"]}
            raw = args.output / f"{index:03d}-{capacity}-{input_name}-{label}-{observation}.log"
            frozen = {fixture: identities["fixture_sha256"], module: module_sha,
                      **{framework / name: sha for name, sha in common.items()}}
            errors = check_inputs(frozen)
            if errors:
                raw.write_text(json.dumps(binding) + "\npreflight=" + json.dumps(errors) + "\n", encoding="utf-8")
                writer.writerow({"index": index, "capacity_mib": capacity, "count": count,
                                 "input": input_name, "observation": observation, "arm": label,
                                 "exit_code": "input_mismatch", "semantic_valid": False,
                                 "fixture_sha256": identities["fixture_sha256"], "module_sha256": module_sha,
                                 "raw": str(raw)})
                stream.flush()
                (args.output / "failure.json").write_text(json.dumps(errors, indent=2) + "\n")
                raise RuntimeError(f"Frozen input mismatch retained before execution: {raw}: {errors}")
            started = time.perf_counter()
            try:
                result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=240)
            except subprocess.TimeoutExpired as error:
                raw.write_text(json.dumps(binding) + "\n" + str(error.stdout) + "\n" +
                               str(error.stderr) + "\nexit=timeout\n", encoding="utf-8")
                writer.writerow({"index": index, "arm": label, "exit_code": "timeout",
                                 "semantic_valid": False, "raw": str(raw)})
                stream.flush()
                raise RuntimeError(f"Timeout retained: {raw}") from error
            parent_wall = (time.perf_counter() - started) * 1000
            raw.write_text(json.dumps(binding) + "\n" + result.stdout + result.stderr +
                           f"\nexit={result.returncode}\n", encoding="utf-8")
            output = [line.removeprefix("log_study=") for line in result.stdout.splitlines()
                      if line.startswith("log_study=")]
            record = {}
            errors = check_inputs(frozen)
            try:
                if len(output) != 1:
                    raise ValueError("expected exactly one log_study record")
                record = json.loads(output[0])
                errors.extend(validate(record, input_name, count, category, nursery, mature,
                                       framework, module_sha, common))
                if category == "LXR" and record["live"]["managed_admitted_bytes"] != capacity * 1024 * 1024:
                    errors.append("actual admission differs from the scheduled fixed input")
            except (ValueError, KeyError, TypeError, AttributeError) as error:
                errors.append(str(error))
            valid = result.returncode == 0 and not errors
            row = {"index": index, "capacity_mib": capacity, "count": count, "input": input_name,
                   "observation": observation, "arm": label, "exit_code": result.returncode,
                   "semantic_valid": valid, "parent_wall_ms": parent_wall,
                   "fixture_sha256": identities["fixture_sha256"], "module_sha256": module_sha,
                   "raw": str(raw), "record": json.dumps(record, separators=(",", ":"))}
            writer.writerow(row)
            stream.flush()
            rows.append(row)
            print(index, capacity, input_name, label, observation, result.returncode, valid, flush=True)
            if not valid:
                (args.output / "failure.json").write_text(json.dumps(errors, indent=2) + "\n")
                raise RuntimeError(f"Failure retained before rejection: {raw}: {errors}")
    summaries = []
    for capacity, count in sizes:
        for input_name in ("bytes", "references"):
            for label, *_ in arms:
                matching = [row for row in rows if row["capacity_mib"] == capacity
                            and row["input"] == input_name and row["arm"] == label]
                normal = [row for row in matching if row["observation"] in ("A0", "B0", "A1", "B1")]
                if not normal:
                    continue
                records = [json.loads(row["record"]) for row in normal]
                summary = {"capacity_mib": capacity, "input": input_name, "arm": label, "normal_n": len(normal)}
                for key in ("elapsed_ms", "cpu_ms", "pause_ms", "cleanup_ms"):
                    summary[key] = statistics.median(record[key] for record in records)
                for key in ("rss_bytes", "private_bytes", "native_plane_committed_bytes", "managed_live_bytes"):
                    values = [record["live"][key] for record in records if record["live"][key] is not None]
                    summary[key] = statistics.median(values) if values else None
                summary["parent_wall_ms"] = statistics.median(row["parent_wall_ms"] for row in normal)
                for prefix in ("A", "B"):
                    summary[prefix + "_elapsed_ms"] = statistics.median(
                        json.loads(row["record"])["elapsed_ms"] for row in normal
                        if row["observation"].startswith(prefix))
                summaries.append(summary)
    if summaries:
        with (args.output / "summary.csv").open("w", newline="", encoding="utf-8") as stream:
            writer = csv.DictWriter(stream, fieldnames=list(summaries[0]))
            writer.writeheader()
            writer.writerows(summaries)


if __name__ == "__main__":
    main()
