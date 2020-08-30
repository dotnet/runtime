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
# Script to handle running SuperPMI Collections, and replays. In addition, this
# script provides support for SuperPMI ASM diffs. Note that some of the options
# provided by this script are also provided in our SuperPMI collect test. The
# test can be found here: https://github.com/dotnet/coreclr/blob/master/tests/src/JIT/superpmi/superpmicollect.cs.
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
import re
import string
import urllib
import urllib.request
import zipfile

import xml.etree.ElementTree

from collections import defaultdict
from sys import platform as _platform

from coreclr_arguments import *

################################################################################
# Argument Parser
################################################################################

description = """\
Script to run SuperPMI replay, ASM diffs, and collections.
The script also manages the Azure store of precreated SuperPMI collection files.
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

list_collections_description = """\
List the existing collections in the SuperPMI Azure storage.
"""

collection_help = "Which collection type to use for replays. Default is to run everything. Use 'superpmi.py list-collections' to find available collections."

log_file_help = "Write output to a log file. Requires --sequential."

host_os_help = "OS (Windows_NT, OSX, Linux). Default: current OS."

arch_help = "Architecture (x64, x86, arm, arm64). Default: current architecture."

build_type_help = "Build type (Debug, Checked, Release). Default: Checked."

core_root_help = "Core_Root location. Optional; it will be deduced if possible from runtime repo root."

product_location_help = "Built Product directory location. Optional; it will be deduced if possible from runtime repo root."

spmi_location_help = """\
Directory in which to put SuperPMI files, such as downloaded MCH files, asm diffs, and repro .MC file.
Optional. Default is 'spmi' within the repo 'artifacts' directory.
"""

superpmi_collect_help = """\
Command to run SuperPMI collect over. Note that there cannot be any dotnet CLI commands
invoked inside this command, as they will fail due to the shim altjit being set.
"""

mch_file_help = """\
Location of the MCH file to use for replay. Note that this may either be a path on disk or a URI to a MCH file to download.
Use this MCH file instead of a named collection set from the cloud MCH file store.
"""

skip_cleanup_help = "Skip intermediate file removal."

break_on_assert_help = "Enable break on assert during SuperPMI replay."

break_on_error_help = "Enable break on error during SuperPMI replay."

force_download_help = "If downloading an MCH file, always download it. Don't use an existing file in the download location."

# Start of parser object creation.

parser = argparse.ArgumentParser(description=description)

subparsers = parser.add_subparsers(dest='mode', help="Command to invoke")

# Create a set of argument common to all commands that run SuperPMI.

superpmi_common_parser = argparse.ArgumentParser(add_help=False)

superpmi_common_parser.add_argument("-arch", default=CoreclrArguments.provide_default_arch(), help=arch_help)
superpmi_common_parser.add_argument("-build_type", default="Checked", help=build_type_help)
superpmi_common_parser.add_argument("-core_root", help=core_root_help)
superpmi_common_parser.add_argument("-spmi_location", help=spmi_location_help)
superpmi_common_parser.add_argument("--break_on_assert", action="store_true", help=break_on_assert_help)
superpmi_common_parser.add_argument("--break_on_error", action="store_true", help=break_on_error_help)
superpmi_common_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)
superpmi_common_parser.add_argument("--sequential", action="store_true", help="Run SuperPMI in sequential mode.")
superpmi_common_parser.add_argument("-log_file", help=log_file_help)

# subparser for collect
collect_parser = subparsers.add_parser("collect", description=collect_description, parents=[superpmi_common_parser])

# Add required arguments
collect_parser.add_argument("collection_command", nargs='?', help=superpmi_collect_help)
collect_parser.add_argument("collection_args", nargs='?', help="Arguments to pass to the SuperPMI collect command.")

collect_parser.add_argument("--pmi", action="store_true", help="Run PMI on a set of directories or assemblies.")
collect_parser.add_argument("-pmi_assemblies", dest="pmi_assemblies", nargs="+", default=[], help="Pass a sequence of managed dlls or directories to recursively run PMI over while collecting. Required if --pmi is specified.")
collect_parser.add_argument("-pmi_location", help="Path to pmi.dll to use during PMI run. Optional; pmi.dll will be downloaded from Azure storage if necessary.")
collect_parser.add_argument("-output_mch_path", help="Location to place the final MCH file. By default it will be placed at artifacts/mch/$(os).$(arch).$(build_type)/$(os).$(arch).$(build_type).mch")
collect_parser.add_argument("--merge_mch_files", action="store_true", help="Merge multiple MCH files. Use the -mch_files flag to pass a list of MCH files to merge.")
collect_parser.add_argument("-mch_files", nargs='+', help="Pass a sequence of MCH files which will be merged. Required by --merge_mch_files.")
collect_parser.add_argument("--use_zapdisable", action="store_true", help="Sets COMPlus_ZapDisable=1 when doing collection to cause NGEN/ReadyToRun images to not be used, and thus causes JIT compilation and SuperPMI collection of these methods.")

# Allow for continuing a collection in progress
collect_parser.add_argument("-existing_temp_dir", help="Specify an existing temporary directory to use. Useful if continuing an ongoing collection process, or forcing a temporary directory to a particular hard drive. Optional; default is to create a temporary directory in the usual TEMP location.")
collect_parser.add_argument("--has_run_collection_command", action="store_true", help="Do not run the collection step.")
collect_parser.add_argument("--has_merged_mch", action="store_true", help="Do not run the merge step.")
collect_parser.add_argument("--has_verified_clean_mch", action="store_true", help="Do not run the collection cleaning step.")
collect_parser.add_argument("--skip_collect_mc_files", action="store_true", help="Do not collect .MC files")

# Create a set of argument common to all SuperPMI replay commands, namely basic replay and ASM diffs.
# Note that SuperPMI collection also runs a replay to verify the final MCH file, so many arguments
# common to replay are also applicable that that replay as well.

replay_common_parser = argparse.ArgumentParser(add_help=False)

replay_common_parser.add_argument("-mch_file", help=mch_file_help)
replay_common_parser.add_argument("-product_location", help=product_location_help)
replay_common_parser.add_argument("--force_download", action="store_true", help=force_download_help)
replay_common_parser.add_argument("-altjit", nargs='?', const=True, help="Replay with an altjit. If an argument is specified, it is used as the name of the altjit (e.g., 'protojit.dll'). Otherwise, the default altjit name is used.")

# subparser for replay
replay_parser = subparsers.add_parser("replay", description=replay_description, parents=[superpmi_common_parser, replay_common_parser])

# Add required arguments
replay_parser.add_argument("collection", nargs='?', default="default", help=collection_help)

replay_parser.add_argument("-jit_path", help="Path to clrjit. Defaults to Core_Root JIT.")

# subparser for asmDiffs
asm_diff_parser = subparsers.add_parser("asmdiffs", description=asm_diff_description, parents=[superpmi_common_parser, replay_common_parser])

# Add required arguments
asm_diff_parser.add_argument("base_jit_path", help="Path to baseline clrjit.")
asm_diff_parser.add_argument("diff_jit_path", nargs='?', help="Path to diff clrjit. Defaults to Core_Root JIT.")

asm_diff_parser.add_argument("collection", nargs='?', default="default", help=collection_help)
asm_diff_parser.add_argument("--diff_with_code", action="store_true", help="Invoke Visual Studio Code to view any diffs.")
asm_diff_parser.add_argument("--diff_with_code_only", action="store_true", help="Invoke Visual Studio Code to view any diffs. Only run the diff command, do not run SuperPMI to regenerate diffs.")
asm_diff_parser.add_argument("--diff_jit_dump", action="store_true", help="Generate JitDump output for diffs. Default: only generate asm, not JitDump.")
asm_diff_parser.add_argument("--diff_jit_dump_only", action="store_true", help="Only diff JitDump output, not asm.")
asm_diff_parser.add_argument("-previous_temp_location", help="Specify a temporary directory used for a previous ASM diffs run (for which --skip_cleanup was used) to view the results.")

# subparser for upload
upload_parser = subparsers.add_parser("upload", description=upload_description)
upload_parser.add_argument("az_storage_key", nargs='?', help="Key for the clrjit Azure Storage location. Default: use the value of the CLRJIT_AZ_KEY environment variable.")
upload_parser.add_argument("-mch_files", nargs='+', help="MCH files to pass")
upload_parser.add_argument("-jit_location", help="Location for the base clrjit. If not passed this will be assumed to be from the Core_Root.")
upload_parser.add_argument("-arch", default=CoreclrArguments.provide_default_arch(), help=arch_help)
upload_parser.add_argument("-build_type", default="Checked", help=build_type_help)
upload_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)

# subparser for list-collections
list_collections_parser = subparsers.add_parser("list-collections", description=list_collections_description)

list_collections_parser.add_argument("-host_os", default=CoreclrArguments.provide_default_host_os(), help=host_os_help)
list_collections_parser.add_argument("-arch", default=CoreclrArguments.provide_default_arch(), help=arch_help)
list_collections_parser.add_argument("-build_type", default="Checked", help=build_type_help)

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
        in a semicolon-separated `pathlist` (e.g., PATH environment variable).

    Args:
        name (str)              : name to search for
        pathlist (str)          : semicolon-separated string of directory names to search
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
        in a semicolon-separated `pathlist` (e.g., PATH environment variable).

    Args:
        filename (str)          : name to search for
        pathlist (str)          : semicolon-separated string of directory names to search

    Returns:
        (str) The pathname of the object, or None if not found.
    """
    return find_in_path(filename, pathlist)

