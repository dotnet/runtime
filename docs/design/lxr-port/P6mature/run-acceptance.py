#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Selected-host three-mode lifetimes; preserve commands, settings and every outcome."""

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
        ("lifetime-nonmoving", [], "0", "0", {}),
        ("lifetime-nursery", [], "1", "0", {}),
        ("lifetime-mature", [], "1", "1", {}),
        ("oom", ["--oom"], "1", "1", {}),
        ("birth", ["--p8-birth"], "1", "1", {}),
        ("source-reuse", ["--lifetime-reuse"], "1", "1", {}),
        ("nursery-on", ["--nursery"], "1", "0", {"DOTNET_LXRNoMutatorLineRecycling": "1"}),
        ("nursery-off", ["--nursery"], "0", "0", {"DOTNET_LXRNoMutatorLineRecycling": "1"}),
        ("weighted", ["--nursery"], "1", "0", {"DOTNET_LXRNoMutatorLineRecycling": "1",
                                            "DOTNET_LXRSurvivalPredictorWeighted": "1"}),
        ("harmonic", ["--nursery"], "1", "0", {"DOTNET_LXRNoMutatorLineRecycling": "1",
                                            "DOTNET_LXRSurvivalPredictorHarmonicMean": "1"}),
        ("default-policy", ["--nursery-default"], "1", "0", {}),
        ("mature", ["--mature"], "1", "1", {}),
        ("retention-bytes", ["--retention-bytes"], "1", "1", {}),
        ("retention-references", ["--retention-references"], "1", "1", {}),
    ]
    base = {key: value for key, value in os.environ.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcServer="0", DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    with (args.output / "results.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=("configuration", "case", "nursery", "mature", "exit_code",
                                                   "checks", "fixture_sha256", "collector_sha256", "raw"))
        writer.writeheader()
        for config in ("Release", "Debug"):
            framework = (args.artifacts / f"{args.stage}-{config}-{platform}").resolve(strict=True)
            module = framework / collector
            for label, flags, nursery, mature, settings in cases:
                child = dict(base)
                child.update(DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery,
                             DOTNET_LXREvacuateMature=mature)
                if not label.startswith("retention-") and label != "mature":
                    child["DOTNET_LXRLegacyCapacity"] = "1"
                child.update(settings)
                command = [str(framework / core), str(fixture), *flags, str(module)]
                if flags == ["--nursery"]:
                    command.append("off" if label == "nursery-off" else "on")
                raw = args.output / f"{config}-{label}.log"
                binding = {"command": command, "settings": {key: value for key, value in child.items()
                                                          if key.upper().startswith("DOTNET_")},
                           "fixture_sha256": digest(fixture), "collector_sha256": digest(module)}
                try:
                    result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=240)
                except subprocess.TimeoutExpired as error:
                    raw.write_text(json.dumps(binding) + "\n" + str(error.stdout) + "\n" +
                                   str(error.stderr) + "\nexit=timeout\n", encoding="utf-8")
                    raise RuntimeError(f"Timeout retained: {raw}") from error
                raw.write_text(json.dumps(binding) + "\n" + result.stdout + result.stderr +
                               f"\nexit={result.returncode}\n", encoding="utf-8")
                matches = re.findall(r"PASS: (\d+)", result.stdout)
                writer.writerow({"configuration": config, "case": label, "nursery": nursery, "mature": mature,
                                 "exit_code": result.returncode, "checks": matches[-1] if matches else "",
                                 **{key: binding[key] for key in ("fixture_sha256", "collector_sha256")},
                                 "raw": str(raw)})
                output.flush()
                print(config, label, result.returncode, matches[-1] if matches else "", flush=True)
                if result.returncode != 0 or len(matches) != 1:
                    raise RuntimeError(f"Failure retained: {raw}")


if __name__ == "__main__":
    main()
