#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
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

description = ("""Script to handle running SuperPMI Collections, and replays. In addition, this
script provides support for SuperPMI ASM diffs. Note that some of the options
provided by this script are also provided in our SuperPMI collect test.""")

superpmi_collect_help = """ Command to run SuperPMI collect over. Note that there
cannot be any dotnet cli command invoked inside this command, as they will fail due
to the shim altjit being set.
"""

superpmi_replay_help = """ Location of the mch file to run a replay over. Note
that this may either be a location to a path on disk or a uri to download the
mch file and replay it.
"""

parser = argparse.ArgumentParser(description=description)

subparsers = parser.add_subparsers(dest='mode')

# subparser for collect
collect_parser = subparsers.add_parser("collect")

# Add required arguments
collect_parser.add_argument("collection_command", nargs='?', help=superpmi_collect_help)
collect_parser.add_argument("collection_args", nargs='?', help="Arguments to pass to the SuperPMI collect command.")

collect_parser.add_argument("--break_on_assert", dest="break_on_assert", default=False, action="store_true", help="While verifying a clean superpmi collection enable break on assert.")
collect_parser.add_argument("--break_on_error", dest="break_on_error", default=False, action="store_true", help="While verifying a clean superpmi collection enable break on assert.")

collect_parser.add_argument("-log_file", dest="log_file", default=None, help="Write output to a log file")

collect_parser.add_argument("-arch", dest="arch", nargs='?', default="x64", help="Arch, default is x64") 
collect_parser.add_argument("-build_type", dest="build_type", nargs='?', default="Checked", help="Build type, Checked is default")
collect_parser.add_argument("-test_location", dest="test_location", nargs="?", default=None, help="Test location. This is optional")
collect_parser.add_argument("-pmi_assemblies", dest="pmi_assemblies", nargs="+", default=[], help="Pass a sequence of managed dlls or directories to recurisvely run pmi over while collecting.")
collect_parser.add_argument("-core_root", dest="core_root", nargs='?', default=None, help="Location of the Core_Root location. If not passed it will be deduced if possible.")
collect_parser.add_argument("-product_location", dest="product_location", nargs='?', default=None, help="Location of the built product, this is optional.")
collect_parser.add_argument("-coreclr_repo_location", dest="coreclr_repo_location", default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))), help="Location of the coreclr repo. Optional.")
collect_parser.add_argument("-test_env", dest="test_env", default=None, help="Test env to pass to the coreclr tests if collecting over the tests.")
collect_parser.add_argument("-output_mch_path", dest="output_mch_path", default=None, help="Location to drop the final mch file. By default it will drop to bin/mch/$(buildType).$(arch).$(config)/$(buildType).$(arch).$(config).mch")

collect_parser.add_argument("--pmi", dest="pmi", default=False, action="store_true", help="Use pmi on a set of directories or assemblies")
collect_parser.add_argument("-mch_files", dest="mch_files", nargs='+', default=None, help="Pass a sequence of mch files which will be merged.")
collect_parser.add_argument("--merge_mch_files", dest="merge_mch_files", default=False, action="store_true", help="Merge multiple mch files. Please use the mch_files flag to pass a list of mch files to merge.")

collect_parser.add_argument("--use_zapdisable", dest="use_zapdisable", default=False, action="store_true", help="Allow redundant calls to the systems libraries for more coverage.")

collect_parser.add_argument("--assume_unclean_mch", dest="assume_unclean_mch", default=False, action="store_true", help="Force clean the mch file. This is useful if the dataset is large and there are expected dups.")

# Allow for continuing a collection in progress
collect_parser.add_argument("-existing_temp_dir", dest="existing_temp_dir", default=None, nargs="?")
collect_parser.add_argument("--has_run_collection_command", dest="has_run_collection_command", default=False, action="store_true")
collect_parser.add_argument("--has_merged_mch", dest="has_merged_mch", default=False, action="store_true")
collect_parser.add_argument("--has_verified_clean_mch", dest="has_verified_clean_mch", default=False, action="store_true")

collect_parser.add_argument("--skip_collect_mc_files", dest="skip_collect_mc_files", default=False, action="store_true")
collect_parser.add_argument("--skip_cleanup", dest="skip_cleanup", default=False, action="store_true")

# subparser for replay
replay_parser = subparsers.add_parser("replay")

# Add required arguments
replay_parser.add_argument("collection", nargs='?', default="default", help="Which collection type to run. Default is to run everything. Use superpmi list to find potential collections")

replay_parser.add_argument("-jit_path", nargs='?', help="Path to clrjit. defaults to core_root jit.")
replay_parser.add_argument("-mch_file", nargs=1, help=superpmi_replay_help)
replay_parser.add_argument("-log_file", dest="log_file", default=None)

replay_parser.add_argument("--break_on_assert", dest="break_on_assert", default=False, action="store_true")
replay_parser.add_argument("--break_on_error", dest="break_on_error", default=False, action="store_true")

