#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_replay.py
#
# Notes:
#
# Script to run "superpmi replay" for various collections under various COMPlus_JitStressRegs values.
#
################################################################################
################################################################################

import argparse
import os
from coreclr_arguments import *
from jitutil import run_command

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")
parser.add_argument("-jit_directory", help="path to the directory containing clrjit binaries")
parser.add_argument("-log_directory", help="path to the directory containing superpmi log files")
parser.add_argument("-partition", help="Partition number specifying which set of flags to use: between 1 and the `-partition_count` value")
parser.add_argument("-partition_count", help="Count of the total number of partitions we are using: should be <= 9 (number of jit_flags_all elements)")

jit_flags_all = [
    "JitStressRegs=0",
    "JitStressRegs=1",
    "JitStressRegs=2",
    "JitStressRegs=3",
    "JitStressRegs=4",
    "JitStressRegs=8",
    "JitStressRegs=0x10",
    "JitStressRegs=0x80",
    "JitStressRegs=0x1000",
]

def split(a, n):
    """ Splits array `a` in `n` partitions.
        Slightly modified from https://stackoverflow.com/a/2135920.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    k, m = divmod(len(a), n)
    return [a[i*k+min(i, m):(i+1)*k+min(i+1, m)] for i in range(n)]


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
                        "arch",
                        lambda unused: True,
                        "Unable to set arch")

    coreclr_args.verify(args,
                        "platform",
                        lambda unused: True,
                        "Unable to set platform")

    coreclr_args.verify(args,
                        "jit_directory",
                        lambda jit_directory: os.path.isdir(jit_directory),
                        "jit_directory doesn't exist")

    coreclr_args.verify(args,
                        "log_directory",
                        lambda log_directory: os.path.isdir(log_directory),
                        "log_directory doesn't exist")

    coreclr_args.verify(args,
                        "partition",
                        lambda partition: True,
                        "Unable to set partition")

    coreclr_args.verify(args,
                        "partition_count",
                        lambda partition: True,
                        "Unable to set partition_count")

    try:
        coreclr_args.partition = int(coreclr_args.partition)
    except ValueError as e:
        print("Illegal `-partition` value: " + str(coreclr_args.partition))
        sys.exit(1)

    try:
        coreclr_args.partition_count = int(coreclr_args.partition_count)
    except ValueError as e:
        print("Illegal `-partition_count` value: " + str(coreclr_args.partition_count))
        sys.exit(1)

    if coreclr_args.partition_count <= 0:
        print("Illegal `-partition_count` value: " + str(coreclr_args.partition_count))
        sys.exit(1)

    if coreclr_args.partition < 1 or coreclr_args.partition > coreclr_args.partition_count:
        print("Illegal `-partition` value: " + str(coreclr_args.partition))
        sys.exit(1)

    return coreclr_args


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """
    python_path = sys.executable
    cwd = os.path.dirname(os.path.realpath(__file__))
    coreclr_args = setup_args(main_args)
    spmi_location = os.path.join(cwd, "artifacts", "spmi")
    log_directory = coreclr_args.log_directory
    platform_name = coreclr_args.platform
    os_name = "win" if platform_name.lower() == "windows" else "unix"
    arch_name = coreclr_args.arch
    host_arch_name = "x64" if arch_name.endswith("64") else "x86"
    os_name = "universal" if arch_name.startswith("arm") else os_name
    jit_path = os.path.join(coreclr_args.jit_directory, 'clrjit_{}_{}_{}.dll'.format(os_name, arch_name, host_arch_name))

    jit_flags_partitioned = split(jit_flags_all, coreclr_args.partition_count)
    jit_flags = jit_flags_partitioned[coreclr_args.partition - 1] # partition number is 1-based

    print("Running superpmi.py download")
    run_command([python_path,
            os.path.join(cwd, "superpmi.py"),
            "download",
            "--no_progress",
            "-target_os", platform_name,
            "-target_arch", arch_name,
            "-core_root", cwd,
            "-spmi_location", spmi_location,
            "-log_level", "debug"], _exit_on_fail=True)

    failed_runs = []
    for jit_flag in jit_flags:
        log_file = os.path.join(log_directory, 'superpmi_{}.log'.format(jit_flag.replace("=", "_")))
        print("Running superpmi.py replay for {}".format(jit_flag))

        _, _, return_code = run_command([
            python_path,
            os.path.join(cwd, "superpmi.py"),
            "replay",
            "-core_root", cwd,
            "-jitoption", jit_flag,
            "-target_os", platform_name,
            "-target_arch", arch_name,
            "-arch", host_arch_name,
            "-jit_path", jit_path,
            "-spmi_location", spmi_location,
            "-log_level", "debug",
            "-log_file", log_file])

        if return_code != 0:
            failed_runs.append("Failure in {}".format(log_file))

    # Consolidate all superpmi_*.logs in superpmi_platform_architecture.log
    final_log_name = os.path.join(log_directory, "superpmi_{}_{}_{}.log".format(platform_name, arch_name, coreclr_args.partition))
    print("Consolidating final {}".format(final_log_name))
    with open(final_log_name, "a") as final_superpmi_log:
        for superpmi_log in os.listdir(log_directory):
            if not superpmi_log.startswith("superpmi_Jit") or not superpmi_log.endswith(".log"):
                continue

            print("Appending {}".format(superpmi_log))
            final_superpmi_log.write("======================================================={}".format(os.linesep))
            final_superpmi_log.write("Contents from {}{}".format(superpmi_log, os.linesep))
            final_superpmi_log.write("======================================================={}".format(os.linesep))
            with open(os.path.join(log_directory, superpmi_log), "r") as current_superpmi_log:
                contents = current_superpmi_log.read()
                final_superpmi_log.write(contents)

        # Log failures summary
        if len(failed_runs) > 0:
            final_superpmi_log.write(os.linesep)
            final_superpmi_log.write(os.linesep)
            final_superpmi_log.write("========Failed runs summary========".format(os.linesep))
            final_superpmi_log.write(os.linesep.join(failed_runs))

    return 0 if len(failed_runs) == 0 else 1


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
