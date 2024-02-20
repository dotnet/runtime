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
# https://github.com/dotnet/runtime/blob/main/src/tests/JIT/superpmi/superpmicollect.cs.
#
################################################################################
################################################################################

import argparse
import asyncio
import csv
import datetime
import locale
import logging
import math
import os
import multiprocessing
import platform
import shutil
import subprocess
import sys
import tempfile
import queue
import re
import urllib
import urllib.request
import zipfile
import time

from coreclr_arguments import *
from jitutil import TempDir, ChangeDir, remove_prefix, is_zero_length_file, is_nonzero_length_file, \
    make_safe_filename, find_file, download_one_url, download_files, report_azure_error, \
    require_azure_storage_libraries, authenticate_using_azure, \
    create_unique_directory_name, create_unique_file_name, get_files_from_path, determine_jit_name, \
    get_deepest_existing_directory

locale.setlocale(locale.LC_ALL, '')  # Use '' for auto, or force e.g. to 'en_US.UTF-8'

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
# 4. A copy of pintools, used to do throughput measurements.

az_account_name = "clrjit2"

az_superpmi_container_name = "superpmi"
az_blob_storage_account_uri = "https://" + az_account_name + ".blob.core.windows.net/"
az_blob_storage_superpmi_container_uri = az_blob_storage_account_uri + az_superpmi_container_name
az_collections_root_folder = "collections"
az_pintools_root_folder = "pintools"
pintools_current_version = "1.0"

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

throughput_description = """\
Measure throughput using PIN on one or more collections.
"""

upload_description = """\
Upload a collection to SuperPMI Azure storage.
"""

upload_private_description = """\
Upload a collection to a local file system path.
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

merge_mch_description = """\
Utility command to merge MCH files. This is a thin wrapper around
'mcs -merge -recursive -dedup -thin' followed by 'mcs -toc'.
"""

spmi_log_file_help = "Write SuperPMI tool output to a log file. Requires --sequential."

jit_ee_version_help = """\
JIT/EE interface version (the JITEEVersionIdentifier GUID from jiteeversionguid.h in the format
'a5eec3a4-4176-43a7-8c2b-a05b551d4f49'). Default: if the mcs tool is found, assume it
was built with the same JIT/EE version as the JIT we are using, and run "mcs -printJITEEVersion"
to get that version. Otherwise, use "unknown-jit-ee-version".
"""

host_os_help = "OS (windows, osx, linux). Default: current OS."

arch_help = "Architecture (x64, x86, arm, arm64). Default: current architecture."

target_os_help = "Target OS, for use with cross-compilation JIT (windows, osx, linux). Default: current OS."

target_arch_help = "Target architecture, for use with cross-compilation JIT (x64, x86, arm, arm64). Passed as asm diffs target to SuperPMI. Default: current architecture."

mch_arch_help = "Architecture of MCH files to download, used for cross-compilation altjit (x64, x86, arm, arm64). Default: target architecture."

build_type_help = "Build type (Debug, Checked, Release). Default: Checked."

throughput_build_type_help = "Build type (Debug, Checked, Release). Default: Release."

core_root_help = "Core_Root location. Optional; it will be deduced if possible from runtime repo root."

log_level_help = """\
Console log level (output verbosity level).
One of: critical, error, warning, info, debug.
Output from this level and higher is output to the console.
All output is always written to the log file.
Default: warning.
"""

log_file_help = "Output log file path. If not specified, a default location is chosen."

product_location_help = "Built Product directory location. Optional; it will be deduced if possible from runtime repo root."

spmi_location_help = """\
Directory in which to put SuperPMI files, such as downloaded MCH files, asm diffs, and repro .MC files.
Optional. Default is 'spmi' within the repo 'artifacts' directory.
If 'SUPERPMI_CACHE_DIRECTORY' environment variable is set to a path, it will use that directory.
"""

superpmi_collect_help = """\
Command to run SuperPMI collect over. Note that there cannot be any dotnet CLI commands
invoked inside this command, as they will fail due to the shim JIT being set.
"""

replay_mch_files_help = """\
MCH files, or directories containing MCH files, to use for replay. For each directory passed,
all recursively found MCH files in that directory root will be used. Files may either be a path
on disk or a URI to a MCH file to download. Use these MCH files instead of a collection from
the Azure Storage MCH file store. UNC paths will be downloaded and cached locally.
"""

private_store_help = """\
Specify the path to one or more private SuperPMI data stores. Default: use the semicolon separated
value of the SUPERPMI_PRIVATE_STORE environment variable, if it exists.
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

download_no_progress_help = """\
If specified, then download progress will not be shown.
"""

merge_mch_pattern_help = """\
A pattern to describing files to merge, passed through directly to `mcs -merge`.
Acceptable patterns include `*.mch`, `file*.mch`, and `c:\\my\\directory\\*.mch`.
Only the final component can contain a `*` wildcard; the directory path cannot.
"""

error_limit_help = """
Specify the failure `limit` after which replay and asmdiffs will exit if it sees
more than `limit` failures.
"""

compile_help = """\
Compile only specified method contexts, e.g., `-compile 20,25`. This is passed directly to the superpmi.exe `-compile` argument. See `superpmi.exe -?` for full documentation about allowed formats.
"""

# Start of parser object creation.

parser = argparse.ArgumentParser(description=description)

subparsers = parser.add_subparsers(dest='mode', help="Command to invoke")
subparsers.required = True

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

def add_core_root_arguments(parser, build_type_default, build_type_help):
    parser.add_argument("-arch", help=arch_help)
    parser.add_argument("-build_type", default=build_type_default, help=build_type_help)
    parser.add_argument("-host_os", help=host_os_help)
    parser.add_argument("-core_root", help=core_root_help)
    parser.add_argument("-log_level", help=log_level_help)
    parser.add_argument("-log_file", help=log_file_help)
    parser.add_argument("-spmi_location", help=spmi_location_help)
    parser.add_argument("--no_progress", action="store_true", help=download_no_progress_help)

core_root_parser = argparse.ArgumentParser(add_help=False)
add_core_root_arguments(core_root_parser, "Checked", build_type_help)

# Create a set of arguments common to target specification. Used for collect, replay, asmdiffs, upload, upload-private, download, list-collections.

target_parser = argparse.ArgumentParser(add_help=False)

target_parser.add_argument("-target_arch", help=target_arch_help)
target_parser.add_argument("-target_os", help=target_os_help)
target_parser.add_argument("-mch_arch", help=mch_arch_help)

# Create a set of arguments common to all commands that run SuperPMI.

superpmi_common_parser = argparse.ArgumentParser(add_help=False)

superpmi_common_parser.add_argument("--break_on_assert", action="store_true", help=break_on_assert_help)
superpmi_common_parser.add_argument("--break_on_error", action="store_true", help=break_on_error_help)
superpmi_common_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)
superpmi_common_parser.add_argument("--sequential", action="store_true", help="Run SuperPMI in sequential mode. Default is to run in parallel for faster runs.")
superpmi_common_parser.add_argument("-spmi_log_file", help=spmi_log_file_help)
superpmi_common_parser.add_argument("-jit_name", help="Specify the filename of the jit to use, e.g., 'clrjit_universal_arm64_x64.dll'. Default is clrjit.dll/libclrjit.so")
superpmi_common_parser.add_argument("--altjit", action="store_true", help="Set the altjit variables on replay.")
superpmi_common_parser.add_argument("-error_limit", help=error_limit_help)

# subparser for collect
collect_parser = subparsers.add_parser("collect", description=collect_description, parents=[core_root_parser, target_parser, superpmi_common_parser])

# Add required arguments
collect_parser.add_argument("collection_command", nargs='?', help=superpmi_collect_help)
collect_parser.add_argument("collection_args", nargs='?', help="Arguments to pass to the SuperPMI collect command. This is a single string; quote it if necessary if the arguments contain spaces.")

collect_parser.add_argument("--pmi", action="store_true", help="Run PMI on a set of directories or assemblies.")
collect_parser.add_argument("--crossgen2", action="store_true", help="Run crossgen2 on a set of directories or assemblies.")
collect_parser.add_argument("--nativeaot", action="store_true", help="Run nativeaot on a set of directories or 'ilc.rsps' files.")
collect_parser.add_argument("-assemblies", dest="assemblies", nargs="+", default=[], help="A list of managed dlls or directories to recursively use while collecting with PMI or crossgen2. Required if --pmi or --crossgen2 is specified.")
collect_parser.add_argument("-ilc_rsps", dest="ilc_rsps", nargs="+", default=[], help="For --nativeaot only. A list of 'ilc.rsp' files.")
collect_parser.add_argument("-exclude", dest="exclude", nargs="+", default=[], help="A list of files or directories to exclude from the files and directories specified by `-assemblies`.")
collect_parser.add_argument("-pmi_location", help="Path to pmi.dll to use during PMI run. Optional; pmi.dll will be downloaded from Azure Storage if necessary.")
collect_parser.add_argument("-pmi_path", metavar="PMIPATH_DIR", nargs='*', help="Specify a \"load path\" where assemblies can be found during pmi.dll run. Optional; the argument values are translated to PMIPATH environment variable.")
collect_parser.add_argument("-output_mch_path", help="Location to place the final MCH file. Default is a constructed file name in the current directory.")
collect_parser.add_argument("--merge_mch_files", action="store_true", help="Merge multiple MCH files. Use the -mch_files flag to pass a list of MCH files to merge.")
collect_parser.add_argument("-mch_files", metavar="MCH_FILE", nargs='+', help="Pass a sequence of MCH files which will be merged. Required by --merge_mch_files.")
collect_parser.add_argument("--use_zapdisable", action="store_true", help="Sets DOTNET_ZapDisable=1 and DOTNET_ReadyToRun=0 when doing collection to cause NGEN/ReadyToRun images to not be used, and thus causes JIT compilation and SuperPMI collection of these methods.")
collect_parser.add_argument("--tiered_compilation", action="store_true", help="Sets DOTNET_TieredCompilation=1 when doing collections.")
collect_parser.add_argument("--tiered_pgo", action="store_true", help="Sets DOTNET_TieredCompilation=1 and DOTNET_TieredPGO=1 when doing collections.")
collect_parser.add_argument("--ci", action="store_true", help="Special collection mode for handling zero-sized files in Azure DevOps + Helix pipelines collections.")

# Allow for continuing a collection in progress
collect_parser.add_argument("-temp_dir", help="Specify an existing temporary directory to use. Useful if continuing an ongoing collection process, or forcing a temporary directory to a particular hard drive. Optional; default is to create a temporary directory in the usual TEMP location.")
collect_parser.add_argument("--clean", action="store_true", help="Clean the collection by removing contexts that fail to replay without error.")
collect_parser.add_argument("--skip_collection_step", action="store_true", help="Do not run the collection step.")
collect_parser.add_argument("--skip_merge_step", action="store_true", help="Do not run the merge step.")
collect_parser.add_argument("--skip_toc_step", action="store_true", help="Do not run the TOC creation step.")
collect_parser.add_argument("--skip_collect_mc_files", action="store_true", help="Do not collect .MC files")

# Create a set of arguments common to all SuperPMI replay commands, namely basic replay and ASM diffs.
# Note that SuperPMI collection also runs a replay to verify the final MCH file, so many arguments
# common to replay are also applicable to that replay as well.

replay_common_parser = argparse.ArgumentParser(add_help=False)

replay_common_parser.add_argument("-mch_files", metavar="MCH_FILE", nargs='+', help=replay_mch_files_help)
replay_common_parser.add_argument("-filter", nargs='+', help=filter_help)
replay_common_parser.add_argument("-product_location", help=product_location_help)
replay_common_parser.add_argument("--force_download", action="store_true", help=force_download_help)
replay_common_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
replay_common_parser.add_argument("-private_store", action="append", help=private_store_help)
replay_common_parser.add_argument("-compile", "-c", help=compile_help)

# subparser for replay
replay_parser = subparsers.add_parser("replay", description=replay_description, parents=[core_root_parser, target_parser, superpmi_common_parser, replay_common_parser])

replay_parser.add_argument("-jit_path", help="Path to clrjit. Defaults to Core_Root JIT.")
replay_parser.add_argument("-jitoption", action="append", help="Pass option through to the jit. Format is key=value, where key is the option name without leading `DOTNET_`. `key#value` is also accepted.")

# common subparser for asmdiffs and throughput
base_diff_parser = argparse.ArgumentParser(add_help=False)
base_diff_parser.add_argument("-base_jit_path", help="Path to baseline clrjit. Defaults to baseline JIT from rolling build, by computing baseline git hash.")
base_diff_parser.add_argument("-diff_jit_path", help="Path to diff clrjit. Defaults to Core_Root JIT.")
base_diff_parser.add_argument("-git_hash", help="Use this git hash as the current hash for use to find a baseline JIT. Defaults to current git hash of source tree.")
base_diff_parser.add_argument("-base_git_hash", help="Use this git hash as the baseline JIT hash. Default: search for the baseline hash.")
base_diff_parser.add_argument("-jitoption", action="append", help="Option to pass to both baseline and diff JIT. Format is key=value, where key is the option name without leading `DOTNET_`. `key#value` is also accepted.")
base_diff_parser.add_argument("-base_jit_option", action="append", help="Option to pass to the baseline JIT. Format is key=value, where key is the option name without leading `DOTNET_`. `key#value` is also accepted.")
base_diff_parser.add_argument("-diff_jit_option", action="append", help="Option to pass to the diff JIT. Format is key=value, where key is the option name without leading `DOTNET_`. `key#value` is also accepted.")

# subparser for asmdiffs
asm_diff_parser = subparsers.add_parser("asmdiffs", description=asm_diff_description, parents=[core_root_parser, target_parser, superpmi_common_parser, replay_common_parser, base_diff_parser])
asm_diff_parser.add_argument("--diff_jit_dump", action="store_true", help="Generate JitDump output for diffs. Default: only generate asm, not JitDump.")
asm_diff_parser.add_argument("--gcinfo", action="store_true", help="Include GC info in disassembly (sets DOTNET_JitGCDump; requires instructions to be prefixed by offsets).")
asm_diff_parser.add_argument("--debuginfo", action="store_true", help="Include debug info after disassembly (sets DOTNET_JitDebugDump).")
asm_diff_parser.add_argument("--alignloops", action="store_true", help="Do diffs with loop alignment enabled (uses DOTNET_JitAlignLoops default; otherwise DOTNET_JitAlignLoops is set to zero (loop alignment disabled)).")
asm_diff_parser.add_argument("-tag", help="Specify a word to add to the directory name where the asm diffs will be placed")
asm_diff_parser.add_argument("-metrics", action="append", help="Metrics option to pass to jit-analyze. Can be specified multiple times, one for each metric.")
asm_diff_parser.add_argument("--diff_with_release", action="store_true", help="Specify if this is asmdiff using release binaries.")
asm_diff_parser.add_argument("--git_diff", action="store_true", help="Produce a '.diff' file from 'base' and 'diff' folders if there were any differences.")

# subparser for throughput
throughput_parser = subparsers.add_parser("tpdiff", description=throughput_description, parents=[target_parser, superpmi_common_parser, replay_common_parser, base_diff_parser])
add_core_root_arguments(throughput_parser, "Release", throughput_build_type_help)

# subparser for upload
upload_parser = subparsers.add_parser("upload", description=upload_description, parents=[core_root_parser, target_parser])

upload_parser.add_argument("-mch_files", metavar="MCH_FILE", required=True, nargs='+', help=upload_mch_files_help)
upload_parser.add_argument("-az_storage_key", help="Key for the clrjit Azure Storage location. Default: use the value of the CLRJIT_AZ_KEY environment variable.")
upload_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
upload_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)

# subparser for upload-private
upload_private_parser = subparsers.add_parser("upload-private", description=upload_private_description, parents=[core_root_parser, target_parser])

upload_private_parser.add_argument("-mch_files", metavar="MCH_FILE", required=True, nargs='+', help=upload_mch_files_help)
upload_private_parser.add_argument("-private_store", required=True, help="Target directory root of the private store in which to place the files.")
upload_private_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
upload_private_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)

# subparser for download
download_parser = subparsers.add_parser("download", description=download_description, parents=[core_root_parser, target_parser])

download_parser.add_argument("-filter", nargs='+', help=filter_help)
download_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
download_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)
download_parser.add_argument("--force_download", action="store_true", help=force_download_help)
download_parser.add_argument("-mch_files", metavar="MCH_FILE", nargs='+', help=replay_mch_files_help)
download_parser.add_argument("-private_store", action="append", help=private_store_help)

# subparser for list-collections
list_collections_parser = subparsers.add_parser("list-collections", description=list_collections_description, parents=[core_root_parser, target_parser])

list_collections_parser.add_argument("-jit_ee_version", help=jit_ee_version_help)
list_collections_parser.add_argument("--all", action="store_true", help="Show all MCH files, not just those for the specified (or default) JIT-EE version, OS, and architecture")
list_collections_parser.add_argument("--local", action="store_true", help="Show the local MCH download cache")

# subparser for merge-mch

merge_mch_parser = subparsers.add_parser("merge-mch", description=merge_mch_description, parents=[core_root_parser])

merge_mch_parser.add_argument("-output_mch_path", required=True, help="Location to place the final MCH file.")
merge_mch_parser.add_argument("-pattern", required=True, help=merge_mch_pattern_help)
merge_mch_parser.add_argument("--ci", action="store_true", help="Special collection mode for handling zero-sized files in Azure DevOps + Helix pipelines collections.")

################################################################################
# Helper functions
################################################################################
def run_and_log(command, log_level=logging.DEBUG):
    """ Run a command and log its output to the debug logger

    Args:
        command (list) : Command to run
        log_level (int) : log level to use for logging output (but not the "Invoking" text)

    Returns:
        Process return code
    """

    logging.log(log_level, "Invoking: %s", " ".join(command))
    proc = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    stdout_output, _ = proc.communicate()
    for line in stdout_output.decode('utf-8', errors='replace').splitlines():  # There won't be any stderr output since it was piped to stdout
        logging.log(log_level, line)
    return proc.returncode


def run_and_log_return_output(command, log_level=logging.DEBUG):
    """ Run a command and log its output to the debug logger, returning the output as an array of string lines

    Args:
        command (list) : Command to run
        log_level (int) : log level to use for logging output (but not the "Invoking" text)

    Returns:
        Tuple of (Process return code, array of string lines of output)
    """

    output = []
    logging.log(log_level, "Invoking: %s", " ".join(command))
    proc = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    stdout_output, _ = proc.communicate()
    for line in stdout_output.decode('utf-8', errors='replace').splitlines():  # There won't be any stderr output since it was piped to stdout
        logging.log(log_level, line)
        output.append(line)
    return (proc.returncode, output)


def write_file_to_log(filepath, log_level=logging.DEBUG):
    """ Read the text of a file and write it to the logger. If the file doesn't exist, don't output anything.

    Args:
        filepath (string) : file to log
        log_level (int)   : log level to use for logging output

    Returns:
        Nothing
    """
    if not os.path.exists(filepath):
        return

    logging.log(log_level, "============== Contents of %s", filepath)

    with open(filepath) as file_handle:
        lines = file_handle.readlines()
        lines = [item.strip() for item in lines]
        for line in lines:
            logging.log(log_level, line)

    logging.log(log_level, "============== End contents of %s", filepath)

# Functions to verify the OS and architecture. They take an instance of CoreclrArguments,
# which is used to find the list of legal OS and architectures

def check_host_os(coreclr_args, host_os):
    return (host_os is not None) and (host_os in coreclr_args.valid_host_os)

def check_target_os(coreclr_args, target_os):
    return (target_os is not None) and (target_os in coreclr_args.valid_host_os)

def check_arch(coreclr_args, arch):
    return (arch is not None) and (arch in coreclr_args.valid_arches)

def check_target_arch(coreclr_args, target_arch):
    return (target_arch is not None) and (target_arch in coreclr_args.valid_arches)

def check_mch_arch(coreclr_args, mch_arch):
    return (mch_arch is not None) and (mch_arch in coreclr_args.valid_arches)


def create_artifacts_base_name(coreclr_args, mch_file):
    """ Create an appropriate "base" name for use creating a directory name related to MCH file playback.
        This will later be prepended by "asm." or "jitdump.", for example, and
        create_unique_directory_name() should be called on the final name to ensure it is unique.

        Use the MCH file base name as the main part of the directory name, removing
        the trailing ".mch", if any.

        If there is a tag specified (for asm diffs), prepend the tag.

    Args:
        coreclr_args   : the parsed arguments
        mch_file (str) : the MCH file name that is being replayed.

    Returns:
        A directory name to be used.
    """
    artifacts_base_name = os.path.basename(mch_file)
    if artifacts_base_name.lower().endswith(".mch"):
        artifacts_base_name = artifacts_base_name[:-4]
    if hasattr(coreclr_args, "tag") and coreclr_args.tag is not None:
        artifacts_base_name = "{}.{}".format(coreclr_args.tag, artifacts_base_name)
    return artifacts_base_name

def read_csv(path):
    with open(path) as csv_file:
        reader = csv.DictReader(csv_file)
        return list(reader)

def decode_clrjit_build_string(clrjit_path):
    """ Obtain information about the compiler that was used to compile the clrjit at the specified path.

    Returns:
        None if the build string was not found; otherwise a tuple (version, with_native_pgo).
        See 'jitBuildString' in buildstring.cpp in the JIT to see where this is defined.
    """

    with open(clrjit_path, "rb") as fh:
        contents = fh.read()

    match = re.search(b'RyuJIT built by ([^\0]+?) targeting ([^\0]+?)-([^\0]+?)(| \(with native PGO\)| \(without native PGO\)|)\0', contents)
    if match is None:
        return None

    version = match.group(1).decode("ascii")
    with_pgo_data = "with native PGO" in match.group(4).decode("ascii")
    return (version, with_pgo_data)

################################################################################
# Helper classes
################################################################################

