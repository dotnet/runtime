#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_collect_setup.py
#
# Notes:
#
# Script to setup directory structure required to perform SuperPMI collection in CI.
# It does the following steps:
# 1.  Create `correlation_payload_directory` that contains files from CORE_ROOT, src\coreclr\scripts.
#     This directory is the one that is sent to all the helix machines that perform SPMI collections.
# 2.  For PMI collections, clone dotnet/jitutils, build it and then copy the `pmi.dll` to
#     `correlation_payload_directory` folder.
# 3.  For PMI/crossgen2 collections, the `input_directory` directory contains the set of assemblies
#     to collect over. This script will partition these folders into equal buckets of approximately
#     `max_size` bytes and stores them under the workitem payload directory. Each sub-folder inside
#     this directory is sent to an individual helix machine to do SPMI collection on. E.g. for
#     `input_directory` to be run on libraries, the parameter is the path to `CORE_ROOT` folder and
#     this script will copy `max_size` bytes of those files under
#     `payload/collectAssembliesDirectory/libraries/0/binaries`,
#     `payload/collectAssembliesDirectory/libraries/1/binaries` and so forth.
# 4.  For benchmarks collections, a specialized script is called to set up the benchmarks collection.
# 5.  Lastly, it sets the pipeline variables.
#
# The helix queues used are defined below in the code, in the `helix_queue` variable.
# Note that this is unfortunate as it doesn't get updated when the common code in
# eng/pipelines/coreclr/templates/helix-queues-setup.yml is updated.
#
################################################################################
################################################################################

import argparse
import os
import shutil
import stat

from coreclr_arguments import *
from jitutil import run_command, copy_directory, copy_files, set_pipeline_variable, ChangeDir, TempDir, determine_jit_name, \
    get_assembly_simple_name, is_managed_assembly

# Start of parser object creation.

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-collection_type", required=True, help="Type of the SPMI collection to be done (nativeaot, crossgen2, pmi, run, run_tiered, run_pgo, run_pgo_optrepeat)")
parser.add_argument("-collection_name", required=True, help="Name of the SPMI collection to be done (e.g., libraries, libraries_tests, coreclr_tests, benchmarks, aspnet2, aspire)")
parser.add_argument("-payload_directory", required=True, help="Path to payload directory to create: subdirectories are created for the correlation payload as well as the per-partition work items")
parser.add_argument("-source_directory", required=True, help="Path to source directory")
parser.add_argument("-core_root_directory", required=True, help="Path to Core_Root directory")
parser.add_argument("-release_core_root_directory", required=True, help="Path to release Core_Root directory")
parser.add_argument("-arch", required=True, help="Architecture")
parser.add_argument("-platform", required=True, help="OS platform")
parser.add_argument("-mch_file_tag", help="Tag to be used to mch files")
parser.add_argument("-input_directory", help="Directory containing assemblies which SuperPMI will use for collection (for pmi/crossgen2 collections)")
parser.add_argument("-max_size", help="Max size of each partition in MB (for pmi/crossgen2 collections)")
parser.add_argument("-public_queues", action="store_true", help="Whether to set up for public queues (or internal ones, if not specified)")
parser.add_argument("-target_os", help="Target OS for cross-targeting crossgen2 collections (e.g., 'browser' for wasm). Defaults to the host platform.")
parser.add_argument("-target_arch", help="Target architecture for cross-targeting crossgen2 collections (e.g., 'wasm'). Defaults to the host architecture.")
parser.add_argument("-target_runtime_pack_directory", help="Path to the target's CoreCLR runtime pack `runtimes/<rid>` directory. Required for cross-targeting crossgen2 collections: its managed assemblies are the crossgen2 reference set and, for the 'libraries' collection, part of the assemblies to collect over.")

is_windows = platform.system() == "Windows"

legal_collection_types = [ "nativeaot", "crossgen2", "pmi", "run", "run_tiered", "run_pgo", "run_pgo_optrepeat" ]

directories_to_ignore = [
    "runtimes", # This appears to be the result of a nuget package that includes a bunch of native code
]

