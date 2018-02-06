#!/usr/bin/env python
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
##########################################################################
##########################################################################
#
# Module: run-corefx-tests.py
#
# Notes:
#
# Script to clone the CoreFx repo, build, and run its tests.
#
##########################################################################
##########################################################################

import argparse
import distutils.dir_util
import os
import re
import shutil
import subprocess
import sys


##########################################################################
# Globals
##########################################################################

Corefx_url = 'https://github.com/dotnet/corefx.git'

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

##########################################################################
# Delete protocol
##########################################################################

def del_rw(action, name, exc):
    os.chmod(name, 0651)
    os.remove(name)

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to facilitate running CoreFx tests from the CoreCLR repo'

parser = argparse.ArgumentParser(description=description)

parser.add_argument('-arch', dest='arch', default='x64')
parser.add_argument('-ci_arch', dest='ci_arch', default=None)
parser.add_argument('-build_type', dest='build_type', default='Debug')
parser.add_argument('-clr_root', dest='clr_root', default=None)
parser.add_argument('-fx_root', dest='fx_root', default=None)
parser.add_argument('-fx_branch', dest='fx_branch', default='master')
parser.add_argument('-fx_commit', dest='fx_commit', default=None)
parser.add_argument('-env_script', dest='env_script', default=None)
parser.add_argument('-no_run_tests', dest='no_run_tests', action="store_true", default=False)


##########################################################################
# Helper Functions
##########################################################################

def validate_args(args):
    """ Validate all of the arguments parsed.
    Args:
        args (argparser.ArgumentParser): Args parsed by the argument parser.
    Returns:
        (arch, ci_arch, build_type, clr_root, fx_root, fx_branch, fx_commit, env_script, no_run_tests)
            (str, str, str, str, str, str, str, str)
    Notes:
    If the arguments are valid then return them all in a tuple. If not, raise
    an exception stating x argument is incorrect.
    """

    arch = args.arch
    ci_arch = args.ci_arch
    build_type = args.build_type
    clr_root = args.clr_root
    fx_root = args.fx_root
    fx_branch = args.fx_branch
    fx_commit = args.fx_commit
    env_script = args.env_script
    no_run_tests = args.no_run_tests

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
    valid_build_types = ['Debug', 'Checked', 'Release']

    arch = next((a for a in valid_archs if a.lower() == arch.lower()), arch)
    build_type = next((b for b in valid_build_types if b.lower() == build_type.lower()), build_type)

    validate_arg(arch, lambda item: item in valid_archs)
    validate_arg(build_type, lambda item: item in valid_build_types)
    validate_arg(fx_branch, lambda item: True)

    if fx_commit is None:
        fx_commit = 'HEAD'

    if clr_root is None:
        clr_root = nth_dirname(os.path.abspath(sys.argv[0]), 3)
    else:
        clr_root = os.path.normpath(clr_root)
        validate_arg(clr_root, lambda item: os.path.isdir(clr_root))

    if fx_root is None:
        fx_root = os.path.join(clr_root, '_', 'fx')
    else:
        fx_root = os.path.normpath(fx_root)

    if env_script is not None:
        validate_arg(env_script, lambda item: os.path.isfile(env_script))
        env_script = os.path.abspath(env_script)

    args = (arch, ci_arch, build_type, clr_root, fx_root, fx_branch, fx_commit, env_script, no_run_tests)

    log('Configuration:')
    log(' arch: %s' % arch)
    log(' ci_arch: %s' % ci_arch)
    log(' build_type: %s' % build_type)
    log(' clr_root: %s' % clr_root)
    log(' fx_root: %s' % fx_root)
    log(' fx_branch: %s' % fx_branch)
    log(' fx_commit: %s' % fx_commit)
    log(' env_script: %s' % env_script)
    log(' no_run_tests: %s' % no_run_tests)

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

##########################################################################
# Main
##########################################################################

