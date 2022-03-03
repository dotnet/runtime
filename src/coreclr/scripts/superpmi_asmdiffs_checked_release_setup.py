#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_asmdiffs_checked_release_setup.py
#
# Notes:
#
# Script to setup directory structure required to perform SuperPMI asmdiffs checked release in CI.
# It creates `correlation_payload_directory` with `base` and `diff` directories
# that contains clrjit*.dll. 
#
################################################################################
################################################################################

import argparse
import logging
import os

from coreclr_arguments import *
from jitutil import copy_directory, set_pipeline_variable, run_command, TempDir, download_files

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-source_directory", help="path to the directory of the dotnet/runtime source tree")
parser.add_argument("-checked_directory", help="path to the directory containing checked binaries (e.g., <source_directory>/artifacts/bin/coreclr/windows.x64.Checked)")
parser.add_argument("-release_directory", help="path to the directory containing release binaries (e.g., <source_directory>/artifacts/bin/coreclr/windows.x64.Release)")

is_windows = platform.system() == "Windows"


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
                        "checked_directory",
                        lambda checked_directory: os.path.isdir(checked_directory),
                        "checked_directory doesn't exist")

    coreclr_args.verify(args,
                        "release_directory",
                        lambda release_directory: os.path.isdir(release_directory),
                        "release_directory doesn't exist")
                        
    return coreclr_args


def match_jit_files(full_path):
    """ Match all the JIT files that we want to copy and use.
        Note that we currently only match Windows files, and not osx cross-compile files.
        We also don't copy the "default" clrjit.dll, since we always use the fully specified
        JITs, e.g., clrjit_win_x64_x64.dll.
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
    """Main entrypoint: Prepare the Helix data for SuperPMI asmdiffs checked release Azure DevOps pipeline.

    The Helix correlation payload directory is created and populated as follows:

    <source_directory>\payload -- the correlation payload directory
        -- contains the *.py scripts from <source_directory>\src\coreclr\scripts
        -- contains superpmi.exe, mcs.exe from the target-specific build
    <source_directory>\payload\base
        -- contains the Checked JITs
    <source_directory>\payload\diff
        -- contains the Release JITs

    Then, AzDO pipeline variables are set.

    Args:
        main_args ([type]): Arguments to the script
    """

    # Set up logging.
    logger = logging.getLogger()
    logger.setLevel(logging.INFO)
    stream_handler = logging.StreamHandler(sys.stdout)
    stream_handler.setLevel(logging.INFO)
    logger.addHandler(stream_handler)

    coreclr_args = setup_args(main_args)

    arch = coreclr_args.arch
    source_directory = coreclr_args.source_directory
    checked_directory = coreclr_args.checked_directory
    release_directory = coreclr_args.release_directory

    python_path = sys.executable

    # CorrelationPayload directories
    correlation_payload_directory = os.path.join(source_directory, "payload")
    superpmi_scripts_directory = os.path.join(source_directory, 'src', 'coreclr', 'scripts')
    base_jit_directory = os.path.join(correlation_payload_directory, "base")
    diff_jit_directory = os.path.join(correlation_payload_directory, "diff")

    ######## Copy SuperPMI python scripts

    # Copy *.py to CorrelationPayload
    print('Copying {} -> {}'.format(superpmi_scripts_directory, correlation_payload_directory))
    copy_directory(superpmi_scripts_directory, correlation_payload_directory, verbose_copy=True,
                   match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    ######## Copy baseline Checked JIT

    # Copy clrjit*_arch.dll binaries from Checked checked_directory to base_jit_directory
    print('Copying base Checked binaries {} -> {}'.format(checked_directory, base_jit_directory))
    copy_directory(checked_directory, base_jit_directory, verbose_copy=True, match_func=match_jit_files)

    ######## Copy diff Release JIT

    # Copy clrjit*_arch.dll binaries from release_directory to diff_jit_directory
    print('Copying diff Release binaries {} -> {}'.format(release_directory, diff_jit_directory))
    copy_directory(release_directory, diff_jit_directory, verbose_copy=True, match_func=match_jit_files)

    ######## Get SuperPMI tools

    # Put the SuperPMI tools directly in the root of the correlation payload directory.
    print('Copying SuperPMI tools {} -> {}'.format(checked_directory, correlation_payload_directory))
    copy_directory(checked_directory, correlation_payload_directory, verbose_copy=True, match_func=match_superpmi_tool_files)


    # Set variables

    helix_source_prefix = "official"
    creator = ""

    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
