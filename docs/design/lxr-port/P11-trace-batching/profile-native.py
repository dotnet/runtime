#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Separate native profiles; never use their times as normal benchmark data."""

import argparse
import csv
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--before-compile-commands", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    windows = os.name == "nt"
    frozen = json.loads((root / "study-inputs.json").read_text())
    spec = importlib.util.spec_from_file_location("accepted", root / "accepted-run-study.py")
    accepted = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(accepted)
    expected = {Path(path): value for path, value in frozen["expected"].items()}
    expected.update({Path(path): value for path, value in frozen["source_expected"].items()})
    errors = accepted.check_inputs(expected)
    if errors:
        raise RuntimeError(errors)
    output = root / ("profile-windows-census" if windows else "profile-linux-fp")
    output.mkdir(exist_ok=False)
    (output / "executed-profiler.py").write_bytes(Path(__file__).read_bytes())
    if not windows:
        if args.before_compile_commands is None:
            raise RuntimeError("Provide the original collector compiler-command evidence")
        proof = {}
        for stage, path in (("before", args.before_compile_commands),
                            ("after", root / "obj-linux-Release" / "compile_commands.json")):
            commands = json.loads(path.read_text())
            command = next(row["command"] for row in commands if row["file"].endswith("lxr_reference_counting.cpp"))
            flags = re.findall(r"-f(?:no-)?omit-frame-pointer", command)
            if not flags or flags[-1] != "-fno-omit-frame-pointer":
                raise RuntimeError(f"No preserved native frame-pointer proof for {stage}: {path}")
            proof[stage] = dict(path=str(path), sha256=sha(path), frame_pointer_flags=flags, command=command)
        (output / "frame-pointer-proof.json").write_text(json.dumps(proof, indent=2) + "\n")
    base = {key: value for key, value in os.environ.items() if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0", DOTNET_gcConcurrent="1",
                DOTNET_GCHeapCount="2", DOTNET_gcServer="0", DOTNET_LXRImmixInitialMB="64", DOTNET_LXRLOSInitialMB="64")
    fixture = root / "fixture-log4" / "runtime-tests.dll"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    with (output / "results.csv").open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=("stage", "mode", "exit", "valid", "raw", "record"))
        writer.writeheader()
        for mode, nursery, mature in (("nonmoving", "0", "0"), ("nursery", "1", "0"), ("mature", "1", "1")):
            for stage in ("before", "after"):
                frame = root / f"{stage}-Release-{'windows' if windows else 'linux'}"
                module = frame / collector
                env = dict(base, DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery, DOTNET_LXREvacuateMature=mature)
                command = [str(frame / ("corerun.exe" if windows else "corerun")), str(fixture), "--log-study",
                           "references", "LXR", str(module), "262144", "normal", "off"]
                prefix = output / f"{stage}-{mode}"
                if windows:
                    launch = [sys.executable, str(root / "windows-sample.py"), "--output", str(prefix) + ".samples.json",
                              "--symbols", str(frame), "--"] + command
                else:
                    launch = ["/usr/lib/linux-tools/5.15.0-198-generic/perf", "record", "-o", str(prefix) + ".data",
                              "-F", "199", "-e", "cpu-clock:u", "--call-graph", "fp", "--"] + command
                Path(str(prefix) + ".binding.json").write_text(json.dumps(dict(
                    command=launch, settings={key: value for key, value in env.items() if key.startswith("DOTNET_")},
                    source_expected=frozen["source_expected"], expected=frozen["expected"],
                    profiler_sha256=sha(Path(__file__))), indent=2) + "\n")
                failures = accepted.check_inputs(expected)
                if failures:
                    raise RuntimeError(failures)
                try:
                    result = subprocess.run(launch, env=env, capture_output=True, text=True, timeout=240)
                except subprocess.TimeoutExpired as error:
                    Path(str(prefix) + ".log").write_text(str(error.stdout) + "\n" + str(error.stderr) + "\nexit=timeout\n")
                    raise
                Path(str(prefix) + ".log").write_text(result.stdout + result.stderr + f"\nexit={result.returncode}\n")
                records = [json.loads(line[len("log_study="):]) for line in result.stdout.splitlines() if line.startswith("log_study=")]
                failures.extend(accepted.check_inputs(expected))
                if len(records) != 1:
                    raise RuntimeError(f"Missing fixture output, raw retained: {prefix}")
                record = records[0]
                failures.extend(accepted.validate(record, "references", 262144, "LXR", nursery, mature,
                                                   frame, frozen["expected"][str(module)], frozen["common"]))
                valid = result.returncode == 0 and not failures
                writer.writerow(dict(stage=stage, mode=mode, exit=result.returncode, valid=valid,
                                     raw=str(prefix) + ".log", record=json.dumps(record)))
                stream.flush()
                print(stage, mode, result.returncode, valid, flush=True)
                if not valid:
                    Path(str(prefix) + ".failure.json").write_text(json.dumps(failures, indent=2) + "\n")
                    raise RuntimeError(f"Profile failure retained without retry: {prefix}")


if __name__ == "__main__":
    main()
