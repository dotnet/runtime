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
# TODO: invoke jitrollingbuild.py or superpmi.py to figure out the baseline from
# an origin/main commit hash?
#
################################################################################
################################################################################

import argparse
import os

from coreclr_arguments import *
from azdo_pipelines_util import copy_directory, copy_files, set_pipeline_variable

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


def match_correlation_files(full_path):
    file_name = os.path.basename(full_path)

    if file_name.startswith("clrjit_") and file_name.endswith(".dll") and file_name.find("osx") == -1:
        return True

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

    # CorrelationPayload directories
    correlation_payload_directory = os.path.join(source_directory, "payload")
    superpmi_src_directory = os.path.join(source_directory, 'src', 'coreclr', 'scripts')

    helix_source_prefix = "official"
    creator = ""

    # Copy *.py to CorrelationPayload
    print('Copying {} -> {}'.format(superpmi_src_directory, correlation_payload_directory))
    copy_directory(superpmi_src_directory, correlation_payload_directory,
                   match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    # Copy base clrjit*_arch.dll binaries to CorrelationPayload\base
    base_jit_directory = os.path.join(correlation_payload_directory, "base")
    print('Copying base binaries {} -> {}'.format(product_directory, base_jit_directory))
    copy_directory(product_directory, base_jit_directory, match_func=match_correlation_files)

    # Copy diff clrjit*_arch.dll binaries to CorrelationPayload\diff
    diff_jit_directory = os.path.join(correlation_payload_directory, "diff")
    print('Copying diff binaries {} -> {}'.format(product_directory, diff_jit_directory))
    copy_directory(product_directory, diff_jit_directory, match_func=match_correlation_files)

    # Set variables
    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
