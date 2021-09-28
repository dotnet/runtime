#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: antigen_unique_issues.py
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
from os import walk
from coreclr_arguments import *
import re

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-issues_directory", help="Path to issues directory")

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
                        "run_configuration",
                        lambda unused: True,
                        "Unable to set run_configuration")

    coreclr_args.verify(args,
                        "issues_directory",
                        lambda issues_directory: os.path.isdir(issues_directory),
                        "issues_directory doesn't exist")

    return coreclr_args

def print_unique_issues_summary(issues_directory):
    """Merge issues-summary-*-PartitionN.txt files from each partitions
    and print unique issues

    Args:
        issues_directory (string): Issues directory
    Returns:
        Number of issues found
    """

    issues_found = 0
    unique_issues_all_partitions = {}
    for file_path, dirs, files in walk(issues_directory, topdown=True):
        for file_name in files:
            if not file_name.startswith("issues-summary-") or  "Partition" not in file_name:
                continue

            issues_summary_file = os.path.join(file_path, file_name)
            partition_name = file_path.split(os.sep)[-1]
            add_header = True
            unique_issues = []
            with open(issues_summary_file, 'r') as sf:
                contents = sf.read()
                unique_issues = list(filter(None, re.split(unique_issue_dir_pattern, contents)))

            # Iterate over all unique issues of this partition
            for unique_issue in unique_issues:
                # Find the matching assertion message
                for assertion_pattern in assertion_patterns:
                    issue_match = re.search(assertion_pattern, unique_issue)
                    if issue_match is not None:
                        assert_string = " ".join(issue_match.groups())
                        # Check if previous partitions has already seen this assert
                        if assert_string not in unique_issues_all_partitions:
                            unique_issues_all_partitions[assert_string] = unique_issue
                            issues_found += 1
                            if add_header:
                                print("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%% {} %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%".format(partition_name))
                                add_header = False
                            print(unique_issue.strip())
                            print("------------------------------------")
                        break

    print("===== Found {} unique issues.".format(issues_found))
    return issues_found

def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    issues_directory = coreclr_args.issues_directory
    issues_found = print_unique_issues_summary(issues_directory)
    return 1 if issues_found > 0 else 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
