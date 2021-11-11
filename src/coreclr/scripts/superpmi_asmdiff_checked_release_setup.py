#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_asmdiff_checked_release_setup.py
#
# Notes:
#
# Script to setup directory structure required to perform SuperPMI asmdiff in CI.
# It creates `correlation_payload_directory` that contains clrjit*_x64.dll and clrjit*_x86.dll
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
parser.add_argument("-product_directory", help="path to the directory containing checked binaries")
parser.add_argument("-release_directory", help="path to the directory containing release binaries")
parser.add_argument("-release_artifactname", help="release artifact name")

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

    coreclr_args.verify(args,
                        "release_directory",
                        lambda release_directory: os.path.isdir(release_directory),
                        "release_directory doesn't exist")
                        
    coreclr_args.verify(args,
                        "release_artifactname",
                        lambda unused: True,
                        "Unable to set release_artifactname")
                        
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
    release_directory = coreclr_args.release_directory
    release_artifactname = coreclr_args.release_artifactname

    # CorrelationPayload directories
    correlation_payload_directory = os.path.join(source_directory, "payload")
    superpmi_src_directory = os.path.join(source_directory, 'src', 'coreclr', 'scripts')
    release_correlation_payload_directory = os.path.join(source_directory, "payload_release")
    
    helix_source_prefix = "official"
    creator = ""

    # Copy *.py to CorrelationPayload
    print('Copying {} -> {}'.format(superpmi_src_directory, correlation_payload_directory))
    copy_directory(superpmi_src_directory, correlation_payload_directory,
                   match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    # Copy clrjit*_arch.dll binaries from Checked to CorrelationPayload
    print('Copying Checked binaries {} -> {}'.format(product_directory, correlation_payload_directory))
    copy_directory(product_directory, correlation_payload_directory, match_func=match_correlation_files)

    # Copy clrjit*_arch.dll binaries from Release to ReleaseCorrelationPayload
    print('Copying Release binaries {} -> {}'.format(release_directory, release_correlation_payload_directory))
    copy_directory(release_directory, release_correlation_payload_directory, match_func=match_correlation_files)

    # Set variables
    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("ReleaseCorrelationPayloadDirectory", release_correlation_payload_directory)
    set_pipeline_variable("ReleaseArtifactName", release_artifactname)
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