def main(args):
    global Corefx_url
    global Unix_name_map

    testing = False

    arch, ci_arch, build_type, clr_root, fx_root, fx_branch, fx_commit, env_script, no_run_tests = validate_args(
        args)

    clr_os = 'Windows_NT' if Is_windows else Unix_name_map[os.uname()[0]]

    core_root = os.path.join(clr_root,
                             'bin',
                             'Product',
                             '%s.%s.%s' % (clr_os, arch, build_type))

    # corefx creates both files that are read-only and files that include non-ascii
    # characters. Using onerror=del_rw allows us to delete all of the read-only files.
    # To delete the files with non-ascii characters, when rmtree fails due to those
    # files, we then will call rd on Windows.

    if not testing and os.path.exists(fx_root):
        if Is_windows:
            while True:
                res = subprocess.check_output(['tasklist'])
                if not 'VBCSCompiler.exe' in res:
                   break                
        os.chdir(fx_root)
        os.system('git clean -fxd')
        os.chdir(clr_root)
        shutil.rmtree(fx_root, onerror=del_rw)

    # Clone the corefx branch

    command = 'git clone -b %s --single-branch %s %s' % (
        fx_branch, Corefx_url, fx_root)
    log(command)
    if testing:
        if not os.path.exists(fx_root):
            os.makedirs(fx_root)
        returncode = 0
    else:
        returncode = os.system(command)

    # Change directory to the corefx root

    cwd = os.getcwd()
    log('[cd] ' + fx_root)
    os.chdir(fx_root)

    # Checkout the appropriate corefx commit

    command = "git checkout %s" % fx_commit
    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        sys.exit(1)

    # On Unix, coreFx build.sh requires HOME to be set, and it isn't by default
    # under our CI system, so set it now.

    if not Is_windows:
        fx_home = os.path.join(fx_root, 'tempHome')
        if not os.path.exists(fx_home):
            os.makedirs(fx_home)
        os.putenv('HOME', fx_home)
        log('HOME=' + fx_home)

    # Determine the RID to specify the to corefix build scripts.  This seems to
    # be way harder than it ought to be.
 
    # Gather up some arguments to pass to both build and build-tests.

    config_args = '-Release -os:%s -buildArch:%s' % (clr_os, arch)

    # Run the primary (non-test) corefx build

    command = ' '.join(('build.cmd' if Is_windows else './build.sh',
                        config_args,
                        '-- /p:CoreCLROverridePath=%s' % core_root))

    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        sys.exit(1)

    # Build the build-tests command line.

    if Is_windows:
        command = 'build-tests.cmd'
    else:
        command = './build-tests.sh'

    # If we're doing altjit testing, then don't run any tests that don't work with altjit.
    if ci_arch is not None and (ci_arch == 'x86_arm_altjit' or ci_arch == 'x64_arm64_altjit'):
        # The property value we need to specify is a semicolon separated list of two values,
        # so the two values must be enclosed in double quotes. Without the quotes, msbuild
        # thinks the item after the semicolon is a different named property. Also, the double
        # quotes need preceeding backslashes or else run.exe (invoked from build-tests.cmd)
        # will eat them. They need to be double backslashes so Python preserves the backslashes.
        without_categories = '/p:WithoutCategories=\\"IgnoreForCI;XsltcExeRequired\\"'
    else:
        without_categories = '/p:WithoutCategories=IgnoreForCI'

    command = ' '.join((
        command,
        config_args,
        '-SkipTests' if no_run_tests else '',
        '--',
        without_categories
    ))

    if env_script is not None:
        command += (' /p:PreExecutionTestScript=%s' % env_script)

    if not Is_windows:
        command += ' /p:TestWithLocalNativeLibraries=true'

    # Run the corefx test build and run the tests themselves.

    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        sys.exit(1)

    sys.exit(0)


##########################################################################
# setup for Main
##########################################################################

if __name__ == '__main__':
    Args = parser.parse_args(sys.argv[1:])

    main(Args)