native_binaries_to_ignore = [
    "api-ms-win-core-console-l1-1-0.dll",
    "api-ms-win-core-datetime-l1-1-0.dll",
    "api-ms-win-core-debug-l1-1-0.dll",
    "api-ms-win-core-errorhandling-l1-1-0.dll",
    "api-ms-win-core-file-l1-1-0.dll",
    "api-ms-win-core-file-l1-2-0.dll",
    "api-ms-win-core-file-l2-1-0.dll",
    "api-ms-win-core-handle-l1-1-0.dll",
    "api-ms-win-core-heap-l1-1-0.dll",
    "api-ms-win-core-interlocked-l1-1-0.dll",
    "api-ms-win-core-libraryloader-l1-1-0.dll",
    "api-ms-win-core-localization-l1-2-0.dll",
    "api-ms-win-core-memory-l1-1-0.dll",
    "api-ms-win-core-namedpipe-l1-1-0.dll",
    "api-ms-win-core-processenvironment-l1-1-0.dll",
    "api-ms-win-core-processthreads-l1-1-0.dll",
    "api-ms-win-core-processthreads-l1-1-1.dll",
    "api-ms-win-core-profile-l1-1-0.dll",
    "api-ms-win-core-rtlsupport-l1-1-0.dll",
    "api-ms-win-core-string-l1-1-0.dll",
    "api-ms-win-core-synch-l1-1-0.dll",
    "api-ms-win-core-synch-l1-2-0.dll",
    "api-ms-win-core-sysinfo-l1-1-0.dll",
    "api-ms-win-core-timezone-l1-1-0.dll",
    "api-ms-win-core-util-l1-1-0.dll",
    "api-ms-win-crt-conio-l1-1-0.dll",
    "api-ms-win-crt-convert-l1-1-0.dll",
    "api-ms-win-crt-environment-l1-1-0.dll",
    "api-ms-win-crt-filesystem-l1-1-0.dll",
    "api-ms-win-crt-heap-l1-1-0.dll",
    "api-ms-win-crt-locale-l1-1-0.dll",
    "api-ms-win-crt-math-l1-1-0.dll",
    "api-ms-win-crt-multibyte-l1-1-0.dll",
    "api-ms-win-crt-private-l1-1-0.dll",
    "api-ms-win-crt-process-l1-1-0.dll",
    "api-ms-win-crt-runtime-l1-1-0.dll",
    "api-ms-win-crt-stdio-l1-1-0.dll",
    "api-ms-win-crt-string-l1-1-0.dll",
    "api-ms-win-crt-time-l1-1-0.dll",
    "api-ms-win-crt-utility-l1-1-0.dll",
    "clretwrc.dll",
    "clrgc.dll",
    "clrjit.dll",
    "clrjit_universal_arm_arm.dll",
    "clrjit_universal_arm_arm64.dll",
    "clrjit_universal_arm_x64.dll",
    "clrjit_universal_arm_x86.dll",
    "clrjit_universal_arm64_arm64.dll",
    "clrjit_universal_arm64_x64.dll",
    "clrjit_unix_arm_arm.dll",
    "clrjit_unix_arm_arm64.dll",
    "clrjit_unix_arm_x64.dll",
    "clrjit_unix_arm_x86.dll",
    "clrjit_unix_arm64_arm64.dll",
    "clrjit_unix_arm64_x64.dll",
    "clrjit_unix_armel_arm.dll",
    "clrjit_unix_armel_arm64.dll",
    "clrjit_unix_armel_x64.dll",
    "clrjit_unix_armel_x86.dll",
    "clrjit_unix_osx_arm64_arm64.dll",
    "clrjit_unix_osx_arm64_x64.dll",
    "clrjit_unix_x64_arm64.dll",
    "clrjit_unix_x64_x64.dll",
    "clrjit_win_arm_arm.dll",
    "clrjit_win_arm_arm64.dll",
    "clrjit_win_arm_x64.dll",
    "clrjit_win_arm_x86.dll",
    "clrjit_win_arm64_arm64.dll",
    "clrjit_win_arm64_x64.dll",
    "clrjit_win_x64_arm64.dll",
    "clrjit_win_x64_x64.dll",
    "clrjit_win_x86_arm.dll",
    "clrjit_win_x86_arm64.dll",
    "clrjit_win_x86_x64.dll",
    "clrjit_win_x86_x86.dll",
    "coreclr.dll",
    "CoreConsole.exe",
    "coredistools.dll",
    "CoreRun.exe",
    "CoreShim.dll",
    "createdump.exe",
    "crossgen.exe",
    "crossgen2.exe",
    "dbgshim.dll",
    "e_sqlite3.dll",
    "FileCheck.exe",
    "ilasm.exe",
    "ilc.exe",
    "ildasm.exe",
    "jitinterface_arm.dll",
    "jitinterface_arm64.dll",
    "jitinterface_x64.dll",
    "jitinterface_x86.dll",
    "KernelTraceControl.dll",
    "KernelTraceControl.Win61.dll",
    "llvm-mca.exe",
    "mcs.exe",
    "Microsoft.DiaSymReader.Native.dll",
    "Microsoft.DiaSymReader.Native.amd64.dll",
    "Microsoft.DiaSymReader.Native.x86.dll",
    "mscordaccore.dll",
    "mscordbi.dll",
    "mscorrc.dll",
    "msdia140.dll",
    "msquic.dll",
    "msvcp140.dll",
    "NativeLibrary.dll",
    "R2RDump.exe",
    "R2RTest.exe",
    "sni.dll",
    "SuperFileCheck.exe",
    "superpmi-shim-collector.dll",
    "superpmi-shim-counter.dll",
    "superpmi-shim-simple.dll",
    "superpmi.exe",
    "System.CommandLine.resources.dll", # Managed, but uninteresting
    "System.IO.Compression.Native.dll",
    "ucrtbase.dll",
    "UnloadableAssembly.dll",
    "vcruntime140.dll",
    "vcruntime140_1.dll",
    "xunit.console.exe",
]

