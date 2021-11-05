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

parser.add_argument("-diff_summary_dir", help="Path to diff summary directory")
parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")

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
                        "diff_summary_dir",
                        lambda diff_summary_dir: os.path.isdir(diff_summary_dir),
                        "diff_summary_dir doesn't exist")

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

    diff_summary_dir = coreclr_args.diff_summary_dir
    arch = coreclr_args.arch
    platform = coreclr_args.platform

    # Consolidate all superpmi_diff_summary_*.md in overall_diff_summary_<platform>_<architecture>.md
    # (Don't name it "superpmi_xxx.md" or we'll consolidate it into itself.)
    final_md_path = os.path.join(diff_summary_dir, "overall_diff_summary_{}_{}.md".format(platform, arch))
    print("Consolidating final {}".format(final_md_path))
    with open(final_md_path, "a") as f:
        for superpmi_md_file in os.listdir(diff_summary_dir):
            if not superpmi_md_file.startswith("superpmi_") or not superpmi_md_file.endswith(".md"):
                continue
            print("Appending {}".format(superpmi_md_file))
            with open(os.path.join(diff_summary_dir, superpmi_md_file), "r") as current_superpmi_md:
                contents = current_superpmi_md.read()
                f.write(contents)

    print("##vso[task.uploadsummary]{}".format(final_md_path))

    with open(final_md_path, "r") as f:
        print(f.read())

    return 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
