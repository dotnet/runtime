#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: antigen_run.py
#
# Notes:
#
# Script to execute Antigen tool on a platform and return back the repro
# issues they found.
#
################################################################################
################################################################################

import shutil
import argparse
from os import path, walk
from os.path import getsize
import os
from coreclr_arguments import *
from jitutil import run_command, TempDir

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-run_configuration", help="RunConfiguration")
parser.add_argument("-antigen_directory", help="Path to antigen tool")
parser.add_argument("-output_directory", help="Path to output directory")
parser.add_argument("-partition", help="Partition name")
parser.add_argument("-core_root", help="path to CORE_ROOT directory")
parser.add_argument("-run_duration", help="Run duration in minutes")
is_windows = platform.system() == "Windows"


def setup_args(args):
    """ Setup the args

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
                        "antigen_directory",
                        lambda antigen_directory: os.path.isdir(antigen_directory),
                        "antigen_directory doesn't exist")

    coreclr_args.verify(args,
                        "output_directory",
                        lambda unused: True,
                        "output_directory doesn't exist")

    coreclr_args.verify(args,
                        "partition",
                        lambda unused: True,
                        "Unable to set partition")

    coreclr_args.verify(args,
                        "core_root",
                        lambda core_root: os.path.isdir(core_root),
                        "core_root doesn't exist")

    coreclr_args.verify(args,
                        "run_duration",
                        lambda unused: True,
                        "Unable to set run_duration")
    return coreclr_args


def copy_issues(issues_directory, upload_directory, tag_name):
    """Copies issue files (only top 5 smallest files from each folder) into the upload_directory

    Args:
        issues_directory (string): Issues directory
        upload_directory (string): Upload directory
        tag_name (string): Tag name for zip file

    Returns:
        [type]: [description]
    """
    # Create upload_directory
    if not os.path.isdir(upload_directory):
        os.makedirs(upload_directory)

    # Create temp directory to copy all issues to upload. We don't want to create a sub-folder
    # inside issues_directory because that will also get included twice.
    with TempDir() as prep_directory:

        def sorter_by_size(pair):
            """ Sorts the pair (file_name, file_size) tuple in ascending order of file_size

            Args:
                pair ([(string, int)]): List of tuple of file_name, file_size
            """
            pair.sort(key=lambda x: x[1], reverse=False)
            return pair

        summary_of_summary = []
        for file_path, dirs, files in walk(issues_directory, topdown=True):
            filename_with_size = []
            # Credit: https://stackoverflow.com/a/19859907
            dirs[:] = [d for d in dirs]
            for name in files:
                if not name.lower().endswith(".g.cs"):
                    continue

                curr_file_path = path.join(file_path, name)
                size = getsize(curr_file_path)
                filename_with_size.append((curr_file_path, size))

            if len(filename_with_size) == 0:
                continue
            summary_file = path.join(file_path, "summary.txt")
            summary_of_summary.append("**** " + file_path)
            with open(summary_file, 'r') as sf:
                summary_of_summary.append(sf.read())
            filename_with_size.append((summary_file, 0))  # always copy summary.txt

            # Copy atmost 5 files from each bucket
            sorted_files = [f[0] for f in sorter_by_size(filename_with_size)[:6]]  # sorter_by_size(filename_with_size)[:6]
            print('### Copying below files from {0} to {1}:'.format(issues_directory, prep_directory))
            print('')
            print(os.linesep.join(sorted_files))
            for src_file in sorted_files:
                dst_file = src_file.replace(issues_directory, prep_directory)
                dst_directory = path.dirname(dst_file)
                if not os.path.exists(dst_directory):
                    os.makedirs(dst_directory)
                try:
                    shutil.copy2(src_file, dst_file)
                except PermissionError as pe_error:
                    print('Ignoring PermissionError: {0}'.format(pe_error))

        issues_summary_file_name = "issues-summary-{}.txt".format(tag_name)
        print("Creating {} in {}".format(issues_summary_file_name, upload_directory))
        issues_summary_file = os.path.join(upload_directory, issues_summary_file_name)
        with open(issues_summary_file, 'w') as sf:
            sf.write(os.linesep.join(summary_of_summary))

        # Also copy the issues-summary inside zip folder
        dst_issue_summary_file = os.path.join(prep_directory, issues_summary_file_name)
        try:
            shutil.copy2(issues_summary_file, dst_issue_summary_file)
        except PermissionError as pe_error:
            print('Ignoring PermissionError: {0}'.format(pe_error))

        # Zip compress the files we will upload
        zip_path = os.path.join(prep_directory, "AllIssues-" + tag_name)
        print("Creating archive: " + zip_path)
        shutil.make_archive(zip_path, 'zip', prep_directory)

        zip_path += ".zip"
        dst_zip_path = os.path.join(upload_directory, "AllIssues-" + tag_name + ".zip")
        print("Copying {} to {}".format(zip_path, dst_zip_path))
        try:
            shutil.copy2(zip_path, dst_zip_path)
        except PermissionError as pe_error:
            print('Ignoring PermissionError: {0}'.format(pe_error))

        src_antigen_log = os.path.join(issues_directory, get_antigen_filename(tag_name))
        dst_antigen_log = os.path.join(upload_directory, get_antigen_filename(tag_name))
        print("Copying {} to {}".format(src_antigen_log, dst_antigen_log))
        try:
            shutil.copy2(src_antigen_log, dst_antigen_log)
        except PermissionError as pe_error:
            print('Ignoring PermissionError: {0}'.format(pe_error))

def get_antigen_filename(tag_name):
    return "Antigen-{}.log".format(tag_name)

def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    antigen_directory = coreclr_args.antigen_directory
    core_root = coreclr_args.core_root
    tag_name = "{}-{}".format(coreclr_args.run_configuration, coreclr_args.partition)
    output_directory = coreclr_args.output_directory
    run_duration = coreclr_args.run_duration
    if not run_duration:
        run_duration = 60

    path_to_corerun = os.path.join(core_root, "corerun")
    path_to_tool = os.path.join(antigen_directory, "Antigen")
    if is_windows:
        path_to_corerun += ".exe"
        path_to_tool += ".exe"

    if not is_windows:
        # Disable core dumps. The fuzzers have their own graceful handling for
        # runtime crashes. Especially on macOS we can quickly fill up the drive
        # with dumps if we find lots of crashes since dumps there are very big.
        import resource
        resource.setrlimit(resource.RLIMIT_CORE, (0, 0))

    try:
        # Run tool such that issues are placed in a temp folder
        with TempDir() as temp_location:
            antigen_log = path.join(temp_location, get_antigen_filename(tag_name))
            run_command([path_to_tool, "-c", path_to_corerun, "-o", temp_location, "-d", str(run_duration)], _exit_on_fail=True, _output_file= antigen_log)

            # Copy issues for upload
            print("Copying issues to " + output_directory)
            copy_issues(temp_location, output_directory, tag_name)
    except PermissionError as pe:
        print("Got error: %s", pe)

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
