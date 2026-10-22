#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""P17 matrix: the port collectors on the P14 programs, accepted build and changed build interleaved.

Run inside WSL. The program is the unchanged P14 gcbench.dll (same sources, same checksums).
Every run is written to the output file, also failed and timed-out runs.
"""

import argparse
import json
import math
import os
import random
import subprocess
import sys
import time

ROOT = "/root/gcbench"
WORKLOADS = {
    "binarytrees": {"size": 16, "ops": 0},
    "requests": {"size": 32768, "ops": 600000},
    "listsuffix": {"size": 4096, "ops": 800000},
}
MODES = {"nonmoving": ("0", "0"), "nursery": ("1", "0"), "mature": ("1", "1")}


def command(frame, mode, workload, size, ops, heap_mib, program, extra_env, no_limit=None):
    los = max(8, heap_mib // 8)
    nursery, mature = MODES[mode]
    env = {"PATH": "/usr/bin:/bin"}
    env.update(DOTNET_TieredCompilation="0", DOTNET_ReadyToRun=os.environ.get("LXR_READYTORUN", "0"), DOTNET_gcConcurrent="1",
               DOTNET_GCHeapCount="2", DOTNET_gcServer="0",
               DOTNET_GCPath=f"{frame}/libclrlxrgc.so", DOTNET_LXRLegacyCapacity="1",
               DOTNET_LXREvacuateNursery=nursery, DOTNET_LXREvacuateMature=mature,
               DOTNET_LXRImmixInitialMB=str(heap_mib - los), DOTNET_LXRLOSInitialMB=str(los),
               DOTNET_LXRManagedHeapHardLimitMB=str(heap_mib))
    if no_limit is not None:
        # P17 S5b: no hard limit. The heap size is the initial admitted size only; the reservation is larger.
        del env["DOTNET_LXRManagedHeapHardLimitMB"]
        env["DOTNET_LXRLegacyCapacity"] = "0"
        env["DOTNET_LXRImmixReserveMB"] = str(no_limit[0])
        env["DOTNET_LXRLOSReserveMB"] = str(no_limit[1])
    env.update(extra_env)
    return [f"{frame}/corerun", program, workload, str(size), str(ops), "run"], env


def execute(frame, mode, workload, heap_mib, timeout, program, extra_env, no_limit=None):
    spec = WORKLOADS[workload]
    cmd, env = command(frame, mode, workload, spec["size"], spec["ops"], heap_mib, program, extra_env, no_limit)
    started = time.time()
    try:
        result = subprocess.run(cmd, env=env, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                text=True, timeout=timeout)
        rc, out = result.returncode, result.stdout
    except subprocess.TimeoutExpired as error:
        rc = "timeout"
        out = error.stdout.decode(errors="replace") if isinstance(error.stdout, bytes) else (error.stdout or "")
    record = {"mode": mode, "workload": workload, "heap_mib": heap_mib, "exit": rc,
              "wall_s": round(time.time() - started, 3)}
    lines = [line for line in out.splitlines() if line.startswith("{")]
    if lines and rc == 0:
        record.update(json.loads(lines[-1]))
    else:
        record["status"] = "failed"
        record["output_tail"] = "\n".join(out.splitlines()[-40:])
    return record


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--builds", required=True, help="label=frame-dir[|NAME=VALUE...],label=frame-dir (frame holds corerun and libclrlxrgc.so)")
    parser.add_argument("--output", required=True)
    parser.add_argument("--sizes", default=f"{ROOT}/sizes.json")
    parser.add_argument("--program", default=f"{ROOT}/net/gcbench.dll")
    parser.add_argument("--modes", default="nonmoving,nursery,mature")
    parser.add_argument("--workloads", default="binarytrees,requests,listsuffix")
    parser.add_argument("--factors", default="2,4,8")
    parser.add_argument("--reps", type=int, default=3)
    parser.add_argument("--rep-indices", default="", help="run only these repetition indices, for example 1 or 0,2")
    parser.add_argument("--timeout", type=int, default=400)
    parser.add_argument("--env", action="append", default=[], help="NAME=VALUE passed to every run")
    parser.add_argument("--seed", default="p17")
    parser.add_argument("--no-hard-limit", default="", help="IMMIX_RESERVE_MB,LOS_RESERVE_MB: run without a hard limit, "
                        "with the heap size as the initial admitted size and these reservations")
    args = parser.parse_args()
    no_limit = tuple(int(v) for v in args.no_hard_limit.split(",")) if args.no_hard_limit else None
    builds, build_env = {}, {}
    for item in args.builds.split(","):
        label, spec = item.split("=", 1)
        frame, *settings = spec.split("|")
        builds[label] = frame
        build_env[label] = dict(setting.split("=", 1) for setting in settings)
    extra = dict(item.split("=", 1) for item in args.env)
    with open(args.sizes) as stream:
        sizes = json.load(stream)
    modes = args.modes.split(",")
    factors = [float(value) for value in args.factors.split(",")]
    with open(args.output, "a") as stream:
        indices = [int(v) for v in args.rep_indices.split(",")] if args.rep_indices else range(args.reps)
        for repetition in indices:
            for workload in args.workloads.split(","):
                for factor in factors:
                    heap = int(math.ceil(sizes[workload]["min_net"] * factor))
                    order = [(label, mode) for label in builds for mode in modes]
                    random.Random(f"{args.seed}{workload}{factor}{repetition}").shuffle(order)
                    for label, mode in order:
                        record = execute(builds[label], mode, workload, heap, args.timeout, args.program, {**extra, **build_env[label]}, no_limit)
                        record.update(build=label, repetition=repetition, factor=factor)
                        stream.write(json.dumps(record) + "\n")
                        stream.flush()
                        ok = record.get("elapsed_ms")
                        print(f"{repetition} {workload} x{factor} {label} {mode} heap={heap}M "
                              f"{'%.0f ms' % ok if ok else 'FAILED exit=%s' % record['exit']}", flush=True)


if __name__ == "__main__":
    sys.exit(main())