class AsyncSubprocessHelper:
    """ Class to help with async multiprocessing tasks.
    """

    def __init__(self, items, subproc_count=multiprocessing.cpu_count(), verbose=False):
        self.items = items
        self.subproc_count = subproc_count
        self.verbose = verbose
        self.subproc_count_queue = None

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
        # going to allow. By default, this will be one entry per CPU.
        # Using subproc_count_queue.get() will block when we're running
        # a task on every CPU.
        chunk_size = self.subproc_count
        self.subproc_count_queue = asyncio.Queue(chunk_size)
        for item in range(chunk_size):
            self.subproc_count_queue.put_nowait(item)

        # Create a 'tasks' list of async function calls, one for each item.
        # When all these calls complete, we're done.
        size = len(self.items)
        count = 1
        tasks = []
        for item in self.items:
            tasks.append(self.__get_item__(item, count, size, async_callback, *extra_args))
            count += 1

        # Invoke all the calls to __get_item__ concurrently and wait for them all to finish.
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

        try:
            if sys.version_info[:2] >= (3, 7):
                loop = asyncio.get_running_loop()
            else:
                loop = asyncio.get_event_loop()
        except RuntimeError:
            if 'win32' in sys.platform:
                # Windows specific event-loop policy & cmd
                loop = asyncio.ProactorEventLoop()
            else:
                loop = asyncio.new_event_loop()

            asyncio.set_event_loop(loop)
            
        loop.run_until_complete(self.__run_to_completion__(async_callback, *extra_args))
        os.environ.clear()
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

        self.core_root = coreclr_args.core_root

        if coreclr_args.host_os == "osx":
            self.collection_shim_name = "libsuperpmi-shim-collector.dylib"
            self.corerun_tool_name = "corerun"
        elif coreclr_args.host_os == "linux":
            self.collection_shim_name = "libsuperpmi-shim-collector.so"
            self.corerun_tool_name = "corerun"
        elif coreclr_args.host_os == "windows":
            self.collection_shim_name = "superpmi-shim-collector.dll"
            self.corerun_tool_name = "corerun.exe"
        else:
            raise RuntimeError("Unsupported OS.")

        self.collection_shim_path = os.path.join(self.core_root, self.collection_shim_name)

        jit_name = get_jit_name(coreclr_args)
        self.jit_path = os.path.join(coreclr_args.core_root, jit_name)

        self.superpmi_path = determine_superpmi_tool_path(coreclr_args)
        self.mcs_path = determine_mcs_tool_path(coreclr_args)

        self.collection_command = coreclr_args.collection_command
        self.collection_args = coreclr_args.collection_args

        if coreclr_args.pmi:
            self.pmi_location = determine_pmi_location(coreclr_args)
            self.corerun = os.path.join(self.core_root, self.corerun_tool_name)

        if coreclr_args.crossgen2:
            self.corerun = os.path.join(self.core_root, self.corerun_tool_name)
            if coreclr_args.dotnet_tool_path is None:
                self.crossgen2_driver_tool = self.corerun
            else:
                self.crossgen2_driver_tool = coreclr_args.dotnet_tool_path
            logging.debug("Using crossgen2 driver tool %s", self.crossgen2_driver_tool)

        if coreclr_args.pmi or coreclr_args.crossgen2:
            self.assemblies = coreclr_args.assemblies
            self.exclude = coreclr_args.exclude
            if coreclr_args.tiered_compilation or coreclr_args.tiered_pgo:
               raise RuntimeError("Tiering options have no effect for pmi or crossgen2 collections.")

        if coreclr_args.nativeaot:
            self.ilc_rsps = coreclr_args.ilc_rsps
            self.exclude = coreclr_args.exclude
            if coreclr_args.tiered_compilation or coreclr_args.tiered_pgo:
                raise RuntimeError("Tiering options have no effect for nativeaot collections.")

        if coreclr_args.tiered_compilation and coreclr_args.tiered_pgo:
            raise RuntimeError("Pass only one tiering option.")

        self.coreclr_args = coreclr_args

        self.temp_location = None

        # Pathname for a temporary .MCL file used for noticing SuperPMI replay failures against base MCH.
        self.base_fail_mcl_file = None

        # The base .MCH file path
        self.base_mch_file = None

        # Final .MCH file path
        if self.coreclr_args.output_mch_path is not None:
            self.final_mch_file = os.path.abspath(self.coreclr_args.output_mch_path)
            final_mch_dir = os.path.dirname(self.final_mch_file)
            if not os.path.isdir(final_mch_dir):
                os.makedirs(final_mch_dir)
        else:
            # Default directory is the current working directory (before we've changed the directory using "TempDir")
            default_mch_location = os.path.abspath(os.getcwd())
            if not os.path.isdir(default_mch_location):
                os.makedirs(default_mch_location)
            default_mch_basename = "{}.{}.{}".format(self.coreclr_args.host_os, self.coreclr_args.arch, self.coreclr_args.build_type)
            default_mch_extension = "mch"
            self.final_mch_file = create_unique_file_name(default_mch_location, default_mch_basename, default_mch_extension)

        # The .TOC file path for the clean thin unique .MCH file
        self.toc_file = "{}.mct".format(self.final_mch_file)

    ############################################################################
    # Instance Methods
    ############################################################################

    def collect(self):
        """ Do the SuperPMI Collection.
        """

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
            with TempDir(self.coreclr_args.temp_dir, self.coreclr_args.skip_cleanup, change_dir=False) as temp_location:
                # Setup all of the temp locations
                self.base_fail_mcl_file = os.path.join(temp_location, "basefail.mcl")
                self.base_mch_file = os.path.join(temp_location, "base.mch")

                self.temp_location = temp_location

                if self.coreclr_args.crossgen2 or self.coreclr_args.nativeaot:
                    jit_name = os.path.basename(self.jit_path)
                    # There are issues when running SuperPMI and crossgen2 when using the same JIT binary. 
                    # Therefore, we produce a copy of the JIT binary for SuperPMI to use. 
                    jit_name_ext = os.path.splitext(jit_name)[1]
                    jit_name_without_ext = os.path.splitext(jit_name)[0]
                    try:
                        self.superpmi_jit_path = os.path.join(self.temp_location, jit_name_without_ext + "_superpmi" + jit_name_ext)
                        shutil.copyfile(self.jit_path, self.superpmi_jit_path)
                    except Exception:
                        # Fallback to original JIT path if we are not able to make a copy.
                        self.superpmi_jit_path = self.jit_path
                else:
                    self.superpmi_jit_path = self.jit_path

                logging.info("SuperPMI JIT Path: %s", self.superpmi_jit_path)

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

                if self.coreclr_args.clean:
                    self.__create_clean_mch_file__()
                else:
                    self.__copy_to_final_mch_file__()

                if not self.coreclr_args.skip_toc_step:
                    self.__create_toc__()

                if self.coreclr_args.clean:
                    # There is no point to verify unless we have run the clean step.
                    self.__verify_final_mch__()

                if self.coreclr_args.ci:
                    self.__process_for_ci__()

                passed = True

        except Exception as exception:
            logging.critical(exception)

        if passed:
            logging.info("Generated MCH file: %s", self.final_mch_file)

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

            # Set environment variables. For crossgen2, we need to pass the DOTNET variables as arguments to the JIT using
            # the `-codegenopt` argument.

            env_copy = os.environ.copy()

            root_env = {}
            root_env["SuperPMIShimLogPath"] = self.temp_location
            root_env["SuperPMIShimPath"] = self.superpmi_jit_path

            dotnet_env = {}
            dotnet_env["EnableExtraSuperPmiQueries"] = "1"

            if self.coreclr_args.tiered_compilation:
                dotnet_env["TieredCompilation"] = "1"
            elif self.coreclr_args.tiered_pgo:
                dotnet_env["TieredCompilation"] = "1"
                dotnet_env["TieredPGO"] = "1"
            else:
                dotnet_env["TieredCompilation"] = "0"

            if self.coreclr_args.use_zapdisable:
                dotnet_env["ZapDisable"] = "1"
                dotnet_env["ReadyToRun"] = "0"

            logging.debug("Starting collection.")
            logging.debug("")

            def set_and_report_env(env, root_env, dotnet_env = None):
                for var, value in root_env.items():
                    env[var] = value
                    print_platform_specific_environment_vars(logging.DEBUG, self.coreclr_args, var, value)
                if dotnet_env is not None:
                    for var, value in dotnet_env.items():
                        dotnet_var = "DOTNET_" + var
                        env[dotnet_var] = value
                        print_platform_specific_environment_vars(logging.DEBUG, self.coreclr_args, dotnet_var, value)

            # If we need them, collect all the assemblies we're going to use for the collection(s).
            # Remove the files matching the `-exclude` arguments (case-insensitive) from the list.
            if self.coreclr_args.pmi or self.coreclr_args.crossgen2 or self.coreclr_args.nativeaot:

                def filter_file(file):
                    if self.coreclr_args.nativeaot:
                        extensions = [".ilc.rsp"]
                    # Skip assemblies in the 'aotsdk' folder when not running 'nativeaot'.
                    elif os.path.dirname(file.lower()).endswith("aotsdk"):
                        return False
                    else:
                        extensions = [".dll", ".exe"]
                    return any(file.endswith(extension) for extension in extensions) and (self.exclude is None or not any(e.lower() in file.lower() for e in self.exclude))

                if self.coreclr_args.nativeaot:
                    ilc_rsps = []
                    for item in self.ilc_rsps:
                        ilc_rsps += get_files_from_path(item, match_func=filter_file)
                    if len(ilc_rsps) == 0:
                        logging.error("No 'ilc.rsp' files found using `-ilc_rsps` and `-exclude` arguments!")
                    else:
                        logging.debug("Using ilc_rsps:")
                        for item in ilc_rsps:
                            logging.debug("  %s", item)
                        logging.debug("") # add trailing empty line
                else:
                    assemblies = []
                    for item in self.assemblies:
                        assemblies += get_files_from_path(item, match_func=filter_file)
                    if len(assemblies) == 0:
                        logging.error("No assemblies found using `-assemblies` and `-exclude` arguments!")
                    else:
                        logging.debug("Using assemblies:")
                        for item in assemblies:
                            logging.debug("  %s", item)
                        logging.debug("") # add trailing empty line

            ################################################################################################ Do collection using given collection command (e.g., script)
            if self.collection_command is not None:
                logging.debug("Starting collection using command")
                begin_time = datetime.datetime.now()

                collection_command_env = env_copy.copy()
                collection_dotnet_env = dotnet_env.copy()
                # In debug/checked builds we have the JitPath variable that the runtime will prefer.
                # We still specify JitName for release builds, but this requires the user to manually
                # copy the shim next to coreclr.
                collection_dotnet_env["JitPath"] = self.collection_shim_path
                collection_dotnet_env["JitName"] = self.collection_shim_name
                set_and_report_env(collection_command_env, root_env, collection_dotnet_env)

                logging.info("Collecting using command:")
                logging.info("  %s %s", self.collection_command, " ".join(self.collection_args))

                assert isinstance(self.collection_command, str)
                assert isinstance(self.collection_args, list)

                command = [self.collection_command, ] + self.collection_args
                proc = subprocess.Popen(command, env=collection_command_env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
                stdout_output, _ = proc.communicate()
                for line in stdout_output.decode('utf-8', errors='replace').splitlines():  # There won't be any stderr output since it was piped to stdout
                    logging.debug(line)

                elapsed_time = datetime.datetime.now() - begin_time
                logging.debug("Done. Elapsed time: %s", elapsed_time)
            ################################################################################################ end of "self.collection_command is not None"

            ################################################################################################ Do collection using PMI
            if self.coreclr_args.pmi is True:
                logging.debug("Starting collection using PMI")

                async def run_pmi(print_prefix, assembly, self):
                    """ Run pmi over all dlls
                    """

                    command = [self.corerun, self.pmi_location, "DRIVEALL", assembly]
                    command_string = " ".join(command)
                    logging.debug("%s%s", print_prefix, command_string)

                    begin_time = datetime.datetime.now()

                    # Save the stdout and stderr to files, so we can see if PMI wrote any interesting messages.
                    # Use the name of the assembly as the basename of the file. mkstemp() will ensure the file
                    # is unique.
                    root_output_filename = make_safe_filename("pmi_" + assembly + "_")
                    try:
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

                        return_code = proc.returncode
                        if return_code != 0:
                            logging.debug("'%s': Error return code: %s", command_string, return_code)
                            write_file_to_log(stdout_filepath, log_level=logging.DEBUG)

                        write_file_to_log(stderr_filepath, log_level=logging.DEBUG)
                    except OSError as ose:
                        if "[WinError 32] The process cannot access the file because it is being used by another " \
                           "process:" in format(ose):
                            logging.warning("Skipping file %s. Got error: %s", root_output_filename, ose)
                        else:
                            raise ose

                    elapsed_time = datetime.datetime.now() - begin_time
                    logging.debug("%sDone. Elapsed time: %s", print_prefix, elapsed_time)

                # Set environment variables.
                pmi_command_env = env_copy.copy()
                pmi_dotnet_env = dotnet_env.copy()
                # In debug/checked builds we have the JitPath variable that the runtime will prefer.
                # We still specify JitName for release builds, but this requires the user to manually
                # copy the shim next to coreclr.
                pmi_dotnet_env["JitPath"] = self.collection_shim_path
                pmi_dotnet_env["JitName"] = self.collection_shim_name

                if self.coreclr_args.pmi_path is not None:
                    pmi_root_env = root_env.copy()
                    pmi_root_env["PMIPATH"] = ";".join(self.coreclr_args.pmi_path)
                else:
                    pmi_root_env = root_env

                set_and_report_env(pmi_command_env, pmi_root_env, pmi_dotnet_env)

                old_env = os.environ.copy()
                os.environ.update(pmi_command_env)

                helper = AsyncSubprocessHelper(assemblies, verbose=True)
                helper.run_to_completion(run_pmi, self)

                os.environ.clear()
                os.environ.update(old_env)
            ################################################################################################ end of "self.coreclr_args.pmi is True"

            ################################################################################################ Do collection using crossgen2
            if self.coreclr_args.crossgen2 is True:
                logging.debug("Starting collection using crossgen2")

                async def run_crossgen2(print_prefix, assembly, self):
                    """ Run crossgen2 over all dlls
                    """

                    root_crossgen2_output_filename = make_safe_filename("crossgen2_" + assembly) + ".out.dll"
                    crossgen2_output_assembly_filename = os.path.join(self.temp_location, root_crossgen2_output_filename)
                    try:
                        if os.path.exists(crossgen2_output_assembly_filename):
                            os.remove(crossgen2_output_assembly_filename)
                    except OSError as ose:
                        if "[WinError 32] The process cannot access the file because it is being used by another " \
                           "process:" in format(ose):
                            logging.warning("Skipping file %s. Got error: %s", crossgen2_output_assembly_filename, ose)
                            return
                        else:
                            raise ose

                    root_output_filename = make_safe_filename("crossgen2_" + assembly + "_")

                    # Create a temporary response file to put all the arguments to crossgen2 (otherwise the path length limit could be exceeded):
                    #
                    # <dll to compile>
                    # -o:<output dll>
                    # -r:<Core_Root>\System.*.dll
                    # -r:<Core_Root>\Microsoft.*.dll
                    # -r:<Core_Root>\System.Private.CoreLib.dll
                    # -r:<Core_Root>\netstandard.dll
                    # --jitpath:<self.collection_shim_name>
                    # --codegenopt:<option>=<value>   /// for each member of dotnet_env
                    #
                    # invoke with:
                    #
                    # dotnet <Core_Root>\crossgen2\crossgen2.dll @<temp.rsp>
                    #
                    # where "dotnet" is one of:
                    # 1. <runtime_root>\dotnet.cmd/sh
                    # 2. "dotnet" on PATH
                    # 3. corerun in Core_Root

                    rsp_file_handle, rsp_filepath = tempfile.mkstemp(suffix=".rsp", prefix=root_output_filename, dir=self.temp_location)
                    with open(rsp_file_handle, "w") as rsp_write_handle:
                        rsp_write_handle.write(assembly + "\n")
                        rsp_write_handle.write("-o:" + crossgen2_output_assembly_filename + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "System.*.dll") + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "Microsoft.*.dll") + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "mscorlib.dll") + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "netstandard.dll") + "\n")
                        rsp_write_handle.write("--parallelism:1" + "\n")
                        rsp_write_handle.write("--jitpath:" + os.path.join(self.core_root, self.collection_shim_name) + "\n")
                        for var, value in dotnet_env.items():
                            rsp_write_handle.write("--codegenopt:" + var + "=" + value + "\n")

                    # Log what is in the response file
                    write_file_to_log(rsp_filepath)

                    command = [self.crossgen2_driver_tool, self.coreclr_args.crossgen2_tool_path, "@" + rsp_filepath]
                    command_string = " ".join(command)
                    logging.debug("%s%s", print_prefix, command_string)

                    begin_time = datetime.datetime.now()

                    # Save the stdout and stderr to files, so we can see if crossgen2 wrote any interesting messages.
                    # Use the name of the assembly as the basename of the file. mkstemp() will ensure the file
                    # is unique.
                    try:
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

                        return_code = proc.returncode
                        if return_code != 0:
                            logging.debug("'%s': Error return code: %s", command_string, return_code)
                            write_file_to_log(stdout_filepath, log_level=logging.DEBUG)

                        write_file_to_log(stderr_filepath, log_level=logging.DEBUG)
                    except OSError as ose:
                        if "[WinError 32] The process cannot access the file because it is being used by another " \
                           "process:" in format(ose):
                            logging.warning("Skipping file %s. Got error: %s", root_output_filename, ose)
                        else:
                            raise ose

                    # Delete the response file unless we are skipping cleanup
                    if not self.coreclr_args.skip_cleanup:
                        os.remove(rsp_filepath)

                    elapsed_time = datetime.datetime.now() - begin_time
                    logging.debug("%sDone. Elapsed time: %s", print_prefix, elapsed_time)

                # Set environment variables.
                crossgen2_command_env = env_copy.copy()
                set_and_report_env(crossgen2_command_env, root_env)

                old_env = os.environ.copy()
                os.environ.update(crossgen2_command_env)

                # Note: crossgen2 compiles in parallel by default. However, it seems to lead to sharing violations
                # in SuperPMI collection, accessing the MC file. So, disable crossgen2 parallism by using
                # the "--parallelism:1" switch, and allowing coarse-grained (per-assembly) parallelism here.
                # It turns out this works better anyway, as there is a lot of non-parallel time between
                # crossgen2 parallel compilations.
                helper = AsyncSubprocessHelper(assemblies, verbose=True)
                helper.run_to_completion(run_crossgen2, self)

                os.environ.clear()
                os.environ.update(old_env)
            ################################################################################################ end of "self.coreclr_args.crossgen2 is True"

            ################################################################################################ Do collection using nativeaot
            if self.coreclr_args.nativeaot is True:
                logging.debug("Starting collection using nativeaot")

                async def run_nativeaot(print_prefix, original_rsp_filepath, self):
                    if not original_rsp_filepath.endswith(".ilc.rsp"):
                        raise RuntimeError(f"Expected a '.ilc.rsp' file for nativeaot, but got {original_rsp_filepath}")

                    # The contents of the rsp contain many knobs and arguments that represent customer scenarios.
                    # Make a copy of the .ilc.rsp file as we are going to modify it. We do not want to modify the original one.
                    # The modifications include re-writing paths for references, input assembly and the output.
                    # We do this because the current paths in the rsp *may* not exist by the time we want to run ilc.
                    rsp_filepath = os.path.join(self.temp_location, make_safe_filename("nativeaot_" + original_rsp_filepath) + ".rsp")
                    shutil.copyfile(original_rsp_filepath, rsp_filepath)

                    rsp_file = open(rsp_filepath, "r")
                    rsp_contents = rsp_file.readlines()
                    rsp_file.close()

                    original_input_filepath = ""

                    for line in rsp_contents:
                        if not line.startswith("-"):
                            original_input_filepath = line

                    if original_input_filepath == "":
                        raise RuntimeError(f"Unable to find input file in {original_rsp_filepath}")

                    input_filename = os.path.basename(original_input_filepath)

                    # The ilc.rsp files are stored in the 'native' folders, ex: 'ComWrappers/ComWrappers/native/ComWrappers.ilc.rsp'
                    test_native_directory = os.path.abspath(os.path.join(original_rsp_filepath, "..")) # ex: 'ComWrappers/ComWrappers/native/'
                    test_output_directory = os.path.join(test_native_directory, "..") # ex: 'ComWrappers/ComWrappers/'
                    test_directory = os.path.join(test_output_directory, "..") # ex: 'ComWrappers/'

                    # The corresponding input assemblies are in the folder above 'native', ex: 'ComWrappers/ComWrappers/'
                    test_input_filepath = os.path.join(test_output_directory, input_filename)

                    # Blow away references, output, input assembly and directpinvokelist as we are going to use new ones.
                    rsp_file = open(rsp_filepath, "w")
                    def filter_rsp_argument(line):
                        if line.startswith("-r:") or line.startswith("-o:") or not line.startswith("-") or line.startswith("--directpinvokelist:"):
                            return False
                        else:
                            return True
                    rsp_contents = list(filter(filter_rsp_argument, rsp_contents))

                    # Fix-up paths for files located in the test's directories.
                    def map_rsp_argument(line):
                        if line.startswith("--exportsfile:"):
                            arg_path = os.path.join(test_native_directory, os.path.basename(line[len("--exportsfile:"):]))
                            return f"--exportsfile:{arg_path}"
                        elif line.startswith("--descriptor:"):
                            arg_path = os.path.join(test_directory, os.path.basename(line[len("--descriptor:"):]))
                            return f"--descriptor:{arg_path}"
                        elif line.startswith("--substitution:"):
                            arg_path = os.path.join(test_directory, os.path.basename(line[len("--substitution:"):]))
                            return f"--substitution:{arg_path}"
                        else:
                            return line
                    rsp_contents = list(map(map_rsp_argument, rsp_contents))

                    rsp_file.writelines(rsp_contents)
                    rsp_file.close()

                    nativeaot_output_filename = os.path.splitext(input_filename)[0]
                    if self.coreclr_args.host_os.lower() == "windows":
                        root_nativeaot_output_filename = make_safe_filename("nativeaot_" + nativeaot_output_filename) + ".out.obj"
                    else:
                        root_nativeaot_output_filename = make_safe_filename("nativeaot_" + nativeaot_output_filename) + ".out.a"
                    nativeaot_output_assembly_filename = os.path.join(self.temp_location, root_nativeaot_output_filename)
                    try:
                        if os.path.exists(nativeaot_output_assembly_filename):
                            os.remove(nativeaot_output_assembly_filename)
                    except OSError as ose:
                        if "[WinError 32] The process cannot access the file because it is being used by another " \
                           "process:" in format(ose):
                            logging.warning("Skipping file %s. Got error: %s", nativeaot_output_assembly_filename, ose)
                            return
                        else:
                            raise ose

                    root_output_filename = make_safe_filename("nativeaot_" + nativeaot_output_filename + "_")

                    with open(rsp_filepath, "a") as rsp_write_handle:
                        rsp_write_handle.write(test_input_filepath + "\n")
                        if self.coreclr_args.host_os.lower() == "windows":
                            rsp_write_handle.write("--directpinvokelist:" + os.path.join(self.core_root, "build", "WindowsAPIs.txt") + "\n")
                        rsp_write_handle.write("-o:" + nativeaot_output_assembly_filename + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.coreclr_args.nativeaot_aotsdk_path, "System.*.dll") + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "System.*.dll") + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "Microsoft.*.dll") + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "mscorlib.dll") + "\n")
                        rsp_write_handle.write("-r:" + os.path.join(self.core_root, "netstandard.dll") + "\n")
                        rsp_write_handle.write("--jitpath:" + os.path.join(self.core_root, self.collection_shim_name) + "\n")
                        for var, value in dotnet_env.items():
                            rsp_write_handle.write("--codegenopt:" + var + "=" + value + "\n")

                    # Log what is in the response file
                    write_file_to_log(rsp_filepath)

                    command = [self.coreclr_args.nativeaot_tool_path, "@" + rsp_filepath]
                    command_string = " ".join(command)
                    logging.debug("%s%s", print_prefix, command_string)

                    begin_time = datetime.datetime.now()

                    try:
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

                        return_code = proc.returncode
                        if return_code != 0:
                            logging.debug("'%s': Error return code: %s", command_string, return_code)
                            write_file_to_log(stdout_filepath, log_level=logging.DEBUG)

                        write_file_to_log(stderr_filepath, log_level=logging.DEBUG)
                    except OSError as ose:
                        if "[WinError 32] The process cannot access the file because it is being used by another " \
                           "process:" in format(ose):
                            logging.warning("Skipping file %s. Got error: %s", root_output_filename, ose)
                        else:
                            raise ose

                    # Delete the response file unless we are skipping cleanup
                    if not self.coreclr_args.skip_cleanup:
                        os.remove(rsp_filepath)

                    elapsed_time = datetime.datetime.now() - begin_time
                    logging.debug("%sDone. Elapsed time: %s", print_prefix, elapsed_time)

                # Set environment variables.
                nativeaot_command_env = env_copy.copy()
                set_and_report_env(nativeaot_command_env, root_env)

                old_env = os.environ.copy()
                os.environ.update(nativeaot_command_env)

                ilc_rsps = list(filter(lambda ilc_rsp: ilc_rsp.endswith(".ilc.rsp"), ilc_rsps))
                helper = AsyncSubprocessHelper(ilc_rsps, verbose=True)
                helper.run_to_completion(run_nativeaot, self)

                os.environ.clear()
                os.environ.update(old_env)
            ################################################################################################ end of "self.coreclr_args.nativeaot is True"

        mc_files = [os.path.join(self.temp_location, item) for item in os.listdir(self.temp_location) if item.endswith(".mc")]
        if len(mc_files) == 0:
            raise RuntimeError("No .mc files generated.")

    def __merge_mc_files__(self):
        """ Merge the mc files that were generated

        Notes:
            mcs -merge <s_baseMchFile> <s_tempDir>\\*.mc -recursive -dedup -thin

        """

        logging.info("Merging MC files")

        pattern = os.path.join(self.temp_location, "*.mc")

        command = [self.mcs_path, "-merge", self.base_mch_file, pattern, "-recursive", "-dedup", "-thin"]
        run_and_log(command)

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

        logging.info("Merging MCH files")

        for item in self.coreclr_args.mch_files:
            command = [self.mcs_path, "-concat", self.base_mch_file, item]
            run_and_log(command)

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

        logging.info("Cleaning MCH file")

        command = [self.superpmi_path, "-p", "-f", self.base_fail_mcl_file, self.base_mch_file, self.jit_path]
        run_and_log(command)

        if is_nonzero_length_file(self.base_fail_mcl_file):
            command = [self.mcs_path, "-strip", self.base_fail_mcl_file, self.base_mch_file, self.final_mch_file]
            run_and_log(command)
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

    def __copy_to_final_mch_file__(self):
        """ When not cleaning the MCH file, copy the base MCH file to the final MCH file location.

        Notes:
            # copy/move base file to final file
            del <s_baseFailMclFile>
        """

        logging.info("Copy base MCH file to final MCH file")

        shutil.copy2(self.base_mch_file, self.final_mch_file)

        if not os.path.isfile(self.final_mch_file):
            raise RuntimeError("Final mch file failed to be generated.")

        if not self.coreclr_args.skip_cleanup:
            if os.path.isfile(self.base_mch_file):
                os.remove(self.base_mch_file)
                self.base_mch_file = None

    def __create_toc__(self):
        """ Create a TOC file

        Notes:
            <mcl> -toc <s_finalMchFile>
        """

        logging.info("Creating TOC file")

        command = [self.mcs_path, "-toc", self.final_mch_file]
        run_and_log(command)

        if not os.path.isfile(self.toc_file):
            raise RuntimeError("Error, toc file not created correctly at: %s" % self.toc_file)

    def __verify_final_mch__(self):
        """ Verify the resulting MCH file is error-free when running SuperPMI against it with the same JIT used for collection.

        Notes:
            <SuperPmiPath> -p -f <s_finalFailMclFile> <s_finalMchFile> <jitPath>
        """

        logging.info("Verifying MCH file")

        mch_files = [ self.final_mch_file ]
        spmi_replay = SuperPMIReplay(self.coreclr_args, mch_files, self.jit_path)
        passed = spmi_replay.replay()

        if not passed:
            raise RuntimeError("Error, unclean replay.")

    def __process_for_ci__(self):
        """ Helix doesn't upload zero-sized files. Sometimes we end up with zero-sized .mch files if
            there is no data collected. Convert these to special "sentinel" files that are later processed
            by "merge-mch" by deleting them. This prevents the <HelixWorkItem.DownloadFilesFromResults>
            file download to succeed (because the file exists) and the MCH merge to also succeed.
        """

        logging.info("Process MCH files for CI")

        if os.path.getsize(self.final_mch_file) == 0:
            # Convert to sentinel file
            logging.info("Converting zero-length MCH file {} to ZEROLENGTH sentinel file".format(self.final_mch_file))
            with open(self.final_mch_file, "w") as write_fh:
                write_fh.write("ZEROLENGTH")

