#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: antigen_setup.py
#
# Notes:
#
# Script to setup cloning and building Antigen tool. It copies all the binaries
# to the correlation payload.
#
################################################################################
################################################################################

import argparse
from os import path
import os
from os import listdir
from coreclr_arguments import *
from superpmi_setup import run_command, copy_directory, set_pipeline_variable
from superpmi import ChangeDir, TempDir
import tempfile

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")
parser.add_argument("-source_directory",  help="path to source directory")
parser.add_argument("-core_root_directory",  help="path to CORE_ROOT directory")
is_windows = platform.system() == "Windows"

def setup_args(args):
    """ Setup the args.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    coreclr_args = CoreclrArguments(args, require_built_core_root=False, require_built_product_dir=False,
                                    require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "source_directory",
                        lambda source_directory: os.path.isdir(source_directory),
                        "source_directory doesn't exist")

    coreclr_args.verify(args,
                        "core_root_directory",
                        lambda core_root_directory: os.path.isdir(core_root_directory),
                        "core_root_directory doesn't exist")

    coreclr_args.verify(args,
                        "arch",
                        lambda unused: True,
                        "Unable to set arch")

    coreclr_args.verify(args,
                        "platform",
                        lambda unused: True,
                        "Unable to set platform")

    return coreclr_args


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)
    arch_name = coreclr_args.arch
    os_name = "win" if coreclr_args.platform.lower() == "windows" else "linux"
    run_configuration = "{}-{}".format(os_name, arch_name)
    source_directory = coreclr_args.source_directory

    # CorrelationPayload directories
    correlation_payload_directory = path.join(coreclr_args.source_directory, "payload")
    scripts_src_directory = path.join(source_directory, "src", "coreclr", 'scripts')
    coreroot_dst_directory = path.join(correlation_payload_directory, "CoreRoot")
    antigen_dst_directory = path.join(correlation_payload_directory, "exploratory")

    helix_source_prefix = "official"
    creator = ""
    ci = True
    if is_windows:
        helix_queue = "Windows.10.Arm64" if arch_name == "arm64" else "Windows.10.Amd64.X86"
    else:
        if arch_name == "arm":
            helix_queue = "(Ubuntu.1804.Arm32)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-helix-arm32v7-bfcd90a-20200121150440"
        elif arch_name == "arm64":
            helix_queue = "(Ubuntu.1804.Arm64)Ubuntu.1804.ArmArch@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-helix-arm64v8-20210531091519-97d8652"
        else:
            helix_queue = "Ubuntu.1804.Amd64"

    # create exploratory directory
    print('Copying {} -> {}'.format(scripts_src_directory, coreroot_dst_directory))
    copy_directory(scripts_src_directory, coreroot_dst_directory, match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    if is_windows:
        acceptable_copy = lambda path: any(path.endswith(extension) for extension in [".py", ".dll", ".exe", ".json"])
    else:
        # Need to accept files without any extension, which is how executable file's names look.
        acceptable_copy = lambda path: (os.path.basename(path).find(".") == -1) or any(path.endswith(extension) for extension in [".py", ".dll", ".so", ".json", ".a"])

    # copy CORE_ROOT
    print('Copying {} -> {}'.format(coreclr_args.core_root_directory, coreroot_dst_directory))
    copy_directory(coreclr_args.core_root_directory, coreroot_dst_directory, match_func=acceptable_copy)

    try:
        with TempDir() as tool_code_directory:
            # clone the tool
            run_command(
                ["git", "clone", "--quiet", "--depth", "1", "https://github.com/kunalspathak/Antigen.git", tool_code_directory])

            antigen_bin_directory = path.join(tool_code_directory, "bin", "Release", "net5.0")

            # build the tool
            with ChangeDir(tool_code_directory):
                dotnet_cmd = os.path.join(source_directory, "dotnet.cmd")
                if not is_windows:
                    dotnet_cmd = os.path.join(source_directory, "dotnet.sh")
                run_command([dotnet_cmd, "publish", "-c", "Release", "--self-contained", "-r", run_configuration, "-o", antigen_bin_directory], _exit_on_fail=True)

            if not os.path.exists(path.join(antigen_bin_directory, "Antigen.dll")):
                raise FileNotFoundError("Antigen.dll not present at {}".format(antigen_bin_directory))

            # copy antigen tool
            print('Copying {} -> {}'.format(antigen_bin_directory, antigen_dst_directory))
            copy_directory(antigen_bin_directory, antigen_dst_directory, match_func=acceptable_copy)
    except PermissionError as pe:
        print("Skipping file. Got error: %s", pe)

    # create foo.txt in work_item directories
    workitem_directory = path.join(source_directory, "workitem")
    os.mkdir(workitem_directory)
    foo_txt = os.path.join(workitem_directory, "foo.txt")
    with open(foo_txt, "w") as foo_txt_file:
        foo_txt_file.write("hello world!")

    # Set variables
    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("WorkItemDirectory", workitem_directory)
    set_pipeline_variable("RunConfiguration", run_configuration)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("Queue", helix_queue)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
