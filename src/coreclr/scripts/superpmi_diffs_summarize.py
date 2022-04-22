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
import os
import re
from coreclr_arguments import *

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-diff_summary_dir", help="Path to diff summary directory")
parser.add_argument("-arch", help="Architecture")

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

    return coreclr_args


def append_diff_file(f, arch, file_name, full_file_path):
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
    #   superpmi_diff_summary_<platform>_<arch>.md

    diff_os = "unknown"
    diff_arch = "unknown"
    match_obj = re.search(r'^superpmi_diff_summary_(.*)_(.*).md', file_name)
    if match_obj is not None:
        diff_os = match_obj.group(1)
        diff_arch = match_obj.group(2)

    with open(full_file_path, "r") as current_superpmi_md:
        contents = current_superpmi_md.read()

        # Were there actually any asm diffs? We currently look to see if the file contains the text "No diffs found",
        # inserted by `superpmi_diffs.py`, instead of just not having a diff summary .md file.
        # (A missing file has the same effect.)
        match_obj = re.search(r'^No diffs found', contents)
        if match_obj is not None:
            # There were no diffs in this file; don't add it to the result
            pass
        else:
            diffs_found = True
            # Write a header for this summary, and create a <summary><details> ... </details> disclosure
            # section around the file.
            f.write("""\

## {0} {1}

<details>

<summary>{0} {1} details</summary>

Summary file: `{2}`

To reproduce these diffs on Windows {3}:
```
superpmi.py asmdiffs -target_os {0} -target_arch {1} -arch {3}
```

""".format(diff_os, diff_arch, file_name, arch))

            # Now write the contents
            f.write(contents)

            # Write the footer (close the <details> section)
            f.write("""\

</details>

""")

    return diffs_found


def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)

    diff_summary_dir = coreclr_args.diff_summary_dir
    arch = coreclr_args.arch

    # Consolidate all superpmi_diff_summary_*.md in overall_diff_summary_<os>_<architecture>.md
    # (Don't name it "superpmi_xxx.md" or we might consolidate it into itself.)
    # If there are no summary files found, add a "No diffs found" text to be explicit about that.
    #
    # Note that we currently do this summarizing in an architecture-specific job. That means that diffs run
    # in a Windows x64 job and those run in a Windows x86 job will be summarized in two separate files.
    # We should create a job that depends on all the diff jobs, downloads all the .md file artifacts,
    # and consolidates everything together in one file.

    any_diffs_found = False

    final_md_path = os.path.join(diff_summary_dir, "overall_diff_summary_windows_{}.md".format(arch))
    print("Consolidating final {}".format(final_md_path))
    with open(final_md_path, "a") as f:

        f.write("""\
# ASM diffs generated on Windows {}
""".format(arch))

        for dirpath, _, files in os.walk(diff_summary_dir):
            for file_name in files:
                if file_name.startswith("superpmi_") and file_name.endswith(".md"):
                    full_file_path = os.path.join(dirpath, file_name)
                    if append_diff_file(f, arch, file_name, full_file_path):
                        any_diffs_found = True

        if not any_diffs_found:
            f.write("""\

No diffs found
""")

    print("##vso[task.uploadsummary]{}".format(final_md_path))

    with open(final_md_path, "r") as f:
        print(f.read())

    return 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