################################################################################
# SuperPMI Replay helpers
################################################################################


def print_superpmi_result(return_code, coreclr_args, base_metrics, diff_metrics):
    """ Print a description of a superpmi return (error) code. If the return code is
        zero, meaning success, don't print anything.
        Note that Python treats process return codes (at least on Windows) as
        unsigned integers, so compare against both signed and unsigned numbers for
        those return codes.
    """
    if return_code == 0:
        logging.info("Clean SuperPMI {} ({} contexts processed)".format("replay" if diff_metrics is None else "diff", base_metrics["Overall"]["Successful compiles"]))
    elif return_code == -1 or return_code == 4294967295:
        logging.error("General fatal error")
    elif return_code == -2 or return_code == 4294967294:
        logging.error("JIT failed to initialize")
    elif return_code == 1:
        logging.warning("Compilation failures")
    elif return_code == 2:
        logging.warning("Asm diffs found")
    elif return_code == 3:
        missing_base = base_metrics["Overall"]["Missing compiles"]
        total_contexts = missing_base + base_metrics["Overall"]["Successful compiles"] + base_metrics["Overall"]["Failing compiles"]

        if diff_metrics is None:
            logging.warning("SuperPMI encountered missing data for {} out of {} contexts".format(missing_base, total_contexts))
        else:
            missing_diff = diff_metrics["Overall"]["Missing compiles"]
            if missing_base == missing_diff:
                logging.warning("SuperPMI encountered missing data for {} out of {} contexts".format(missing_base, total_contexts))
            else:
                logging.warning("SuperPMI encountered missing data. Missing with base JIT: {}. Missing with diff JIT: {}. Total contexts: {}.".format(missing_base, missing_diff, total_contexts))

    elif return_code == 139 and coreclr_args.host_os != "windows":
        logging.error("Fatal error, SuperPMI has returned SIGSEGV (segmentation fault)")
    else:
        logging.error("Unknown error code %s", return_code)


def print_fail_mcl_file_method_numbers(fail_mcl_file):
    """ Given a SuperPMI ".mcl" file (containing a list of failure indices), print out the method numbers.
    """
    with open(fail_mcl_file) as file_handle:
        mcl_lines = file_handle.readlines()
        mcl_lines = [item.strip() for item in mcl_lines]
        logging.debug("Method numbers with compilation failures:")
        for line in mcl_lines:
            logging.debug(line)


def save_repro_mc_files(temp_location, coreclr_args, artifacts_base_name, repro_base_command_line):
    """ For commands that use the superpmi "-r" option to create "repro" .mc files, copy these to a
        location where they are saved (and not in a "temp" directory) for easy use by the user.
    """
    # If there are any .mc files, drop them into artifacts/repro/<host_os>.<arch>.<build_type>/*.mc
    mc_files = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if item.endswith(".mc")]
    if len(mc_files) > 0:
        repro_location = create_unique_directory_name(coreclr_args.spmi_location, "repro.{}".format(artifacts_base_name))

        repro_files = []
        for item in mc_files:
            repro_files.append(os.path.join(repro_location, os.path.basename(item)))
            logging.debug("Moving %s -> %s", item, repro_location)
            shutil.move(item, repro_location)

        logging.info("")
        logging.info("Repro {} .mc file(s) created for failures:".format(len(repro_files)))
        for item in repro_files:
            logging.info(item)

        logging.info("")
        logging.info("To run a specific failure (replace JIT path and .mc filename as needed):")
        logging.info("")
        logging.info("%s %s%sxxxxx.mc", repro_base_command_line, repro_location, os.path.sep)
        logging.info("")


def parse_replay_asserts(mch_file, replay_output):
    """ Parse output from failed replay, looking for asserts and correlating them to provide the best
        repro scenarios.

        Look for lines like:

        ISSUE: <ASSERT> #242532 C:\gh\runtime4\src\coreclr\jit\fgdiagnostic.cpp (2810) - Assertion failed '!"Jump into middle of try region"' in 'Test.AA:TestEntryPoint()' during 'Compute blocks reachability' (IL size 35; hash 0x367fd548; FullOpts)

        Create a map of all unique asserts: <assert text> => <array of instances>
        where an instance is a dictionary of method context number and IL size.
        Sort the instances by increasing IL size (smallest first). The idea is that
        reproducing a failure on the smallest IL size is preferable.

    Return:
        Returns the dictionary of asserts
    """
    asserts = {}
    for line in replay_output:
        match = re.search(r'ISSUE: <ASSERT> #([0-9]+) (.*) \(([0-9]*)\) - Assertion failed \'(.*)\' in \'.*\' during \'.*\' \(IL size ([0-9]*); hash .*\)', line)
        if match is not None:
            mc_num = match.group(1)
            filename = match.group(2)
            line_num = match.group(3)
            assertion_text = match.group(4)
            il_size = match.group(5)
            assertion_key = "{} ({}:{})".format(assertion_text, filename, line_num)
            assertion_value = {'il': int(il_size), 'mch_file': mch_file, 'mc_num': mc_num}
            if assertion_key in asserts:
                asserts[assertion_key].append(assertion_value)
            else:
                asserts[assertion_key] = [assertion_value]

    return asserts


def add_to_all_asserts(all_asserts, asserts):
    """ Add the newly found `asserts` to the set of asserts for all MCH files.
    """
    if asserts:
        for assertion_key, assertion_value in asserts.items():
            if assertion_key in all_asserts:
                all_asserts[assertion_key].extend(assertion_value)
            else:
                all_asserts[assertion_key] = assertion_value


def report_replay_asserts(asserts, output_mch_file):
    """ Report the replay asserts previously parsed by parse_replay_asserts.

        Output each assert followed by a list of method contexts and IL size.

    Args:
        asserts (dict): dictionary of asserts in the format produced by parse_replay_asserts
        output_mch_file (bool): True to output the MCH file name as well
    """

    if asserts:
        logging.info("============================== Assertions:")
        for assertion_key, assertion_value in asserts.items():
            logging.info("%s", assertion_key)
            # Sort the values by increasing il size
            sorted_instances = sorted(assertion_value, key=lambda d: d['il'])
            for instance in sorted_instances:
                if output_mch_file:
                    logging.info("  %s # %s : IL size %s", instance['mch_file'], instance['mc_num'], instance['il'])
                else:
                    logging.info("  # %s : IL size %s", instance['mc_num'], instance['il'])


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

        result = True  # Assume success

        with TempDir() as temp_location:
            logging.debug("")
            logging.debug("Temp Location: %s", temp_location)
            logging.debug("")

            # `repro_flags` are the subset of flags we tell the user to pass to superpmi when reproducing
            # a failure. This won't include things like "-p" for parallelism or "-r" to create a repro .mc file.
            repro_flags = []

            common_flags = [
                "-v", "ewi",  # display errors, warnings, missing, jit info
                "-r", os.path.join(temp_location, "repro") # Repro name prefix, create .mc repro files
            ]

            if self.coreclr_args.altjit:
                repro_flags += [
                    "-jitoption", "force", "AltJit=*",
                    "-jitoption", "force", "AltJitNgen=*"
                ]

            if self.coreclr_args.arch != self.coreclr_args.target_arch:
                repro_flags += [ "-target", self.coreclr_args.target_arch ]

            if not self.coreclr_args.sequential and not self.coreclr_args.compile:
                common_flags += [ "-p" ]

            if self.coreclr_args.break_on_assert:
                common_flags += [ "-boa" ]

            if self.coreclr_args.break_on_error:
                common_flags += [ "-boe" ]

            if self.coreclr_args.compile:
                common_flags += [ "-c", self.coreclr_args.compile ]

            if self.coreclr_args.spmi_log_file is not None:
                common_flags += [ "-w", self.coreclr_args.spmi_log_file ]

            if self.coreclr_args.error_limit is not None:
                common_flags += ["-failureLimit", self.coreclr_args.error_limit]

            if self.coreclr_args.jitoption:
                for o in self.coreclr_args.jitoption:
                    repro_flags += "-jitoption", o

            common_flags += repro_flags

            # For each MCH file that we are going to replay, do the replay and replay post-processing.
            #
            # Consider: currently, we loop over all the steps for each MCH file, including (1) invoke
            # SuperPMI, (2) process results. It might be better to do (1) for each MCH file, then
            # process all the results at once. Currently, the results for some processing can be
            # obscured by the normal run output for subsequent MCH files.

            # Keep track of any MCH file replay failures
            files_with_replay_failures = []

            # Keep track of all asserts
            all_asserts = {}

            for mch_file in self.mch_files:

                logging.info("Running SuperPMI replay of %s", mch_file)

                flags = common_flags.copy()

                fail_mcl_file = os.path.join(temp_location, os.path.basename(mch_file) + "_fail.mcl")
                details_info_file = os.path.join(temp_location, os.path.basename(mch_file) + "_details.csv")

                flags += [
                    "-f", fail_mcl_file,  # Failing mc List
                    "-details", details_info_file
                ]

                command = [self.superpmi_path] + flags + [self.jit_path, mch_file]
                (return_code, replay_output) = run_and_log_return_output(command)

                details = read_csv(details_info_file)
                print_superpmi_result(return_code, self.coreclr_args, self.aggregate_replay_metrics(details), None)

                if return_code != 0:
                    # Don't report as replay failure missing data (return code 3).
                    # Anything else, such as compilation failure (return code 1, typically a JIT assert) will be
                    # reported as a replay failure.
                    if return_code != 3:
                        result = False
                        files_with_replay_failures.append(mch_file)
                        asserts = parse_replay_asserts(mch_file, replay_output)
                        report_replay_asserts(asserts, output_mch_file=True)
                        add_to_all_asserts(all_asserts, asserts)

                        if is_nonzero_length_file(fail_mcl_file):
                            # Unclean replay. Examine the contents of the fail.mcl file to dig into failures.
                            if return_code == 0:
                                logging.warning("Warning: SuperPMI returned a zero exit code, but generated a non-zero-sized mcl file")
                            print_fail_mcl_file_method_numbers(fail_mcl_file)
                            repro_base_command_line = "{} {} {}".format(self.superpmi_path, " ".join(repro_flags), self.jit_path)
                            artifacts_base_name = create_artifacts_base_name(self.coreclr_args, mch_file)
                            save_repro_mc_files(temp_location, self.coreclr_args, artifacts_base_name, repro_base_command_line)

                if not self.coreclr_args.skip_cleanup:
                    if os.path.isfile(fail_mcl_file):
                        os.remove(fail_mcl_file)
                    fail_mcl_file = None
            ################################################################################################ end of for mch_file in self.mch_files

        report_replay_asserts(all_asserts, output_mch_file=True)

        logging.info("Replay summary:")

        if len(files_with_replay_failures) == 0:
            logging.info("  All replays clean")
        else:
            logging.info("  Replay failures in %s MCH files:", len(files_with_replay_failures))
            for file in files_with_replay_failures:
                logging.info("    %s", file)

        return result

    def aggregate_replay_metrics(self, details):
        """ Given the CSV details file output by SPMI for a replay aggregate the
        successes, misses and failures

        Returns:
            A dictionary of metrics
        """

        num_successes = 0
        num_misses = 0
        num_failures = 0
        for row in details:
            result = row["Result"]
            if result == "Success":
                num_successes += 1
            elif result == "Miss":
                num_misses += 1
            else:
                assert(result == "Error")
                num_failures += 1

        return {"Overall": {"Successful compiles": num_successes, "Missing compiles": num_misses, "Failing compiles": num_failures}}

################################################################################
# SuperPMI Replay/AsmDiffs
################################################################################

def html_color(color, text):
    return "<span style=\"color:{}\">{}</span>".format(color, text)

def calculate_improvements_regressions(base_diff_sizes):
    num_improvements = sum(1 for (base_size, diff_size) in base_diff_sizes if diff_size < base_size)
    num_regressions = sum(1 for (base_size, diff_size) in base_diff_sizes if diff_size > base_size)
    num_same = sum(1 for (base_size, diff_size) in base_diff_sizes if diff_size == base_size)

    byte_improvements = sum(max(0, base_size - diff_size) for (base_size, diff_size) in base_diff_sizes)
    byte_regressions = sum(max(0, diff_size - base_size) for (base_size, diff_size) in base_diff_sizes)

    return (num_improvements, num_regressions, num_same, byte_improvements, byte_regressions)

def format_delta(base, diff):
    plus_if_positive = "+" if diff >= base else ""
    text = "{}{:,d}".format(plus_if_positive, diff - base)

    if diff != base:
        color = "red" if diff > base else "green"
        return html_color(color, text)

    return text

def compute_pct(base, diff):
    if base > 0:
        return (diff - base) / base * 100
    else:
        return 0.0

def format_pct(pct, num_decimals = 2):
    plus_if_positive = "+" if pct > 0 else ""

    text = "{}{:.{prec}f}%".format(plus_if_positive, pct, prec=num_decimals)
    if pct != 0:
        color = "red" if pct > 0 else "green"
        return html_color(color, text)

    return text

def compute_and_format_pct(base, diff):
    return format_pct(compute_pct(base, diff))

def write_jit_options(coreclr_args, write_fh):
    """ If any custom JIT options are specified then write their values out to a summmary

    Args:
        coreclr_args: args class instance
        write_fh: file to output to
    
    """
    base_options = []
    diff_options = []
    
    if coreclr_args.jitoption:
        base_options += coreclr_args.jitoption
        diff_options += coreclr_args.jitoption

    if coreclr_args.base_jit_option:
        base_options += coreclr_args.base_jit_option

    if coreclr_args.diff_jit_option:
        diff_options += coreclr_args.diff_jit_option

    if len(base_options) > 0:
        write_fh.write("Base JIT options: {}\n\n".format(";".join(base_options)))

    if len(diff_options) > 0:
        write_fh.write("Diff JIT options: {}\n\n".format(";".join(diff_options)))

class DetailsSection:
    def __init__(self, write_fh, summary_text):
        self.write_fh = write_fh
        self.summary_text = summary_text

    def __enter__(self):
        self.write_fh.write("\n<details>\n")
        self.write_fh.write("<summary>{}</summary>\n".format(self.summary_text))
        self.write_fh.write('<div style="margin-left:1em">\n\n')

    def __exit__(self, *args):
        self.write_fh.write("\n\n</div></details>\n")

