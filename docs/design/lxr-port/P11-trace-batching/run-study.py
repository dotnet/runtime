#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Frozen, interleaved collector-only comparison. Retain failures; never retry."""

import argparse
import csv
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import statistics
import subprocess
import sys
import time
import zlib

SOURCE = Path(__file__).read_bytes()
SOURCE_SHA = hashlib.sha256(SOURCE).hexdigest()
FIXTURE_SHA = "5201b0ccc41ab3cd51313643d62eda6509cbc2e9c17e7aa0131d021f9913cae9"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def write(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def schedule(phase):
    arms = [(f"{stage}-{mode}", "LXR", stage, nursery, mature)
            for mode, nursery, mature in (("nonmoving", "0", "0"), ("nursery", "1", "0"), ("mature", "1", "1"))
            for stage in ("before", "after")]
    if phase in ("timing", "pressure"):
        arms += [("WKS", "WKS", "before", None, None), ("Server", "Server", "before", None, None)]
    sizes = ((262144, "references", 4), (65536, "references", 2),
             (131072, "references", 2), (262144, "bytes", 1))
    if phase == "diagnostic":
        sizes = ((262144, "references", 1), (65536, "references", 1),
                 (131072, "references", 1), (262144, "bytes", 1))
    elif phase == "profile":
        sizes = ((262144, "references", 1),)
    elif phase == "pressure":
        sizes = ((524288, "allocation", 2),)
    result = []
    for count, input_name, repeats in sizes:
        for sample in range(repeats):
            ordered = arms[sample:] + arms[:sample]
            if sample % 2:
                ordered = list(reversed(ordered))
            result += [(count, input_name, sample, arm) for arm in ordered]
    return result


def validate_modules(record, frame, category, common, collector_sha):
    errors = []
    loaded = {Path(row["path"]).name.lower(): row for row in record.get("loaded_native_modules", [])}
    names = [name for name in common if name != "System.Private.CoreLib.dll"]
    if category == "LXR":
        names.append("clrlxrgc.dll" if os.name == "nt" else "libclrlxrgc.so")
    for name in names:
        row = loaded.get(name.lower())
        if (row is None or Path(row["path"]).resolve() != (frame / name).resolve()
                or row["sha256"].lower() != common.get(name, collector_sha)):
            errors.append(f"Actual loaded native module mismatch: {name}")
    corelib = record.get("framework_corelib_input", {})
    if (corelib.get("sha256", "").lower() != common["System.Private.CoreLib.dll"]
            or Path(corelib.get("path", "")).resolve() != (frame / "System.Private.CoreLib.dll").resolve()):
        errors.append("CoreLib input mismatch")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--phase", choices=("prepare", "timing", "diagnostic", "pressure", "profile"), required=True)
    parser.add_argument("--label")
    args = parser.parse_args()
    root = args.root.resolve()
    windows = os.name == "nt"
    os_name = "windows" if windows else "linux"
    host, vm, jit = ("corerun.exe", "coreclr.dll", "clrjit.dll") if windows else ("corerun", "libcoreclr.so", "libclrjit.so")
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    accepted = load("accepted", root / "accepted-run-study.py")
    fixture = root / "fixture-log4" / "runtime-tests.dll"
    diagnostic = root / "fixture-diagnostic" / "runtime-diagnostics.dll"
    frames = {stage: root / f"{stage}-Release-{os_name}" for stage in ("before", "after")}
    frozen_path = root / "study-inputs.json"
    if args.phase == "prepare":
        if sha(fixture) != FIXTURE_SHA:
            raise RuntimeError("The frozen performance fixture was replaced")
        expected = {str(fixture): FIXTURE_SHA, str(diagnostic): sha(diagnostic)}
        for path in (fixture.with_suffix(".pdb"), diagnostic.with_suffix(".pdb")):
            expected[str(path)] = sha(path)
        source_root = Path(__file__).resolve().parents[4]
        source_expected = {
            str(path): sha(path) for path in (
                source_root / "src" / "coreclr" / "gc" / "lxr_reference_counting.cpp",
                source_root / "src" / "coreclr" / "gc" / "lxr_reference_counting.h",
                source_root / "src" / "coreclr" / "gc" / "lxr_gc.cpp",
                source_root / "src" / "coreclr" / "gc" / "lxr_gcload.cpp",
                Path(__file__).resolve(), root / "accepted-run-study.py", root / "identity-tools.py",
            )
        }
        common = {name: sha(frames["before"] / name) for name in (host, vm, jit, "System.Private.CoreLib.dll")}
        identities = load("identities", root / "identity-tools.py")
        symbol_bindings = []
        for configuration in ("Release", "Debug"):
            for stage in ("before", "after"):
                frame = root / f"{stage}-{configuration}-{os_name}"
                module = frame / collector
                companion = frame / ("clrlxrgc.pdb" if windows else "libclrlxrgc.so.dbg")
                if windows:
                    actual, symbols = identities.pe_id(module), identities.pdb_id(companion)
                    match = actual == symbols
                    binding = dict(guid=actual[0], age=actual[1], pdb_guid=symbols[0], pdb_age=symbols[1])
                else:
                    filename, crc = identities.elf_link(module)
                    actual = zlib.crc32(companion.read_bytes())
                    match = filename == companion.name and crc == actual
                    binding = dict(debuglink=filename, crc32=f"{crc:08x}", actual_crc32=f"{actual:08x}")
                if not match:
                    raise RuntimeError(f"Collector symbol mismatch: {module}")
                symbol_bindings.append(dict(stage=stage, configuration=configuration, module=str(module),
                                            module_sha256=sha(module), companion=str(companion),
                                            companion_sha256=sha(companion), matched=True, **binding))
        write(root / "module-identities.json", symbol_bindings)
        for stage, frame in frames.items():
            for name, value in common.items():
                if sha(frame / name) != value:
                    raise RuntimeError(f"Collector-only comparison changed {name}")
                expected[str(frame / name)] = value
            expected[str(frame / collector)] = sha(frame / collector)
            companion = frame / ("clrlxrgc.pdb" if windows else "libclrlxrgc.so.dbg")
            expected[str(companion)] = sha(companion)
        original = "e1917aee80262c4bc89e02d1f18ef2b01c59d7015390955e160009edd858313e" if windows else "1e8c0da8348cc010617b48b7c1a2981c32f46f4d163192154c5421bd08811445"
        if expected[str(frames["before"] / collector)] != original:
            raise RuntimeError("The original collector was replaced")
        write(frozen_path, dict(expected=expected, source_expected=source_expected, common=common, runner_sha256=SOURCE_SHA,
                                timing_schedule=schedule("timing"), pressure_schedule=schedule("pressure"),
                                source_base="9f25603ec751a1407808d4306b70198435873549"))
        print("Prepared 72 normal and 16 automatic-pressure observations; frozen source", SOURCE_SHA)
        return
    frozen = json.loads(frozen_path.read_text())
    expected = {Path(path): digest for path, digest in frozen["expected"].items()}
    source_expected = {Path(path): digest for path, digest in frozen["source_expected"].items()}
    expected.update(source_expected)
    failures = accepted.check_inputs(expected)
    if failures or frozen["runner_sha256"] != SOURCE_SHA:
        raise RuntimeError(f"Frozen study input mismatch: {failures}; runner={SOURCE_SHA}")
    output = root / (args.label or args.phase)
    output.mkdir(exist_ok=False)
    (output / "executed-runner.py").write_bytes(SOURCE)
    plan = schedule(args.phase)
    write(output / "schedule.json", plan)
    base = {key: value for key, value in os.environ.items() if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0", DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    rows = []
    with (output / "results.csv").open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=("index", "arm", "input", "count", "sample", "exit", "valid", "parent_wall_ms", "raw", "record"))
        writer.writeheader()
        for index, (count, input_name, sample, arm) in enumerate(plan):
            label, category, stage, nursery, mature = arm
            frame = frames[stage]
            module = frame / (collector if category == "LXR" else vm)
            module_sha = frozen["expected"][str(module)]
            selected_fixture = diagnostic if args.phase in ("diagnostic", "pressure") else fixture
            env = dict(base, DOTNET_gcServer="1" if category == "Server" else "0")
            capacity = 24 if args.phase == "pressure" else 128
            if category == "LXR":
                env.update(DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery,
                           DOTNET_LXREvacuateMature=mature,
                           DOTNET_LXRImmixInitialMB=str(8 if capacity == 24 else 64),
                           DOTNET_LXRLOSInitialMB=str(16 if capacity == 24 else 64))
                if args.phase == "pressure":
                    env["DOTNET_LXRManagedHeapHardLimitMB"] = "24"
            elif args.phase == "pressure":
                env["DOTNET_GCHeapHardLimit"] = "1800000"
            command = [str(frame / host), str(selected_fixture)]
            if args.phase == "pressure":
                command += ["--vertical", "allocation", category, str(module), "normal"]
            else:
                command += ["--log-study", input_name, category, str(module), str(count), "normal",
                            "on" if args.phase == "diagnostic" else "off"]
            prefix = output / f"{index:02d}-{count}-{input_name}-{label}"
            launch = list(command)
            if args.phase == "profile":
                if windows:
                    launch = [sys.executable, str(root / "windows-sample.py"), "--output", str(prefix) + ".samples.json",
                              "--symbols", str(frame), "--"] + command
                else:
                    launch = ["/usr/lib/linux-tools/5.15.0-198-generic/perf", "record", "-o", str(prefix) + ".data",
                              "-F", "199", "-e", "cpu-clock:u", "--call-graph", "dwarf,16384", "--"] + command
            write(Path(str(prefix) + ".binding.json"), dict(
                command=launch, settings={key: value for key, value in env.items() if key.upper().startswith(("DOTNET_", "COMPLUS_"))},
                expected=frozen["expected"], fixture_sha256=frozen["expected"][str(selected_fixture)],
                runner_sha256=SOURCE_SHA, utc=time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())))
            checked = {path: digest for path, digest in expected.items() if path.parent == frame or path == selected_fixture}
            checked.update(source_expected)
            failures = accepted.check_inputs(checked)
            if failures:
                write(Path(str(prefix) + ".failure.json"), failures)
                raise RuntimeError(failures)
            started = time.perf_counter()
            try:
                result = subprocess.run(launch, env=env, capture_output=True, text=True, timeout=240)
            except subprocess.TimeoutExpired as error:
                Path(str(prefix) + ".log").write_text(str(error.stdout) + "\n" + str(error.stderr) + "\nexit=timeout\n")
                raise
            parent_wall = (time.perf_counter() - started) * 1000
            Path(str(prefix) + ".log").write_text(result.stdout + result.stderr + f"\nexit={result.returncode}\n")
            failures.extend(accepted.check_inputs(checked))
            record = {}
            try:
                if args.phase == "pressure":
                    lines = [line for line in result.stdout.splitlines() if line.startswith("vertical=")]
                    if len(lines) != 1:
                        raise ValueError("Expected one automatic-pressure record")
                    record = dict(field.split("=", 1) for field in lines[0].split())
                    record = {key: (value if key in ("vertical", "collector", "control", "module_sha256", "exhausted") else float(value))
                              for key, value in record.items()}
                    bindings = [json.loads(line[len("pressure_binding="):]) for line in result.stdout.splitlines() if line.startswith("pressure_binding=")]
                    if len(bindings) != 1:
                        raise ValueError("Expected one actual pressure module binding")
                    record.update(bindings[0])
                    failures.extend(validate_modules(record, frame, category, frozen["common"], module_sha))
                    if (record["vertical"] != "allocation" or record["collector"] != category
                            or record["checksum"] != 1073479680 or record["operations"] != 524288
                            or record["exhausted"] != "False" or record["module_sha256"].lower() != module_sha):
                        failures.append("Automatic-pressure workload changed")
                    if category == "LXR" and (record["collections"] <= 0 or record["nursery"] != int(nursery)
                                             or record["mature"] != int(mature) or record["admitted_bytes"] != 24 * 2**20):
                        failures.append("Automatic pressure did not use the owning mode/admission or trigger collection")
                else:
                    records = [json.loads(line[len("log_study="):]) for line in result.stdout.splitlines() if line.startswith("log_study=")]
                    if len(records) != 1:
                        raise ValueError("Expected one log-study record")
                    record = records[0]
                    failures.extend(accepted.validate(record, input_name, count, category, nursery, mature,
                                                       frame, module_sha, frozen["common"]))
                    if category == "LXR" and record["live"]["managed_admitted_bytes"] != 128 * 2**20:
                        failures.append("Fixed admission changed")
                    if args.phase == "diagnostic":
                        before, live = record["before"]["packet_counters"], record["live"]["packet_counters"]
                        if live["collections"] - before["collections"] != 8 or live["full"] - before["full"] != 8:
                            failures.append("Diagnostic snapshot interval has more or fewer than eight full collections")
            except (ValueError, KeyError, TypeError) as error:
                failures.append(str(error))
            valid = result.returncode == 0 and not failures
            row = dict(index=index, arm=label, input=input_name, count=count, sample=sample,
                       exit=result.returncode, valid=valid, parent_wall_ms=parent_wall,
                       raw=str(prefix) + ".log", record=json.dumps(record))
            writer.writerow(row)
            stream.flush()
            rows.append(row)
            print(args.phase, index, label, count, result.returncode, valid, flush=True)
            if not valid:
                write(Path(str(prefix) + ".failure.json"), failures)
                raise RuntimeError(f"Failure retained, no retry: {prefix}: {failures}")
    summaries = []
    for count, input_name in sorted({(row["count"], row["input"]) for row in rows}):
        for arm in sorted({row["arm"] for row in rows}):
            records = [json.loads(row["record"]) for row in rows if row["count"] == count and row["input"] == input_name and row["arm"] == arm]
            if not records:
                continue
            summary = dict(count=count, input=input_name, arm=arm, n=len(records))
            for key in ("elapsed_ms", "cpu_ms", "pause_ms"):
                summary[key] = statistics.median(record[key] for record in records)
            if args.phase == "pressure":
                summary["collections"] = statistics.median(record["collections"] for record in records)
                summary["rss_bytes"] = statistics.median(record["working_set_bytes"] for record in records)
                summary["private_bytes"] = statistics.median(record["private_bytes"] for record in records)
            else:
                summary["full_sum_ms"] = statistics.median(sum(record["full_call_ms"]) for record in records)
                summary["rss_bytes"] = statistics.median(record["live"]["rss_bytes"] for record in records)
                summary["private_bytes"] = statistics.median(record["live"]["private_bytes"] for record in records)
                calls = sorted(value for record in records for value in record["full_call_ms"])
                position = (len(calls) - 1) * 0.95
                low = int(position)
                summary["call_p95_ms"] = calls[low] + (calls[min(low + 1, len(calls) - 1)] - calls[low]) * (position - low)
            summaries.append(summary)
    with (output / "summary.csv").open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(summaries[0]))
        writer.writeheader()
        writer.writerows(summaries)


if __name__ == "__main__":
    main()
