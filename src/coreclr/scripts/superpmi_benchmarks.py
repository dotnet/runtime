#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: superpmi_benchmarks.py
#
# Notes:
#
# Script to perform the superpmi collection while executing the Microbenchmarks present
# in https://github.com/dotnet/performance/tree/master/src/benchmarks/micro.

import argparse
import re
import sys

from os import path
from coreclr_arguments import *
from superpmi import ChangeDir
from superpmi_setup import run_command

# Start of parser object creation.
is_windows = platform.system() == "Windows"
parser = argparse.ArgumentParser(description="description")

parser.add_argument("-performance_directory", help="Path to performance directory")
parser.add_argument("-superpmi_directory", help="Path to superpmi directory")
parser.add_argument("-python_path", help="Path to python")
parser.add_argument("-core_root", help="Path to Core_Root directory")
parser.add_argument("-shim_name", help="Name of collector shim")
parser.add_argument("-output_mch_path", help="Absolute path to the mch file to produce")
parser.add_argument("-log_file", help="Name of the log file")
parser.add_argument("-partition_count", help="Total number of partitions")
parser.add_argument("-partition_index", help="Partition index to do the collection for")


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
                        lambda log_file: not os.path.isfile(log_file),
                        "log_file already exist")

    coreclr_args.verify(args,
                        "core_root",
                        lambda core_root: os.path.isdir(core_root),
                        "core_root doesn't exist")

    coreclr_args.verify(args,
                        "python_path",
                        lambda python_path: os.path.isfile(python_path),
                        "python_path doesn't exist")

    coreclr_args.verify(args,
                        "shim_name",
                        lambda unused: True,
                        "Unable to set shim_name")

    coreclr_args.verify(args,
                        "partition_count",
                        lambda partition_count: partition_count.isnumeric(),
                        "Unable to set partition_count")

    coreclr_args.verify(args,
                        "partition_index",
                        lambda partition_index: partition_index.isnumeric(),
                        "Unable to set partition_index")

    return coreclr_args


def execute(coreclr_args, output_mch_name):
    """Execute the superpmi collection for Microbenchmarks

    Args:
        coreclr_args (CoreclrArguments): Arguments
        output_mch_name (string): The name of output mch file name.
    """
    python_path = coreclr_args.python_path
    core_root = coreclr_args.core_root
    superpmi_directory = coreclr_args.superpmi_directory
    performance_directory = coreclr_args.performance_directory
    shim_name = coreclr_args.shim_name
    log_file = coreclr_args.log_file
    partition_count = coreclr_args.partition_count
    partition_index = coreclr_args.partition_index
    benchmarks_dll = path.join(performance_directory, "artifacts", "Microbenchmarks.dll")

    with ChangeDir(performance_directory):
        print("Inside " + performance_directory)
        dotnet_exe_name = "dotnet.exe" if is_windows else "dotnet"
        corerun_exe_name = "CoreRun.exe" if is_windows else "corerun"
        dotnet_exe = path.join(performance_directory, "tools", "dotnet", dotnet_exe_name)
        run_command([
            python_path, path.join(superpmi_directory, "superpmi.py"), "collect",

            # dotnet command to execute Microbenchmarks.dll
            dotnet_exe, benchmarks_dll + " --filter * --corerun " + path.join(core_root, corerun_exe_name) +
            " --partition-count " + partition_count + " --partition-index " + partition_index +
            " --envVars COMPlus_JitName:" + shim_name + " --iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart",

            # superpmi.py collect arguments

            # Path to core_root because the script will be ran from "performance" repo.
            "-core_root", core_root,

            # Specify that temp_dir is current performance directory, because in order to execute
            # microbenchmarks, it needs access to the source code.
            # Also, skip cleaning up once done, because the superpmi script is being
            # executed from the same folder.
            "-temp_dir", performance_directory, "--skip_cleanup",

            # Disable ReadyToRun so we always JIT R2R methods and collect them
            "--use_zapdisable",
            "-output_mch_path", output_mch_name, "-log_file", log_file, "-log_level", "debug"])


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
    mcs_command = [mcs_exe, "/dumpMap", old_mch_filename]

    # Gather method list to strip
    (mcs_out, mcs_error) = run_command(mcs_command, _capture_output=True)
    if len(mcs_error) > 0:
        print("Error executing mcs /dumpMap")
        return

    method_context_list = mcs_out.decode("utf-8").split(os.linesep)
    filtered_context_list = []

    match_pattern = re.compile('^(\\d+),(BenchmarkDotNet|Perfolizer)')
    for mc_entry in method_context_list:
        matched = match_pattern.match(mc_entry)
        if matched:
            filtered_context_list.append(matched.group(1))

    with open(methods_to_strip_list, "w") as f:
        f.write('\n'.join(filtered_context_list))

    # Strip and produce new .mcs file
    run_command([mcs_exe, "-strip", methods_to_strip_list, old_mch_filename, new_mch_filename])

    # Create toc file
    run_command([mcs_exe, "-toc", new_mch_filename])


def main(main_args):
    """ Main entry point

    Args:
        main_args ([type]): Arguments to the script
    """
    coreclr_args = setup_args(main_args)

    all_output_mch_name = path.join(coreclr_args.output_mch_path + "_all.mch")
    execute(coreclr_args, all_output_mch_name)
    if os.path.isfile(all_output_mch_name):
        pass
    else:
        print("No mch file generated.")

    strip_unrelated_mc(coreclr_args, all_output_mch_name, coreclr_args.output_mch_path)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
