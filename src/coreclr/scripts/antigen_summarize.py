#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: antigen_summarize.py
#
# Notes:
#
# Script to identify unique issues from all partitions and print them on console.
#
################################################################################
################################################################################
# import sys
import argparse
import os
import re
from collections import defaultdict
from coreclr_arguments import *
from os import walk

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-issues_directory", help="Path to issues directory")
parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")
parser.add_argument("-build_config", help="Build configuration of runtime under test")

unique_issue_dir_pattern = re.compile(r"\*\*\*\* .*UniqueIssue\d+")
assertion_patterns = [re.compile(r"Assertion failed '(.*)' in '.*' during '(.*)'"),
                      re.compile(r"Assert failure\(PID \d+ \[0x[0-9a-f]+], Thread: \d+ \[0x[0-9a-f]+]\):(.*)")]

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
                        "issues_directory",
                        lambda issues_directory: os.path.isdir(issues_directory),
                        "issues_directory doesn't exist")

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

def extract_assertion_error(text):
    """ Extract assertion error from stderr output

    Args:
        text (string): The text that might contain an assertion
    Returns:
        The assertion as a string, or None if no assertion error is in the text.
    """

    for assertion_pattern in assertion_patterns:
        issue_match = re.search(assertion_pattern, text)
        if issue_match is not None:
            assert_string = " ".join(issue_match.groups())
            return assert_string.strip()

    return None

def print_unique_issues_summary(issues_directory, platform, arch, build_config):
    """Merge issues-summary-*-PartitionN.txt files from each partitions
    and print a summary to markdown

    Args:
        issues_directory (string): Issues directory
    Returns:
        Number of issues found
    """

    issues_by_assert = defaultdict(list)
    remaining_issues = defaultdict(int)
    for file_path, dirs, files in walk(issues_directory, topdown=True):
        for file_name in files:
            if not file_name.startswith("issues-summary-") or  "Partition" not in file_name:
                continue

            issues_summary_file = os.path.join(file_path, file_name)
            partition_name = file_path.split(os.sep)[-1]
            unique_issues = []
            with open(issues_summary_file, 'r') as sf:
                contents = sf.read()
                unique_issues = list(filter(None, re.split(unique_issue_dir_pattern, contents)))

            # Iterate over all unique issues of this partition
            for unique_issue in unique_issues:
                assertion_error = extract_assertion_error(unique_issue)
                if assertion_error:
                    issues_by_assert[assertion_error].append((partition_name, unique_issue))
                else:
                    remaining_issues[unique_issue] += 1

    md_name = "Summary of Antigen run"
    if platform or arch or build_config:
        md_name += " on"
    if platform:
        md_name += " " + platform
    if arch:
        md_name += " " + arch
    if build_config:
        md_name += " " + build_config

    md_name += ".md"

    md_path = os.path.join(issues_directory, md_name)
    with open(md_path, "w") as f:
        f.write("# General info about run\n")
        if platform:
            f.write("* Platform: {}\n".format(platform))
        if arch:
            f.write("* Architecture: {}\n".format(arch))
        if build_config:
            f.write("* Build config: {}\n".format(build_config))

        f.write("* Number of unique examples found: {}\n".format(len(issues_by_assert) + len(remaining_issues)))

        f.write("\n")

        if len(issues_by_assert) > 0:
            f.write("# {} distinct assertion errors seen\n".format(len(issues_by_assert)))
            for message, issues in sorted(issues_by_assert.items(), key=lambda p: len(p[1]), reverse=True):
                f.write("## ({} occurrences) {}\n".format(len(issues), message))
                (partition, issue) = issues[0]
                f.write("Example occurrence from {}:\n".format(partition))
                f.write("```scala\n")
                f.write(issue.strip() + "\n")
                f.write("```\n\n")

        if len(remaining_issues) > 0:
            f.write("# {} uncategorized issues found\n".format(len(remaining_issues)))
# Turned off since the output does not seem particularly useful
#            for issue, occurrences in sorted(remaining_issues.items(), key=lambda p: p[1], reverse=True):
#                f.write("## {} occurrences\n".format(occurrences))
#                f.write("```scala\n")
#                f.write(issue.strip() + "\n")
#                f.write("```\n\n")

    print("##vso[task.uploadsummary]{}".format(md_path))

    with open(md_path, "r") as f:
        print(f.read())

    return len(issues_by_assert) + len(remaining_issues)

def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    issues_directory = coreclr_args.issues_directory
    platform = coreclr_args.platform
    arch = coreclr_args.arch
    build_config = coreclr_args.build_config
    issues_found = print_unique_issues_summary(issues_directory, platform, arch, build_config)
    return 1 if issues_found > 0 else 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
