#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               : superpmi.py
#
# Notes:
#
# Script to orchestrate SuperPMI collections, replays, asm diffs, and SuperPMI
# data management. Note that some of the options provided by this script are
# also provided in our SuperPMI collect test. The test can be found here:
# https://github.com/dotnet/runtime/blob/master/src/tests/JIT/superpmi/superpmicollect.cs.
#
################################################################################
################################################################################

import argparse
import asyncio
import datetime
import json
import math
import os
import multiprocessing
import platform
import shutil
import subprocess
import sys
import tempfile
import time
import queue
import re
import string
import urllib
import urllib.request
import zipfile

import locale
locale.setlocale(locale.LC_ALL, '')  # Use '' for auto, or force e.g. to 'en_US.UTF-8'

import xml.etree.ElementTree

from collections import defaultdict
from sys import platform as _platform

from coreclr_arguments import *

################################################################################
# Azure Storage information
################################################################################

# We store several things in Azure Blob Storage:
# 1. SuperPMI collections
# 2. A copy of PMI.dll, as a fallback in case we need it but can't find it locally,
#    so we don't need to download dotnet/jitutils and build it ourselves.
#    (Note: if PMI is ever published as a package, we could just download that instead.)
# 3. A copy of coredistools. If, when doing asm diffs, a copy of the coredistools
#    library is not found in the Core_Root directory, we download a cached copy.
#    Note: it would be better to download and use the official coredistools
#    NuGet packages (like the setup-stress-dependencies scripts do).

az_account_name = "clrjit2"
az_superpmi_container_name = "superpmi"
az_collections_root_folder = "collections"
az_blob_storage_account_uri = "https://" + az_account_name + ".blob.core.windows.net/"
az_blob_storage_superpmi_container_uri = az_blob_storage_account_uri + az_superpmi_container_name

az_jitrollingbuild_container_name = "jitrollingbuild"
az_builds_root_folder = "builds"
az_blob_storage_jitrollingbuild_container_uri = az_blob_storage_account_uri + az_jitrollingbuild_container_name

################################################################################
# Argument Parser
################################################################################

description = """\
Script to run SuperPMI replay, ASM diffs, and collections.
The script also manages the Azure store of pre-created SuperPMI collection files.
Help for each individual command can be shown by asking for help on the individual command, for example
`superpmi.py collect --help`.
"""

collect_description = """\
Automate a SuperPMI collection.
"""

replay_description = """\
Run SuperPMI replay on one or more collections.
"""

asm_diff_description = """\
Run SuperPMI ASM diffs on one or more collections.
"""

upload_description = """\
Upload a collection to SuperPMI Azure storage.
"""

download_description = """\
Download collections from SuperPMI Azure storage.
Normally, collections are automatically downloaded to a local cache
as part of doing a 'replay' operation. This command allows you to
download without doing a 'replay'.
"""

list_collections_description = """\
List the existing collections in the SuperPMI Azure storage.
"""

log_file_help = "Write output to a log file. Requires --sequential."

jit_ee_version_help = """\
JIT/EE interface version (the JITEEVersionIdentifier GUID from corinfo.h in the format
'a5eec3a4-4176-43a7-8c2b-a05b551d4f49'). Default: if the mcs tool is found, assume it
was built with the same JIT/EE version as the JIT we are using, and run "mcs -printJITEEVersion"
to get that version. Otherwise, use "unknown-jit-ee-version".
"""

host_os_help = "OS (Windows_NT, OSX, Linux). Default: current OS."

arch_help = "Architecture (x64, x86, arm, arm64). Default: current architecture."

build_type_help = "Build type (Debug, Checked, Release). Default: Checked."

core_root_help = "Core_Root location. Optional; it will be deduced if possible from runtime repo root."

product_location_help = "Built Product directory location. Optional; it will be deduced if possible from runtime repo root."

spmi_location_help = """\
Directory in which to put SuperPMI files, such as downloaded MCH files, asm diffs, and repro .MC files.
Optional. Default is 'spmi' within the repo 'artifacts' directory.
"""

superpmi_collect_help = """\
Command to run SuperPMI collect over. Note that there cannot be any dotnet CLI commands
invoked inside this command, as they will fail due to the shim altjit being set.
"""

replay_mch_files_help = """\
MCH files, or directories containing MCH files, to use for replay. For each directory passed,
all recursively found MCH files in that directory root will be used. Files may either be a path
on disk or a URI to a MCH file to download. Use these MCH files instead of a collection from
the Azure Storage MCH file store. UNC paths will be downloaded and cached locally.
"""

filter_help = """\
Specify one or more filters to restrict the set of MCH files to download or use from the local cache.
A filter is a simple case-insensitive substring search against the MCH file path. If multiple filter
strings are specified, any maching path is accepted (it is "or", not "and").
"""

upload_mch_files_help = """\
MCH files, or directories containing MCH files, to upload. For each directory passed,
all recursively found MCH files in that directory root will be uploaded. MCT files are also uploaded.
"""

skip_cleanup_help = "Skip intermediate file removal."

break_on_assert_help = "Enable break on assert during SuperPMI replay."

break_on_error_help = "Enable break on error during SuperPMI replay."

force_download_help = """\
If downloading an MCH file, always download it. Don't use an existing file in the download location.
Normally, we don't download if the target directory exists. This forces download even if the
target directory already exists.
"""

# Start of parser object creation.

parser = argparse.ArgumentParser(description=description)

subparsers = parser.add_subparsers(dest='mode', required=True, help="Command to invoke")

# Create a parser for core_root. It can be specified directly,
# or computed from the script location and host OS, architecture, and build type:
#
#    script location implies repo root,
#    implies artifacts location,
#    implies test location from host OS, architecture, build type,
#    implies Core_Root path
#
# You normally use the default host OS, but for Azure Storage upload and other
# operations, it can be useful to allow it to be specified.

core_root_parser = argparse.ArgumentParser(add_help=False)

core_root_parser.add_argument("-arch", help=arch_help)
core_root_parser.add_argument("-build_type", default="Checked", help=build_type_help)
core_root_parser.add_argument("-host_os", help=host_os_help)
core_root_parser.add_argument("-core_root", help=core_root_help)

# Create a set of argument common to all commands that run SuperPMI.

superpmi_common_parser = argparse.ArgumentParser(add_help=False)

superpmi_common_parser.add_argument("-spmi_location", help=spmi_location_help)
superpmi_common_parser.add_argument("--break_on_assert", action="store_true", help=break_on_assert_help)
superpmi_common_parser.add_argument("--break_on_error", action="store_true", help=break_on_error_help)
superpmi_common_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)
superpmi_common_parser.add_argument("--sequential", action="store_true", help="Run SuperPMI in sequential mode. Default is to run in parallel for faster runs.")
superpmi_common_parser.add_argument("-log_file", help=log_file_help)

# subparser for collect
collect_parser = subparsers.add_parser("collect", description=collect_description, parents=[core_root_parser, superpmi_common_parser])

# Add required arguments
collect_parser.add_argument("collection_command", nargs='?', help=superpmi_collect_help)
collect_parser.add_argument("collection_args", nargs='?', help="Arguments to pass to the SuperPMI collect command.")

collect_parser.add_argument("--pmi", action="store_true", help="Run PMI on a set of directories or assemblies.")
collect_parser.add_argument("-pmi_assemblies", dest="pmi_assemblies", nargs="+", default=[], help="Pass a sequence of managed dlls or directories to recursively run PMI over while collecting. Required if --pmi is specified.")
collect_parser.add_argument("-pmi_location", help="Path to pmi.dll to use during PMI run. Optional; pmi.dll will be downloaded from Azure Storage if necessary.")
collect_parser.add_argument("-output_mch_path", help="Location to place the final MCH file.")
collect_parser.add_argument("--merge_mch_files", action="store_true", help="Merge multiple MCH files. Use the -mch_files flag to pass a list of MCH files to merge.")
collect_parser.add_argument("-mch_files", metavar="MCH_FILE", nargs='+', help="Pass a sequence of MCH files which will be merged. Required by --merge_mch_files.")
collect_parser.add_argument("--use_zapdisable", action="store_true", help="Sets COMPlus_ZapDisable=1 and COMPlus_ReadyToRun=0 when doing collection to cause NGEN/ReadyToRun images to not be used, and thus causes JIT compilation and SuperPMI collection of these methods.")

# Allow for continuing a collection in progress
collect_parser.add_argument("-temp_dir", help="Specify an existing temporary directory to use. Useful if continuing an ongoing collection process, or forcing a temporary directory to a particular hard drive. Optional; default is to create a temporary directory in the usual TEMP location.")
collect_parser.add_argument("--skip_collection_step", action="store_true", help="Do not run the collection step.")
collect_parser.add_argument("--skip_merge_step", action="store_true", help="Do not run the merge step.")
collect_parser.add_argument("--skip_clean_and_verify_step", action="store_true", help="Do not run the collection cleaning, TOC creation, and verifying step.")
collect_parser.add_argument("--skip_collect_mc_files", action="store_true", help="Do not collect .MC files")

# Create a set of argument common to all SuperPMI replay commands, namely basic replay and ASM diffs.
# Note that SuperPMI collection also runs a replay to verify the final MCH file, so many arguments
# common to replay are also applicable that that replay as well.

replay_common_parser = argparse.ArgumentParser(add_help=False)

replay_common_parser.add_argument("-mch_files", metavar="MCH_FILE", nargs='+', help=replay_mch_files_help)
replay_common_parser.add_argument("-filter", nargs='+', help=filter_help)
replay_common_parser.add_argument("-product_location", help=product_location_help)
replay_common_parser.add_argument("--force_download", action="store_true", help=force_download_help)
replay_common_parser.add_argument("-altjit", help="Replay with an altjit. Specify the filename of the altjit to use, e.g., 'clrjit_win_arm64_x64.dll'.")
replay_common_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)

# subparser for replay
replay_parser = subparsers.add_parser("replay", description=replay_description, parents=[core_root_parser, superpmi_common_parser, replay_common_parser])

# Add required arguments
replay_parser.add_argument("-jit_path", help="Path to clrjit. Defaults to Core_Root JIT.")

# subparser for asmDiffs
asm_diff_parser = subparsers.add_parser("asmdiffs", description=asm_diff_description, parents=[core_root_parser, superpmi_common_parser, replay_common_parser])

# Add required arguments
asm_diff_parser.add_argument("-base_jit_path", help="Path to baseline clrjit. Defaults to baseline JIT from rolling build, by computing baseline git hash.")
asm_diff_parser.add_argument("-diff_jit_path", help="Path to diff clrjit. Defaults to Core_Root JIT.")
asm_diff_parser.add_argument("-git_hash", help="Use this git hash as the current to use to find a baseline JIT. Defaults to current git hash of source tree.")
asm_diff_parser.add_argument("-base_git_hash", help="Use this git hash as the baseline JIT hash. Default: search for the baseline hash.")

asm_diff_parser.add_argument("--diff_with_code", action="store_true", help="Invoke Visual Studio Code to view any diffs.")
asm_diff_parser.add_argument("--diff_with_code_only", action="store_true", help="Invoke Visual Studio Code to view any diffs. Only run the diff command, do not run SuperPMI to regenerate diffs.")
asm_diff_parser.add_argument("--diff_jit_dump", action="store_true", help="Generate JitDump output for diffs. Default: only generate asm, not JitDump.")
asm_diff_parser.add_argument("--diff_jit_dump_only", action="store_true", help="Only diff JitDump output, not asm.")
asm_diff_parser.add_argument("-temp_dir", help="Specify a temporary directory used for a previous ASM diffs run (for which --skip_cleanup was used) to view the results. The replay command is skipped.")

# subparser for upload
upload_parser = subparsers.add_parser("upload", description=upload_description, parents=[core_root_parser])

upload_parser.add_argument("-mch_files", metavar="MCH_FILE", required=True, nargs='+', help=upload_mch_files_help)
upload_parser.add_argument("-az_storage_key", help="Key for the clrjit Azure Storage location. Default: use the value of the CLRJIT_AZ_KEY environment variable.")
upload_parser.add_argument("-jit_location", help="Location for the base clrjit. If not passed this will be assumed to be from the Core_Root.")
upload_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
upload_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)

# subparser for download
download_parser = subparsers.add_parser("download", description=download_description, parents=[core_root_parser])

download_parser.add_argument("-filter", nargs='+', help=filter_help)
download_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
download_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)
download_parser.add_argument("--force_download", action="store_true", help=force_download_help)
download_parser.add_argument("-mch_files", metavar="MCH_FILE", nargs='+', help=replay_mch_files_help)

# subparser for list-collections
list_collections_parser = subparsers.add_parser("list-collections", description=list_collections_description, parents=[core_root_parser])

list_collections_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
list_collections_parser.add_argument("--all", action="store_true", help="Show all MCH files, not just those for the specified (or default) JIT-EE version, OS, and architecture")
list_collections_parser.add_argument("--local", action="store_true", help="Show the local MCH download cache")
list_collections_parser.add_argument("-spmi_location", help=spmi_location_help)

################################################################################
# Helper functions
################################################################################

def is_zero_length_file(fpath):
    """ Determine if a file system path refers to an existing file that is zero length

    Args:
        fpath (str) : file system path to test

    Returns:
        bool : true if the path is an existing file that is zero length
    """
    return os.path.isfile(fpath) and os.stat(fpath).st_size == 0

