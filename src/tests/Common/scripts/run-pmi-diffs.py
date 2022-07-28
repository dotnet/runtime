#!/usr/bin/env python
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               :run-pmi-diffs.py
#
# Notes:
#
# TODO: Instead of downloading and extracting the dotnet CLI, can we convert
# to using init-tools.cmd/sh and the Tools/dotnetcli "last known good"
# version? (This maybe should be done for format.py as well.)
#
# Script to automate running PMI diffs on a pull request
#
##########################################################################
##########################################################################

import argparse
import distutils.dir_util
import os
import re
import shutil
import subprocess
import urllib
import sys
import tarfile
import zipfile

# Version specific imports
if sys.version_info.major < 3:
    import urllib
else:
    import urllib.request

sys.path.append(os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "scripts"))
from coreclr_arguments import *

##########################################################################
# Globals
##########################################################################

testing = False

Coreclr_url = 'https://github.com/dotnet/coreclr.git'
Jitutils_url = 'https://github.com/dotnet/jitutils.git'

# The Docker file and possibly options should be hoisted out to a text file to be shared between scripts.

Docker_name_arm32 = 'mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-14.04-cross-e435274-20180426002420'
Docker_opts_arm32 = '-e ROOTFS_DIR=/crossrootfs/arm'

Docker_name_arm64 = 'mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-a3ae44b-20180315221921'
Docker_opts_arm64 = '-e ROOTFS_DIR=/crossrootfs/arm64'

Is_illumos = ('illumos' in subprocess.Popen(["uname", "-o"], stdout=subprocess.PIPE, stderr=subprocess.PIPE).communicate()[0].decode('utf-8'))

# This should be factored out of build.sh
Unix_name_map = {
    'Linux': 'Linux',
    'Darwin': 'OSX',
    'FreeBSD': 'FreeBSD',
    'OpenBSD': 'OpenBSD',
    'NetBSD': 'NetBSD',
    'SunOS': 'illumos' if Is_illumos else 'Solaris'
}

Is_windows = (os.name == 'nt')
Clr_os = 'windows' if Is_windows else Unix_name_map[os.uname()[0]]

##########################################################################
# Delete protocol
##########################################################################

def del_rw(action, name, exc):
    os.chmod(name, 0o651)
    os.remove(name)

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to generate JIT assembly diffs from the CoreCLR repo'

parser = argparse.ArgumentParser(description=description)

# base_root is normally expected to be None, in which case we'll clone the
# coreclr tree and build it. If base_root is passed, we'll use it, and not
# clone or build the base.

parser.add_argument('-arch', dest='arch', default='x64')
parser.add_argument('-ci_arch', dest='ci_arch', default=None)
parser.add_argument('-build_type', dest='build_type', default='Checked')
parser.add_argument('-base_root', dest='base_root', default=None)
parser.add_argument('-diff_root', dest='diff_root', default=None)
parser.add_argument('-scratch_root', dest='scratch_root', default=None)
parser.add_argument('--skip_baseline_build', dest='skip_baseline_build', action='store_true', default=False)
parser.add_argument('--skip_diffs', dest='skip_diffs', action='store_true', default=False)
parser.add_argument('-target_branch', dest='target_branch', default='main')
parser.add_argument('-commit_hash', dest='commit_hash', default=None)

##########################################################################
# Class to change the current directory, and automatically restore the
# directory back to what it used to be, on exit.
##########################################################################

class ChangeDir:
    def __init__(self, dir):
        self.dir = dir
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        log('[cd] %s' % self.dir)
        if not testing:
            os.chdir(self.dir)

    def __exit__(self, exc_type, exc_val, exc_tb):
        log('[cd] %s' % self.cwd)
        if not testing:
            os.chdir(self.cwd)

##########################################################################
# Helper Functions
##########################################################################