replay_parser.add_argument("-arch", dest="arch", nargs='?', default="x64")
replay_parser.add_argument("-build_type", dest="build_type", nargs='?', default="Checked")
replay_parser.add_argument("-test_location", dest="test_location", nargs="?", default=None)
replay_parser.add_argument("-core_root", dest="core_root", nargs='?', default=None)
replay_parser.add_argument("-product_location", dest="product_location", nargs='?', default=None)
replay_parser.add_argument("-coreclr_repo_location", dest="coreclr_repo_location", default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
replay_parser.add_argument("-test_env", dest="test_env", default=None)

replay_parser.add_argument("--skip_cleanup", dest="skip_cleanup", default=False, action="store_true")
replay_parser.add_argument("--force_download", dest="force_download", default=False, action="store_true")

# subparser for asmDiffs
asm_diff_parser = subparsers.add_parser("asmdiffs")

# Add required arguments
asm_diff_parser.add_argument("base_jit_path", nargs=1, help="Path to baseline clrjit.")
asm_diff_parser.add_argument("diff_jit_path", nargs=1, help="Path to diff clrjit.")
asm_diff_parser.add_argument("collection", nargs='?', default="default", help="Which collection type to run. Default is to run everything. Use superpmi list to find potential collections")


asm_diff_parser.add_argument("-mch_file", nargs=1, help=superpmi_replay_help)

asm_diff_parser.add_argument("-log_file", dest="log_file", default=None)
asm_diff_parser.add_argument("--break_on_assert", dest="break_on_assert", default=False, action="store_true")
asm_diff_parser.add_argument("--break_on_error", dest="break_on_error", default=False, action="store_true")

asm_diff_parser.add_argument("-arch", dest="arch", nargs='?', default="x64")
asm_diff_parser.add_argument("-build_type", dest="build_type", nargs='?', default="Checked")
asm_diff_parser.add_argument("-test_location", dest="test_location", nargs="?", default=None)
asm_diff_parser.add_argument("-core_root", dest="core_root", nargs='?', default=None)
asm_diff_parser.add_argument("-product_location", dest="product_location", nargs='?', default=None)
asm_diff_parser.add_argument("-coreclr_repo_location", dest="coreclr_repo_location", default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
asm_diff_parser.add_argument("-test_env", dest="test_env", default=None)

asm_diff_parser.add_argument("--skip_cleanup", dest="skip_cleanup", default=False, action="store_true")
asm_diff_parser.add_argument("--force_download", dest="force_download", default=False, action="store_true")

asm_diff_parser.add_argument("--diff_with_code", dest="diff_with_code", default=False, action="store_true")
asm_diff_parser.add_argument("--diff_with_code_only", dest="diff_with_code_only", default=False, action="store_true", help="Only run the diff command, do not run SuperPMI to regenerate diffs.")

asm_diff_parser.add_argument("--diff_jit_dump", dest="diff_jit_dump", default=False, action="store_true")
asm_diff_parser.add_argument("--diff_jit_dump_only", dest="diff_jit_dump_only", default=False, action="store_true", help="Only diff jitdumps, not asm.")

# subparser for upload
upload_parser = subparsers.add_parser("upload")

asm_diff_parser.add_argument("az_storage_key", nargs='?', help="Key for the clrjit az storage location.")

upload_parser.add_argument("-mch_files", nargs='+', help="mch files to pass")
upload_parser.add_argument("-jit_location", nargs=1, default=None, help="Location for the base clrjit. If not passed this will be assumed to be from the core root.")

upload_parser.add_argument("-arch", dest="arch", nargs='?', default="x64")
upload_parser.add_argument("-build_type", dest="build_type", nargs='?', default="Checked")
upload_parser.add_argument("-coreclr_repo_location", dest="coreclr_repo_location", default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

upload_parser.add_argument("--skip_cleanup", dest="skip_cleanup", default=False, action="store_true")

# subparser for list-collections
list_parser = subparsers.add_parser("list-collections")

list_parser.add_argument("-arch", dest="arch", nargs='?', default="x64")
list_parser.add_argument("-build_type", dest="build_type", nargs='?', default="Checked")
list_parser.add_argument("-coreclr_repo_location", dest="coreclr_repo_location", default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

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
        The object is responsible for setting up a super pmi collection given
        the arguments passed into the script.
    """

    def __init__(self, args):
        """ Constructor

        Args:
            args (CoreclrArguments): parsed args

        """

        if args.host_os == "OSX":
            self.standalone_jit_name = "libclrjit.dylib"
            self.collection_shim_name = "libsuperpmi-shim-collector.dylib"
            self.superpmi_tool_name = "superpmi"
            self.mcs_tool_name = "mcs"
        elif args.host_os == "Linux":
            self.standalone_jit_name = "libclrjit.so"
            self.collection_shim_name = "libsuperpmi-shim-collector.so"
            self.superpmi_tool_name = "superpmi"
            self.mcs_tool_name = "mcs"
        elif args.host_os == "Windows_NT":
            self.standalone_jit_name = "clrjit.dll"
            self.collection_shim_name = "superpmi-shim-collector.dll"
            self.superpmi_tool_name = "superpmi.exe"
            self.mcs_tool_name = "mcs.exe"
        else:
            raise RuntimeError("Unsupported OS.")

        self.jit_path = os.path.join(args.core_root, self.standalone_jit_name)
        self.superpmi_path = os.path.join(args.core_root, self.superpmi_tool_name)
        self.mcs_path = os.path.join(args.core_root, self.mcs_tool_name)

        self.coreclr_args = args

        self.core_root = self.coreclr_args.core_root

        self.command = self.coreclr_args.collection_command
        self.args = self.coreclr_args.collection_args

        if self.coreclr_args.pmi:
            self.pmi_location = determine_pmi_location(self.coreclr_args)
            self.pmi_assemblies = self.coreclr_args.pmi_assemblies
            self.corerun = os.path.join(self.core_root, "corerun" if self.coreclr_args.host_os != "Windows_NT" else "corerun.exe")

    ############################################################################
    # Instance Methods
    ############################################################################

    def collect(self):
        """ Do the SuperPMI Collection.
        """

        # Pathname for a temporary .MCL file used for noticing SuperPMI replay failures against base MCH.
        self.base_fail_mcl_file = None

        # Pathname for a temporary .MCL file used for noticing SuperPMI replay failures against final MCH.
        self.final_fail_mcl_file = None

        # The base .MCH file path
        self.base_mch_file = None

        # Clean .MCH file path
        self.clean_mch_file = None

        # Final .MCH file path
        self.final_mch_file = None

        # The .TOC file path for the clean thin unique .MCH file
        self.toc_file = None

        self.save_the_final_mch_file = False

        # Do a basic SuperPMI collect and validation:
        #   1. Collect MC files by running a set of sample apps.
        #   2. Merge the MC files into a single MCH using "mcs -merge *.mc -recursive".
        #   3. Create a clean MCH by running SuperPMI over the MCH, and using "mcs -strip" to filter
        #       out any failures (if any).
        #    4. Create a thin unique MCH by using "mcs -removeDup -thin".
        #    5. Create a TOC using "mcs -toc".
        #    6. Verify the resulting MCH file is error-free when running SuperPMI against it with the
        #       same JIT used for collection.
        #
        #    MCH files are big. If we don't need them anymore, clean them up right away to avoid
        #    running out of disk space in disk constrained situations.

        passed = False

        try:
            with TempDir(self.coreclr_args.existing_temp_dir) as temp_location:
                # Setup all of the temp locations
                self.base_fail_mcl_file = os.path.join(temp_location, "basefail.mcl")
                self.final_fail_mcl_file = os.path.join(temp_location, "finalfail.mcl")
                
                self.base_mch_file = os.path.join(temp_location, "base.mch")
                self.clean_mch_file = os.path.join(temp_location, "clean.mch")

                self.temp_location = temp_location

                if self.coreclr_args.output_mch_path is not None:
                    self.save_the_final_mch_file = True
                    self.final_mch_file = os.path.abspath(self.coreclr_args.output_mch_path)
                    self.toc_file = self.final_mch_file + ".mct"
                else:
                    self.save_the_final_mch_file = True

                    default_coreclr_bin_mch_location = self.coreclr_args.default_coreclr_bin_mch_location

                    self.final_mch_file = os.path.join(default_coreclr_bin_mch_location, "{}.{}.{}.mch".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))
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
                    self.__create_thin_unique_mch__()
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
            env_copy["COMPlus_AltJitName"] = self.collection_shim_name

            if self.coreclr_args.use_zapdisable:
                env_copy["COMPlus_ZapDisable"] = "1"

            print("Starting collection.")
            print("")
            print_platform_specific_environment_vars(self.coreclr_args, "SuperPMIShimLogPath", self.temp_location)
            print_platform_specific_environment_vars(self.coreclr_args, "SuperPMIShimPath", self.jit_path)
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJit", "*")
            print_platform_specific_environment_vars(self.coreclr_args, "COMPlus_AltJitName", self.collection_shim_name)
            print("")

            if self.command != None:
                print("%s %s" % (self.command, " ".join(self.args)))

                assert isinstance(self.command, str)
                assert isinstance(self.args, list)

                return_code = 1

                self.command = [self.command,] + self.args
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
                    print("{}{}".format(print_prefix, " ".join(command)))
                    
                    proc = await asyncio.create_subprocess_shell(
                        " ".join(command),
                        stdout=asyncio.subprocess.PIPE,
                        stderr=asyncio.subprocess.PIPE)

                    await proc.communicate()

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

        contents = os.listdir(self.temp_location)
        mc_contents = [os.path.join(self.temp_location, item) for item in contents if ".mc" in item]

        if len(mc_contents) == 0:
            raise RuntimeError("No .mc files generated.")

        self.mc_contents = mc_contents

    def __merge_mc_files__(self):
        """ Merge the mc files that were generated

        Notes:
            mcs -merge <s_baseMchFile> <s_tempDir>\*.mc -recursive

        """

        pattern = os.path.join(self.temp_location, "*.mc")

        command = [self.mcs_path, "-merge", self.base_mch_file, pattern, "-recursive"]
        print("Invoking: " + " ".join(command))
        proc = subprocess.Popen(command)

        proc.communicate()

        if not os.path.isfile(self.mcs_path):
            raise RuntimeError("mch file failed to be generated at: %s" % self.mcs_path)

        contents = os.listdir(self.temp_location)
        mc_contents = [os.path.join(self.temp_location, item) for item in contents if ".mc" in item and not ".mch" in item]

        # All the individual MC files are no longer necessary, now that we have
        # merged them into the base.mch. Delete them.
        if not self.coreclr_args.skip_cleanup:
            for item in mc_contents:
                os.remove(item)

    def __merge_mch_files__(self):
        """ Merge the mch files that were passed

        Notes:
            mcs -concat <s_baseMchFile> [self.coreclr_args.mch_files]

        """

        mch_files = self.coreclr_args.mch_files

        for item in mch_files:
            command = [self.mcs_path, "-concat", self.base_mch_file, item]
            print("Invoking: " + " ".join(command))
            proc = subprocess.Popen(command)

            proc.communicate()

        if not os.path.isfile(self.mcs_path):
            raise RuntimeError("mch file failed to be generated at: %s" % self.mcs_path)
    
    def __create_clean_mch_file__(self):
        """ Create a clean mch file based on the original

        Notes:
            <SuperPMIPath> -p -f <s_baseFailMclFile> <s_baseMchFile> <jitPath>

            if <s_baseFailMclFile> is non-empty:
                <mcl> -strip <s_baseFailMclFile> <s_baseMchFile> <s_cleanMchFile>
            else
                # no need to copy, just change the names
                clean_mch_file = base_mch_file
            del <s_baseFailMclFile>
        """

        command = [self.superpmi_path, "-p", "-f", self.base_fail_mcl_file, self.base_mch_file, self.jit_path]
        print (" ".join(command))
        proc = subprocess.Popen(command)

        proc.communicate()

        if os.path.isfile(self.base_fail_mcl_file) and os.stat(self.base_fail_mcl_file).st_size != 0:
            command = [self.mcs_path, "-strip", self.base_fail_mcl_file, self.base_mch_file, self.clean_mch_file]
            print (" ".join(command))
            proc = subprocess.Popen(command)

            proc.communicate()
        else:
            self.clean_mch_file = self.base_mch_file
            self.base_mch_file = None

        if not os.path.isfile(self.clean_mch_file):
            raise RuntimeError("Clean mch file failed to be generated.")

        if not self.coreclr_args.skip_cleanup:
            if os.path.isfile(self.base_fail_mcl_file):
                os.remove(self.base_fail_mcl_file)
                self.base_fail_mcl_file = None

    def __create_thin_unique_mch__(self):
        """  Create a thin unique MCH
        
        Notes:
            <mcl> -removeDup -thin <s_cleanMchFile> <s_finalMchFile>
        """

        command = [self.mcs_path, "-removeDup", "-thin", self.clean_mch_file, self.final_mch_file]
        proc = subprocess.Popen(command)
        proc.communicate()

        if not os.path.isfile(self.final_mch_file):
            raise RuntimeError("Error, final mch file not created correctly.")

        if not self.coreclr_args.skip_cleanup:
            os.remove(self.clean_mch_file)
            self.clean_mch_file = None
        
    def __create_toc__(self):
        """ Create a TOC file
        
        Notes:
            <mcl> -toc <s_finalMchFile>
        """

        command = [self.mcs_path, "-toc", self.final_mch_file]
        proc = subprocess.Popen(command)
        proc.communicate()

        if not os.path.isfile(self.toc_file):
            raise RuntimeError("Error, toc file not created correctly.")

    def __verify_final_mch__(self):
        """ Verify the resulting MCH file is error-free when running SuperPMI against it with the same JIT used for collection.
        
        Notes:
            <superPmiPath> -p -f <s_finalFailMclFile> <s_finalMchFile> <jitPath>
        """

        spmi_replay = SuperPMIReplay(self.coreclr_args, self.final_mch_file, self.jit_path)
        passed = spmi_replay.replay()

        if not passed:
            raise RuntimeError("Error unclean replay.")

################################################################################
# SuperPMI Replay
################################################################################

class SuperPMIReplay:
    """ SuperPMI Replay class

    Notes:
        The object is responsible for replaying the mch final given to the
        instance of the class
    """

    def __init__(self, coreclr_args, mch_file, jit_path):
        """ Constructor

        Args:
            args (CoreclrArguments) : parsed args
            mch_file (str)          : final mch file from the collection
            jit_path (str)          : path to clrjit/libclrjit

        """

        self.jit_path = jit_path
        self.mch_file = mch_file
        self.superpmi_path = os.path.join(coreclr_args.core_root, "superpmi")

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
            print("Starting SuperPMI replay.")
            print("")
            print("Temp Location: {}".format(temp_location))
            print("")

            self.fail_mcl_file = os.path.join(temp_location, "fail.mcl")

            # TODO: add aljit support
            #
            # Set: -jitoption force AltJit=* -jitoption force AltJitNgen=*
            force_altjit_options = [
                "-jitoption",
                "force",
                "AltJit=",
                "-jitoption",
                "force",
                "AltJitNgen="
            ]

            flags = [
                "-p", # Parallel
                "-f", # Failing mc List
                self.fail_mcl_file,
                "-r", # Repro name, create .mc repro files
                os.path.join(temp_location, "repro")
            ]

            flags += force_altjit_options

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
                print("Clean SuperPMI Replay")
                return_code = True

            if os.path.isfile(self.fail_mcl_file) and os.stat(self.fail_mcl_file).st_size != 0:
                # Unclean replay.
                #
                # Save the contents of the fail.mcl file to dig into failures.

                assert(return_code != 0)

                if return_code == -1:
                    print("General fatal error.")
                elif return_code == -2:
                    print("Jit failed to initialize.")
                elif return_code == 1:
                    print("Complition failures.")
                else:
                    print("Unknown error code.")

                self.fail_mcl_contents = None
                with open(self.fail_mcl_file) as file_handle:
                    self.fail_mcl_contents = file_handle.read()

                # If there are any .mc files, drop them into bin/repro/<host_os>.<arch>.<build_type>/*.mc
                mc_files = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if item.endswith(".mc")]

                if len(mc_files) > 0:
                    repro_location = os.path.join(self.coreclr_args.coreclr_repo_location, "bin", "repro", "{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))

                    # Delete existing repro location
                    if os.path.isdir(repro_location):
                        shutil.rmtree(repro_location)

                    assert(not os.path.isdir(repro_location))

                    os.makedirs(repro_location)
                    
                    repro_files = []
                    for item in mc_files:
                        repro_files.append(os.path.join(repro_location, os.path.basename(item)))
                        shutil.copy2(item, repro_location)

                    print("")
                    print("Repro .mc files:")
                    print("")

                    for item in repro_files:
                        print(item)
                    
                    print("")

                    print("To run an specific failure:")
                    print("")
                    print("<SuperPMI_path>/SuperPMI <core_root|product_dir>/clrjit.dll|libclrjit.so|libclrjit.dylib <<coreclr_path>/bin/repro/<host_os>.<arch>.<build_type>/1xxxx.mc")
                    print("")

                else:
                    print(self.fail_mcl_contents)

            if not self.coreclr_args.skip_cleanup:
                if os.path.isfile(self.fail_mcl_file):
                    os.remove(self.fail_mcl_file)
                    self.fail_mcl_file = None

                return_code == False

        return return_code

################################################################################
# SuperPMI Replay/AsmDiffs
################################################################################

class SuperPMIReplayAsmDiffs:
    """ SuperPMI Replay AsmDiffs class

    Notes:
        The object is responsible for replaying the mch final given to the
        instance of the class and doing diffs on the two passed jits.
    """

    def __init__(self, coreclr_args, mch_file, base_jit_path, diff_jit_path):
        """ Constructor

        Args:
            args (CoreclrArguments) : parsed args
            mch_file (str)          : final mch file from the collection
            base_jit_path (str)     : path to clrjit/libclrjit
            diff_jit_path (str)     : path to clrjit/libclrjit

        """

        self.base_jit_path = base_jit_path
        self.diff_jit_path = diff_jit_path

        self.mch_file = mch_file
        self.superpmi_path = os.path.join(coreclr_args.core_root, "superpmi")

        self.coreclr_args = coreclr_args

    ############################################################################
    # Instance Methods
    ############################################################################

    def replay_with_asm_diffs(self, previous_temp_location=None):
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

        with TempDir(previous_temp_location) as temp_location:
            print("Starting SuperPMI AsmDiffs.")
            print("")
            print("Temp Location: {}".format(temp_location))
            print("")

            self.fail_mcl_file = os.path.join(temp_location, "fail.mcl")
            self.diff_mcl_file = os.path.join(temp_location, "diff.mcl")

            if previous_temp_location is None:

                # TODO: add aljit support
                #
                # Set: -jitoption force AltJit=* -jitoption force AltJitNgen=*
                force_altjit_options = [
                    "-jitoption",
                    "force",
                    "AltJit=",
                    "-jitoption",
                    "force",
                    "AltJitNgen=",
                    "-jit2option",
                    "force",
                    "AltJit=",
                    "-jit2option",
                    "force",
                    "AltJitNgen="
                ]

                flags = [
                    "-a", # Asm diffs
                    "-p", # Parallel
                    "-f", # Failing mc List
                    self.fail_mcl_file,
                    "-diffMCList", # Create all of the diffs in an mcl file
                    self.diff_mcl_file,
                    "-r", # Repro name, create .mc repro files
                    os.path.join(temp_location, "repro")
                ]

                flags += force_altjit_options

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
                        print("Clean SuperPMI Replay")

                else:
                    return_code = 2
            else:
                return_code = 1;

            if os.path.isfile(self.fail_mcl_file) and os.stat(self.fail_mcl_file).st_size != 0:
                # Unclean replay.
                #
                # Save the contents of the fail.mcl file to dig into failures.

                assert(return_code != 0)

                if return_code == -1:
                    print("General fatal error.")
                elif return_code == -2:
                    print("Jit failed to initialize.")
                elif return_code == 1:
                    print("Complition failures.")
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

                # If there are any .mc files, drop them into bin/repro/<host_os>.<arch>.<build_type>/*.mc
                mc_files = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if item.endswith(".mc")]

                if len(mc_files) > 0:
                    repro_location = os.path.join(self.coreclr_args.coreclr_repo_location, "bin", "repro", "{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type))

                    # Delete existing repro location
                    if os.path.isdir(repro_location):
                        shutil.rmtree(repro_location)

                    assert(not os.path.isdir(repro_location))

                    os.makedirs(repro_location)
                    
                    repro_files = []
                    for item in mc_files:
                        repro_files.append(os.path.join(repro_location, os.path.basename(item)))
                        shutil.copy2(item, repro_location)

                    print("")
                    print("Repro .mc files:")
                    print("")

                    for item in repro_files:
                        print(item)
                    
                    print("")

                    print("To run an specific failure:")
                    print("")
                    print("<SuperPMI_path>/SuperPMI <core_root|product_dir>/clrjit.dll|libclrjit.so|libclrjit.dylib <<coreclr_path>/bin/repro/<host_os>.<arch>.<build_type>/1xxxx.mc")
                    print("")

                print(self.fail_mcl_contents)

            # There were diffs. Go through each method that created diffs and
            # create a base/diff asm file with diffable asm. In addition, create
            # a standalone .mc for easy iteration.
            if os.path.isfile(self.diff_mcl_file) and os.stat(self.diff_mcl_file).st_size != 0 or self.coreclr_args.diff_with_code_only:
                # AsmDiffs.
                #
                # Save the contents of the fail.mcl file to dig into failures.
                
                assert(return_code != 0)

                if return_code == -1:
                    print("General fatal error.")
                elif return_code == -2:
                    print("Jit failed to initialize.")
                elif return_code == 1:
                    print("Complition failures.")
                elif return_code == 139 and self.coreclr_args != "Windows_NT":
                    print("Fatal error, SuperPMI has returned SIG_SEV (segmentation fault).")
                else:
                    print("Unknown error code.")

                if not self.coreclr_args.diff_with_code_only:
                    self.diff_mcl_contents = None
                    with open(self.diff_mcl_file) as file_handle:
                        mcl_lines = file_handle.readlines()
                        mcl_lines = [item.strip() for item in mcl_lines]
                        self.diff_mcl_contents = mcl_lines

                bin_asm_location = os.path.join(self.coreclr_args.bin_location, "asm", "asm")

                count = 0
                while os.path.isdir(bin_asm_location):
                    new_bin_asm_location = os.path.join(self.coreclr_args.bin_location, "asm", "asm" + str(count))
                    
                    count += 1

                    print("{} location exists. Attempting to create: {}".format(bin_asm_location, new_bin_asm_location))
                    bin_asm_location = new_bin_asm_location
                    

                base_asm_location = os.path.join(bin_asm_location, "base")
                diff_asm_location = os.path.join(bin_asm_location, "diff")

                bin_dump_location = os.path.join(self.coreclr_args.bin_location, "jit_dump", "jit_dump")

                count = 0
                while os.path.isdir(bin_dump_location):
                    new_base_dump_location = os.path.join(self.coreclr_args.bin_location, "jit_dump", "jit_dump" + str(count))
                    
                    count += 1

                    print("{} location exists. Attempting to create: {}".format(bin_dump_location, new_base_dump_location))
                    bin_dump_location = new_base_dump_location

                base_dump_location = os.path.join(bin_dump_location, "base")
                diff_dump_location = os.path.join(bin_dump_location, "diff")

                if not self.coreclr_args.diff_with_code_only:
                    # Delete the old asm.
                    
                    # Create a diff and baseline directory
                    if os.path.isdir(base_asm_location):
                        shutil.rmtree(base_asm_location)
                    if os.path.isdir(diff_asm_location):
                        shutil.rmtree(diff_asm_location)

                    os.makedirs(base_asm_location)
                    os.makedirs(diff_asm_location)

                    assert(os.path.isdir(base_asm_location))
                    assert(os.path.isdir(diff_asm_location))

                    assert(len(os.listdir(base_asm_location)) == 0)
                    assert(len(os.listdir(diff_asm_location)) == 0)

                    if self.coreclr_args.diff_jit_dump:
                        # Create a diff and baseline directory for jit_dumps
                        if os.path.isdir(base_dump_location):
                            shutil.rmtree(base_dump_location)
                        if os.path.isdir(diff_dump_location):
                            shutil.rmtree(diff_dump_location)

                        os.makedirs(base_dump_location)
                        os.makedirs(diff_dump_location)

                        assert(os.path.isdir(base_dump_location))
                        assert(os.path.isdir(diff_dump_location))

                        assert(len(os.listdir(base_dump_location)) == 0)
                        assert(len(os.listdir(diff_dump_location)) == 0)

                text_differences = asyncio.Queue()
                jit_dump_differences = asyncio.Queue()

                async def create_asm(print_prefix, item, self, text_differences, base_asm_location, diff_asm_location):
                    """ Run superpmi over an mc to create dasm for the method.
                    """
                    # Setup to call SuperPMI for both the diff jit and the base
                    # jit

                    # TODO: add aljit support
                    #
                    # Set: -jitoption force AltJit=* -jitoption force AltJitNgen=*

                    force_altjit_options = [
                        "-jitoption",
                        "force",
                        "AltJit=",
                        "-jitoption",
                        "force",
                        "AltJitNgen="
                    ]

                    flags = [
                        "-c",
                        item,
                        "-v",
                        "q" # only log from the jit.
                    ]

                    flags += force_altjit_options
                    
                    asm_env = os.environ.copy()
                    asm_env["COMPlus_JitDisasm"] = "*"
                    asm_env["COMPlus_JitUnwindDump"] = "*"
                    asm_env["COMPlus_JitEHDump"] = "*"
                    asm_env["COMPlus_JitDiffableDasm"] = "1"
                    asm_env["COMPlus_NgenDisasm"] = "*"
                    asm_env["COMPlus_NgenDump"] = "*"
                    asm_env["COMPlus_NgenUnwindDump"] = "*"
                    asm_env["COMPlus_NgenEHDump"] = "*"
                    asm_env["COMPlus_JitEnableNoWayAssert"] = "1"
                    asm_env["COMPlus_JitNoForceFallback"] = "1"
                    asm_env["COMPlus_JitRequired"] = "1"
                    asm_env["COMPlus_TieredCompilation"] = "0"

                    # Change the working directory to the core root we will call SuperPMI from.
                    # This is done to allow libcorcedistools to be loaded correctly on unix
                    # as the loadlibrary path will be relative to the current directory.
                    with ChangeDir(self.coreclr_args.core_root) as dir:
                        command = [self.superpmi_path] + flags + [self.base_jit_path, self.mch_file]

                        # Generate diff and base asm
                        base_txt = None
                        diff_txt = None

                        with open(os.path.join(base_asm_location, "{}.dasm".format(item)), 'w') as file_handle:
                            os.environ.update(asm_env)

                            print("{}Invoking: {}".format(print_prefix, " ".join(command)))
                            
                            proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                            await proc.communicate()

                        with open(os.path.join(base_asm_location, "{}.dasm".format(item)), 'r') as file_handle:
                            base_txt = file_handle.read()

                        command = [self.superpmi_path] + flags + [self.diff_jit_path, self.mch_file]

                        with open(os.path.join(diff_asm_location, "{}.dasm".format(item)), 'w') as file_handle:
                            os.environ.update(asm_env)

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
                    # Setup to call SuperPMI for both the diff jit and the base
                    # jit

                    # TODO: add aljit support
                    #
                    # Set: -jitoption force AltJit=* -jitoption force AltJitNgen=*

                    force_altjit_options = [
                        "-jitoption",
                        "force",
                        "AltJit=",
                        "-jitoption",
                        "force",
                        "AltJitNgen="
                    ]

                    flags = [
                        "-c",
                        item,
                        "-v",
                        "q" # only log from the jit.
                    ]

                    flags += force_altjit_options
                    
                    jit_dump_env = os.environ.copy()
                    jit_dump_env["COMPlus_JitEnableNoWayAssert"] = "1"
                    jit_dump_env["COMPlus_JitNoForceFallback"] = "1"
                    jit_dump_env["COMPlus_JitRequired"] = "1"
                    jit_dump_env["COMPlus_JitDump"] = "*"
                    
                    # Generate jit dumps
                    base_txt = None
                    diff_txt = None

                    # Change the working directory to the core root we will call SuperPMI from.
                    # This is done to allow libcorcedistools to be loaded correctly on unix
                    # as the loadlibrary path will be relative to the current directory.
                    with ChangeDir(self.coreclr_args.core_root) as dir:

                        command = [self.superpmi_path] + flags + [self.base_jit_path, self.mch_file]

                        with open(os.path.join(base_dump_location, "{}.txt".format(item)), 'w') as file_handle:
                            os.environ.update(jit_dump_env)

                            print("{}Invoking: ".format(print_prefix) + " ".join(command))
                            proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=file_handle, stderr=asyncio.subprocess.PIPE)
                            
                            await proc.communicate()

                        with open(os.path.join(base_dump_location, "{}.txt".format(item)), 'r') as file_handle:
                            base_txt = file_handle.read()

                        command = [self.superpmi_path] + flags + [self.diff_jit_path, self.mch_file]

                        with open(os.path.join(diff_dump_location, "{}.txt".format(item)), 'w') as file_handle:
                            os.environ.update(jit_dump_env)

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

                    subproc_helper = AsyncSubprocessHelper(diff_items, verbose=True)
                    subproc_helper.run_to_completion(create_asm, self, text_differences, base_asm_location, diff_asm_location)

                    if self.coreclr_args.diff_jit_dump:
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
                    print("Differences found, to replay SuperPMI use <path_to_SuperPMI> -jitoption force AltJit= -jitoption force AltJitNgen= -c ### <path_to_jit> <path_to_mcl>")
                    print("")
                    print("Binary differences found with superpmi -a")
                    print("")
                    print("Method numbers with binary differences:")
                    print(self.diff_mcl_contents)
                    print("")

                try:
                    current_text_diff = text_differences.get_nowait()
                except:
                    current_text_diff = None

                if current_text_diff is not None:
                    print("Textual differences found, the asm is located under %s and %s" % (base_asm_location, diff_asm_location))
                    print("")
                    print("Method numbers with textual differences:")

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
                    print("Diffs found in the JitDump generated. These files are located under <coreclr_dir>/bin/jit_dump/base and <coreclr_dir>/bin/jit_dump/diff")
                    print("")
                    print("Method numbers with textual differences:")

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
    if not os.path.isfile(coredistools_location):
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
        If unable to find pmi tools, download it from azure storage.
    """
    pmi_dll_name = "pmi.dll"
    pmi_uri = "https://clrjit.blob.core.windows.net/superpmi/pmi/pmi.dll"

    pmi_location = os.path.join(coreclr_args.core_root, pmi_dll_name)
    if not os.path.isfile(pmi_location):
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

    if coreclr_args.host_os == "OSX":
        return "libclrjit.dylib"
    elif coreclr_args.host_os == "Linux":
        return "libclrjit.so"
    elif coreclr_args.host_os == "Windows_NT":
        return "clrjit.dll"
    else:
        raise RuntimeError("Unknown os.")

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

    assert(len(urls) == 1)
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
    default_mch_dir = os.path.join(coreclr_args.bin_location, "mch", "{}.{}.{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))

    if not os.path.isdir(default_mch_dir):
        os.makedirs(default_mch_dir)

    with TempDir() as temp_location:
        for url in urls:
            temp_location_jtems = [os.path.join(temp_location, item) for item in os.listdir(temp_location)]
            for item in temp_location_jtems:
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
    coreclr_args = CoreclrArguments(args, require_built_core_root=True, require_built_product_dir=False, require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "skip_cleanup",
                        lambda unused: True,
                        "Unable to set skip_cleanup")

    coreclr_args.verify(args,
                        "mode",
                        lambda mode: mode in ["collect", "replay", "asmdiffs", "upload", "list-collections"],
                        'Incorrect mode passed, please choose from ["collect", "replay", "asmdiffs", "upload", "list-collections"]')

    default_coreclr_bin_mch_location = os.path.join(coreclr_args.bin_location, "mch", "{}.{}.{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))

    def setup_mch_arg(arg):
        default_mch_location = os.path.join(coreclr_args.bin_location, "mch", "{}.{}.{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type), "{}.{}.{}.mch".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))

        if os.path.isfile(default_mch_location) and not args.force_download and coreclr_args.collection is "default":
            return default_mch_location

        # Download the mch
        else:
            index = download_index(coreclr_args)
            
            mch_location = os.path.join(coreclr_args.bin_location, "mch", "{}.{}.{}".format(coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type), index[coreclr_args.collection])

            if not os.path.isfile(mch_location):
                download_mch(coreclr_args, specific_mch=index[coreclr_args.collection], include_baseline_jit=True)

            return mch_location

    if not os.path.isdir(default_coreclr_bin_mch_location):
        os.makedirs(default_coreclr_bin_mch_location)

    coreclr_args.verify(default_coreclr_bin_mch_location,
                        "default_coreclr_bin_mch_location",
                        lambda unused: True,
                        "Error setting default_coreclr_bin_mch_location")

    if coreclr_args.mode == "collect":
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
                            "output_mch_path",
                            lambda unused: True,
                            "Unable to set output_mch_path")
        
        coreclr_args.verify(args,
                            "log_file",
                            lambda unused: True,
                            "Unable to set log_file.")

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
                            "assume_unclean_mch",
                            lambda unused: True,
                            "Unable to set assume_unclean_mch.")

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
                            "break_on_assert",
                            lambda unused: True,
                            "Unable to set break_on_assert")

        coreclr_args.verify(args,
                            "break_on_error",
                            lambda unused: True,
                            "Unable to set break_on_error")

        coreclr_args.verify(args,
                            "use_zapdisable",
                            lambda unused: True,
                            "Unable to set use_zapdisable")

        jit_location = os.path.join(coreclr_args.core_root, determine_jit_name(coreclr_args))
        assert(os.path.isfile(jit_location))

        if args.collection_command is None and args.merge_mch_files is not True:
            assert args.collection_args is None

            assert args.pmi is True
            assert len(args.pmi_assemblies) > 0

        if coreclr_args.merge_mch_files:
            assert len(coreclr_args.mch_files) > 0
            coreclr_args.has_run_collection_command = True
    
    elif coreclr_args.mode == "replay":
        coreclr_args.verify(args,
                            "collection",
                            lambda collection_name: collection_name in download_index(coreclr_args),
                            "Invalid collection. Please run superpmi.py list-collections to see valid options.")

        coreclr_args.verify(args,
                            "mch_file",
                            lambda mch_file: os.path.isfile(mch_file),
                            lambda mch_file: "Incorrect file path to mch_file: {}".format(mch_file),
                            modify_arg=lambda arg: arg[0] if arg is not None else setup_mch_arg(arg))
        
        coreclr_args.verify(args,
                            "jit_path",
                            lambda jit_path: os.path.isfile(jit_path),
                            "Unable to set jit_path",
                            modify_arg=lambda arg: os.path.join(coreclr_args.core_root, determine_jit_name(coreclr_args)) if arg is None else arg)

        coreclr_args.verify(args,
                            "log_file",
                            lambda unused: True,
                            "Unable to set log_file.")

        coreclr_args.verify(args,
                            "break_on_assert",
                            lambda unused: True,
                            "Unable to set break_on_assert")

        coreclr_args.verify(args,
                            "break_on_error",
                            lambda unused: True,
                            "Unable to set break_on_error")

        standard_location = False
        if coreclr_args.bin_location.lower() in coreclr_args.jit_path.lower():
            standard_location = True

        determined_arch = None
        determined_build_type = None
        if standard_location:
            standard_location_split = coreclr_args.jit_path.split(coreclr_args.bin_location)

            assert(coreclr_args.host_os in standard_location_split[1])
            specialized_path = standard_location_split[1].split(coreclr_args.host_os)

            specialized_path = specialized_path[1].split("/")[0]

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
                            modify_arg=lambda arg: arg[0] if arg is not None else setup_mch_arg(arg))

    elif coreclr_args.mode == "asmdiffs":
        coreclr_args.verify(args,
                            "base_jit_path",
                            lambda unused: True,
                            "Unable to set base_jit_path",
                            modify_arg=lambda arg: arg[0])

        coreclr_args.verify(args,
                            "diff_jit_path",
                            lambda jit_path: True,
                            "Unable to set base_jit_path",
                            modify_arg=lambda arg: arg[0])

        coreclr_args.verify(args,
                            "collection",
                            lambda collection_name: collection_name in download_index(coreclr_args),
                            "Invalid collection. Please run superpmi.py list-collections to see valid options.")

        coreclr_args.verify(args,
                            "log_file",
                            lambda unused: True,
                            "Unable to set log_file.")

        coreclr_args.verify(args,
                            "break_on_assert",
                            lambda unused: True,
                            "Unable to set break_on_assert")

        coreclr_args.verify(args,
                            "break_on_error",
                            lambda unused: True,
                            "Unable to set break_on_error")

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
            coreclr_args.verify(True,
                                "diff_jit_dump",
                                lambda unused: True,
                                "Unable to set diff_jit_dump.")

        standard_location = False
        if coreclr_args.bin_location.lower() in coreclr_args.base_jit_path.lower():
            standard_location = True

        determined_arch = None
        determined_build_type = None
        if standard_location:
            standard_location_split = coreclr_args.base_jit_path.split(coreclr_args.bin_location)

            assert(coreclr_args.host_os in standard_location_split[1])
            specialized_path = standard_location_split[1].split(coreclr_args.host_os)

            specialized_path = specialized_path[1].split("/")[0]

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
                            modify_arg=lambda arg: arg[0] if arg is not None else setup_mch_arg(arg))

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
    
    return coreclr_args

################################################################################
# main
################################################################################

def main(args):
    """ Main method
    """

    # await/async requires python >= 3.5
    if sys.version_info.major < 3 and sys.version_info.minor < 5:
        print("Error, language features require the latest python version.")
        print("Please install python 3.7 or greater")

        return 1

    # Force tieried compilation off. It will effect both collection and replay
    os.environ["COMPlus_TieredCompilation"] = "0"

    coreclr_args = setup_args(args)
    success = True

    if coreclr_args.mode == "collect":
        # Start a new SuperPMI Collection.

        begin_time = datetime.datetime.now()

        print("SuperPMI Collect")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        collection = SuperPMICollect(coreclr_args)
        success = collection.collect()

        print("Finished SuperPMI collect")

        if coreclr_args.output_mch_path != None:
            print("mch path: {}".format(coreclr_args.output_mch_path))

        end_time = datetime.datetime.now()

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))

    elif coreclr_args.mode == "replay":
        # Start a new SuperPMI Replay

        begin_time = datetime.datetime.now()

        print("SuperPMI Replay")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        mch_file = coreclr_args.mch_file
        jit_path = coreclr_args.jit_path

        print("")

        print("MCH Path: {}".format(mch_file))
        print("Jit Path: {}".format(jit_path))

        replay = SuperPMIReplay(coreclr_args, mch_file, jit_path)
        success = replay.replay()

        print("Finished SuperPMI replay")

        end_time = datetime.datetime.now()

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))

    elif coreclr_args.mode == "asmdiffs":
        # Start a new SuperPMI Replay with AsmDiffs

        begin_time = datetime.datetime.now()

        print("SuperPMI Replay")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        mch_file = coreclr_args.mch_file
        base_jit_path = coreclr_args.base_jit_path
        diff_jit_path = coreclr_args.diff_jit_path

        print("")

        print("MCH Path: {}".format(mch_file))
        print("Base Jit Path: {}".format(base_jit_path))
        print("Diff Jit Path: {}".format(diff_jit_path))

        asm_diffs = SuperPMIReplayAsmDiffs(coreclr_args, mch_file, base_jit_path, diff_jit_path)
        success = asm_diffs.replay_with_asm_diffs(coreclr_args.previous_temp_location)

        print("Finished SuperPMI replay")

        end_time = datetime.datetime.now()

        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))

    elif coreclr_args.mode == "upload":
        begin_time = datetime.datetime.now()

        print("SuperPMI upload")
        print("------------------------------------------------------------")
        print("Start time: {}".format(begin_time.strftime("%H:%M:%S")))

        upload_mch(coreclr_args)

        print("Finished SuperPMI upload")

        end_time = datetime.datetime.now()
        print("Finish time: {}".format(end_time.strftime("%H:%M:%S")))
    elif coreclr_args.mode == "list-collections":
        index = download_index(coreclr_args)

        index_count = len(index)
        print("SuperPMI list-collections")
        print("")
        print("{} different collections".format(index_count))
        print("")
        
        for item in index:
            print(item)
        
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
