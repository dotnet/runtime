#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: fuzzer_setup.py
#
# Notes:
#
# Script to setup cloning and building Antigen/Fuzzlyn tools. It copies all the
# binaries to the correlation payload.
#
################################################################################
################################################################################

import argparse
import os
from coreclr_arguments import *
from os import path
from jitutil import run_command, copy_directory, set_pipeline_variable, ChangeDir, TempDir

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-tool_name", help="Name of tool ('Antigen' or 'Fuzzlyn')")
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
                        "tool_name",
                        lambda name: name == "Antigen" or name == "Fuzzlyn",
                        "tool_name should be Antigen or Fuzzlyn")

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
    os_name = coreclr_args.platform.lower()
    if os_name == "windows":
        os_name = "win"

    run_configuration = "{}-{}".format(os_name, arch_name)
    source_directory = coreclr_args.source_directory

    # CorrelationPayload directories
    correlation_payload_directory = path.join(coreclr_args.source_directory, "payload")
    scripts_src_directory = path.join(source_directory, "src", "coreclr", 'scripts')
    coreroot_directory = path.join(correlation_payload_directory, "CoreRoot")
    dst_directory = path.join(correlation_payload_directory, "exploratory")

    helix_source_prefix = "official"
    creator = ""

    repo_urls = {
        "Antigen": "https://github.com/kunalspathak/Antigen.git",
        "Fuzzlyn": "https://github.com/jakobbotsch/Fuzzlyn.git",
    }

    # tool_name is verifed in setup_args
    assert coreclr_args.tool_name in repo_urls
    repo_url = repo_urls[coreclr_args.tool_name]

    # create exploratory directory
    print('Copying {} -> {}'.format(scripts_src_directory, coreroot_directory))
    copy_directory(scripts_src_directory, coreroot_directory, verbose_output=True, match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    if is_windows:
        acceptable_copy = lambda path: any(path.endswith(extension) for extension in [".py", ".dll", ".exe", ".json"])
    else:
        # Need to accept files without any extension, which is how executable file's names look.
        acceptable_copy = lambda path: (os.path.basename(path).find(".") == -1) or any(path.endswith(extension) for extension in [".py", ".dll", ".so", ".dylib", ".json", ".a"])

    # copy CORE_ROOT
    print('Copying {} -> {}'.format(coreclr_args.core_root_directory, coreroot_directory))
    copy_directory(coreclr_args.core_root_directory, coreroot_directory, verbose_output=True, match_func=acceptable_copy)

    try:
        with TempDir() as tool_code_directory:
            # clone the tool
            run_command(
                ["git", "clone", "--quiet", "--depth", "1", repo_url, tool_code_directory])

            publish_dir = path.join(tool_code_directory, "publish")

            # build the tool
            with ChangeDir(tool_code_directory):
                dotnet_cmd = os.path.join(source_directory, "dotnet.cmd")
                if not is_windows:
                    dotnet_cmd = os.path.join(source_directory, "dotnet.sh")
                run_command([dotnet_cmd, "publish", "-c", "Release", "--self-contained", "-r", run_configuration, "-o", publish_dir], _exit_on_fail=True)

            dll_name = coreclr_args.tool_name + ".dll"
            if not os.path.exists(path.join(publish_dir, dll_name)):
                raise FileNotFoundError("{} not present at {}".format(dll_name, publish_dir))

            # copy tool
            print('Copying {} -> {}'.format(publish_dir, dst_directory))
            copy_directory(publish_dir, dst_directory, verbose_output=True, match_func=acceptable_copy)
    except PermissionError as pe:
        print("Skipping file. Got error: %s", pe)

    # create a dummy file in the work_item directories, otherwise Helix complains
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
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