# Managed Core_Root assemblies that are not staged for cross-target crossgen2 collections:
# COM interop wrappers and out-of-band libraries that only support Windows (or other
# non-browser platforms), whose code isn't representative of what runs on the target.
core_root_assemblies_to_ignore_for_cross_target = [
    "Dia2Lib.dll",
    "Microsoft.DiaSymReader.dll",
    "Microsoft.Extensions.Hosting.WindowsServices.dll",
    "Microsoft.Extensions.Logging.EventLog.dll",
    "Microsoft.Win32.Registry.AccessControl.dll",
    "Microsoft.Win32.SystemEvents.dll",
    "System.Data.Odbc.dll",
    "System.Data.OleDb.dll",
    "System.Diagnostics.EventLog.dll",
    "System.Diagnostics.EventLog.Messages.dll",
    "System.Diagnostics.PerformanceCounter.dll",
    "System.DirectoryServices.dll",
    "System.DirectoryServices.AccountManagement.dll",
    "System.DirectoryServices.Protocols.dll",
    "System.IO.Ports.dll",
    "System.Management.dll",
    "System.Net.Http.WinHttpHandler.dll",
    "System.Security.Cryptography.ProtectedData.dll",
    "System.ServiceProcess.ServiceController.dll",
    "System.Speech.dll",
    "System.Windows.Extensions.dll",
    "TraceReloggerLib.dll",
]

# Test infrastructure assemblies that are copied next to every test in the coreclr test
# build. Cross-target coreclr_tests collections stage one copy as a crossgen2 reference
# rather than compiling each copy.
coreclr_test_infrastructure_assemblies = [
    "TestLibrary.dll",
    "XUnitWrapperLibrary.dll",
]

MAX_FILES_COUNT = 1500


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
                        "payload_directory",
                        lambda unused: True,
                        "Unable to set payload_directory",
                        modify_arg=lambda payload_directory: os.path.abspath(payload_directory))

    coreclr_args.verify(args,
                        "source_directory",
                        lambda source_directory: os.path.isdir(source_directory),
                        "source_directory doesn't exist",
                        modify_arg=lambda source_directory: os.path.abspath(source_directory))

    check_dir = os.path.join(coreclr_args.source_directory, 'src', 'coreclr', 'scripts')
    if not os.path.isdir(check_dir):
        print("Specified directory {0} doesn't looks like a source directory".format(coreclr_args.source_directory))
        sys.exit(1)

    coreclr_args.verify(args,
                        "core_root_directory",
                        lambda core_root_directory: os.path.isdir(core_root_directory),
                        "core_root_directory doesn't exist",
                        modify_arg=lambda core_root_directory: os.path.abspath(core_root_directory))

    coreclr_args.verify(args,
                        "release_core_root_directory",
                        lambda release_core_root_directory: os.path.isdir(release_core_root_directory),
                        "release_core_root_directory doesn't exist",
                        modify_arg=lambda release_core_root_directory: os.path.abspath(release_core_root_directory))

    coreclr_args.verify(args,
                        "arch",
                        lambda unused: True,
                        "Unable to set arch")

    coreclr_args.verify(args,
                        "platform",
                        lambda unused: True,
                        "Unable to set platform")

    coreclr_args.verify(args,
                        "mch_file_tag",
                        lambda unused: True,
                        "Unable to set mch_file_tag")

    coreclr_args.verify(args,
                        "collection_name",
                        lambda unused: True,
                        "Unable to set collection_name")

    coreclr_args.verify(args,
                        "collection_type",
                        lambda collection_type: collection_type in legal_collection_types,
                        "Please specify one of the allowed collection types: " + ' '.join(legal_collection_types))

    coreclr_args.verify(args,
                        "input_directory",
                        lambda input_directory: coreclr_args.collection_type not in [ "pmi", "crossgen2" ] or os.path.isdir(input_directory),
                        "input_directory doesn't exist",
                        modify_arg=lambda input_directory: None if input_directory is None else os.path.abspath(input_directory))

    coreclr_args.verify(args,
                        "max_size",
                        lambda max_size: coreclr_args.collection_type not in [ "pmi", "crossgen2", "nativeaot" ] or max_size > 0,
                        "Please enter valid positive numeric max_size",
                        modify_arg=lambda max_size: int(
                            max_size) * 1000 * 1000 if max_size is not None and max_size.isnumeric() else 0
                        # Convert to MB
                        )

    coreclr_args.verify(args,
                        "public_queues",
                        lambda unused: True,
                        "Whether to use public queues (or, if not specified, internal queues)")

    coreclr_args.verify(args,
                        "target_os",
                        lambda unused: True,
                        "Unable to set target_os",
                        modify_arg=lambda target_os: target_os if target_os else coreclr_args.platform)

    coreclr_args.verify(args,
                        "target_arch",
                        lambda unused: True,
                        "Unable to set target_arch",
                        modify_arg=lambda target_arch: target_arch if target_arch else coreclr_args.arch)

    is_cross_target_crossgen2 = coreclr_args.collection_type == "crossgen2" and \
        ((coreclr_args.target_os != coreclr_args.platform) or (coreclr_args.target_arch != coreclr_args.arch))
    coreclr_args.verify(args,
                        "target_runtime_pack_directory",
                        lambda target_runtime_pack_directory: not is_cross_target_crossgen2 or
                            (target_runtime_pack_directory is not None and os.path.isdir(target_runtime_pack_directory)),
                        "target_runtime_pack_directory is required for cross-target crossgen2 collections and must exist",
                        modify_arg=lambda target_runtime_pack_directory: None if target_runtime_pack_directory is None else os.path.abspath(target_runtime_pack_directory))
    return coreclr_args