def validate_args(args):
    """ Validate all of the arguments parsed.
    Args:
        args (argparser.ArgumentParser) : Args parsed by the argument parser.
    Returns:
        args (CoreclrArguments)         : Args parsed
    Notes:
        If the arguments are valid then return them all in a tuple. If not,
        raise an exception stating x argument is incorrect.
    """

    coreclr_setup_args = CoreclrArguments(args,
                                          require_built_test_dir=False,
                                          require_built_core_root=True,
                                          require_built_product_dir=False)

    coreclr_setup_args.verify(args,
                              "base_root",
                              lambda directory: os.path.isdir(directory) if directory is not None else True,
                              "Base root is not a valid directory")

    coreclr_setup_args.verify(args,
                              "diff_root",
                              lambda directory: os.path.isdir(directory) if directory is not None else True,
                              "Diff root is not a valid directory",
                              modify_arg=lambda directory: nth_dirname(os.path.abspath(sys.argv[0]), 3) if directory is None else os.path.abspath(directory))

    coreclr_setup_args.verify(args,
                              "scratch_root",
                              lambda unused: True,
                              "Error setting scratch_root",
                              modify_arg=lambda directory: os.path.join(coreclr_setup_args.diff_root, '_', 'pmi') if directory is None else os.path.abspath(directory))

    coreclr_setup_args.verify(args,
                              "skip_baseline_build",
                              lambda unused: True,
                              "Error setting baseline build")

    coreclr_setup_args.verify(args,
                              "skip_diffs",
                              lambda unused: True,
                              "Error setting skip_diffs")

    coreclr_setup_args.verify(args,
                              "target_branch",
                              lambda unused: True,
                              "Error setting target_branch")

    coreclr_setup_args.verify(args,
                              "commit_hash",
                              lambda unused: True,
                              "Error setting commit_hash")

    coreclr_setup_args.verify(args,
                              "ci_arch",
                              lambda ci_arch: ci_arch in coreclr_setup_args.valid_arches + ['x86_arm_altjit', 'x64_arm64_altjit'],
                              "Error setting ci_arch")

    args = (
        coreclr_setup_args.arch,
        coreclr_setup_args.ci_arch,
        coreclr_setup_args.build_type,
        coreclr_setup_args.base_root,
        coreclr_setup_args.diff_root,
        coreclr_setup_args.scratch_root,
        coreclr_setup_args.skip_baseline_build,
        coreclr_setup_args.skip_diffs,
        coreclr_setup_args.target_branch,
        coreclr_setup_args.commit_hash
    )

    log('Configuration:')
    log('    arch: %s' % coreclr_setup_args.arch)
    log('    ci_arch: %s' % coreclr_setup_args.ci_arch)
    log('    build_type: %s' % coreclr_setup_args.build_type)
    log('    base_root: %s' % coreclr_setup_args.base_root)
    log('    diff_root: %s' % coreclr_setup_args.diff_root)
    log('    scratch_root: %s' % coreclr_setup_args.scratch_root)
    log('    skip_baseline_build: %s' % coreclr_setup_args.skip_baseline_build)
    log('    skip_diffs: %s' % coreclr_setup_args.skip_diffs)
    log('    target_branch: %s' % coreclr_setup_args.target_branch)
    log('    commit_hash: %s' % coreclr_setup_args.commit_hash)

    return args

def nth_dirname(path, n):
    """ Find the Nth parent directory of the given path
    Args:
        path (str): path name containing at least N components
        n (int): num of basenames to remove
    Returns:
        outpath (str): path with the last n components removed
    Notes:
        If n is 0, path is returned unmodified
    """

    assert n >= 0

    for i in range(0, n):
        path = os.path.dirname(path)

    return path

def log(message):
    """ Print logging information
    Args:
        message (str): message to be printed
    """

    print('[%s]: %s' % (sys.argv[0], message))

def copy_files(source_dir, target_dir):
    """ Copy any files in the source_dir to the target_dir.
        The copy is not recursive.
        The directories must already exist.
    Args:
        source_dir (str): source directory path
        target_dir (str): target directory path
    Returns:
        Nothing
    """

    global testing
    assert os.path.isdir(source_dir)
    assert os.path.isdir(target_dir)

    for source_filename in os.listdir(source_dir):
        source_pathname = os.path.join(source_dir, source_filename)
        if os.path.isfile(source_pathname):
            target_pathname = os.path.join(target_dir, source_filename)
            log('Copy: %s => %s' % (source_pathname, target_pathname))
            if not testing:
                shutil.copy2(source_pathname, target_pathname)

