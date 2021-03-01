#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
#
# Title: superpmi_benchmarks.py
#
# Notes:
#
# Script to perform the superpmi collection while executing the Microbenchmarks present
# in https://github.com/dotnet/performance/tree/master/src/benchmarks/micro.

import argparse
import re
import sys

import stat
from os import path
from os.path import isfile
from shutil import copyfile
from coreclr_arguments import *
from superpmi import ChangeDir, TempDir
from superpmi_setup import run_command

# Start of parser object creation.
is_windows = platform.system() == "Windows"
parser = argparse.ArgumentParser(description="description")

parser.add_argument("-performance_directory", help="Path to performance directory")
parser.add_argument("-superpmi_directory", help="Path to superpmi directory")
parser.add_argument("-core_root", help="Path to Core_Root directory")
parser.add_argument("-output_mch_path", help="Absolute path to the mch file to produce")
parser.add_argument("-log_file", help="Name of the log file")
parser.add_argument("-partition_count", help="Total number of partitions")
parser.add_argument("-partition_index", help="Partition index to do the collection for")
parser.add_argument("-arch", help="Architecture")


def setup_args(args):
    """ Setup the args for SuperPMI to use.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    coreclr_args = CoreclrArguments(args, require_built_core_root=False, require_built_product_dir=False,
                                    require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "performance_directory",
                        lambda performance_directory: os.path.isdir(performance_directory),
                        "performance_directory doesn't exist")

    coreclr_args.verify(args,
                        "superpmi_directory",
                        lambda superpmi_directory: os.path.isdir(superpmi_directory),
                        "superpmi_directory doesn't exist")

    coreclr_args.verify(args,
                        "output_mch_path",
                        lambda output_mch_path: not os.path.isfile(output_mch_path),
                        "output_mch_path already exist")

    coreclr_args.verify(args,
                        "log_file",
                        lambda log_file: True,  # not os.path.isfile(log_file),
                        "log_file already exist")

    coreclr_args.verify(args,
                        "core_root",
                        lambda core_root: os.path.isdir(core_root),
                        "core_root doesn't exist")

    coreclr_args.verify(args,
                        "partition_count",
                        lambda partition_count: partition_count.isnumeric(),
                        "Unable to set partition_count")

    coreclr_args.verify(args,
                        "partition_index",
                        lambda partition_index: partition_index.isnumeric(),
                        "Unable to set partition_index")

    coreclr_args.verify(args,
                        "arch",
                        lambda arch: arch.lower() in ["x86", "x64", "arm", "arm64"],
                        "Unable to set arch")

    return coreclr_args


def make_executable(file_name):
    """Make file executable by changing the permission

    Args:
        file_name (string): file to execute
    """
    if is_windows:
        return

    print("Inside make_executable")
    run_command(["ls", "-l", file_name])
    os.chmod(file_name,
             # read+execute for owner
             (stat.S_IRUSR | stat.S_IXUSR) |
             # read+execute for group
             (stat.S_IRGRP | stat.S_IXGRP) |
             # read+execute for other
             (stat.S_IROTH | stat.S_IXOTH))
    run_command(["ls", "-l", file_name])


def build_and_run(coreclr_args, output_mch_name):
    """Build the microbenchmarks and run them under "superpmi collect"

    Args:
        coreclr_args (CoreClrArguments): Arguments use to drive
        output_mch_name (string): Name of output mch file name
    """
    arch = coreclr_args.arch
    python_path = sys.executable
    core_root = coreclr_args.core_root
    superpmi_directory = coreclr_args.superpmi_directory
    performance_directory = coreclr_args.performance_directory
    log_file = coreclr_args.log_file
    partition_count = coreclr_args.partition_count
    partition_index = coreclr_args.partition_index
    dotnet_directory = os.path.join(performance_directory, "tools", "dotnet", arch)
    dotnet_exe = os.path.join(dotnet_directory, "dotnet")

    artifacts_directory = os.path.join(performance_directory, "artifacts")
    artifacts_packages_directory = os.path.join(artifacts_directory, "packages")
    project_file = path.join(performance_directory, "src", "benchmarks", "micro", "MicroBenchmarks.csproj")
    benchmarks_dll = path.join(artifacts_directory, "MicroBenchmarks.dll")

    if is_windows:
        shim_name = "%JitName%"
        corerun_exe = "CoreRun.exe"
        script_name = "run_microbenchmarks.bat"
    else:
        shim_name = "$JitName"
        corerun_exe = "corerun"
        script_name = "run_microbenchmarks.sh"

    make_executable(dotnet_exe)

    run_command(
        [dotnet_exe, "restore", project_file, "--packages",
         artifacts_packages_directory], _exit_on_fail=True)

    run_command(
        [dotnet_exe, "build", project_file, "--configuration", "Release",
         "--framework", "net6.0", "--no-restore", "/p:NuGetPackageRoot=" + artifacts_packages_directory,
         "-o", artifacts_directory], _exit_on_fail=True)

    collection_command = f"{dotnet_exe} {benchmarks_dll}  --filter \"*\" --corerun {path.join(core_root, corerun_exe)} --partition-count {partition_count} " \
                         f"--partition-index {partition_index} --envVars COMPlus_JitName:{shim_name} " \
                         "--iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart"

    # Generate the execution script in Temp location
    with TempDir() as temp_location:
        script_name = path.join(temp_location, script_name)

        contents = []
        # Unset the JitName so dotnet process will not fail
        if is_windows:
            contents.append("set JitName=%COMPlus_JitName%")
            contents.append("set COMPlus_JitName=")
        else:
            contents.append("#!/bin/bash")
            contents.append("export JitName=$COMPlus_JitName")
            contents.append("unset COMPlus_JitName")
        contents.append(f"pushd {performance_directory}")
        contents.append(collection_command)

        with open(script_name, "w") as collection_script:
            collection_script.write(os.linesep.join(contents))

        print()
        print(f"{script_name} contents:")
        print("******************************************")
        print(os.linesep.join(contents))
        print("******************************************")

        make_executable(script_name)

        run_command([
            python_path, path.join(superpmi_directory, "superpmi.py"), "collect", "-core_root", core_root,
            # Disable ReadyToRun so we always JIT R2R methods and collect them
            "--use_zapdisable",
            "-output_mch_path", output_mch_name, "-log_file", log_file, "-log_level", "debug",
            script_name], _exit_on_fail=True)


def strip_unrelated_mc(coreclr_args, old_mch_filename, new_mch_filename):
    """Perform the post processing of produced .mch file by stripping the method contexts
    that are specific to BenchmarkDotnet boilerplate code and hard

    Args:
        coreclr_args (CoreclrArguments): Arguments
        old_mch_filename (string): Name of source .mch file
        new_mch_filename (string): Name of new .mch file to produce post-processing.
    """
    performance_directory = coreclr_args.performance_directory
    core_root = coreclr_args.core_root
    methods_to_strip_list = path.join(performance_directory, "methods_to_strip.mcl")

    mcs_exe = path.join(core_root, "mcs")
    mcs_command = [mcs_exe, "-dumpMap", old_mch_filename]

    # Gather method list to strip
    (mcs_out, _, return_code) = run_command(mcs_command)
    if return_code != 0:
        # If strip command fails, then just copy the old_mch to new_mch
        print(f"-dumpMap failed. Copying {old_mch_filename} to {new_mch_filename}.")
        copyfile(old_mch_filename, new_mch_filename)
        copyfile(old_mch_filename + ".mct", new_mch_filename + ".mct")
        return

    method_context_list = mcs_out.decode("utf-8").split(os.linesep)
    filtered_context_list = []

    match_pattern = re.compile('^(\\d+),(BenchmarkDotNet|Perfolizer)')
    print("Method indices to strip:")
    for mc_entry in method_context_list:
        matched = match_pattern.match(mc_entry)
        if matched:
            print(matched.group(1))
            filtered_context_list.append(matched.group(1))
    print(f"Total {len(filtered_context_list)} methods.")

    with open(methods_to_strip_list, "w") as f:
        f.write('\n'.join(filtered_context_list))

    # Strip and produce new .mcs file
    if run_command([mcs_exe, "-strip", methods_to_strip_list, old_mch_filename, new_mch_filename])[2] != 0:
        # If strip command fails, then just copy the old_mch to new_mch
        print(f"-strip failed. Copying {old_mch_filename} to {new_mch_filename}.")
        copyfile(old_mch_filename, new_mch_filename)
        copyfile(old_mch_filename + ".mct", new_mch_filename + ".mct")
        return

    # Create toc file
    run_command([mcs_exe, "-toc", new_mch_filename])


def main(main_args):
    """ Main entry point

    Args:
        main_args ([type]): Arguments to the script
    """
    coreclr_args = setup_args(main_args)

    all_output_mch_name = path.join(coreclr_args.output_mch_path + "_all.mch")
    build_and_run(coreclr_args, all_output_mch_name)
    if os.path.isfile(all_output_mch_name):
        pass
    else:
        print("No mch file generated.")

    strip_unrelated_mc(coreclr_args, all_output_mch_name, coreclr_args.output_mch_path)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