def is_nonzero_length_file(fpath):
    """ Determine if a file system path refers to an existing file that is non-zero length

    Args:
        fpath (str) : file system path to test

    Returns:
        bool : true if the path is an existing file that is non-zero length
    """
    return os.path.isfile(fpath) and os.stat(fpath).st_size != 0

def make_safe_filename(s):
    """ Turn a string into a string usable as a single file name component; replace illegal characters with underscores.

    Args:
        s (str) : string to convert to a file name

    Returns:
        (str) : The converted string
    """
    def safe_char(c):
        if c.isalnum():
            return c
        else:
            return "_"
    return "".join(safe_char(c) for c in s)

def find_in_path(name, pathlist, matchFunc=os.path.isfile):
    """ Find a name (e.g., directory name or file name) in the file system by searching the directories
        in a `pathlist` (e.g., PATH environment variable that has been semi-colon
        split into a list).

    Args:
        name (str)              : name to search for
        pathlist (list)         : list of directory names to search
        matchFunc (str -> bool) : determines if the name is a match

    Returns:
        (str) The pathname of the object, or None if not found.
    """
    for dirname in pathlist:
        candidate = os.path.join(dirname, name)
        if matchFunc(candidate):
            return candidate
    return None

def find_file(filename, pathlist):
    """ Find a filename in the file system by searching the directories
        in a `pathlist` (e.g., PATH environment variable that has been semi-colon
        split into a list).

    Args:
        filename (str)          : name to search for
        pathlist (list)         : list of directory names to search

    Returns:
        (str) The pathname of the object, or None if not found.
    """
    return find_in_path(filename, pathlist)

def find_dir(dirname, pathlist):
    """ Find a directory name in the file system by searching the directories
        in a `pathlist` (e.g., PATH environment variable that has been semi-colon
        split into a list).

    Args:
        dirname (str)           : name to search for
        pathlist (list)         : list of directory names to search

    Returns:
        (str) The pathname of the object, or None if not found.
    """
    return find_in_path(dirname, pathlist, matchFunc=os.path.isdir)

def create_unique_directory_name(root_directory, base_name):
    """ Create a unique directory name by joining `root_directory` and `base_name`.
        If this name already exists, append ".1", ".2", ".3", etc., to the final
        name component until the full directory name is not found.

    Args:
        root_directory (str)     : root directory in which a new directory will be created
        base_name (str)          : the base name of the new directory name component to be added

    Returns:
        (str) The full absolute path of the new directory. The directory has been created.
    """

    root_directory = os.path.abspath(root_directory)
    full_path = os.path.join(root_directory, base_name)

    count = 1
    while os.path.isdir(full_path):
        new_full_path = os.path.join(root_directory, base_name + "." + str(count))
        count += 1
        full_path = new_full_path

    os.makedirs(full_path)
    return full_path

def get_files_from_path(path, matchFunc=lambda path: True):
    """ Return all files in a directory tree matching a criteria.

    Args:
        path (str)              : Either a single file to include, or a directory to traverse looking for matching
                                  files.
        matchFunc (str -> bool) : Criteria function determining if a file is added to the list

    Returns:
        Array of absolute paths of matching files
    """

    if not(os.path.isdir(path) or os.path.isfile(path)):
        print("Warning: \"{}\" is not a file or directory".format(path))
        return []

    path = os.path.abspath(path)

    files = []

    if os.path.isdir(path):
        for item in os.listdir(path):
            files += get_files_from_path(os.path.join(path, item), matchFunc)
    else:
        if matchFunc(path):
            files.append(path)

    return files

################################################################################
# Helper classes
################################################################################

class TempDir:
    def __init__(self, path=None):
        self.dir = tempfile.mkdtemp() if path is None else path
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.dir)
        return self.dir

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)
        # Note: we are using the global `args`, not coreclr_args. This works because
        # the `skip_cleanup` argument is not processed by CoreclrArguments, but is
        # just copied there.
        if not args.skip_cleanup:
            shutil.rmtree(self.dir)

class ChangeDir:
    def __init__(self, dir):
        self.dir = dir
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.dir)

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)

class AsyncSubprocessHelper:
    def __init__(self, items, subproc_count=multiprocessing.cpu_count(), verbose=False):
        self.items = items
        self.subproc_count = subproc_count
        self.verbose = verbose

        if 'win32' in sys.platform:
            # Windows specific event-loop policy & cmd
            asyncio.set_event_loop_policy(asyncio.WindowsProactorEventLoopPolicy())

    async def __get_item__(self, item, index, size, async_callback, *extra_args):
        """ Wrapper to the async callback which will schedule based on the queue
        """

        # Wait for the subproc_id queue to become free, meaning we have an available
        # processor to run a task (specifically, we are below our maximum allowed
        # parallelism). Then start running the sub process.
        subproc_id = await self.subproc_count_queue.get()

        print_prefix = ""
        if self.verbose:
            print_prefix = "[{}:{}]: ".format(index, size)

        await async_callback(print_prefix, item, *extra_args)

        # Add back to the queue, in case another process wants to run.
        self.subproc_count_queue.put_nowait(subproc_id)

    async def __run_to_completion__(self, async_callback, *extra_args):
        """ async wrapper for run_to_completion
        """

        # Create a queue with one entry for each of the threads we're
        # going to allow. By default, this will be one entry per cpu.
        # using subproc_count_queue.get() will block when we're running
        # a task on every CPU.
        chunk_size = self.subproc_count
        subproc_count_queue = asyncio.Queue(chunk_size)
        for item in range(chunk_size):
            subproc_count_queue.put_nowait(item)
        self.subproc_count_queue = subproc_count_queue

        # Create a 'tasks' list of async function calls, one for each item.
        # When all these calls complete, we're done.
        size = len(self.items)
        count = 1
        tasks = []
        for item in self.items:
            tasks.append(self.__get_item__(item, count, size, async_callback, *extra_args))
            count += 1

        # Inovke all the calls to __get_item__ concurrently and wait for them all to finish.
        await asyncio.gather(*tasks)

    def run_to_completion(self, async_callback, *extra_args):
        """ Run until the item queue has been depleted

             Notes:
            Acts as a wrapper to abstract the async calls to
            async_callback. Note that this will allow cpu_count
            amount of running subprocesses. Each time the queue
            is emptied, another process will start. Note that
            the python code is single threaded, it will just
            rely on async/await to start subprocesses at
            subprocess_count
        """

        reset_env = os.environ.copy()
        asyncio.run(self.__run_to_completion__(async_callback, *extra_args))
        os.environ.update(reset_env)

################################################################################
# SuperPMI Collect
################################################################################

class SuperPMICollect:
    """ SuperPMI Collect class

    Notes:
        The object is responsible for setting up a SuperPMI collection given
        the arguments passed into the script.
    """

    def __init__(self, coreclr_args):
        """ Constructor

        Args:
            coreclr_args (CoreclrArguments) : parsed args

        """

        if coreclr_args.host_os == "OSX":
            self.collection_shim_name = "libsuperpmi-shim-collector.dylib"
            self.mcs_tool_name = "mcs"
            self.corerun_tool_name = "corerun"
        elif coreclr_args.host_os == "Linux":
            self.collection_shim_name = "libsuperpmi-shim-collector.so"
            self.mcs_tool_name = "mcs"
            self.corerun_tool_name = "corerun"
        elif coreclr_args.host_os == "Windows_NT":
            self.collection_shim_name = "superpmi-shim-collector.dll"
            self.mcs_tool_name = "mcs.exe"
            self.corerun_tool_name = "corerun.exe"
        else:
            raise RuntimeError("Unsupported OS.")

        self.jit_path = os.path.join(coreclr_args.core_root, determine_jit_name(coreclr_args))
        self.superpmi_path = determine_superpmi_tool_path(coreclr_args)
        self.mcs_path = os.path.join(coreclr_args.core_root, self.mcs_tool_name)

        self.core_root = coreclr_args.core_root

        self.collection_command = coreclr_args.collection_command
        self.collection_args = coreclr_args.collection_args

        if coreclr_args.pmi:
            self.pmi_location = determine_pmi_location(coreclr_args)
            self.pmi_assemblies = coreclr_args.pmi_assemblies
            self.corerun = os.path.join(self.core_root, self.corerun_tool_name)

        self.coreclr_args = coreclr_args

    ############################################################################
    # Instance Methods
    ############################################################################

    def collect(self):
        """ Do the SuperPMI Collection.
        """

        # Pathname for a temporary .MCL file used for noticing SuperPMI replay failures against base MCH.
        self.base_fail_mcl_file = None

        # The base .MCH file path
        self.base_mch_file = None

        # Final .MCH file path
        self.final_mch_file = None

        # The .TOC file path for the clean thin unique .MCH file
        self.toc_file = None

        self.save_the_final_mch_file = False

        # Do a basic SuperPMI collect and validation:
        #   1. Collect MC files by running a set of sample apps.
        #   2. Create a merged thin unique MCH by using "mcs -merge -recursive -dedup -thin base.mch *.mc".
        #   3. Create a clean MCH by running SuperPMI over the MCH, and using "mcs -strip" to filter
        #      out any failures (if any).
        #   4. Create a TOC using "mcs -toc".
        #   5. Verify the resulting MCH file is error-free when running SuperPMI against it with the
        #      same JIT used for collection.
        #
        #   MCH files are big. If we don't need them anymore, clean them up right away to avoid
        #   running out of disk space in disk constrained situations.

        passed = False

        try:
            with TempDir(self.coreclr_args.temp_dir) as temp_location:
                # Setup all of the temp locations
                self.base_fail_mcl_file = os.path.join(temp_location, "basefail.mcl")
                self.base_mch_file = os.path.join(temp_location, "base.mch")

                self.temp_location = temp_location

                if self.coreclr_args.output_mch_path is not None:
                    self.final_mch_file = os.path.abspath(self.coreclr_args.output_mch_path)
                    final_mch_dir = os.path.dirname(self.final_mch_file)
                    if not os.path.isdir(final_mch_dir):
                        os.makedirs(final_mch_dir)
                else:
                    default_coreclr_bin_mch_location = os.path.join(self.coreclr_args.spmi_location, "mch", "{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))
                    if not os.path.isdir(default_coreclr_bin_mch_location):
                        os.makedirs(default_coreclr_bin_mch_location)
                    self.final_mch_file = os.path.abspath(os.path.join(default_coreclr_bin_mch_location, "{}.{}.{}.mch".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type)))

                self.save_the_final_mch_file = True
                self.toc_file = "{}.mct".format(self.final_mch_file)

                # If we have passed temp_dir, then we have a few flags we need
                # to check to see where we are in the collection process. Note that this
                # functionality exists to help not lose progress during a SuperPMI collection.

                # It is not unreasonable for the SuperPMI collection to take many hours
                # therefore allow re-use of a collection in progress

                if not self.coreclr_args.skip_collection_step:
                    self.__collect_mc_files__()

                if not self.coreclr_args.skip_merge_step:
                    if not self.coreclr_args.merge_mch_files:
                        self.__merge_mc_files__()
                    else:
                        self.__merge_mch_files__()

                if not self.coreclr_args.skip_clean_and_verify_step:
                    self.__create_clean_mch_file__()
                    self.__create_toc__()
                    self.__verify_final_mch__()

                passed = True

        except Exception as exception:
            print(exception)

        return passed

    ############################################################################
    # Helper Methods
    ############################################################################

    def __collect_mc_files__(self):
        """ Do the actual SuperPMI collection for a command

        Returns:
            None
        """

        if not self.coreclr_args.skip_collect_mc_files:
            assert os.path.isdir(self.temp_location)

            # Set environment variables.
            env_copy = os.environ.copy()
            env_copy["SuperPMIShimLogPath"] = self.temp_location
            env_copy["SuperPMIShimPath"] = self.jit_path
            env_copy["COMPlus_AltJit"] = "*"
            env_copy["COMPlus_AltJitNgen"] = "*"
            env_copy["COMPlus_AltJitName"] = self.collection_shim_name
            env_copy["COMPlus_EnableExtraSuperPmiQueries"] = "1"
            env_copy["COMPlus_TieredCompilation"] = "0"

            if self.coreclr_args.use_zapdisable:
                env_copy["COMPlus_ZapDisable"] = "1"
                env_copy["COMPlus_ReadyToRun"] = "0"

            print("Starting collection.")
            print("")
            print_platform_specific_environment_vars(self.coreclr_args, "SuperPMIShimLogPath", self.temp_location)
            print_platform_specific_environment_vars(self.coreclr_args, "SuperPMIShimPath", self.jit_path)
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJit", "*")
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJitNgen", "*")
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJitName", self.collection_shim_name)
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_EnableExtraSuperPmiQueries", "1")
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_TieredCompilation", "0")
            if self.coreclr_args.use_zapdisable:
                print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_ZapDisable", "1")
                print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_ReadyToRun", "0")
            print("")

            if self.collection_command is not None:
                print("%s %s" % (self.collection_command, " ".join(self.collection_args)))

                assert isinstance(self.collection_command, str)
                assert isinstance(self.collection_args, list)

                self.command = [self.collection_command,] + self.collection_args
                proc = subprocess.Popen(self.command, env=env_copy)
                proc.communicate()
                return_code = proc.returncode

            if self.coreclr_args.pmi is True:
                async def run_pmi(print_prefix, assembly, self):
                    """ Run pmi over all dlls
                    """

                    command = [self.corerun, self.pmi_location, "DRIVEALL", assembly]
                    command_string = " ".join(command)
                    print("{}{}".format(print_prefix, command_string))

                    # Save the stdout and stderr to files, so we can see if PMI wrote any interesting messages.
                    # Use the name of the assembly as the basename of the file. mkstemp() will ensure the file
                    # is unique.
                    root_output_filename = make_safe_filename("pmi_" + assembly + "_")
                    stdout_file_handle, stdout_filepath = tempfile.mkstemp(suffix=".stdout", prefix=root_output_filename, dir=self.temp_location)
                    stderr_file_handle, stderr_filepath = tempfile.mkstemp(suffix=".stderr", prefix=root_output_filename, dir=self.temp_location)

                    proc = await asyncio.create_subprocess_shell(
                        command_string,
                        stdout=stdout_file_handle,
                        stderr=stderr_file_handle)

                    await proc.communicate()

                    os.close(stdout_file_handle)
                    os.close(stderr_file_handle)

                    # No need to keep zero-length files
                    if is_zero_length_file(stdout_filepath):
                        os.remove(stdout_filepath)
                    if is_zero_length_file(stderr_filepath):
                        os.remove(stderr_filepath)

                assemblies = []
                for item in self.pmi_assemblies:
                    assemblies += get_files_from_path(item, matchFunc=lambda file: any(file.endswith(extension) for extension in [".dll", ".exe"]))

                # Set environment variables.
                old_env = os.environ.copy()
                os.environ.update(env_copy)

                helper = AsyncSubprocessHelper(assemblies, verbose=True)
                helper.run_to_completion(run_pmi, self)

                os.environ.update(old_env)

        mc_files = [os.path.join(self.temp_location, item) for item in os.listdir(self.temp_location) if item.endswith(".mc")]
        if len(mc_files) == 0:
            raise RuntimeError("No .mc files generated.")

    def __merge_mc_files__(self):
        """ Merge the mc files that were generated

        Notes:
            mcs -merge <s_baseMchFile> <s_tempDir>\*.mc -recursive -dedup -thin

        """

        pattern = os.path.join(self.temp_location, "*.mc")

        command = [self.mcs_path, "-merge", self.base_mch_file, pattern, "-recursive", "-dedup", "-thin"]
        print("Invoking: " + " ".join(command))
        proc = subprocess.Popen(command)
        proc.communicate()

        if not os.path.isfile(self.base_mch_file):
            raise RuntimeError("MCH file failed to be generated at: %s" % self.base_mch_file)

        # All the individual MC files are no longer necessary, now that we have
        # merged them into the base.mch. Delete them.
        if not self.coreclr_args.skip_cleanup:
            mc_files = [os.path.join(self.temp_location, item) for item in os.listdir(self.temp_location) if item.endswith(".mc")]
            for item in mc_files:
                os.remove(item)

    def __merge_mch_files__(self):
        """ Merge MCH files in the mch_files list. This is only used with the `--merge_mch_files` argument.

        Notes:
            mcs -concat <s_baseMchFile> [self.coreclr_args.mch_files]

        """

        for item in self.coreclr_args.mch_files:
            command = [self.mcs_path, "-concat", self.base_mch_file, item]
            print("Invoking: " + " ".join(command))
            proc = subprocess.Popen(command)
            proc.communicate()

        if not os.path.isfile(self.base_mch_file):
            raise RuntimeError("MCH file failed to be generated at: %s" % self.base_mch_file)

    def __create_clean_mch_file__(self):
        """ Create a clean mch file

        Notes:
            <SuperPMIPath> -p -f <s_baseFailMclFile> <s_baseMchFile> <jitPath>

            if <s_baseFailMclFile> is non-empty:
                <mcl> -strip <s_baseFailMclFile> <s_baseMchFile> <s_finalMchFile>
            else
                # copy/move base file to final file
            del <s_baseFailMclFile>
        """

        command = [self.superpmi_path, "-p", "-f", self.base_fail_mcl_file, self.base_mch_file, self.jit_path]
        print("Invoking: " + " ".join(command))
        proc = subprocess.Popen(command)
        proc.communicate()

        if is_nonzero_length_file(self.base_fail_mcl_file):
            command = [self.mcs_path, "-strip", self.base_fail_mcl_file, self.base_mch_file, self.final_mch_file]
            print("Invoking: " + " ".join(command))
            proc = subprocess.Popen(command)
            proc.communicate()
        else:
            # Ideally we could just rename this file instead of copying it.
            shutil.copy2(self.base_mch_file, self.final_mch_file)

        if not os.path.isfile(self.final_mch_file):
            raise RuntimeError("Final mch file failed to be generated.")

        if not self.coreclr_args.skip_cleanup:
            if os.path.isfile(self.base_fail_mcl_file):
                os.remove(self.base_fail_mcl_file)
                self.base_fail_mcl_file = None
            if os.path.isfile(self.base_mch_file):
                os.remove(self.base_mch_file)
                self.base_mch_file = None

    def __create_toc__(self):
        """ Create a TOC file

        Notes:
            <mcl> -toc <s_finalMchFile>
        """

        command = [self.mcs_path, "-toc", self.final_mch_file]
        print("Invoking: " + " ".join(command))
        proc = subprocess.Popen(command)
        proc.communicate()

        if not os.path.isfile(self.toc_file):
            raise RuntimeError("Error, toc file not created correctly at: %s" % self.toc_file)

    def __verify_final_mch__(self):
        """ Verify the resulting MCH file is error-free when running SuperPMI against it with the same JIT used for collection.

        Notes:
            <SuperPmiPath> -p -f <s_finalFailMclFile> <s_finalMchFile> <jitPath>
        """

        mch_files = [ self.final_mch_file ]
        spmi_replay = SuperPMIReplay(self.coreclr_args, mch_files, self.jit_path)
        passed = spmi_replay.replay()

        if not passed:
            raise RuntimeError("Error, unclean replay.")