def get_files_sorted_by_size(src_directory, exclude_directories, exclude_files):
    """ For a given src_directory, returns all the .dll files sorted by size.

    Args:
        src_directory (string): Path of directory to enumerate.
        exclude_directories ([string]): Directory names to exclude.
        exclude_files ([string]): File names to exclude.
    """

    def sorter_by_size(pair):
        """ Sorts the pair (file_name, file_size) tuple in descending order of file_size

        Args:
            pair ([(string, int)]): List of tuple of file_name, file_size
        """
        pair.sort(key=lambda x: x[1], reverse=True)
        return pair

    filename_with_size = []
    exclude_files_lower = [filename.lower() for filename in exclude_files]

    for file_path, dirs, files in os.walk(src_directory, topdown=True):
        # Credit: https://stackoverflow.com/a/19859907
        dirs[:] = [d for d in dirs if d not in exclude_directories]
        for name in files:
            # Make the exclude check case-insensitive
            if name.lower() in exclude_files_lower:
                continue
            curr_file_path = os.path.join(file_path, name)

            if not os.path.isfile(curr_file_path):
                continue
            if not name.endswith(".dll") and not name.endswith(".exe") and not name.endswith(".ilc.rsp") and not name.endswith(".xml") and not name.endswith(".exports"):
                continue

            size = os.path.getsize(curr_file_path)
            filename_with_size.append((curr_file_path, size))

    return sorter_by_size(filename_with_size)


def first_fit(sorted_by_size, max_size):
    """ Given a list of file names along with size in descending order, divides the files
    in number of buckets such that each bucket doesn't exceed max_size (unless a single file exceeds
    max_size, in which case it gets its own bucket). Since this is a first-fit
    approach, it doesn't guarantee to find the bucket with tighest spot available.

    Args:
        sorted_by_size ((string, int)): (file_name, file_size) tuple. file_name can also be a tuple
            of file names that must stay in the same bucket, with file_size their total size.
        max_size (int): Maximum size (in bytes) of each bucket.

    Returns:
        [{int, [string]}]: Returns a dictionary of partition-index to list of file names following in that bucket.
    """
    def file_count(bucket):
        return sum(len(names) if isinstance(names, tuple) else 1 for names, _ in bucket)

    partitions = {}
    for curr_file in sorted_by_size:
        _, file_size = curr_file

        # Find the right bucket
        found_bucket = False

        if file_size < max_size:
            for p_index in partitions:
                total_in_curr_par = sum(n for _, n in partitions[p_index])
                if ((total_in_curr_par + file_size) < max_size) and (file_count(partitions[p_index]) < MAX_FILES_COUNT):
                    partitions[p_index].append(curr_file)
                    found_bucket = True
                    break

        if not found_bucket:
            partitions[len(partitions)] = [curr_file]

    total_size = 0
    for p_index in partitions:
        partition_size = sum(n for _, n in partitions[p_index])
        print("Partition {0}: {1} files with {2} bytes.".format(p_index, file_count(partitions[p_index]), partition_size))
        total_size += partition_size
    print("Total {0} partitions with {1} bytes.".format(str(len(partitions)), total_size))

    return partitions


def partition_files(src_directory, dst_directory, max_size, exclude_directories=[],
                    exclude_files=native_binaries_to_ignore, keep_directories_together=False):
    """ Copy bucketized files based on size to destination folder.

    Args:
        src_directory (string): Source folder containing files to be copied.
        dst_directory (string): Destination folder where files should be copied.
        max_size (int): Maximum partition size in bytes
        exclude_directories ([string]): List of folder names to be excluded.
        exclude_files ([string]): List of files names to be excluded.
        keep_directories_together (bool): Put all the files of a directory in the same partition,
            e.g. so a test ships with the dependencies copied next to it.
    """

    print('Partitioning files from {0} to {1}'.format(src_directory, dst_directory))
    sorted_by_size = get_files_sorted_by_size(src_directory, exclude_directories, exclude_files)
    if keep_directories_together:
        files_by_directory = {}
        for file_name, file_size in sorted_by_size:
            files_by_directory.setdefault(os.path.dirname(file_name), []).append((file_name, file_size))
        sorted_by_size = sorted(((tuple(name for name, _ in files), sum(size for _, size in files))
                                 for files in files_by_directory.values()), key=lambda x: x[1], reverse=True)
    partitions = first_fit(sorted_by_size, max_size)

    index = 0
    for p_index in partitions:
        file_names = []
        for names, _ in partitions[p_index]:
            file_names += list(names) if isinstance(names, tuple) else [names]
        curr_dst_path = os.path.join(dst_directory, str(index), "binaries")
        copy_files(src_directory, curr_dst_path, file_names)
        index += 1


