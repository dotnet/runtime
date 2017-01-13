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
parser.add_argument('-build_type', dest='build_type', default='Debug')
parser.add_argument('-clr_root', dest='clr_root', default=None)
parser.add_argument('-fx_root', dest='fx_root', default=None)
parser.add_argument('-fx_branch', dest='fx_branch', default='master')
parser.add_argument('-fx_commit', dest='fx_commit', default=None)
parser.add_argument('-env_script', dest='env_script', default=None)


##########################################################################
# Helper Functions
##########################################################################

def validate_args(args):
    """ Validate all of the arguments parsed.
    Args:
        args (argparser.ArgumentParser): Args parsed by the argument parser.
    Returns:
        (arch, build_type, clr_root, fx_root, fx_branch, fx_commit, env_script)
            (str, str, str, str, str, str, str)
    Notes:
    If the arguments are valid then return them all in a tuple. If not, raise
    an exception stating x argument is incorrect.
    """

    arch = args.arch
    build_type = args.build_type
    clr_root = args.clr_root
    fx_root = args.fx_root
    fx_branch = args.fx_branch
    fx_commit = args.fx_commit
    env_script = args.env_script

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
        fx_commit = '551fe49174378adcbf785c0ab12fc69355cef6e8' if fx_branch == 'master' else 'HEAD'

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

    args = (arch, build_type, clr_root, fx_root, fx_branch, fx_commit, env_script)

    log('Configuration:')
    log(' arch: %s' % arch)
    log(' build_type: %s' % build_type)
    log(' clr_root: %s' % clr_root)
    log(' fx_root: %s' % fx_root)
    log(' fx_branch: %s' % fx_branch)
    log(' fx_commit: %s' % fx_commit)
    log(' env_script: %s' % env_script)

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


def dotnet_rid_os(dotnet_path):
    """ Determine the OS identifier from the RID as reported by dotnet
    Args:
        dotnet_path (str): path to folder containing dotnet(.exe)
    Returns:
        rid_os (str): OS component of RID as reported by dotnet
    """
    dotnet_info = subprocess.check_output([os.path.join(dotnet_path, 'dotnet'), '--info'])
    m = re.search('^\s*RID:\s+([^-]*)-(\S*)\s*$', dotnet_info, re.MULTILINE)
    return m.group(1)


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

    arch, build_type, clr_root, fx_root, fx_branch, fx_commit, env_script = validate_args(
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

    if os.path.exists(fx_root):
        if Is_windows:
            vbcscompiler_running = True
            while vbcscompiler_running:
                res = subprocess.check_output(['tasklist'])
                if not 'VBCSCompiler.exe' in res:
                    vbcscompiler_running = False
        os.chdir(fx_root)
        os.system('git clean -fxd')
        os.chdir(clr_root)
        shutil.rmtree(fx_root, onerror=del_rw)

    command = 'git clone -b %s --single-branch %s %s' % (
        fx_branch, Corefx_url, fx_root)

    log(command)

    testing = False

    if testing:
        os.makedirs(fx_root)
        returncode = 0
    else:
        returncode = os.system(command)

    if returncode != 0:
        sys.exit(returncode)


    command = "git -C %s checkout %s" % (
        fx_root, fx_commit)

    log(command)

    if testing:
        returncode = 0
    else:
        returncode = os.system(command)

    if returncode != 0:
        sys.exit(returncode)

    cwd = os.getcwd()
    log('cd ' + fx_root)
    os.chdir(fx_root)

    if Is_windows:
        command = '.\\build.cmd'
        if env_script is not None:
            command = ('cmd /c %s&&' % env_script) + command
    else:
        # CoreFx build.sh requires HOME to be set, and it isn't by default
        # under our CI.
        fx_home = os.path.join(fx_root, 'tempHome')
        if not os.path.exists(fx_home):
            os.makedirs(fx_home)
        os.putenv('HOME', fx_home)
        log('HOME=' + fx_home)

        command = './build.sh'
        if env_script is not None:
            command = ('. %s;' % env_script) + command

    if testing:
        rid_os = dotnet_rid_os('')
    else:
        if clr_os == "Windows_NT":
            rid_os = "win7"
        else:
            rid_os = dotnet_rid_os(os.path.join(clr_root, 'Tools', 'dotnetcli'))

    command = ' '.join((
        command,
        '-Release',
        '-TestNugetRuntimeId=%s-%s' % (rid_os, arch),
        '--',
        '/p:BUILDTOOLS_OVERRIDE_RUNTIME="%s"' % core_root,
        '/p:WithoutCategories=IgnoreForCI'
    ))

    if not Is_windows:
        command += ' /p:TestWithLocalNativeLibraries=true'

    log(command)

    if testing:
        returncode = 0
    else:
        returncode = os.system(command)

    sys.exit(returncode)


##########################################################################
# setup for Main
##########################################################################

if __name__ == '__main__':
    Args = parser.parse_args(sys.argv[1:])

    main(Args)