################################################################################
# SuperPMI Replay helpers
################################################################################

def print_superpmi_failure_code(return_code, coreclr_args):
    """ Print a description of a superpmi return (error) code. If the return code is
        zero, meaning success, don't print anything.
    """
    if return_code == 0:
        # Don't print anything if the code is zero, which is success.
        pass
    elif return_code == -1:
        print("General fatal error.")
    elif return_code == -2:
        print("JIT failed to initialize.")
    elif return_code == 1:
        print("Compilation failures.")
    elif return_code == 2:
        print("Asm diffs found.")
    elif return_code == 139 and coreclr_args.host_os != "Windows_NT":
        print("Fatal error, SuperPMI has returned SIGSEGV (segmentation fault).")
    else:
        print("Unknown error code {}.".format(return_code))

def print_fail_mcl_file_method_numbers(fail_mcl_file):
    """ Given a SuperPMI ".mcl" file (containing a list of failure indices), print out the method numbers.
    """
    with open(fail_mcl_file) as file_handle:
        mcl_lines = file_handle.readlines()
        mcl_lines = [item.strip() for item in mcl_lines]
        fail_mcl_contents = os.linesep.join(mcl_lines)
        print("Method numbers with compilation failures:")
        print(fail_mcl_contents)

def save_repro_mc_files(temp_location, coreclr_args, repro_base_command_line):
    """ For commands that use the superpmi "-r" option to create "repro" .mc files, copy these to a
        location where they are saved (and not in a "temp" directory) for easy use by the user.
    """
    # If there are any .mc files, drop them into artifacts/repro/<host_os>.<arch>.<build_type>/*.mc
    mc_files = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if item.endswith(".mc")]
    if len(mc_files) > 0:
        repro_location = create_unique_directory_name(coreclr_args.spmi_location, "repro.{}.{}.{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))

        repro_files = []
        for item in mc_files:
            repro_files.append(os.path.join(repro_location, os.path.basename(item)))
            print("Copying {} -> {}".format(item, repro_location))
            shutil.copy2(item, repro_location)

        print("")
        print("Repro .mc files:")
        print("")
        for item in repro_files:
            print(item)
        print("")

        print("To run a specific failure (replace JIT path and .mc filename as needed):")
        print("")
        print("{} {}{}xxxxx.mc".format(repro_base_command_line, repro_location, os.path.sep))
        print("")

################################################################################
# SuperPMI Replay
################################################################################

class SuperPMIReplay:
    """ SuperPMI Replay class

    Notes:
        The object is responsible for replaying the MCH files given to the
        instance of the class
    """

    def __init__(self, coreclr_args, mch_files, jit_path):
        """ Constructor

        Args:
            coreclr_args (CoreclrArguments) : parsed args
            mch_files (list)                : list of MCH files to replay
            jit_path (str)                  : path to clrjit

        """

        self.jit_path = jit_path
        self.mch_files = mch_files
        self.superpmi_path = determine_superpmi_tool_path(coreclr_args)
        self.coreclr_args = coreclr_args

    ############################################################################
    # Instance Methods
    ############################################################################

    def replay(self):
        """ Replay the given SuperPMI collection

        Returns:
            (bool) True on success; False otherwise
        """

        result = False

        # Possible return codes from SuperPMI
        #
        # 0  : success
        # -1 : general fatal error (e.g., failed to initialize, failed to read files)
        # -2 : JIT failed to initialize
        # 1  : there were compilation failures
        # 2  : there were assembly diffs

        with TempDir() as temp_location:
            print("")
            print("Temp Location: {}".format(temp_location))
            print("")

            flags = [
                "-v", "ew", # only display errors and warnings
                "-r", os.path.join(temp_location, "repro") # Repro name, create .mc repro files
            ]

            altjit_string = "*" if self.coreclr_args.altjit else ""
            altjit_flags = [
                "-jitoption", "force", "AltJit=" + altjit_string,
                "-jitoption", "force", "AltJitNgen=" + altjit_string,
                "-jitoption", "force", "EnableExtraSuperPmiQueries=0"
            ]
            flags += altjit_flags

            if not self.coreclr_args.sequential:
                flags += [ "-p" ]

            if self.coreclr_args.break_on_assert:
                flags += [ "-boa" ]

            if self.coreclr_args.break_on_error:
                flags += [ "-boe" ]

            if self.coreclr_args.log_file is not None:
                flags += [ "-w", self.coreclr_args.log_file ]

            # For each MCH file that we are going to replay, do the replay and replay post-processing.
            #
            # Consider: currently, we loop over all the steps for each MCH file, including (1) invoke
            # SuperPMI, (2) process results. It might be better to do (1) for each MCH file, then
            # process all the results at once. Currently, the results for some processing can be
            # obscured by the normal run output for subsequent MCH files.

            for mch_file in self.mch_files:

                fail_mcl_file = os.path.join(temp_location, os.path.basename(mch_file) + "_fail.mcl")
                flags += [
                    "-f", fail_mcl_file, # Failing mc List
                ]

                command = [self.superpmi_path] + flags + [self.jit_path, mch_file]
                print("Invoking: " + " ".join(command))
                proc = subprocess.Popen(command)
                proc.communicate()
                return_code = proc.returncode
                if return_code == 0:
                    print("Clean SuperPMI replay")
                    result = True

                if is_nonzero_length_file(fail_mcl_file):
                    # Unclean replay. Examine the contents of the fail.mcl file to dig into failures.
                    if return_code == 0:
                        print("Warning: SuperPMI returned a zero exit code, but generated a non-zero-sized mcl file")
                    print_superpmi_failure_code(return_code, self.coreclr_args)
                    print_fail_mcl_file_method_numbers(fail_mcl_file)
                    repro_base_command_line = "{} {} {}".format(self.superpmi_path, " ".join(altjit_flags), self.jit_path)
                    save_repro_mc_files(temp_location, self.coreclr_args, repro_base_command_line)

                if not self.coreclr_args.skip_cleanup:
                    if os.path.isfile(fail_mcl_file):
                        os.remove(fail_mcl_file)
                    fail_mcl_file = None
            ################################################################################################ end of for mch_file in self.mch_files

        return result

################################################################################
# SuperPMI Replay/AsmDiffs
################################################################################

class SuperPMIReplayAsmDiffs:
    """ SuperPMI Replay AsmDiffs class

    Notes:
        The object is responsible for replaying the mch file given to the
        instance of the class and doing diffs using the two passed jits.
    """

    def __init__(self, coreclr_args, mch_files, base_jit_path, diff_jit_path):
        """ Constructor

        Args:
            coreclr_args (CoreclrArguments) : parsed args
            mch_files (list)                : list of MCH files to replay
            base_jit_path (str)             : path to baseline clrjit
            diff_jit_path (str)             : path to diff clrjit

        """

        self.base_jit_path = base_jit_path
        self.diff_jit_path = diff_jit_path
        self.mch_files = mch_files
        self.superpmi_path = determine_superpmi_tool_path(coreclr_args)
        self.coreclr_args = coreclr_args

    ############################################################################
    # Instance Methods
    ############################################################################

    def replay_with_asm_diffs(self):
        """ Replay the given SuperPMI collection, generating asm diffs

        Returns:
            (bool) True on success; False otherwise
        """

        result = False

        # Possible return codes from SuperPMI
        #
        # 0  : success
        # -1 : general fatal error (e.g., failed to initialize, failed to read files)
        # -2 : JIT failed to initialize
        # 1  : there were compilation failures
        # 2  : there were assembly diffs

        with TempDir(self.coreclr_args.temp_dir) as temp_location:
            print("")
            print("Temp Location: {}".format(temp_location))
            print("")

            # For each MCH file that we are going to replay, do the replay and replay post-processing.
            #
            # Consider: currently, we loop over all the steps for each MCH file, including (1) invoke
            # SuperPMI, (2) process results. It might be better to do (1) for each MCH file, then
            # process all the results at once. Currently, the results for some processing can be
            # obscured by the normal run output for subsequent MCH files.

            for mch_file in self.mch_files:

                fail_mcl_file = os.path.join(temp_location, os.path.basename(mch_file) + "_fail.mcl")
                diff_mcl_file = os.path.join(temp_location, os.path.basename(mch_file) + "_diff.mcl")

                # If the user passed -temp_dir or --diff_with_code_only, we skip the SuperPMI replay process,
                # and rely on what we find from a previous run.
                if self.coreclr_args.temp_dir is not None or self.coreclr_args.diff_with_code_only:
                    return_code = 1
                else:
                    flags = [
                        "-a", # Asm diffs
                        "-v", "ew", # only display errors and warnings
                        "-f", fail_mcl_file, # Failing mc List
                        "-diffMCList", diff_mcl_file, # Create all of the diffs in an mcl file
                        "-r", os.path.join(temp_location, "repro") # Repro name, create .mc repro files
                    ]

                    altjit_string = "*" if self.coreclr_args.altjit else ""
                    altjit_asm_diffs_flags = [
                        "-jitoption", "force", "AltJit=" + altjit_string,
                        "-jitoption", "force", "AltJitNgen=" + altjit_string,
                        "-jitoption", "force", "EnableExtraSuperPmiQueries=0",
                        "-jit2option", "force", "AltJit=" + altjit_string,
                        "-jit2option", "force", "AltJitNgen=" + altjit_string,
                        "-jit2option", "force", "EnableExtraSuperPmiQueries=0"
                    ]
                    flags += altjit_asm_diffs_flags

                    if not self.coreclr_args.sequential:
                        flags += [ "-p" ]

                    if self.coreclr_args.break_on_assert:
                        flags += [ "-boa" ]

                    if self.coreclr_args.break_on_error:
                        flags += [ "-boe" ]

                    if self.coreclr_args.log_file is not None:
                        flags += [ "-w", self.coreclr_args.log_file ]

                    # Change the working directory to the Core_Root we will call SuperPMI from.
                    # This is done to allow libcoredistools to be loaded correctly on unix
                    # as the loadlibrary path will be relative to the current directory.
                    with ChangeDir(self.coreclr_args.core_root) as dir:
                        command = [self.superpmi_path] + flags + [self.base_jit_path, self.diff_jit_path, mch_file]
                        print("Invoking: " + " ".join(command))
                        proc = subprocess.Popen(command)
                        proc.communicate()
                        return_code = proc.returncode
                        if return_code == 0:
                            print("Clean SuperPMI replay")
                            result = True

                if is_nonzero_length_file(fail_mcl_file):
                    # Unclean replay. Examine the contents of the fail.mcl file to dig into failures.
                    if return_code == 0:
                        print("Warning: SuperPMI returned a zero exit code, but generated a non-zero-sized mcl file")
                    print_superpmi_failure_code(return_code, self.coreclr_args)
                    print_fail_mcl_file_method_numbers(fail_mcl_file)
                    repro_base_command_line = "{} {} {}".format(self.superpmi_path, " ".join(altjit_asm_diffs_flags), self.diff_jit_path)
                    save_repro_mc_files(temp_location, self.coreclr_args, repro_base_command_line)

                # There were diffs. Go through each method that created diffs and
                # create a base/diff asm file with diffable asm. In addition, create
                # a standalone .mc for easy iteration.
                if is_nonzero_length_file(diff_mcl_file) or self.coreclr_args.diff_with_code_only:
                    # AsmDiffs. Save the contents of the fail.mcl file to dig into failures.

                    if return_code == 0:
                        print("Warning: SuperPMI returned a zero exit code, but generated a non-zero-sized mcl file")
                    print_superpmi_failure_code(return_code, self.coreclr_args)

                    if not self.coreclr_args.diff_with_code_only:
                        self.diff_mcl_contents = None
                        with open(diff_mcl_file) as file_handle:
                            mcl_lines = file_handle.readlines()
                            mcl_lines = [item.strip() for item in mcl_lines]
                            self.diff_mcl_contents = mcl_lines

                    asm_root_dir = create_unique_directory_name(self.coreclr_args.spmi_location, "asm.{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))
                    base_asm_location = os.path.join(asm_root_dir, "base")
                    diff_asm_location = os.path.join(asm_root_dir, "diff")

                    if not self.coreclr_args.diff_with_code_only:
                        # Create a diff and baseline directory
                        assert(not os.path.isdir(base_asm_location))
                        assert(not os.path.isdir(diff_asm_location))

                        os.makedirs(base_asm_location)
                        os.makedirs(diff_asm_location)

                        if self.coreclr_args.diff_jit_dump:
                            # If JIT dumps are requested, create a diff and baseline directory for JIT dumps
                            jitdump_root_dir = create_unique_directory_name(self.coreclr_args.spmi_location, "jitdump.{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))
                            base_dump_location = os.path.join(jitdump_root_dir, "base")
                            diff_dump_location = os.path.join(jitdump_root_dir, "diff")

                            assert(not os.path.isdir(base_dump_location))
                            assert(not os.path.isdir(diff_dump_location))

                            os.makedirs(base_dump_location)
                            os.makedirs(diff_dump_location)

                    text_differences = queue.Queue()
                    jit_dump_differences = queue.Queue()

                    asm_complus_vars = {
                            "COMPlus_JitDisasm": "*",
                            "COMPlus_JitUnwindDump": "*",
                            "COMPlus_JitEHDump": "*",
                            "COMPlus_JitDiffableDasm": "1",
                            "COMPlus_NgenDisasm": "*",
                            "COMPlus_NgenDump": "*",
                            "COMPlus_NgenUnwindDump": "*",
                            "COMPlus_NgenEHDump": "*",
                            "COMPlus_JitEnableNoWayAssert": "1",
                            "COMPlus_JitNoForceFallback": "1",
                            "COMPlus_JitRequired": "1",
                            "COMPlus_TieredCompilation": "0" }

                    jit_dump_complus_vars = {
                            "COMPlus_JitDisasm": "*",
                            "COMPlus_JitUnwindDump": "*",
                            "COMPlus_JitEHDump": "*",
                            "COMPlus_JitDiffableDasm": "1",
                            "COMPlus_NgenDisasm": "*",
                            "COMPlus_NgenDump": "*",
                            "COMPlus_NgenUnwindDump": "*",
                            "COMPlus_NgenEHDump": "*",
                            "COMPlus_JitEnableNoWayAssert": "1",
                            "COMPlus_JitNoForceFallback": "1",
                            "COMPlus_JitRequired": "1",
                            "COMPlus_TieredCompilation": "0",
                            "COMPlus_JitDump": "*",
                            "COMPlus_NgenDump": "*" }

                    altjit_string = "*" if self.coreclr_args.altjit else ""
                    altjit_flags = [
                        "-jitoption", "force", "AltJit=" + altjit_string,
                        "-jitoption", "force", "AltJitNgen=" + altjit_string,
                        "-jitoption", "force", "EnableExtraSuperPmiQueries=0"
                    ]

                    async def create_asm(print_prefix, item, self, mch_file, text_differences, base_asm_location, diff_asm_location):
                        """ Run superpmi over an mc to create dasm for the method.
                        """
                        # Setup flags to call SuperPMI for both the diff jit and the base jit

                        flags = [
                            "-c", item,
                            "-v", "q" # only log from the jit.
                        ]

                        flags += altjit_flags

                        # Add in all the COMPlus variables we need to generate asm.
                        os.environ.update(asm_complus_vars)

                        # Change the working directory to the core root we will call SuperPMI from.
                        # This is done to allow libcorcedistools to be loaded correctly on unix
                        # as the loadlibrary path will be relative to the current directory.
                        with ChangeDir(self.coreclr_args.core_root) as dir:
                            # Generate diff and base asm
                            base_txt = None
                            diff_txt = None

                            command = [self.superpmi_path] + flags + [self.base_jit_path, mch_file]

                            base_asm_path = os.path.join(base_asm_location, "{}.dasm".format(item))
                            with open(base_asm_path, 'w') as file_handle:
                                # print("{}Invoking: {}".format(print_prefix, " ".join(command))) # only for verbose?
                                print("{}Generating {}".format(print_prefix, base_asm_path))
                                proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                                await proc.communicate()

                            with open(base_asm_path, 'r') as file_handle:
                                base_txt = file_handle.read()

                            command = [self.superpmi_path] + flags + [self.diff_jit_path, mch_file]

                            diff_asm_path = os.path.join(diff_asm_location, "{}.dasm".format(item))
                            with open(diff_asm_path, 'w') as file_handle:
                                # print("{}Invoking: {}".format(print_prefix, " ".join(command))) # only for verbose?
                                print("{}Generating {}".format(print_prefix, diff_asm_path))
                                proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                                await proc.communicate()

                            with open(diff_asm_path, 'r') as file_handle:
                                diff_txt = file_handle.read()

                            # Sanity checks
                            assert base_txt is not None
                            assert base_txt != ""

                            assert diff_txt is not None
                            assert diff_txt != ""

                            if base_txt != diff_txt:
                                text_differences.put_nowait(item)
                    ################################################################################################ end of create_asm()

                    async def create_jit_dump(print_prefix, item, self, mch_file, jit_dump_differences, base_dump_location, diff_dump_location):
                        """ Run superpmi over an mc to create JIT dumps for the method.
                        """
                        # Setup flags to call SuperPMI for both the diff jit and the base jit

                        flags = [
                            "-c", item,
                            "-v", "q" # only log from the jit.
                        ]

                        flags += altjit_flags

                        # Add in all the COMPlus variables we need to generate JitDump.
                        os.environ.update(jit_dump_complus_vars)

                        # Generate jit dumps
                        base_txt = None
                        diff_txt = None

                        # Change the working directory to the core root we will call SuperPMI from.
                        # This is done to allow libcoredistools to be loaded correctly on unix
                        # as the loadlibrary path will be relative to the current directory.
                        with ChangeDir(self.coreclr_args.core_root) as dir:

                            command = [self.superpmi_path] + flags + [self.base_jit_path, mch_file]

                            base_dump_path = os.path.join(base_dump_location, "{}.txt".format(item))
                            with open(base_dump_path, 'w') as file_handle:
                                # print("{}Invoking: ".format(print_prefix) + " ".join(command)) # only for verbose?
                                print("{}Generating {}".format(print_prefix, base_dump_path))
                                proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                                await proc.communicate()

                            with open(base_dump_path, 'r') as file_handle:
                                base_txt = file_handle.read()

                            command = [self.superpmi_path] + flags + [self.diff_jit_path, mch_file]

                            diff_dump_path = os.path.join(diff_dump_location, "{}.txt".format(item))
                            with open(diff_dump_path, 'w') as file_handle:
                                # print("{}Invoking: ".format(print_prefix) + " ".join(command)) # only for verbose?
                                print("{}Generating {}".format(print_prefix, diff_dump_path))
                                proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                                await proc.communicate()

                            with open(diff_dump_path, 'r') as file_handle:
                                diff_txt = file_handle.read()

                            # Sanity checks
                            assert base_txt is not None
                            assert base_txt != ""

                            assert diff_txt is not None
                            assert diff_txt != ""

                            if base_txt != diff_txt:
                                jit_dump_differences.put_nowait(item)
                    ################################################################################################ end of create_jit_dump()

                    if not self.coreclr_args.diff_with_code_only:
                        diff_items = []
                        for item in self.diff_mcl_contents:
                            diff_items.append(item)

                        print("Creating asm files")
                        subproc_helper = AsyncSubprocessHelper(diff_items, verbose=True)
                        subproc_helper.run_to_completion(create_asm, self, mch_file, text_differences, base_asm_location, diff_asm_location)

                        if self.coreclr_args.diff_jit_dump:
                            print("Creating JitDump files")
                            subproc_helper.run_to_completion(create_jit_dump, self, mch_file, jit_dump_differences, base_dump_location, diff_dump_location)

                    else:
                        # This is the `--diff_with_code_only` path.
                        # We have already generated asm under <coreclr_bin_path>/asm/base and <coreclr_bin_path>/asm/diff
                        for item in os.listdir(base_asm_location):
                            base_asm_file = os.path.join(base_asm_location, item)
                            diff_asm_file = os.path.join(diff_asm_location, item)

                            base_txt = None
                            diff_txt = None

                            # Every file should have a diff asm file.
                            assert os.path.isfile(diff_asm_file)

                            with open(base_asm_file) as file_handle:
                                base_txt = file_handle.read()

                            with open(diff_asm_file) as file_handle:
                                diff_txt = file_handle.read()

                            if base_txt != diff_txt:
                                text_differences.append(item[:-4])

                        if self.coreclr_args.diff_jit_dump:
                            for item in os.listdir(base_dump_location):
                                base_dump_file = os.path.join(base_dump_location, item)
                                diff_dump_file = os.path.join(diff_dump_location, item)

                                base_txt = None
                                diff_txt = None

                                # Every file should have a diff asm file.
                                assert os.path.isfile(diff_dump_file)

                                with open(base_dump_file) as file_handle:
                                    base_txt = file_handle.read()

                                with open(diff_dump_file) as file_handle:
                                    diff_txt = file_handle.read()

                                if base_txt != diff_txt:
                                    jit_dump_differences.append(item[:-4])

                    if not self.coreclr_args.diff_with_code_only:
                        print("Differences found. To replay SuperPMI use:")
                        print("")
                        for var, value in asm_complus_vars.items():
                            print_platform_specific_environment_vars(self.coreclr_args, var, value)
                        print("{} {} -c ### {} {}".format(self.superpmi_path, " ".join(altjit_flags), self.diff_jit_path, mch_file))
                        print("")
                        if self.coreclr_args.diff_jit_dump:
                            print("To generate JitDump with SuperPMI use:")
                            print("")
                            for var, value in jit_dump_complus_vars.items():
                                print_platform_specific_environment_vars(self.coreclr_args, var, value)
                            print("{} {} -c ### {} {}".format(self.superpmi_path, " ".join(altjit_flags), self.diff_jit_path, mch_file))
                            print("")
                        print("Method numbers with binary differences:")
                        print(self.diff_mcl_contents)
                        print("")

                    try:
                        current_text_diff = text_differences.get_nowait()
                    except:
                        current_text_diff = None

                    if current_text_diff is not None:
                        print("Textual differences found. Asm is located under {} {}".format(base_asm_location, diff_asm_location))
                        print("Generate a diff analysis report by building jit-analyze from https://github.com/dotnet/jitutils and running:")
                        print("    jit-analyze -r --base {} --diff {}".format(base_asm_location, diff_asm_location))

                        # Find jit-analyze.bat/sh on PATH, if it exists, then invoke it.
                        search_path = os.environ.get("PATH")
                        if search_path is not None:
                            search_path = search_path.split(";")
                            jit_analyze_file = "jit-analyze.bat" if platform.system() == "Windows" else "jit-analyze.sh"
                            jit_analyze_path = find_file(jit_analyze_file, search_path)
                            if jit_analyze_path is not None:
                                # It appears we have a built jit-analyze on the path, so try to run it.
                                command = [ jit_analyze_path, "-r", "--base", base_asm_location, "--diff", diff_asm_location ]
                                print("Invoking: " + " ".join(command))
                                proc = subprocess.Popen(command)
                                proc.communicate()

                        if self.coreclr_args.diff_with_code and not self.coreclr_args.diff_jit_dump_only:
                            # Open VS Code on the diffs.
                            #
                            # TODO: it looks like there's a bug here where we're missing a:
                            #   for item in os.listdir(base_asm_location)
                            # ?
                            batch_command = ["cmd", "/c"] if platform.system() == "Windows" else []
                            index = 0
                            while current_text_diff is not None:
                                command = batch_command + [
                                    "code",
                                    "-d",
                                    os.path.join(base_asm_location, "{}.asm".format(item)),
                                    os.path.join(diff_asm_location, "{}.asm".format(item))
                                ]
                                print("Invoking: " + " ".join(command))
                                proc = subprocess.Popen(command)

                                if index > 5:
                                    break

                                try:
                                    current_text_diff = text_differences.get_nowait()
                                except:
                                    current_text_diff = None
                                index += 1

                        print("")
                    else:
                        print("No textual differences. Is this an issue with coredistools?")

                    try:
                        current_jit_dump_diff = jit_dump_differences.get_nowait()
                    except:
                        current_jit_dump_diff = None

                    if current_jit_dump_diff is not None:
                        print("Textual differences found in JitDump. JitDump is located under {} {}".format(base_dump_location, diff_dump_location))

                        if self.coreclr_args.diff_with_code:
                            # Open VS Code on the diffs.
                            #
                            # TODO: it looks like there's a bug here where we're missing a:
                            #   for item in os.listdir(base_asm_location)
                            # ?
                            batch_command = ["cmd", "/c"] if platform.system() == "Windows" else []
                            index = 0
                            while current_jit_dump_diff is not None:
                                command = batch_command + [
                                    "code",
                                    "-d",
                                    os.path.join(base_dump_location, "{}.txt".format(item)),
                                    os.path.join(diff_dump_location, "{}.txt".format(item))
                                ]
                                print("Invoking: " + " ".join(command))
                                proc = subprocess.Popen(command)

                                if index > 5:
                                    break

                                try:
                                    current_jit_dump_diff = jit_dump_differences.get_nowait()
                                except:
                                    current_jit_dump_diff = None

                                index += 1

                        print("")
                ################################################################################################ end of processing asm diffs (if is_nonzero_length_file(diff_mcl_file)...

                if not self.coreclr_args.skip_cleanup:
                    if os.path.isfile(fail_mcl_file):
                        os.remove(fail_mcl_file)
                        fail_mcl_file = None

            ################################################################################################ end of for mch_file in self.mch_files

        return result
        ################################################################################################ end of replay_with_asm_diffs()

################################################################################
# Argument handling helpers
################################################################################

def determine_coredis_tools(coreclr_args):
    """ Determine the coredistools location. First, look in Core_Root. It will be there if
        the setup-stress-dependencies.cmd/sh script has been run, which is typically only
        if tests have been run. If unable to find coredistools, download it from a cached
        copy in the CLRJIT Azure Storage. (Ideally, we would instead download the NuGet
        package and extract it using the same mechanism as setup-stress-dependencies
        instead of having our own copy in Azure Storage).

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        coredistools_location (str)     : path of [lib]coredistools.dylib|so|dll
    """

    coredistools_dll_name = None
    if coreclr_args.host_os.lower() == "osx":
        coredistools_dll_name = "libcoredistools.dylib"
    elif coreclr_args.host_os.lower() == "linux":
        coredistools_dll_name = "libcoredistools.so"
    elif coreclr_args.host_os.lower() == "windows_nt":
        coredistools_dll_name = "coredistools.dll"
    else:
        raise RuntimeError("Unknown host os: {}").format(coreclr_args.host_os)

    coredistools_location = os.path.join(coreclr_args.core_root, coredistools_dll_name)
    if os.path.isfile(coredistools_location):
        print("Using coredistools found at {}".format(coredistools_location))
    else:
        coredistools_uri = az_blob_storage_superpmi_container_uri + "/libcoredistools/{}-{}/{}".format(coreclr_args.host_os.lower(), coreclr_args.arch.lower(), coredistools_dll_name)
        print("Download: {} -> {}".format(coredistools_uri, coredistools_location))
        urllib.request.urlretrieve(coredistools_uri, coredistools_location)

    assert os.path.isfile(coredistools_location)
    return coredistools_location

def determine_pmi_location(coreclr_args):
    """ Determine pmi.dll location, using the following steps:
        First, use the `-pmi_location` argument, if set.
        Else, look for pmi.dll on the PATH. This will be true if you build jitutils yourself
            and put the built `bin` directory on your PATH.
        Else, look for pmi.dll in Core_Root. This is where we cache it if downloaded from Azure Storage
        Otherwise, download a cached copy from CLRJIT Azure Storage and cache it in Core_Root.

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        pmi_location (str)     : path of pmi.dll
    """
    if coreclr_args.pmi_location is not None:
        pmi_location = os.path.abspath(coreclr_args.pmi_location)
        if not os.path.isfile(pmi_location):
            raise RuntimeError("PMI not found at {}".format(pmi_location))
        print("Using PMI at {}".format(pmi_location))
    else:
        search_path = os.environ.get("PATH")
        pmi_location = find_file("pmi.dll", search_path.split(";")) if search_path is not None else None
        if pmi_location is not None:
            print("Using PMI found on PATH at {}".format(pmi_location))
        else:
            pmi_location = os.path.join(coreclr_args.core_root, "pmi.dll")
            if os.path.isfile(pmi_location):
                print("Using PMI found at {}".format(pmi_location))
            else:
                pmi_uri = az_blob_storage_superpmi_container_uri + "/pmi/pmi.dll"
                print("Download: {} -> {}".format(pmi_uri, pmi_location))
                urllib.request.urlretrieve(pmi_uri, pmi_location)

    assert os.path.isfile(pmi_location)
    return pmi_location

def determine_jit_name(coreclr_args):
    """ Determine the jit based on the OS. If "-altjit" is specified, then use the specified altjit.
        This function is called for cases where the "-altjit" flag is not used, so be careful not
        to depend on the "altjit" attribute existing.

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        jit_name(str) : name of the jit for this OS
    """

    # If `-altjit` is used, it must be given a full filename, not just a "base name", so use it without additional processing.
    if hasattr(coreclr_args, "altjit") and coreclr_args.altjit is not None:
        return coreclr_args.altjit

    jit_base_name = "clrjit"
    if coreclr_args.host_os == "OSX":
        return "lib" + jit_base_name + ".dylib"
    elif coreclr_args.host_os == "Linux":
        return "lib" + jit_base_name + ".so"
    elif coreclr_args.host_os == "Windows_NT":
        return jit_base_name + ".dll"
    else:
        raise RuntimeError("Unknown OS.")

def determine_superpmi_tool_name(coreclr_args):
    """ Determine the superpmi tool name based on the OS

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) Name of the superpmi tool to use
    """

    if coreclr_args.host_os == "OSX":
        return "superpmi"
    elif coreclr_args.host_os == "Linux":
        return "superpmi"
    elif coreclr_args.host_os == "Windows_NT":
        return "superpmi.exe"
    else:
        raise RuntimeError("Unknown OS.")

def determine_superpmi_tool_path(coreclr_args):
    """ Determine the superpmi tool full path

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) Path of the superpmi tool to use
    """

    superpmi_tool_name = determine_superpmi_tool_name(coreclr_args)
    superpmi_tool_path = os.path.join(coreclr_args.core_root, superpmi_tool_name)
    if not os.path.isfile(superpmi_tool_path):
        # We couldn't find superpmi in core_root. This is probably fatal.
        # However, just in case, check for it on the PATH.
        search_path = os.environ.get("PATH")
        superpmi_tool_path = find_file(superpmi_tool_name, search_path.split(";")) if search_path is not None else None
        if superpmi_tool_path is None:
            raise RuntimeError("Superpmi tool not found. Have you built the runtime repo and created a Core_Root?")

    return superpmi_tool_path

def print_platform_specific_environment_vars(coreclr_args, var, value):
    """ Print environment variables as set {}={} or export {}={}

    Args:
        coreclr_args (CoreclrArguments): parsed args
        var   (str): variable to set
        value (str): value being set.
    """

    if coreclr_args.host_os == "Windows_NT":
        print("set {}={}".format(var, value))
    else:
        print("export {}={}".format(var, value))

def list_superpmi_collections_container_via_rest_api(coreclr_args, filter=lambda unused: True):
    """ List the superpmi collections using the Azure Storage REST api

    Args:
        filter (lambda: string -> bool): filter to apply to the list. The filter takes a URL and returns True if this URL is acceptable.

    Returns:
        urls (list): set of collection URLs in Azure Storage that match the filter.

    Notes:
        This method does not require installing the Azure Storage python package.
    """

    # This URI will return *all* the blobs, for all jit-ee-version/OS/architecture combinations.
    # pass "prefix=foo/bar/..." to only show a subset. Or, we can filter later using string search.
    list_superpmi_container_uri = az_blob_storage_superpmi_container_uri + "?restype=container&comp=list&prefix=" + az_collections_root_folder + "/"

    try:
        contents = urllib.request.urlopen(list_superpmi_container_uri).read().decode('utf-8')
    except Exception as exception:
        print("Didn't find any collections using {}".format(list_superpmi_container_uri))
        print("  Error: {}".format(exception))
        return None

    # Contents is an XML file with contents like:
    #
    # <EnumerationResults ContainerName="https://clrjit.blob.core.windows.net/superpmi">
    #   <Blobs>
    #     <Blob>
    #       <Name>jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip</Name>
    #       <Url>https://clrjit.blob.core.windows.net/superpmi/jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip</Url>
    #       <Properties>
    #         ...
    #       </Properties>
    #     </Blob>
    #     <Blob>
    #       <Name>jit-ee-guid/Linux/x64/Linux.x64.Checked.mch.zip</Name>
    #       <Url>https://clrjit.blob.core.windows.net/superpmi/jit-ee-guid/Linux/x64/Linux.x64.Checked.mch.zip</Url>
    #     ... etc. ...
    #   </Blobs>
    # </EnumerationResults>
    #
    # We just want to extract the <Url> entries. We could probably use an XML parsing package, but we just
    # use regular expressions.

    urls_split = contents.split("<Url>")[1:]
    urls = []
    for item in urls_split:
        url = item.split("</Url>")[0].strip()
        if filter(url):
            urls.append(url)

    return urls

def process_mch_files_arg(coreclr_args):
    """ Process the -mch_files argument. If the argument is empty, then download files from Azure Storage.
        If the argument is non-empty, check it for UNC paths and download/cache those files, replacing
        them with a reference to the newly cached local paths (this is on Windows only).

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Returns:
        nothing

        coreclr_args.mch_files is updated

    """

    if coreclr_args.mch_files is None:
        coreclr_args.mch_files = download_mch(coreclr_args, include_baseline_jit=True)
        return

    # Create the cache location. Note that we'll create it even if we end up not copying anything.
    default_mch_root_dir = os.path.join(coreclr_args.spmi_location, "mch")
    default_mch_dir = os.path.join(default_mch_root_dir, "{}.{}.{}".format(coreclr_args.jit_ee_version, coreclr_args.host_os, coreclr_args.arch))
    if not os.path.isdir(default_mch_dir):
        os.makedirs(default_mch_dir)

    # Process the mch_files list. Download and cache UNC and HTTP files.
    urls = []
    local_mch_files = []
    for item in coreclr_args.mch_files:
        # On Windows only, see if any of the mch_files are UNC paths (i.e., "\\server\share\...").
        # If so, download and cache all the files found there to our usual local cache location, to avoid future network access.
        if coreclr_args.host_os == "Windows_NT" and item.startswith("\\\\"):
            # Special case: if the user specifies a .mch file, we'll also look for and cache a .mch.mct file next to it, if one exists.
            # This happens naturally if a directory is passed and we search for all .mch and .mct files in that directory.
            mch_file = os.path.abspath(item)
            if os.path.isfile(mch_file) and mch_file.endswith(".mch"):
                files = [ mch_file ]
                mct_file = mch_file + ".mct"
                if os.path.isfile(mct_file):
                    files.append(mct_file)
            else:
                files = get_files_from_path(mch_file, matchFunc=lambda path: any(path.endswith(extension) for extension in [".mch", ".mct"]))

            for file in files:
                # Download file to cache, and report that as the file to use.
                cache_file = os.path.join(default_mch_dir, os.path.basename(file))
                print("Cache {} => {}".format(file, cache_file))
                local_mch_file = shutil.copy2(file, cache_file)
                local_mch_files.append(local_mch_file)
        elif item.lower().startswith("http:") or item.lower().startswith("https:"): # probably could use urllib.parse to be more precise
            urls.append(item)
        else:
            # Doesn't appear to be a URL (on Windows) or a URL, so just use it as-is.
            local_mch_files.append(item)

    # Download all the urls at once, and add the local cache filenames to our accumulated list of local file names.
    if len(urls) != 0:
        local_mch_files += download_urls(urls, default_mch_dir)

    coreclr_args.mch_files = local_mch_files


def download_mch(coreclr_args, include_baseline_jit=False):
    """ Download the mch files. This can be called to re-download files and
        overwrite them in the target location.

    Args:
        coreclr_args (CoreclrArguments): parsed args
        include_baseline_jit (bool): If True, also download the baseline jit

    Returns:
        list containing the directory to which the files were downloaded

    """

    default_mch_root_dir = os.path.join(coreclr_args.spmi_location, "mch")
    default_mch_dir = os.path.join(default_mch_root_dir, "{}.{}.{}".format(coreclr_args.jit_ee_version, coreclr_args.host_os, coreclr_args.arch))

    if os.path.isdir(default_mch_dir) and not coreclr_args.force_download:
        # The cache directory is already there, and "--force_download" was passed, so just
        # assume it's got what we want.
        # NOTE: a different solution might be to verify that everything we would download is
        #       already in the cache, and simply not download if it is. However, that would
        #       require hitting the network, and currently once you've cached these, you
        #       don't need to do that.
        print("Found download cache directory \"{}\" and --force_download not set; skipping download".format(default_mch_dir))
        return [ default_mch_dir ]

    blob_filter_string = "{}/{}/{}".format(coreclr_args.jit_ee_version, coreclr_args.host_os, coreclr_args.arch)
    blob_prefix_filter = "{}/{}/{}".format(az_blob_storage_superpmi_container_uri, az_collections_root_folder, blob_filter_string).lower()

    # Determine if a URL in Azure Storage should be allowed. The URL looks like:
    #   https://clrjit.blob.core.windows.net/superpmi/jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip
    # Filter to just the current jit-ee-guid, OS, and architecture.
    # Include both MCH and MCT files as well as the CLR JIT dll (processed below).
    # If there are filters, only download those matching files.
    def filter_superpmi_collections(url):
        url = url.lower()
        if "clrjit" in url and not include_baseline_jit:
            return False
        return url.startswith(blob_prefix_filter) and ((coreclr_args.filter is None) or any((filter_item.lower() in url) for filter_item in coreclr_args.filter))

    urls = list_superpmi_collections_container_via_rest_api(coreclr_args, filter_superpmi_collections)
    if urls is None:
        return []

    download_urls(urls, default_mch_dir)
    return [ default_mch_dir ]


def download_urls(urls, target_dir, verbose=True, fail_if_not_found=True):
    """ Download a set of files, specified as URLs, to a target directory.
        If the URLs are to .ZIP files, then uncompress them and copy all contents
        to the target directory.

    Args:
        urls (list): the URLs to download
        target_dir (str): target directory where files are copied. Directory must exist
        fail_if_not_found (bool): if True, fail if a download fails due to file not found (HTTP error 404).
                                  Otherwise, ignore the failure.

    Returns:
        list of local filenames of downloaded files
    """

    if verbose:
        print("Downloading:")
        for url in urls:
            print("  {}".format(url))

    local_files = []

    # In case we'll need a temp directory for ZIP file processing, create it first.
    with TempDir() as temp_location:
        for url in urls:
            item_name = url.split("/")[-1]

            if url.lower().endswith(".zip"):
                # Delete everything in the temp_location (from previous iterations of this loop, so previous URL downloads).
                temp_location_items = [os.path.join(temp_location, item) for item in os.listdir(temp_location)]
                for item in temp_location_items:
                    if os.path.isdir(item):
                        shutil.rmtree(item)
                    else:
                        os.remove(item)

                download_path = os.path.join(temp_location, item_name)

                try:
                    if verbose:
                        print("Download: {} -> {}".format(url, download_path))
                    urllib.request.urlretrieve(url, download_path)
                except urllib.error.HTTPError as httperror:
                    if (httperror == 404) and fail_if_not_found:
                        raise httperror
                    # Otherwise, swallow the error and continue to next file.
                    continue

                if verbose:
                    print("Uncompress {}".format(download_path))
                with zipfile.ZipFile(download_path, "r") as file_handle:
                    file_handle.extractall(temp_location)

                # Copy everything that was extracted to the target directory.
                if not os.path.isdir(target_dir):
                    os.makedirs(target_dir)
                items = [ os.path.join(temp_location, item) for item in os.listdir(temp_location) if not item.endswith(".zip") ]
                for item in items:
                    target_path = os.path.join(target_dir, os.path.basename(item))
                    if verbose:
                        print("Copy {} -> {}".format(item, target_path))
                    shutil.copy2(item, target_dir)
                    local_files.append(target_path)
            else:
                # Not a zip file; download directory to target directory
                if not os.path.isdir(target_dir):
                    os.makedirs(target_dir)
                download_path = os.path.join(target_dir, item_name)

                try:
                    if verbose:
                        print("Download: {} -> {}".format(url, download_path))
                    urllib.request.urlretrieve(url, download_path)
                    local_files.append(download_path)
                except urllib.error.HTTPError as httperror:
                    if (httperror == 404) and fail_if_not_found:
                        raise httperror
                    # Otherwise, swallow the error and continue to next file.
                    continue

    return local_files


def upload_mch(coreclr_args):
    """ Upload a set of MCH files. Each MCH file is first ZIP compressed to save data space and upload/download time.

        TODO: Upload baseline altjits or cross-compile JITs?

    Args:
        coreclr_args (CoreclrArguments): parsed args
    """

    def upload_blob(file, blob_name):
        blob_client = blob_service_client.get_blob_client(container=az_superpmi_container_name, blob=blob_name)

        # Check if the blob already exists, and delete it if it does, before uploading / replacing it.
        try:
            blob_properties = blob_client.get_blob_properties()
            # If no exception, then the blob already exists. Delete it!
            print("Warning: replacing existing blob!")
            blob_client.delete_blob()
        except Exception as StorageErrorException:
            # Blob doesn't exist already; that's good
            pass

        with open(file, "rb") as data:
            blob_client.upload_blob(data)

    files = []
    for item in coreclr_args.mch_files:
        files += get_files_from_path(item, matchFunc=lambda path: any(path.endswith(extension) for extension in [".mch", ".mct"]))

    print("Uploading:")
    for item in files:
        print("  {}".format(item))

    try:
        from azure.storage.blob import BlobServiceClient, BlobClient

    except:
        print("Please install:")
        print("  pip install azure-storage-blob")
        print("See also https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-python")
        raise RuntimeError("Missing azure storage package.")

    blob_service_client = BlobServiceClient(account_url=az_blob_storage_account_uri, credential=coreclr_args.az_storage_key)
    blob_folder_name = "{}/{}/{}/{}".format(az_collections_root_folder, coreclr_args.jit_ee_version, coreclr_args.host_os, coreclr_args.arch)

    total_bytes_uploaded = 0

    with TempDir() as temp_location:
        for file in files:
            # Zip compress the file we will upload
            zip_name = os.path.basename(file) + ".zip"
            zip_path = os.path.join(temp_location, zip_name)
            print("Compress {} -> {}".format(file, zip_path))
            with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zip_file:
                zip_file.write(file, os.path.basename(file))

            original_stat_result = os.stat(file)
            zip_stat_result = os.stat(zip_path)
            print("Compressed {:n} to {:n} bytes".format(original_stat_result.st_size, zip_stat_result.st_size))
            total_bytes_uploaded += zip_stat_result.st_size

            blob_name = "{}/{}".format(blob_folder_name, zip_name)
            print("Uploading: {} ({}) -> {}".format(file, zip_path, az_blob_storage_superpmi_container_uri + "/" + blob_name))
            upload_blob(zip_path, blob_name)

        # Upload a JIT matching the MCH files just collected.
        # TODO: rename uploaded JIT to include build_type?

        jit_location = coreclr_args.jit_location
        if jit_location is None:
            jit_name = determine_jit_name(coreclr_args)
            jit_location = os.path.join(coreclr_args.core_root, jit_name)

        assert os.path.isfile(jit_location)

        jit_name = os.path.basename(jit_location)
        jit_blob_name = "{}/{}".format(blob_folder_name, jit_name)
        print("Uploading: {} -> {}".format(jit_location, az_blob_storage_superpmi_container_uri + "/" + jit_blob_name))
        upload_blob(jit_location, jit_blob_name)

        jit_stat_result = os.stat(jit_location)
        total_bytes_uploaded += jit_stat_result.st_size

    print("Uploaded {:n} bytes".format(total_bytes_uploaded))


def list_collections_command(coreclr_args):
    """ List the SuperPMI collections in Azure Storage

    Args:
        coreclr_args (CoreclrArguments) : parsed args
    """

    blob_filter_string = "{}/{}/{}".format(coreclr_args.jit_ee_version, coreclr_args.host_os, coreclr_args.arch)
    blob_prefix_filter = "{}/{}/{}".format(az_blob_storage_superpmi_container_uri, az_collections_root_folder, blob_filter_string).lower()

    # Determine if a URL in Azure Storage should be allowed. The URL looks like:
    #   https://clrjit.blob.core.windows.net/superpmi/jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip
    # By default, filter to just the current jit-ee-guid, OS, and architecture.
    # Only include MCH files, not clrjit.dll or MCT (TOC) files.
    def filter_superpmi_collections(url):
        url = url.lower()
        return (".mch." in url) and (".mct." not in url) and (coreclr_args.all or url.startswith(blob_prefix_filter))

    urls = list_superpmi_collections_container_via_rest_api(coreclr_args, filter_superpmi_collections)
    if urls is None:
        return

    count = len(urls)

    print("SuperPMI list-collections")
    print("")
    if coreclr_args.all:
        print("{} collections".format(count))
    else:
        print("{} collections for {}".format(count, blob_filter_string))
    print("")

    for url in urls:
        print("{}".format(url))

    print("")


def list_collections_local_command(coreclr_args):
    """ List the SuperPMI collections local cache: where the Azure Storage collections are copied

    Args:
        coreclr_args (CoreclrArguments) : parsed args
    """

    # Display the blob filter string the local cache corresponds to
    blob_filter_string = "{}/{}/{}".format(coreclr_args.jit_ee_version, coreclr_args.host_os, coreclr_args.arch)

    default_mch_root_dir = os.path.join(coreclr_args.spmi_location, "mch")
    default_mch_dir = os.path.join(default_mch_root_dir, "{}.{}.{}".format(coreclr_args.jit_ee_version, coreclr_args.host_os, coreclr_args.arch))

    # Determine if a URL in Azure Storage should be allowed. The URL looks like:
    #   https://clrjit.blob.core.windows.net/superpmi/jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip
    # By default, filter to just the current jit-ee-guid, OS, and architecture.
    # Only include MCH files, not clrjit.dll or MCT (TOC) files.
    def filter_superpmi_collections(path):
        path = path.lower()
        return (".mch." in path) and (".mct." not in path)

    if coreclr_args.all:
        if not os.path.isdir(default_mch_root_dir):
            print("Local dir \"{}\" not found".format(default_mch_root_dir))
            return
        local_items = get_files_from_path(default_mch_root_dir)
    else:
        if not os.path.isdir(default_mch_dir):
            print("Local dir \"{}\" not found".format(default_mch_dir))
            return
        local_items = get_files_from_path(default_mch_dir)

    filtered_local_items = [item for item in local_items if filter_superpmi_collections(item)]

    count = len(filtered_local_items)

    print("SuperPMI list-collections --local")
    print("")
    if coreclr_args.all:
        print("{} collections".format(count))
    else:
        print("{} collections for {}".format(count, blob_filter_string))
    print("")

    for item in filtered_local_items:
        print("{}".format(item))

    print("")


def get_mch_files_for_replay(coreclr_args):
    """ Given the argument `mch_files`, and any specified filters, find all the MCH files to
        use for replay.

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        None if error (with an error message already printed), else a list of MCH files.
    """

    if coreclr_args.mch_files is None:
        print("No MCH files specified")
        return None

    mch_files = []
    for item in coreclr_args.mch_files:
        # If there are specified filters, only run those matching files.
        mch_files += get_files_from_path(item,
                matchFunc=lambda path: any(path.endswith(extension) for extension in [".mch"]) and ((coreclr_args.filter is None) or any(filter_item.lower() in path for filter_item in coreclr_args.filter)))

    if len(mch_files) == 0:
        print("No MCH files found to replay")
        return None

    return mch_files


def process_base_jit_path_arg(coreclr_args):
    """ Process the -base_jit_path argument coreclr_args.base_jit_path.
        If the argument is present, check it for being a path to a file.
        If not present, try to find and download a baseline JIT based on the current environment:
        1. Determine the current git hash using:
             git rev-parse HEAD
           or use the `-git_hash` argument (call the result `git_hash`).
        2. Determine the baseline: where does this hash meet `master` using:
             git merge-base `git_hash` master
           or use the `-base_git_hash` argument (call the result `base_git_hash`).
        3. If the `-base_git_hash` argument is used, use that directly as the exact git
           hash of the baseline JIT to use.
        4. Otherwise, figure out the latest hash, starting with `base_git_hash`, that contains any changes to
           the src\coreclr\src\jit directory. (We do this because the JIT rolling build only includes
           builds for changes to this directory. So, this logic needs to stay in sync with the logic
           that determines what causes the JIT directory to be rebuilt. E.g., it should also get
           rebuilt if the JIT-EE interface GUID changes. Alternatively, we can take the entire list
           of changes, and probe the rolling build drop for all of them.)
        5. Check if we've already downloaded a JIT that matches `base_git_hash`, and use that if available.
        6. Starting with `base_git_hash`, and possibly walking to older changes, look for matching builds
           in the JIT rolling build drops.
        7. If a baseline clrjit is found, download it to the `spmi/basejit/git-hash.os.architecture.build_type`
           cache directory.
        8. Set coreclr_args.base_jit_path to the full path to the downloaded baseline JIT.

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        Nothing

        coreclr_args.base_jit_path is set to the path to the JIT to use for the baseline JIT.
    """

    if coreclr_args.base_jit_path is not None:
        if not os.path.isfile(coreclr_args.base_jit_path):
            raise RuntimeError("Specified -base_jit_path does not point to a file")
        return

    # Used for debugging
    verbose = False

    # We cache baseline jits under the following directory. Note that we can't create the full directory path
    # until we know the baseline JIT hash.
    default_basejit_root_dir = os.path.join(coreclr_args.spmi_location, "basejit")

    # Do all the remaining commands, including a number of 'git' commands including relative paths,
    # from the root of the runtime repo.

    with ChangeDir(coreclr_args.runtime_repo_location) as dir:
        if coreclr_args.git_hash is None:
            command = [ "git", "rev-parse", "HEAD" ]
            if verbose:
                print("Invoking: " + " ".join(command))
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_git_rev_parse, stderr_git_rev_parse = proc.communicate()
            return_code = proc.returncode
            if return_code == 0:
                current_hash = stdout_git_rev_parse.decode('utf-8').strip()
                if verbose:
                    print("Current hash: {}".format(current_hash))
            else:
                raise RuntimeError("Couldn't determine current git hash")
        else:
            current_hash = coreclr_args.git_hash

        if coreclr_args.base_git_hash is None:
            # We've got the current hash; figure out the baseline hash.
            command = [ "git", "merge-base", current_hash, "master" ]
            if verbose:
                print("Invoking: " + " ".join(command))
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_git_merge_base, stderr_git_merge_base = proc.communicate()
            return_code = proc.returncode
            if return_code == 0:
                baseline_hash = stdout_git_merge_base.decode('utf-8').strip()
                print("Baseline hash: {}".format(current_hash))
            else:
                raise RuntimeError("Couldn't determine baseline git hash")
        else:
            baseline_hash = coreclr_args.base_git_hash

        if coreclr_args.base_git_hash is None:
            # Enumerate the last 20 changes, starting with the baseline, that included JIT changes.
            command = [ "git", "log", "--pretty=format:%H", baseline_hash, "-20", "--", "src/coreclr/src/jit/*" ]
            if verbose:
                print("Invoking: " + " ".join(command))
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_change_list, stderr_change_list = proc.communicate()
            return_code = proc.returncode
            change_list_hashes = []
            if return_code == 0:
                change_list_hashes = stdout_change_list.decode('utf-8').strip().splitlines()
            else:
                raise RuntimeError("Couldn't determine list of JIT changes starting with baseline hash")

            if len(change_list_hashes) == 0:
                raise RuntimeError("No JIT changes found starting with baseline hash")
        else:
            # If `-base_git_hash` is specified, then we use exactly that hash and no other for the baseline.
            change_list_hashes = [ coreclr_args.base_git_hash ]
        
        # For each hash, (1) see if we have the JIT already, and if not (2) try to download the corresponding JIT from the rolling build.

        hashnum = 1
        for hash in change_list_hashes:
            if verbose:
                print("{}: {}".format(hashnum, hash))

            jit_name = determine_jit_name(coreclr_args)
            basejit_dir = os.path.join(default_basejit_root_dir, "{}.{}.{}.{}".format(hash, coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))
            basejit_path = os.path.join(basejit_dir, jit_name)
            if os.path.isfile(basejit_path):
                # We found this baseline JIT in our cache; use it!
                coreclr_args.base_jit_path = basejit_path
                print("Using baseline {}".format(coreclr_args.base_jit_path))
                return

            # It's not in our cache; is there one built by the rolling build to download?
            blob_folder_name = "{}/{}/{}/{}/{}/{}".format(az_builds_root_folder, hash, coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type, jit_name)
            blob_uri = "{}/{}".format(az_blob_storage_jitrollingbuild_container_uri, blob_folder_name)
            urls = [ blob_uri ]
            local_files = download_urls(urls, basejit_dir, verbose=verbose, fail_if_not_found=False)

            if len(local_files) > 0:
                if hashnum > 1:
                    print("Warning: the baseline found is not built with the first hash with JIT code changes; there may be extraneous diffs")
                # We expect local_files to be length 1, since we only attempted to download a single file.
                if len(local_files) > 1:
                    print("Error: downloaded more than one file?")

                coreclr_args.base_jit_path = local_files[0]
                print("Downloaded {}".format(blob_uri))
                print("Using baseline {}".format(coreclr_args.base_jit_path))
                return

            # We didn't find a baseline; keep looking
            hashnum += 1

        # We ran out of hashes of JIT changes, and didn't find a baseline. Give up.
        print("Error: no baseline JIT found")

    raise RuntimeError("No baseline JIT found")


def setup_args(args):
    """ Setup the args for SuperPMI to use.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    coreclr_args = CoreclrArguments(args, require_built_core_root=False, require_built_product_dir=False, require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "mode", # "mode" is from the `parser.add_subparsers(dest='mode')` call
                        lambda unused: True,
                        "Unable to set mode")

    def setup_spmi_location_arg(spmi_location):
        return os.path.abspath(os.path.join(coreclr_args.artifacts_location, "spmi")) if spmi_location is None else spmi_location

    def setup_jit_ee_version_arg(jit_ee_version):
        if jit_ee_version is not None:
            # The user passed a specific jit_ee_version on the command-line, so use that
            return jit_ee_version

        # Try to find the mcs tool, and run "mcs -printJITEEVersion" to find the version.
        # NOTE: we need to run this tool. So we need a version that will run. If a user specifies a "-arch" that creates
        #       a core_root path that won't run, like an arm32 core_root on an x64 machine, this won't work. This could happen
        #       if doing "upload" or "list-collections" on collections from a machine that didn't create the native collections.
        #       We should create a "native" core_root and use that in case there are "cross-arch" scenarios.

        # NOTE: there's some code duplication between here and SuperPMICollect::__init__, for finding the mcs path.

        if coreclr_args.host_os == "OSX" or coreclr_args.host_os == "Linux":
            mcs_tool_name = "mcs"
        elif coreclr_args.host_os == "Windows_NT":
            mcs_tool_name = "mcs.exe"
        else:
            raise RuntimeError("Unsupported OS.")

        mcs_path = os.path.join(coreclr_args.core_root, mcs_tool_name)
        if os.path.isfile(mcs_path):
            command = [mcs_path, "-printJITEEVersion"]
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_jit_ee_version, stderr_output = proc.communicate()
            return_code = proc.returncode
            if return_code == 0:
                mcs_jit_ee_version = stdout_jit_ee_version.decode('utf-8').strip()
                print("Using JIT/EE Version: {}".format(mcs_jit_ee_version))
                return mcs_jit_ee_version
            else:
                print("Note: \"{}\" failed".format(" ".join(command)))
        else:
            print("Note: \"{}\" not found".format(mcs_path))

        # Otherwise, use the default "unknown" version.
        default_jit_ee_version = "unknown-jit-ee-version"
        print("Using JIT/EE Version: {}".format(default_jit_ee_version))
        return default_jit_ee_version

    def verify_superpmi_common_args():

        # core_root has already been verified in CoreclrArguments() initialization.

        coreclr_args.verify(args,
                            "spmi_location",
                            lambda unused: True,
                            "Unable to set spmi_location",
                            modify_arg=lambda spmi_location: setup_spmi_location_arg(spmi_location))

        coreclr_args.verify(args,
                            "break_on_assert",
                            lambda unused: True,
                            "Unable to set break_on_assert")

        coreclr_args.verify(args,
                            "break_on_error",
                            lambda unused: True,
                            "Unable to set break_on_error")

        coreclr_args.verify(args,
                            "skip_cleanup",
                            lambda unused: True,
                            "Unable to set skip_cleanup")

        coreclr_args.verify(args,
                            "sequential",
                            lambda unused: True,
                            "Unable to set sequential.")

        coreclr_args.verify(args,
                            "log_file",
                            lambda unused: True,
                            "Unable to set log_file.")

        if coreclr_args.log_file is not None and not coreclr_args.sequential:
            print("-log_file requires --sequential")
            sys.exit(1)

    def verify_replay_common_args():

        coreclr_args.verify(args,
                            "force_download",
                            lambda unused: True,
                            "Unable to set force_download")

        coreclr_args.verify(args,
                            "altjit",                   # Must be set before `jit_path` (determine_jit_name() depends on it)
                            lambda unused: True,
                            "Unable to set altjit.")

        coreclr_args.verify(args,
                            "jit_ee_version",
                            lambda unused: True,
                            "Invalid JIT-EE Version.",
                            modify_arg=lambda arg: setup_jit_ee_version_arg(arg))

        coreclr_args.verify(args,
                            "filter",
                            lambda unused: True,
                            "Unable to set filter.")

        coreclr_args.verify(args,
                            "mch_files",
                            lambda unused: True,
                            "Unable to set mch_files")

    if coreclr_args.mode == "collect":

        verify_superpmi_common_args()

        coreclr_args.verify(args,
                            "collection_command",
                            lambda command: command is None or os.path.isfile(command),
                            "Unable to find script.")

        coreclr_args.verify(args,
                            "collection_args",
                            lambda unused: True,
                            "Unable to set collection_args",
                            modify_arg=lambda collection_args: collection_args.split(" ") if collection_args is not None else collection_args)

        coreclr_args.verify(args,
                            "pmi",
                            lambda unused: True,
                            "Unable to set pmi")

        coreclr_args.verify(args,
                            "pmi_assemblies",
                            lambda items: args.pmi is False or len(items) > 0,
                            "Unable to set pmi_assemblies",
                            modify_arg=lambda items: [item for item in items if os.path.isdir(item) or os.path.isfile(item)])

        coreclr_args.verify(args,
                            "pmi_location",
                            lambda unused: True,
                            "Unable to set pmi_location")

        coreclr_args.verify(args,
                            "output_mch_path",
                            lambda output_mch_path: output_mch_path is None or (not os.path.isdir(os.path.abspath(output_mch_path)) and not os.path.isfile(os.path.abspath(output_mch_path))),
                            "Invalid output_mch_path; is it an existing directory or file?")

        coreclr_args.verify(args,
                            "merge_mch_files",
                            lambda unused: True,
                            "Unable to set merge_mch_files.")

        coreclr_args.verify(args,
                            "mch_files",
                            lambda items: items is None or len(items) > 0,
                            "Unable to set mch_files.")

        coreclr_args.verify(args,
                            "skip_collect_mc_files",
                            lambda unused: True,
                            "Unable to set skip_collect_mc_files")

        coreclr_args.verify(args,
                            "temp_dir",
                            lambda unused: True,
                            "Unable to set temp_dir.")

        coreclr_args.verify(args,
                            "skip_collection_step",
                            lambda unused: True,
                            "Unable to set skip_collection_step.")

        coreclr_args.verify(args,
                            "skip_merge_step",
                            lambda unused: True,
                            "Unable to set skip_merge_step.")

        coreclr_args.verify(args,
                            "skip_clean_and_verify_step",
                            lambda unused: True,
                            "Unable to set skip_clean_and_verify_step.")

        coreclr_args.verify(args,
                            "use_zapdisable",
                            lambda unused: True,
                            "Unable to set use_zapdisable")

        coreclr_args.verify(False,          # Force it to false. TODO: support altjit collections?
                            "altjit",
                            lambda unused: True,
                            "Unable to set altjit.")

        if args.collection_command is None and args.merge_mch_files is not True:
            assert args.collection_args is None
            assert args.pmi is True
            assert len(args.pmi_assemblies) > 0

        if coreclr_args.merge_mch_files:
            assert len(coreclr_args.mch_files) > 0
            coreclr_args.skip_collection_step = True

    elif coreclr_args.mode == "replay":

        verify_superpmi_common_args()
        verify_replay_common_args()

        coreclr_args.verify(args,
                            "jit_path",
                            lambda jit_path: os.path.isfile(jit_path),
                            lambda jit_path: "Error: JIT not found at jit_path {}".format(jit_path),
                            modify_arg=lambda arg: os.path.join(coreclr_args.core_root, determine_jit_name(coreclr_args)) if arg is None else os.path.abspath(arg))

        standard_location = False
        if coreclr_args.product_location.lower() in coreclr_args.jit_path.lower():
            standard_location = True

        determined_arch = None
        determined_build_type = None
        if standard_location:
            # Get os/arch/flavor directory, e.g. split "F:\gh\runtime\artifacts\bin\coreclr\Windows_NT.x64.Checked" with "F:\gh\runtime\artifacts\bin\coreclr"
            # yielding
            # [0]: ""
            # [1]: "\Windows_NT.x64.Checked"
            standard_location_split = os.path.dirname(coreclr_args.jit_path).split(os.path.dirname(coreclr_args.product_location))
            assert(coreclr_args.host_os in standard_location_split[1])

            # Get arch/flavor. Remove leading slash.
            specialized_path = standard_location_split[1].split(os.path.sep)[1]

            # Split components: "Windows_NT.x64.Checked" into:
            # [0]: "Windows_NT"
            # [1]: "x64"
            # [2]: "Checked"
            determined_split = specialized_path.split(".")

            determined_arch = determined_split[1]
            determined_build_type = determined_split[2]

        # Make a more intelligent decision about the arch and build type
        # based on the path of the jit passed
        if standard_location and not coreclr_args.build_type in coreclr_args.jit_path:
            coreclr_args.verify(determined_arch.lower(),
                                "arch",
                                lambda unused: True,
                                "Unable to set arch")

            coreclr_args.verify(determined_build_type,
                                "build_type",
                                coreclr_args.check_build_type,
                                "Invalid build_type")

    elif coreclr_args.mode == "asmdiffs":

        verify_superpmi_common_args()
        verify_replay_common_args()

        coreclr_args.verify(args,
                            "base_jit_path",
                            lambda unused: True,
                            "Unable to set base_jit_path")

        coreclr_args.verify(args,
                            "diff_jit_path",
                            lambda jit_path: os.path.isfile(jit_path),
                            "Unable to set diff_jit_path",
                            modify_arg=lambda arg: os.path.join(coreclr_args.core_root, determine_jit_name(coreclr_args)) if arg is None else os.path.abspath(arg))

        coreclr_args.verify(args,
                            "git_hash",
                            lambda unused: True,
                            "Unable to set git_hash")

        coreclr_args.verify(args,
                            "base_git_hash",
                            lambda unused: True,
                            "Unable to set base_git_hash")

        coreclr_args.verify(args,
                            "diff_with_code",
                            lambda unused: True,
                            "Unable to set diff_with_code.")

        coreclr_args.verify(args,
                            "diff_with_code_only",
                            lambda unused: True,
                            "Unable to set diff_with_code_only.")

        coreclr_args.verify(args,
                            "temp_dir",
                            lambda unused: True,
                            "Unable to set temp_dir.")

        if coreclr_args.diff_with_code_only:
            # Set diff with code if we are not running SuperPMI to regenerate diffs.
            # This avoids having to re-run generating asm diffs.
            coreclr_args.verify(True,
                                "diff_with_code",
                                lambda unused: True,
                                "Unable to set diff_with_code.")

        coreclr_args.verify(args,
                            "diff_jit_dump",
                            lambda unused: True,
                            "Unable to set diff_jit_dump.")

        coreclr_args.verify(args,
                            "diff_jit_dump_only",
                            lambda unused: True,
                            "Unable to set diff_jit_dump_only.")

        if coreclr_args.diff_jit_dump_only:
            coreclr_args.verify(True,                          # force `diff_jit_dump` to True
                                "diff_jit_dump",
                                lambda unused: True,
                                "Unable to set diff_jit_dump.")

        process_base_jit_path_arg(coreclr_args)

        standard_location = False
        if coreclr_args.product_location.lower() in coreclr_args.base_jit_path.lower():
            standard_location = True

        determined_arch = None
        determined_build_type = None
        if standard_location:
            # Get os/arch/flavor directory, e.g. split "F:\gh\runtime\artifacts\bin\coreclr\Windows_NT.x64.Checked" with "F:\gh\runtime\artifacts\bin\coreclr"
            # yielding
            # [0]: ""
            # [1]: "\Windows_NT.x64.Checked"
            standard_location_split = os.path.dirname(coreclr_args.base_jit_path).split(os.path.dirname(coreclr_args.product_location))
            assert(coreclr_args.host_os in standard_location_split[1])

            # Get arch/flavor. Remove leading slash.
            specialized_path = standard_location_split[1].split(os.path.sep)[1]

            # Split components: "Windows_NT.x64.Checked" into:
            # [0]: "Windows_NT"
            # [1]: "x64"
            # [2]: "Checked"
            determined_split = specialized_path.split(".")

            determined_arch = determined_split[1]
            determined_build_type = determined_split[2]

        # Make a more intelligent decision about the arch and build type
        # based on the path of the jit passed
        if standard_location and not coreclr_args.build_type in coreclr_args.base_jit_path:
            coreclr_args.verify(determined_build_type,
                                "build_type",
                                coreclr_args.check_build_type,
                                "Invalid build_type")

        if standard_location and not coreclr_args.arch in coreclr_args.base_jit_path:
            coreclr_args.verify(determined_arch.lower(),
                                "arch",
                                lambda unused: True,
                                "Unable to set arch")

        coreclr_args.verify(determine_coredis_tools(coreclr_args),
                            "coredistools_location",
                            lambda coredistools_path: os.path.isfile(coredistools_path),
                            "Unable to find coredistools.")

    elif coreclr_args.mode == "upload":

        coreclr_args.verify(args,
                            "az_storage_key",
                            lambda item: item is not None,
                            "Specify az_storage_key or set environment variable CLRJIT_AZ_KEY to the key to use.",
                            modify_arg=lambda arg: os.environ["CLRJIT_AZ_KEY"] if arg is None and "CLRJIT_AZ_KEY" in os.environ else arg)

        coreclr_args.verify(args,
                            "jit_location",
                            lambda unused: True,
                            "Unable to set jit_location.")

        coreclr_args.verify(args,
                            "jit_ee_version",
                            lambda unused: True,
                            "Invalid JIT-EE Version.",
                            modify_arg=lambda arg: setup_jit_ee_version_arg(arg))

        coreclr_args.verify(args,
                            "mch_files",
                            lambda unused: True,
                            "Unable to set mch_files")

    elif coreclr_args.mode == "download":

        coreclr_args.verify(args,
                            "spmi_location",
                            lambda unused: True,
                            "Unable to set spmi_location",
                            modify_arg=lambda spmi_location: setup_spmi_location_arg(spmi_location))

        coreclr_args.verify(args,
                            "force_download",
                            lambda unused: True,
                            "Unable to set force_download")

        coreclr_args.verify(args,
                            "jit_ee_version",
                            lambda unused: True,
                            "Invalid JIT-EE Version.",
                            modify_arg=lambda arg: setup_jit_ee_version_arg(arg))

        coreclr_args.verify(args,
                            "filter",
                            lambda unused: True,
                            "Unable to set filter.")

        coreclr_args.verify(args,
                            "mch_files",
                            lambda unused: True,
                            "Unable to set mch_files")

    elif coreclr_args.mode == "list-collections":

        coreclr_args.verify(args,
                            "jit_ee_version",
                            lambda unused: True,
                            "Invalid JIT-EE Version.",
                            modify_arg=lambda arg: setup_jit_ee_version_arg(arg))

        coreclr_args.verify(args,
                            "all",
                            lambda unused: True,
                            "Unable to set all")

        coreclr_args.verify(args,
                            "local",
                            lambda unused: True,
                            "Unable to set local")

        # spmi_location is needed for `--local` to determine the local cache location
        coreclr_args.verify(args,
                            "spmi_location",
                            lambda unused: True,
                            "Unable to set spmi_location",
                            modify_arg=lambda spmi_location: os.path.abspath(os.path.join(coreclr_args.artifacts_location, "spmi")) if spmi_location is None else spmi_location)

    return coreclr_args

################################################################################
# main
################################################################################

def main(args):
    """ Main method
    """

    # await/async requires python >= 3.7
    if sys.version_info.major < 3 and sys.version_info.minor < 7:
        print("Error, language features require the latest python version.")
        print("Please install python 3.7 or greater")

        return 1

    # Force tiered compilation off. It will affect both collection and replay.
    # REVIEW: Is this true for replay? We specifically set this when doing collections. Can we remove this line?
    #         Or move it more close to the location that requires it, and output to the console that we're setting this?
    os.environ["COMPlus_TieredCompilation"] = "0"

    coreclr_args = setup_args(args)
    success = True

    if coreclr_args.mode == "collect":
        # Start a new SuperPMI Collection.

        begin_time = datetime.datetime.now()

        print("SuperPMI collect")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        collection = SuperPMICollect(coreclr_args)
        success = collection.collect()

        print("Finished SuperPMI collect")

        if coreclr_args.output_mch_path is not None:
            print("MCH path: {}".format(coreclr_args.output_mch_path))

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
        print("Elapsed time: {}".format(elapsed_time))

    elif coreclr_args.mode == "replay":
        # Start a new SuperPMI Replay

        process_mch_files_arg(coreclr_args)
        mch_files = get_mch_files_for_replay(coreclr_args)
        if mch_files is None:
            return 1

        begin_time = datetime.datetime.now()

        print("SuperPMI replay")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        jit_path = coreclr_args.jit_path

        print("")
        print("JIT Path: {}".format(jit_path))

        print("Using MCH files:")
        for mch_file in mch_files:
            print("  {}".format(mch_file))

        replay = SuperPMIReplay(coreclr_args, mch_files, jit_path)
        success = replay.replay()

        print("Finished SuperPMI replay")

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
        print("Elapsed time: {}".format(elapsed_time))

    elif coreclr_args.mode == "asmdiffs":
        # Start a new SuperPMI Replay with AsmDiffs

        process_mch_files_arg(coreclr_args)
        mch_files = get_mch_files_for_replay(coreclr_args)
        if mch_files is None:
            return 1

        begin_time = datetime.datetime.now()

        print("SuperPMI ASM diffs")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        base_jit_path = coreclr_args.base_jit_path
        diff_jit_path = coreclr_args.diff_jit_path

        print("")
        print("Base JIT Path: {}".format(base_jit_path))
        print("Diff JIT Path: {}".format(diff_jit_path))

        print("Using MCH files:")
        for mch_file in mch_files:
            print("  {}".format(mch_file))

        asm_diffs = SuperPMIReplayAsmDiffs(coreclr_args, mch_files, base_jit_path, diff_jit_path)
        success = asm_diffs.replay_with_asm_diffs()

        print("Finished SuperPMI replay")

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
        print("Elapsed time: {}".format(elapsed_time))

    elif coreclr_args.mode == "upload":
        begin_time = datetime.datetime.now()

        print("SuperPMI upload")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        upload_mch(coreclr_args)

        print("Finished SuperPMI upload")

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
        print("Elapsed time: {}".format(elapsed_time))

    elif coreclr_args.mode == "download":
        begin_time = datetime.datetime.now()

        print("SuperPMI download")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        # Processing the arg does the download and caching
        process_mch_files_arg(coreclr_args)

        print("Finished SuperPMI download")

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
        print("Elapsed time: {}".format(elapsed_time))

    elif coreclr_args.mode == "list-collections":
        if coreclr_args.local:
            list_collections_local_command(coreclr_args)
        else:
            list_collections_command(coreclr_args)

    else:
        raise NotImplementedError(coreclr_args.mode)

    return 0 if success else 1

################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
