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
# It does the following steps:
# 1. It creates `correlation_payload_directory` that contains clrjit*_x64.dll and clrjit*_x86.dll
# 2. The script takes `input_artifacts` parameter which contains *.mch.zip and *.mct.zip. It will then
#    partition it by moving each pair of *.mch.zip/*.mct.zip into its own folder under 'payload'
#    directory.
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
# parser.add_argument("-platform", help="OS platform")
parser.add_argument("-source_directory", help="path to the directory containing binaries")
parser.add_argument("-product_directory", help="path to the directory containing binaries")
# parser.add_argument("-mch_directory", help="path to directory containing compressed mch files")


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

    # coreclr_args.verify(args,
    #                     "platform",
    #                     lambda unused: True,
    #                     "Unable to set platform")

    coreclr_args.verify(args,
                        "source_directory",
                        lambda source_directory: os.path.isdir(source_directory),
                        "source_directory doesn't exist")

    coreclr_args.verify(args,
                        "product_directory",
                        lambda product_directory: os.path.isdir(product_directory),
                        "product_directory doesn't exist")

    # coreclr_args.verify(args,
    #                     "mch_directory",
    #                     lambda mch_directory: True, #os.path.isdir(mch_directory),
    #                     "mch_directory doesn't exist")
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
    # mch_directory = coreclr_args.mch_directory

    # CorrelationPayload directories
    correlation_payload_directory = path.join(coreclr_args.source_directory, "payload")
    superpmi_src_directory = path.join(source_directory, 'src', 'coreclr', 'scripts')

    # Workitem directories
    workitem_directory = path.join(source_directory, "workitem")

    helix_source_prefix = "official"
    creator = ""
    ci = True
    helix_queue = "Windows.10.Amd64.X86"

    # Copy *.py to CorrelationPayload
    print('Copying {} -> {}'.format(superpmi_src_directory, correlation_payload_directory))
    copy_directory(superpmi_src_directory, correlation_payload_directory, match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))
    
    # Copy clrjit*_arch.dll binaries to CorrelationPayload
    print('Copying binaries {} -> {}'.format(arch, product_directory, correlation_payload_directory))
    copy_directory(product_directory, correlation_payload_directory, match_func=match_correlation_files)

    if not os.path.exists(workitem_directory):
        os.makedirs(workitem_directory)
    dummy_workitem_file = path.join(workitem_directory, "dummy.txt")
    with open(dummy_workitem_file, "a") as dummy_file:
        dummy_file.write("Hello World!")

    #TODO: Just send appropriate clrjit*.dll files to workitem_directory
    # # Copy clrjit*_arch.dll binaries to workitem_directory
    # print('Copying clrjit_{}_{}.dll {} -> {}'.format(arch, product_directory, correlation_payload_directory))
    # copy_directory(product_directory, correlation_payload_directory, match_func=match_correlation_files)

    # Partition mch/mct zip files
    # partition_mch(mch_directory, workitem_directory)

    # # Print correlation_payload_directory and workitem_directory
    # print("==> correlation_payload_directory:")
    # for file_path, dirs, files in walk(correlation_payload_directory, topdown=True):
    #     for name in files:
    #         curr_file_path = path.join(file_path, name)
    #         print(curr_file_path)

    # print("==> workitem_directory:")
    # for file_path, dirs, files in walk(workitem_directory, topdown=True):
    #     for name in files:
    #         curr_file_path = path.join(file_path, name)
    #         print(curr_file_path)

    # Set variables
    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("WorkItemDirectory", workitem_directory)
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("Queue", helix_queue)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
