#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Run the natural-collection benchmark on every collector. Run inside WSL (Linux)."""

import argparse
import json
import math
import os
import random
import re
import subprocess
import sys
import time

JAVA = "/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin/java"
NET = "/root/lxr-batching-d2eccaea/after-Release-linux"
ROOT = "/root/gcbench"

# Fixed work for each workload.
WORKLOADS = {
    "binarytrees": {"size": 16, "ops": 0},
    "requests": {"size": 32768, "ops": 600000},
    "listsuffix": {"size": 4096, "ops": 800000},
}

COLLECTORS = [
    "java-lxr", "java-g1", "java-parallel", "java-serial", "java-zgc",
    "net-lxr-nonmoving", "net-lxr-nursery", "net-lxr-mature", "net-wks", "net-server",
]


def command(collector, workload, size, ops, heap_mib, mode="run"):
    args = [workload, str(size), str(ops), mode]
    env = {key: value for key, value in os.environ.items()
           if not key.upper().startswith(("DOTNET_", "COMPLUS_", "MMTK_"))}
    if collector.startswith("java-"):
        flags = {
            "java-lxr": ["-XX:+UseThirdPartyHeap"],
            "java-g1": ["-XX:+UseG1GC"],
            "java-parallel": ["-XX:+UseParallelGC"],
            "java-serial": ["-XX:+UseSerialGC"],
            "java-zgc": ["-XX:+UnlockExperimentalVMOptions", "-XX:+UseZGC"],
        }[collector]
        if collector == "java-lxr":
            env["MMTK_PLAN"] = "LXR"
            env["MMTK_VERBOSE"] = "2"
        heap = [f"-Xms{heap_mib}M", f"-Xmx{heap_mib}M"]
        return [JAVA, "-server", *flags, *heap, "-cp", f"{ROOT}/java", "GcBench", *args], env
    env.update(DOTNET_TieredCompilation="0", DOTNET_ReadyToRun="0", DOTNET_gcConcurrent="1",
               DOTNET_GCHeapCount="2", DOTNET_gcServer="1" if collector == "net-server" else "0")
    if heap_mib:
        env["DOTNET_GCHeapHardLimit"] = format(heap_mib * 1048576, "x")
    if collector.startswith("net-lxr-"):
        mode_name = collector.split("-")[-1]
        los = max(8, heap_mib // 8)
        env.pop("DOTNET_GCHeapHardLimit", None)
        env.update(DOTNET_GCPath=f"{NET}/libclrlxrgc.so", DOTNET_LXRLegacyCapacity="1",
                   DOTNET_LXREvacuateNursery="0" if mode_name == "nonmoving" else "1",
                   DOTNET_LXREvacuateMature="1" if mode_name == "mature" else "0",
                   DOTNET_LXRImmixInitialMB=str(heap_mib - los), DOTNET_LXRLOSInitialMB=str(los),
                   DOTNET_LXRManagedHeapHardLimitMB=str(heap_mib))
    return [f"{NET}/corerun", f"{ROOT}/net/gcbench.dll", *args], env


PAUSE = re.compile(r"GC\((\d+)\) (\w+) finished\..*?pause-time=([0-9.]+)ms")


def java_lxr_pauses(text):
    """Collection kinds and pause time from the Java LXR's own log (the JVM beans report none)."""
    kinds, total, count, longest = {}, 0.0, 0, 0.0
    start = text.find("BENCH_MARK start")
    end = text.find("BENCH_MARK end")
    if start >= 0 and end > start:
        text = text[start:end]
    for match in PAUSE.finditer(text):
        kind, pause = match.group(2), float(match.group(3))
        kinds[kind] = kinds.get(kind, 0) + 1
        total += pause
        count += 1
        longest = max(longest, pause)
    return {"gc_count": count, "gc_ms": int(total), "lxr_pauses": count, "lxr_full": kinds.get("Full", 0),
            "lxr_initial_mark": kinds.get("InitialMark", 0), "lxr_final_mark": kinds.get("FinalMark", 0),
            "lxr_longest_pause_ms": longest}


def execute(collector, workload, size, ops, heap_mib, timeout, mode="run"):
    cmd, env = command(collector, workload, size, ops, heap_mib, mode)
    started = time.time()
    try:
        result = subprocess.run(cmd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, timeout=timeout)
        rc, out, err = result.returncode, result.stdout, ""
    except subprocess.TimeoutExpired as error:
        rc, out, err = "timeout", (error.stdout or b"").decode(errors="replace") if isinstance(error.stdout, bytes) else (error.stdout or ""), "timeout"
    record = {"collector": collector, "workload": workload, "heap_mib": heap_mib, "exit": rc,
              "wall_s": round(time.time() - started, 3)}
    lines = [line for line in out.splitlines() if line.startswith("{")]
    if lines and rc == 0:
        record.update(json.loads(lines[-1]))
        if collector == "java-lxr":
            record.update(java_lxr_pauses(out + "\n" + err))
    else:
        record["status"] = "failed"
        record["stderr_tail"] = "\n".join((out + "\n" + (err or "")).splitlines()[-8:])
        record["stdout_tail"] = "\n".join(out.splitlines()[-3:])
    return record


def succeeds(collector, workload, size, ops, heap_mib):
    return execute(collector, workload, size, ops, heap_mib, 300).get("elapsed_ms") is not None


def minimum_heap(collector, workload, size, ops):
    """Smallest heap (MiB) in which the stock collector finishes a shortened run."""
    short = max(1, ops // 20)
    low, high = 4, 2048
    if not succeeds(collector, workload, size, short, high):
        raise RuntimeError(f"{collector} {workload} fails even at {high} MiB")
    while high - low > 1:
        middle = (low + high) // 2
        if succeeds(collector, workload, size, short, middle):
            high = middle
        else:
            low = middle
    return high


def calibrate(args):
    sizes = {}
    for workload, spec in WORKLOADS.items():
        item = {"size": spec["size"], "ops": spec["ops"]}
        item["min_java"] = minimum_heap("java-serial", workload, spec["size"], spec["ops"])
        item["min_net"] = minimum_heap("net-wks", workload, spec["size"], spec["ops"])
        sizes[workload] = item
        print(workload, item, flush=True)
    with open(args.output, "w") as stream:
        json.dump(sizes, stream, indent=2)


def heap_for(spec, runtime, factor):
    return int(math.ceil(spec["min_java" if runtime == "java" else "min_net"] * factor))


def run(args):
    with open(args.sizes) as stream:
        sizes = json.load(stream)
    collectors = args.collectors.split(",") if args.collectors else COLLECTORS
    workloads = args.workloads.split(",") if args.workloads else list(WORKLOADS)
    factors = [float(value) for value in args.factors.split(",")]
    cells = [(w, f) for w in workloads for f in factors]
    with open(args.output, "a") as stream:
        for repetition in range(args.reps):
            for workload, factor in cells:
                order = list(collectors)
                random.Random(f"{workload}{factor}{repetition}").shuffle(order)
                for collector in order:
                    spec = sizes[workload]
                    work = WORKLOADS[workload]
                    runtime = "java" if collector.startswith("java-") else "net"
                    heap = heap_for(spec, runtime, factor)
                    record = execute(collector, workload, work["size"], work["ops"], heap, args.timeout)
                    record.update(repetition=repetition, factor=factor)
                    stream.write(json.dumps(record) + "\n")
                    stream.flush()
                    ok = record.get("elapsed_ms")
                    print(f"{repetition} {workload} x{factor} {collector} heap={heap}M "
                          f"{'%.0f ms' % ok if ok else 'FAILED exit=%s' % record['exit']}", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="verb", required=True)
    cal = sub.add_parser("calibrate")
    cal.add_argument("--output", default=f"{ROOT}/sizes.json")
    runner = sub.add_parser("run")
    runner.add_argument("--sizes", default=f"{ROOT}/sizes.json")
    runner.add_argument("--output", required=True)
    runner.add_argument("--collectors")
    runner.add_argument("--workloads")
    runner.add_argument("--factors", default="3,6")
    runner.add_argument("--reps", type=int, default=3)
    runner.add_argument("--timeout", type=int, default=300)
    args = parser.parse_args()
    {"calibrate": calibrate, "run": run}[args.verb](args)


if __name__ == "__main__":
    sys.exit(main())