def find_dir(dirname, pathlist):
    """ Find a directory name in the file system by searching the directories
        in a semicolon-separated `pathlist` (e.g., PATH environment variable).

    Args:
        dirname (str)           : name to search for
        pathlist (str)          : semicolon-separated string of directory names to search

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
        item_queue = asyncio.Queue()
        for item in items:
            item_queue.put_nowait(item)

        self.items = items
        self.subproc_count = subproc_count
        self.verbose = verbose

        if 'win32' in sys.platform:
            # Windows specific event-loop policy & cmd
            asyncio.set_event_loop_policy(asyncio.WindowsProactorEventLoopPolicy())

    async def __get_item__(self, item, index, size, async_callback, *extra_args):
        """ Wrapper to the async callback which will schedule based on the queue
        """

        # Wait for the queue to become free. Then start
        # running the sub process.
        subproc_id = await self.subproc_count_queue.get()

        print_prefix = ""

        if self.verbose:
            print_prefix = "[{}:{}]: ".format(index, size)

        await async_callback(print_prefix, item, *extra_args)

        # Add back to the queue, incase another process wants to run.
        self.subproc_count_queue.put_nowait(subproc_id)

    async def __run_to_completion__(self, async_callback, *extra_args):
        """ async wrapper for run_to_completion
        """

        chunk_size = self.subproc_count

        # Create a queue with a chunk size of the cpu count
        #
        # Each run_pmi invocation will remove an item from the
        # queue before running a potentially long running pmi run.
        #
        # When the queue is drained, we will wait queue.get which
        # will wait for when a run_pmi instance has added back to the
        subproc_count_queue = asyncio.Queue(chunk_size)
        diff_queue = asyncio.Queue()

        for item in self.items:
            diff_queue.put_nowait(item)

        for item in range(chunk_size):
            subproc_count_queue.put_nowait(item)

        self.subproc_count_queue = subproc_count_queue
        tasks = []
        size = diff_queue.qsize()

        count = 1
        item = diff_queue.get_nowait() if not diff_queue.empty() else None
        while item is not None:
            tasks.append(self.__get_item__(item, count, size, async_callback, *extra_args))
            count += 1

            item = diff_queue.get_nowait() if not diff_queue.empty() else None

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
        self.superpmi_path = os.path.join(coreclr_args.core_root, determine_superpmi_tool_name(coreclr_args))
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
            with TempDir(self.coreclr_args.existing_temp_dir) as temp_location:
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

                # If we have passed existing_temp_dir, then we have a few flags we need
                # to check to see where we are in the collection process. Note that this
                # functionality exists to help not lose progress during a SuperPMI collection.

                # It is not unreasonable for the SuperPMI collection to take many hours
                # therefore allow re-use of a collection in progress

                if not self.coreclr_args.has_run_collection_command:
                    self.__collect_mc_files__()

                if not self.coreclr_args.has_merged_mch:
                    if not self.coreclr_args.merge_mch_files:
                        self.__merge_mc_files__()
                    else:
                        self.__merge_mch_files__()

                if not self.coreclr_args.has_verified_clean_mch:
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

            if self.coreclr_args.use_zapdisable:
                env_copy["COMPlus_ZapDisable"] = "1"

            print("Starting collection.")
            print("")
            print_platform_specific_environment_vars(self.coreclr_args, "SuperPMIShimLogPath", self.temp_location)
            print_platform_specific_environment_vars(self.coreclr_args, "SuperPMIShimPath", self.jit_path)
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJit", "*")
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJitName", self.collection_shim_name)
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJitNgen", "*")
            print("")

            if self.collection_command != None:
                print("%s %s" % (self.collection_command, " ".join(self.collection_args)))

                assert isinstance(self.collection_command, str)
                assert isinstance(self.collection_args, list)

                self.command = [self.collection_command,] + self.collection_args
                proc = subprocess.Popen(self.command, env=env_copy)
                proc.communicate()
                return_code = proc.returncode

            if self.coreclr_args.pmi is True:
                def get_all_assemblies(location, root=True):
                    """ Return all potential managed assemblies in a directory
                    """

                    assert os.path.isdir(location) or os.path.isfile(location)

                    valid_extensions = [".dll", ".exe"]

                    assemblies = []

                    if os.path.isdir(location):
                        for item in os.listdir(location):
                            assemblies += get_all_assemblies(os.path.join(location, item), False)
                    else:
                        for item in valid_extensions:
                            if location.endswith(item):
                                assemblies.append(location)

                    return assemblies

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
                    if os.path.isdir(item):
                        assemblies += get_all_assemblies(item)
                    else:
                        assemblies.append(item)

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
        """ Merge the MCH files that were passed

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

        spmi_replay = SuperPMIReplay(self.coreclr_args, self.final_mch_file, self.jit_path)
        passed = spmi_replay.replay()

        if not passed:
            raise RuntimeError("Error, unclean replay.")

