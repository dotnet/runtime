#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               : superpmi_setup.py
#
# Notes:
#
# Script to run "superpmi replay" for various collections under various COMPlus_JitStressRegs value.
################################################################################
################################################################################


import argparse
from os import path
import os
from os import listdir
from coreclr_arguments import *
from superpmi_setup import run_command

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")
parser.add_argument("-jit_directory", help="path to the directory containing clrjit binaries")
parser.add_argument("-log_directory", help="path to the directory containing superpmi log files")

jit_flags = [
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
                        lambda log_directory: True,
                        "log_directory doesn't exist")

    return coreclr_args


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    python_path = sys.executable
    cwd = os.path.dirname(os.path.realpath(__file__))
    coreclr_args = setup_args(main_args)
    spmi_location = path.join(cwd, "artifacts", "spmi")
    log_directory = coreclr_args.log_directory
    platform_name = coreclr_args.platform
    os_name = "win" if platform_name.lower() == "windows" else "unix"
    arch_name = coreclr_args.arch
    host_arch_name = "x64" if arch_name.endswith("64") else "x86"
    jit_path = path.join(coreclr_args.jit_directory, 'clrjit_{}_{}_{}.dll'.format(os_name, arch_name, host_arch_name))

    print("Running superpmi.py download")
    run_command([python_path, path.join(cwd, "superpmi.py"), "download", "--no_progress", "-target_os", platform_name,
                 "-target_arch", arch_name, "-core_root", cwd, "-spmi_location", spmi_location], _exit_on_fail=True)

    failed_runs = []
    for jit_flag in jit_flags:
        log_file = path.join(log_directory, 'superpmi_{}.log'.format(jit_flag.replace("=", "_")))
        print("Running superpmi.py replay for {}".format(jit_flag))

        _, _, return_code = run_command([
            python_path, path.join(cwd, "superpmi.py"), "replay", "-core_root", cwd,
            "-jitoption", jit_flag, "-jitoption", "TieredCompilation=0",
            "-target_os", platform_name, "-target_arch", arch_name,
            "-arch", host_arch_name,
            "-jit_path", jit_path, "-spmi_location", spmi_location,
            "-log_level", "debug", "-log_file", log_file])

        if return_code != 0:
            failed_runs.append("Failure in {}".format(log_file))

    # Consolidate all superpmi_*.logs in superpmi_platform_architecture.log
    final_log_name = path.join(log_directory, "superpmi_{}_{}.log".format(platform_name, arch_name))
    print("Consolidating final {}".format(final_log_name))
    with open(final_log_name, "a") as final_superpmi_log:
        for superpmi_log in listdir(log_directory):
            if not superpmi_log.startswith("superpmi_Jit") or not superpmi_log.endswith(".log"):
                continue

            print("Appending {}".format(superpmi_log))
            final_superpmi_log.write("======================================================={}".format(os.linesep))
            final_superpmi_log.write("Contents from {}{}".format(superpmi_log, os.linesep))
            final_superpmi_log.write("======================================================={}".format(os.linesep))
            with open(path.join(log_directory, superpmi_log), "r") as current_superpmi_log:
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
