#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_asmdiffs.py
#
# Notes:
#
# Script to run "superpmi asmdiffs" for various collections.
#
################################################################################
################################################################################

import argparse
import os
import shutil
from coreclr_arguments import *
from jitutil import run_command

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")
parser.add_argument("-base_jit_directory", help="path to the directory containing base clrjit binaries")
parser.add_argument("-diff_jit_directory", help="path to the directory containing diff clrjit binaries")
parser.add_argument("-log_directory", help="path to the directory containing superpmi log files")

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
                        "base_jit_directory",
                        lambda jit_directory: os.path.isdir(jit_directory),
                        "base_jit_directory doesn't exist")

    coreclr_args.verify(args,
                        "diff_jit_directory",
                        lambda jit_directory: os.path.isdir(jit_directory),
                        "diff_jit_directory doesn't exist")

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
    script_dir = os.path.abspath(os.path.dirname(os.path.realpath(__file__)))
    coreclr_args = setup_args(main_args)

    # It doesn't really matter where we put the SPMI artifacts, except we need to find the log files (summary.md) and
    # (possibly) generated .dasm files for upload. Here, they are put in <root>/src/coreclr/scripts/artifacts/spmi.
    spmi_location = os.path.join(script_dir, "artifacts", "spmi")

    log_directory = coreclr_args.log_directory
    platform_name = coreclr_args.platform

    # Find the built jit-analyze and put its directory on the PATH
    jit_analyze_dir = os.path.join(script_dir, "jit-analyze")
    if not os.path.isdir(jit_analyze_dir):
        print("Error: jit-analyze not found in {} (continuing)".format(jit_analyze_dir))
    else:
        # Put the jit-analyze directory on the PATH so superpmi.py can find it.
        print("Adding {} to PATH".format(jit_analyze_dir))
        os.environ["PATH"] = jit_analyze_dir + os.pathsep + os.environ["PATH"]

    # Find the portable `git` installation, and put `git.exe` on the PATH, for use by `jit-analyze`.
    git_directory = os.path.join(script_dir, "git", "cmd")
    git_exe_tool = os.path.join(git_directory, "git.exe")
    if not os.path.isfile(git_exe_tool):
        print("Error: `git` not found at {} (continuing)".format(git_exe_tool))
    else:
        # Put the git\cmd directory on the PATH so jit-analyze can find it.
        print("Adding {} to PATH".format(git_directory))
        os.environ["PATH"] = git_directory + os.pathsep + os.environ["PATH"]

    # Figure out which JITs to use
    os_name = "win" if platform_name.lower() == "windows" else "unix"
    arch_name = coreclr_args.arch
    host_arch_name = "x64" if arch_name.endswith("64") else "x86"
    os_name = "universal" if arch_name.startswith("arm") else os_name
    base_jit_path = os.path.join(coreclr_args.base_jit_directory, 'clrjit_{}_{}_{}.dll'.format(os_name, arch_name, host_arch_name))
    diff_jit_path = os.path.join(coreclr_args.diff_jit_directory, 'clrjit_{}_{}_{}.dll'.format(os_name, arch_name, host_arch_name))

    # Core_Root is where the superpmi tools (superpmi.exe, mcs.exe) are expected to be found.
    # We pass the full path of the JITs to use as arguments.
    core_root_dir = script_dir

    print("Running superpmi.py download to get MCH files")

    log_file = os.path.join(log_directory, "superpmi_download_{}_{}.log".format(platform_name, arch_name))
    # Add this for restricted testing:
    #   "-filter", "benchmarks", "libraries.crossgen2", ######## *********** TEMPORARY: to make testing faster, only download one MCH file
    run_command([
        python_path,
        os.path.join(script_dir, "superpmi.py"),
        "download",
        "--no_progress",
        "-core_root", core_root_dir,
        "-target_os", platform_name,
        "-target_arch", arch_name,
        "-spmi_location", spmi_location,
        "-log_level", "debug",
        "-log_file", log_file
        ], _exit_on_fail=True)

    print("Running superpmi.py asmdiffs")
    log_file = os.path.join(log_directory, "superpmi_{}_{}.log".format(platform_name, arch_name))

    # REVIEW: SuperPMI asm diffs are going to be created in `spmi_location` (./artifacts/spmi)
    # Will they get copied back? Do we need to put them in `log_directory`?
    # We need jit-analyze from jitutils to be able to create the summary.md file

    overall_md_summary_file = os.path.join(spmi_location, "diff_summary.md")
    if os.path.isfile(overall_md_summary_file):
        os.remove(overall_md_summary_file)

    # Add to force asm diffs:
    #   "-diff_jit_option", "JitCloneLoops=0"
    _, _, return_code = run_command([
        python_path,
        os.path.join(script_dir, "superpmi.py"),
        "asmdiffs",
        "--no_progress",
        "-diff_jit_option", "JitCloneLoops=0",
        "-core_root", core_root_dir,
        "-target_os", platform_name,
        "-target_arch", arch_name,
        "-arch", host_arch_name,
        "-base_jit_path", base_jit_path,
        "-diff_jit_path", diff_jit_path,
        "-spmi_location", spmi_location,
        "-error_limit", "100",
        "-log_level", "debug",
        "-log_file", log_file])

    # If there are asm diffs, and jit-analyze ran, we'll get a diff_summary.md file in the spmi_location directory.
    # We make sure the file doesn't exist before we run diffs, so we don't need to worry about superpmi.py creating
    # a unique, numbered file.

    if os.path.isfile(overall_md_summary_file):
        try:
            overall_md_summary_file_target = os.path.join(log_directory, "superpmi_diff_summary_{}_{}.md".format(platform_name, arch_name))
            print("Copying summary file {} -> {}".format(overall_md_summary_file, overall_md_summary_file_target))
            shutil.copy2(overall_md_summary_file, overall_md_summary_file_target)
        except PermissionError as pe_error:
            print('Ignoring PermissionError: {0}'.format(pe_error))

    # TODO: the superpmi.py asmdiffs command returns a failure code if there are MISSING data even if there are
    # no asm diffs. We should probably only fail if there are actual failures (not MISSING or asm diffs).

    if return_code != 0:
        print("Failure in {}".format(log_file))
        return 1

    return 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
