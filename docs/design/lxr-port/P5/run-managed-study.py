#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""One fixed, interleaved characterization; never retries or selects successful samples."""

import argparse
import csv
import hashlib
import os
from pathlib import Path
import statistics
import subprocess


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while chunk := stream.read(1024 * 1024):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("corerun", "test", "lxr", "coreclr", "corelib", "output"):
        parser.add_argument("--" + name, required=True, type=Path)
    args = parser.parse_args()
    for name in ("corerun", "test", "lxr", "coreclr", "corelib"):
        path = getattr(args, name).resolve(strict=True)
        setattr(args, name, path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    identities = {
        name + "_sha256": sha256(getattr(args, name))
        for name in ("corerun", "test", "coreclr", "corelib")
    }
    environment = dict(os.environ)
    for name in (
        "DOTNET_GCPath", "COMPlus_GCPath", "DOTNET_GCName", "COMPlus_GCName",
        "DOTNET_gcServer", "COMPlus_gcServer",
    ):
        environment.pop(name, None)
    environment.update({
        "DOTNET_ReadyToRun": "0",
        "DOTNET_TieredCompilation": "0",
        "DOTNET_gcConcurrent": "1",
        "DOTNET_GCHeapCount": "2",
    })
    rows = []
    with args.output.open("w", newline="", encoding="utf-8") as stream:
        writer = None
        for round_index, order in enumerate((
            ("LXR", "WKS", "Server"), ("WKS", "Server", "LXR"), ("Server", "LXR", "WKS"),
        )):
            for replicate, control in (("A", "normal"), ("B", "normal"), ("sensitivity", "injected")):
                for collector in order:
                    child_environment = dict(environment)
                    child_environment["DOTNET_gcServer"] = "1" if collector == "Server" else "0"
                    module = args.lxr if collector == "LXR" else args.coreclr
                    if collector == "LXR":
                        child_environment["DOTNET_GCPath"] = str(args.lxr)
                    command = [
                        str(args.corerun), str(args.test), "--study",
                        collector, str(module), control,
                    ]
                    result = subprocess.run(
                        command, env=child_environment, capture_output=True, text=True, timeout=180,
                    )
                    raw = args.output.with_name(
                        f"{args.output.stem}-r{round_index}-{replicate}-{collector}.log"
                    )
                    raw.write_text(
                        "command=" + repr(command) + "\n" +
                        "settings=" + repr({key: child_environment.get(key) for key in (
                            "DOTNET_GCPath", "DOTNET_gcServer", "DOTNET_gcConcurrent",
                            "DOTNET_GCHeapCount", "DOTNET_ReadyToRun", "DOTNET_TieredCompilation",
                        )}) + "\n" + result.stdout + result.stderr +
                        f"\nexit={result.returncode}\n", encoding="utf-8",
                    )
                    if result.returncode != 0:
                        raise RuntimeError(f"Study process failed ({result.returncode}); retained {raw}")
                    lines = [line for line in result.stdout.splitlines() if line.startswith("study=")]
                    if len(lines) != 1:
                        raise RuntimeError(f"Study produced {len(lines)} result rows; retained {raw}")
                    row = {
                        "round": round_index, "replicate": replicate,
                        **dict(item.split("=", 1) for item in lines[0].split()), **identities,
                    }
                    if row["study"] != collector or row["control"] != control or row["checksum"] != "49152":
                        raise RuntimeError(f"Unexpected collector/input identity; retained {raw}")
                    if writer is None:
                        writer = csv.DictWriter(stream, fieldnames=list(row))
                        writer.writeheader()
                    writer.writerow(row)
                    stream.flush()
                    rows.append(row)
    for collector in ("LXR", "WKS", "Server"):
        selected = [row for row in rows if row["study"] == collector]
        groups = {
            name: [float(row["elapsed_ms"]) for row in selected if row["replicate"] == name]
            for name in ("A", "B", "sensitivity")
        }
        normal = groups["A"] + groups["B"]
        a = statistics.median(groups["A"])
        b = statistics.median(groups["B"])
        injected = statistics.median(groups["sensitivity"])
        sensitive = min(groups["sensitivity"]) > max(normal) and injected - statistics.median(normal) >= 320
        print(
            f"{collector}: n=6 normal_ms={statistics.median(normal):.3f} "
            f"AA_A_ms={a:.3f} AA_B_ms={b:.3f} AA_delta_pct={(b / a - 1) * 100:.2f} "
            f"injected_ms={injected:.3f} sensitivity={'detected' if sensitive else 'inconclusive'}"
        )


if __name__ == "__main__":
    main()
