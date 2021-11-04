#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: superpmi_asmdiffs_summarize.py
#
# Notes:
#
# Script to summarize issues found from all partitions and print them on console.
#
################################################################################
################################################################################

import argparse
import json
import os
import re
import zipfile
from collections import defaultdict
from os import walk
from coreclr_arguments import *

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")
parser.add_argument("-build_config", help="Build configuration of runtime under test")

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
                        "arch",
                        lambda unused: True,
                        "Unable to set arch")

    coreclr_args.verify(args,
                        "platform",
                        lambda unused: True,
                        "Unable to set platform")

    coreclr_args.verify(args,
                        "build_config",
                        lambda unused: True,
                        "Unable to set build_config")

    return coreclr_args


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    arch = coreclr_args.arch
    platform = coreclr_args.platform
    build_config = coreclr_args.build_config

    print("arch: {}".format(arch))
    print("platform: {}".format(platform))
    print("build_config: {}".format(build_config))

    # TODO: collect all the summary.md files from the partitions into a full summary.md file
    # and tell AzDO about it so it creates an "Extensions" page on the AzDO UI showing the
    # asm diffs. Also, upload this file.

    #print("##vso[task.uploadsummary]{}".format(md_path))

    #with open(md_path, "r") as f:
    #    print(f.read())

    return 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
