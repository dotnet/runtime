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

parser.add_argument("-src_directory", help="path to src")
parser.add_argument("-dst_directory", help="path to dst")
parser.add_argument("-dst_folder_name", dest='dst_folder_name', default="binaries", help="Folder under dst/N/")
parser.add_argument("-exclude_directories", dest='exclude_directories', default='', help="semi-colon separated list "
                                                                                         "of directories to exclude")
parser.add_argument("-max_size", help="Max size of partition in MB")


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
                        "src_directory",
                        lambda src_directory: os.path.isdir(src_directory),
                        "src_directory doesn't exist")

    coreclr_args.verify(args,
                        "dst_directory",
                        lambda dst_directory: (not os.path.isdir(dst_directory)),
                        "dst_directory already exist")

    coreclr_args.verify(args,
                        "dst_folder_name",
                        lambda unused: True,
                        "Unable to set dst_folder_name")

    coreclr_args.verify(args,
                        "exclude_directories",
                        lambda unused: True,
                        "Unable to set exclude_directories",
                        modify_arg=lambda exclude_directories: exclude_directories.split(';'))

    coreclr_args.verify(args,
                        "max_size",
                        lambda max_size: max_size > 0,
                        "Please enter valid positive numeric max_size",
                        modify_arg=lambda max_size: int(max_size) * 1000 * 1000 if max_size.isnumeric() else 0
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


def run_command(command_to_run):
    """ Runs the command.

    Args:
        command_to_run ([string]): Command to run along with arguments.
    """
    print("Running: " + " ".join(command_to_run))
    with subprocess.Popen(command_to_run, stdout=subprocess.PIPE, stderr=subprocess.PIPE) as proc:
        stdout, _ = proc.communicate()
        print(stdout.decode('utf-8'))


def copy_files_windows(src_path, dst_path, file_names):
    """ On Windows, copies files specified in file_names from src_path to dst_path using "robocopy".

    Args:
        src_path (string): Path of source directory
        dst_path (string): Path of destination directory
        file_names ([string]): List of files names to be copied
    """
    command = ["robocopy", src_path, dst_path] + file_names
    command += [
        "/S",  # copy from sub-directories
        "/R:2",  # no. of retries
        "/W:5",  # seconds before retry
        "/NS",  # don't log file sizes
        "/NC",  # don't log file classes
        "/NFL",  # don't log file names
        "/NDL",  # don't log directory names
        "/NJH"  # No Job Header.
    ]
    run_command(command)


def copy_files_linux(src_path, dst_path, file_names):
    """ On Linux, copies files specified in file_names from src_path to dst_path using "rsync".

    Args:
        src_path (string): Path of source directory
        dst_path (string): Path of destination directory
        file_names ([string]): List of files names to be copied
    """

    # create dst_path
    run_command(["mkdir", "-p", dst_path])

    with tempfile.NamedTemporaryFile(mode='w+t') as tmp:
        # create temp file containing name of files to copy
        to_write = os.linesep.join(file_names)
        tmp.write(to_write)
        tmp.flush()

        # use rsync
        command = ["rsync", "-avr", "--files-from={0}".format(tmp.name), src_path, dst_path]
        run_command(command)


def partition_files(coreclr_args):
    """ Copy bucketized files based on size to destination folder.

    Args:
        coreclr_args (CoreclrArguments): Command line arguments.
    """
    src_directory = coreclr_args.src_directory
    exclude_directories = coreclr_args.exclude_directories
    dst_directory = coreclr_args.dst_directory
    dst_folder_name = coreclr_args.dst_folder_name
    max_size = coreclr_args.max_size

    sorted_by_size = get_files_sorted_by_size(src_directory, exclude_directories)
    partitions = first_fit(sorted_by_size, max_size)

    index = 0
    for p_index in partitions:
        file_names = list(set([path.basename(curr_file[0]) for curr_file in partitions[p_index]]))
        curr_dst_path = path.join(dst_directory, str(index), dst_folder_name)
        if platform.system() == "Windows":
            copy_files_windows(src_directory, curr_dst_path, file_names)
        else:
            copy_files_linux(src_directory, curr_dst_path, file_names)
        index += 1

    total_partitions = str(len(partitions))
    print('Total partitions: %s' % total_partitions)
    set_pipeline_variable("SuperPmiJobCount", total_partitions)


def set_pipeline_variable(name, value):
    """ This method sets pipeline variable.

    Args:
        name (string): Name of the variable.
        value (string): Value of the variable.
    """
    define_variable_format = "##vso[task.setvariable variable={0}]{1}"
    print(define_variable_format.format(name, value))


def main(args):
    """ Main entrypoint

    Args:
        args ([type]): Arguments to the script
    """
    coreclr_args = setup_args(args)
    partition_files(coreclr_args)


################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
