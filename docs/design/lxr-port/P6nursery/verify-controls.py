#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Compile isolated counterfactual objects; never edit product sources or replace qualified modules."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(command, cwd, log):
    result = subprocess.run(command, cwd=cwd, shell=True, capture_output=True, text=True, timeout=180)
    log.write_text(command + "\n" + result.stdout + result.stderr + f"\nexit={result.returncode}\n", encoding="utf-8")
    if result.returncode:
        raise RuntimeError(f"Build failure retained: {log}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--native", type=Path, required=True)
    parser.add_argument("--framework", type=Path, required=True)
    parser.add_argument("--fixture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    repo, build = args.repo.resolve(strict=True), args.build.resolve(strict=True)
    args.output = args.output.resolve()
    args.output.mkdir(parents=True, exist_ok=False)
    windows = os.name == "nt"
    commands = json.loads((build / "compile_commands.json").read_text())
    cache = (build / "CMakeCache.txt").read_text()
    ninja = re.search(r"^CMAKE_MAKE_PROGRAM:FILEPATH=(.+)$", cache, re.MULTILINE).group(1)
    target = "gc/clrlxrgc.dll" if windows else "gc/libclrlxrgc.so"
    lines = subprocess.check_output([ninja, "-t", "commands", target], cwd=build, text=True).splitlines()
    link = next(line for line in reversed(lines) if ("vs_link_dll" in line if windows else " -shared " in line))
    core = repo / "src/coreclr/gc/lxr_reference_counting.cpp"
    host = repo / "src/coreclr/gc/lxr_gc.cpp"
    controls = [
        ("forwarding-off", core,
         "if (m_configuration.evacuateNursery && IsImmix(start) && object == source)",
         "if (false && m_configuration.evacuateNursery && IsImmix(start) && object == source)", "native"),
        ("copied-feed", core,
         "m_pause_copied_bytes.fetch_add(size, std::memory_order_relaxed);",
         "static_cast<void>(size);", "native"),
        ("recycled-overflow", core,
         "bool recycled = size <= ImmixBlockGeometry::LineBytes && m_configuration.copyLineRecycling;",
         "bool recycled = m_configuration.copyLineRecycling;", "native"),
        ("pinned-source-release", core,
         "LxrRcResult LxrReferenceCounting::ReclaimNursery()\n{",
         """LxrRcResult LxrReferenceCounting::ReclaimNursery()
{
#ifdef LXR_SIDE_METADATA_TEST_PLATFORM
    for (size_t slot = FindNextOccupiedSlot(0);
         slot < GetImmixCapacityBytes() / sizeof(uintptr_t);
         slot = FindNextOccupiedSlot(slot + 1))
    {
        uintptr_t start = GetReservationStart() + slot * sizeof(uintptr_t);
        Object* object = Reference(start);
        if (m_object_model.isPinned != nullptr && m_object_model.isPinned(object, m_object_model.context))
        {
            size_t size = ExtentSlot(slot).load(std::memory_order_acquire);
            LxrRcResult result = FromMetadata(m_counts.ResetObjectQuiescent(start, size));
            if (result != LxrRcResult::Success || Reclaim(start) != LxrRcResult::Success)
            {
                return GetFailure();
            }
        }
    }
#endif // LXR_SIDE_METADATA_TEST_PLATFORM
""", "native"),
        ("frozen-pin", host,
         "AddRoot(first[index], GC_CALL_PINNED, true);",
         "AddRoot(first[index], 0, true);", "managed"),
        ("late-observed-pin", host,
         "(Pinned | ObservedPin)) != 0;",
         "Pinned) != 0;", "managed"),
    ]
    records = []
    for label, source, before, after, kind in controls:
        directory = args.output / label
        directory.mkdir()
        content = source.read_text()
        if content.count(before) != 1:
            raise RuntimeError(f"Control must perturb exactly one real mechanism: {label}")
        variant = directory / source.name
        variant.write_text(content.replace(before, after), encoding="utf-8")
        entry = next(item for item in commands if Path(item["file"]).resolve() == source)
        output_path = Path(entry["output"])
        old_object = str(output_path.relative_to(build) if output_path.is_absolute() else output_path)
        old_source = str(source)
        object_path = directory / ("variant.obj" if windows else "variant.o")
        compile_command = entry["command"]
        if windows:
            compile_command = compile_command.replace(old_source, str(variant))
            compile_command = compile_command.replace(old_object, str(object_path))
            compile_command = re.sub(r"/Fd\S+", "/Fd" + str(directory / "variant.pdb").replace("\\", r"\\"), compile_command)
        else:
            compile_command = compile_command.replace(source.as_posix(), variant.as_posix())
            compile_command = compile_command.replace(Path(old_object).as_posix(), object_path.as_posix())
        run(compile_command, build, directory / "compile.log")
        record = {"control": label, "source": str(source), "source_sha256": digest(source),
                  "variant_sha256": digest(variant), "kind": kind}
        if kind == "native":
            command_file = args.native / "compile-command.txt"
            if not windows:
                command_file = args.native / "compile-command.txt"
            native_command = command_file.read_text().strip()
            native_command = native_command.replace(str(args.native.resolve()), str(directory))
            native_command = native_command.replace(str(args.native), str(directory))
            native_command = native_command.replace(str(source), str(variant))
            if not windows:
                native_command = native_command.replace(source.as_posix(), variant.as_posix())
            run(native_command, repo, directory / "native-build.log")
            executable = directory / ("lxr-rc-validation.exe" if windows else "lxr-rc-validation")
            process = subprocess.run([str(executable), "--nursery"], capture_output=True, text=True, timeout=180)
        else:
            module = directory / ("clrlxrgc.dll" if windows else "libclrlxrgc.so")
            controlled_link = link.replace(old_object, str(object_path))
            if windows:
                for extension in ("dll", "lib", "pdb", "exp"):
                    controlled_link = controlled_link.replace("gc\\clrlxrgc." + extension,
                                                              str(directory / ("clrlxrgc." + extension)))
            else:
                controlled_link = controlled_link.replace(Path(old_object).as_posix(), object_path.as_posix())
                controlled_link = controlled_link.split(" && cd ", 1)[0]
                controlled_link = controlled_link.replace("gc/libclrlxrgc.so", module.as_posix())
            run(controlled_link, build, directory / "link.log")
            environment = {key: value for key, value in os.environ.items()
                           if not key.upper().startswith(("DOTNET_", "COMPLUS_"))}
            environment.update(DOTNET_GCPath=str(module), DOTNET_ReadyToRun="0", DOTNET_TieredCompilation="0",
                               DOTNET_gcServer="0", DOTNET_gcConcurrent="1", DOTNET_GCHeapCount="2",
                               DOTNET_LXRLegacyCapacity="1", DOTNET_LXRNoMutatorLineRecycling="1")
            command = [str(args.framework / ("corerun.exe" if windows else "corerun")),
                       str(args.fixture), "--nursery", str(module), "on"]
            record["command"] = command
            record["settings"] = {key: value for key, value in environment.items() if key.startswith("DOTNET_")}
            record["module_sha256"] = digest(module)
            process = subprocess.run(command, env=environment, capture_output=True, text=True, timeout=180)
        (directory / "run.log").write_text(json.dumps(record) + "\n" + process.stdout + process.stderr +
                                          f"\nexit={process.returncode}\n", encoding="utf-8")
        record["exit_code"] = process.returncode
        records.append(record)
        (args.output / "results.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
        print(label, process.returncode, flush=True)
        detected = "FAIL:" in process.stderr or "InvalidOperationException" in process.stderr
        if label == "late-observed-pin":
            detected = detected or "Fatal error." in process.stderr
        if process.returncode == 0 or not detected:
            raise RuntimeError(f"The wrong mechanism was not decisively detected: {directory}")


if __name__ == "__main__":
    main()
