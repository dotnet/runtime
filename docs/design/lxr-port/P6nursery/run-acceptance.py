#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Retain every selected-host process; stop on an unexpected result without retries."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifacts", type=Path, required=True)
    parser.add_argument("--stage", required=True)
    parser.add_argument("--fixture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=False)
    fixture = args.fixture.resolve(strict=True)
    windows = os.name == "nt"
    platform = "windows" if windows else "linux"
    core = "corerun.exe" if windows else "corerun"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    cases = [
        ("lifetime-on", [], {}),
        ("lifetime-off", [], {"DOTNET_LXREvacuateNursery": "0"}),
        ("oom", ["--oom"], {}),
        ("birth", ["--p8-birth"], {}),
        ("source-reuse", ["--lifetime-reuse"], {}),
        ("nursery-on", ["--nursery"], {"DOTNET_LXRNoMutatorLineRecycling": "1"}),
        ("nursery-off", ["--nursery"], {"DOTNET_LXRNoMutatorLineRecycling": "1", "DOTNET_LXREvacuateNursery": "0"}),
        ("weighted", ["--nursery"], {"DOTNET_LXRNoMutatorLineRecycling": "1", "DOTNET_LXRSurvivalPredictorWeighted": "1"}),
        ("harmonic", ["--nursery"], {"DOTNET_LXRNoMutatorLineRecycling": "1", "DOTNET_LXRSurvivalPredictorHarmonicMean": "1"}),
        ("default-policy", ["--nursery-default"], {}),
        ("retention-bytes", ["--retention-bytes"], {}),
        ("retention-references", ["--retention-references"], {}),
    ]
    base = {key: value for key, value in os.environ.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcServer="0", DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    with (args.output / "results.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=("configuration", "case", "exit_code", "checks",
                                                   "fixture_sha256", "collector_sha256", "raw"))
        writer.writeheader()
        for config in ("Release", "Debug"):
            framework = (args.artifacts / f"{args.stage}-{config}-{platform}").resolve(strict=True)
            module = framework / collector
            for label, flags, settings in cases:
                child = dict(base)
                child["DOTNET_GCPath"] = str(module)
                if not label.startswith("retention-"):
                    child["DOTNET_LXRLegacyCapacity"] = "1"
                child.update(settings)
                command = [str(framework / core), str(fixture), *flags, str(module)]
                if flags == ["--nursery"]:
                    command.append("off" if label == "nursery-off" else "on")
                raw = args.output / f"{config}-{label}.log"
                record = {"command": command, "settings": {key: value for key, value in child.items()
                                                          if key.upper().startswith("DOTNET_")},
                          "fixture_sha256": digest(fixture), "collector_sha256": digest(module)}
                try:
                    result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
                except subprocess.TimeoutExpired as error:
                    raw.write_text(json.dumps(record) + "\n" + str(error.stdout) + "\n" +
                                   str(error.stderr) + "\nexit=timeout\n", encoding="utf-8")
                    raise RuntimeError(f"Timeout retained: {raw}") from error
                raw.write_text(json.dumps(record) + "\n" + result.stdout + result.stderr +
                               f"\nexit={result.returncode}\n", encoding="utf-8")
                matches = re.findall(r"PASS: (\d+)", result.stdout)
                row = {"configuration": config, "case": label, "exit_code": result.returncode,
                       "checks": matches[-1] if matches else "", **{key: record[key] for key in
                                                                  ("fixture_sha256", "collector_sha256")},
                       "raw": str(raw)}
                writer.writerow(row)
                output.flush()
                print(config, label, result.returncode, row["checks"], flush=True)
                if result.returncode != 0 or len(matches) != 1:
                    raise RuntimeError(f"Failure retained: {raw}")


if __name__ == "__main__":
    main()
