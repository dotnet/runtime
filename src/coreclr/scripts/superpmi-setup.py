#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               : superpmi-setup.py
#
# Notes:
#
# Script to setup directory structure to perform SuperPMI collection in CI.
#
################################################################################
################################################################################


import subprocess
import argparse

from os import listdir, path, walk
from os.path import isfile, join, getsize
from coreclr_arguments import *

# Start of parser object creation.

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-source_directory", help="path to source directory")
parser.add_argument("-core_root_directory", help="path to core_root directory")
# parser.add_argument("-managed_test_directory", help="path to managed test artifacts directory")
parser.add_argument("-arch", help="Architecture")
parser.add_argument("-mch_file_tag", help="Tag to be used to mch files")

parser.add_argument("-assemblies_directory", help="directory containing assemblies for which superpmi collection to "
                                                  "be done")
parser.add_argument("-max_size", help="Max size of partition in MB")
is_windows = platform.system() == "Windows"


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
                        "source_directory",
                        lambda source_directory: os.path.isdir(source_directory),
                        "source_directory doesn't exist")

    coreclr_args.verify(args,
                        "core_root_directory",
                        lambda core_root_directory: os.path.isdir(core_root_directory),
                        "core_root_directory doesn't exist")

    coreclr_args.verify(args,
                        "arch",
                        lambda unused: True,
                        "Unable to set arch")

    coreclr_args.verify(args,
                        "mch_file_tag",
                        lambda unused: True,
                        "Unable to set mch_file_tag")

    coreclr_args.verify(args,
                        "assemblies_directory",
                        lambda assemblies_directory: os.path.isdir(assemblies_directory),
                        "assemblies_directory doesn't exist")

    coreclr_args.verify(args,
                        "max_size",
                        lambda max_size: max_size > 0,
                        "Please enter valid positive numeric max_size",
                        modify_arg=lambda max_size: int(
                            max_size) * 1000 * 1000 if max_size is not None and max_size.isnumeric() else 0
                        # Convert to MB
                        )
    return coreclr_args


def get_files_sorted_by_size(src_directory, exclude_directories):
    """ For a given src_directory, returns all the .dll files sorted by size.

    Args:
        src_directory (string): Path of directory to enumerate.
        exclude_directories (string): Directory names to exclude.
    """

    def sorter_by_size(pair):
        """ Sorts the pair (file_name, file_size) tuple in descending order of file_size

        Args:
            pair ([(string, int)]): List of tuple of file_name, file_size
        """
        pair.sort(key=lambda x: x[1], reverse=True)
        return pair

    filename_with_size = []

    for file_path, dirs, files in walk(src_directory, topdown=True):
        # Credit: https://stackoverflow.com/a/19859907
        dirs[:] = [d for d in dirs if d not in exclude_directories]
        for name in files:
            curr_file_path = path.join(file_path, name)
            if not isfile(curr_file_path) or not name.endswith(".dll"):
                continue
            size = getsize(curr_file_path)
            filename_with_size.append((curr_file_path, size))

    return sorter_by_size(filename_with_size)


def first_fit(sorted_by_size, max_size):
    """ Given a list of file names along with size in descending order, divides the files
    in number of buckets such that each bucket doesn't exceed max_size. Since this is a first-fit
    approach, it doesn't guarantee to find the bucket with tighest spot available.

    Args:
        sorted_by_size ((string, int)): (file_name, file_size) tuple
        max_size (int): Maximum size (in bytes) of each bucket.

    Returns:
        [{int, [string]}]: Returns a dictionary of partition-index to list of file names following in that bucket.
    """
    end_result = {}
    for curr_file in sorted_by_size:
        _, file_size = curr_file

        # Find the right bucket
        found_bucket = False

        if file_size < max_size:
            for p_index in end_result:
                total_in_curr_par = sum(n for _, n in end_result[p_index])
                if (total_in_curr_par + file_size) < max_size:
                    end_result[p_index].append(curr_file)
                    found_bucket = True
                    break

            if not found_bucket:
                end_result[len(end_result) - 1] = [curr_file]

    return end_result


def run_command(command_to_run, _cwd=None):
    """ Runs the command.

    Args:
        command_to_run ([string]): Command to run along with arguments.
        _cmd (string): Current working directory
    """
    print("Running: " + " ".join(command_to_run))
    with subprocess.Popen(command_to_run, stdout=subprocess.PIPE, stderr=subprocess.PIPE, cwd=_cwd) as proc:
        stdout, stderr = proc.communicate()
        if len(stdout) > 0:
            print(stdout.decode("utf-8"))
        if len(stderr) > 0:
            print(stderr.decode("utf-8"))


def copy_files(src_path, dst_path, file_names):
    if is_windows:
        copy_files_windows(src_path, dst_path, file_names)
    else:
        copy_files_linux(src_path, dst_path, file_names)


def copy_files_windows(src_path, dst_path, file_paths):
    """ On Windows, copies files specified in file_names from src_path to dst_path using "robocopy".

    Args:
        src_path (string): Path of source directory
        dst_path (string): Path of destination directory
        file_paths ([string]): List of files paths to be copied
    """

    # Extract just the unique files names
    file_names = list(set([path.basename(curr_file) for curr_file in file_paths]))
    command = ["robocopy", src_path, dst_path] + file_names
    command += [
        # "*.dll",  # only copy .dll
        # "*.exe",  # or .exe
        # "*.py", #TODO : Have this option only if file_names is empty
        "/S",  # copy from sub-directories
        "/R:2",  # no. of retries
        "/W:5",  # seconds before retry
        "/NS",  # don't log file sizes
        "/NC",  # don't log file classes
        "/NFL",  # don't log file names
        "/NDL",  # don't log directory names
        "/NJH",  # No Job Header.
        "/XF",   # Exclude
        "*.pdb"  #  *.pdb files
    ]
    run_command(command)


