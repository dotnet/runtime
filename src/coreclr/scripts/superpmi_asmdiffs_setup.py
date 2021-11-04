#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_asmdiffs_setup.py
#
# Notes:
#
# Script to setup directory structure required to perform SuperPMI asmdiffs in CI.
# It creates `correlation_payload_directory` with `base` and `diff` directories
# that contain clrjit*.dll. It figures out the baseline commit hash to use for
# a particular GitHub pull request, and downloads the JIT rolling build for that
# commit hash.
#
################################################################################
################################################################################

import argparse
import os

from coreclr_arguments import *
from azdo_pipelines_util import copy_directory, copy_files, set_pipeline_variable, run_command

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-source_directory", help="path to the directory containing binaries")
parser.add_argument("-product_directory", help="path to the directory containing binaries")


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
                        "source_directory",
                        lambda source_directory: os.path.isdir(source_directory),
                        "source_directory doesn't exist")

    coreclr_args.verify(args,
                        "product_directory",
                        lambda product_directory: os.path.isdir(product_directory),
                        "product_directory doesn't exist")

    return coreclr_args


def match_jit_files(full_path):
    """ Match all the JIT files that we want to copy and use.
        Note that we currently only match Windows files, and not osx cross-compile files.
    """
    file_name = os.path.basename(full_path)

    if file_name.startswith("clrjit_") and file_name.endswith(".dll") and file_name.find("osx") == -1:
        return True

    return False


def match_superpmi_tool_files(full_path):
    """ Match all the SuperPMI tool files that we want to copy and use.
        Note that we currently only match Windows files.
    """
    file_name = os.path.basename(full_path)

    if file_name == "superpmi.exe" or file_name == "mcs.exe":
        return True

    return False


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """
    coreclr_args = setup_args(main_args)

    arch = coreclr_args.arch
    source_directory = coreclr_args.source_directory
    product_directory = coreclr_args.product_directory

    python_path = sys.executable

    # CorrelationPayload directories
    correlation_payload_directory = os.path.join(source_directory, "payload")
    superpmi_scripts_directory = os.path.join(source_directory, 'src', 'coreclr', 'scripts')

    helix_source_prefix = "official"
    creator = ""

    ######## Get SuperPMI python scripts

    # Copy *.py to CorrelationPayload
    print('Copying {} -> {}'.format(superpmi_scripts_directory, correlation_payload_directory))
    copy_directory(superpmi_scripts_directory, correlation_payload_directory, verbose_copy=True,
                   match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    ######## Get baseline JIT

    # Figure out which baseline JIT to use, and download it.
    # Copy base clrjit*_arch.dll binaries to CorrelationPayload\base
    base_jit_directory = os.path.join(correlation_payload_directory, "base")
    if not os.path.exists(base_jit_directory):
        os.makedirs(base_jit_directory)

    print("Fetching history of `main` branch so we can find the baseline JIT")
    run_command(["git", "fetch", "origin", "main"], _exit_on_fail=True)

    # Note: we only support downloading Windows versions of the JIT currently. To support downloading
    # non-Windows JITs on a Windows machine, pass `-host_os <os>` to jitrollingbuild.py.
    print("Running jitrollingbuild.py download to get baseline")
    _, _, return_code = run_command([
        python_path,
        os.path.join(superpmi_scripts_directory, "jitrollingbuild.py"),
        "download",
        "-arch", arch,
        "-target_dir", base_jit_directory])

    ######## Get diff JIT

    # Copy diff clrjit*_arch.dll binaries to CorrelationPayload\diff
    diff_jit_directory = os.path.join(correlation_payload_directory, "diff")
    print('Copying diff binaries {} -> {}'.format(product_directory, diff_jit_directory))
    copy_directory(product_directory, diff_jit_directory, verbose_copy=True, match_func=match_jit_files)

    ######## Get SuperPMI tools

    # Put the SuperPMI tools directly in the root of the correlation payload directory.
    print('Copying SuperPMI tools {} -> {}'.format(product_directory, correlation_payload_directory))
    copy_directory(product_directory, correlation_payload_directory, verbose_copy=True, match_func=match_superpmi_tool_files)

    ######## Set pipeline variables

    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
