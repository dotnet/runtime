#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_diffs.py
#
# Notes:
#
# Script to do base-diff jit measurements for various collections on the Helix machines.
#
################################################################################
################################################################################

import argparse
from os import walk, path
import shutil
from coreclr_arguments import *
from jitutil import run_command, TempDir, determine_jit_name

parser = argparse.ArgumentParser(description="description")

host_os_help = "OS (windows, osx, linux). Default: current OS."

target_os_help = "Target OS, for use with cross-compilation JIT (windows, osx, linux). Default: current OS."

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-type", help="Type of diff (asmdiffs, tpdiff, all)")
parser.add_argument("-host_os", help=host_os_help)
parser.add_argument("-target_os", help=target_os_help)
parser.add_argument("-base_jit_directory", help="path to the directory containing base clrjit binaries")
parser.add_argument("-diff_jit_directory", help="path to the directory containing diff clrjit binaries")
parser.add_argument("-base_jit_options", help="Semicolon separated list of base jit options (in format A=B without DOTNET_ prefix)")
parser.add_argument("-diff_jit_options", help="Semicolon separated list of diff jit options (in format A=B without DOTNET_ prefix)")
parser.add_argument("-log_directory", help="path to the directory containing superpmi log files")


def check_target_os(coreclr_args, target_os):
    return (target_os is not None) and (target_os in coreclr_args.valid_host_os)


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
                        "type",
                        lambda type: type in ["asmdiffs", "tpdiff", "all"],
                        "Invalid type \"{}\"".format)

    coreclr_args.verify(args,
                        "target_os",
                        lambda target_os: check_target_os(coreclr_args, target_os),
                        lambda target_os: "Unknown target_os {}\nSupported OS: {}".format(target_os, (", ".join(coreclr_args.valid_host_os))),
                        modify_arg=lambda target_os: target_os if target_os is not None else coreclr_args.host_os)  # Default to `host_os`

    coreclr_args.verify(args,
                        "base_jit_directory",
                        lambda jit_directory: os.path.isdir(jit_directory),
                        "base_jit_directory doesn't exist")

    coreclr_args.verify(args,
                        "diff_jit_directory",
                        lambda jit_directory: os.path.isdir(jit_directory),
                        "diff_jit_directory doesn't exist")

    coreclr_args.verify(args,
                        "base_jit_options",
                        lambda unused: True,
                        "Unable to set base_jit_options")

    coreclr_args.verify(args,
                        "diff_jit_options",
                        lambda unused: True,
                        "Unable to set diff_jit_options")

    coreclr_args.verify(args,
                        "log_directory",
                        lambda log_directory: True,
                        "log_directory doesn't exist")

    return coreclr_args