def copy_files_linux(src_path, dst_path, file_paths):
    """ On Linux, copies files specified in file_names from src_path to dst_path using "rsync".

    Args:
        src_path (string): Path of source directory
        dst_path (string): Path of destination directory
        file_paths ([string]): List of files paths to be copied
    """

    # Extract just the unique relative path of files names
    file_names = list(set([curr_file.replace(src_path + '/', '') for curr_file in file_paths]))

    # create dst_path
    run_command(["mkdir", "-p", dst_path])

    if len(file_names) == 0:
        # if file_names is empty, copy everything
        command = ["rsync", "-avr", "--include='*.dll'", "--include='*.exe'", "--exclude='*'", src_path + '/', dst_path]
        run_command(command)
    else:
        with tempfile.NamedTemporaryFile(mode="w+t", delete=False) as tmp:
            # create temp file containing name of files to copy
            to_write = os.linesep.join(file_names)
            tmp.write(to_write)
            tmp.flush()

            # use rsync
            command = ["rsync", "-avr", "--files-from={0}".format(tmp.name), src_path, dst_path]
            run_command(command)


def partition_files(coreclr_args, dst_directory, exclude_directories):
    """ Copy bucketized files based on size to destination folder.

    Args:
        coreclr_args (CoreclrArguments): Command line arguments.
        dst_directory (string): Destination folder where files are copied.
        exclude_directories ([string]): List of folder names to be excluded
    """
    src_directory = coreclr_args.assemblies_directory
    max_size = coreclr_args.max_size

    sorted_by_size = get_files_sorted_by_size(src_directory, exclude_directories)
    partitions = first_fit(sorted_by_size, max_size)

    index = 0
    for p_index in partitions:
        file_names = [curr_file[0] for curr_file in partitions[p_index]]
        curr_dst_path = path.join(dst_directory, str(index), "binaries")
        copy_files(src_directory, curr_dst_path, file_names)
        index += 1

    total_partitions = str(len(partitions))
    print("Total partitions: %s" % total_partitions)
    set_pipeline_variable("SuperPmiJobCount", total_partitions)


def set_pipeline_variable(name, value):
    """ This method sets pipeline variable.

    Args:
        name (string): Name of the variable.
        value (string): Value of the variable.
    """
    define_variable_format = "##vso[task.setvariable variable={0}]{1}"
    print("{0} -> {1}".format(name, value)) # logging
    print(define_variable_format.format(name, value)) # set variable


def main(args):
    """ Main entrypoint

    Args:
        args ([type]): Arguments to the script
    """
    coreclr_args = setup_args(args)
    source_directory = coreclr_args.source_directory

    # CorelationPayload directories
    correlation_payload_directory = path.join(coreclr_args.source_directory, "payload")
    superpmi_src_directory = path.join(source_directory, 'src', 'coreclr', 'scripts')
    superpmi_dst_directory = path.join(correlation_payload_directory, "superpmi")
    arch = coreclr_args.arch
    helix_source_prefix = "official"
    creator = ""
    ci = True
    if is_windows:
        helix_queue = "Windows.10.Arm64" if arch == "arm64" else "Windows.10.Amd64"
    else:
        if arch == "arm":
            helix_queue = "(Ubuntu.1804.Arm32)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-helix-arm32v7-bfcd90a-20200121150440"
        elif arch == "arm64":
            helix_queue = "(Ubuntu.1804.Arm64)Ubuntu.1804.ArmArch@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-helix-arm64v8-a45aeeb-20190620155855"
        else:
            helix_queue = "Ubuntu.1804.Amd64"

    # create superpmi directory
    copy_files(superpmi_src_directory, superpmi_dst_directory, [])
    copy_files(coreclr_args.core_root_directory, superpmi_dst_directory, [])

    # Clone and build jitutils
    try:
        with tempfile.TemporaryDirectory() as jitutils_directory:
            run_command(
                ["git", "clone", "--quiet", "--depth", "1", "https://github.com/dotnet/jitutils", jitutils_directory])
            # Set dotnet path to run bootstrap
            os.environ["PATH"] = path.join(source_directory, ".dotnet") + os.pathsep + os.environ["PATH"]
            if is_windows:
                run_command([path.join(jitutils_directory, "bootstrap.cmd")], jitutils_directory)
            else:
                run_command([path.join(jitutils_directory, "bootstrap.sh")], jitutils_directory)

            copy_files(path.join(jitutils_directory, "bin"), superpmi_dst_directory, ["pmi.dll"])
    except PermissionError as pe_error:
        # Details: https://bugs.python.org/issue26660
        print('Ignoring PermissionError: {0}'.format(pe_error))

    # Workitem directories
    workitem_directory = path.join(source_directory, "workitem")
    pmiassemblies_directory = path.join(workitem_directory, "pmiAssembliesDirectory")

    # libraries
    libraries_artifacts = path.join(pmiassemblies_directory, "Core_Root")
    partition_files(coreclr_args, libraries_artifacts, [])

    # test
    # likewise for test

    # Set variables
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("WorkItemDirectory", workitem_directory)
    set_pipeline_variable("LibrariesArtifacts", libraries_artifacts)
    # set_pipeline_variable("TestsArtifacts", libraries_artifacts)
    if is_windows:
        set_pipeline_variable("Python", "py -3")
    else:
        set_pipeline_variable("Python", "python3")
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("Queue", helix_queue)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)
    set_pipeline_variable("MchFileTag", coreclr_args.mch_file_tag)


################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