################################################################################
# SuperPMI Replay
################################################################################

class SuperPMIReplay:
    """ SuperPMI Replay class

    Notes:
        The object is responsible for replaying the MCH file given to the
        instance of the class
    """

    def __init__(self, coreclr_args, mch_file, jit_path):
        """ Constructor

        Args:
            coreclr_args (CoreclrArguments) : parsed args
            mch_file (str)                  : MCH file to replay
            jit_path (str)                  : path to clrjit/libclrjit.

        """

        self.jit_path = jit_path
        self.mch_file = mch_file
        self.superpmi_path = os.path.join(coreclr_args.core_root, determine_superpmi_tool_name(coreclr_args))

        self.coreclr_args = coreclr_args

    ############################################################################
    # Instance Methods
    ############################################################################

    def replay(self):
        """ Replay the given SuperPMI collection

        Returns:
            sucessful_replay (bool)
        """

        return_code = False

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

            self.fail_mcl_file = os.path.join(temp_location, "fail.mcl")

            flags = [
                "-f", # Failing mc List
                self.fail_mcl_file,
                "-r", # Repro name, create .mc repro files
                os.path.join(temp_location, "repro")
            ]

            altjit_string = "*" if self.coreclr_args.altjit else ""
            altjit_flags = [
                "-jitoption", "force", "AltJit=" + altjit_string,
                "-jitoption", "force", "AltJitNgen=" + altjit_string,
                "-jitoption", "force", "EnableExtraSuperPmiQueries=0"
            ]
            flags += altjit_flags

            if not self.coreclr_args.sequential:
                flags += [
                    "-p" # parallel
                ]

            if self.coreclr_args.break_on_assert:
                flags += [
                    "-boa" # break on assert
                ]

            if self.coreclr_args.break_on_error:
                flags += [
                    "-boe" # break on error
                ]

            if self.coreclr_args.log_file != None:
                flags += [
                    "-w",
                    self.coreclr_args.log_file
                ]

            command = [self.superpmi_path] + flags + [self.jit_path, self.mch_file]

            print("Invoking: " + " ".join(command))
            proc = subprocess.Popen(command)
            proc.communicate()

            return_code = proc.returncode

            if return_code == 0:
                print("Clean SuperPMI replay")
                return_code = True

            if is_nonzero_length_file(self.fail_mcl_file):
                # Unclean replay.
                #
                # Save the contents of the fail.mcl file to dig into failures.

                assert(return_code != 0)

                if return_code == -1:
                    print("General fatal error.")
                elif return_code == -2:
                    print("Jit failed to initialize.")
                elif return_code == 1:
                    print("Compilation failures.")
                elif return_code == 139 and self.coreclr_args != "Windows_NT":
                    print("Fatal error, SuperPMI has returned SIG_SEV (segmentation fault).")
                else:
                    print("Unknown error code.")

                self.fail_mcl_contents = None
                mcl_lines = []
                with open(self.fail_mcl_file) as file_handle:
                    mcl_lines = file_handle.readlines()
                    mcl_lines = [item.strip() for item in mcl_lines]
                    self.fail_mcl_contents = os.linesep.join(mcl_lines)
                    print("Method numbers with compilation failures:")
                    print(self.fail_mcl_contents)

                # If there are any .mc files, drop them into artifacts/repro/<host_os>.<arch>.<build_type>/*.mc
                mc_files = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if item.endswith(".mc")]

                if len(mc_files) > 0:
                    repro_location = create_unique_directory_name(self.coreclr_args.spmi_location, "repro.{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))

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

                    print("To run a specific failure (replace .mc filename as needed):")
                    print("")
                    print("{} {} {} {}{}xxxxx.mc".format(self.superpmi_path, " ".join(altjit_flags), self.jit_path, os.path.sep, repro_location))
                    print("")

            if not self.coreclr_args.skip_cleanup:
                if os.path.isfile(self.fail_mcl_file):
                    os.remove(self.fail_mcl_file)
                    self.fail_mcl_file = None

        return return_code

################################################################################
# SuperPMI Replay/AsmDiffs
################################################################################

class SuperPMIReplayAsmDiffs:
    """ SuperPMI Replay AsmDiffs class

    Notes:
        The object is responsible for replaying the mch file given to the
        instance of the class and doing diffs using the two passed jits.
    """

    def __init__(self, coreclr_args, mch_file, base_jit_path, diff_jit_path):
        """ Constructor

        Args:
            coreclr_args (CoreclrArguments) : parsed args
            mch_file (str)                  : MCH file to replay
            base_jit_path (str)             : path to baselin clrjit/libclrjit
            diff_jit_path (str)             : path to diff clrjit/libclrjit

        """

        self.base_jit_path = base_jit_path
        self.diff_jit_path = diff_jit_path
        self.mch_file = mch_file
        self.superpmi_path = os.path.join(coreclr_args.core_root, determine_superpmi_tool_name(coreclr_args))

        self.coreclr_args = coreclr_args

    ############################################################################
    # Instance Methods
    ############################################################################

    def replay_with_asm_diffs(self):
        """ Replay the given SuperPMI collection

        Returns:
            sucessful_replay (bool)
        """

        return_code = False

        # Possible return codes from SuperPMI
        #
        # 0  : success
        # -1 : general fatal error (e.g., failed to initialize, failed to read files)
        # -2 : JIT failed to initialize
        # 1  : there were compilation failures
        # 2  : there were assembly diffs

        with TempDir(self.coreclr_args.previous_temp_location) as temp_location:
            print("")
            print("Temp Location: {}".format(temp_location))
            print("")

            self.fail_mcl_file = os.path.join(temp_location, "fail.mcl")
            self.diff_mcl_file = os.path.join(temp_location, "diff.mcl")

            if self.coreclr_args.previous_temp_location is None:

                flags = [
                    "-a", # Asm diffs
                    "-f", # Failing mc List
                    self.fail_mcl_file,
                    "-diffMCList", # Create all of the diffs in an mcl file
                    self.diff_mcl_file,
                    "-r", # Repro name, create .mc repro files
                    os.path.join(temp_location, "repro")
                ]

                altjit_string = "*" if self.coreclr_args.altjit else ""
                altjit_flags = [
                    "-jitoption", "force", "AltJit=" + altjit_string,
                    "-jitoption", "force", "AltJitNgen=" + altjit_string,
                    "-jitoption", "force", "EnableExtraSuperPmiQueries=0",
                    "-jit2option", "force", "AltJit=" + altjit_string,
                    "-jit2option", "force", "AltJitNgen=" + altjit_string,
                    "-jit2option", "force", "EnableExtraSuperPmiQueries=0"
                ]
                flags += altjit_flags

                if not self.coreclr_args.sequential:
                    flags += [
                        "-p" # parallel
                    ]

                if self.coreclr_args.break_on_assert:
                    flags += [
                        "-boa" # break on assert
                    ]

                if self.coreclr_args.break_on_error:
                    flags += [
                        "-boe" # break on error
                    ]

                if self.coreclr_args.log_file != None:
                    flags += [
                        "-w",
                        self.coreclr_args.log_file
                    ]

                if not self.coreclr_args.diff_with_code_only:
                    # Change the working directory to the core root we will call SuperPMI from.
                    # This is done to allow libcoredistools to be loaded correctly on unix
                    # as the loadlibrary path will be relative to the current directory.
                    with ChangeDir(self.coreclr_args.core_root) as dir:
                        command = [self.superpmi_path] + flags + [self.base_jit_path, self.diff_jit_path, self.mch_file]
                        print("Invoking: " + " ".join(command))
                        proc = subprocess.Popen(command)
                        proc.communicate()
                        return_code = proc.returncode
                        if return_code == 0:
                            print("Clean SuperPMI replay")
                else:
                    return_code = 2
            else:
                return_code = 1;

            if is_nonzero_length_file(self.fail_mcl_file):
                # Unclean replay.
                #
                # Save the contents of the fail.mcl file to dig into failures.

                assert(return_code != 0)

                if return_code == -1:
                    print("General fatal error.")
                elif return_code == -2:
                    print("Jit failed to initialize.")
                elif return_code == 1:
                    print("Compilation failures.")
                elif return_code == 139 and self.coreclr_args != "Windows_NT":
                    print("Fatal error, SuperPMI has returned SIG_SEV (segmentation fault).")
                else:
                    print("Unknown error code.")

                self.fail_mcl_contents = None
                mcl_lines = []
                with open(self.fail_mcl_file) as file_handle:
                    mcl_lines = file_handle.readlines()
                    mcl_lines = [item.strip() for item in mcl_lines]
                    self.fail_mcl_contents = os.linesep.join(mcl_lines)
                    print("Method numbers with compilation failures:")
                    print(self.fail_mcl_contents)

                # If there are any .mc files, drop them into artifacts/repro/<host_os>.<arch>.<build_type>/*.mc
                mc_files = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if item.endswith(".mc")]

                if len(mc_files) > 0:
                    repro_location = create_unique_directory_name(self.coreclr_args.spmi_location, "repro.{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))

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
                    print("{} {} {} {}{}xxxxx.mc".format(self.superpmi_path, " ".join(altjit_flags), self.diff_jit_path, os.path.sep, repro_location))
                    print("")

            # There were diffs. Go through each method that created diffs and
            # create a base/diff asm file with diffable asm. In addition, create
            # a standalone .mc for easy iteration.
            if is_nonzero_length_file(self.diff_mcl_file) or self.coreclr_args.diff_with_code_only:
                # AsmDiffs.
                #
                # Save the contents of the fail.mcl file to dig into failures.

                assert(return_code != 0)

                if return_code == -1:
                    print("General fatal error.")
                elif return_code == -2:
                    print("Jit failed to initialize.")
                elif return_code == 1:
                    print("Compilation failures.")
                elif return_code == 139 and self.coreclr_args != "Windows_NT":
                    print("Fatal error, SuperPMI has returned SIG_SEV (segmentation fault).")
                elif return_code == 2:
                    print("Asm diffs found.")
                else:
                    print("Unknown error code.")

                if not self.coreclr_args.diff_with_code_only:
                    self.diff_mcl_contents = None
                    with open(self.diff_mcl_file) as file_handle:
                        mcl_lines = file_handle.readlines()
                        mcl_lines = [item.strip() for item in mcl_lines]
                        self.diff_mcl_contents = mcl_lines

                bin_asm_location = create_unique_directory_name(self.coreclr_args.spmi_location, "asm.{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))
                base_asm_location = os.path.join(bin_asm_location, "base")
                diff_asm_location = os.path.join(bin_asm_location, "diff")

                if not self.coreclr_args.diff_with_code_only:
                    # Create a diff and baseline directory
                    assert(not os.path.isdir(base_asm_location))
                    assert(not os.path.isdir(diff_asm_location))

                    os.makedirs(base_asm_location)
                    os.makedirs(diff_asm_location)

                    if self.coreclr_args.diff_jit_dump:
                        # Create a diff and baseline directory for jit_dumps
                        bin_dump_location = create_unique_directory_name(self.coreclr_args.spmi_location, "jitdump.{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))
                        base_dump_location = os.path.join(bin_dump_location, "base")
                        diff_dump_location = os.path.join(bin_dump_location, "diff")

                        assert(not os.path.isdir(base_dump_location))
                        assert(not os.path.isdir(diff_dump_location))

                        os.makedirs(base_dump_location)
                        os.makedirs(diff_dump_location)

                text_differences = asyncio.Queue()
                jit_dump_differences = asyncio.Queue()

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
                        "COMPlus_JitEnableNoWayAssert": "1",
                        "COMPlus_JitNoForceFallback": "1",
                        "COMPlus_JitRequired": "1",
                        "COMPlus_JitDump": "*" }

                altjit_string = "*" if self.coreclr_args.altjit else ""
                altjit_flags = [
                    "-jitoption", "force", "AltJit=" + altjit_string,
                    "-jitoption", "force", "AltJitNgen=" + altjit_string,
                    "-jitoption", "force", "EnableExtraSuperPmiQueries=0"
                ]

                async def create_asm(print_prefix, item, self, text_differences, base_asm_location, diff_asm_location):
                    """ Run superpmi over an mc to create dasm for the method.
                    """
                    # Setup to call SuperPMI for both the diff jit and the base jit

                    flags = [
                        "-c",
                        item,
                        "-v",
                        "q" # only log from the jit.
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

                        command = [self.superpmi_path] + flags + [self.base_jit_path, self.mch_file]

                        with open(os.path.join(base_asm_location, "{}.dasm".format(item)), 'w') as file_handle:
                            print("{}Invoking: {}".format(print_prefix, " ".join(command)))
                            proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                            await proc.communicate()

                        with open(os.path.join(base_asm_location, "{}.dasm".format(item)), 'r') as file_handle:
                            base_txt = file_handle.read()

                        command = [self.superpmi_path] + flags + [self.diff_jit_path, self.mch_file]

                        with open(os.path.join(diff_asm_location, "{}.dasm".format(item)), 'w') as file_handle:
                            print("Invoking: ".format(print_prefix) + " ".join(command))
                            proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                            await proc.communicate()

                        with open(os.path.join(diff_asm_location, "{}.dasm".format(item)), 'r') as file_handle:
                            diff_txt = file_handle.read()

                        # Sanity checks
                        assert base_txt != ""
                        assert base_txt is not None

                        assert diff_txt != ""
                        assert diff_txt is not None

                        if base_txt != diff_txt:
                            text_differences.put_nowait(item)

                    print("{}Finished. ------------------------------------------------------------------".format(print_prefix))

                async def create_jit_dump(print_prefix, item, self, jit_dump_differences, base_dump_location, diff_dump_location):
                    """ Run superpmi over an mc to create dasm for the method.
                    """
                    # Setup to call SuperPMI for both the diff jit and the base jit

                    flags = [
                        "-c",
                        item,
                        "-v",
                        "q" # only log from the jit.
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

                        command = [self.superpmi_path] + flags + [self.base_jit_path, self.mch_file]

                        with open(os.path.join(base_dump_location, "{}.txt".format(item)), 'w') as file_handle:
                            print("{}Invoking: ".format(print_prefix) + " ".join(command))
                            proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                            await proc.communicate()

                        with open(os.path.join(base_dump_location, "{}.txt".format(item)), 'r') as file_handle:
                            base_txt = file_handle.read()

                        command = [self.superpmi_path] + flags + [self.diff_jit_path, self.mch_file]

                        with open(os.path.join(diff_dump_location, "{}.txt".format(item)), 'w') as file_handle:
                            print("{}Invoking: ".format(print_prefix) + " ".join(command))
                            proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                            await proc.communicate()

                        with open(os.path.join(diff_dump_location, "{}.txt".format(item)), 'r') as file_handle:
                            diff_txt = file_handle.read()

                        # Sanity checks
                        assert base_txt != ""
                        assert base_txt is not None

                        assert diff_txt != ""
                        assert diff_txt is not None

                        if base_txt != diff_txt:
                            jit_dump_differences.put_nowait(item)

                if not self.coreclr_args.diff_with_code_only:
                    diff_items = []

                    for item in self.diff_mcl_contents:
                        diff_items.append(item)

                    print("Creating asm files")
                    subproc_helper = AsyncSubprocessHelper(diff_items, verbose=True)
                    subproc_helper.run_to_completion(create_asm, self, text_differences, base_asm_location, diff_asm_location)

                    if self.coreclr_args.diff_jit_dump:
                        print("Creating JitDump files")
                        subproc_helper.run_to_completion(create_jit_dump, self, jit_dump_differences, base_dump_location, diff_dump_location)

                else:
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
                    print("{} {} -c ### {} {}".format(self.superpmi_path, " ".join(altjit_flags), self.diff_jit_path, self.mch_file))
                    print("")
                    if self.coreclr_args.diff_jit_dump:
                        print("To generate JitDump with SuperPMI use:")
                        print("")
                        for var, value in jit_dump_complus_vars.items():
                            print_platform_specific_environment_vars(self.coreclr_args, var, value)
                        print("{} {} -c ### {} {}".format(self.superpmi_path, " ".join(altjit_flags), self.diff_jit_path, self.mch_file))
                        print("")
                    print("Method numbers with binary differences:")
                    print(self.diff_mcl_contents)
                    print("")

                try:
                    current_text_diff = text_differences.get_nowait()
                except:
                    current_text_diff = None

                if current_text_diff is not None:
                    print("Textual differences found. Asm is located under %s %s" % (base_asm_location, diff_asm_location))
                    print("Generate a diff analysis report by building jit-analyze from https://github.com/dotnet/jitutils and running:")
                    print("    jit-analyze -r --base %s --diff %s" % (base_asm_location, diff_asm_location))

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

                    # Open VS Code on the diffs.

                    if self.coreclr_args.diff_with_code and not self.coreclr_args.diff_jit_dump_only:
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
                    print("No textual differences. Is this an issue with libcoredistools?")

                try:
                    current_jit_dump_diff = jit_dump_differences.get_nowait()
                except:
                    current_jit_dump_diff = None

                if current_jit_dump_diff is not None:
                    print("Textual differences found in JitDump. JitDump is located under %s %s" % (base_dump_location, diff_dump_location))

                    if self.coreclr_args.diff_with_code:
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

            if not self.coreclr_args.skip_cleanup:
                if os.path.isfile(self.fail_mcl_file):
                    os.remove(self.fail_mcl_file)
                    self.fail_mcl_file = None

        return return_code

################################################################################
# Helper Methods
################################################################################

def determine_coredis_tools(coreclr_args):
    """ Determine the coredistools location

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        coredistools_location (str)     : path of libcoredistools.dylib|so|dll

    Notes:
        If unable to find libcoredis tools, download it from azure storage.
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

    coredistools_uri = "https://clrjit.blob.core.windows.net/superpmi/libcoredistools/{}-{}/{}".format(coreclr_args.host_os.lower(), coreclr_args.arch.lower(), coredistools_dll_name)

    coredistools_location = os.path.join(coreclr_args.core_root, coredistools_dll_name)
    if os.path.isfile(coredistools_location):
        print("Using coredistools found at {}".format(coredistools_location))
    else:
        print("Download: {} -> {}".format(coredistools_uri, coredistools_location))
        urllib.request.urlretrieve(coredistools_uri, coredistools_location)

    assert os.path.isfile(coredistools_location)
    return coredistools_location

def determine_pmi_location(coreclr_args):
    """ Determine pmi location

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        pmi_location (str)     : path of pmi.dll

    Notes:
        If unable to find pmi.dll, download it from Azure storage.

        TODO: look for pmi.dll on PATH?
    """
    if coreclr_args.pmi_location is not None:
        pmi_location = os.path.abspath(coreclr_args.pmi_location)
        if not os.path.isfile(pmi_location):
            raise RuntimeError("PMI not found at {}".format(pmi_location))
        print("Using PMI at {}".format(pmi_location))
    else:
        pmi_dll_name = "pmi.dll"
        pmi_uri = "https://clrjit.blob.core.windows.net/superpmi/pmi/pmi.dll"
        pmi_location = os.path.join(coreclr_args.core_root, pmi_dll_name)
        if os.path.isfile(pmi_location):
            print("Using PMI found at {}".format(pmi_location))
        else:
            print("Download: {} -> {}".format(pmi_uri, pmi_location))
            urllib.request.urlretrieve(pmi_uri, pmi_location)

    assert os.path.isfile(pmi_location)
    return pmi_location

def determine_remote_mch_location(coreclr_args):
    """ Determine where the azure storage location for the mch files is

    Args:
        coreclr_args (CoreclrArguments): parsed_args

    Returns:
        mch_remote_uri (str):   uri for the mch files

    """

    location = "https://clrjit.blob.core.windows.net/superpmi/{}/{}/{}/".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type)

    return location

def determine_jit_name(coreclr_args):
    """ Determine the jit based on the os

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        jit_name(str) : name of the jit for this os
    """

    jit_base_name = "clrjit"
    if isinstance(coreclr_args.altjit, str):
        jit_base_name = coreclr_args.altjit
    elif coreclr_args.altjit == True:
        jit_base_name = "protononjit"

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
        superpmi_tool_name(str) : name of the jit for this OS
    """

    if coreclr_args.host_os == "OSX":
        return "superpmi"
    elif coreclr_args.host_os == "Linux":
        return "superpmi"
    elif coreclr_args.host_os == "Windows_NT":
        return "superpmi.exe"
    else:
        raise RuntimeError("Unknown OS.")

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

def list_superpmi_container_via_rest_api(coreclr_args, filter=lambda unused: True):
    """ List the superpmi using the azure storage rest api

    Args:
        filter (lambda: string): filter to apply to the list

    Notes:
        This method does not require installing the azure storage python
        package.
    """

    list_superpmi_container_uri = "https://clrjit.blob.core.windows.net/superpmi?restype=container&comp=list"

    contents = urllib.request.urlopen(list_superpmi_container_uri).read().decode('utf-8')
    urls_split = contents.split("<Url>")[1:]
    urls = []
    for item in urls_split:
        url = item.split("</Url>")[0].strip()

        if "{}/{}/{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type) in url and filter(url):
            urls.append(url)

    return urls

def download_index(coreclr_args):
    """ Download the index.json for the collection.

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Notes:
        The index.json file includes a dictionary of all of the different
        collections that were done.

        The index.json file is a simply a dictionary mapping the a name of a
        collection to the file name that will be stored on disk.

        Example:

        {
            "frameworks": "Windows_NT.x64.Checked.frameworks.mch",
            "default": "Windows_NT.x64.Checked.mch",
            "tests": "Windows_NT.x64.Checked.tests.mch"
        }
    """

    urls = list_superpmi_container_via_rest_api(coreclr_args, lambda url: "index.json" in url)

    if len(urls) == 0:
        print("Didn't find any index.json for the specified configuration.")
        sys.exit(1)
    elif len(urls) > 1:
        print("Error: found {} index.json files (expected 1).".format(len(urls)))
        sys.exit(1)

    json_string = urllib.request.urlopen(urls[0]).read().decode('utf-8')

    json_obj = json.loads(json_string)
    return json_obj

def download_mch(coreclr_args, specific_mch=None, include_baseline_jit=False):
    """ Download the mch files

    Args:
        coreclr_args (CoreclrArguments): parsed args
        specific_mch (str): Download a specific mch file
        include_baseline_jit (bool): include downloading the baseline jit

    Returns:
        index (defaultdict(lambda: None)): collection type -> name

    """

    urls = list_superpmi_container_via_rest_api(coreclr_args)
    default_mch_dir = os.path.join(coreclr_args.spmi_location, "mch", "{}.{}.{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))

    if not os.path.isdir(default_mch_dir):
        os.makedirs(default_mch_dir)

    with TempDir() as temp_location:
        for url in urls:
            temp_location_items = [os.path.join(temp_location, item) for item in os.listdir(temp_location)]
            for item in temp_location_items:
                if os.path.isdir(item):
                    shutil.rmtree(item)
                else:
                    os.remove(item)

            if "clrjit" in url and not include_baseline_jit:
                continue

            if "index.json" in url:
                continue

            if specific_mch is not None:
                if specific_mch not in url:
                    continue

            item_name = url.split("/")[-1]
            download_path = os.path.join(temp_location, item_name)

            print("Download: {} -> {}".format(url, download_path))
            urllib.request.urlretrieve(url, download_path)

            if url.endswith(".zip"):
                print ("unzip {}".format(download_path))
                with zipfile.ZipFile(download_path, "r") as file_handle:
                    file_handle.extractall(temp_location)

            print("")

            items = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if not item.endswith(".zip")]

            for item in items:
                print("Copying: {} -> {}".format(item, default_mch_dir))
                shutil.copy2(item, default_mch_dir)


def upload_mch(coreclr_args):
    """ Upload the mch files

    Args:
        coreclr_args (CoreclrArguments): parsed args
    """

    try:
        from azure.storage.blob import BlockBlobService, PublicAccess

    except:
        print("Please install:")
        print("pip install azure-storage-blob")
        print("pip install cffi")

        raise RuntimeError("Missing azure storage package.")

    block_blob_service = BlockBlobService(account_name="clrjit", account_key=coreclr_args.az_storage_key)

    container_name = "superpmi"
    json_item = defaultdict(lambda: None)

    with TempDir() as temp_location:
        for item in coreclr_args.mch_files:
            item_name = "{}/{}/{}/{}.zip".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type, os.path.basename(item))
            zip_name = os.path.join(temp_location, os.path.basename(item) + ".zip")

            print ("zip {} {}".format(zip_name, item))

            # Zip the file we will upload
            with zipfile.ZipFile(zip_name, 'w', zipfile.ZIP_DEFLATED) as zip_file:
                zip_file.write(item, os.path.basename(item))

            print("")
            print("Uploading: {} -> {}".format(item, "https://clrjit.blob.core.windows.net/superpmi/" + item_name))
            block_blob_service.create_blob_from_path(container_name, item_name, zip_name)
            print("")

            item_basename = os.path.basename(item)

            collection_name = item_basename.split(".")[3]
            if collection_name == "mch":
                collection_name = "default"

            json_item[collection_name] = os.path.basename(item)

        file_handle = tempfile.NamedTemporaryFile(delete=False, mode='w')
        try:
            json.dump(json_item, file_handle)
            file_handle.close()

            item_name = "{}/{}/{}/index.json".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type)
            print("Uploading: {} -> {}".format(file_handle.name, "https://clrjit.blob.core.windows.net/superpmi/" + item_name))
            block_blob_service.create_blob_from_path(container_name, item_name, file_handle.name)
        finally:
            os.remove(file_handle.name)

        jit_location = coreclr_args.jit_location
        if jit_location is None:
            jit_name = determine_jit_name(coreclr_args)
            jit_location = os.path.join(coreclr_args.core_root, jit_name)

        container_path = "{}/{}/{}/{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type, os.path.basename(jit_location))

        assert os.path.isfile(jit_location)
        print("Uploading: {} -> {}".format(jit_location, "https://clrjit.blob.core.windows.net/superpmi/" + os.path.basename(jit_location)))
        block_blob_service.create_blob_from_path(container_name, container_path, jit_location)

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

    def setup_mch_arg(mch_file):
        if mch_file is not None:
            return mch_file

        mch_directory = os.path.join(coreclr_args.spmi_location, "mch", "{}.{}.{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))
        mch_filename = "{}.{}.{}.mch".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type)
        default_mch_location = os.path.join(mch_directory, mch_filename)

        if os.path.isfile(default_mch_location) and not args.force_download and coreclr_args.collection == "default":
            return default_mch_location

        # Download the mch
        else:
            index = download_index(coreclr_args)

            mch_filename = index[coreclr_args.collection]
            mch_location = os.path.join(mch_directory, mch_filename)

            if not os.path.isfile(mch_location):
                download_mch(coreclr_args, specific_mch=index[coreclr_args.collection], include_baseline_jit=True)

            return mch_location

    def verify_superpmi_common_args():

        # core_root has already been verified in CoreclrArguments() initialization.

        coreclr_args.verify(args,
                            "break_on_assert",
                            lambda unused: True,
                            "Unable to set break_on_assert")

        coreclr_args.verify(args,
                            "break_on_error",
                            lambda unused: True,
                            "Unable to set break_on_error")

        coreclr_args.verify(args,
                            "spmi_location",
                            lambda unused: True,
                            "Unable to set spmi_location",
                            modify_arg=lambda spmi_location: os.path.abspath(os.path.join(coreclr_args.artifacts_location, "spmi")) if spmi_location is None else spmi_location)

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
                            "existing_temp_dir",
                            lambda unused: True,
                            "Unable to set existing_temp_dir.")

        coreclr_args.verify(args,
                            "has_run_collection_command",
                            lambda unused: True,
                            "Unable to set has_run_collection_command.")

        coreclr_args.verify(args,
                            "has_merged_mch",
                            lambda unused: True,
                            "Unable to set has_merged_mch.")

        coreclr_args.verify(args,
                            "has_verified_clean_mch",
                            lambda unused: True,
                            "Unable to set has_verified_clean_mch.")

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
            coreclr_args.has_run_collection_command = True

    elif coreclr_args.mode == "replay":

        verify_superpmi_common_args()

        coreclr_args.verify(args,
                            "collection",
                            lambda collection_name: collection_name in download_index(coreclr_args),
                            "Invalid collection. Please run 'superpmi.py list-collections' to see valid options.")

        coreclr_args.verify(args,
                            "altjit",                   # Must be set before `jit_path` (determine_jit_name() depends on it)
                            lambda unused: True,
                            "Unable to set altjit.")

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

        coreclr_args.verify(args,
                            "mch_file",
                            lambda mch_file: os.path.isfile(mch_file),
                            lambda mch_file: "Incorrect file path to mch_file: {}".format(mch_file),
                            modify_arg=lambda arg: setup_mch_arg(arg))

    elif coreclr_args.mode == "asmdiffs":

        verify_superpmi_common_args()

        coreclr_args.verify(args,
                            "base_jit_path",
                            lambda unused: True,
                            "Unable to set base_jit_path")

        coreclr_args.verify(args,
                            "altjit",                   # Must be set before `jit_path` (determine_jit_name() depends on it)
                            lambda unused: True,
                            "Unable to set altjit.")

        coreclr_args.verify(args,
                            "diff_jit_path",
                            lambda jit_path: os.path.isfile(jit_path),
                            "Unable to set diff_jit_path",
                            modify_arg=lambda arg: os.path.join(coreclr_args.core_root, determine_jit_name(coreclr_args)) if arg is None else os.path.abspath(arg))

        coreclr_args.verify(args,
                            "collection",
                            lambda collection_name: collection_name in download_index(coreclr_args),
                            "Invalid collection. Please run 'superpmi.py list-collections' to see valid options.")

        coreclr_args.verify(args,
                            "diff_with_code",
                            lambda unused: True,
                            "Unable to set diff_with_code.")

        coreclr_args.verify(args,
                            "diff_with_code_only",
                            lambda unused: True,
                            "Unable to set diff_with_code_only.")

        coreclr_args.verify(args,
                            "previous_temp_location",
                            lambda unused: True,
                            "Unable to set previous_temp_location.")

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

        coreclr_args.verify(args,
                            "mch_file",
                            lambda mch_file: os.path.isfile(mch_file),
                            lambda mch_file: "Incorrect file path to mch_file: {}".format(mch_file),
                            modify_arg=lambda arg: setup_mch_arg(arg))

    elif coreclr_args.mode == "upload":

        coreclr_args.verify(args,
                            "az_storage_key",
                            lambda item: item is not None,
                            "Unable to set az_storage_key.",
                            modify_arg=lambda arg: os.environ["CLRJIT_AZ_KEY"] if arg is None else arg)

        coreclr_args.verify(args,
                            "mch_files",
                            lambda mch_files: all(os.path.isfile(item) for item in mch_files),
                            "Unable to set mch_files.")

        coreclr_args.verify(args,
                            "jit_location",
                            lambda unused: True,
                            "Unable to set jit_location.")

        coreclr_args.verify(False,          # Force `altjit` to false. TODO: support altjit uploads?
                            "altjit",
                            lambda unused: True,
                            "Unable to set altjit.")

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

        if coreclr_args.output_mch_path != None:
            print("MCH path: {}".format(coreclr_args.output_mch_path))

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
        print("Elapsed time: {}".format(elapsed_time))

    elif coreclr_args.mode == "replay":
        # Start a new SuperPMI Replay

        begin_time = datetime.datetime.now()

        print("SuperPMI replay")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        mch_file = coreclr_args.mch_file
        jit_path = coreclr_args.jit_path

        print("")
        print("MCH Path: {}".format(mch_file))
        print("JIT Path: {}".format(jit_path))

        replay = SuperPMIReplay(coreclr_args, mch_file, jit_path)
        success = replay.replay()

        print("Finished SuperPMI replay")

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
        print("Elapsed time: {}".format(elapsed_time))

    elif coreclr_args.mode == "asmdiffs":
        # Start a new SuperPMI Replay with AsmDiffs

        begin_time = datetime.datetime.now()

        print("SuperPMI ASM diffs")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        mch_file = coreclr_args.mch_file
        base_jit_path = coreclr_args.base_jit_path
        diff_jit_path = coreclr_args.diff_jit_path

        print("")
        print("MCH Path: {}".format(mch_file))
        print("Base JIT Path: {}".format(base_jit_path))
        print("Diff JIT Path: {}".format(diff_jit_path))

        asm_diffs = SuperPMIReplayAsmDiffs(coreclr_args, mch_file, base_jit_path, diff_jit_path)
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

    elif coreclr_args.mode == "list-collections":
        index = download_index(coreclr_args)

        index_count = len(index)
        print("SuperPMI list-collections")
        print("")
        print("{} different collections".format(index_count))
        print("")

        for item, value in index.items():
            print("{} : {}".format(item, value))

        print("")
    else:
        raise NotImplementedError(coreclr_args.mode)

    return 0 if success else 1

################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
