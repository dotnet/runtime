#!/usr/bin/env python
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
##########################################################################
##########################################################################
#
# Module: run-pmi-diffs.py
#
# Notes:
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
import urllib2
import sys
import tarfile
import zipfile

##########################################################################
# Globals
##########################################################################

testing = False

Coreclr_url = 'https://github.com/dotnet/coreclr.git'
Jitutils_url = 'https://github.com/dotnet/jitutils.git'

# This should be factored out of build.sh
Unix_name_map = {
    'Linux': 'Linux',
    'Darwin': 'OSX',
    'FreeBSD': 'FreeBSD',
    'OpenBSD': 'OpenBSD',
    'NetBSD': 'NetBSD',
    'SunOS': 'SunOS'
}

Is_windows = (os.name == 'nt')
clr_os = 'Windows_NT' if Is_windows else Unix_name_map[os.uname()[0]]

##########################################################################
# Delete protocol
##########################################################################

def del_rw(action, name, exc):
    os.chmod(name, 0651)
    os.remove(name)

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to generate JIT assembly diffs from the CoreCLR repo'

parser = argparse.ArgumentParser(description=description)

# base_root is normally expected to be None, in which case we'll clone the
# coreclr tree and build it. If base_root is passed, we'll use it, and not
# clone or build the base.

# TODO: need to fix parser so -skip_baseline_build / -skip_diffs don't take an argument

parser.add_argument('-arch', dest='arch', default='x64')
parser.add_argument('-ci_arch', dest='ci_arch', default=None)
parser.add_argument('-build_type', dest='build_type', default='Checked')
parser.add_argument('-base_root', dest='base_root', default=None)
parser.add_argument('-diff_root', dest='diff_root', default=None)
parser.add_argument('-scratch_root', dest='scratch_root', default=None)
parser.add_argument('-skip_baseline_build', dest='skip_baseline_build', default=False)
parser.add_argument('-skip_diffs', dest='skip_diffs', default=False)
parser.add_argument('-target_branch', dest='target_branch', default='master')
parser.add_argument('-commit_hash', dest='commit_hash', default=None)

##########################################################################
# Helper Functions
##########################################################################

