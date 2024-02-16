#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: superpmi_diffs_summarize.py
#
# Notes:
#
# Script to summarize issues found from all partitions and print them on console.
#
################################################################################
################################################################################

import argparse
import html
import os
import re
from coreclr_arguments import *

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-diff_summary_dir", required=True, help="Path to diff summary directory")
parser.add_argument("-arch", required=True, help="Architecture")
parser.add_argument("-platform", required=True, help="OS platform")
parser.add_argument("-type", required=True, help="Type of diff (asmdiffs, tpdiff, all)")

target_windows = True


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

    coreclr_args.verify(args,
                        "type",
                        lambda type: type in ["asmdiffs", "tpdiff", "all"],
                        "Invalid type \"{}\"".format)

    do_asmdiffs = False
    do_tpdiff = False
    if coreclr_args.type == 'asmdiffs':
        do_asmdiffs = True
    if coreclr_args.type == 'tpdiff':
        do_tpdiff = True
    if coreclr_args.type == 'all':
        do_asmdiffs = True
        do_tpdiff = True

    if coreclr_args.platform.lower() != "windows" and do_asmdiffs:
        print("asmdiffs currently only implemented for windows")
        sys.exit(1)

    target_windows = coreclr_args.platform.lower() == "windows"

    return coreclr_args


def append_diff_file(f, file_name, full_file_path):
    """ Append a single summary file to the consolidated diff file.

    Args:
        f : File we are appending to
        arch (string): architecture we ran on
        file_name (string): base file name of file to append (not including path components)
        full_file_path (string): full path to file to append

    Returns:
        True if diffs were found in the file, False otherwise
    """

    diffs_found = False
    print("Appending {}".format(full_file_path))

    # What platform is this file summarizing? We parse the filename itself, which is of the form:
    #   superpmi_asmdiffs_summary_<platform>_<arch>.md
    #   superpmi_tpdiff_summary_<platform>_<arch>.md

    diff_os = "unknown"
    diff_arch = "unknown"
    match_obj = re.search(r'^superpmi_(tpdiff|asmdiffs)_summary_(.*)_(.*).md', file_name)
    if match_obj is not None:
        diff_os = match_obj.group(2)
        diff_arch = match_obj.group(3)

    with open(full_file_path, "r") as current_superpmi_md:
        contents = current_superpmi_md.read()

        # Were there actually any asm diffs? We currently look to see if the file contains the text "<empty>",
        # inserted by `superpmi_diffs.py`, instead of just not having a diff summary .md file.
        # (A missing file has the same effect.)
        match_obj = re.search(r'^<empty>', contents)
        if match_obj is not None:
            # There were no diffs in this file; don't add it to the result
            pass
        else:
            diffs_found = True
            f.write("## {} {}\n".format(diff_os, diff_arch))
            f.write(contents)
            f.write("\n\n---\n\n")

    return diffs_found


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    diff_summary_dir = coreclr_args.diff_summary_dir
    arch = coreclr_args.arch
    platform_name = coreclr_args.platform.lower()

    do_asmdiffs = False
    do_tpdiff = False
    if coreclr_args.type == 'asmdiffs':
        do_asmdiffs = True
    if coreclr_args.type == 'tpdiff':
        do_tpdiff = True
    if coreclr_args.type == 'all':
        do_asmdiffs = True
        do_tpdiff = True

    # Consolidate all superpmi_asmdiffs_summary_*.md and superpmi_tpdiff_summary_*.md
    # into overall_<asmdiffs|tpdiff>_summary_<os>_<architecture>.md.
    # (Don't name it "superpmi_xxx.md" or we might consolidate it into itself.)
    # If there are no summary files found, add a "No diffs found" text to be explicit about that.
    #
    # Note that we currently do this summarizing in an architecture-specific job. That means that diffs that run
    # in a Windows x64 job and those that run in a Windows x86 job will be summarized in two separate files.
    # We should create a job that depends on all the diff jobs, downloads all the .md file artifacts,
    # and consolidates everything together in one file.

    final_md_path = os.path.join(diff_summary_dir, "overall_{}_summary_{}_{}.md".format(coreclr_args.type, platform_name, arch))
    print("Consolidating final {}".format(final_md_path))
    with open(final_md_path, "a") as f:

        if do_asmdiffs:
            f.write("# ASM diffs generated on {} {}\n\n".format(platform_name, arch))

            any_asmdiffs_found = False
            for dirpath, _, files in os.walk(diff_summary_dir):
                for file_name in files:
                    if file_name.startswith("superpmi_asmdiffs") and file_name.endswith(".md") and "_short_" not in file_name:
                        full_file_path = os.path.join(dirpath, file_name)
                        if append_diff_file(f, file_name, full_file_path):
                            any_asmdiffs_found = True

            if not any_asmdiffs_found:
                f.write("No asmdiffs found\n")

        if do_tpdiff:
            f.write("# Throughput impact on {} {}\n\n".format(platform_name, arch))
            f.write("The following shows the impact on throughput " +
                    "in terms of number of instructions executed inside the JIT. " +
                    "Negative percentages/lower numbers are better.\n\n")

            any_tpdiff_found = False
            for dirpath, _, files in os.walk(diff_summary_dir):
                for file_name in files:
                    if file_name.startswith("superpmi_tpdiff") and file_name.endswith(".md"):
                        full_file_path = os.path.join(dirpath, file_name)
                        if append_diff_file(f, file_name, full_file_path):
                            any_tpdiff_found = True

            if not any_tpdiff_found:
                f.write("No throughput diffs found\n")

    with open(final_md_path, "r") as f:
        print(f.read())

    # AzDO does not support syntax highlighting in fenced blocks. This hack rewrites ```diff blocks to HTML for AzDO purposes.
    print("Coloring for AzDO...")
    with open(final_md_path, "r") as f:
        lines = f.read().splitlines()

    inside_diff = False
    cur_diff_lines = []
    new_lines = []
    for line in lines:
        if line.startswith("```diff"):
            inside_diff = True
            cur_diff_lines = []
        elif inside_diff and line.startswith("```"):
            inside_diff = False
            new_lines.append(html_color_diff(cur_diff_lines))
        elif inside_diff:
            cur_diff_lines.append(html.escape(line, False))
        else:
            new_lines.append(line)

    with open(final_md_path, "w") as f:
        for line in new_lines:
            f.write(line)
            f.write("\n")

    print("##vso[task.uploadsummary]{}".format(final_md_path))

    return 0


def html_color_diff(lines):
    new_text = ""

    addition_line_color = "rgba(46,160,67,0.15)"
    deletion_line_color = "rgba(248,81,73,0.15)"

    cur_block = None
    cur_block_color = None

    def commit_block():
        nonlocal new_text, cur_block, cur_block_color
        if cur_block is None:
            return

        style = ""

        if cur_block_color is not None:
            style = ' style="background-color:{}"'.format(cur_block_color)

        new_block_text = '<div{}>'.format(style) + cur_block + "</div>"
        new_text += new_block_text

    def add_block_line(line, color):
        nonlocal cur_block, cur_block_color

        if cur_block_color != color:
            commit_block()
            cur_block_color = color
            cur_block = line
        else:
            if cur_block is None:
                cur_block = line
            else:
                cur_block += "\n" + line

    for line in lines:
        if line.startswith("+"):
            add_block_line(line, addition_line_color)
        elif line.startswith("-"):
            add_block_line(line, deletion_line_color)
        else:
            add_block_line(line, None)

    commit_block()
    return "<pre><code>" + new_text + "</code></pre>"


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