def stage_runtime_pack_assemblies(runtime_pack_rid_directory, dst_directory):
    """ Stage the managed assemblies of a CoreCLR runtime pack into a single flat directory.

    In a CoreCLR runtime pack the shared framework libraries live under `lib/<tfm>` while
    `System.Private.CoreLib.dll` lives under `native`. crossgen2 wants them all in one
    place, both as the set of assemblies to compile and as the reference set.

    Args:
        runtime_pack_rid_directory (string): Path to the runtime pack `runtimes/<rid>` directory.
        dst_directory (string): Directory to stage the assemblies into.

    Returns:
        string: The staging directory, i.e. `dst_directory`.
    """

    lib_directory = os.path.join(runtime_pack_rid_directory, "lib")
    if not os.path.isdir(lib_directory):
        raise RuntimeError("Cannot find runtime pack lib directory " + lib_directory)

    # Require a single target framework rather than guessing which assembly set to use.
    tfm_directories = [os.path.join(lib_directory, name) for name in sorted(os.listdir(lib_directory))
                       if os.path.isdir(os.path.join(lib_directory, name))]
    if len(tfm_directories) == 0:
        raise RuntimeError("Cannot find a target framework directory under " + lib_directory)

    if len(tfm_directories) > 1:
        raise RuntimeError("Expected one target framework directory under {0}; found: {1}".format(
            lib_directory, ", ".join(os.path.basename(path) for path in tfm_directories)))
    tfm_directory = tfm_directories[0]

    corelib_path = os.path.join(runtime_pack_rid_directory, "native", "System.Private.CoreLib.dll")
    if not os.path.isfile(corelib_path):
        raise RuntimeError("Cannot find System.Private.CoreLib.dll at " + corelib_path)

    os.makedirs(dst_directory, exist_ok=True)

    assemblies = [os.path.join(tfm_directory, name) for name in sorted(os.listdir(tfm_directory))
                  if name.endswith(".dll") and os.path.isfile(os.path.join(tfm_directory, name))]
    copy_files(tfm_directory, dst_directory, assemblies)
    copy_files(os.path.dirname(corelib_path), dst_directory, [corelib_path])

    return dst_directory


def stage_core_root_extra_assemblies(core_root_directory, dst_directory, exclude_simple_names):
    """ Stage the managed assemblies at the top level of Core_Root that aren't part of the target's
    shared framework into a flat directory.

    For host collections the `libraries` crossgen2 collection compiles all of Core_Root, which
    besides the shared framework holds a lot of portable IL: Roslyn, TraceEvent, FSharp.Core,
    Newtonsoft.Json, xunit, and the out-of-band libraries. Cross-target collections compile the
    target's runtime pack instead, so stage these extra assemblies to collect over (and to
    resolve references against) as well. Only the top level is considered so assembly simple
    names stay unique; subdirectories hold copies of tools and their dependencies.

    Args:
        core_root_directory (string): Path to the host Core_Root directory.
        dst_directory (string): Directory to stage the assemblies into.
        exclude_simple_names (set): Lowercased simple names to skip, e.g. those already
            provided by the target's runtime pack.

    Returns:
        [string]: The file names that were staged.
    """

    ignored = set(get_assembly_simple_name(name) for name in core_root_assemblies_to_ignore_for_cross_target)
    ignored |= set(get_assembly_simple_name(name) for name in native_binaries_to_ignore)

    assemblies = []
    for name in sorted(os.listdir(core_root_directory)):
        path = os.path.join(core_root_directory, name)
        if not name.endswith(".dll") or name.endswith(".resources.dll") or not os.path.isfile(path):
            continue
        simple_name = get_assembly_simple_name(name)
        if simple_name in exclude_simple_names or simple_name in ignored:
            continue
        if not is_managed_assembly(path):
            continue
        assemblies.append(path)

    os.makedirs(dst_directory, exist_ok=True)
    copy_files(core_root_directory, dst_directory, assemblies)

    return [os.path.basename(path) for path in assemblies]


def stage_coreclr_test_infrastructure_assemblies(tests_directory, dst_directory):
    """ Stage one copy of each coreclr test infrastructure assembly found in the test tree.

    Args:
        tests_directory (string): Root of the coreclr test tree being collected over.
        dst_directory (string): Directory to stage the assemblies into.

    Returns:
        [string]: The file names that were staged.
    """

    remaining = set(name.lower() for name in coreclr_test_infrastructure_assemblies)
    found = []
    for file_path, _, files in os.walk(tests_directory):
        for name in files:
            if name.lower() in remaining:
                remaining.discard(name.lower())
                found.append(os.path.join(file_path, name))
        if len(remaining) == 0:
            break

    os.makedirs(dst_directory, exist_ok=True)
    for path in found:
        copy_files(os.path.dirname(path), dst_directory, [path])

    return [os.path.basename(path) for path in found]


def setup_benchmark(workitem_directory, arch):
    """ Perform setup of microbenchmarks/realworld

    Args:
        workitem_directory (string): Path to work
        arch (string): Architecture for which dotnet will be installed
    """
    performance_directory = os.path.join(workitem_directory, "performance")

    run_command(
        ["git", "clone", "--quiet", "--depth", "1", "https://github.com/dotnet/performance", performance_directory])

    try:
        shutil.rmtree(os.path.join(performance_directory, ".git"))
    except Exception as ex:
        print("Warning: failed to remove directory \"%s\": %s", os.path.join(performance_directory, ".git"), ex)

def get_python_name():
    """Gets the python name

    Returns:
        [string]: Returns the appropriate python name depending on the OS.
    """
    if is_windows:
        return ["py", "-3"]
    else:
        return ["python3"]