def validate_args(args):
    """ Validate all of the arguments parsed.
    Args:
        args (argparser.ArgumentParser): Args parsed by the argument parser.
    Returns:
        (arch, ci_arch, build_type, base_root, diff_root, scratch_root, skip_baseline_build, skip_diffs, target_branch, commit_hash)
            (str, str, str, str, str, str, bool, bool, str, str)
    Notes:
    If the arguments are valid then return them all in a tuple. If not, raise
    an exception stating x argument is incorrect.
    """

    arch = args.arch
    ci_arch = args.ci_arch
    build_type = args.build_type
    base_root = args.base_root
    diff_root = args.diff_root
    scratch_root = args.scratch_root
    skip_baseline_build = args.skip_baseline_build
    skip_diffs = args.skip_diffs
    target_branch = args.target_branch
    commit_hash = args.commit_hash

    def validate_arg(arg, check):
        """ Validate an individual arg
        Args:
           arg (str|bool): argument to be validated
           check (lambda: x-> bool): test that returns either True or False
                                   : based on whether the check passes.

        Returns:
           is_valid (bool): Is the argument valid?
        """

        helper = lambda item: item is not None and check(item)

        if not helper(arg):
            raise Exception('Argument: %s is not valid.' % (arg))

    valid_archs = ['x86', 'x64', 'arm', 'arm64']
    valid_ci_archs = valid_archs + ['x86_arm_altjit', 'x64_arm64_altjit']
    valid_build_types = ['Debug', 'Checked', 'Release']

    arch = next((a for a in valid_archs if a.lower() == arch.lower()), arch)
    build_type = next((b for b in valid_build_types if b.lower() == build_type.lower()), build_type)

    validate_arg(arch, lambda item: item in valid_archs)
    validate_arg(build_type, lambda item: item in valid_build_types)

    if diff_root is None:
        diff_root = nth_dirname(os.path.abspath(sys.argv[0]), 3)
    else:
        diff_root = os.path.abspath(diff_root)
        validate_arg(diff_root, lambda item: os.path.isdir(diff_root))

    if scratch_root is None:
        scratch_root = os.path.join(diff_root, '_')
    else:
        scratch_root = os.path.abspath(scratch_root)

    if ci_arch is not None:
        validate_arg(ci_arch, lambda item: item in valid_ci_archs)

    args = (arch, ci_arch, build_type, base_root, diff_root, scratch_root, skip_baseline_build, skip_diffs, target_branch, commit_hash)

    log('Configuration:')
    log(' arch: %s' % arch)
    log(' ci_arch: %s' % ci_arch)
    log(' build_type: %s' % build_type)
    log(' base_root: %s' % base_root)
    log(' diff_root: %s' % diff_root)
    log(' scratch_root: %s' % scratch_root)
    log(' skip_baseline_build: %s' % skip_baseline_build)
    log(' skip_diffs: %s' % skip_diffs)
    log(' target_branch: %s' % target_branch)
    log(' commit_hash: %s' % commit_hash)

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

    print '[%s]: %s' % (sys.argv[0], message)

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

    cwd = os.getcwd()
    log('[cd] %s' % baseCoreClrPath)
    if not testing:
        os.chdir(baseCoreClrPath)

    # Set up for possible docker usage

    scriptPath = '.'
    buildOpts = ''
    dockerCmd = ''
    if not Is_windows and (arch == 'arm' or arch == 'arm64'):
        # Linux arm and arm64 builds are cross-compilation builds using Docker.
        if arch == 'arm':
            dockerFile = 'microsoft/dotnet-buildtools-prereqs:ubuntu-14.04-cross-e435274-20180426002420'
            dockerOpts = '-e ROOTFS_DIR=/crossrootfs/arm -e CAC_ROOTFS_DIR=/crossrootfs/x86'
        else:
            # arch == 'arm64'
            dockerFile = 'microsoft/dotnet-buildtools-prereqs:ubuntu-16.04-cross-arm64-a3ae44b-20180315221921'
            dockerOpts = '-e ROOTFS_DIR=/crossrootfs/arm64'

        dockerCmd = 'docker run -i --rm -v %s:%s -w %s %s %s ' % (baseCoreClrPath, baseCoreClrPath, baseCoreClrPath, dockerOpts, dockerFile)
        buildOpts = 'cross crosscomponent'
        scriptPath = baseCoreClrPath

    # Build a checked baseline jit 

    if Is_windows:
        command = 'set __TestIntermediateDir=int&&build.cmd %s checked skiptests skipbuildpackages' % arch
    else:
        command = '%s%s/build.sh %s checked skiptests skipbuildpackages %s' % (dockerCmd, scriptPath, arch, buildOpts)
    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        log('ERROR: build failed')
        return 1

    # Build the layout (Core_Root) directory

    # For Windows, you need to first do a restore. It's unfortunately complicated. Run:
    #   run.cmd build -Project="tests\build.proj" -BuildOS=Windows_NT -BuildType=Checked -BuildArch=x64 -BatchRestorePackages

    if Is_windows:
        command = 'run.cmd build -Project="tests\\build.proj" -BuildOS=Windows_NT -BuildType=%s -BuildArch=%s -BatchRestorePackages' % (build_type, arch)
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

    # After baseline build, change directory back to where we started

    log('[cd] %s' % cwd)
    if not testing:
        os.chdir(cwd)

    return 0

##########################################################################
# Do PMI diff run:
# 1. download dotnet CLI (needed by jitutils)
# 2. clone jitutils repo
# 3. build jitutils
# 4. run PMI asm generation on baseline
# 5. run PMI asm generation on diff
# 6. run jit-analyze to compare baseline and diff
##########################################################################