def aggregate_diff_metrics(details):
    """ Given the CSV details file output by SPMI for a diff aggregate the metrics.
    """

    base_minopts = {"Successful compiles": 0, "Missing compiles": 0, "Failing compiles": 0,
                    "Contexts with diffs": 0, "Diffed code bytes": 0,
                    "Diffed PerfScore" : 0.0, "Relative PerfScore Geomean": 0.0,
                    "Diff executed instructions": 0, "Diffed contexts": 0}
    base_fullopts = base_minopts.copy()

    diff_minopts = base_minopts.copy()
    diff_fullopts = base_minopts.copy()

    for row in details:
        base_result = row["Base result"]

        if row["MinOpts"] == "True":
            base_dict = base_minopts
            diff_dict = diff_minopts
        else:
            base_dict = base_fullopts
            diff_dict = diff_fullopts

        if base_result == "Success":
            base_dict["Successful compiles"] += 1
        elif base_result == "Miss":
            base_dict["Missing compiles"] += 1
        else:
            assert(base_result == "Error")
            base_dict["Failing compiles"] += 1

        diff_result = row["Diff result"]
        if diff_result == "Success":
            diff_dict["Successful compiles"] += 1
        elif diff_result == "Miss":
            diff_dict["Missing compiles"] += 1
        else:
            assert(diff_result == "Error")
            diff_dict["Failing compiles"] += 1

        if base_result == "Success" and diff_result == "Success":
            base_size = int(row["Base size"])
            diff_size = int(row["Diff size"])
            base_dict["Diffed code bytes"] += base_size
            diff_dict["Diffed code bytes"] += diff_size

            base_insts = int(row["Base instructions"])
            diff_insts = int(row["Diff instructions"])
            base_dict["Diff executed instructions"] += base_insts
            diff_dict["Diff executed instructions"] += diff_insts

            base_perfscore = float(row["Base PerfScore"])
            diff_perfscore = float(row["Diff PerfScore"])
            base_dict["Diffed PerfScore"] += base_perfscore
            diff_dict["Diffed PerfScore"] += diff_perfscore

            if base_perfscore > 0:
                log_relative_perfscore = math.log(diff_perfscore / base_perfscore)
                base_dict["Relative PerfScore Geomean"] += log_relative_perfscore
                diff_dict["Relative PerfScore Geomean"] += log_relative_perfscore

            base_dict["Diffed contexts"] += 1
            diff_dict["Diffed contexts"] += 1

            if row["Has diff"] == "True":
                base_dict["Contexts with diffs"] += 1
                diff_dict["Contexts with diffs"] += 1

    base_overall = base_minopts.copy()
    for k in base_overall.keys():
        base_overall[k] += base_fullopts[k]

    diff_overall = diff_minopts.copy()
    for k in diff_overall.keys():
        diff_overall[k] += diff_fullopts[k]

    for d in [base_overall, base_minopts, base_fullopts, diff_overall, diff_minopts, diff_fullopts]:
        sum_of_logs = d["Relative PerfScore Geomean"]
        if d["Diffed contexts"] > 0:
            d["Relative PerfScore Geomean"] = math.exp(sum_of_logs / d["Diffed contexts"])
        else:
            d["Relative PerfScore Geomean"] = 1

        if d["Contexts with diffs"] > 0:
            d["Relative PerfScore Geomean (Diffs)"] = math.exp(sum_of_logs / d["Contexts with diffs"])
        else:
            d["Relative PerfScore Geomean (Diffs)"] = 1

    return ({"Overall": base_overall, "MinOpts": base_minopts, "FullOpts": base_fullopts},
            {"Overall": diff_overall, "MinOpts": diff_minopts, "FullOpts": diff_fullopts})


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
        self.diff_mcl_contents = None

    ############################################################################
    # Instance Methods
    ############################################################################

    def replay_with_asm_diffs(self):
        """ Replay the given SuperPMI collection, generating asm diffs

        Returns:
            (bool) True on success; False otherwise
        """

        result = True  # Assume success

        # Set up some settings we'll use below.

        # These vars are force overridden in the SPMI runs for both the base and diff, always.
        replay_vars = {
            "DOTNET_JitEnableNoWayAssert": "1",
            "DOTNET_JitNoForceFallback": "1",
        }

        if not self.coreclr_args.alignloops:
            replay_vars.update({
                "DOTNET_JitAlignLoops": "0" })

        # These vars are used in addition to the replay vars when creating disassembly for the interesting contexts
        asm_dotnet_vars = {
            "DOTNET_JitDisasm": "*",
            "DOTNET_JitUnwindDump": "*",
            "DOTNET_JitEHDump": "*",
            "DOTNET_JitDisasmDiffable": "1",
            "DOTNET_JitDisasmWithGC": "1"
        }

        if self.coreclr_args.gcinfo:
            asm_dotnet_vars.update({
                "DOTNET_JitGCDump": "*" })

        if self.coreclr_args.debuginfo:
            asm_dotnet_vars.update({
                "DOTNET_JitDebugDump": "*",
                "DOTNET_JitDisasmWithDebugInfo": "1" })

        jit_dump_dotnet_vars = asm_dotnet_vars.copy()
        jit_dump_dotnet_vars.update({
            "DOTNET_JitDump": "*" })

        asm_dotnet_vars_full_env = os.environ.copy()
        asm_dotnet_vars_full_env.update(asm_dotnet_vars)

        jit_dump_dotnet_vars_full_env = os.environ.copy()
        jit_dump_dotnet_vars_full_env.update(jit_dump_dotnet_vars)

        target_flags = []
        if self.coreclr_args.arch != self.coreclr_args.target_arch:
            target_flags += [ "-target", self.coreclr_args.target_arch ]

        altjit_asm_diffs_flags = target_flags
        altjit_replay_flags = target_flags

        base_option_flags = []
        base_option_flags_for_diff_artifact = []
        diff_option_flags = []
        diff_option_flags_for_diff_artifact = []

        for key, val in replay_vars.items():
            assert(key.startswith("DOTNET_"))
            name = key[len("DOTNET_"):]
            base_option_flags += [ "-jitoption", "force", name + "=" + val ]
            base_option_flags_for_diff_artifact += [ "-jitoption", "force", name + "=" + val ]
            diff_option_flags += [ "-jit2option", "force", name + "=" + val ]
            diff_option_flags_for_diff_artifact += [ "-jitoption", "force", name + "=" + val ]

        if self.coreclr_args.base_jit_option:
            for o in self.coreclr_args.base_jit_option:
                base_option_flags += "-jitoption", o
                base_option_flags_for_diff_artifact += "-jitoption", o
        if self.coreclr_args.jitoption:
            for o in self.coreclr_args.jitoption:
                base_option_flags += "-jitoption", o
                base_option_flags_for_diff_artifact += "-jitoption", o

        if self.coreclr_args.diff_jit_option:
            for o in self.coreclr_args.diff_jit_option:
                diff_option_flags += "-jit2option", o
                diff_option_flags_for_diff_artifact += "-jitoption", o
        if self.coreclr_args.jitoption:
            for o in self.coreclr_args.jitoption:
                diff_option_flags += "-jit2option", o
                diff_option_flags_for_diff_artifact += "-jitoption", o

        if self.coreclr_args.altjit:
            altjit_asm_diffs_flags += [
                "-jitoption", "force", "AltJit=*",
                "-jitoption", "force", "AltJitNgen=*",
                "-jit2option", "force", "AltJit=*",
                "-jit2option", "force", "AltJitNgen=*"
            ]

            altjit_replay_flags += [
                "-jitoption", "force", "AltJit=*",
                "-jitoption", "force", "AltJitNgen=*"
            ]

        # Keep track if any MCH file replay had asm diffs
        files_with_asm_diffs = []
        files_with_replay_failures = []

        # List of all Markdown summary files
        asm_diffs = []

        with TempDir(None, self.coreclr_args.skip_cleanup) as temp_location:
            logging.debug("")
            logging.debug("Temp Location: %s", temp_location)
            logging.debug("")

            # For each MCH file that we are going to replay, do the replay and replay post-processing.
            #
            # Consider: currently, we loop over all the steps for each MCH file, including (1) invoke
            # SuperPMI, (2) process results. It might be better to do (1) for each MCH file, then
            # process all the results at once. Currently, the results for some processing can be
            # obscured by the normal run output for subsequent MCH files.

            for mch_file in self.mch_files:

                logging.info("Running asm diffs of %s", mch_file)

                fail_mcl_file = os.path.join(temp_location, os.path.basename(mch_file) + "_fail.mcl")
                detailed_info_file = os.path.join(temp_location, os.path.basename(mch_file) + "_details.csv")

                flags = [
                    "-a",  # Asm diffs
                    "-v", "ewi",  # display errors, warnings, missing, jit info
                    "-f", fail_mcl_file,  # Failing mc List
                    "-details", detailed_info_file,  # Detailed information about each context
                    "-r", os.path.join(temp_location, "repro"),  # Repro name prefix, create .mc repro files
                ]
                flags += altjit_asm_diffs_flags
                flags += base_option_flags
                flags += diff_option_flags

                if not self.coreclr_args.sequential and not self.coreclr_args.compile:
                    flags += [ "-p" ]

                if self.coreclr_args.break_on_assert:
                    flags += [ "-boa" ]

                if self.coreclr_args.break_on_error:
                    flags += [ "-boe" ]

                if self.coreclr_args.compile:
                    flags += [ "-c", self.coreclr_args.compile ]

                if self.coreclr_args.spmi_log_file is not None:
                    flags += [ "-w", self.coreclr_args.spmi_log_file ]

                if self.coreclr_args.error_limit is not None:
                    flags += ["-failureLimit", self.coreclr_args.error_limit]

                if self.coreclr_args.diff_with_release:
                    # If one of the JITs is Release, ignore all the stored configuration variables.
                    # This isn't necessary if both are Release builds (and, in fact, it can clear variables
                    # that both Release compilers can interpret). However, we rarely or never compare
                    # two Release compilers, so this is safest.
                    flags += [ "-ignoreStoredConfig" ]

                # Change the working directory to the Core_Root we will call SuperPMI from.
                # This is done to allow libcoredistools to be loaded correctly on unix
                # as the loadlibrary path will be relative to the current directory.
                with ChangeDir(self.coreclr_args.core_root):
                    command = [self.superpmi_path] + flags + [self.base_jit_path, self.diff_jit_path, mch_file]
                    return_code = run_and_log(command)

                details = read_csv(detailed_info_file)
                (base_metrics, diff_metrics) = aggregate_diff_metrics(details)

                print_superpmi_result(return_code, self.coreclr_args, base_metrics, diff_metrics)
                artifacts_base_name = create_artifacts_base_name(self.coreclr_args, mch_file)

                if return_code != 0:

                    # Don't report as replay failure asm diffs (return code 2) if not checking diffs with Release build or missing data (return code 3).
                    # Anything else, such as compilation failure (return code 1, typically a JIT assert) will be
                    # reported as a replay failure.
                    if (return_code != 2 or self.coreclr_args.diff_with_release) and return_code != 3:
                        result = False
                        files_with_replay_failures.append(mch_file)

                        if is_nonzero_length_file(fail_mcl_file):
                            # Unclean replay. Examine the contents of the fail.mcl file to dig into failures.
                            print_fail_mcl_file_method_numbers(fail_mcl_file)
                            repro_base_command_line = "{} {} {}".format(self.superpmi_path, " ".join(altjit_asm_diffs_flags), self.diff_jit_path)
                            save_repro_mc_files(temp_location, self.coreclr_args, artifacts_base_name, repro_base_command_line)

                diffs = [r for r in details if r["Has diff"] == "True"]
                if any(diffs):
                    files_with_asm_diffs.append(mch_file)

                # There were diffs. Go through each method that created diffs and
                # create a base/diff asm file with diffable asm so we can analyze
                # it. In addition, create a standalone .mc for easy iteration.
                jit_analyze_summary_file = None
                examples_to_put_in_summary = []

                if any(diffs) and not self.coreclr_args.diff_with_release:
                    # AsmDiffs. Save the contents of the fail.mcl file to dig into failures.

                    if return_code == 0:
                        logging.warning("Warning: SuperPMI returned a zero exit code, but generated a non-zero-sized mcl file")

                    asm_root_dir = create_unique_directory_name(self.coreclr_args.spmi_location, "asm.{}".format(artifacts_base_name))
                    base_asm_location = os.path.join(asm_root_dir, "base")
                    diff_asm_location = os.path.join(asm_root_dir, "diff")
                    os.makedirs(base_asm_location)
                    os.makedirs(diff_asm_location)

                    if self.coreclr_args.diff_jit_dump:
                        # If JIT dumps are requested, create a diff and baseline directory for JIT dumps
                        jitdump_root_dir = create_unique_directory_name(self.coreclr_args.spmi_location, "jitdump.{}".format(artifacts_base_name))
                        base_dump_location = os.path.join(jitdump_root_dir, "base")
                        diff_dump_location = os.path.join(jitdump_root_dir, "diff")
                        os.makedirs(base_dump_location)
                        os.makedirs(diff_dump_location)

                    text_differences = queue.Queue()
                    jit_dump_differences = queue.Queue()

                    async def create_replay_artifacts(print_prefix, context_index, self, mch_file, env, jit_differences_queue, base_location, diff_location, extension):
                        """ Run superpmi over an MC to create JIT asm or JIT dumps for the method.
                        """
                        # Setup flags to call SuperPMI for both the diff jit and the base jit

                        flags = ["-c", str(context_index)]
                        flags += altjit_replay_flags

                        # Change the working directory to the core root we will call SuperPMI from.
                        # This is done to allow libcoredistools to be loaded correctly on unix
                        # as the LoadLibrary path will be relative to the current directory.
                        with ChangeDir(self.coreclr_args.core_root):

                            async def create_one_artifact(jit_path: str, location: str, flags) -> str:
                                command = [self.superpmi_path] + flags + [jit_path, mch_file]
                                item_path = os.path.join(location, "{}{}".format(context_index, extension))
                                modified_env = env.copy()
                                modified_env['DOTNET_JitStdOutFile'] = item_path
                                logging.debug("%sGenerating %s", print_prefix, item_path)
                                logging.debug("%sInvoking: %s", print_prefix, " ".join(command))
                                proc = await asyncio.create_subprocess_shell(" ".join(command), stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.PIPE, env=modified_env)
                                (stdout, stderr) = await proc.communicate()

                                def create_exception():
                                    return Exception("Failure while creating JitStdOutFile.\nExit code: {}\nstdout:\n{}\n\nstderr:\n{}".format(proc.returncode, stdout.decode(), stderr.decode()))

                                if proc.returncode != 0:
                                    # No miss/replay failure is expected in contexts that were reported as having diffs since then they succeeded during the diffs run.
                                    raise create_exception()

                                try:
                                    with open(item_path, 'r') as file_handle:
                                        return file_handle.read()
                                except BaseException as err:
                                    raise create_exception() from err

                            # Generate diff and base JIT dumps
                            base_txt = await create_one_artifact(self.base_jit_path, base_location, flags + base_option_flags_for_diff_artifact)
                            diff_txt = await create_one_artifact(self.diff_jit_path, diff_location, flags + diff_option_flags_for_diff_artifact)

                            if base_txt != diff_txt:
                                jit_differences_queue.put_nowait(context_index)
                    ################################################################################################ end of create_replay_artifacts()

                    logging.info("Creating dasm files: %s %s", base_asm_location, diff_asm_location)
                    (dasm_contexts, example_diffs_to_put_in_summary) = self.pick_contexts_to_disassemble(diffs)
                    subproc_helper = AsyncSubprocessHelper(dasm_contexts, verbose=True)
                    subproc_helper.run_to_completion(create_replay_artifacts, self, mch_file, asm_dotnet_vars_full_env, text_differences, base_asm_location, diff_asm_location, ".dasm")

                    for diff in example_diffs_to_put_in_summary:
                        context_num = int(diff["Context"])
                        base_path = os.path.join(base_asm_location, str(context_num) + ".dasm")
                        diff_path = os.path.join(diff_asm_location, str(context_num) + ".dasm")
                        examples_to_put_in_summary.append((diff, base_path, diff_path))

                    if self.coreclr_args.diff_jit_dump:
                        logging.info("Creating JitDump files: %s %s", base_dump_location, diff_dump_location)
                        subproc_helper.run_to_completion(create_replay_artifacts, self, mch_file, jit_dump_dotnet_vars_full_env, jit_dump_differences, base_dump_location, diff_dump_location, ".txt")

                    logging.info("Differences found. To replay SuperPMI use:".format(len(diffs)))

                    logging.info("")
                    for var, value in replay_vars.items():
                        print_platform_specific_environment_vars(logging.INFO, self.coreclr_args, var, value)
                    for var, value in asm_dotnet_vars.items():
                        print_platform_specific_environment_vars(logging.INFO, self.coreclr_args, var, value)
                    logging.info("%s %s -c ### %s %s", self.superpmi_path, " ".join(altjit_replay_flags), self.diff_jit_path, mch_file)
                    logging.info("")

                    if self.coreclr_args.diff_jit_dump:
                        logging.info("To generate JitDump with SuperPMI use:")
                        logging.info("")
                        for var, value in jit_dump_dotnet_vars.items():
                            print_platform_specific_environment_vars(logging.INFO, self.coreclr_args, var, value)
                        logging.info("%s %s -c ### %s %s", self.superpmi_path, " ".join(altjit_replay_flags), self.diff_jit_path, mch_file)
                        logging.info("")

                    if base_metrics is not None and diff_metrics is not None:
                        base_bytes = base_metrics["Overall"]["Diffed code bytes"]
                        diff_bytes = diff_metrics["Overall"]["Diffed code bytes"]
                        logging.info("Total bytes of base: {}".format(base_bytes))
                        logging.info("Total bytes of diff: {}".format(diff_bytes))
                        delta_bytes = diff_bytes - base_bytes
                        logging.info("Total bytes of delta: {} ({:.2%} of base)".format(delta_bytes, delta_bytes / base_bytes))
                        logging.info("")

                        base_perfscore = base_metrics["Overall"]["Diffed PerfScore"]
                        diff_perfscore = diff_metrics["Overall"]["Diffed PerfScore"]
                        logging.info("Total PerfScore of base: {}".format(base_perfscore))
                        logging.info("Total PerfScore of diff: {}".format(diff_perfscore))
                        delta_perfscore = diff_perfscore - base_perfscore
                        logging.info("Total PerfScore of delta: {} ({:.2%} of base)".format(delta_perfscore, delta_perfscore / base_perfscore))
                        logging.info("")

                        relative_perfscore_geomean = diff_metrics["Overall"]["Relative PerfScore Geomean"]
                        logging.info("Relative PerfScore Geomean: {:.4%}".format(relative_perfscore_geomean - 1))
                        relative_perfscore_geomean_diffs = diff_metrics["Overall"]["Relative PerfScore Geomean (Diffs)"]
                        logging.info("Relative PerfScore Geomean (Diffs): {:.4%}".format(relative_perfscore_geomean_diffs - 1))
                        logging.info("")

                    try:
                        current_text_diff = text_differences.get_nowait()
                    except:
                        current_text_diff = None

                    logging.info("Generated asm is located under %s %s", base_asm_location, diff_asm_location)

                    if current_text_diff is not None:
                        logging.info("Textual differences found in generated asm.")

                        # Find jit-analyze on PATH, if it exists, then invoke it.
                        ran_jit_analyze = False
                        path_var = os.environ.get("PATH")
                        if path_var is not None:
                            jit_analyze_file = "jit-analyze.exe" if platform.system() == "Windows" else "jit-analyze"
                            jit_analyze_path = find_file(jit_analyze_file, path_var.split(os.pathsep))
                            if jit_analyze_path is not None:
                                # It appears we have a built jit-analyze on the path, so try to run it.
                                jit_analyze_summary_file = os.path.join(asm_root_dir, "summary.md")
                                command = [ jit_analyze_path, "--md", jit_analyze_summary_file, "-r", "--base", base_asm_location, "--diff", diff_asm_location ]
                                if self.coreclr_args.metrics:
                                    for metric in self.coreclr_args.metrics:
                                        command += [ "--metrics", metric ]

                                    if self.coreclr_args.metrics == ["PerfScore"]:
                                        command += [ "--override-total-base-metric", str(base_perfscore), "--override-total-diff-metric", str(diff_perfscore) ]

                                elif base_bytes is not None and diff_bytes is not None:
                                    command += [ "--override-total-base-metric", str(base_bytes), "--override-total-diff-metric", str(diff_bytes) ]

                                if len(dasm_contexts) < len(diffs):
                                    # Avoid producing analysis output that assumes these are all contexts
                                    # with diffs. When no custom metrics are specified we pick only a subset
                                    # of interesting contexts to disassemble, to avoid spending too long.
                                    # See pick_contexts_to_disassemble.
                                    command += [ "--is-subset-of-diffs" ]
                                else:
                                    # Ask jit-analyze to avoid producing analysis output that assumes these are
                                    # all contexts (we never produce .dasm files for contexts without diffs).
                                    command += [ "--is-diffs-only" ]

                                run_and_log(command, logging.INFO)
                                ran_jit_analyze = True

                        if not ran_jit_analyze:
                            logging.info("jit-analyze not found on PATH. Generate a diff analysis report by building jit-analyze from https://github.com/dotnet/jitutils and running:")
                            logging.info("    jit-analyze -r --base %s --diff %s", base_asm_location, diff_asm_location)

                        if self.coreclr_args.git_diff:
                            asm_diffs_location = os.path.join(asm_root_dir, "asm_diffs.diff")
                            git_diff_command = [ "git", "diff", "--output=" + asm_diffs_location, "--no-index", "--", base_asm_location, diff_asm_location ]
                            git_diff_time = time.time() * 1000
                            git_diff_proc = subprocess.Popen(git_diff_command, stdout=subprocess.PIPE)
                            git_diff_proc.communicate()
                            git_diff_elapsed_time = round((time.time() * 1000) - git_diff_time, 1)
                            git_diff_return_code = git_diff_proc.returncode
                            if git_diff_return_code == 0 or git_diff_return_code == 1: # 0 means no differences and 1 means differences
                                logging.info("Created git diff file at %s in %s ms", asm_diffs_location, git_diff_elapsed_time)
                                logging.info("-------")
                            else:
                                raise RuntimeError("Couldn't create git diff")

                    else:
                        logging.warning("No textual differences. Is this an issue with coredistools?")

                    # If we are not specifying custom metrics then print a summary here, otherwise leave the summarization up to jit-analyze.
                    if self.coreclr_args.metrics is None:
                        base_diff_sizes = [(int(r["Base size"]), int(r["Diff size"])) for r in diffs]

                        (num_improvements, num_regressions, num_same, byte_improvements, byte_regressions) = calculate_improvements_regressions(base_diff_sizes)

                        logging.info("{:,d} contexts with diffs ({:,d} improvements, {:,d} regressions, {:,d} same size)".format(
                            len(diffs),
                            num_improvements,
                            num_regressions,
                            num_same,
                            byte_improvements,
                            byte_regressions))

                        if byte_improvements > 0 and byte_regressions > 0:
                            logging.info("  -{:,d}/+{:,d} bytes".format(byte_improvements, byte_regressions))
                        elif byte_improvements > 0:
                            logging.info("  -{:,d} bytes".format(byte_improvements))
                        elif byte_regressions > 0:
                            logging.info("  +{:,d} bytes".format(byte_regressions))

                        logging.info("")
                        logging.info("")

                    if self.coreclr_args.diff_jit_dump:
                        try:
                            current_jit_dump_diff = jit_dump_differences.get_nowait()
                        except:
                            current_jit_dump_diff = None

                        logging.info("Generated JitDump is located under %s %s", base_dump_location, diff_dump_location)

                        if current_jit_dump_diff is not None:
                            logging.info("Textual differences found in generated JitDump.")
                        else:
                            logging.warning("No textual differences found in generated JitDump. Is this an issue with coredistools?")

                    if base_metrics is not None and diff_metrics is not None:
                        missing_base = base_metrics["Overall"]["Missing compiles"]
                        missing_diff = diff_metrics["Overall"]["Missing compiles"]
                        total_contexts = missing_base + base_metrics["Overall"]["Successful compiles"] + base_metrics["Overall"]["Failing compiles"]

                        if missing_base > 0 or missing_diff > 0:
                            logging.warning("Warning: SuperPMI encountered missing data during the diff. The diff summary printed above may be misleading.")
                            logging.warning("Missing with base JIT: {}. Missing with diff JIT: {}. Total contexts: {}.".format(missing_base, missing_diff, total_contexts))

                ################################################################################################ end of processing asm diffs (if any(diffs)...

                mch_file_basename = os.path.basename(mch_file)
                asm_diffs.append((mch_file_basename, base_metrics, diff_metrics, diffs, jit_analyze_summary_file, examples_to_put_in_summary))

                if not self.coreclr_args.skip_cleanup:
                    if os.path.isfile(fail_mcl_file):
                        os.remove(fail_mcl_file)
                        fail_mcl_file = None

                    if os.path.isfile(detailed_info_file):
                        os.remove(detailed_info_file)
                        detailed_info_file = None

            ################################################################################################ end of for mch_file in self.mch_files

        # Report the overall results summary of the asmdiffs run

        logging.info("Asm diffs summary:")

        # Construct an overall Markdown summary file.

        if any(asm_diffs) and not self.coreclr_args.diff_with_release:
            if not os.path.isdir(self.coreclr_args.spmi_location):
                os.makedirs(self.coreclr_args.spmi_location)

            overall_md_summary_file = create_unique_file_name(self.coreclr_args.spmi_location, "diff_summary", "md")
            if os.path.isfile(overall_md_summary_file):
                os.remove(overall_md_summary_file)

            with open(overall_md_summary_file, "w") as write_fh:
                self.write_asmdiffs_markdown_summary(write_fh, asm_diffs, True)
                logging.info("  Summary Markdown file: %s", overall_md_summary_file)

            short_md_summary_file = create_unique_file_name(self.coreclr_args.spmi_location, "diff_short_summary", "md")
            if os.path.isfile(short_md_summary_file):
                os.remove(short_md_summary_file)

            with open(short_md_summary_file, "w") as write_fh:
                self.write_asmdiffs_markdown_summary(write_fh, asm_diffs, False)
                logging.info("  Short Summary Markdown file: %s", short_md_summary_file)

        # Report the set of MCH files with asm diffs and replay failures.

        if len(files_with_replay_failures) != 0:
            logging.info("  Replay failures in %s MCH files:", len(files_with_replay_failures))
            for file in files_with_replay_failures:
                logging.info("    %s", file)

        if len(files_with_asm_diffs) == 0:
            logging.info("  No asm diffs")
        else:
            logging.info("  Asm diffs in %s MCH files:", len(files_with_asm_diffs))
            for file in files_with_asm_diffs:
                logging.info("    %s", file)

        return result
        ################################################################################################ end of replay_with_asm_diffs()

    def write_asmdiffs_markdown_summary(self, write_fh, asm_diffs, include_details):
        """ Write a markdown summary file of the diffs that were found.

        Args:
            write_fh  : file handle for file to output to
            asm_diffs : list of tuples: (mch_name, base_metrics, diff_metrics, diffs_info, jit_analyze_summary_file, examples_to_put_in_summary)

        """

        def sum_base(row, col):
            return sum(base_metrics[row][col] for (_, base_metrics, _, _, _, _) in asm_diffs)

        def sum_diff(row, col):
            return sum(diff_metrics[row][col] for (_, _, diff_metrics, _, _, _) in asm_diffs)

        diffed_contexts = sum_diff("Overall", "Diffed contexts")
        diffed_minopts_contexts = sum_diff("MinOpts", "Diffed contexts")
        diffed_opts_contexts = sum_diff("FullOpts", "Diffed contexts")
        missing_base_contexts = sum_base("Overall", "Missing compiles")
        missing_diff_contexts = sum_diff("Overall", "Missing compiles")
        total_contexts = missing_base_contexts + sum_base("Overall", "Successful compiles") + sum_base("Overall", "Failing compiles")

        def write_top_context_section():
            num_contexts_color = "#1460aa"
            write_fh.write("Diffs are based on {} contexts ({} MinOpts, {} FullOpts).\n\n".format(
                html_color(num_contexts_color, "{:,d}".format(diffed_contexts)),
                html_color(num_contexts_color, "{:,d}".format(diffed_minopts_contexts)),
                html_color(num_contexts_color, "{:,d}".format(diffed_opts_contexts))))

            if missing_base_contexts > 0 or missing_diff_contexts > 0:
                missed_color = "#d35400"
                if missing_base_contexts == missing_diff_contexts:
                    write_fh.write("{} contexts: {}\n\n".format(
                        html_color(missed_color, "MISSED"),
                        html_color(missed_color, "{:,d} ({:1.2f}%)".format(missing_base_contexts, missing_base_contexts / total_contexts * 100))))
                else:
                    base_color = missed_color if missing_base_contexts > 0 else "green"
                    diff_color = missed_color if missing_diff_contexts > 0 else "green"
                    write_fh.write("{} contexts: base: {}, diff: {}\n\n".format(
                        html_color(missed_color, "MISSED"),
                        html_color(base_color, "{:,d} ({:1.2f}%)".format(missing_base_contexts, missing_base_contexts / total_contexts * 100)),
                        html_color(diff_color, "{:,d} ({:1.2f}%)".format(missing_diff_contexts, missing_diff_contexts / total_contexts * 100))))

            write_jit_options(self.coreclr_args, write_fh)

        def has_diffs(row):
            return row["Contexts with diffs"] > 0

        any_diffs = any(has_diffs(diff_metrics["Overall"]) for (_, _, diff_metrics, _, _, _) in asm_diffs)
        # Exclude entire diffs section?
        if any_diffs:
            def write_pivot_section(row):
                # Exclude this particular Overall/MinOpts/FullOpts table?
                if not any(has_diffs(diff_metrics[row]) for (_, _, diff_metrics, _, _, _) in asm_diffs):
                    return

                sum_base = sum(base_metrics[row]["Diffed code bytes"] for (_, base_metrics, _, _, _, _) in asm_diffs)
                sum_diff = sum(diff_metrics[row]["Diffed code bytes"] for (_, _, diff_metrics, _, _, _) in asm_diffs)

                with DetailsSection(write_fh, "{} ({} bytes)".format(row, format_delta(sum_base, sum_diff))):
                    write_fh.write("|Collection|Base size (bytes)|Diff size (bytes)|Rel PerfScore Geomean|Rel PerfScore Geomean over Diffs\n")
                    write_fh.write("|---|--:|--:|--:|--:|\n")
                    for (mch_file, base_metrics, diff_metrics, _, _, _) in asm_diffs:
                        # Exclude this particular row?
                        if not has_diffs(diff_metrics[row]):
                            continue

                        write_fh.write("|{}|{:,d}|{}|{}|{}|\n".format(
                            mch_file,
                            base_metrics[row]["Diffed code bytes"],
                            format_delta(
                                base_metrics[row]["Diffed code bytes"],
                                diff_metrics[row]["Diffed code bytes"]),
                            format_pct(diff_metrics[row]["Relative PerfScore Geomean"] * 100 - 100, 4),
                            format_pct(diff_metrics[row]["Relative PerfScore Geomean (Diffs)"] * 100 - 100, 4)))

            write_top_context_section()
            write_pivot_section("Overall")
            write_pivot_section("MinOpts")
            write_pivot_section("FullOpts")

            if include_details:
                # Next add a section with example diffs for each collection.
                self.write_example_diffs_to_markdown_summary(write_fh, asm_diffs)
        elif include_details:
            write_top_context_section()
            write_fh.write("No diffs found.\n")

        if include_details:
            # Next write a detailed section
            with DetailsSection(write_fh, "Details"):
                if any_diffs:
                    write_fh.write("#### Improvements/regressions per collection\n\n")
                    write_fh.write("|Collection|Contexts with diffs|Improvements|Regressions|Same size|Improvements (bytes)|Regressions (bytes)|\n")
                    write_fh.write("|---|--:|--:|--:|--:|--:|--:|\n")

                    def write_row(name, diffs):
                        base_diff_sizes = [(int(r["Base size"]), int(r["Diff size"])) for r in diffs]
                        (num_improvements, num_regressions, num_same, byte_improvements, byte_regressions) = calculate_improvements_regressions(base_diff_sizes)
                        write_fh.write("|{}|{:,d}|{}|{}|{}|{}|{}|\n".format(
                            name,
                            len(diffs),
                            html_color("green", "{:,d}".format(num_improvements)),
                            html_color("red", "{:,d}".format(num_regressions)),
                            html_color("blue", "{:,d}".format(num_same)),
                            html_color("green", "-{:,d}".format(byte_improvements)),
                            html_color("red", "+{:,d}".format(byte_regressions))))

                    for (mch_file, _, _, diffs, _, _) in asm_diffs:
                        write_row(mch_file, diffs)

                    if len(asm_diffs) > 1:
                        write_row("", [r for (_, _, _, diffs, _, _) in asm_diffs for r in diffs])

                    write_fh.write("\n---\n\n")

                write_fh.write("#### Context information\n\n")
                write_fh.write("|Collection|Diffed contexts|MinOpts|FullOpts|Missed, base|Missed, diff|\n")
                write_fh.write("|---|--:|--:|--:|--:|--:|\n")

                rows = [(mch_file,
                            diff_metrics["Overall"]["Diffed contexts"],
                            diff_metrics["MinOpts"]["Diffed contexts"],
                            diff_metrics["FullOpts"]["Diffed contexts"],
                            base_metrics["Overall"]["Missing compiles"],
                            diff_metrics["Overall"]["Missing compiles"],
                            base_metrics["Overall"]["Successful compiles"] + base_metrics["Overall"]["Failing compiles"] + base_metrics["Overall"]["Missing compiles"])
                            for (mch_file, base_metrics, diff_metrics, _, _, _) in asm_diffs]

                def write_row(name, diffed_contexts, num_minopts, num_fullopts, num_missed_base, num_missed_diff, total_num_contexts):
                    write_fh.write("|{}|{:,d}|{:,d}|{:,d}|{:,d} ({:1.2f}%)|{:,d} ({:1.2f}%)|\n".format(
                        name,
                        diffed_contexts,
                        num_minopts,
                        num_fullopts,
                        num_missed_base,
                        num_missed_base / total_num_contexts * 100,
                        num_missed_diff,
                        num_missed_diff / total_num_contexts * 100))

                for t in rows:
                    write_row(*t)

                if len(rows) > 1:
                    def sum_row(index):
                        return sum(r[index] for r in rows)

                    write_row("", sum_row(1), sum_row(2), sum_row(3), sum_row(4), sum_row(5), sum_row(6))

                write_fh.write("\n\n")

                if any(has_diff for (_, _, _, has_diff, _, _) in asm_diffs):
                    write_fh.write("---\n\n")
                    write_fh.write("#### jit-analyze output\n")

                    for (mch_file, base_metrics, diff_metrics, has_diffs, jit_analyze_summary_file, _) in asm_diffs:
                        if not has_diffs or jit_analyze_summary_file is None:
                            continue

                        with open(jit_analyze_summary_file, "r") as read_fh:
                            with DetailsSection(write_fh, mch_file):
                                write_fh.write("To reproduce these diffs on Windows {0}:\n".format(self.coreclr_args.arch))
                                write_fh.write("```\n")
                                write_fh.write("superpmi.py asmdiffs -target_os {0} -target_arch {1} -arch {2}\n".format(self.coreclr_args.target_os, self.coreclr_args.target_arch, self.coreclr_args.arch))
                                write_fh.write("```\n\n")

                                shutil.copyfileobj(read_fh, write_fh)

    def write_example_diffs_to_markdown_summary(self, write_fh, asm_diffs):
        """ Write a section with example diffs to the markdown summary.

        Args:
            write_fh  : file handle for file to output to
            asm_diffs : list of tuples: (mch_name, base_metrics, diff_metrics, diffs_info, jit_analyze_summary_file, examples_to_put_in_summary)

        """

        git_exe = "git.exe" if platform.system() == "Windows" else "git"
        path_var = os.environ.get("PATH")
        git_path = find_file(git_exe, path_var.split(os.pathsep)) if path_var is not None else None

        if git_path is None:
            write_fh.write("\nCould not find a git executable in PATH to create example diffs.\n\n")
            return

        with DetailsSection(write_fh, "Example diffs"):
            for (collection_name, _, _, _, _, examples_to_put_in_summary) in asm_diffs:
                if not any(examples_to_put_in_summary):
                    continue

                with DetailsSection(write_fh, collection_name):
                    for (diff, base_dasm_path, diff_dasm_path) in examples_to_put_in_summary:
                        context_num = int(diff["Context"])
                        func_name = str(context_num) + ".dasm"

                        if not os.path.exists(base_dasm_path) or not os.path.exists(diff_dasm_path):
                            write_fh.write("Did not find base/diff .dasm files for context {}; cannot display example diff\n\n".format(context_num))
                            continue

                        with open(base_dasm_path) as f:
                            first_line = f.readline().rstrip()
                            if first_line and first_line.startswith("; Assembly listing for method "):
                                func_name += " - " + first_line[len("; Assembly listing for method "):]

                        git_diff_command = [ git_path, "diff", "--diff-algorithm=histogram", "--no-index", "--", base_dasm_path, diff_dasm_path ]
                        git_diff_proc = subprocess.Popen(git_diff_command, stdout=subprocess.PIPE)
                        (stdout, _) = git_diff_proc.communicate()
                        code = git_diff_proc.returncode
                        diff_lines = stdout.decode().splitlines()
                        diff_lines = diff_lines[4:] # Exclude patch header

                        base_size = int(diff["Base size"])
                        diff_size = int(diff["Diff size"])
                        with DetailsSection(write_fh, "{} ({}) : {}".format(format_delta(base_size, diff_size), compute_and_format_pct(base_size, diff_size), func_name)):
                            if code == 0 or len(diff_lines) <= 0:
                                write_fh.write("No diffs found?\n\n")
                            else:
                                write_fh.write("```diff\n")
                                if len(diff_lines) > 250:
                                    diff_lines = diff_lines[:250]
                                    diff_lines.append("...")

                                for line in diff_lines:
                                    write_fh.write(line)
                                    write_fh.write("\n")

                                write_fh.write("\n```\n")

    def pick_contexts_to_disassemble(self, diffs):
        """ Given information about diffs, pick the context numbers to create .dasm files for and examples to show diffs for.

        Returns:
            Tuple of (list of method context numbers to create disassembly for, list of examples)
        """

        # If there are non-default metrics then we need to disassemble
        # everything so that jit-analyze can handle those.
        if self.coreclr_args.metrics is not None and self.coreclr_args.metrics != ["PerfScore"]:
            contexts = diffs
            examples = []
        else:
            # In the default case we have size improvements/regressions
            # available without needing to disassemble all, so pick a subset of
            # interesting diffs to pass to jit-analyze.

            def display_subset(message, subset):
                logging.debug(message.format(len(subset)))
                for diff in subset:
                    logging.debug(diff["Context"])
                logging.debug("")

            # 20 smallest method contexts with diffs
            smallest_contexts = sorted(diffs, key=lambda r: int(r["Context size"]))[:20]
            display_subset("Smallest {} contexts with binary differences:", smallest_contexts)

            if self.coreclr_args.metrics is None:
                base_metric_name = "Base size"
                diff_metric_name = "Diff size"
            else:
                base_metric_name = "Base PerfScore"
                diff_metric_name = "Diff PerfScore"

            # Order by improvement, largest improvements first
            by_diff = sorted(diffs, key=lambda r: float(r[diff_metric_name]) - float(r[base_metric_name]))
            # 20 top improvements
            top_improvements = by_diff[:20]
            # 20 top regressions
            top_regressions = by_diff[-20:]

            display_subset("Top {} improvements:", top_improvements)
            display_subset("Top {} regressions:", top_regressions)

            # Order by percentage-wise size improvement, largest improvements first
            def diff_pct(r):
                base = float(r[base_metric_name])
                if base == 0:
                    return 0
                diff = float(r[diff_metric_name])
                return (diff - base) / base

            by_diff_size_pct = sorted(diffs, key=diff_pct)
            top_improvements_pct = by_diff_size_pct[:20]
            top_regressions_pct = by_diff_size_pct[-20:]

            display_subset("Top {} improvements, percentage-wise:", top_improvements_pct)
            display_subset("Top {} regressions, percentage-wise:", top_regressions_pct)

            # 20 contexts without size diffs (possibly GC info diffs), sorted by size
            zero_size_diffs = filter(lambda r: int(r["Diff size"]) == int(r["Base size"]), diffs)
            smallest_zero_size_contexts = sorted(zero_size_diffs, key=lambda r: int(r["Context size"]))[:20]

            display_subset("Smallest {} zero sized diffs:", smallest_zero_size_contexts)

            by_diff_size_pct_examples = [diff for diff in by_diff_size_pct if abs(int(diff['Diff size']) - int(diff['Base size'])) < 50]
            if len(by_diff_size_pct_examples) == 0:
                by_diff_size_pct_examples = by_diff_size_pct

            example_improvements = by_diff_size_pct_examples[:3]
            example_regressions = by_diff_size_pct_examples[3:][-3:]
            contexts = smallest_contexts + top_improvements + top_regressions + top_improvements_pct + top_regressions_pct + smallest_zero_size_contexts + example_improvements + example_regressions
            examples = example_improvements + example_regressions

        final_contexts_indices = list(set(int(r["Context"]) for r in contexts))
        final_contexts_indices.sort()
        return (final_contexts_indices, examples)

