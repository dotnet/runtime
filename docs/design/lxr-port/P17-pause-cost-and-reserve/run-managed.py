#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Run the managed LXR qualification (scope5 fixture) against a changed collector module.

Every process is recorded, also failures. The runtime layout (corerun, coreclr, CoreLib) comes from the
accepted e8d2d23 frame; only the collector module is replaced through DOTNET_GCPath.
usage: run-managed.py --frames Release=DIR,Debug=DIR --modules Release=FILE,Debug=FILE --fixtures DIR --output DIR
"""

import argparse
import csv
import os
from pathlib import Path
import re
import subprocess

FULL_CASES = ("full", "finalizer-closure", "dependent")
EXTRA_CASES = ("weak-finalizer", "reregister", "weak-identity", "churn", "weak-interior-statics",
               "refcounted", "dependent-null", "finalizer-reentrant", "collectible-cycles")
# P17 S6: finalization and weak references with the registry. They keep many objects alive until a tracing pause, so
# they run with a larger heap than the other cases.
S6_CASES = ("finalizer-churn", "finalizer-churn-concurrent", "weak-churn", "resurrection", "prompt-collect",
             "pooled-reregister")
S6_HEAP = dict(DOTNET_LXRImmixInitialMB="192", DOTNET_LXRLOSInitialMB="32", DOTNET_LXRManagedHeapHardLimitMB="224")
# P17 S7: pinning. Natural collections run while pinned handles, `fixed` locals and pinned-object-heap arrays live.
S7_CASES = ("pin-handles-churn", "fixed-pressure", "poh-churn", "pin-finalizers-weak", "pin-concurrent-trace")
S7_HEAP = dict(DOTNET_LXRImmixInitialMB="96", DOTNET_LXRLOSInitialMB="32", DOTNET_LXRManagedHeapHardLimitMB="128")
# P21 (D1): references cleared by precompiled framework code are logged (the framework is ReadyToRun compiled).
R2R_CASES = ("r2r-stores",)
# P22 (D2): the stock bump-pointer helpers. Interior roots into young objects, allocation from many threads while pauses
# stop them, and sizes around a line and a block.
D2_CASES = ("young-interior", "alloc-storm", "alloc-sizes")
D2_HEAP = dict(DOTNET_LXRImmixInitialMB="192", DOTNET_LXRLOSInitialMB="64", DOTNET_LXRManagedHeapHardLimitMB="256")
MODES = (("nonmoving", "0", "0"), ("nursery", "1", "0"), ("mature", "1", "1"))


def pairs(text):
    return dict(item.split("=", 1) for item in text.split(","))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--frames", required=True)
    parser.add_argument("--modules", required=True)
    parser.add_argument("--fixtures", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--timeout", type=int, default=300)
    args = parser.parse_args()
    frames, modules = pairs(args.frames), pairs(args.modules)
    windows = os.name == "nt"
    host = "corerun.exe" if windows else "corerun"
    args.output.mkdir(parents=True, exist_ok=True)
    base = {k: v for k, v in os.environ.items() if not k.upper().startswith(("DOTNET_", "COMPLUS_"))}
    base.update(DOTNET_ReadyToRun=os.environ.get("LXR_READYTORUN", "1"), DOTNET_TieredCompilation="0", DOTNET_gcServer="0",
                DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2", DOTNET_LXRLegacyCapacity="1")
    failures = 0
    with (args.output / "results.csv").open("w", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(("native", "managed", "mode", "case", "exit", "checks"))
        for native in ("Release", "Debug"):
            frame = Path(frames[native])
            module = modules[native]
            for managed in ("Release", "Debug"):
                fixture = args.fixtures / f"fixture-scope5-{managed}" / "runtime-tests.dll"
                cases = FULL_CASES + (EXTRA_CASES + S6_CASES + S7_CASES + R2R_CASES + D2_CASES if managed == "Release" else ())
                for mode, nursery, mature in MODES:
                    for case in cases:
                        env = dict(base, DOTNET_GCPath=module, DOTNET_LXREvacuateNursery=nursery,
                                   DOTNET_LXREvacuateMature=mature)
                        if case in S6_CASES:
                            env.update(S6_HEAP)
                        if case in S7_CASES:
                            env.update(S7_HEAP)
                        if case in D2_CASES:
                            env.update(D2_HEAP)
                        command = [str(frame / host), str(fixture)]
                        command += [module] if case == "full" else ["--p7-lifecycle", case, module]
                        log = args.output / f"{native}-{managed}-{mode}-{case}.log"
                        try:
                            result = subprocess.run(command, env=env, capture_output=True, text=True, timeout=args.timeout)
                            code, out = result.returncode, result.stdout + result.stderr
                        except subprocess.TimeoutExpired as error:
                            code, out = "timeout", str(error.stdout) + str(error.stderr)
                        log.write_text(out + f"\nexit={code}\n")
                        match = re.search(r"PASS: (\d+) ", out)
                        checks = match[1] if match else 0
                        ok = code == 0 and match is not None
                        if case == "full":
                            # P21 (D1): 100631 checks. The check "the collector is not the server collector" is gone,
                            # because a custom collector reports IsServerGC as true (the PR sets the heap type to CUSTOM).
                            ok = ok and int(checks) == 100631
                        failures += 0 if ok else 1
                        writer.writerow((native, managed, mode, case, code, checks))
                        stream.flush()
                        print(native, managed, mode, case, code, checks, "ok" if ok else "FAILED", flush=True)
    print(f"managed failures={failures}")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
