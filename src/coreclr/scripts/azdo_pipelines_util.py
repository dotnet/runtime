#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : azdo_pipelines_util.py
#
# Notes:
#
# Utility functions used by Python scripts involved with Azure DevOps Pipelines
# setup.
#
################################################################################
################################################################################

import os
import shutil
import subprocess
import sys
import tempfile


def run_command(command_to_run, _cwd=None, _exit_on_fail=False, _output_file=None):
    """ Runs the command.

    Args:
        command_to_run ([string]): Command to run along with arguments.
        _cwd (string): Current working directory.
        _exit_on_fail (bool): If it should exit on failure.
    Returns:
        (string, string, int): Returns a tuple of stdout, stderr, and command return code if _output_file= None
        Otherwise stdout, stderr are empty.
    """
    print("Running: " + " ".join(command_to_run))
    command_stdout = ""
    command_stderr = ""
    return_code = 1

    output_type = subprocess.STDOUT if _output_file else subprocess.PIPE
    with subprocess.Popen(command_to_run, stdout=subprocess.PIPE, stderr=output_type, cwd=_cwd) as proc:

        # For long running command, continuously print the output
        if _output_file:
            while True:
                with open(_output_file, 'a') as of:
                    output = proc.stdout.readline()
                    if proc.poll() is not None:
                        break
                    if output:
                        output_str = output.strip().decode("utf-8")
                        print(output_str)
                        of.write(output_str + "\n")
        else:
            command_stdout, command_stderr = proc.communicate()
            if len(command_stdout) > 0:
                print(command_stdout.decode("utf-8"))
            if len(command_stderr) > 0:
                print(command_stderr.decode("utf-8"))

        return_code = proc.returncode
        if _exit_on_fail and return_code != 0:
            print("Command failed. Exiting.")
            sys.exit(1)
    return command_stdout, command_stderr, return_code


def copy_directory(src_path, dst_path, verbose_output=True, match_func=lambda path: True):
    """Copies directory in 'src_path' to 'dst_path' maintaining the directory
    structure. https://docs.python.org/3.5/library/shutil.html#shutil.copytree can't
    be used in this case because it expects the destination directory should not
    exist, however we do call copy_directory() to copy files to same destination directory.

    Args:
        src_path (string): Path of source directory that need to be copied.
        dst_path (string): Path where directory should be copied.
        verbose_output (bool): True to print every copy or skipped file.
        match_func (str -> bool) : Criteria function determining if a file is copied.
    """
    if not os.path.exists(dst_path):
        os.makedirs(dst_path)
    for item in os.listdir(src_path):
        src_item = os.path.join(src_path, item)
        dst_item = os.path.join(dst_path, item)
        if os.path.isdir(src_item):
            copy_directory(src_item, dst_item, verbose_output, match_func)
        else:
            try:
                if match_func(src_item):
                    if verbose_output:
                        print("> copy {0} => {1}".format(src_item, dst_item))
                    try:
                        shutil.copy2(src_item, dst_item)
                    except PermissionError as pe_error:
                        print('Ignoring PermissionError: {0}'.format(pe_error))
                else:
                    if verbose_output:
                        print("> skipping {0}".format(src_item))
            except UnicodeEncodeError:
                if verbose_output:
                    print("> Got UnicodeEncodeError")


def copy_files(src_path, dst_path, file_names):
    """Copy files from 'file_names' list from 'src_path' to 'dst_path'.
    It retains the original directory structure of src_path.

    Args:
        src_path (string): Source directory from where files are copied.
        dst_path (string): Destination directory where files to be copied.
        file_names ([string]): List of full path file names to be copied.
    """

    print('### Copying below files from {0} to {1}:'.format(src_path, dst_path))
    print('')
    print(os.linesep.join(file_names))
    for f in file_names:
        # Create same structure in dst so we don't clobber same files names present in different directories
        dst_path_of_file = f.replace(src_path, dst_path)

        dst_directory = os.path.dirname(dst_path_of_file)
        if not os.path.exists(dst_directory):
            os.makedirs(dst_directory)
        try:
            shutil.copy2(f, dst_path_of_file)
        except PermissionError as pe_error:
            print('Ignoring PermissionError: {0}'.format(pe_error))


def set_pipeline_variable(name, value):
    """ This method sets pipeline variable.

    Args:
        name (string): Name of the variable.
        value (string): Value of the variable.
    """
    define_variable_format = "##vso[task.setvariable variable={0}]{1}"
    print("{0} -> {1}".format(name, value))  # logging
    print(define_variable_format.format(name, value))  # set variable


class TempDir:
    """ Class to create a temporary working directory, or use one that is passed as an argument.

        Use with: "with TempDir() as temp_dir" to change to that directory and then automatically
        change back to the original working directory afterwards and remove the temporary
        directory and its contents (if skip_cleanup is False).
    """

    def __init__(self, path=None, skip_cleanup=False):
        self.mydir = tempfile.mkdtemp() if path is None else path
        self.cwd = None
        self._skip_cleanup = skip_cleanup

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.mydir)
        return self.mydir

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)
        if not self._skip_cleanup:
            shutil.rmtree(self.mydir)


class ChangeDir:
    """ Class to temporarily change to a given directory. Use with "with".
    """

    def __init__(self, mydir):
        self.mydir = mydir
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.mydir)

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)