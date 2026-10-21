#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Startup/configuration controls through both environment and actual JSON hosting."""

import argparse
import csv
import json
import os
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--framework", required=True, type=Path)
    parser.add_argument("--json-host", required=True, type=Path)
    parser.add_argument("--test", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    windows = os.name == "nt"
    framework = args.framework.resolve(strict=True)
    test = args.test.resolve(strict=True)
    module = framework / ("clrlxrgc.dll" if windows else "libclrlxrgc.so")
    corerun = framework / ("corerun.exe" if windows else "corerun")
    host = args.json_host.resolve(strict=True) / ("dotnet.exe" if windows else "dotnet")
    args.output.mkdir(parents=True, exist_ok=True)
    environment = dict(os.environ)
    for key in tuple(environment):
        if key.upper().startswith(("DOTNET_LXR", "COMPLUS_LXR", "DOTNET_GCHEAPHARDLIMIT", "COMPLUS_GCHEAPHARDLIMIT")):
            del environment[key]
        elif key.upper() in ("DOTNET_GCPATH", "COMPLUS_GCPATH", "DOTNET_GCNAME", "COMPLUS_GCNAME"):
            del environment[key]
    environment.update({
        "DOTNET_GCPath": str(module), "DOTNET_ReadyToRun": "0",
        "DOTNET_TieredCompilation": "0", "DOTNET_gcServer": "0",
    })
    cases = [
        ("default", {}, {}, True, "mutation"),
        ("decimal-managed-64", {"DOTNET_LXRManagedHeapHardLimitMB": "64"}, {}, True, "growth-hard"),
        ("native-plane-256", {
            "DOTNET_LXRNativePlaneLimitMB": "256", "DOTNET_LXRManagedHeapHardLimitMB": "64",
        }, {}, True, "growth-hard"),
        ("native-plane-too-small", {"DOTNET_LXRNativePlaneLimitMB": "128"}, {}, False, "mutation"),
        ("zero-initial", {"DOTNET_LXRImmixInitialMB": "0"}, {}, False, "mutation"),
        ("malformed", {"DOTNET_LXRImmixReserveMB": "garbage"}, {}, False, "mutation"),
        ("empty", {"DOTNET_LXRYoungLimitMB": ""}, {}, False, "mutation"),
        ("negative", {"DOTNET_LXRManagedHeapHardLimitMB": "-1"}, {}, False, "mutation"),
        ("overflow", {"DOTNET_LXRManagedHeapHardLimitMB": "18446744073709551616"}, {}, False, "mutation"),
        ("decimal-not-hex", {"DOTNET_LXRManagedHeapHardLimitMB": "0x40"}, {}, False, "mutation"),
        ("conflicting-legacy", {"DOTNET_LXRLegacyCapacity": "1", "DOTNET_LXRImmixReserveMB": "64"}, {}, False, "mutation"),
        ("unsupported-buffer", {"DOTNET_LXRBufferSize": "2048"}, {}, False, "mutation"),
        ("moving-nursery", {"DOTNET_LXREvacuateNursery": "1"}, {}, False, "mutation"),
        ("moving-mature", {"DOTNET_LXREvacuateMature": "1"}, {}, False, "mutation"),
        ("moving-defrag", {"DOTNET_LXRDefragHeadroomPercent": "5"}, {}, False, "mutation"),
        ("json-booleans", {}, {
            "System.GC.LXRLazyDecrements": False, "System.GC.LXRFastAllocation": False,
            "System.GC.LXRSurvivalPredictorWeighted": True,
        }, True, "mutation"),
        ("json-decimal-managed", {}, {"System.GC.LXRManagedHeapHardLimitMB": 64}, True, "growth-hard"),
        ("json-malformed", {}, {"System.GC.LXRManagedHeapHardLimitMB": "wrong"}, False, "mutation"),
        ("json-moving", {}, {"System.GC.LXREvacuateNursery": True}, False, "mutation"),
        ("json-legacy-conflict", {}, {
            "System.GC.LXRLegacyCapacity": True, "System.GC.LXRImmixReserveMB": 64,
        }, False, "mutation"),
        ("json-standard-limit", {}, {"System.GC.HeapHardLimit": 67108864}, False, "mutation"),
        ("single-coalesced", {
            "DOTNET_LXRImmixInitialMB": "1", "DOTNET_LXRImmixReserveMB": "1",
            "DOTNET_LXRLOSInitialMB": "1", "DOTNET_LXRLOSReserveMB": "2",
            "DOTNET_LXRManagedHeapHardLimitMB": "3",
        }, {}, True, "single"),
        ("r2r", {"DOTNET_ReadyToRun": "1"}, {}, False, "mutation"),
    ]
    for key in (
        "GCHeapHardLimit", "GCHeapHardLimitPercent", "GCHeapHardLimitSOH", "GCHeapHardLimitLOH",
        "GCHeapHardLimitPOH", "GCHeapHardLimitSOHPercent", "GCHeapHardLimitLOHPercent", "GCHeapHardLimitPOHPercent",
    ):
        cases.append(("standard-" + key, {"DOTNET_" + key: "1"}, {}, False, "mutation"))
    rows = []
    for label, settings, properties, succeeds, scenario in cases:
        child = dict(environment)
        child.update(settings)
        command = [str(corerun), str(test)]
        if properties:
            runtimeconfig = args.output / (label + ".runtimeconfig.json")
            runtimeconfig.write_text(json.dumps({"runtimeOptions": {
                "tfm": "net11.0", "framework": {"name": "Microsoft.NETCore.App", "version": "11.0.0"},
                "configProperties": properties,
            }}, indent=2), encoding="utf-8")
            command = [str(host), "exec", "--runtimeconfig", str(runtimeconfig.resolve()), str(test)]
        command += ["--p8-single-growth", str(module)] if scenario == "single" else [
            "--vertical", scenario, "LXR", str(module), "normal",
        ]
        result = subprocess.run(command, env=child, capture_output=True, text=True, timeout=180)
        log = args.output / (label + ".log")
        log.write_text(json.dumps({"command": command, "settings": settings, "properties": properties}) +
                       "\n" + result.stdout + result.stderr + f"\nexit={result.returncode}\n", encoding="utf-8")
        passed = result.returncode == 0 if succeeds else result.returncode != 0 and "vertical=" not in result.stdout
        rows.append({
            "case": label, "expected_success": succeeds, "exit_code": result.returncode,
            "passed": passed, "json": bool(properties),
        })
        if not passed:
            with (args.output / "summary.csv").open("w", newline="", encoding="utf-8") as stream:
                writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
                writer.writeheader()
                writer.writerows(rows)
            raise RuntimeError(f"Configuration control {label} failed; retained {log}")
    with (args.output / "summary.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    print(f"PASS: {len(rows)} environment/JSON configuration controls")


if __name__ == "__main__":
    main()
