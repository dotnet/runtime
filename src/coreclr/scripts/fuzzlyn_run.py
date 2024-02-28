#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: fuzzlyn_run.py
#
# Notes:
#
# Script to execute Fuzzlyn tool on a platform and return back the repro
# issues they found.
#
################################################################################
################################################################################

import argparse
import json
import os
import re
import shutil
import threading
from jitutil import run_command, TempDir
from coreclr_arguments import *
from os import path

jit_assert_regex = re.compile(r"Assertion failed '(.*)' in '.*' during '(.*)'")

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-run_configuration", help="RunConfiguration")
parser.add_argument("-fuzzlyn_directory", help="Path to fuzzlyn tool directory")
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
                        "fuzzlyn_directory",
                        lambda fuzzlyn_directory: os.path.isdir(fuzzlyn_directory),
                        "fuzzlyn_directory doesn't exist")

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

def extract_jit_assertion_error(text):
    """ Extract a JIT assertion error
    
    Args:
        text (string): The text that might contain an assertion
    Returns:
        The assertion as a string, or None if no assertion error is in the text.
    """

    issue_match = re.search(jit_assert_regex, text)
    if issue_match is not None:
        assert_string = " ".join(issue_match.groups())
        return assert_string.strip()

    return None

class ReduceExamples(threading.Thread):
    def __init__(self, examples_file, examples_dir, fuzzlyn_path, host_path, exit_evt):
        super(ReduceExamples, self).__init__()
        self.examples_file = examples_file
        self.examples_dir = examples_dir
        self.fuzzlyn_path = fuzzlyn_path
        self.host_path = host_path
        self.exit_evt = exit_evt
        self.reduced_jit_asserts = set()

    def run(self):
        num_reduced = 0
        while not self.exit_evt.wait(0.5):
            try:
                new_line = self.examples_file.readline()
            except ValueError:
                # File closed, means other thread exited (probably ctrl-C)
                return

            if new_line:
                evt = json.loads(new_line)
                # Only reduce BadResult examples since crashes take very long to reduce.
                # We will still report crashes, just not with a reduced example.
                if evt["Kind"] == "ExampleFound":
                    ex = evt["Example"]
                    ex_assert_err = None

                    reduce_this = False
                    if ex["Kind"] == "BadResult":
                        reduce_this = True
                    elif ex["Kind"] == "HitsJitAssert":
                        ex_assert_err = extract_jit_assertion_error(ex["Message"])
                        reduce_this = ex_assert_err is not None and ex_assert_err not in self.reduced_jit_asserts

                    if reduce_this:
                        print("Reducing {}".format(ex['Seed']))
                        output_path = path.join(self.examples_dir, str(ex["Seed"]) + ".cs")
                        spmi_collections_path = path.join(self.examples_dir, str(ex["Seed"]) + "_spmi")
                        os.mkdir(spmi_collections_path)
                        cmd = [self.fuzzlyn_path,
                            "--host", self.host_path,
                            "--reduce",
                            "--seed", str(ex['Seed']),
                            "--collect-spmi-to", spmi_collections_path,
                            "--output", output_path]
                        run_command(cmd)
                        if path.exists(output_path):
                            num_reduced += 1
                            if num_reduced >= 5:
                                print("Skipping reduction of remaining examples (reached limit of 5)")
                                return

                            if ex_assert_err is not None:
                                self.reduced_jit_asserts.add(ex_assert_err)


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    fuzzlyn_directory = coreclr_args.fuzzlyn_directory
    core_root = coreclr_args.core_root
    tag_name = "{}-{}".format(coreclr_args.run_configuration, coreclr_args.partition)
    output_directory = coreclr_args.output_directory
    if not coreclr_args.run_duration:
        run_duration = 60 * 60 # 60 minutes by default
    else:
        run_duration = int(coreclr_args.run_duration) * 60 # Run for duration in seconds

    path_to_corerun = os.path.join(core_root, "corerun")
    path_to_tool = os.path.join(fuzzlyn_directory, "Fuzzlyn")
    if is_windows:
        path_to_corerun += ".exe"
        path_to_tool += ".exe"

    os.makedirs(output_directory, exist_ok=True)

    if not is_windows:
        # Disable core dumps. The fuzzers have their own graceful handling for
        # runtime crashes. Especially on macOS we can quickly fill up the drive
        # with dumps if we find lots of crashes since dumps there are very big.
        import resource
        resource.setrlimit(resource.RLIMIT_CORE, (0, 0))

    with TempDir() as temp_location:
        summary_file_name = "issues-summary-{}.txt".format(tag_name)
        summary_file_path = path.join(temp_location, summary_file_name)
        with open(summary_file_path, 'w'):
            pass

        upload_fuzzer_output_path = path.join(output_directory, "Fuzzlyn-{}.log".format(tag_name))

        with open(summary_file_path, 'r') as fp:
            exit_evt = threading.Event()
            reduce_examples = ReduceExamples(fp, temp_location, path_to_tool, path_to_corerun, exit_evt)
            reduce_examples.start()

            run_command([
                path_to_tool,
                "--seconds-to-run", str(run_duration),
                "--output-events-to", summary_file_path,
                "--host", path_to_corerun,
                "--parallelism", "-1",
                "--known-errors", "dotnet/runtime"],
                _exit_on_fail=True, _output_file=upload_fuzzer_output_path)

            exit_evt.set()
            reduce_examples.join()

        upload_summary_file_path = path.join(output_directory, summary_file_name)
        print("Copying summary: {} -> {}".format(summary_file_path, upload_summary_file_path))
        shutil.copy2(summary_file_path, upload_summary_file_path)

        upload_issues_zip_path = path.join(output_directory, "AllIssues-{}".format(tag_name))
        print("Creating zip {}.zip".format(upload_issues_zip_path))
        shutil.make_archive(upload_issues_zip_path, 'zip', temp_location)

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