def do_pmi_diffs():
    global baseCoreClrPath

    # Setup scratch directories. Names are short to avoid path length problems on Windows.
    dotnetcliPath = os.path.abspath(os.path.join(scratch_root, '_d'))
    jitutilsPath = os.path.abspath(os.path.join(scratch_root, '_j'))
    asmRootPath = os.path.abspath(os.path.join(scratch_root, '_asm'))

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
                    log('Invoking: %s' % (' '.join(command)))
                    proc = subprocess.Popen(command, env=temp_env)
                    output,error = proc.communicate()
                    returncode = proc.returncode
                    if returncode != 0:
                        log('Return code = %s' % returncode)

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
                log('ERROR: cannot create diff directory %s' % asmRootPath)
                return 1

    log('dotnet CLI install directory: %s' % dotnetcliPath)
    log('jitutils install directory: %s' % jitutilsPath)
    log('asm directory: %s' % asmRootPath)

    # Download .NET CLI

    log('Downloading .Net CLI')

    dotnetcliUrl = ""
    dotnetcliFilename = ""

    if clr_os == 'Linux':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-linux-x64.tar.gz"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')
    elif clr_os == 'OSX':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-osx-x64.tar.gz"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')
    elif clr_os == 'Windows_NT':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-win-x64.zip"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.zip')
    else:
        log('ERROR: unknown or unsupported OS %s' % os)
        return 1

    log('Downloading: %s => %s' % (dotnetcliUrl, dotnetcliFilename))

    if not testing:
        response = urllib2.urlopen(dotnetcliUrl)
        request_url = response.geturl()
        testfile = urllib.URLopener()
        testfile.retrieve(request_url, dotnetcliFilename)

        if not os.path.isfile(dotnetcliFilename):
            log('ERROR: Did not download .Net CLI')
            return 1

    # Install .Net CLI

    log('Unpacking .Net CLI')

    if not testing:
        if Is_windows:
            with zipfile.ZipFile(dotnetcliFilename, "r") as z:
                z.extractall(dotnetcliPath)
        else:
            tar = tarfile.open(dotnetcliFilename)
            tar.extractall(dotnetcliPath)
            tar.close()

        if not os.path.isfile(os.path.join(dotnetcliPath, dotnet_tool)):
            log('ERROR: did not extract .Net CLI from download')
            return 1

    # Add dotnet CLI to PATH we'll use to spawn processes.

    log('Add %s to my PATH' % dotnetcliPath)
    my_env["PATH"] = dotnetcliPath + os.pathsep + my_env["PATH"]

    # Clone jitutils

    command = 'git clone -b master --single-branch %s %s' % (Jitutils_url, jitutilsPath)
    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        log('ERROR: cannot clone jitutils');
        return 1

    #
    # Build jitutils, including "dotnet restore"
    #

    # Change directory to the jitutils root

    cwd = os.getcwd()
    log('[cd] %s' % jitutilsPath)
    if not testing:
        os.chdir(jitutilsPath)

    # Do "dotnet restore"

    command = ["dotnet", "restore"]
    log('Invoking: %s' % (' '.join(command)))
    if not testing:
        proc = subprocess.Popen(command, env=my_env)
        output,error = proc.communicate()
        returncode = proc.returncode
        if returncode != 0:
            log('Return code = %s' % returncode)

    # Do build

    command = ['build.cmd' if Is_windows else 'build.sh', '-p']
    log('Invoking: %s' % (' '.join(command)))
    if not testing:
        proc = subprocess.Popen(command, env=my_env)
        output,error = proc.communicate()
        returncode = proc.returncode
        if returncode != 0:
            log('Return code = %s' % returncode)
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

    # After baseline build, change directory back to where we started

    log('[cd] %s' % cwd)
    if not testing:
        os.chdir(cwd)

    #
    # Run PMI asm diffs
    #

    # We continue through many failures, to get as much asm generated as possible. But make sure we return
    # a failure code if there are any failures.

    result = 0

    # First, generate the diffs

    # Invoke command like:
    #   dotnet c:\gh\jitutils\bin\jit-diff.dll diff --pmi --corelib --diff --diff_root f:\gh\coreclr10 --arch x64 --build Checked --tag diff --output f:\output\diffs
    #
    # TODO: Fix issues when invoking this from a script:
    # 1. There is no way to turn off the progress output
    # 2. Make it easier to specify the exact directory you want output to go to?
    # 3. run base and diff with a single command?
    # 4. put base and diff in saner directory names.

    altjit_args = []
    if ci_arch is not None and (ci_arch == 'x86_arm_altjit' or ci_arch == 'x64_arm64_altjit'):
        altjit_args = ["--altjit", "protononjit.dll"]

    # Over which set of assemblies should we generate asm?
    # TODO: parameterize this
    asm_source_args = ["--corelib"]
    # asm_source_args = ["--frameworks"]

    command = ["dotnet", jitDiffPath, "diff", "--pmi", "--diff", "--diff_root", diff_root, "--arch", arch, "--build", build_type, "--tag", "diff", "--output", asmRootPath] + asm_source_args + altjit_args
    log('Invoking: %s' % (' '.join(command)))
    if not testing:
        proc = subprocess.Popen(command, env=my_env)
        output,error = proc.communicate()
        returncode = proc.returncode
        if returncode != 0:
            log('Return code = %s' % returncode)
            result = 1

    # Did we get any diffs?

    diffOutputDir = os.path.join(asmRootPath, "diff", "diff")
    if not testing and not os.path.isdir(diffOutputDir):
        log("ERROR: diff asm not generated")
        return 1

    # Next, generate the baseline asm

    command = ["dotnet", jitDiffPath, "diff", "--pmi", "--base", "--base_root", baseCoreClrPath, "--arch", arch, "--build", build_type, "--tag", "base", "--output", asmRootPath] + asm_source_args + altjit_args
    log('Invoking: %s' % (' '.join(command)))
    if not testing:
        proc = subprocess.Popen(command, env=my_env)
        output,error = proc.communicate()
        returncode = proc.returncode
        if returncode != 0:
            log('Return code = %s' % returncode)
            result = 1

    # Did we get any diffs?

    baseOutputDir = os.path.join(asmRootPath, "base", "base")
    if not testing and not os.path.isdir(baseOutputDir):
        log("ERROR: base asm not generated")
        return 1

    # Do the jit-analyze comparison:
    #   dotnet c:\gh\jitutils\bin\jit-analyze.dll --base f:\output\diffs\base\diff --recursive --diff f:\output\diffs\diff\diff

    command = ["dotnet", jitAnalyzePath, "--base", baseOutputDir, "--diff", diffOutputDir]
    log('Invoking: %s' % (' '.join(command)))
    if not testing:
        proc = subprocess.Popen(command, env=my_env)
        output,error = proc.communicate()
        returncode = proc.returncode
        if returncode != 0:
            log('Return code = %s' % returncode)
            log('Compare: %s %s' % (baseOutputDir, diffOutputDir))

    # Shutdown the dotnet build servers before cleaning things up
    # TODO: make this shutdown happen anytime after we've run any 'dotnet' commands. I.e., try/finally style.

    log('Shutting down build servers')
    command = ["dotnet", "build-server", "shutdown"]
    log('Invoking: %s' % (' '.join(command)))
    if not testing:
        proc = subprocess.Popen(command, env=my_env)
        output,error = proc.communicate()
        returncode = proc.returncode
        if returncode != 0:
            log('Return code = %s' % returncode)

    return result

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
                                    '%s.%s.%s' % (clr_os, arch, build_type),
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
        baseCoreClrPath = os.path.abspath(os.path.join(scratch_root, '_c'))
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
                                    '%s.%s.%s' % (clr_os, arch, build_type),
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
