#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Real-handle/finalizer lifecycle qualification, with every process retained."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifacts", type=Path, required=True)
    parser.add_argument("--stage", required=True)
    parser.add_argument("--fixture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--case", action="append", choices=(
        "finalizer-closure", "dependent", "weak-finalizer", "reregister",
        "weak-identity", "churn", "weak-interior-statics", "refcounted", "dependent-null", "finalizer-reentrant"))
    args = parser.parse_args()
    cases = args.case or [
        "finalizer-closure", "dependent", "weak-finalizer", "reregister",
        "weak-identity", "churn", "weak-interior-statics", "refcounted", "dependent-null", "finalizer-reentrant"]
    fixture = args.fixture.resolve(strict=True)
    args.output.mkdir(parents=True, exist_ok=False)
    windows = os.name == "nt"
    platform = "windows" if windows else "linux"
    core = "corerun.exe" if windows else "corerun"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    coreclr = "coreclr.dll" if windows else "libcoreclr.so"
    base = {key: value for key, value in os.environ.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    with (args.output / "results.csv").open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=(
            "configuration", "category", "case", "exit_code", "checks",
            "fixture_sha256", "module_sha256", "raw"))
        writer.writeheader()
        for configuration in ("Release", "Debug"):
            framework = (args.artifacts / f"{args.stage}-{configuration}-{platform}").resolve(strict=True)
            for category, nursery, mature in (
                    ("nonmoving", "0", "0"), ("nursery", "1", "0"), ("mature", "1", "1"),
                    ("WKS", None, None), ("Server", None, None)):
                for case in cases:
                    if nursery is None and case != "finalizer-closure":
                        continue
                    child = dict(base)
                    child["DOTNET_gcServer"] = "1" if category == "Server" else "0"
                    module = framework / (collector if nursery is not None else coreclr)
                    if nursery is not None:
                        child.update(DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery,
                                     DOTNET_LXREvacuateMature=mature, DOTNET_LXRLegacyCapacity="1")
                    command = [str(framework / core), str(fixture), "--p7-lifecycle", case, str(module)]
                    raw = args.output / f"{configuration}-{category}-{case}.log"
                    binding = {"command": command,
                               "settings": {key: value for key, value in child.items() if key.startswith("DOTNET_")},
                               "fixture_sha256": hashlib.sha256(fixture.read_bytes()).hexdigest(),
                               "module_sha256": hashlib.sha256(module.read_bytes()).hexdigest()}
                    try:
                        result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
                    except subprocess.TimeoutExpired as error:
                        raw.write_text(json.dumps(binding) + "\n" + str(error.stdout) + "\n" +
                                       str(error.stderr) + "\nexit=timeout\n")
                        raise RuntimeError(f"Timeout retained: {raw}") from error
                    raw.write_text(json.dumps(binding) + "\n" + result.stdout + result.stderr +
                                   f"\nexit={result.returncode}\n")
                    matches = re.findall(r"PASS: (\d+) lifecycle checks", result.stdout)
                    writer.writerow({"configuration": configuration, "category": category, "case": case,
                                     "exit_code": result.returncode, "checks": matches[-1] if matches else None,
                                     "fixture_sha256": binding["fixture_sha256"],
                                     "module_sha256": binding["module_sha256"], "raw": str(raw)})
                    stream.flush()
                    print(configuration, category, case, result.returncode, matches, flush=True)
                    if result.returncode != 0 or len(matches) != 1:
                        raise RuntimeError(f"Failure retained: {raw}")


if __name__ == "__main__":
    main()
