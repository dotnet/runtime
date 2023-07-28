#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: fuzzlyn_summarize.py
#
# Notes:
#
# Script to summarize issues found from all partitions and print them on console.
#
################################################################################
################################################################################
# import sys
import argparse
import json
import os
import re
import zipfile
from collections import defaultdict
from os import walk
from coreclr_arguments import *

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-issues_directory", help="Path to issues directory")
parser.add_argument("-arch", help="Architecture")
parser.add_argument("-platform", help="OS platform")
parser.add_argument("-build_config", help="Build configuration of runtime under test")

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

def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    arch = coreclr_args.arch
    platform = coreclr_args.platform
    build_config = coreclr_args.build_config
    issues_directory = coreclr_args.issues_directory

    # partition_results[partition_name] = { summary: _, examples: [], reduced_examples: [(seed, source)] }
    partition_results = {}

    def ensure_partition(name):
        if name not in partition_results:
            partition_results[name] = { "examples": [], "summary": None, "reduced_examples": [] }

    for file_path, dirs, files in walk(issues_directory, topdown=True):
        for file_name in files:
            if file_name.startswith("issues-summary-") and "Partition" in file_name:
                partition_name = os.path.splitext(file_name)[0].split("-")[-1]
                ensure_partition(partition_name)

                issues_summary_file = os.path.join(file_path, file_name)
                with open(issues_summary_file, "r") as sf:
                    events = [json.loads(x) for x in sf.readlines()]

                summary = next((x["RunSummary"] for x in events if x["Kind"] == "RunSummary"), None)
                if summary is not None:
                    partition_results[partition_name]["summary"] = summary

                examples = [x["Example"] for x in events if x["Kind"] == "ExampleFound"]
                partition_results[partition_name]["examples"].extend(examples)
            elif file_name.startswith("AllIssues-") and "Partition" in file_name:
                partition_name = os.path.splitext(file_name)[0].split("-")[-1]
                ensure_partition(partition_name)

                with zipfile.ZipFile(os.path.join(file_path, file_name)) as zip:
                    reduced_source_file_names = [x for x in zip.namelist() if x.endswith(".cs")]
                    def seed_from_internal_zip_path(path):
                        """ Given x/y/12345.cs, return 12345 """
                        return int(os.path.splitext(path.split("/")[-1])[0])

                    reduced_examples = [(seed_from_internal_zip_path(path), zip.read(path).decode("utf8").strip()) for path in reduced_source_file_names]
                    partition_results[partition_name]["reduced_examples"].extend(reduced_examples)

    total_examples_generated = 0
    total_examples_with_known_errors = 0
    all_reduced_examples = []
    all_examples = []
    for partition_name, results in partition_results.items():
        if results['summary'] is not None:
            # {"DegreeOfParallelism":32,"TotalProgramsGenerated":354,"TotalProgramsWithKnownErrors":11,"TotalRunTime":"00:00:47.0918613"}
            total_examples_generated += results['summary']['TotalProgramsGenerated']
            total_examples_with_known_errors += results['summary']['TotalProgramsWithKnownErrors']

        all_reduced_examples.extend(results['reduced_examples'])
        all_examples.extend(results['examples'])

    unreduced_examples = []
    crashes_by_assert = defaultdict(list)
    remaining = []

    for example in all_examples:
        if any(seed for (seed, _) in all_reduced_examples if example['Seed'] == seed):
            # Was reduced
            continue

        unreduced_examples.append(example)
        if example['Kind'] == "Crash" or example['Kind'] == "HitsJitAssert":
            assertion_error = extract_assertion_error(example['Message'])
            if assertion_error:
                crashes_by_assert[assertion_error].append(example)
            else:
                remaining.append(example)
        else:
            remaining.append(example)

    md_name = "Summary of Fuzzlyn run"
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
        f.write("# General information about run\n")

        if platform:
            f.write("* Platform: {}\n".format(platform))
        if arch:
            f.write("* Architecture: {}\n".format(arch))
        if build_config:
            f.write("* Build config: {}\n".format(build_config))

        f.write("* Total programs generated: {}\n".format(total_examples_generated))
        f.write("* Number of examples found: {}\n".format(len(all_examples)))
        f.write("* Number of known errors hit: {}\n".format(total_examples_with_known_errors))

        f.write("\n")

        if len(all_reduced_examples) > 0:
            f.write("# {} reduced examples are available\n".format(len(all_reduced_examples)))
            for (_, source) in sorted(all_reduced_examples, key=lambda p: len(p[1])):
                f.write("```csharp\n")
                f.write(source.replace("\r", "") + "\n")
                f.write("```\n\n")

        if len(crashes_by_assert) > 0:
            f.write("# {} distinct assertion errors seen\n".format(len(crashes_by_assert)))
            for error, examples in sorted(crashes_by_assert.items(), key=lambda p: len(p[1]), reverse=True):
                f.write("## ({} occurrences) {}\n".format(len(examples), error))
                if len(examples) > 1:
                    f.write("Example occurrence:\n")
                f.write("```scala\n")
                f.write(examples[0]['Message'].strip() + "\n")
                f.write("```\n")
                f.write("Affected seeds{}:\n".format(" (10 shown)" if len(examples) > 10 else ""))
                f.write("\n".join("* `" + str(ex['Seed']) + "`" for ex in sorted(examples[:10], key=lambda ex: ex['Seed'])))
                f.write("\n\n")

        if len(remaining) > 0:
            f.write("# {} uncategorized/unreduced examples remain\n".format(len(remaining)))
            for ex in remaining:
                f.write("* `{}`: {}\n".format(ex['Seed'], ex['Kind']))
                if ex['Message'] and len(ex['Message'].strip()) > 0:
                    f.write("```scala\n")
                    f.write(ex['Message'].strip() + "\n")
                    f.write("```\n")

            f.write("\n")

        if len(partition_results) > 0:
            f.write("# Run summaries per partition\n")
            f.write("|Partition|# Programs generated|# Examples found|# Examples with known errors|Run time|Degree of parallelism|\n")
            f.write("|---|---|---|---|---|---|\n")
            for partition_name, results in sorted(partition_results.items(), key=lambda p: p[0]):
                summary = results['summary']
                if summary is not None:
                    # {"DegreeOfParallelism":32,"TotalProgramsGenerated":354,"TotalProgramsWithKnownErrors":11,"TotalRunTime":"00:00:47.0918613"}
                    f.write("|{}|{}|{}|{}|{}|{}|\n".format(partition_name, summary['TotalProgramsGenerated'], len(results['examples']), summary['TotalProgramsWithKnownErrors'], summary['TotalRunTime'], summary['DegreeOfParallelism']))

    print("##vso[task.uploadsummary]{}".format(md_path))

    with open(md_path, "r") as f:
        print(f.read())

    return -1 if len(all_examples) > 0 else 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