################################################################################
# SuperPMI Replay/TP diff
################################################################################


class SuperPMIReplayThroughputDiff:
    """ SuperPMI Replay throughput diff class

    Notes:
        The object is responsible for replaying the mch files given to the
        instance of the class and doing TP measurements of the two passed jits.
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
        self.pin_path = get_pin_exe_path(coreclr_args)
        self.inscount_pintool_path = get_inscount_pintool_path(coreclr_args)

        self.coreclr_args = coreclr_args
        self.diff_mcl_contents = None

    ############################################################################
    # Instance Methods
    ############################################################################

    def replay_with_throughput_diff(self):
        """ Replay SuperPMI collections measuring throughput differences.

        Returns:
            (bool) True on success; False otherwise
        """

        # Set up some settings we'll use below.

        target_flags = []
        if self.coreclr_args.arch != self.coreclr_args.target_arch:
            target_flags += [ "-target", self.coreclr_args.target_arch ]

        base_option_flags = []
        if self.coreclr_args.base_jit_option:
            for o in self.coreclr_args.base_jit_option:
                base_option_flags += "-jitoption", o
        if self.coreclr_args.jitoption:
            for o in self.coreclr_args.jitoption:
                base_option_flags += "-jitoption", o

        diff_option_flags = []
        if self.coreclr_args.diff_jit_option:
            for o in self.coreclr_args.diff_jit_option:
                diff_option_flags += "-jit2option", o
        if self.coreclr_args.jitoption:
            for o in self.coreclr_args.jitoption:
                diff_option_flags += "-jit2option", o

        base_jit_build_string_decoded = decode_clrjit_build_string(self.base_jit_path)
        diff_jit_build_string_decoded = decode_clrjit_build_string(self.diff_jit_path)

        if not base_jit_build_string_decoded:
            logging.warning("Warning: Could not decode base JIT build string")
        if not diff_jit_build_string_decoded:
            logging.warning("Warning: Could not decode diff JIT build string")
        if base_jit_build_string_decoded and diff_jit_build_string_decoded:
            (base_jit_compiler_version, base_jit_with_native_pgo) = base_jit_build_string_decoded
            (diff_jit_compiler_version, diff_jit_with_native_pgo) = diff_jit_build_string_decoded

            if base_jit_compiler_version != diff_jit_compiler_version:
                logging.warning("Warning: Different compilers used for base and diff JITs. Results may be misleading.")
                logging.warning("  Base JIT's compiler: {}".format(base_jit_compiler_version))
                logging.warning("  Diff JIT's compiler: {}".format(diff_jit_compiler_version))

            if base_jit_with_native_pgo:
                logging.warning("Warning: Base JIT was compiled with native PGO. Results may be misleading. Specify -p:NoPgoOptimize=true when building.")
            if diff_jit_with_native_pgo:
                logging.warning("Warning: Diff JIT was compiled with native PGO. Results may be misleading. Specify -p:NoPgoOptimize=true when building.")

        tp_diffs = []

        with TempDir(None, self.coreclr_args.skip_cleanup) as temp_location:
            logging.debug("")
            logging.debug("Temp Location: %s", temp_location)
            logging.debug("")

            for mch_file in self.mch_files:

                logging.info("Running throughput diff of %s", mch_file)

                detailed_info_file = os.path.join(temp_location, os.path.basename(mch_file) + "_details.csv")

                pin_options = [
                    "-follow_execv", # attach to child processes
                    "-t", self.inscount_pintool_path, "-quiet",
                ]
                flags = [
                    "-applyDiff",
                    "-details", detailed_info_file,
                ]
                flags += target_flags
                flags += base_option_flags
                flags += diff_option_flags

                if not self.coreclr_args.sequential and not self.coreclr_args.compile:
                    flags += [ "-p" ]

                if self.coreclr_args.break_on_assert:
                    flags += [ "-boa" ]

                if self.coreclr_args.break_on_error:
                    flags += [ "-boe" ]

                if self.coreclr_args.compile:
                    flags += [ "-c", self.coreclr_args.compile ]

                if self.coreclr_args.spmi_log_file is not None:
                    flags += [ "-w", self.coreclr_args.spmi_log_file ]

                if self.coreclr_args.error_limit is not None:
                    flags += ["-failureLimit", self.coreclr_args.error_limit]

                # Change the working directory to the Core_Root we will call SuperPMI from.
                # This is done to allow libcoredistools to be loaded correctly on unix
                # as the loadlibrary path will be relative to the current directory.
                with ChangeDir(self.coreclr_args.core_root):
                    command = [self.pin_path] + pin_options + ["--"] + [self.superpmi_path] + flags + [self.base_jit_path, self.diff_jit_path, mch_file]
                    return_code = run_and_log(command)

                if return_code != 0:
                    command_string = " ".join(command)
                    logging.debug("'%s': Error return code: %s", command_string, return_code)

                details = read_csv(detailed_info_file)
                (base_metrics, diff_metrics) = aggregate_diff_metrics(details)

                if base_metrics is not None and diff_metrics is not None:
                    base_instructions = base_metrics["Overall"]["Diff executed instructions"]
                    diff_instructions = diff_metrics["Overall"]["Diff executed instructions"]

                    logging.info("Total instructions executed by base: {}".format(base_instructions))
                    logging.info("Total instructions executed by diff: {}".format(diff_instructions))
                    if base_instructions != 0 and diff_instructions != 0:
                        delta_instructions = diff_instructions - base_instructions
                        logging.info("Total instructions executed delta: {} ({:.2%} of base)".format(delta_instructions, delta_instructions / base_instructions))
                        tp_diffs.append((os.path.basename(mch_file), base_metrics, diff_metrics))
                    else:
                        logging.warning("One compilation failed to produce any results")
                else:
                    logging.warning("No metric files present?")

                if not self.coreclr_args.skip_cleanup:
                    if os.path.isfile(detailed_info_file):
                        os.remove(detailed_info_file)
                        detailed_info_file = None

            ################################################################################################ end of for mch_file in self.mch_files

        # Report the overall results summary of the tpdiff run

        logging.info("Throughput diff summary:")

        # Construct an overall Markdown summary file.

        if len(tp_diffs) > 0:
            if not os.path.isdir(self.coreclr_args.spmi_location):
                os.makedirs(self.coreclr_args.spmi_location)

            overall_md_summary_file = create_unique_file_name(self.coreclr_args.spmi_location, "tpdiff_summary", "md")

            if os.path.isfile(overall_md_summary_file):
                os.remove(overall_md_summary_file)

            with open(overall_md_summary_file, "w") as write_fh:
                self.write_tpdiff_markdown_summary(write_fh, tp_diffs, base_jit_build_string_decoded, diff_jit_build_string_decoded, True)
                logging.info("  Summary Markdown file: %s", overall_md_summary_file)

            short_md_summary_file = create_unique_file_name(self.coreclr_args.spmi_location, "tpdiff_short_summary", "md")

            if os.path.isfile(short_md_summary_file):
                os.remove(short_md_summary_file)

            with open(short_md_summary_file, "w") as write_fh:
                self.write_tpdiff_markdown_summary(write_fh, tp_diffs, base_jit_build_string_decoded, diff_jit_build_string_decoded, False)
                logging.info("  Short Summary Markdown file: %s", short_md_summary_file)                

        return True
        ################################################################################################ end of replay_with_throughput_diff()

    def write_tpdiff_markdown_summary(self, write_fh, tp_diffs, base_jit_build_string_decoded, diff_jit_build_string_decoded, include_details):

        def write_top_context_section():
            if not base_jit_build_string_decoded:
                write_fh.write("{} Could not decode base JIT build string".format(html_color("red", "Warning:")))
            if not diff_jit_build_string_decoded:
                write_fh.write("{} Could not decode diff JIT build string".format(html_color("red", "Warning:")))
            if base_jit_build_string_decoded and diff_jit_build_string_decoded:
                (base_jit_compiler_version, base_jit_with_native_pgo) = base_jit_build_string_decoded
                (diff_jit_compiler_version, diff_jit_with_native_pgo) = diff_jit_build_string_decoded

                if base_jit_compiler_version != diff_jit_compiler_version:
                    write_fh.write("{} Different compilers used for base and diff JITs. Results may be misleading.\n".format(html_color("red", "Warning:")))
                    write_fh.write("Base JIT's compiler: {}\n".format(base_jit_compiler_version))
                    write_fh.write("Diff JIT's compiler: {}\n".format(diff_jit_compiler_version))

                if base_jit_with_native_pgo:
                    write_fh.write("{} Base JIT was compiled with native PGO. Results may be misleading. Specify -p:NoPgoOptimize=true when building.".format(html_color("red", "Warning:")))
                if diff_jit_with_native_pgo:
                    write_fh.write("{} Diff JIT was compiled with native PGO. Results may be misleading. Specify -p:NoPgoOptimize=true when building.".format(html_color("red", "Warning:")))

            write_jit_options(self.coreclr_args, write_fh)

        # We write two tables, an overview one with just significantly
        # impacted collections and a detailed one that includes raw
        # instruction count and all collections.
        def is_significant_pct(base, diff):
            if base == 0:
                return diff != 0

            return round((diff - base) / base * 100, 2) != 0

        def is_significant(row, base, diff):
            return is_significant_pct(base[row]["Diff executed instructions"], diff[row]["Diff executed instructions"])

        if any(is_significant(row, base, diff) for row in ["Overall", "MinOpts", "FullOpts"] for (_, base, diff) in tp_diffs):
            def write_pivot_section(row):
                if not any(is_significant(row, base, diff) for (_, base, diff) in tp_diffs):
                    return

                pcts = [compute_pct(base_metrics[row]["Diff executed instructions"], diff_metrics[row]["Diff executed instructions"]) for (_, base_metrics, diff_metrics) in tp_diffs]
                min_pct_str = format_pct(min(pcts))
                max_pct_str = format_pct(max(pcts))
                if min_pct_str == max_pct_str:
                    tp_summary = "{} ({})".format(row, min_pct_str)
                else:
                    tp_summary = "{} ({} to {})".format(row, min_pct_str, max_pct_str)

                with DetailsSection(write_fh, tp_summary):
                    write_fh.write("|Collection|PDIFF|\n")
                    write_fh.write("|---|--:|\n")
                    for mch_file, base, diff in tp_diffs:
                        base_instructions = base[row]["Diff executed instructions"]
                        diff_instructions = diff[row]["Diff executed instructions"]

                        if is_significant(row, base, diff):
                            write_fh.write("|{}|{}|\n".format(
                                mch_file,
                                compute_and_format_pct(base_instructions, diff_instructions)))

            write_top_context_section()
            write_pivot_section("Overall")
            write_pivot_section("MinOpts")
            write_pivot_section("FullOpts")
        elif include_details:
            write_top_context_section()
            write_fh.write("No significant throughput differences found\n")

        if include_details:
            with DetailsSection(write_fh, "Details"):
                for (disp, row) in [("All", "Overall"), ("MinOpts", "MinOpts"), ("FullOpts", "FullOpts")]:
                    write_fh.write("{} contexts:\n\n".format(disp))
                    write_fh.write("|Collection|Base # instructions|Diff # instructions|PDIFF|\n")
                    write_fh.write("|---|--:|--:|--:|\n")
                    for mch_file, base, diff in tp_diffs:
                        base_instructions = base[row]["Diff executed instructions"]
                        diff_instructions = diff[row]["Diff executed instructions"]
                        write_fh.write("|{}|{:,d}|{:,d}|{}|\n".format(
                            mch_file, base_instructions, diff_instructions,
                            compute_and_format_pct(base_instructions, diff_instructions)))
                    write_fh.write("\n")

################################################################################
# Argument handling helpers
################################################################################


def determine_coredis_tools(coreclr_args):
    """ Determine the coredistools location in one of several locations:
        1. First, look in Core_Root. It will be there if the setup-stress-dependencies.cmd/sh
        script has been run, which is typically only if tests have been run.
        2. The build appears to restore it and put it in the R2RDump sub-directory. Look there.
        Copy it to the Core_Root directory if found.
        3. If unable to find coredistools, download it from a cached
        copy in the CLRJIT Azure Storage. (Ideally, we would instead download the NuGet
        package and extract it using the same mechanism as setup-stress-dependencies
        instead of having our own copy in Azure Storage).

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        coredistools_location (str)     : path of [lib]coredistools.dylib|so|dll
    """

    if not hasattr(coreclr_args, "core_root") or coreclr_args.core_root is None:
        raise RuntimeError("Core_Root not set properly")

    coredistools_dll_name = None
    if coreclr_args.host_os.lower() == "osx":
        coredistools_dll_name = "libcoredistools.dylib"
    elif coreclr_args.host_os.lower() == "linux":
        coredistools_dll_name = "libcoredistools.so"
    elif coreclr_args.host_os.lower() == "windows":
        coredistools_dll_name = "coredistools.dll"
    else:
        raise RuntimeError("Unknown host os: {}".format(coreclr_args.host_os))

    coredistools_location = os.path.join(coreclr_args.core_root, coredistools_dll_name)
    if os.path.isfile(coredistools_location):
        logging.info("Using coredistools found at %s", coredistools_location)
    else:
        coredistools_r2rdump_location = os.path.join(coreclr_args.core_root, "R2RDump", coredistools_dll_name)
        if os.path.isfile(coredistools_r2rdump_location):
            logging.info("Using coredistools found at %s (copying to %s)", coredistools_r2rdump_location, coredistools_location)
            logging.debug("Copying %s -> %s", coredistools_r2rdump_location, coredistools_location)
            shutil.copy2(coredistools_r2rdump_location, coredistools_location)
        else:
            # Often, Core_Root will already exist. However, you can do a product build without
            # creating a Core_Root, and successfully run replay or asm diffs, if we just create Core_Root
            # and copy coredistools there. Note that our replays all depend on Core_Root existing, as we
            # set the current directory to Core_Root before running superpmi.
            if not os.path.isdir(coreclr_args.core_root):
                logging.warning("Warning: Core_Root does not exist at \"%s\"; creating it now", coreclr_args.core_root)
                os.makedirs(coreclr_args.core_root)
            coredistools_uri = az_blob_storage_superpmi_container_uri + "/libcoredistools/{}-{}/{}".format(coreclr_args.host_os.lower(), coreclr_args.arch.lower(), coredistools_dll_name)
            skip_progress = hasattr(coreclr_args, 'no_progress') and coreclr_args.no_progress
            download_one_url(coredistools_uri, coredistools_location, is_azure_storage=True, display_progress=not skip_progress)

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
        logging.info("Using PMI at %s", pmi_location)
    else:
        path_var = os.environ.get("PATH")
        pmi_location = find_file("pmi.dll", path_var.split(os.pathsep)) if path_var is not None else None
        if pmi_location is not None:
            logging.info("Using PMI found on PATH at %s", pmi_location)
        else:
            pmi_location = os.path.join(coreclr_args.core_root, "pmi.dll")
            if os.path.isfile(pmi_location):
                logging.info("Using PMI found at %s", pmi_location)
            else:
                pmi_uri = az_blob_storage_superpmi_container_uri + "/pmi/pmi.dll"
                skip_progress = hasattr(coreclr_args, 'no_progress') and coreclr_args.no_progress
                download_one_url(pmi_uri, pmi_location, is_azure_storage=True, display_progress=not skip_progress)

    assert os.path.isfile(pmi_location)
    return pmi_location


def get_jit_name(coreclr_args):
    """ Determine the JIT file name to use based on the platform. If "-jit_name" is specified, then use the specified jit.
        This function is called for cases where the "-jit_name" flag is not used, so be careful not
        to depend on the "jit_name" attribute existing.

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) : name of the jit for this OS
    """

    # If `-jit_name` is used, it must be given a full filename, not just a "base name", so use it without additional processing.
    if hasattr(coreclr_args, "jit_name") and coreclr_args.jit_name is not None:
        return coreclr_args.jit_name

    return determine_jit_name(coreclr_args.host_os, coreclr_args.target_os, coreclr_args.arch, coreclr_args.target_arch)


def find_tool(coreclr_args, tool_name, search_core_root=True, search_product_location=True, search_path=True, throw_on_not_found=True):
    """ Find a tool or any specified file (e.g., clrjit.dll) and return the full path to that tool if found.

    Args:
        coreclr_args (CoreclrArguments): parsed args
        tool_name (str): tool to find, e.g., "superpmi.exe"
        search_core_root (bool): True to search the Core_Root folder
        search_product_location: True to search the build product folder
        search_path: True to search along the PATH

    Return:
        (str) Full path of the tool, or None if not found.
    """

    # First, look in Core_Root, if there is one.
    if search_core_root and hasattr(coreclr_args, "core_root") and coreclr_args.core_root is not None and os.path.isdir(coreclr_args.core_root):
        tool_path = os.path.join(coreclr_args.core_root, tool_name)
        if os.path.isfile(tool_path):
            logging.debug("Using %s from Core_Root: %s", tool_name, tool_path)
            return tool_path

    # Next, look in the built product directory, if it exists. We can use superpmi/mcs directly from the
    # product build directory instead from Core_Root because they don't depend on managed code libraries.
    if search_product_location and hasattr(coreclr_args, "product_location") and coreclr_args.product_location is not None and os.path.isdir(coreclr_args.product_location):
        tool_path = os.path.join(coreclr_args.product_location, tool_name)
        if os.path.isfile(tool_path):
            logging.debug("Using %s from product build location: %s", tool_name, tool_path)
            return tool_path

    # Finally, look on the PATH
    if search_path:
        path_var = os.environ.get("PATH")
        if path_var is not None:
            tool_path = find_file(tool_name, path_var.split(os.pathsep))
            if tool_path is not None:
                logging.debug("Using %s from PATH: %s", tool_name, tool_path)
                return tool_path

    if throw_on_not_found:
        raise RuntimeError("Tool " + tool_name + " not found. Have you built the runtime repo and created a Core_Root, or put it on your PATH?")

    return None


def determine_superpmi_tool_name(coreclr_args):
    """ Determine the superpmi tool name based on the OS

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) Name of the superpmi tool to use
    """

    if coreclr_args.host_os == "osx" or coreclr_args.host_os == "linux":
        return "superpmi"
    elif coreclr_args.host_os == "windows":
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
    return find_tool(coreclr_args, superpmi_tool_name)


def determine_mcs_tool_name(coreclr_args):
    """ Determine the mcs tool name based on the OS

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) Name of the mcs tool to use
    """

    if coreclr_args.host_os == "osx" or coreclr_args.host_os == "linux":
        return "mcs"
    elif coreclr_args.host_os == "windows":
        return "mcs.exe"
    else:
        raise RuntimeError("Unsupported OS.")


def determine_mcs_tool_path(coreclr_args):
    """ Determine the mcs tool full path

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) Path of the mcs tool to use
    """

    mcs_tool_name = determine_mcs_tool_name(coreclr_args)
    return find_tool(coreclr_args, mcs_tool_name)


def determine_dotnet_tool_name(coreclr_args):
    """ Determine the dotnet tool name based on the OS

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) Name of the dotnet tool to use
    """

    if coreclr_args.host_os == "osx" or coreclr_args.host_os == "linux":
        return "dotnet"
    elif coreclr_args.host_os == "windows":
        return "dotnet.exe"
    else:
        raise RuntimeError("Unsupported OS.")


def determine_jit_ee_version(coreclr_args):
    """ Determine the JIT-EE version to use.

        The JIT-EE version is used for determining which MCH files to download and use. It is determined as follows:
        1. Try to parse it out of the source code. If we can find src\\coreclr\\inc\\jiteeversionguid.h in the source
           tree (and we're already assuming we can find the repo root from the relative path of this script),
           then the JIT-EE version lives in jiteeversionguid.h as follows:

           constexpr GUID JITEEVersionIdentifier = { /* a5eec3a4-4176-43a7-8c2b-a05b551d4f49 */
               0xa5eec3a4,
               0x4176,
               0x43a7,
               {0x8c, 0x2b, 0xa0, 0x5b, 0x55, 0x1d, 0x4f, 0x49}
           };

           We want the string between the /* */ comments.
        2. Find the mcs tool and run "mcs -printJITEEVersion".
        3. Otherwise, just use "unknown-jit-ee-version", which will probably cause downstream failures.

        NOTE: When using mcs, we need to run the tool. So we need a version that will run. If a user specifies
              an "-arch" argument that creates a Core_Root path that won't run, like an arm32 Core_Root on an
              x64 machine, this won't work. This could happen if doing "upload", "upload-private", or "list-collections" on
              collections from a machine that didn't create the native collections. We should create a "native"
              Core_Root and use that in case there are "cross-arch" scenarios.

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) The JIT-EE version to use
    """

    jiteeversionguid_h_path = os.path.join(coreclr_args.coreclr_dir, "inc", "jiteeversionguid.h")
    if os.path.isfile(jiteeversionguid_h_path):
        # The string is near the beginning of the somewhat large file, so just read a line at a time when searching.
        with open(jiteeversionguid_h_path, 'r') as file_handle:
            for line in file_handle:
                match_obj = re.search(r'^constexpr GUID JITEEVersionIdentifier *= *{ */\* *([^ ]*) *\*/', line)
                if match_obj is not None:
                    jiteeversionguid_h_jit_ee_version = match_obj.group(1)
                    jiteeversionguid_h_jit_ee_version = jiteeversionguid_h_jit_ee_version.lower()
                    logging.info("Using JIT/EE Version from jiteeversionguid.h: %s", jiteeversionguid_h_jit_ee_version)
                    return jiteeversionguid_h_jit_ee_version
            logging.warning("Warning: couldn't find JITEEVersionIdentifier in %s; is the file corrupt?", jiteeversionguid_h_path)

    mcs_path = determine_mcs_tool_path(coreclr_args)
    command = [mcs_path, "-printJITEEVersion"]
    proc = subprocess.Popen(command, stdout=subprocess.PIPE)
    stdout_jit_ee_version, _ = proc.communicate()
    return_code = proc.returncode
    if return_code == 0:
        mcs_jit_ee_version = stdout_jit_ee_version.decode('utf-8').strip()
        mcs_jit_ee_version = mcs_jit_ee_version.lower()
        logging.info("Using JIT/EE Version from mcs: %s", mcs_jit_ee_version)
        return mcs_jit_ee_version

    # Otherwise, use the default "unknown" version.
    default_jit_ee_version = "unknown-jit-ee-version"
    logging.info("Using default JIT/EE Version: %s", default_jit_ee_version)
    return default_jit_ee_version

def print_platform_specific_environment_vars(loglevel, coreclr_args, var, value):
    """ Print environment variables as set {}={} or export {}={}

    Args:
        coreclr_args (CoreclrArguments): parsed args
        var   (str): variable to set
        value (str): value being set.
    """

    if coreclr_args.host_os == "windows":
        logging.log(loglevel, "set %s=%s", var, value)
    else:
        logging.log(loglevel, "export %s=%s", var, value)


def list_superpmi_collections_container_via_rest_api(path_filter=lambda unused: True):
    """ List the superpmi collections using the Azure Storage REST api

    Args:
        path_filter (lambda: string -> bool): filter to apply to the list. The filter takes a relative
            collection path and returns True if this path is acceptable.

    Returns:
        Returns a list of collections, each element a relative path with:
        <jit-ee-guid>/<os>/<architecture>/<filename>

    Notes:
        This method does not require installing the Azure Storage python package.
    """

    # This URI will return *all* the blobs, for all jit-ee-version/OS/architecture combinations.
    # pass "prefix=foo/bar/..." to only show a subset. Or, we can filter later using string search.
    #
    # Note that there is a maximum number of results returned in one query of 5000. So we might need to
    # iterate. In that case, the XML result contains a `<NextMarker>` element like:
    #
    # <NextMarker>2!184!MDAwMDkyIWJ1aWxkcy8wMTZlYzI5OTAzMzkwMmY2ZTY4Yzg0YWMwYTNlYzkxN2Y5MzA0OTQ2L0xpbnV4L3g2NC9DaGVja2VkL2xpYmNscmppdF93aW5fYXJtNjRfeDY0LnNvITAwMDAyOCE5OTk5LTEyLTMxVDIzOjU5OjU5Ljk5OTk5OTlaIQ--</NextMarker>
    #
    # which we need to pass to the REST API with `marker=...`.

    paths = []

    list_superpmi_container_uri_base = az_blob_storage_superpmi_container_uri + "?restype=container&comp=list&prefix=" + az_collections_root_folder + "/"

    iter = 1
    marker = ""

    while True:
        list_superpmi_container_uri = list_superpmi_container_uri_base + marker

        try:
            contents = urllib.request.urlopen(list_superpmi_container_uri).read().decode('utf-8')
        except Exception as exception:
            logging.error("Didn't find any collections using %s", list_superpmi_container_uri)
            logging.error("  Error: %s", exception)
            return None

        # Contents is an XML file with contents like:
        #
        # <EnumerationResults ContainerName="https://clrjit.blob.core.windows.net/superpmi/collections">
        #   <Blobs>
        #     <Blob>
        #       <Name>jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip</Name>
        #       <Url>https://clrjit.blob.core.windows.net/superpmi/collections/jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip</Url>
        #       <Properties>
        #         ...
        #       </Properties>
        #     </Blob>
        #     <Blob>
        #       <Name>jit-ee-guid/Linux/x64/Linux.x64.Checked.mch.zip</Name>
        #       <Url>https://clrjit.blob.core.windows.net/superpmi/collections/jit-ee-guid/Linux/x64/Linux.x64.Checked.mch.zip</Url>
        #     ... etc. ...
        #   </Blobs>
        # </EnumerationResults>
        #
        # We just want to extract the <Url> entries. We could probably use an XML parsing package, but we just
        # use regular expressions.

        url_prefix = az_blob_storage_superpmi_container_uri + "/" + az_collections_root_folder + "/"

        urls_split = contents.split("<Url>")[1:]
        for item in urls_split:
            url = item.split("</Url>")[0].strip()
            path = remove_prefix(url, url_prefix)
            if path_filter(path):
                paths.append(path)

        # Look for a continuation marker.
        re_match = re.match(r'.*<NextMarker>(.*)</NextMarker>.*', contents)
        if re_match:
            marker_text = re_match.group(1)
            marker = "&marker=" + marker_text
            iter += 1
        else:
            break

    return paths


def list_superpmi_collections_container_via_azure_api(path_filter=lambda unused: True):
    """ List the superpmi collections using the Azure Storage API

    Args:
        path_filter (lambda: string -> bool): filter to apply to the list. The filter takes a relative
            collection path and returns True if this path is acceptable.

    Returns:
        Returns a list of collections, each element a relative path with:
        <jit-ee-guid>/<os>/<architecture>/<filename>
    """

    require_azure_storage_libraries()
    from jitutil import ContainerClient, AzureCliCredential

    superpmi_container_url = az_blob_storage_superpmi_container_uri

    paths = []
    ok = True
    try:
        az_credential = AzureCliCredential()
        container = ContainerClient.from_container_url(superpmi_container_url, credential=az_credential)
        blob_name_prefix = az_collections_root_folder + "/"
        blob_list = container.list_blobs(name_starts_with=blob_name_prefix, retry_total=0)
        for blob in blob_list:
            # The blob name looks something like:
            #    collections/f556df6c-b9c7-479c-b895-8e1f1959fe59/Linux/arm/tests.pmi.Linux.arm.checked.mch.zip
            # remove the leading "collections/" part of the name.
            path = remove_prefix(blob.name, blob_name_prefix)
            if path_filter(path):
                paths.append(path)
    except Exception as exception:
        logging.error("Failed to list collections: %s", superpmi_container_url)
        report_azure_error()
        logging.error(exception)
        return None

    return paths


def list_superpmi_collections_container(path_filter=lambda unused: True):
    """ List the superpmi collections using either the REST API or the Azure API with authentication.

    Args:
        path_filter (lambda: string -> bool): filter to apply to the list. The filter takes a relative
            collection path and returns True if this path is acceptable.

    Returns:
        Returns a list of collections, each element a relative path with:
        <jit-ee-guid>/<os>/<architecture>/<filename>
    """
    if authenticate_using_azure:
        return list_superpmi_collections_container_via_azure_api(path_filter)
    else:
        return list_superpmi_collections_container_via_rest_api(path_filter)


def process_local_mch_files(coreclr_args, mch_files, mch_cache_dir):
    """ Process the MCH files to use.

    Args:
        coreclr_args (CoreclrArguments): parsed args
        mch_files (list): list of MCH files locations. Normally, this comes from the `-mch_files` argument, but it can
            also come from the `private_store` argument. It can be a list of files or directories or both.
        mch_cache_dir (str): the directory to cache any downloads.

    Returns:
        list of full paths of locally cached MCH files to use
    """

    # Create the cache location. Note that we'll create it even if we end up not copying anything.
    if not os.path.isdir(mch_cache_dir):
        os.makedirs(mch_cache_dir)

    # Process the mch_files list. Download and cache UNC and HTTP files.
    urls = []
    local_mch_files = []
    for item in mch_files:
        # On Windows only, see if any of the mch_files are UNC paths (i.e., "\\server\share\...").
        # If so, download and cache all the files found there to our usual local cache location, to avoid future network access.
        if coreclr_args.host_os == "windows" and item.startswith("\\\\"):
            # Special case: if the user specifies a .mch file, we'll also look for and cache a .mch.mct file next to it, if one exists.
            # This happens naturally if a directory is passed and we search for all .mch and .mct files in that directory.
            mch_file = os.path.abspath(item)
            if os.path.isfile(mch_file) and mch_file.endswith(".mch"):
                urls.append(mch_file)
                mct_file = mch_file + ".mct"
                if os.path.isfile(mct_file):
                    urls.append(mct_file)
            else:
                urls += get_files_from_path(mch_file, match_func=lambda path: any(path.lower().endswith(extension) for extension in [".mch", ".mct", ".zip"]))
        elif item.lower().startswith("http:") or item.lower().startswith("https:"):  # probably could use urllib.parse to be more precise
            urls.append(item)
        else:
            # Doesn't appear to be a UNC path (on Windows) or a URL, so just use it as-is.
            local_mch_files.append(item)

    # Now apply any filtering we've been asked to do.
    def filter_local_path(path):
        path = path.lower()
        return (coreclr_args.filter is None) or any((filter_item.lower() in path) for filter_item in coreclr_args.filter)

    urls = [url for url in urls if filter_local_path(url)]

    # Download all the urls at once, and add the local cache filenames to our accumulated list of local file names.
    skip_progress = hasattr(coreclr_args, 'no_progress') and coreclr_args.no_progress
    if len(urls) != 0:
        local_mch_files += download_files(urls, mch_cache_dir, is_azure_storage=True, display_progress=not skip_progress)

    # Special case: walk the URLs list and for every ".mch" or ".mch.zip" file, check to see that either the associated ".mct" file is already
    # in the list, or add it to a new list to attempt to download (but don't fail the download if it doesn't exist).
    mct_urls = []
    for url in urls:
        if url.endswith(".mch") or url.endswith(".mch.zip"):
            mct_url = url.replace(".mch", ".mch.mct")
            if mct_url not in urls:
                mct_urls.append(mct_url)
    if len(mct_urls) != 0:
        local_mch_files += download_files(mct_urls, mch_cache_dir, fail_if_not_found=False, is_azure_storage=True, display_progress=not skip_progress)

    # Even though we might have downloaded MCT files, only return the set of MCH files.
    local_mch_files = [file for file in local_mch_files if any(file.lower().endswith(extension) for extension in [".mch"])]

    return local_mch_files


def process_mch_files_arg(coreclr_args):
    """ Process the -mch_files argument. If the argument is not specified, then download files
        from Azure Storage and any specified private MCH stores.

        Any files on UNC (i.e., "\\server\share" paths on Windows) or Azure Storage stores,
        even if specified via the `-mch_files` argument, will be downloaded and cached locally,
        replacing the paths with a reference to the newly cached local paths.

        If the `-mch_files` argument is specified, files are always either used directly or copied and
        cached locally. These will be the only files used.

        If the `-mch_files` argument is not specified, and there exists a cache, then only files already
        in the cache are used and no MCH stores are consulted, unless the `--force_download` option is
        specified, in which case normal MCH store processing is done. This behavior is to avoid
        touching the network unless required.

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Returns:
        list of local full paths of MCH files or directories to use
    """

    mch_cache_dir = os.path.join(coreclr_args.spmi_location, "mch", "{}.{}.{}".format(coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch))

    # If an `-mch_files` argument was given, then use exactly that set of files.
    if coreclr_args.mch_files is not None:
        return process_local_mch_files(coreclr_args, coreclr_args.mch_files, mch_cache_dir)

    # Otherwise, use both Azure Storage, and optionally, private stores.
    # See if the cache directory already exists. If so, we just use it (unless `--force_download` is passed).

    if os.path.isdir(mch_cache_dir) and not coreclr_args.force_download:
        # The cache directory is already there, and "--force_download" was not passed, so just
        # assume it's got what we want.
        # NOTE: a different solution might be to verify that everything we would download is
        #       already in the cache, and simply not download if it is. However, that would
        #       require hitting the network, and currently once you've cached these, you
        #       don't need to do that.
        logging.info("Found download cache directory \"%s\" and --force_download not set; skipping download", mch_cache_dir)
        return [ mch_cache_dir ]

    # MCH files can be large. Log the disk space before and after the download.
    root_space_directory = get_deepest_existing_directory(coreclr_args.spmi_location)
    if root_space_directory is not None:
        before_total, _, before_free = shutil.disk_usage(root_space_directory)
        before_total_gb = int(before_total / 1024 / 1024 / 1024)
        before_free_gb = int(before_free / 1024 / 1024 / 1024)
        logging.debug("Disk usage (%s): total %s GB; free %s GB", root_space_directory, format(before_total_gb, ','), format(before_free_gb, ','))

    local_mch_paths = download_mch_from_azure(coreclr_args, mch_cache_dir)

    if root_space_directory is not None:
        after_total, _, after_free = shutil.disk_usage(root_space_directory)
        after_total_gb = int(after_total / 1024 / 1024 / 1024)
        after_free_gb = int(after_free / 1024 / 1024 / 1024)
        consumed_gb = before_free_gb - after_free_gb
        logging.debug("Disk usage (%s): total %s GB; free %s GB; consumed by download %s GB", root_space_directory, format(after_total_gb, ','), format(after_free_gb, ','), format(consumed_gb, ','))

    # Add the private store files
    if coreclr_args.private_store is not None:
        # Only include the directories corresponding to the current JIT/EE version, target OS, and MCH architecture (this is the
        # same filtering done for Azure storage). Only include them if they actually exist (e.g., the private store might have
        # windows x64 but not Linux arm).
        target_specific_stores = [ os.path.abspath(os.path.join(store, coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch)) for store in coreclr_args.private_store ]
        filtered_stores = [ s for s in target_specific_stores if os.path.isdir(s) ]
        local_mch_paths += process_local_mch_files(coreclr_args, filtered_stores, mch_cache_dir)

    return local_mch_paths


def download_mch_from_azure(coreclr_args, target_dir):
    """ Download the mch files. This can be called to re-download files and
        overwrite them in the target location.

    Args:
        coreclr_args (CoreclrArguments): parsed args
        target_dir (str): target directory to download the files

    Returns:
        list containing the local path of files downloaded
    """

    blob_filter_string =  "{}/{}/{}/".format(coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch).lower()

    # Determine if a URL in Azure Storage should be allowed. The path looks like:
    #   jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip
    # Filter to just the current jit-ee-guid, OS, and architecture.
    # Include both MCH and MCT files as well as the CLR JIT dll (processed below).
    # If there are filters, only download those matching files.
    def filter_superpmi_collections(path):
        path = path.lower()
        return path.startswith(blob_filter_string) and ((coreclr_args.filter is None) or any((filter_item.lower() in path) for filter_item in coreclr_args.filter))

    paths = list_superpmi_collections_container(filter_superpmi_collections)
    if paths is None or len(paths) == 0:
        print("No Azure Storage MCH files to download from {}".format(blob_filter_string))
        return []

    blob_url_prefix = "{}/{}/".format(az_blob_storage_superpmi_container_uri, az_collections_root_folder)
    urls = [blob_url_prefix + path for path in paths]

    skip_progress = hasattr(coreclr_args, 'no_progress') and coreclr_args.no_progress
    return download_files(urls, target_dir, is_azure_storage=True, display_progress=not skip_progress)


def upload_mch(coreclr_args):
    """ Upload a set of MCH files. Each MCH file is first ZIP compressed to save data space and upload/download time.

    Args:
        coreclr_args (CoreclrArguments): parsed args
    """

    require_azure_storage_libraries(need_azure_identity=False)
    from jitutil import BlobServiceClient

    def upload_blob(file, blob_name):
        blob_client = blob_service_client.get_blob_client(container=az_superpmi_container_name, blob=blob_name)

        # Check if the blob already exists, and delete it if it does, before uploading / replacing it.
        try:
            blob_client.get_blob_properties()
            # If no exception, then the blob already exists. Delete it!
            logging.warning("Warning: replacing existing blob!")
            blob_client.delete_blob()
        except Exception:
            # Blob doesn't exist already; that's good
            pass

        with open(file, "rb") as data:
            blob_client.upload_blob(data)

    files = []
    for item in coreclr_args.mch_files:
        files += get_files_from_path(item, match_func=lambda path: any(path.endswith(extension) for extension in [".mch"]))

    files_to_upload = []
    # Special case: walk the files list and for every ".mch" file, check to see that either the associated ".mct" file is already
    # in the list, or add it if the ".mct" file exists.
    for file in files.copy():
        if file.endswith(".mch") and os.stat(file).st_size > 0:
            files_to_upload.append(file)
            mct_file = file + ".mct"
            if os.path.isfile(mct_file) and os.stat(mct_file).st_size > 0:
                files_to_upload.append(mct_file)

    logging.info("Uploading:")
    for item in files_to_upload:
        logging.info("  %s", item)

    blob_service_client = BlobServiceClient(account_url=az_blob_storage_account_uri, credential=coreclr_args.az_storage_key)
    blob_folder_name = "{}/{}/{}/{}".format(az_collections_root_folder, coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch)

    total_bytes_uploaded = 0

    with TempDir() as temp_location:
        for file in files_to_upload:
            # Zip compress the file we will upload
            zip_name = os.path.basename(file) + ".zip"
            zip_path = os.path.join(temp_location, zip_name)
            logging.info("Compress %s -> %s", file, zip_path)
            with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zip_file:
                zip_file.write(file, os.path.basename(file))

            original_stat_result = os.stat(file)
            zip_stat_result = os.stat(zip_path)
            logging.info("Compressed {:n} to {:n} bytes".format(original_stat_result.st_size, zip_stat_result.st_size))
            total_bytes_uploaded += zip_stat_result.st_size

            blob_name = "{}/{}".format(blob_folder_name, zip_name)
            logging.info("Uploading: %s (%s) -> %s", file, zip_path, az_blob_storage_superpmi_container_uri + "/" + blob_name)
            upload_blob(zip_path, blob_name)

    logging.info("Uploaded {:n} bytes".format(total_bytes_uploaded))


def upload_private_mch(coreclr_args):
    """ Upload a set of MCH files. Each MCH file is first ZIP compressed to save data space and upload/download time.

    Args:
        coreclr_args (CoreclrArguments): parsed args
    """

    files = []
    for item in coreclr_args.mch_files:
        files += get_files_from_path(item, match_func=lambda path: any(path.endswith(extension) for extension in [".mch"]))

    files_to_upload = []
    # Special case: walk the files list and for every ".mch" file, check to see that either the associated ".mct" file is already
    # in the list, or add it if the ".mct" file exists.
    for file in files.copy():
        if file.endswith(".mch") and os.stat(file).st_size > 0:
            files_to_upload.append(file)
            mct_file = file + ".mct"
            if os.path.isfile(mct_file) and os.stat(mct_file).st_size > 0:
                files_to_upload.append(mct_file)

    logging.info("Uploading:")
    for item in files_to_upload:
        logging.info("  %s", item)

    file_folder_name = os.path.join(coreclr_args.private_store, coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch)
    if not os.path.isdir(file_folder_name):
        os.makedirs(file_folder_name)

    total_bytes_uploaded = 0

    with TempDir() as temp_location:
        for file in files_to_upload:
            # Zip compress the file we will upload
            zip_name = os.path.basename(file) + ".zip"
            zip_path = os.path.join(temp_location, zip_name)
            logging.info("Compress %s -> %s", file, zip_path)
            with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zip_file:
                zip_file.write(file, os.path.basename(file))

            original_stat_result = os.stat(file)
            zip_stat_result = os.stat(zip_path)
            logging.info("Compressed {:n} to {:n} bytes".format(original_stat_result.st_size, zip_stat_result.st_size))
            total_bytes_uploaded += zip_stat_result.st_size

            target_path = os.path.join(file_folder_name, zip_name)
            logging.info("Uploading: %s (%s) -> %s", file, zip_path, target_path)

            if os.path.exists(target_path):
                logging.warning("Warning: replacing existing file '%s'!", target_path)
                os.remove(target_path)

            shutil.copy2(zip_path, target_path)

    logging.info("Uploaded {:n} bytes".format(total_bytes_uploaded))


def list_collections_command(coreclr_args):
    """ List the SuperPMI collections in Azure Storage

    Args:
        coreclr_args (CoreclrArguments) : parsed args
    """

    blob_filter_string = "{}/{}/{}/".format(coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch).lower()

    # Determine if a URL in Azure Storage should be allowed. The URL looks like:
    #   https://clrjit.blob.core.windows.net/superpmi/jit-ee-guid/Linux/x64/Linux.x64.Checked.frameworks.mch.zip
    # By default, filter to just the current jit-ee-guid, OS, and architecture.
    # Only include MCH files, not MCT (TOC) files.
    def filter_superpmi_collections(path: str):
        path = path.lower()
        return (path.endswith(".mch") or path.endswith(".mch.zip")) and (coreclr_args.all or path.startswith(blob_filter_string))

    paths = list_superpmi_collections_container(filter_superpmi_collections)
    if paths is None:
        return

    blob_url_prefix = "{}/{}/".format(az_blob_storage_superpmi_container_uri, az_collections_root_folder)
    urls = [blob_url_prefix + path for path in paths]

    count = len(urls)

    logging.info("SuperPMI list-collections")
    logging.info("")
    if coreclr_args.all:
        logging.info("%s collections", count)
    else:
        logging.info("%s collections for %s", count, blob_filter_string)
    logging.info("")

    for url in urls:
        logging.info("%s", url)


def list_collections_local_command(coreclr_args):
    """ List the SuperPMI collections local cache: where the Azure Storage collections are copied

    Args:
        coreclr_args (CoreclrArguments) : parsed args
    """

    # Display the blob filter string the local cache corresponds to
    blob_filter_string = "{}/{}/{}/".format(coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch)

    default_mch_root_dir = os.path.join(coreclr_args.spmi_location, "mch")
    default_mch_dir = os.path.join(default_mch_root_dir, "{}.{}.{}".format(coreclr_args.jit_ee_version, coreclr_args.target_os, coreclr_args.mch_arch))

    # Determine if a file should be allowed. The filenames look like:
    #   c:\gh\runtime\artifacts\spmi\mch\a5eec3a4-4176-43a7-8c2b-a05b551d4f49.windows.x64\corelib.windows.x64.Checked.mch
    #   c:\gh\runtime\artifacts\spmi\mch\a5eec3a4-4176-43a7-8c2b-a05b551d4f49.windows.x64\corelib.windows.x64.Checked.mch.mct
    # Only include MCH files, not MCT (TOC) files.
    def filter_superpmi_collections(path: str):
        return path.lower().endswith(".mch")

    if coreclr_args.all:
        if not os.path.isdir(default_mch_root_dir):
            logging.error("Local dir \"%s\" not found", default_mch_root_dir)
            return
        local_items = get_files_from_path(default_mch_root_dir)
    else:
        if not os.path.isdir(default_mch_dir):
            logging.error("Local dir \"%s\" not found", default_mch_dir)
            return
        local_items = get_files_from_path(default_mch_dir)

    filtered_local_items = [item for item in local_items if filter_superpmi_collections(item)]

    count = len(filtered_local_items)

    logging.info("SuperPMI list-collections --local")
    logging.info("")
    if coreclr_args.all:
        logging.info("%s collections", count)
    else:
        logging.info("%s collections for %s", count, blob_filter_string)
    logging.info("")

    for item in filtered_local_items:
        logging.info("%s", item)


def merge_mch(coreclr_args):
    """ Merge all the files specified by a given pattern into a single output MCH file.
        This is a utility function mostly for use by the CI scripting. It is a
        thin wrapper around:

            mcs -merge <output_mch_path> <pattern> -recursive -dedup -thin
            mcs -toc <output_mch_path>

        If `--ci` is passed, it looks for special "ZEROLENGTH" files and deletes them
        before invoking the merge. This is to handle a CI issue where we generate zero-length
        .MCH files in Helix, convert them to non-zero-length sentinel files so they will be
        successfully uploaded to artifacts storage, then downloaded on AzDO using the
        HelixWorkItem.DownloadFilesFromResults mechanism. Then, we delete them before merging.

    Args:
        coreclr_args (CoreclrArguments) : parsed args

    Returns:
        True on success, else False
    """

    if coreclr_args.ci:
        # Handle zero-length sentinel files.
        # Recurse looking for small files that have the text ZEROLENGTH, and delete them.
        try:
            for dirpath, _, filenames in os.walk(os.path.dirname(coreclr_args.pattern)):
                for file_name in filenames:
                    file_path = os.path.join(dirpath, file_name)
                    if os.path.getsize(file_path) < 15:
                        with open(file_path, "rb") as fh:
                            contents = fh.read()
                        match = re.search(b'ZEROLENGTH', contents)
                        if match is not None:
                            logging.info("Removing zero-length MCH sentinel file {}".format(file_path))
                            os.remove(file_path)
        except Exception:
            pass

    logging.info("Merging %s -> %s", coreclr_args.pattern, coreclr_args.output_mch_path)
    mcs_path = determine_mcs_tool_path(coreclr_args)
    command = [mcs_path, "-merge", coreclr_args.output_mch_path, coreclr_args.pattern, "-recursive", "-dedup", "-thin"]
    return_code = run_and_log(command)
    if return_code != 0:
        logging.error("mcs -merge Failed with code %s", return_code)
        return False

    logging.info("Creating MCT file for %s", coreclr_args.output_mch_path)
    command = [mcs_path, "-toc", coreclr_args.output_mch_path]
    return_code = run_and_log(command)
    if return_code != 0:
        logging.error("mcs -toc Failed with code %s", return_code)
        return False

    return True


def get_mch_files_for_replay(local_mch_paths, filters):
    """ Given a list of local MCH files, and any specified filters (in coreclr_args.filter),
        find all the MCH files to use for replay. Note that `local_mch_paths` can contain
        both files and directories.

    Args:
        local_mch_paths (list) : list of local files and directories to use to find MCH files to use
        filters (list) : list of strings, one of which must match each candidate MCH path

    Returns:
        None if error (with an error message already printed), else a filtered list of full paths of MCH files.
    """

    if local_mch_paths is None:
        logging.error("No MCH files specified")
        return None

    mch_files = []
    for item in local_mch_paths:
        # If there are specified filters, only run those matching files.
        mch_files += get_files_from_path(item,
                                         match_func=lambda path:
                                             any(path.endswith(extension) for extension in [".mch"])
                                             and ((filters is None) or any(filter_item.lower() in path for filter_item in filters)))

    if len(mch_files) == 0:
        logging.error("No MCH files found to replay")
        return None

    return mch_files


def process_base_jit_path_arg(coreclr_args):
    """ Process the -base_jit_path argument.

        If the argument is present, check it for being a path to a file.
        If not present, try to find and download a baseline JIT based on the current environment:
        1. Determine the current git hash using:
             git rev-parse HEAD
           or use the `-git_hash` argument (call the result `git_hash`).
        2. Determine the baseline: where does this hash meet the newest `main` branch of any remote using:
             git branch -r --sort=-committerdate -v --list "*/main" followed by git merge-base
           or use the `-base_git_hash` argument (call the result `base_git_hash`).
        3. If the `-base_git_hash` argument is used, use that directly as the exact git
           hash of the baseline JIT to use.
        4. Otherwise, figure out the latest hash, starting with `base_git_hash`, that contains any changes to
           the src\\coreclr\\jit directory. (We do this because the JIT rolling build only includes
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
        coreclr_args.base_jit_path = os.path.abspath(coreclr_args.base_jit_path)
        return

    # We cache baseline jits under the following directory. Note that we can't create the full directory path
    # until we know the baseline JIT hash.
    default_basejit_root_dir = os.path.join(coreclr_args.spmi_location, "basejit")

    # Do all the remaining commands, including a number of 'git' commands including relative paths,
    # from the root of the runtime repo.

    with ChangeDir(coreclr_args.runtime_repo_location):
        if coreclr_args.git_hash is None:
            command = [ "git", "rev-parse", "HEAD" ]
            logging.debug("Invoking: %s", " ".join(command))
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_git_rev_parse, _ = proc.communicate()
            return_code = proc.returncode
            if return_code == 0:
                current_hash = stdout_git_rev_parse.decode('utf-8').strip()
                logging.debug("Current hash: %s", current_hash)
            else:
                raise RuntimeError("Couldn't determine current git hash")
        else:
            current_hash = coreclr_args.git_hash

        if coreclr_args.base_git_hash is None:
            # We've got the current hash; figure out the baseline hash.
            # First find the newest hash for any branch matching */main.
            command = [ "git", "branch", "-r", "--sort=-committerdate", "-v", "--list", "*/main" ]
            logging.debug("Invoking: %s", " ".join(command))
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_git_main_branch, _ = proc.communicate()
            return_code = proc.returncode
            if return_code != 0:
                raise RuntimeError("Couldn't determine newest 'main' git hash")

            main_hash = stdout_git_main_branch.decode('utf-8').strip().split()[1]

            # Get the merge-base between the newest main and our current rev
            command = [ "git", "merge-base", current_hash, main_hash ]
            logging.debug("Invoking: %s", " ".join(command))
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_git_merge_base, _ = proc.communicate()
            return_code = proc.returncode
            if return_code != 0:
                raise RuntimeError("Couldn't determine baseline git hash")

            baseline_hash = stdout_git_merge_base.decode('utf-8').strip()
            logging.info("Baseline hash: %s", baseline_hash)
        else:
            baseline_hash = coreclr_args.base_git_hash

        if coreclr_args.base_git_hash is None:
            # Enumerate the last 20 changes, starting with the baseline, that included JIT changes.
            command = [ "git", "log", "--pretty=format:%H", baseline_hash, "-20", "--", "src/coreclr/jit/*" ]
            logging.debug("Invoking: %s", " ".join(command))
            proc = subprocess.Popen(command, stdout=subprocess.PIPE)
            stdout_change_list, _ = proc.communicate()
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
        for git_hash in change_list_hashes:
            logging.debug("%s: %s", hashnum, git_hash)

            jit_name = get_jit_name(coreclr_args)
            basejit_dir = os.path.join(default_basejit_root_dir, "{}.{}.{}.{}".format(git_hash, coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type))
            basejit_path = os.path.join(basejit_dir, jit_name)
            if os.path.isfile(basejit_path):
                # We found this baseline JIT in our cache; use it!
                coreclr_args.base_jit_path = basejit_path
                logging.info("Using baseline %s", coreclr_args.base_jit_path)
                return

            # It's not in our cache; is there one built by the rolling build to download?
            blob_folder_name = "{}/{}/{}/{}/{}/{}".format(az_builds_root_folder, git_hash, coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type, jit_name)
            blob_uri = "{}/{}".format(az_blob_storage_jitrollingbuild_container_uri, blob_folder_name)
            urls = [ blob_uri ]
            local_files = download_files(urls, basejit_dir, verbose=False, is_azure_storage=True, fail_if_not_found=False)

            if len(local_files) > 0:
                if hashnum > 1:
                    logging.warning("Warning: the baseline found is not built with the first git hash with JIT code changes; there may be extraneous diffs")
                # We expect local_files to be length 1, since we only attempted to download a single file.
                if len(local_files) > 1:
                    logging.error("Error: downloaded more than one file?")

                coreclr_args.base_jit_path = local_files[0]
                logging.info("Downloaded %s", blob_uri)
                logging.info("Using baseline %s", coreclr_args.base_jit_path)
                return

            # We didn't find a baseline; keep looking
            hashnum += 1

        # We ran out of hashes of JIT changes, and didn't find a baseline. Give up.
        logging.error("Error: no baseline JIT found")

    raise RuntimeError("No baseline JIT found")

def get_pintools_path(coreclr_args):
    """ Get the local path where we expect pintools for this OS to be located

    Returns:
        A path to the folder.
    """
    return os.path.join(coreclr_args.spmi_location, "pintools", pintools_current_version, coreclr_args.host_os.lower())

def get_pin_exe_path(coreclr_args):
    """ Get the local path where we expect the pin executable to be located

    Returns:
        A path to the executable.
    """
    root = get_pintools_path(coreclr_args)
    exe = "pin.exe" if coreclr_args.host_os.lower() == "windows" else "pin"
    return os.path.join(root, exe)

def get_inscount_pintool_path(coreclr_args):
    """ Get the local path where we expect the clrjit inscount pintool to be located

    Returns:
        A path to the pintool library.
    """
    if coreclr_args.host_os.lower() == "osx":
        pintool_filename = "libclrjit_inscount.dylib"
    elif coreclr_args.host_os.lower() == "windows":
        pintool_filename = "clrjit_inscount.dll"
    else:
        pintool_filename = "libclrjit_inscount.so"

    return os.path.join(get_pintools_path(coreclr_args), "clrjit_inscount_" + coreclr_args.arch, pintool_filename)

def download_clrjit_pintool(coreclr_args):
    """ Download the pintool package for doing measurements of the JIT from Azure Storage.
    """

    if os.path.isfile(get_pin_exe_path(coreclr_args)):
        return

    pin_dir_path = get_pintools_path(coreclr_args)
    extension = "zip" if coreclr_args.host_os.lower() == "windows" else "tar.gz"
    pintools_rel_path = "{}/{}/{}.{}".format(az_pintools_root_folder, pintools_current_version, coreclr_args.host_os.lower(), extension)
    pintool_uri = "{}/{}".format(az_blob_storage_superpmi_container_uri, pintools_rel_path)
    local_files = download_files([pintool_uri], pin_dir_path, verbose=False, is_azure_storage=True, fail_if_not_found=False)
    if len(local_files) <= 0:
        logging.error("Error: {} not found".format(pintools_rel_path))
        raise RuntimeError("{} not found".format(pintools_rel_path))

def setup_args(args):
    """ Setup the args for SuperPMI to use.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """

    # Start setting up logging.
    # Set up the console logger immediately. Later, after we've parsed some arguments, we'll add the file logger and
    # change the console logger level to the one parsed by the arguments. We need to do this initial setup before the first
    # logging command is executed.
    logger = logging.getLogger()
    logger.setLevel(logging.DEBUG)

    formatter = logging.Formatter("[%(asctime)s] %(message)s", datefmt="%H:%M:%S")

    stream_handler = logging.StreamHandler(sys.stdout)
    stream_handler.setLevel(logging.DEBUG)
    stream_handler.setFormatter(formatter)
    logger.addHandler(stream_handler)

    # Parse the arguments

    coreclr_args = CoreclrArguments(args, require_built_core_root=False, require_built_product_dir=False, require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "mode",  # "mode" is from the `parser.add_subparsers(dest='mode')` call
                        lambda unused: True,
                        "Unable to set mode")

    coreclr_args.verify(args,
                        "log_level",
                        lambda arg: any(arg.upper() == level for level in [ "CRITICAL", "ERROR", "WARNING", "INFO", "DEBUG" ]),
                        "Unable to set log_level {}".format,
                        modify_arg=lambda arg: "INFO" if arg is None else arg.upper())

    coreclr_args.verify(args,
                        "log_file",
                        lambda unused: True,
                        "Unable to set log_file.")

    def setup_spmi_location_arg(spmi_location):
        if spmi_location is None:
            if "SUPERPMI_CACHE_DIRECTORY" in os.environ:
                spmi_location = os.environ["SUPERPMI_CACHE_DIRECTORY"]
                spmi_location = os.path.abspath(spmi_location)
            else:
                spmi_location = os.path.abspath(os.path.join(coreclr_args.artifacts_location, "spmi"))
        return spmi_location

    coreclr_args.verify(args,
                        "spmi_location",
                        lambda unused: True,
                        "Unable to set spmi_location",
                        modify_arg=setup_spmi_location_arg)

    coreclr_args.verify(args,
                        "no_progress",
                        lambda unused: True,
                        "Unable to set no_progress")

    # Finish setting up logging.
    # The spmi_location is the root directory where we put the log file.
    # Log everything to the log file and only the specified verbosity to the console logger.

    # Now, change the stream handler output level.
    stream_handler.setLevel(coreclr_args.log_level)

    log_file = None
    if coreclr_args.log_file is None:
        if hasattr(coreclr_args, "spmi_location"):
            log_file = create_unique_file_name(coreclr_args.spmi_location, "superpmi", "log")
            if not os.path.isdir(coreclr_args.spmi_location):
                os.makedirs(coreclr_args.spmi_location)
    else:
        log_file = coreclr_args.log_file
        log_dir = os.path.dirname(log_file)
        if not os.path.isdir(log_dir):
            print("Creating log directory {} for log file {}".format(log_dir, log_file))
            os.makedirs(log_dir)

    if log_file is not None:
        # If the log file exists, we could use the default behavior and simply append.
        # For now, though, just delete it and warn. We can change behavior later if there's user feedback on it.
        if os.path.isfile(log_file):
            logging.critical("Warning: deleting existing log file %s", log_file)
            os.remove(log_file)
        file_handler = logging.FileHandler(log_file, encoding='utf8')
        file_handler.setLevel(logging.DEBUG)
        file_handler.setFormatter(formatter)
        logger.addHandler(file_handler)
        logging.critical("================ Logging to %s", log_file)

    # Finish verifying the arguments

    def setup_jit_ee_version_arg(jit_ee_version):
        if jit_ee_version is not None:
            # The user passed a specific jit_ee_version on the command-line, so use that
            return jit_ee_version
        return determine_jit_ee_version(coreclr_args)

    def setup_jit_path_arg(jit_path):
        if jit_path is not None:
            return os.path.abspath(jit_path)
        return find_tool(coreclr_args, get_jit_name(coreclr_args), search_path=False)  # It doesn't make sense to search PATH for the JIT dll.

    def setup_error_limit(error_limit):
        if error_limit is None:
            return None
        elif not error_limit.isnumeric():
            return None
        return error_limit

    def verify_jit_ee_version_arg():

        coreclr_args.verify(args,
                            "jit_ee_version",
                            lambda unused: True,
                            "Invalid JIT-EE Version.",
                            modify_arg=setup_jit_ee_version_arg)

    def verify_target_args():

        coreclr_args.verify(args,
                            "target_os",
                            lambda target_os: check_target_os(coreclr_args, target_os),
                            lambda target_os: "Unknown target_os {}\nSupported OS: {}".format(target_os, (", ".join(coreclr_args.valid_host_os))),
                            modify_arg=lambda target_os: target_os if target_os is not None else coreclr_args.host_os) # Default to `host_os`

        coreclr_args.verify(args,
                            "target_arch",
                            lambda target_arch: check_target_arch(coreclr_args, target_arch),
                            lambda target_arch: "Unknown target_arch {}\nSupported architectures: {}".format(target_arch, (", ".join(coreclr_args.valid_arches))),
                            modify_arg=lambda target_arch: target_arch if target_arch is not None else coreclr_args.arch) # Default to `arch`

        coreclr_args.verify(args,
                            "mch_arch",
                            lambda mch_arch: check_mch_arch(coreclr_args, mch_arch),
                            lambda mch_arch: "Unknown mch_arch {}\nSupported architectures: {}".format(mch_arch, (", ".join(coreclr_args.valid_arches))),
                            modify_arg=lambda mch_arch: mch_arch if mch_arch is not None else coreclr_args.target_arch) # Default to `target_arch`

    def verify_superpmi_common_args():

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
                            "error_limit",
                            lambda unused: True,
                            "Unable to set error_limit",
                            modify_arg=setup_error_limit)

        coreclr_args.verify(args,
                            "spmi_log_file",
                            lambda unused: True,
                            "Unable to set spmi_log_file.")

        if coreclr_args.spmi_log_file is not None and not coreclr_args.sequential:
            print("-spmi_log_file requires --sequential")
            sys.exit(1)

    def verify_replay_common_args():

        verify_jit_ee_version_arg()

        coreclr_args.verify(args,
                            "force_download",
                            lambda unused: True,
                            "Unable to set force_download")

        coreclr_args.verify(args,
                            "jit_name",
                            lambda unused: True,
                            "Unable to set jit_name.")

        coreclr_args.verify(args,
                            "altjit",                   # Must be set before `jit_path` (get_jit_name() depends on it)
                            lambda unused: True,
                            "Unable to set altjit.")

        coreclr_args.verify(args,
                            "filter",
                            lambda unused: True,
                            "Unable to set filter.")

        coreclr_args.verify(args,
                            "mch_files",
                            lambda unused: True,
                            "Unable to set mch_files")

        coreclr_args.verify(args,
                            "compile",
                            lambda unused: True,
                            "Method context not valid")

        coreclr_args.verify(args,
                            "private_store",
                            lambda item: True,
                            "Specify private_store or set environment variable SUPERPMI_PRIVATE_STORE to use a private store.",
                            modify_arg=lambda arg: os.environ["SUPERPMI_PRIVATE_STORE"].split(";") if arg is None and "SUPERPMI_PRIVATE_STORE" in os.environ else arg)

    def verify_base_diff_args():

        coreclr_args.verify(args,
                            "base_jit_path",
                            lambda unused: True,
                            "Unable to set base_jit_path")

        coreclr_args.verify(args,
                            "diff_jit_path",
                            os.path.isfile,
                            "Error: JIT not found at diff_jit_path {}".format,
                            modify_arg=setup_jit_path_arg)

        coreclr_args.verify(args,
                            "git_hash",
                            lambda unused: True,
                            "Unable to set git_hash")

        coreclr_args.verify(args,
                            "base_git_hash",
                            lambda unused: True,
                            "Unable to set base_git_hash")

        coreclr_args.verify(args,
                            "jitoption",
                            lambda unused: True,
                            "Unable to set jitoption")

        coreclr_args.verify(args,
                            "base_jit_option",
                            lambda unused: True,
                            "Unable to set base_jit_option.")

        coreclr_args.verify(args,
                            "diff_jit_option",
                            lambda unused: True,
                            "Unable to set diff_jit_option.")

        if coreclr_args.jitoption:
            for o in coreclr_args.jitoption:
                if o.startswith("DOTNET_"):
                    logging.warning("")
                    logging.warning("WARNING: `-jitoption` starts with DOTNET_ : " + o)
                    logging.warning("")

        if coreclr_args.base_jit_option:
            for o in coreclr_args.base_jit_option:
                if o.startswith("DOTNET_"):
                    logging.warning("")
                    logging.warning("WARNING: `-base_jit_option` starts with DOTNET_ : " + o)
                    logging.warning("")

        if coreclr_args.diff_jit_option:
            for o in coreclr_args.diff_jit_option:
                if o.startswith("DOTNET_"):
                    logging.warning("")
                    logging.warning("WARNING: `-diff_jit_option` starts with DOTNET_ : " + o)
                    logging.warning("")


    if coreclr_args.mode == "collect":

        verify_target_args()
        verify_superpmi_common_args()

        coreclr_args.verify(args,
                            "jit_name",  # The replay code checks this, so make sure it's set
                            lambda unused: True,
                            "Unable to set jit_name.")

        coreclr_args.verify(args,
                            "altjit",  # The replay code checks this, so make sure it's set
                            lambda unused: True,
                            "Unable to set altjit.")

        coreclr_args.verify(args,
                            "jitoption",  # The replay code checks this, so make sure it's set
                            lambda unused: True,
                            "Unable to set jitoption")

        coreclr_args.verify(args,
                            "compile",  # The replay code checks this, so make sure it's set
                            lambda unused: True,
                            "Method context not valid")

        coreclr_args.verify(args,
                            "collection_command",
                            lambda unused: True,
                            "Unable to set collection_command.")

        coreclr_args.verify(args,
                            "collection_args",
                            lambda unused: True,
                            "Unable to set collection_args",
                            modify_arg=lambda collection_args: collection_args.split(" ") if collection_args is not None else [])

        coreclr_args.verify(args,
                            "pmi",
                            lambda unused: True,
                            "Unable to set pmi")

        coreclr_args.verify(args,
                            "crossgen2",
                            lambda unused: True,
                            "Unable to set crossgen2")

        coreclr_args.verify(args,
                            "nativeaot",
                            lambda unused: True,
                            "Unable to set nativeaot")

        coreclr_args.verify(args,
                            "assemblies",
                            lambda unused: True,
                            "Unable to set assemblies",
                            modify_arg=lambda items: [item for item in items if os.path.isdir(item) or os.path.isfile(item)])

        coreclr_args.verify(args,
                            "ilc_rsps",
                            lambda unused: True,
                            "Unable to set ilc_rsps",
                            modify_arg=lambda items: [item for item in items if os.path.isdir(item) or os.path.isfile(item)])

        coreclr_args.verify(args,
                            "exclude",
                            lambda unused: True,
                            "Unable to set exclude")

        coreclr_args.verify(args,
                            "pmi_location",
                            lambda unused: True,
                            "Unable to set pmi_location")

        coreclr_args.verify(args,
                            "output_mch_path",
                            lambda output_mch_path: output_mch_path is None or (not os.path.isdir(os.path.abspath(output_mch_path)) and not os.path.isfile(os.path.abspath(output_mch_path))),
                            "Invalid output_mch_path \"{}\"; is it an existing directory or file?".format,
                            modify_arg=lambda output_mch_path: None if output_mch_path is None else os.path.abspath(output_mch_path))

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
                            "skip_toc_step",
                            lambda unused: True,
                            "Unable to set skip_toc_step.")

        coreclr_args.verify(args,
                            "ci",
                            lambda unused: True,
                            "Unable to set ci.")

        coreclr_args.verify(args,
                            "clean",
                            lambda unused: True,
                            "Unable to set clean.")

        coreclr_args.verify(args,
                            "use_zapdisable",
                            lambda unused: True,
                            "Unable to set use_zapdisable")

        coreclr_args.verify(args,
                            "tiered_compilation",
                            lambda unused: True,
                            "Unable to set tiered_compilation")

        coreclr_args.verify(args,
                            "tiered_pgo",
                            lambda unused: True,
                            "Unable to set tiered_pgo")

        coreclr_args.verify(args,
                            "pmi_path",
                            lambda unused: True,
                            "Unable to set pmi_path")

        if (args.collection_command is None) and (args.pmi is False) and (args.crossgen2 is False) and (args.nativeaot is False) and not coreclr_args.skip_collection_step:
            print("Either a collection command or `--pmi` or `--crossgen2` or `--nativeaot` or `--skip_collection_step` must be specified")
            sys.exit(1)

        if (args.collection_command is not None) and (len(args.assemblies) > 0):
            print("Don't specify `-assemblies` if a collection command is given")
            sys.exit(1)

        if (args.collection_command is not None) and (len(args.ilc_rsps) > 0):
            print("Don't specify `-ilc_rsps` if a collection command is given")
            sys.exit(1)

        if (args.collection_command is not None) and (len(args.exclude) > 0):
            print("Don't specify `-exclude` if a collection command is given")
            sys.exit(1)

        if ((args.pmi is True) or (args.crossgen2 is True)) and (len(args.assemblies) == 0):
            print("Specify `-assemblies` if `--pmi` or `--crossgen2` is given")
            sys.exit(1)

        if ((args.nativeaot is True)) and (len(args.ilc_rsps) == 0):
            print("Specify `-ilc_rsps` if `--nativeaot` is given")
            sys.exit(1)

        if not args.pmi:
            if args.pmi_path is not None:
                logging.warning("Warning: -pmi_path is set but --pmi is not.")
            if args.pmi_location is not None:
                logging.warning("Warning: -pmi_location is set but --pmi is not.")

        if args.collection_command is None and args.merge_mch_files is not True and not coreclr_args.skip_collection_step:
            assert args.collection_args is None
            if args.nativeaot:
                assert len(args.ilc_rsps) > 0
            else:
                assert (args.pmi is True) or (args.crossgen2 is True)
                assert len(args.assemblies) > 0

        if coreclr_args.merge_mch_files:
            assert len(coreclr_args.mch_files) > 0
            coreclr_args.skip_collection_step = True

        if coreclr_args.crossgen2:
            # Can we find crossgen2?
            crossgen2_tool_name = "crossgen2.dll"
            crossgen2_tool_path = os.path.abspath(os.path.join(coreclr_args.core_root, "crossgen2", crossgen2_tool_name))
            if not os.path.exists(crossgen2_tool_path):
                print("`--crossgen2` is specified, but couldn't find " + crossgen2_tool_path + ". (Is it built?)")
                sys.exit(1)

            # Which dotnet will we use to run it?
            dotnet_script_name = "dotnet.cmd" if platform.system() == "Windows" else "dotnet.sh"
            dotnet_tool_path = os.path.abspath(os.path.join(coreclr_args.runtime_repo_location, dotnet_script_name))
            if not os.path.exists(dotnet_tool_path):
                dotnet_tool_name = determine_dotnet_tool_name(coreclr_args)
                dotnet_tool_path = find_tool(coreclr_args, dotnet_tool_name, search_core_root=False, search_product_location=False, search_path=True, throw_on_not_found=False)  # Only search path

            coreclr_args.crossgen2_tool_path = crossgen2_tool_path
            coreclr_args.dotnet_tool_path = dotnet_tool_path
            logging.debug("Using crossgen2 tool %s", coreclr_args.crossgen2_tool_path)
            if coreclr_args.dotnet_tool_path is not None:
                logging.debug("Using dotnet tool %s", coreclr_args.dotnet_tool_path)

        if coreclr_args.nativeaot:
            # Can we find nativeaot?
            nativeaot_tool_name = "ilc.exe" if platform.system() == "Windows" else "ilc"
            nativeaot_tool_path = os.path.abspath(os.path.join(coreclr_args.core_root, "ilc-published", nativeaot_tool_name))
            nativeaot_aotsdk_path = os.path.abspath(os.path.join(coreclr_args.core_root, "aotsdk"))
            if not os.path.exists(nativeaot_tool_path):
                print("`--nativeaot` is specified, but couldn't find " + nativeaot_tool_path + ". (Is it built?)")
                sys.exit(1)
            if not os.path.exists(nativeaot_aotsdk_path):
                print("`--nativeaot` is specified, but couldn't find directory " + nativeaot_aotsdk_path + ". (Is it built?)")
                sys.exit(1)

            # dotnet will not be used to run ilc.exe, but we need to establish the dotnet tool path regardless
            dotnet_script_name = "dotnet.cmd" if platform.system() == "Windows" else "dotnet.sh"
            dotnet_tool_path = os.path.abspath(os.path.join(coreclr_args.runtime_repo_location, dotnet_script_name))
            if not os.path.exists(dotnet_tool_path):
                dotnet_tool_name = determine_dotnet_tool_name(coreclr_args)
                dotnet_tool_path = find_tool(coreclr_args, dotnet_tool_name, search_core_root=False, search_product_location=False, search_path=True, throw_on_not_found=False)  # Only search path

            coreclr_args.nativeaot_tool_path = nativeaot_tool_path
            coreclr_args.nativeaot_aotsdk_path = nativeaot_aotsdk_path
            coreclr_args.dotnet_tool_path = dotnet_tool_path
            logging.debug("Using nativeaot tool %s", coreclr_args.nativeaot_tool_path)
            if coreclr_args.dotnet_tool_path is not None:
                logging.debug("Using dotnet tool %s", coreclr_args.dotnet_tool_path)

        if coreclr_args.temp_dir is not None:
            coreclr_args.temp_dir = os.path.abspath(coreclr_args.temp_dir)
            logging.debug("Using temp_dir %s", coreclr_args.temp_dir)

        if coreclr_args.collection_command is not None:
            if os.path.isfile(coreclr_args.collection_command):
                coreclr_args.collection_command = os.path.abspath(coreclr_args.collection_command)
            else:
                # Look on path and in Core_Root. Searching Core_Root is useful so you can just specify "corerun.exe" as the collection command in it can be found.
                collection_tool_path = find_tool(coreclr_args, coreclr_args.collection_command, search_core_root=True, search_product_location=False, search_path=True, throw_on_not_found=False)
                if collection_tool_path is None:
                    print("Couldn't find collection command \"{}\"".format(coreclr_args.collection_command))
                    sys.exit(1)
                coreclr_args.collection_command = collection_tool_path
                logging.info("Using collection command from PATH: \"%s\"", coreclr_args.collection_command)

    elif coreclr_args.mode == "replay":

        verify_target_args()
        verify_superpmi_common_args()
        verify_replay_common_args()

        coreclr_args.verify(args,
                            "jit_path",
                            os.path.isfile,
                            "Error: JIT not found at jit_path {}".format,
                            modify_arg=setup_jit_path_arg)

        coreclr_args.verify(args,
                            "jitoption",
                            lambda unused: True,
                            "Unable to set jitoption")

        jit_in_product_location = False
        if coreclr_args.product_location.lower() in coreclr_args.jit_path.lower():
            jit_in_product_location = True

        determined_arch = None
        determined_build_type = None
        if jit_in_product_location:
            # Get os/arch/flavor directory, e.g. split "F:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked" with "F:\gh\runtime\artifacts\bin\coreclr"
            # yielding
            # [0]: ""
            # [1]: "\windows.x64.Checked"
            standard_location_split = os.path.dirname(coreclr_args.jit_path).split(os.path.dirname(coreclr_args.product_location))
            assert coreclr_args.host_os in standard_location_split[1]

            # Get arch/flavor. Remove leading slash.
            specialized_path = standard_location_split[1].split(os.path.sep)[1]

            # Split components: "windows.x64.Checked" into:
            # [0]: "windows"
            # [1]: "x64"
            # [2]: "Checked"
            determined_split = specialized_path.split(".")

            determined_arch = determined_split[1]
            determined_build_type = determined_split[2]

        # Make a more intelligent decision about the arch and build type
        # based on the path of the jit passed
        if jit_in_product_location and coreclr_args.build_type not in coreclr_args.jit_path:
            coreclr_args.verify(determined_arch.lower(),
                                "arch",
                                lambda unused: True,
                                "Unable to set arch")

            coreclr_args.verify(determined_build_type,
                                "build_type",
                                coreclr_args.check_build_type,
                                "Invalid build_type")

        if coreclr_args.jitoption:
            for o in coreclr_args.jitoption:
                if o.startswith("DOTNET_"):
                    logging.warning("")
                    logging.warning("WARNING: `-jitoption` starts with DOTNET_ : " + o)
                    logging.warning("")

    elif coreclr_args.mode == "asmdiffs":

        verify_target_args()
        verify_superpmi_common_args()
        verify_replay_common_args()
        verify_base_diff_args()

        coreclr_args.verify(args,
                            "gcinfo",
                            lambda unused: True,
                            "Unable to set gcinfo.")

        coreclr_args.verify(args,
                            "debuginfo",
                            lambda unused: True,
                            "Unable to set debuginfo.")

        coreclr_args.verify(args,
                            "alignloops",
                            lambda unused: True,
                            "Unable to set alignloops.")

        coreclr_args.verify(args,
                            "diff_jit_dump",
                            lambda unused: True,
                            "Unable to set diff_jit_dump.")

        coreclr_args.verify(args,
                            "tag",
                            lambda unused: True,
                            "Unable to set tag.",
                            modify_arg=lambda arg: make_safe_filename(arg) if arg is not None else arg)

        coreclr_args.verify(args,
                            "metrics",
                            lambda unused: True,
                            "Unable to set metrics.")

        coreclr_args.verify(args,
                            "diff_with_release",
                            lambda unused: True,
                            "Unable to set diff_with_release.")

        coreclr_args.verify(args,
                            "git_diff",
                            lambda unused: True,
                            "Unable to set git_diff.")

        process_base_jit_path_arg(coreclr_args)

        jit_in_product_location = False
        if coreclr_args.product_location.lower() in coreclr_args.base_jit_path.lower():
            jit_in_product_location = True

        determined_arch = None
        determined_build_type = None
        if jit_in_product_location:
            # Get os/arch/flavor directory, e.g. split "F:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked" with "F:\gh\runtime\artifacts\bin\coreclr"
            # yielding
            # [0]: ""
            # [1]: "\windows.x64.Checked"
            standard_location_split = os.path.dirname(coreclr_args.base_jit_path).split(os.path.dirname(coreclr_args.product_location))
            assert coreclr_args.host_os in standard_location_split[1]

            # Get arch/flavor. Remove leading slash.
            specialized_path = standard_location_split[1].split(os.path.sep)[1]

            # Split components: "windows.x64.Checked" into:
            # [0]: "windows"
            # [1]: "x64"
            # [2]: "Checked"
            determined_split = specialized_path.split(".")

            determined_arch = determined_split[1]
            determined_build_type = determined_split[2]

        # Make a more intelligent decision about the arch and build type
        # based on the path of the jit passed
        if jit_in_product_location and coreclr_args.build_type not in coreclr_args.base_jit_path:
            coreclr_args.verify(determined_build_type,
                                "build_type",
                                coreclr_args.check_build_type,
                                "Invalid build_type")

        if jit_in_product_location and coreclr_args.arch not in coreclr_args.base_jit_path:
            coreclr_args.verify(determined_arch.lower(),
                                "arch",
                                lambda unused: True,
                                "Unable to set arch")

        coreclr_args.verify(determine_coredis_tools(coreclr_args),
                            "coredistools_location",
                            os.path.isfile,
                            "Unable to find coredistools.")

    elif coreclr_args.mode == "tpdiff":

        verify_target_args()
        verify_superpmi_common_args()
        verify_replay_common_args()
        verify_base_diff_args()

        coreclr_args.verify(coreclr_args.arch,
                            "arch",
                            lambda arch: arch == "x86" or arch == "x64",
                            "Throughput measurements not supported on platform {}".format(coreclr_args.arch))

        coreclr_args.verify(determine_coredis_tools(coreclr_args),
                            "coredistools_location",
                            os.path.isfile,
                            "Unable to find coredistools.")

        process_base_jit_path_arg(coreclr_args)
        download_clrjit_pintool(coreclr_args)

    elif coreclr_args.mode == "upload":

        verify_target_args()
        verify_jit_ee_version_arg()

        coreclr_args.verify(args,
                            "az_storage_key",
                            lambda item: item is not None,
                            "Specify az_storage_key or set environment variable CLRJIT_AZ_KEY to the key to use.",
                            modify_arg=lambda arg: os.environ["CLRJIT_AZ_KEY"] if arg is None and "CLRJIT_AZ_KEY" in os.environ else arg)

        coreclr_args.verify(args,
                            "mch_files",
                            lambda unused: True,
                            "Unable to set mch_files")

    elif coreclr_args.mode == "upload-private":

        verify_target_args()
        verify_jit_ee_version_arg()

        coreclr_args.verify(args,
                            "mch_files",
                            lambda unused: True,
                            "Unable to set mch_files")

        coreclr_args.verify(args,
                            "private_store",
                            lambda unused: True,
                            "Unable to set private_store")

        if not os.path.isdir(coreclr_args.private_store):
            print("Error: private store directory '" + coreclr_args.private_store + "' not found.")
            sys.exit(1)

        # Safety measure: don't allow CLRJIT_AZ_KEY to be set if we are uploading to a private store.
        # Note that this should be safe anyway, since we're publishing something private, not public.
        if "CLRJIT_AZ_KEY" in os.environ:
            print("Error: environment variable CLRJIT_AZ_KEY is set, but command is `upload-private`, not `upload`. That is not allowed.")
            sys.exit(1)

    elif coreclr_args.mode == "download":

        verify_target_args()
        verify_jit_ee_version_arg()

        coreclr_args.verify(args,
                            "force_download",
                            lambda unused: True,
                            "Unable to set force_download")

        coreclr_args.verify(args,
                            "filter",
                            lambda unused: True,
                            "Unable to set filter.")

        coreclr_args.verify(args,
                            "mch_files",
                            lambda unused: True,
                            "Unable to set mch_files")

        coreclr_args.verify(args,
                            "private_store",
                            lambda item: True,
                            "Specify private_store or set environment variable SUPERPMI_PRIVATE_STORE to use a private store.",
                            modify_arg=lambda arg: os.environ["SUPERPMI_PRIVATE_STORE"].split(";") if arg is None and "SUPERPMI_PRIVATE_STORE" in os.environ else arg)

    elif coreclr_args.mode == "list-collections":

        verify_target_args()
        verify_jit_ee_version_arg()

        coreclr_args.verify(args,
                            "all",
                            lambda unused: True,
                            "Unable to set all")

        coreclr_args.verify(args,
                            "local",
                            lambda unused: True,
                            "Unable to set local")

    elif coreclr_args.mode == "merge-mch":

        coreclr_args.verify(args,
                            "output_mch_path",
                            lambda output_mch_path: not os.path.isdir(os.path.abspath(output_mch_path)) and not os.path.isfile(os.path.abspath(output_mch_path)),
                            "Invalid output_mch_path \"{}\"; is it an existing directory or file?".format,
                            modify_arg=lambda output_mch_path: os.path.abspath(output_mch_path))

        coreclr_args.verify(args,
                            "pattern",
                            lambda unused: True,
                            "Unable to set pattern")

        coreclr_args.verify(args,
                            "ci",
                            lambda unused: True,
                            "Unable to set ci.")

    if coreclr_args.mode == "replay" or coreclr_args.mode == "asmdiffs" or coreclr_args.mode == "tpdiff" or coreclr_args.mode == "download":
        if hasattr(coreclr_args, "private_store") and coreclr_args.private_store is not None:
            logging.info("Using private stores:")
            for path in coreclr_args.private_store:
                logging.info("  %s", path)

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

    # Parse the arguments.

    coreclr_args = setup_args(args)

    #
    # Run the selected command
    #

    success = True

    if coreclr_args.mode == "collect":
        # Start a new SuperPMI Collection.

        begin_time = datetime.datetime.now()

        logging.info("SuperPMI collect")
        logging.debug("------------------------------------------------------------")
        logging.debug("Start time: %s", begin_time.strftime("%H:%M:%S"))

        collection = SuperPMICollect(coreclr_args)
        success = collection.collect()

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        logging.debug("Finish time: %s", end_time.strftime("%H:%M:%S"))
        logging.debug("Elapsed time: %s", elapsed_time)

    elif coreclr_args.mode == "replay":
        # Start a new SuperPMI Replay

        local_mch_paths = process_mch_files_arg(coreclr_args)
        mch_files = get_mch_files_for_replay(local_mch_paths, coreclr_args.filter)
        if mch_files is None:
            return 1

        begin_time = datetime.datetime.now()

        logging.info("SuperPMI replay")
        logging.debug("------------------------------------------------------------")
        logging.debug("Start time: %s", begin_time.strftime("%H:%M:%S"))

        jit_path = coreclr_args.jit_path

        logging.info("JIT Path: %s", jit_path)

        logging.info("Using MCH files:")
        for mch_file in mch_files:
            logging.info("  %s", mch_file)

        replay = SuperPMIReplay(coreclr_args, mch_files, jit_path)
        success = replay.replay()

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        logging.debug("Finish time: %s", end_time.strftime("%H:%M:%S"))
        logging.debug("Elapsed time: %s", elapsed_time)

    elif coreclr_args.mode == "asmdiffs":
        # Start a new SuperPMI Replay with AsmDiffs

        local_mch_paths = process_mch_files_arg(coreclr_args)
        mch_files = get_mch_files_for_replay(local_mch_paths, coreclr_args.filter)
        if mch_files is None:
            return 1

        begin_time = datetime.datetime.now()

        logging.info("SuperPMI ASM diffs")
        logging.debug("------------------------------------------------------------")
        logging.debug("Start time: %s", begin_time.strftime("%H:%M:%S"))

        base_jit_path = coreclr_args.base_jit_path
        diff_jit_path = coreclr_args.diff_jit_path

        if coreclr_args.diff_with_release:
            logging.info("Diff between Checked and Release.")
        logging.info("Base JIT Path: %s", base_jit_path)
        logging.info("Diff JIT Path: %s", diff_jit_path)

        logging.info("Using MCH files:")
        for mch_file in mch_files:
            logging.info("  %s", mch_file)

        asm_diffs = SuperPMIReplayAsmDiffs(coreclr_args, mch_files, base_jit_path, diff_jit_path)
        success = asm_diffs.replay_with_asm_diffs()

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        logging.debug("Finish time: %s", end_time.strftime("%H:%M:%S"))
        logging.debug("Elapsed time: %s", elapsed_time)

    elif coreclr_args.mode == "tpdiff":
        local_mch_paths = process_mch_files_arg(coreclr_args)
        mch_files = get_mch_files_for_replay(local_mch_paths, coreclr_args.filter)
        if mch_files is None:
            return 1

        begin_time = datetime.datetime.now()

        logging.info("SuperPMI throughput diff")
        logging.debug("------------------------------------------------------------")
        logging.debug("Start time: %s", begin_time.strftime("%H:%M:%S"))

        base_jit_path = coreclr_args.base_jit_path
        diff_jit_path = coreclr_args.diff_jit_path

        logging.info("Base JIT Path: %s", base_jit_path)
        logging.info("Diff JIT Path: %s", diff_jit_path)

        logging.info("Using MCH files:")
        for mch_file in mch_files:
            logging.info("  %s", mch_file)

        tp_diff = SuperPMIReplayThroughputDiff(coreclr_args, mch_files, base_jit_path, diff_jit_path)
        success = tp_diff.replay_with_throughput_diff()

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        logging.debug("Finish time: %s", end_time.strftime("%H:%M:%S"))
        logging.debug("Elapsed time: %s", elapsed_time)

    elif coreclr_args.mode == "upload":

        begin_time = datetime.datetime.now()

        logging.info("SuperPMI upload")
        logging.debug("------------------------------------------------------------")
        logging.debug("Start time: %s", begin_time.strftime("%H:%M:%S"))

        upload_mch(coreclr_args)

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        logging.debug("Finish time: %s", end_time.strftime("%H:%M:%S"))
        logging.debug("Elapsed time: %s", elapsed_time)

    elif coreclr_args.mode == "upload-private":

        begin_time = datetime.datetime.now()

        logging.info("SuperPMI upload-private")
        logging.debug("------------------------------------------------------------")
        logging.debug("Start time: %s", begin_time.strftime("%H:%M:%S"))

        upload_private_mch(coreclr_args)

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        logging.debug("Finish time: %s", end_time.strftime("%H:%M:%S"))
        logging.debug("Elapsed time: %s", elapsed_time)

    elif coreclr_args.mode == "download":

        begin_time = datetime.datetime.now()

        logging.info("SuperPMI download")
        logging.debug("------------------------------------------------------------")
        logging.debug("Start time: %s", begin_time.strftime("%H:%M:%S"))

        # Processing the arg does the download and caching
        process_mch_files_arg(coreclr_args)

        end_time = datetime.datetime.now()
        elapsed_time = end_time - begin_time

        logging.debug("Finish time: %s", end_time.strftime("%H:%M:%S"))
        logging.debug("Elapsed time: %s", elapsed_time)

    elif coreclr_args.mode == "list-collections":
        if coreclr_args.local:
            list_collections_local_command(coreclr_args)
        else:
            list_collections_command(coreclr_args)

    elif coreclr_args.mode == "merge-mch":
        success = merge_mch(coreclr_args)

    else:
        raise NotImplementedError(coreclr_args.mode)

    return 0 if success else 1

################################################################################
# __main__
################################################################################


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
