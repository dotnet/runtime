#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Qualify immutable scope5 assemblies against both native configurations."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve()
    windows = os.name == "nt"
    os_name = "windows" if windows else "linux"
    collector = "clrlxrgc.dll" if windows else "libclrlxrgc.so"
    host = "corerun.exe" if windows else "corerun"
    output = root / "managed-final"
    output.mkdir(exist_ok=False)
    expected = {
        "Release": "f6d03a208922bed4fcb37fe139b980906d417287d5d7fc43d04097859dd38436",
        "Debug": "1d7690b49960a0cb0f7ec347437034eb1089b6244cd9cdb97972f840e0747861",
    }
    base = {key: value for key, value in os.environ.items() if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0", DOTNET_gcServer="0",
                DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2", DOTNET_LXRLegacyCapacity="1")
    with (output / "results.csv").open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=("native", "managed", "mode", "case", "exit", "checks", "fixture_sha256", "module_sha256", "raw"))
        writer.writeheader()
        for native in ("Release", "Debug"):
            frame = root / f"after-{native}-{os_name}"
            module = frame / collector
            module_sha = sha(module)
            common = {path: sha(path) for path in (frame / host, frame / ("coreclr.dll" if windows else "libcoreclr.so"),
                                                   frame / ("clrjit.dll" if windows else "libclrjit.so"), frame / "System.Private.CoreLib.dll")}
            for managed in ("Release", "Debug"):
                fixture = root / f"fixture-scope5-{managed}" / "runtime-tests.dll"
                if sha(fixture) != expected[managed]:
                    raise RuntimeError(f"Scope5 fixture changed: {fixture}")
                for mode, nursery, mature in (("nonmoving", "0", "0"), ("nursery", "1", "0"), ("mature", "1", "1")):
                    for case in ("full", "finalizer-closure", "dependent"):
                        env = dict(base, DOTNET_GCPath=str(module), DOTNET_LXREvacuateNursery=nursery, DOTNET_LXREvacuateMature=mature)
                        command = [str(frame / host), str(fixture)]
                        command += [str(module)] if case == "full" else ["--p7-lifecycle", case, str(module)]
                        prefix = output / f"{native}-{managed}-{mode}-{case}"
                        binding = dict(command=command, settings={key: value for key, value in env.items() if key.startswith("DOTNET_")},
                                       fixture_sha256=expected[managed], module_sha256=module_sha,
                                       common={str(path): value for path, value in common.items()})
                        Path(str(prefix) + ".binding.json").write_text(json.dumps(binding, indent=2) + "\n")
                        try:
                            result = subprocess.run(command, env=env, capture_output=True, text=True, timeout=240)
                        except subprocess.TimeoutExpired as error:
                            Path(str(prefix) + ".log").write_text(str(error.stdout) + "\n" + str(error.stderr) + "\nexit=timeout\n")
                            raise
                        Path(str(prefix) + ".log").write_text(result.stdout + result.stderr + f"\nexit={result.returncode}\n")
                        checks = re.search(r"PASS: (\d+) ", result.stdout)
                        unchanged = (sha(fixture) == expected[managed] and sha(module) == module_sha
                                     and all(sha(path) == value for path, value in common.items()))
                        valid = result.returncode == 0 and checks is not None and unchanged
                        if case == "full":
                            valid = valid and int(checks[1]) == 100632 and module_sha.upper() in result.stdout.upper()
                        writer.writerow(dict(native=native, managed=managed, mode=mode, case=case, exit=result.returncode,
                                             checks=checks[1] if checks else 0, fixture_sha256=expected[managed],
                                             module_sha256=module_sha, raw=str(prefix) + ".log"))
                        stream.flush()
                        print(native, managed, mode, case, result.returncode, checks[1] if checks else 0, valid, flush=True)
                        if not valid:
                            raise RuntimeError(f"Managed qualification failed; raw retained: {prefix}")


if __name__ == "__main__":
    main()