def run_command(command, command_env):
    """ Run a command (process) in a given environment. stdout/stderr are output piped through.
    Args:
        command (array): the command to run, with components of the command as separate elements.
        command_env (map): environment in which the command should be run
    Returns:
        The return code of the command.
    """

    returncode = 0

    log('Invoking: %s' % (' '.join(command)))
    if not testing:
        proc = subprocess.Popen(command, env=command_env)
        output,error = proc.communicate()
        returncode = proc.returncode
        if returncode != 0:
            log('Return code = %s' % returncode)

    return returncode

##########################################################################
# Do baseline build:
# 1. determine appropriate commit,
# 2. clone coreclr,
# 3. do build
##########################################################################

def baseline_build():

    if not testing:
        if os.path.isdir(baseCoreClrPath):
            log('Removing existing tree: %s' % baseCoreClrPath)
            shutil.rmtree(baseCoreClrPath, onerror=del_rw)

    # Find the baseline commit

    # Clone at that commit

    command = 'git clone -b %s --single-branch %s %s' % (
        target_branch, Coreclr_url, baseCoreClrPath)
    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        log('ERROR: git clone failed')
        return 1

    # Change directory to the baseline root

    with ChangeDir(baseCoreClrPath):

        # Set up for possible docker usage

        scriptPath = '.'
        buildOpts = ''
        dockerCmd = ''
        if not Is_windows and (arch == 'arm' or arch == 'arm64'):
            # Linux arm and arm64 builds are cross-compilation builds using Docker.
            if arch == 'arm':
                dockerFile = Docker_name_arm32
                dockerOpts = Docker_opts_arm32
            else:
                # arch == 'arm64'
                dockerFile = Docker_name_arm64
                dockerOpts = Docker_opts_arm64

            dockerCmd = 'docker run -i --rm -v %s:%s -w %s %s %s ' % (baseCoreClrPath, baseCoreClrPath, baseCoreClrPath, dockerOpts, dockerFile)
            buildOpts = 'cross'
            scriptPath = baseCoreClrPath

        # Build a checked baseline jit

        if Is_windows:
            command = 'set __TestIntermediateDir=int&&build.cmd %s checked skiptests skipbuildpackages' % arch
        else:
            command = '%s%s/build.sh %s checked skipbuildpackages %s' % (dockerCmd, scriptPath, arch, buildOpts)
        log(command)
        returncode = 0 if testing else os.system(command)
        if returncode != 0:
            log('ERROR: build failed')
            return 1

        # Build the layout (Core_Root) directory
        # For Windows, invoke build-test.cmd to restore packages before generating the layout.

        if Is_windows:
            command = 'build-test.cmd %s %s skipmanaged skipnative' % (build_type, arch)
            log(command)
            returncode = 0 if testing else os.system(command)
            if returncode != 0:
                log('ERROR: restoring packages failed')
                return 1

        if Is_windows:
            command = 'tests\\runtest.cmd %s checked GenerateLayoutOnly' % arch
        else:
            command = '%s%s/build-test.sh %s checked generatelayoutonly' % (dockerCmd, scriptPath, arch)
        log(command)
        returncode = 0 if testing else os.system(command)
        if returncode != 0:
            log('ERROR: generating layout failed')
            return 1

    return 0

##########################################################################
# Do PMI diff run:
# 1. download dotnet CLI (needed by jitutils)
# 2. clone jitutils repo
# 3. build jitutils
# 4. run PMI asm generation on baseline and diffs
# 5. run jit-analyze to compare baseline and diff
##########################################################################