class Diff:
    """ Class handling asmdiffs and tpdiff invocations
    """

    def __init__(self, coreclr_args):
        """ Constructor

        Args:
            coreclr_args (CoreclrArguments) : parsed args
            ...
        """
        self.coreclr_args = coreclr_args

        self.python_path = sys.executable
        self.script_dir = os.path.abspath(os.path.dirname(os.path.realpath(__file__)))

        # It doesn't really matter where we put the downloaded SPMI artifacts.
        # Here, they are put in <correlation_payload>/artifacts/spmi.
        self.spmi_location = os.path.join(self.script_dir, "artifacts", "spmi")

        self.log_directory = coreclr_args.log_directory
        self.host_os = coreclr_args.host_os
        self.target_os = coreclr_args.target_os
        self.arch_name = coreclr_args.arch
        self.host_arch_name = "x64" if self.arch_name.endswith("64") else "x86"

        # Core_Root is where the superpmi tools (superpmi.exe, mcs.exe) are expected to be found.
        # We pass the full path of the JITs to use as arguments.
        self.core_root_dir = self.script_dir

        # Assume everything succeeded. If any step fails, it will change this to True.
        self.failed = False

        # List of summary MarkDown files
        self.summary_md_files = []

    def download_mch(self):
        """ Download MCH files for the diff
        """
        print("Running superpmi.py download to get MCH files")

        log_file = os.path.join(self.log_directory, "superpmi_download_{}_{}.log".format(self.target_os, self.arch_name))
        run_command([
            self.python_path,
            os.path.join(self.script_dir, "superpmi.py"),
            "download",
            "--no_progress",
            "-core_root", self.core_root_dir,
            "-target_os", self.target_os,
            "-target_arch", self.arch_name,
            "-spmi_location", self.spmi_location,
            "-log_level", "debug",
            "-log_file", log_file], _exit_on_fail=True)

    def copy_dasm_files(self, upload_directory, tag_name):
        """ Copies .dasm files to a tempDirectory, zip it, and copy the compressed file to the upload directory.

        Args:
            upload_directory (string): Upload directory
            tag_name (string): tag_name used in zip file name.
        """

        print("Copy .dasm files")

        # Create upload_directory
        if not os.path.isdir(upload_directory):
            os.makedirs(upload_directory)

        dasm_file_present = False
        # Create temp directory to copy all issues to upload. We don't want to create a sub-folder
        # inside issues_directory because that will also get included twice.
        with TempDir() as prep_directory:
            for file_path, dirs, files in walk(self.spmi_location, topdown=True):
                # Credit: https://stackoverflow.com/a/19859907
                dirs[:] = [d for d in dirs]
                for name in files:
                    if not name.lower().endswith(".dasm"):
                        continue

                    dasm_src_file = path.join(file_path, name)
                    dasm_dst_file = dasm_src_file.replace(self.spmi_location, prep_directory)
                    dst_directory = path.dirname(dasm_dst_file)
                    if not os.path.exists(dst_directory):
                        os.makedirs(dst_directory)
                    try:
                        shutil.copy2(dasm_src_file, dasm_dst_file)
                        dasm_file_present = True
                    except PermissionError as pe_error:
                        print('Ignoring PermissionError: {0}'.format(pe_error))

            # If there are no diffs, create an zip file with single file in it.
            # Otherwise, Azdo considers it as failed job.
            # See https://github.com/dotnet/arcade/issues/8200
            if not dasm_file_present:
                no_diff = os.path.join(prep_directory, "nodiff.txt")
                with open(no_diff, "w") as no_diff_file:
                    no_diff_file.write("No diffs found!")

            # Zip compress the files we will upload
            zip_path = os.path.join(prep_directory, "Asmdiffs_" + tag_name)
            print("Creating archive: " + zip_path)
            shutil.make_archive(zip_path, 'zip', prep_directory)

            zip_path += ".zip"
            dst_zip_path = os.path.join(upload_directory, "Asmdiffs_" + tag_name + ".zip")
            print("Copying {} to {}".format(zip_path, dst_zip_path))
            try:
                shutil.copy2(zip_path, dst_zip_path)
            except PermissionError as pe_error:
                print('Ignoring PermissionError: {0}'.format(pe_error))

    def do_asmdiffs(self):
        """ Run asmdiffs
        """

        print("Running asmdiffs")

        # Find the built jit-analyze and put its directory on the PATH
        jit_analyze_dir = os.path.join(self.script_dir, "jit-analyze")
        if not os.path.isdir(jit_analyze_dir):
            print("Error: jit-analyze not found in {} (continuing)".format(jit_analyze_dir))
        else:
            # Put the jit-analyze directory on the PATH so superpmi.py can find it.
            print("Adding jit-analyze directory {} to PATH".format(jit_analyze_dir))
            os.environ["PATH"] = jit_analyze_dir + os.pathsep + os.environ["PATH"]

        # Find the portable `git` installation, and put `git.exe` on the PATH, for use by `jit-analyze`.
        git_directory = os.path.join(self.script_dir, "git", "cmd")
        git_exe_tool = os.path.join(git_directory, "git.exe")
        if not os.path.isfile(git_exe_tool):
            print("Error: `git` not found at {} (continuing)".format(git_exe_tool))
        else:
            # Put the git/cmd directory on the PATH so jit-analyze can find it.
            print("Adding git directory {} to PATH".format(git_directory))
            os.environ["PATH"] = git_directory + os.pathsep + os.environ["PATH"]

        # Figure out which JITs to use
        jit_name = determine_jit_name(self.host_os, self.target_os, self.host_arch_name, self.arch_name, use_cross_compile_jit=True)
        base_checked_jit_path = os.path.join(self.coreclr_args.base_jit_directory, "checked", jit_name)
        diff_checked_jit_path = os.path.join(self.coreclr_args.diff_jit_directory, "checked", jit_name)

        log_file = os.path.join(self.log_directory, "superpmi_asmdiffs_{}_{}.log".format(self.target_os, self.arch_name))

        # This is the summary file name and location written by superpmi.py. If the file exists, remove it to ensure superpmi.py doesn't created a numbered version.
        overall_md_asmdiffs_summary_file = os.path.join(self.spmi_location, "diff_summary.md")
        if os.path.isfile(overall_md_asmdiffs_summary_file):
            os.remove(overall_md_asmdiffs_summary_file)

        overall_md_asmdiffs_summary_file_target = os.path.join(self.log_directory, "superpmi_asmdiffs_summary_{}_{}.md".format(self.target_os, self.arch_name))
        self.summary_md_files.append((overall_md_asmdiffs_summary_file, overall_md_asmdiffs_summary_file_target))

        _, _, return_code = run_command([
            self.python_path,
            os.path.join(self.script_dir, "superpmi.py"),
            "asmdiffs",
            "--no_progress",
            "-core_root", self.core_root_dir,
            "-target_os", self.target_os,
            "-target_arch", self.arch_name,
            "-arch", self.host_arch_name,
            "-base_jit_path", base_checked_jit_path,
            "-diff_jit_path", diff_checked_jit_path,
            "-spmi_location", self.spmi_location,
            "-error_limit", "100",
            "-log_level", "debug",
            "-log_file", log_file] + self.create_jit_options_args())

        if return_code != 0:
            print("Failed during asmdiffs. Log file: {}".format(log_file))
            self.failed = True

        # Prepare .dasm files to upload to AzDO
        self.copy_dasm_files(self.log_directory, "{}_{}".format(self.target_os, self.arch_name))

    def do_tpdiff(self):
        """ Run tpdiff
        """

        print("Running tpdiff")

        # Figure out which JITs to use
        jit_name = determine_jit_name(self.host_os, self.target_os, self.host_arch_name, self.arch_name, use_cross_compile_jit=True)
        base_release_jit_path = os.path.join(self.coreclr_args.base_jit_directory, "release", jit_name)
        diff_release_jit_path = os.path.join(self.coreclr_args.diff_jit_directory, "release", jit_name)

        log_file = os.path.join(self.log_directory, "superpmi_tpdiff_{}_{}.log".format(self.target_os, self.arch_name))

        # This is the summary file name and location written by superpmi.py. If the file exists, remove it to ensure superpmi.py doesn't created a numbered version.
        overall_md_tpdiff_summary_file = os.path.join(self.spmi_location, "tpdiff_summary.md")
        if os.path.isfile(overall_md_tpdiff_summary_file):
            os.remove(overall_md_tpdiff_summary_file)

        overall_md_tpdiff_summary_file_target = os.path.join(self.log_directory, "superpmi_tpdiff_summary_{}_{}.md".format(self.target_os, self.arch_name))
        self.summary_md_files.append((overall_md_tpdiff_summary_file, overall_md_tpdiff_summary_file_target))

        _, _, return_code = run_command([
            self.python_path,
            os.path.join(self.script_dir, "superpmi.py"),
            "tpdiff",
            "--no_progress",
            "-core_root", self.core_root_dir,
            "-target_os", self.target_os,
            "-target_arch", self.arch_name,
            "-arch", self.host_arch_name,
            "-base_jit_path", base_release_jit_path,
            "-diff_jit_path", diff_release_jit_path,
            "-spmi_location", self.spmi_location,
            "-error_limit", "100",
            "-log_level", "debug",
            "-log_file", log_file] + self.create_jit_options_args())

        if return_code != 0:
            print("Failed during tpdiff. Log file: {}".format(log_file))
            self.failed = True

    def create_jit_options_args(self):
        options = []
        if self.coreclr_args.base_jit_options is not None:
            for v in self.coreclr_args.base_jit_options.split(';'):
                options += "-base_jit_option", v

        if self.coreclr_args.diff_jit_options is not None:
            for v in self.coreclr_args.diff_jit_options.split(';'):
                options += "-diff_jit_option", v

        return options

    def summarize(self):
        """ Summarize the diffs
        """
        # If there are diffs, we'll get summary md files in the spmi_location directory.
        # If there are no diffs, we still want to create this file and indicate there were no diffs.

        for source, target in self.summary_md_files:
            if os.path.isfile(source):
                try:
                    print("Copying summary file {} -> {}".format(source, target))
                    shutil.copy2(source, target)
                except PermissionError as pe_error:
                    print('Ignoring PermissionError: {0}'.format(pe_error))
            else:
                # Write a basic summary file. Ideally, we should not generate a summary.md file. However, Helix will report
                # errors when the Helix work item fails to upload this specified file if it doesn't exist. We should change the
                # upload to be conditional, or otherwise not error.
                with open(target, "a") as f:
                    f.write("<empty>")


def main(main_args):
    """ Run base-diff JIT measurements on the Helix machines.

    See superpmi_diffs_setup.py for how the directory structure is set up in the
    correlation payload. This script lives in the root of that directory tree.

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    do_asmdiffs = False
    do_tpdiff = False
    if coreclr_args.type == 'asmdiffs':
        do_asmdiffs = True
    if coreclr_args.type == 'tpdiff':
        do_tpdiff = True
    if coreclr_args.type == 'all':
        do_asmdiffs = True
        do_tpdiff = True

    diff = Diff(coreclr_args)

    diff.download_mch()

    if do_asmdiffs:
        diff.do_asmdiffs()
    if do_tpdiff:
        diff.do_tpdiff()

    diff.summarize()

    if diff.failed:
        print("Failure")
        return 1

    return 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
