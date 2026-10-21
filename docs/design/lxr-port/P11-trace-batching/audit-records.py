#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Validate the frozen local record set and create its final raw catalog."""

import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import re


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def rows(path):
    with path.open(newline="", encoding="utf-8-sig") as stream:
        return list(csv.DictReader(stream))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve()
    windows = os.name == "nt"
    os_name = "windows" if windows else "linux"
    frozen = json.loads((root / "study-inputs.json").read_text())
    for path, expected in {**frozen["expected"], **frozen["source_expected"]}.items():
        if sha(Path(path)) != expected:
            raise RuntimeError(f"Frozen comparison input changed: {path}")
    for row in json.loads((root / "module-identities.json").read_text()):
        if sha(Path(row["module"])) != row["module_sha256"] or sha(Path(row["companion"])) != row["companion_sha256"] or not row["matched"]:
            raise RuntimeError("Frozen native module/symbol binding changed")
    counts = {}
    for phase, expected in (("timing", 72), ("pressure", 16), ("diagnostic", 24)):
        observations = rows(root / phase / "results.csv")
        if len(observations) != expected or any(row["exit"] != "0" or row["valid"] != "True" for row in observations):
            raise RuntimeError(f"Incomplete or invalid phase: {phase}")
        for row in observations:
            record = json.loads(row["record"])
            if phase != "pressure":
                if record["collections"] != 8 or record["primary_full_calls"] != 8 or len(record["full_call_ms"]) != 8:
                    raise RuntimeError("Forced-full workload shape changed")
            if phase == "diagnostic":
                initial, live = record["before"]["packet_counters"], record["live"]["packet_counters"]
                if live["full"] - initial["full"] != 8:
                    raise RuntimeError("Diagnostic interval is not eight full epochs")
        counts[phase] = len(observations)
    fixture_hashes = {
        "Release": "f6d03a208922bed4fcb37fe139b980906d417287d5d7fc43d04097859dd38436",
        "Debug": "1d7690b49960a0cb0f7ec347437034eb1089b6244cd9cdb97972f840e0747861",
    }
    modules = {row["configuration"]: row["module_sha256"]
               for row in json.loads((root / "module-identities.json").read_text()) if row["stage"] == "after"}
    managed = rows(root / "managed-final" / "results.csv")
    if len(managed) != 36:
        raise RuntimeError("Missing full/finalizer/dependent managed cell")
    for row in managed:
        if (row["exit"] != "0" or row["fixture_sha256"] != fixture_hashes[row["managed"]]
                or row["module_sha256"] != modules[row["native"]]):
            raise RuntimeError("Invalid managed full/lifecycle binding")
        if row["case"] == "full" and row["checks"] != "100632":
            raise RuntimeError("Managed full check denominator changed")
    counts["managed_initial"] = len(managed)
    for managed_config, fixture_hash in fixture_hashes.items():
        if sha(root / f"fixture-scope5-{managed_config}" / "runtime-tests.dll") != fixture_hash:
            raise RuntimeError("Scope5 assembly changed")
        extra = rows(root / f"managed-extra-{managed_config}-{os_name}" / "results.csv")
        if len(extra) != 48:
            raise RuntimeError("Missing added lifecycle cell")
        for row in extra:
            if row["exit_code"] != "0" or row["fixture_sha256"] != fixture_hash or row["module_sha256"] != modules[row["configuration"]]:
                raise RuntimeError("Invalid added lifecycle binding")
        counts[f"managed_extra_{managed_config}"] = len(extra)
    native = {}
    for configuration in ("Release", "Debug"):
        directory = root / f"final-native-{configuration}-{os_name}"
        native[configuration] = {}
        for path in sorted(directory.glob("*.log")):
            if path.name == "build.log":
                continue
            matches = re.findall(r"PASS: (\d+)/(\d+) ", path.read_text(errors="replace"))
            if len(matches) != 1 or matches[0][0] != matches[0][1] or matches[0][0] == "0":
                raise RuntimeError(f"Native checks failed or absent: {path}")
            native[configuration][path.stem] = int(matches[0][0])
        if native[configuration].get("trace-batching") != 313569 or native[configuration].get("root-batching") != 6170:
            raise RuntimeError("Native batching denominator changed")
        for control in ("TraceBatchDrop", "TraceBatchUnbatched", "TraceBatchEarly"):
            directory = root / f"control-{configuration}-{control}-{os_name}"
            text = (directory / "run.log").read_text(errors="replace")
            if "FAIL:" not in text or "PASS:" in text:
                raise RuntimeError(f"Wrong-batching control did not fail: {directory}")
    profile = root / ("profile-windows-census" if windows else "profile-linux-fp")
    observations = rows(profile / "results.csv")
    if len(observations) != 6 or any(row["exit"] != "0" or row["valid"] != "True" for row in observations):
        raise RuntimeError("Native profile coverage is incomplete")
    counts["profiles"] = len(observations)
    goals = rows(root / "goals.csv")
    if len(goals) != 18:
        raise RuntimeError("Frozen goal denominator changed")
    report = dict(os=os_name, observation_counts=counts, native=native,
                  goals_passed=sum(row["passed"] == "True" for row in goals), goals_total=len(goals))
    (root / "record-audit.json").write_text(json.dumps(report, indent=2) + "\n")
    excluded = ("before-", "after-", "obj-", "sdk-", "fixture-", "diagnostic-obj",
                "diagnostic-final-obj", "diagnostic-bound-obj")
    with (root / "raw-catalog.csv").open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=("path", "bytes", "sha256"))
        writer.writeheader()
        count = 0
        for path in sorted(root.rglob("*")):
            relative = path.relative_to(root)
            if (not path.is_file() or relative.parts[0].startswith(excluded)
                    or relative.parts[0] in ("linux-records", "__pycache__")
                    or path.name in ("raw-catalog.csv", "record-audit.console.log", "evidence-linux.tar.gz", "evidence-windows.zip")):
                continue
            writer.writerow(dict(path=str(relative), bytes=path.stat().st_size, sha256=sha(path)))
            count += 1
    print(json.dumps(report, indent=2))
    print("Cataloged", count, "raw evidence files")


if __name__ == "__main__":
    main()