def do_pmi_diffs():
    global baseCoreClrPath

    # Setup scratch directories. Names are short to avoid path length problems on Windows.
    dotnetcliPath = os.path.abspath(os.path.join(scratch_root, 'cli'))
    jitutilsPath = os.path.abspath(os.path.join(scratch_root, 'jitutils'))
    asmRootPath = os.path.abspath(os.path.join(scratch_root, 'asm'))

    dotnet_tool = 'dotnet.exe' if Is_windows else 'dotnet'

    # Make sure the temporary directories do not exist. If they do already, delete them.

    if not testing:
        # If we can't delete the dotnet tree, it might be because a previous run failed or was
        # cancelled, and the build servers are still running. Try to stop it if that happens.
        if os.path.isdir(dotnetcliPath):
            try:
                log('Removing existing tree: %s' % dotnetcliPath)
                shutil.rmtree(dotnetcliPath, onerror=del_rw)
            except OSError:
                if os.path.isfile(os.path.join(dotnetcliPath, dotnet_tool)):
                    log('Failed to remove existing tree; trying to shutdown the dotnet build servers before trying again.')

                    # Looks like the dotnet too is still there; try to run it to shut down the build servers.
                    temp_env = my_env
                    temp_env["PATH"] = dotnetcliPath + os.pathsep + my_env["PATH"]
                    log('Shutting down build servers')
                    command = ["dotnet", "build-server", "shutdown"]
                    returncode = run_command(command, temp_env)

                    # Try again
                    log('Trying again to remove existing tree: %s' % dotnetcliPath)
                    shutil.rmtree(dotnetcliPath, onerror=del_rw)
                else:
                    log('Failed to remove existing tree')
                    return 1

        if os.path.isdir(jitutilsPath):
            log('Removing existing tree: %s' % jitutilsPath)
            shutil.rmtree(jitutilsPath, onerror=del_rw)
        if os.path.isdir(asmRootPath):
            log('Removing existing tree: %s' % asmRootPath)
            shutil.rmtree(asmRootPath, onerror=del_rw)

        try:
            os.makedirs(dotnetcliPath)
            os.makedirs(jitutilsPath)
            os.makedirs(asmRootPath)
        except OSError:
            if not os.path.isdir(dotnetcliPath):
                log('ERROR: cannot create CLI install directory %s' % dotnetcliPath)
                return 1
            if not os.path.isdir(jitutilsPath):
                log('ERROR: cannot create jitutils install directory %s' % jitutilsPath)
                return 1
            if not os.path.isdir(asmRootPath):
                log('ERROR: cannot create asm directory %s' % asmRootPath)
                return 1

    log('dotnet CLI install directory: %s' % dotnetcliPath)
    log('jitutils install directory: %s' % jitutilsPath)
    log('asm directory: %s' % asmRootPath)

    # Download .NET CLI

    log('Downloading .NET CLI')

    dotnetcliUrl = ""
    dotnetcliFilename = ""

    if Clr_os == 'Linux' and arch == 'x64':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-linux-x64.tar.gz"
    elif Clr_os == 'Linux' and arch == 'arm':
        dotnetcliUrl = "https://dotnetcli.blob.core.windows.net/dotnet/Sdk/release/2.1.4xx/dotnet-sdk-latest-linux-arm.tar.gz"
    elif Clr_os == 'Linux' and arch == 'arm64':
        # Use the latest (3.0) dotnet SDK. Earlier versions don't work.
        dotnetcliUrl = "https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-linux-arm64.tar.gz"
    elif Clr_os == 'OSX':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-osx-x64.tar.gz"
    elif Clr_os == 'windows':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-win-x64.zip"
    else:
        log('ERROR: unknown or unsupported OS (%s) architecture (%s) combination' % (Clr_os, arch))
        return 1

    if Is_windows:
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.zip')
    else:
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')

    log('Downloading: %s => %s' % (dotnetcliUrl, dotnetcliFilename))

    if not testing:
        urlretrieve = urllib.urlretrieve if sys.version_info.major < 3 else urllib.request.urlretrieve
        urlretrieve(dotnetcliUrl, dotnetcliFilename)

        if not os.path.isfile(dotnetcliFilename):
            log('ERROR: Did not download .NET CLI')
            return 1

    # Install .NET CLI

    log('Unpacking .NET CLI')

    if not testing:
        if Is_windows:
            with zipfile.ZipFile(dotnetcliFilename, "r") as z:
                z.extractall(dotnetcliPath)
        else:
            tar = tarfile.open(dotnetcliFilename)
            tar.extractall(dotnetcliPath)
            tar.close()

        if not os.path.isfile(os.path.join(dotnetcliPath, dotnet_tool)):
            log('ERROR: did not extract .NET CLI from download')
            return 1

    # Add dotnet CLI to PATH we'll use to spawn processes.

    log('Add %s to my PATH' % dotnetcliPath)
    my_env["PATH"] = dotnetcliPath + os.pathsep + my_env["PATH"]

    # To aid diagnosing problems, do "dotnet --info" to output to any capturing logfile.

    command = ["dotnet", "--info"]
    returncode = run_command(command, my_env)

    # Clone jitutils

    command = 'git clone -b main --single-branch %s %s' % (Jitutils_url, jitutilsPath)
    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        log('ERROR: cannot clone jitutils');
        return 1

    # We're going to start running dotnet CLI commands. Unfortunately, once you've done that,
    # the dotnet CLI sticks around with a set of build server processes running. Put all this
    # in a try/finally, and stop the build servers under any circumstance.

    try:

        #
        # Build jitutils, including "dotnet restore"
        #

        # Change directory to the jitutils root

        with ChangeDir(jitutilsPath):

            # Do "dotnet restore"

            command = ["dotnet", "restore"]
            returncode = run_command(command, my_env)

            # Do build

            command = ['build.cmd', '-p'] if Is_windows else ['bash', './build.sh', '-p']
            returncode = run_command(command, my_env)
            if returncode != 0:
                log('ERROR: jitutils build failed')
                return 1

            jitutilsBin = os.path.join(jitutilsPath, "bin")

            if not testing and not os.path.isdir(jitutilsBin):
                log("ERROR: jitutils not correctly built")
                return 1

            jitDiffPath = os.path.join(jitutilsBin, "jit-diff.dll")
            if not testing and not os.path.isfile(jitDiffPath):
                log("ERROR: jit-diff.dll not built")
                return 1

            jitAnalyzePath = os.path.join(jitutilsBin, "jit-analyze.dll")
            if not testing and not os.path.isfile(jitAnalyzePath):
                log("ERROR: jit-analyze.dll not built")
                return 1

            # Add jitutils bin to path for spawned processes

            log('Add %s to my PATH' % jitutilsBin)
            my_env["PATH"] = jitutilsBin + os.pathsep + my_env["PATH"]

        #
        # Run PMI asm diffs
        #

        # We want this script as a whole to return 0 if it succeeds (even if there are diffs) and only
        # return non-zero if there are any fatal errors.
        #
        # TO DO: figure out how to differentiate fatal errors and a return code indicating there are diffs,
        # and have the invoking netci.groovy code act differently for each case.

        # Generate the diffs
        #
        # Invoke command like:
        #   dotnet c:\gh\jitutils\artifacts\jit-diff.dll diff --pmi --base --base_root f:\gh\coreclr12 --diff --diff_root f:\gh\coreclr10 --arch x64 --build Checked --tag 1 --noanalyze --output f:\output --corelib
        #
        # We pass --noanalyze and call jit-analyze manually. This isn't really necessary, but it does give us better output
        # due to https://github.com/dotnet/jitutils/issues/175.

        altjit_args = []
        if ci_arch is not None and (ci_arch == 'x86_arm_altjit' or ci_arch == 'x64_arm64_altjit'):
            altjit_args = ["--altjit", "protononjit.dll"]

        # Over which set of assemblies should we generate asm?
        # TODO: parameterize this
        asm_source_args = ["--frameworks", "--benchmarks"]

        command = ["dotnet", jitDiffPath, "diff", "--pmi", "--base", "--base_root", baseCoreClrPath, "--diff", "--diff_root", diff_root, "--arch", arch, "--build", build_type, "--tag", "1", "--noanalyze", "--output", asmRootPath] + asm_source_args + altjit_args
        returncode = run_command(command, my_env)

        # We ignore the return code: it is non-zero if there are any diffs. If there are fatal errors here, we will miss them.
        # Question: does jit-diff distinguish between non-zero fatal error code and the existence of diffs?

        # Did we get any diffs?

        baseOutputDir = os.path.join(asmRootPath, "1", "base")
        if not testing and not os.path.isdir(baseOutputDir):
            log("ERROR: base asm not generated")
            return 1

        diffOutputDir = os.path.join(asmRootPath, "1", "diff")
        if not testing and not os.path.isdir(diffOutputDir):
            log("ERROR: diff asm not generated")
            return 1

        # Do the jit-analyze comparison:
        #   dotnet c:\gh\jitutils\artifacts\jit-analyze.dll --base f:\output\diffs\1\base --recursive --diff f:\output\diffs\1\diff

        command = ["dotnet", jitAnalyzePath, "--recursive", "--base", baseOutputDir, "--diff", diffOutputDir]
        returncode = run_command(command, my_env)
        if returncode != 0:
            # This is not a fatal error.
            log('Compare: %s %s' % (baseOutputDir, diffOutputDir))

    finally:

        # Shutdown the dotnet build servers before cleaning things up
        # TODO: make this shutdown happen anytime after we've run any 'dotnet' commands. I.e., try/finally style.

        log('Shutting down build servers')
        command = ["dotnet", "build-server", "shutdown"]
        returncode = run_command(command, my_env)

    return 0

