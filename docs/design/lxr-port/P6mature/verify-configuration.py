#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Actual environment/JSON startup controls; preserve every result and module binding."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import subprocess


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--framework", type=Path, required=True)
    parser.add_argument("--json-host", type=Path, required=True)
    parser.add_argument("--fixture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=False)
    framework = args.framework.resolve(strict=True)
    fixture = args.fixture.resolve(strict=True)
    host = args.json_host.resolve(strict=True)
    windows = os.name == "nt"
    module = framework / ("clrlxrgc.dll" if windows else "libclrlxrgc.so")
    core = framework / ("corerun.exe" if windows else "corerun")
    dotnet = host / ("dotnet.exe" if windows else "dotnet")
    for name in (("coreclr.dll", "clrjit.dll") if windows else ("libcoreclr.so", "libclrjit.so")) + \
            ("System.Private.CoreLib.dll",):
        if digest(framework / name) != digest(host / "shared/Microsoft.NETCore.App/11.0.0" / name):
            raise RuntimeError(f"JSON hosting uses a different runtime: {name}")
    cases = [
        ("default", {}, {}, True),
        ("mature-on", {"DOTNET_LXREvacuateMature": "1"}, {}, True),
        ("mature-off", {"DOTNET_LXREvacuateMature": "0"}, {}, True),
        ("headroom-zero", {"DOTNET_LXRDefragHeadroomPercent": "0"}, {}, True),
        ("headroom-max", {"DOTNET_LXRDefragHeadroomPercent": "50"}, {}, True),
        ("headroom-range", {"DOTNET_LXRDefragHeadroomPercent": "51"}, {}, False),
        ("chunk-zero", {"DOTNET_LXRChunkDefragPercent": "0"}, {}, True),
        ("chunk-max", {"DOTNET_LXRChunkDefragPercent": "100"}, {}, True),
        ("chunk-range", {"DOTNET_LXRChunkDefragPercent": "101"}, {}, False),
        ("mature-invalid", {"DOTNET_LXREvacuateMature": "2"}, {}, False),
        ("mature-empty", {"DOTNET_LXREvacuateMature": ""}, {}, False),
        ("negative", {"DOTNET_LXRDefragHeadroomPercent": "-1"}, {}, False),
        ("overflow", {"DOTNET_LXRChunkDefragPercent": "18446744073709551616"}, {}, False),
        ("malformed", {"DOTNET_LXRDefragHeadroomPercent": "wrong"}, {}, False),
        ("json-mature", {}, {"System.GC.LXREvacuateMature": True}, True),
        ("json-mature-off", {}, {"System.GC.LXREvacuateMature": False}, True),
        ("json-headroom", {}, {"System.GC.LXRDefragHeadroomPercent": 5}, True),
        ("json-headroom-range", {}, {"System.GC.LXRDefragHeadroomPercent": 51}, False),
        ("json-chunk", {}, {"System.GC.LXRChunkDefragPercent": 32}, True),
        ("json-chunk-range", {}, {"System.GC.LXRChunkDefragPercent": 101}, False),
        ("json-negative", {}, {"System.GC.LXRDefragHeadroomPercent": -1}, False),
        ("json-malformed", {}, {"System.GC.LXREvacuateMature": "wrong"}, False),
        ("json-standard-limit", {}, {"System.GC.HeapHardLimit": 67108864}, False),
        ("unsupported-pause", {"DOTNET_LXRMaxPauseMillis": "1"}, {}, False),
        ("unsupported-hugepage", {"DOTNET_LXRTransparentHugepage": "1"}, {}, False),
        ("r2r", {"DOTNET_ReadyToRun": "1"}, {}, False),
    ]
    for name in ("GCHeapHardLimit", "GCHeapHardLimitPercent", "GCHeapHardLimitSOH", "GCHeapHardLimitLOH",
                 "GCHeapHardLimitPOH", "GCHeapHardLimitSOHPercent", "GCHeapHardLimitLOHPercent", "GCHeapHardLimitPOHPercent"):
        cases.append(("standard-" + name, {"DOTNET_" + name: "1"}, {}, False))
    base = {key: value for key, value in os.environ.items()
            if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_GCPath=str(module), DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                DOTNET_gcServer="0", DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2")
    with (args.output / "results.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=("case", "expected_success", "exit_code", "passed", "json"))
        writer.writeheader()
        for label, settings, properties, expected in cases:
            child = dict(base)
            child.update(settings)
            command = [str(core), str(fixture), "--vertical", "mutation", "LXR", str(module), "normal"]
            if properties:
                config = args.output / (label + ".runtimeconfig.json")
                config.write_text(json.dumps({"runtimeOptions": {
                    "tfm": "net11.0", "framework": {"name": "Microsoft.NETCore.App", "version": "11.0.0"},
                    "configProperties": properties,
                }}), encoding="utf-8")
                command = [str(dotnet), "exec", "--runtimeconfig", str(config.resolve()), *command[1:]]
            result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
            record = {"command": command, "settings": {key: value for key, value in child.items()
                                                      if key.upper().startswith("DOTNET_")},
                      "properties": properties, "module_sha256": digest(module), "fixture_sha256": digest(fixture)}
            raw = args.output / (label + ".log")
            raw.write_text(json.dumps(record) + "\n" + result.stdout + result.stderr +
                           f"\nexit={result.returncode}\n", encoding="utf-8")
            success = result.returncode == 0 and "vertical=" in result.stdout
            rejected = result.returncode != 0 and "vertical=" not in result.stdout
            passed = success if expected else rejected
            writer.writerow({"case": label, "expected_success": expected, "exit_code": result.returncode,
                             "passed": passed, "json": bool(properties)})
            output.flush()
            if not passed:
                raise RuntimeError(f"Configuration result differs; retained {raw}")
    print(f"PASS: {len(cases)} actual environment/JSON mature configuration controls")


if __name__ == "__main__":
    main()
