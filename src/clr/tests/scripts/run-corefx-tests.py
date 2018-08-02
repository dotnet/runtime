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

testing = False

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
            (str, str, str, str, str, str, str, str, str)
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
# Main
##########################################################################

def main(args):
    global Corefx_url
    global Unix_name_map
    global testing

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
 
    # Gather up some arguments to pass to build-managed, build-native, and build-tests scripts.

    config_args = '-Release -os:%s -buildArch:%s' % (clr_os, arch)

    # Run the primary (non-test) corefx build. We previously passed the argument:
    #
    #    /p:CoreCLROverridePath=<path-to-core_root>
    #
    # which causes the corefx build to overwrite its built runtime with the binaries from
    # the coreclr build. However, this often causes build failures when breaking changes are
    # in progress (e.g., a breaking change is made in coreclr that has not yet had compensating
    # changes made in the corefx repo). Instead, build corefx normally. This should always work
    # since corefx is protected by a CI testing system. Then, overwrite the built corefx
    # runtime with the runtime built in the coreclr build. The result will be that perhaps
    # some, hopefully few, corefx tests will fail, but the builds will never fail.

    # Cross build corefx for arm64 on x64.
    # Cross build corefx for arm32 on x86.

    build_native_args = ''

    if not Is_windows and arch == 'arm' :
        # We need to force clang5.0; we are building in a docker container that doesn't have
        # clang3.9, which is currently the default used by build-native.sh. We need to pass
        # "-cross", but we also pass "-portable", which build-native.sh normally passes
        # (there doesn't appear to be a way to pass these individually).
        build_native_args += ' -AdditionalArgs:"-portable -cross" -Clang:clang5.0'

    if not Is_windows and arch == 'arm64' :
        # We need to pass "-cross", but we also pass "-portable", which build-native.sh normally
        # passes (there doesn't appear to be a way to pass these individually).
        build_native_args += ' -AdditionalArgs:"-portable -cross"'

    command = ' '.join(('build-native.cmd' if Is_windows else './build-native.sh',
                        config_args,
                        build_native_args))
    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        log('Error: exit code %s' % returncode)
        sys.exit(1)

    command = ' '.join(('build-managed.cmd' if Is_windows else './build-managed.sh', config_args))
    log(command)
    returncode = 0 if testing else os.system(command)
    if returncode != 0:
        log('Error: exit code %s' % returncode)
        sys.exit(1)

    # Override the built corefx runtime (which it picked up by copying from packages determined
    # by its dependencies.props file). Note that we always build Release corefx.
    # We must copy all files, not just the files that already exist in the corefx runtime
    # directory. This is required so we copy over all altjit compilers.
    # TODO: it might be cleaner to encapsulate the knowledge of how to do this in the
    # corefx msbuild files somewhere.

    fx_runtime = os.path.join(fx_root,
                             'bin',
                             'testhost',
                             'netcoreapp-%s-%s-%s' % (clr_os, 'Release', arch),
                             'shared',
                             'Microsoft.NETCore.App',
                             '9.9.9')

    log('Updating CoreCLR: %s => %s' % (core_root, fx_runtime))
    copy_files(core_root, fx_runtime)

    # Build the build-tests command line.

    if Is_windows:
        command = 'build-tests.cmd'
    else:
        command = './build-tests.sh'

    # If we're doing altjit testing, then don't run any tests that don't work with altjit.
    if ci_arch is not None and (ci_arch == 'x86_arm_altjit' or ci_arch == 'x64_arm64_altjit'):
        # The property value we need to specify for the WithoutCategories property is a semicolon
        # separated list of two values, so the two values must be enclosed in double quotes, namely:
        #
        #  /p:WithoutCategories="IgnoreForCI;XsltcExeRequired"
        #
        # Without the quotes, msbuild interprets the semicolon as separating two name/value pairs,
        # which is incorrect (and causes an error).
        #
        # If we pass this on the command-line, it requires an extraordinary number of backslashes
        # to prevent special Python, dotnet CLI, CMD, and other command-line processing, as the command
        # filters through batch files, the RUN tool, dotnet CLI, and finally gets to msbuild. To avoid
        # this, and make it simpler and hopefully more resilient to scripting changes, we create an
        # msbuild response file with the required text and pass the response file on to msbuild.

        without_categories_filename = os.path.join(fx_root, 'msbuild_commands.rsp')
        without_categories_string = '/p:WithoutCategories="IgnoreForCI;XsltcExeRequired"'
        with open(without_categories_filename, "w") as without_categories_file:
            without_categories_file.write(without_categories_string)
        without_categories = "-- @%s" % without_categories_filename

        log('Response file %s contents:' % without_categories_filename)
        log('%s' % without_categories_string)
        log('[end response file contents]')
    else:
        without_categories = '-- /p:WithoutCategories=IgnoreForCI'

    command = ' '.join((
        command,
        config_args,
        '-SkipTests' if no_run_tests else '',
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
        log('Error: exit code %s' % returncode)
        sys.exit(1)

    sys.exit(0)


##########################################################################
# setup for Main
##########################################################################

if __name__ == '__main__':
    Args = parser.parse_args(sys.argv[1:])

    main(Args)