##########################################################################
# Main
##########################################################################

def main(args):

    global arch, ci_arch, build_type, base_root, diff_root, scratch_root, skip_baseline_build, skip_diffs, target_branch, commit_hash
    global my_env
    global base_layout_root
    global diff_layout_root
    global baseCoreClrPath
    global testing

    arch, ci_arch, build_type, base_root, diff_root, scratch_root, skip_baseline_build, skip_diffs, target_branch, commit_hash = validate_args(args)

    my_env = os.environ

    if not testing and not os.path.isdir(diff_root):
       log('ERROR: root directory for coreclr diff tree not found: %s' % diff_root)
       return 1

    # Check the diff layout directory before going too far.

    diff_layout_root = os.path.join(diff_root,
                                    'bin',
                                    'tests',
                                    '%s.%s.%s' % (Clr_os, arch, build_type),
                                    'Tests',
                                    'Core_Root')

    if not testing and not os.path.isdir(diff_layout_root):
       log('ERROR: diff test overlay not found or is not a directory: %s' % diff_layout_root)
       return 1

    # Create the scratch root directory

    if not testing:
        try:
            os.makedirs(scratch_root)
        except OSError:
            if not os.path.isdir(scratch_root):
                log('ERROR: cannot create scratch directory %s' % scratch_root)
                return 1

    # Set up baseline root directory. If one is passed to us, we use it. Otherwise, we create
    # a temporary directory.

    if base_root is None:
        # Setup scratch directories. Names are short to avoid path length problems on Windows.
        # No need to create this directory now, as the "git clone" will do it later.
        baseCoreClrPath = os.path.abspath(os.path.join(scratch_root, 'base'))
    else:
        baseCoreClrPath = os.path.abspath(base_root)
        if not testing and not os.path.isdir(baseCoreClrPath):
           log('ERROR: base root directory not found or is not a directory: %s' % baseCoreClrPath)
           return 1

    # Do the baseline build, if needed

    if not skip_baseline_build and base_root is None:
        returncode = baseline_build()
        if returncode != 0:
            return 1

    # Check that the baseline root directory was created.

    base_layout_root = os.path.join(baseCoreClrPath,
                                    'bin',
                                    'tests',
                                    '%s.%s.%s' % (Clr_os, arch, build_type),
                                    'Tests',
                                    'Core_Root')

    if not testing and not os.path.isdir(base_layout_root):
       log('ERROR: baseline test overlay not found or is not a directory: %s' % base_layout_root)
       return 1

    # Do the diff run, if needed

    if not skip_diffs:
        returncode = do_pmi_diffs()
        if returncode != 0:
            return 1

    return 0


##########################################################################
# setup for Main
##########################################################################

if __name__ == '__main__':
    Args = parser.parse_args(sys.argv[1:])
    return_code = main(Args)
    log('Exit code: %s' % return_code)
    sys.exit(return_code)
