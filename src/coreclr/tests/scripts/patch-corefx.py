#!/usr/bin/env python
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
##########################################################################
##########################################################################
#
# Module: patch-corefx.py
#
# Notes:
#
# Script to overwrite the nuget downloaded corefx libraries with ones 
# built from a local enlistment.
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
    os.chmod(name, 0o651)
    os.remove(name)

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to patch CoreFx tests on the CoreCLR repo'

parser = argparse.ArgumentParser(description=description)

parser.add_argument('-arch', dest='arch', default='x64')
parser.add_argument('-build_type', dest='build_type', default='Debug')
parser.add_argument('-clr_core_root', dest='clr_core_root', default=None)
parser.add_argument('-fx_root', dest='fx_root', default=None)


##########################################################################
# Helper Functions
##########################################################################

def validate_args(args):
    """ Validate all of the arguments parsed.
    Args:
        args (argparser.ArgumentParser): Args parsed by the argument parser.
    Returns:
        (arch, build_type, clr_core_root, fx_root,)
            (str, str, str, str)
    Notes:
    If the arguments are valid then return them all in a tuple. If not, raise
    an exception stating x argument is incorrect.
    """

    arch = args.arch
    build_type = args.build_type
    clr_core_root = args.clr_core_root
    fx_root = args.fx_root

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

    if clr_core_root is None:
        raise Exception('No clr_core_root argument provided')
    else:
        clr_core_root = os.path.normpath(clr_core_root)
        validate_arg(clr_core_root, lambda item: os.path.isdir(clr_core_root))

    if fx_root is None:
        raise Exception('No fx_root argument provided')
    else:
        fx_root = os.path.normpath(fx_root)

    args = (arch, build_type, clr_core_root, fx_root)

    log('Configuration:')
    log(' arch: %s' % arch)
    log(' build_type: %s' % build_type)
    log(' clr_core_root: %s' % clr_core_root)
    log(' fx_root: %s' % fx_root)

    return args

def log(message):
    """ Print logging information
    Args:
        message (str): message to be printed
    """

    print('[%s]: %s' % (sys.argv[0], message))

def test_log(message):
    """ Print logging information only if testing mode is enabled
    Args:
        message (str): message to be printed
    """
    if testing:
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
    assert testing or os.path.isdir(target_dir)

    for source_filename in os.listdir(source_dir):
        source_pathname = os.path.join(source_dir, source_filename)
        if os.path.isfile(source_pathname):
            target_pathname = os.path.join(target_dir, source_filename)
            log('Copy: %s => %s' % (source_pathname, target_pathname))
            if not testing:
                shutil.copy2(source_pathname, target_pathname)

def patch_coreclr_root(core_root, fx_bin):
    """ Walk through the fx bin and patch corefx dlls to the core root.
    Args:
        core_root       (str):       the core root path
        fx_bin          (str):       the runtime folder from a corefx build
    Returns:
        nothing
    """
    test_log('Patching coreclr core_root')

    forbidden_names = ['coreclr.dll', 
                       'system.private.corelib.dll',
                       'r2rdump.dll',
                       'runincontext.dll',
                       'mscordaccore.dll',
                       'linuxnonjit.dll',
                       'protononjit.dll',
                       'mscordbi.dll',
                       'clrjit.dll',
                       'dbgshim.dll',
                       'coreshim.dll',
                       'clrgc.dll',
                       'superpmi-shim-counter.dll',
                       'clretwrc.dll',
                       'superpmi-shim-collector.dll',
                       'superpmi-shim-simple.dll',
                       'jitinterface.dll',
                       'mscorrc.debug.dll',
                       'mscorrc.dll',
                       'sos.dll']

    test_log('forbidden_names = %s' % forbidden_names)

    for file in os.listdir(fx_bin):
        test_log('considering file %s' % file)

        filename = os.path.basename(file)
        comparename = filename.lower()
        if (    comparename.endswith('.dll') and  
                comparename not in forbidden_names and
                not comparename.startswith('api-ms-core')   ):
            source_pathname = os.path.join(fx_bin, filename)
            target_pathname = os.path.join(core_root, filename)

            test_log ('copying file %s to file %s' % (source_pathname, target_pathname))

            shutil.copy2(source_pathname, target_pathname)

##########################################################################
# Main
##########################################################################

def main(args):
    """
        The way this script decides what to patch is by looking at the core
        root for a list of dlls, then filtering out any ones built by coreclr.
        This leaves us with a list of non-coreclr build dlls. Now we can use
        that list to go through the corefx repo and any ones that also exist
        in the corefx bin folder are copied over.
    """

    log('Patching CoreFX binaries from local enlistment.')

    arch, build_type, clr_core_root, fx_root = validate_args(args)

    clr_os = 'Windows_NT' if Is_windows else Unix_name_map[os.uname()[0]]

    if not os.path.exists(clr_core_root):
        raise Exception('Core root path %s does not exist.' % (clr_core_root))

    fx_bin = os.path.join(fx_root,
                          'artifacts',
                          'bin',
                          'runtime',
                          'netcoreapp-%s-%s-%s' % (clr_os, build_type, arch))

    if not os.path.exists(fx_bin):
        raise Exception('CoreFX bin path %s does not exist.' % (fx_bin))

    patch_coreclr_root(clr_core_root, fx_bin)


##########################################################################
# setup for Main
##########################################################################

if __name__ == '__main__':
    Args = parser.parse_args(sys.argv[1:])

    main(Args)
