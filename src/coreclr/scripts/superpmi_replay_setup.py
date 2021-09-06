#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               : superpmi_replay_setup.py
#
# Notes:
#
# Script to setup directory structure required to perform SuperPMI replay in CI.
#  It creates `correlation_payload_directory` that contains clrjit*_x64.dll and clrjit*_x86.dll
################################################################################
################################################################################

import argparse
from os import path, walk
import os
import shutil
import stat
import subprocess
import tempfile

from os.path import isfile, join
from coreclr_arguments import *
from superpmi_setup import copy_directory, copy_files, set_pipeline_variable, run_command

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


def partition_mch(mch_directory, dst_directory):
    from os import listdir

    print("Inside partition_mch")
    mch_zip_files = []
    for file_path, dirs, files in walk(mch_directory, topdown=True):
        for name in files:
            curr_file_path = path.join(file_path, name)

            if not isfile(curr_file_path):
                continue
            if not name.endswith(".mch.zip"):
                continue

            mch_zip_files.append(curr_file_path)

    index = 1
    for mch_file in mch_zip_files:
        print("Processing {}".format(mch_file))
        file_names = []
        file_names += [mch_file]
        file_names += [mch_file.replace(".mch.zip", ".mch.mct.zip")]
        curr_dst_path = path.join(dst_directory, "partitions", str(index))
        copy_files(mch_directory, curr_dst_path, file_names)
        index += 1


def match_correlation_files(full_path):
    file_name = os.path.basename(full_path)

    if file_name.startswith("clrjit_") and file_name.endswith(".dll") and file_name.find(
        "osx") == -1 and file_name.find("armel") == -1:
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
    correlation_payload_directory = path.join(coreclr_args.source_directory, "payload")
    superpmi_src_directory = path.join(source_directory, 'src', 'coreclr', 'scripts')

    helix_source_prefix = "official"
    creator = ""
    ci = True
    helix_queue = "Windows.10.Amd64.X86"

    # Copy *.py to CorrelationPayload
    print('Copying {} -> {}'.format(superpmi_src_directory, correlation_payload_directory))
    copy_directory(superpmi_src_directory, correlation_payload_directory,
                   match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    # Copy clrjit*_arch.dll binaries to CorrelationPayload
    print('Copying binaries {} -> {}'.format(arch, product_directory, correlation_payload_directory))
    copy_directory(product_directory, correlation_payload_directory, match_func=match_correlation_files)

    # Set variables
    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("Queue", helix_queue)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
