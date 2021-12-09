#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title: get_commit_hash.py
#
# Notes:
#
# Script to store git commit hash of certain paths and store them in a file.
# Used by CacheTask@2 task of yml pipeline.
#
################################################################################
################################################################################

import argparse
import os
from coreclr_arguments import *
from jitutil import run_command

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-configuration", help="Configuration text to identify the type of build. Might be OS/Arch, etc.")
parser.add_argument("-run_type", help="Type of run. Options: 'coreclr', 'libraries'")
parser.add_argument("-output", help="Path to output file")

# Dictionary that maps the type of build to the file paths that build is affected by.
# Commit hashes of all the paths for a specific builds are fetched and stored in an
# output file. The output file is helpful in determining if there were any changes in
# the paths and if a build is needed or not. If there were no changes, the cached build
# (if available) will be used instead of doing a build.

# 'common' is a special category. If anything is modified in one of those paths, we will
# always run all the builds. So, in future, for diagnostic purpose, if the cache is hit in
# scenarios where it was not supposed to hit, touching one of the paths listed under 'common'
# will guarantee that nothing is restored from cache. This happens because when we fetch the
# latest commit hash for those paths, it will always be a merge commit which will be different
# than the previous recorded commits and hence will create a unique cache key which will not
# be present in CacheTask.
paths = {
    'common': [
        'global.json',
        'Directory.Build.targets',
        'Directory.Build.props',
        'Directory.Solution.props',
#        'eng'
    ],
    'coreclr': [
        'src/coreclr',
        'src/native',
    ],
    'libraries': [
        'src/libraries',
        'src/coreclr/System.Private.CoreLib',
    ]
}


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
                        "configuration",
                        lambda unused: True,
                        "Unable to set configuration")

    coreclr_args.verify(args,
                        "run_type",
                        lambda unused: True,
                        "Unable to set run_type")

    coreclr_args.verify(args,
                        "output",
                        lambda unused: True,
                        "Unable to set paths")

    return coreclr_args


def create_commits_file(configuration, run_types, source_directory, output_file):
    """Creates a commit file containing hashes for all the paths specific
    to the run_types specified

    Args:
        configuration (string): Configuration text.
        run_types ([string]): Array of run_types.
        source_directory (string): Name of the source directory.
        output_file (string): Name of the output file.
    """

    with open(output_file, 'w') as of:
        of.write("'Configuration': {}".format(configuration))
        for run_type in run_types:
            for git_path in paths[run_type]:
                file_path = os.path.join(source_directory, git_path)
                if not os.path.exists(file_path):
                    continue
                git_output, _, _ = run_command(["git", "--no-pager", "log", "-n", "1", "--oneline", "--decorate", "--", file_path])
                git_output = git_output.decode("utf-8")

                if "(grafted)" in git_output:
                    # If the latest commit for git_path is the 'grafted', then just print 'grafted' instead of
                    # the actual commit, because it will change in every run and hence cache key will always be different.
                    git_output = "grafted{}".format(os.linesep)

                of.write("'{}': {}".format(git_path, git_output))

    print(" -- {} contents: ".format(output_file))
    with open(output_file, 'r') as of:
        print(of.read())

def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    coreclr_args = setup_args(main_args)
    configuration = coreclr_args.configuration
    run_type = coreclr_args.run_type
    output_file = coreclr_args.output
    current_directory = os.path.dirname(os.path.abspath(__file__))
    source_directory = os.path.join(current_directory, "..", "..", "..")

    access_token = os.environ['SYSTEM_ACCESSTOKEN']
    repo_url = os.environ['REPO_URI'].replace("https://dnceng", "https://{}".format(access_token))

    print("Repo url: {}".format(repo_url))
    print("Source directory: {}".format(source_directory))

    run_command(["git", "--version"], source_directory, _exit_on_fail=True)

    print(" -- Printing commit history")
    run_command(["git", "--no-pager", "log", "-100", "--oneline"], source_directory, _exit_on_fail=True)

    # We fetch upto 20 commits only when the repository is cloned. All the commits past 20 commits are grafted. If
    # the changes in PR are 20+ commits, we will get "grafted" commit hash of folders that are not touched.
    # Hence, fetch 80 more commits to hope to get accurate commit hash.
    #
    # If the PR branch has 100+ commits, we might still see similar issue, but changes are rare to have
    # a PR having 100+ commits. In such case, the commit hash will be "grafted" everytime.
    print(" -- Fetching history of 200 commits from HEAD, so we can find the correct hash.")
    _, _, return_code = run_command(["git", "fetch", "--depth=100", repo_url, "HEAD"], source_directory)

    if return_code != 0:
        print("git fetch failed. Use grafted commit hashes.")

    print(" -- Printing commit history")
    run_command(["git", "--no-pager", "log", "-1000", "--oneline"], source_directory,
                                   _exit_on_fail=True)

    print(" -- Creating commits file")
    create_commits_file(configuration, ['common', run_type], source_directory, output_file)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