def main(main_args):
    """ Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """
    coreclr_args = setup_args(main_args)
    source_directory = coreclr_args.source_directory

    # A cross-target collection runs the tools on the host but compiles for a different
    # OS/architecture (e.g. crossgen2 on windows/x64 producing browser/wasm code).
    is_cross_target_collection = (coreclr_args.target_os != coreclr_args.platform) or \
                                 (coreclr_args.target_arch != coreclr_args.arch)

    # If the payload directory doesn't already exist (it probably shouldn't) then create it.
    if not os.path.isdir(coreclr_args.payload_directory):
        os.makedirs(coreclr_args.payload_directory)

    correlation_payload_directory = os.path.join(coreclr_args.payload_directory, 'correlation')
    workitem_payload_directory = os.path.join(coreclr_args.payload_directory, 'workitem')

    superpmi_src_directory = os.path.join(source_directory, 'src', 'coreclr', 'scripts')

    tests_directory = os.path.join(source_directory, 'src', 'tests')

    # Correlation payload directories (sent to every Helix machine).
    # Currently, all the Core_Root files, superpmi script files, and pmi.dll go in the same place.
    superpmi_dst_directory = os.path.join(correlation_payload_directory, "superpmi")
    core_root_dst_directory = superpmi_dst_directory

    # Workitem directories
    # input_artifacts is only used for pmi/crossgen2/nativeaot collections.
    input_artifacts = ""

    # Name of the folder in the correlation payload holding the crossgen2 reference
    # assemblies; only set for cross-target crossgen2 collections.
    crossgen2_reference_dir_name = ""

    arch = coreclr_args.arch
    platform_name = coreclr_args.platform.lower()
    helix_source_prefix = "official"

    # Determine the Helix queue name to use when running jobs.
    if coreclr_args.public_queues:
        if platform_name == "windows":
            if arch == "arm64": # public windows_arm64
                helix_queue = "Windows.11.Arm64.Open"
            else: # public windows_x64
                helix_queue = "Windows.10.Amd64.Open"
        elif platform_name == "linux":
            if arch == "arm": # public linux_arm
                helix_queue = "(Debian.12.Arm32.Open)Ubuntu.2204.ArmArch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7"
            elif arch == "arm64": # public linux_arm64
                helix_queue = "(Ubuntu.2404.Arm64.Open)Ubuntu.2204.Armarch.Open@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-24.04-helix-arm64v8"
            else: # public linux_x64
                helix_queue = "azurelinux.3.amd64.open"
        elif platform_name == "osx":
            if arch == "arm64": # public osx_arm64
                helix_queue = "OSX.26.Arm64.Open"
            else: # public osx_x64
                helix_queue = "OSX.15.Amd64.Open"
    else:
        if platform_name == "windows":
            if arch == "arm64": # internal windows_arm64
                helix_queue = "Windows.11.Arm64"
            else: # internal superpmi windows_x64
                helix_queue = "Windows.10.Amd64.X86.Rt"
        elif platform_name == "linux":
            if arch == "arm": # internal linux_arm
                helix_queue = "(Debian.12.Arm32)Ubuntu.2204.ArmArch@mcr.microsoft.com/dotnet-buildtools/prereqs:debian-12-helix-arm32v7"
            elif arch == "arm64": # internal linux_arm64
                helix_queue = "(Ubuntu.2404.Arm64)Ubuntu.2204.ArmArch@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-24.04-helix-arm64v8"
            else: # internal linux_x64
                helix_queue = "azurelinux.3.amd64"
        elif platform_name == "osx":
            if arch == "arm64": # internal osx_arm64
                helix_queue = "OSX.26.Arm64"
            else: # internal osx_x64
                helix_queue = "OSX.15.Amd64"

    # Copy the superpmi scripts

    print('Copying {} -> {}'.format(superpmi_src_directory, superpmi_dst_directory))
    copy_directory(superpmi_src_directory, superpmi_dst_directory, verbose_output=True, match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    # Copy Core_Root

    if platform_name == "windows":
        acceptable_copy = lambda path: any(path.endswith(extension) for extension in [".py", ".dll", ".exe", ".json", ".txt", ".xml", ".exports"])
    else:
        acceptable_extensions = [".py", ".dll", ".json", ".txt", ".xml", ".exports"]
        acceptable_extensions.append(".so" if platform_name == "linux" else ".dylib")
        if platform_name == "linux":
            acceptable_extensions.append(".so.1")
        # Need to accept files without any extension, which is how executable file's names look.
        acceptable_copy = lambda path: (os.path.basename(path).find(".") == -1) or any(path.endswith(extension) for extension in acceptable_extensions)

    if coreclr_args.collection_name == "benchmarks" or coreclr_args.collection_name == "realworld" or coreclr_args.collection_name == "aspnet2":
      # create a directory with release runtime bits and a checked jit
      print('Copying {} -> {}'.format(coreclr_args.release_core_root_directory, core_root_dst_directory))
      copy_directory(coreclr_args.release_core_root_directory, core_root_dst_directory, verbose_output=True, match_func=acceptable_copy)
      jitname = determine_jit_name(coreclr_args.platform, coreclr_args.platform, coreclr_args.arch, coreclr_args.arch)
      print('Copying checked {} -> {}'.format(jitname, core_root_dst_directory))
      copy_files(coreclr_args.core_root_directory, core_root_dst_directory, [os.path.join(coreclr_args.core_root_directory, jitname)])
    elif coreclr_args.collection_type == "crossgen2" and is_cross_target_collection:
      # For a cross-target crossgen2 collection (e.g. browser/wasm), use the release
      # Core_Root for the host (crossgen2 itself) and overlay the checked cross-targeting
      # JIT so that SPMI collections include JIT asserts. The assemblies crossgen2
      # consumes are the release target-built ones staged below.
      print('Copying {} -> {}'.format(coreclr_args.release_core_root_directory, core_root_dst_directory))
      copy_directory(coreclr_args.release_core_root_directory, core_root_dst_directory, verbose_output=True, match_func=acceptable_copy)
      jitname = determine_jit_name(coreclr_args.platform, coreclr_args.target_os, coreclr_args.arch, coreclr_args.target_arch)
      print('Copying checked {} -> {}'.format(jitname, core_root_dst_directory))
      copy_files(coreclr_args.core_root_directory, core_root_dst_directory, [os.path.join(coreclr_args.core_root_directory, jitname)])
    else:
      print('Copying {} -> {}'.format(coreclr_args.core_root_directory, core_root_dst_directory))
      copy_directory(coreclr_args.core_root_directory, core_root_dst_directory, verbose_output=True, match_func=acceptable_copy)

    if coreclr_args.collection_name == "benchmarks" or coreclr_args.collection_name == "realworld":
        # Setup benchmarks
        setup_benchmark(workitem_payload_directory, arch)
    elif coreclr_args.collection_name == "aspnet2" or coreclr_args.collection_name == "aspire":
        # Nothing to prepare for aspnet2/aspire, their scripts are fully self-contained.
        # Just make sure workitem_payload_directory folder exists:
        os.makedirs(workitem_payload_directory, exist_ok=True)
    else:
        # Setup for pmi/crossgen2/nativeaot runs

        # For libraries tests, copy all the test files to the single 
        # The reason is there are lot of dependencies with *.Tests.dll and to ensure we do not get
        # Reflection errors, just copy everything to CORE_ROOT so for all individual partitions, the
        # references will be present in CORE_ROOT.
        if coreclr_args.collection_name == "libraries_tests":

            def make_readable(folder_name):
                """Make file executable by changing the permission

                Args:
                    folder_name (string): folder to mark with 744
                """
                if is_windows:
                    return

                print("Inside make_readable")
                run_command(["ls", "-l", folder_name])
                for file_path, dirs, files in os.walk(folder_name, topdown=True):
                    for d in dirs:
                        os.chmod(os.path.join(file_path, d),
                        # read+write+execute for owner
                        (stat.S_IRUSR | stat.S_IWUSR | stat.S_IXUSR) |
                        # read for group
                        (stat.S_IRGRP) |
                        # read for other
                        (stat.S_IROTH))

                    for f in files:
                        os.chmod(os.path.join(file_path, f),
                        # read+write+execute for owner
                        (stat.S_IRUSR | stat.S_IWUSR | stat.S_IXUSR) |
                        # read for group
                        (stat.S_IRGRP) |
                        # read for other
                        (stat.S_IROTH))
                run_command(["ls", "-l", folder_name])

            make_readable(coreclr_args.input_directory)
            print('Copying {} -> {}'.format(coreclr_args.input_directory, core_root_dst_directory))
            copy_directory(coreclr_args.input_directory, core_root_dst_directory, verbose_output=True, match_func=acceptable_copy)

        # We need the PMI tool if we're doing a PMI collection. We could download a cached copy from Azure DevOps JIT blob
        # storage, but instead we clone and build jitutils to build pmi.dll.
        if coreclr_args.collection_type == "pmi":
            try:
                with TempDir() as jitutils_directory:
                    run_command(
                        ["git", "clone", "--quiet", "--depth", "1", "https://github.com/dotnet/jitutils", jitutils_directory])

                    # Make sure ".dotnet" directory exists, by running the script at least once
                    dotnet_script_name = "dotnet.cmd" if is_windows else "dotnet.sh"
                    dotnet_script_path = os.path.join(source_directory, dotnet_script_name)
                    run_command([dotnet_script_path, "--info"], jitutils_directory)

                    # Set dotnet path to run build
                    os.environ["PATH"] = os.path.join(source_directory, ".dotnet") + os.pathsep + os.environ["PATH"]
                    build_file = "build.cmd" if is_windows else "build.sh"
                    run_command([os.path.join(jitutils_directory, build_file), "-p"], jitutils_directory)

                    copy_files(os.path.join(jitutils_directory, "bin"), core_root_dst_directory, [os.path.join(jitutils_directory, "bin", "pmi.dll")])
            except PermissionError as pe_error:
                # Details: https://bugs.python.org/issue26660
                print('Ignoring PermissionError: {0}'.format(pe_error))

        # Build nativeaot tests
        if coreclr_args.collection_type == "nativeaot":
            build_file = "build.cmd" if is_windows else "build.sh"
            tests_build_file = "build.cmd" if is_windows else "build.sh"
            run_command([os.path.join(source_directory, build_file), "Tools.ILLink", "-arch", arch, "-c", coreclr_args.build_type])
            run_command([os.path.join(tests_directory, tests_build_file), "nativeaot", arch, coreclr_args.build_type, "tree", "nativeaot/SmokeTests"], source_directory)

        # NOTE: we can't use the build machine ".dotnet" to run on all platforms. E.g., the Windows x86 build uses a
        # Windows x64 .dotnet\dotnet.exe that can't load a 32-bit shim. Thus, we always use corerun from Core_Root to invoke crossgen2.
        # The following will copy .dotnet to the correlation payload in case we change our mind, and need or want to use it for some scenarios.

        # # Copy ".dotnet" to correlation_payload_directory for crossgen2 job; it is needed to invoke crossgen2.dll
        # if coreclr_args.collection_type == "crossgen2":
        #     dotnet_src_directory = os.path.join(source_directory, ".dotnet")
        #     dotnet_dst_directory = os.path.join(correlation_payload_directory, ".dotnet")
        #     print('Copying {} -> {}'.format(dotnet_src_directory, dotnet_dst_directory))
        #     copy_directory(dotnet_src_directory, dotnet_dst_directory, verbose_output=False)

        input_artifacts = os.path.join(workitem_payload_directory, "collectAssembliesDirectory", coreclr_args.collection_name)

        exclude_directories = list(directories_to_ignore)
        if coreclr_args.collection_name == "coreclr_tests":
            exclude_directories += ['Core_Root']

        exclude_files = list(native_binaries_to_ignore)
        keep_directories_together = False
        if coreclr_args.collection_type == "crossgen2":
            print('Adding exclusions for crossgen2')
            # Currently, trying to crossgen2 R2RTest\Microsoft.Build.dll causes a pop-up failure, so exclude it.
            exclude_files += ["Microsoft.Build.dll"]

        if coreclr_args.collection_name == "libraries_tests":
            # libraries_tests artifacts contains files from core_root folder. Exclude them.
            core_root_dir = coreclr_args.core_root_directory
            exclude_files += [item for item in os.listdir(core_root_dir)
                              if os.path.isfile(os.path.join(core_root_dir, item)) and (item.endswith(".dll") or item.endswith(".exe"))]

        if coreclr_args.collection_name == "smoke_tests":
            if coreclr_args.collection_type != "nativeaot":
                raise RuntimeError("Collection 'smoke_tests' is only available for 'nativeaot' collections.")

        if coreclr_args.collection_type == "crossgen2" and is_cross_target_collection:
            # A cross-target crossgen2 collection compiles assemblies built for the target,
            # not the host Core_Root, and resolves references against them. Stage the target's
            # assembly set into a single flat directory: the managed assemblies of its CoreCLR
            # runtime pack (lib/<tfm>/*.dll plus native/System.Private.CoreLib.dll), plus the
            # portable non-framework IL that Core_Root carries (Roslyn, TraceEvent, xunit, the
            # out-of-band libraries, ...), which host `libraries` collections compile as well.
            target_assemblies_directory = os.path.join(workitem_payload_directory, "target_assemblies")
            print('Staging target assemblies from {} -> {}'.format(coreclr_args.target_runtime_pack_directory, target_assemblies_directory))
            stage_runtime_pack_assemblies(coreclr_args.target_runtime_pack_directory, target_assemblies_directory)
            framework_simple_names = set(get_assembly_simple_name(name) for name in os.listdir(target_assemblies_directory))
            print('Staging non-framework Core_Root assemblies from {} -> {}'.format(coreclr_args.release_core_root_directory, target_assemblies_directory))
            stage_core_root_extra_assemblies(coreclr_args.release_core_root_directory, target_assemblies_directory, framework_simple_names)

            # crossgen2 has to resolve references against the target's assemblies rather than
            # the host Core_Root, so stage a full copy into the correlation payload, which every
            # Helix work item sees. superpmi-collect.proj turns `Crossgen2ReferenceDirName`
            # into superpmi.py's -crossgen2_reference_directory.
            crossgen2_reference_dir_name = "crossgen2References"
            crossgen2_reference_directory = os.path.join(core_root_dst_directory, crossgen2_reference_dir_name)
            print('Copying {} -> {}'.format(target_assemblies_directory, crossgen2_reference_directory))
            copy_directory(target_assemblies_directory, crossgen2_reference_directory, verbose_output=False,
                           match_func=lambda path: path.endswith(".dll"))

            if coreclr_args.collection_name == "libraries":
                # Collect over the whole staged target assembly set.
                coreclr_args.input_directory = target_assemblies_directory
            elif coreclr_args.collection_name == "coreclr_tests":
                # Collect over the test assemblies under input_directory. Every test directory
                # carries its own copy of the test infrastructure; stage one copy of it as a
                # reference, and don't compile copies of anything that is already a reference
                # (the `libraries` collection covers those).
                print('Staging coreclr test infrastructure assemblies from {} -> {}'.format(coreclr_args.input_directory, crossgen2_reference_directory))
                stage_coreclr_test_infrastructure_assemblies(coreclr_args.input_directory, crossgen2_reference_directory)
                exclude_files += [name for name in os.listdir(crossgen2_reference_directory) if name.endswith(".dll")]
                # crossgen2 resolves a test's other dependencies from its own directory, so don't
                # split directories across work items.
                keep_directories_together = True
            else:
                raise RuntimeError("Cross-target crossgen2 collections support only the 'libraries' and 'coreclr_tests' collections, not '{}'".format(coreclr_args.collection_name))

        partition_files(coreclr_args.input_directory, input_artifacts, coreclr_args.max_size, exclude_directories,
                        exclude_files, keep_directories_together=keep_directories_together)

    # Set variables
    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("WorkItemDirectory", workitem_payload_directory)
    set_pipeline_variable("InputArtifacts", input_artifacts)
    set_pipeline_variable("Python", ' '.join(get_python_name()))
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Queue", helix_queue)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)
    set_pipeline_variable("MchFileTag", coreclr_args.mch_file_tag)
    set_pipeline_variable("TargetOS", coreclr_args.target_os)
    set_pipeline_variable("TargetArchitecture", coreclr_args.target_arch)
    set_pipeline_variable("Crossgen2ReferenceDirName", crossgen2_reference_dir_name)


################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
