#!/usr/bin/env python
################################################################################
################################################################################
#
# Module: runtest.py
#
# Notes:
#  
# Universal script to setup and run the xunit msbuild test runner.
#
# Use the instructions here:
#    https://github.com/dotnet/coreclr/blob/master/Documentation/building/windows-test-instructions.md 
#    https://github.com/dotnet/coreclr/blob/master/Documentation/building/unix-test-instructions.md
#
################################################################################
################################################################################

import argparse
import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile

from collections import defaultdict
from sys import platform as _platform

################################################################################
# Argument Parser
################################################################################

description = ("""Simple script that essentially sets up and runs either runtest.cmd
                  or runtests.sh. This wrapper is necessary to do all the setup work.

                  Note that this is required because there is not a unified test runner
                  for coreclr.""")

# Use either - or / to designate switches.
parser = argparse.ArgumentParser(description=description, prefix_chars='-/')

parser.add_argument("-arch", dest="arch", nargs='?', default="x64")
parser.add_argument("-build_type", dest="build_type", nargs='?', default="Debug")
parser.add_argument("-test_location", dest="test_location", nargs="?", default=None)
parser.add_argument("-core_root", dest="core_root", nargs='?', default=None)
parser.add_argument("-coreclr_repo_location", dest="coreclr_repo_location", default=os.getcwd())

# Only used on Unix
parser.add_argument("-test_native_bin_location", dest="test_native_bin_location", nargs='?', default=None)

################################################################################
# Helper Functions
################################################################################
   
def create_and_use_test_env(_os, env, func):
    """ Create a test env based on the env passed

    Args:
        _os(str)                        : OS name
        env(defaultdict(lambda: None))  : complus variables, key,value dict
        func(lambda)                    : lambda to call, after creating the 
                                        : test_env

    Notes:
        Using the env passed, create a temporary file to use as the
        test_env to be passed for runtest.cmd. Note that this only happens
        on windows, until xunit is used on unix there is no managed code run
        in runtest.sh.
    """

    complus_vars = defaultdict(lambda: None)

    for key in env:
        value = env[key]
        if "complus" in key.lower():
            complus_vars[key] = value

    if len(complus_vars.keys()) > 0:
        print "Found COMPlus variables in the current environment"
        print

        file_header = None

        if _os == "Windows_NT":
            file_header = \
"""@echo off
REM Temporary test env for test run.

"""
        else:
            file_header = \
"""# Temporary test env for test run.

"""

        with tempfile.NamedTemporaryFile() as test_env:
            with open(test_env.name, 'w') as file_handle:
                file_handle.write(file_header)
                
                for key in complus_vars:
                    value = complus_vars[key]
                    command = None
                    if _os == "Windows_NT":
                        command = "set"
                    else:
                        command = "export"

                    print "Unset %s" % key
                    os.environ[key] = ""

                    file_handle.write("%s %s=%s%s" % (command, key, value, os.linesep))

            contents = None
            with open(test_env.name) as file_handle:
                contents = file_handle.read()

            print
            print "TestEnv: %s" % test_env.name
            print 
            print "Contents:"
            print
            print contents
            print

            func(test_env.name)

    else:
        func(None)

def get_environment():
    """ Get all the COMPlus_* Environment variables
    
    Notes:
        Windows uses msbuild for its test runner. Therefore, all COMPlus
        variables will need to be captured as a test_env script and passed
        to runtest.cmd.
    """

    complus_vars = defaultdict(lambda: "")
    
    for key in os.environ:
        if "complus" in key.lower():
            complus_vars[key] = os.environ[key]
            os.environ[key] = ''
        elif "superpmi" in key.lower():
            complus_vars[key] = os.environ[key]
            os.environ[key] = ''

    return complus_vars

def call_msbuild(coreclr_repo_location,
                 msbuild_location,
                 host_os,
                 arch,
                 build_type, 
                 sequential=False):
    """ Call msbuild to run the tests built.

    Args:
        coreclr_repo_location(str)  : path to coreclr repo
        msbuild_location(str)       : path to msbuild
        sequential(bool)            : run sequentially if True

        host_os(str)                : os
        arch(str)                   : architecture
        build_type(str)             : configuration

    Notes:
        At this point the environment should be setup correctly, including
        the test_env, should it need to be passed.

    """

    common_msbuild_arguments = ["/nologo", "/nodeReuse:false", "/p:Platform=%s" % arch]

    if sequential:
        common_msbuild_arguments += ["/p:ParallelRun=false"]
    else:
        common_msbuild_arguments += ["/maxcpucount"]

    logs_dir = os.path.join(coreclr_repo_location, "bin", "Logs")
    if not os.path.isdir(logs_dir):
        os.makedirs(logs_dir)
    
    command =   [msbuild_location,
                os.path.join(coreclr_repo_location, "tests", "runtest.proj"),
                "/p:Runtests=true",
                "/clp:showcommandline"]

    log_path = os.path.join(logs_dir, "TestRunResults_%s_%s_%s" % (host_os, arch, build_type))
    build_log = log_path + ".log"
    wrn_log = log_path + ".wrn"
    err_log = log_path + ".err"

    msbuild_log_args = ["/fileloggerparameters:\"Verbosity=normal;LogFile=%s\"" % build_log,
                        "/fileloggerparameters1:\"WarningsOnly;LogFile=%s\"" % wrn_log,
                        "/fileloggerparameters2:\"ErrorsOnly;LogFile=%s\"" % err_log,
                        "/consoleloggerparameters:Summary",
                        "/verbosity:diag"]

    command += msbuild_log_args

    command += ["/p:__BuildOS=%s" % host_os,
                "/p:__BuildArch=%s" % arch,
                "/p:__BuildType=%s" % build_type,
                "/p:__LogsDir=%s" % logs_dir]

    if host_os != "Windows_NT":
        command = ["bash"] + command

    print " ".join(command)
    subprocess.check_output(command)

def copy_native_test_bin_to_core_root(host_os, path, core_root):
    """ Recursively copy all files to core_root
    
    Args:
        host_os(str)    : os
        path(str)       : native test bin location
        core_root(str)  : core_root location
    """
    assert os.path.isdir(path) or os.path.isfile(path)
    assert os.path.isdir(core_root)

    extension = "so" if host_os == "Linux" else "dylib"

    if os.path.isdir(path):
        for item in os.listdir(path):
            copy_native_test_bin_to_core_root(host_os, os.path.join(path, item), core_root)
    elif path.endswith(extension):
        print "cp -p %s %s" % (path, core_root)
        shutil.copy2(path, core_root) 

def run_tests(host_os,
              arch,
              build_type, 
              core_root,
              coreclr_repo_location, 
              test_location, 
              test_native_bin_location, 
              test_env=None,
              is_long_gc=False,
              is_gcsimulator=False,
              is_jitdasm=False,
              is_ilasm=False,
              run_sequential=False):
    """ Run the coreclr tests
    
    Args:
        host_os(str)                : os
        arch(str)                   : arch
        build_type(str)             : configuration
        coreclr_repo_location(str)  : path to the root of the repo
        core_root(str)              : Core_Root path
        test_location(str)          : Test bin, location
        test_native_bin_location    : Native test components, None and windows.
        test_env(str)               : path to the test_env to be used
    """

    # Copy all the native libs to core_root
    if host_os != "Windows_NT":
        copy_native_test_bin_to_core_root(host_os, os.path.join(test_native_bin_location, "src"), core_root)

    # Setup the msbuild location
    msbuild_location = os.path.join(coreclr_repo_location, "Tools", "msbuild.%s" % ("cmd" if host_os == "Windows_NT" else "sh"))

    # Setup the environment
    if is_long_gc:
        print "Running Long GC Tests, extending timeout to 20 minutes."
        os.environ["__TestTimeout"] = "1200000" # 1,200,000
        os.environ["RunningLongGCTests"] = "1"
    
    if is_gcsimulator:
        print "Running GCSimulator tests, extending timeout to one hour."
        os.environ["__TestTimeout"] = "3600000" # 3,600,000
        os.environ["RunningGCSimulatorTests"] = "1"

    if is_jitdasm:
        print "Running jit disasm on framework and test assemblies."
        os.environ["RunningJitDisasm"] = "1"

    if is_ilasm:
        print "Running ILasm round trip."
        os.environ["RunningIlasmRoundTrip"] = "1"

    # Set Core_Root
    os.environ["CORE_ROOT"] = core_root

    # Call msbuild.
    call_msbuild(coreclr_repo_location,
                 msbuild_location,
                 host_os,
                 arch,
                 build_type,
                 sequential=run_sequential)


def setup_args(args):
    """ Setup the args based on the argparser obj

    Args:
        args(ArgParser): Parsed arguments

    Notes:
        If there is no core_root, or test location passed, create a default
        location using the build type and the arch.
    """

    host_os = None
    arch = args.arch
    build_type = args.build_type

    test_location = args.test_location
    core_root = args.core_root
    test_native_bin_location = args.test_native_bin_location

    coreclr_repo_location = args.coreclr_repo_location
    if os.path.basename(coreclr_repo_location) == "tests":
        coreclr_repo_location = os.path.dirname(coreclr_repo_location)
   
    if _platform == "linux" or _platform == "linux2":
        host_os = "Linux"
    elif _platform == "darwin":
        host_os = "OSX"
    elif _platform == "win32":
        host_os = "Windows_NT"
    else:
        print "Unknown OS: %s" % host_os
        sys.exit(1)

    assert os.path.isdir(coreclr_repo_location)

    if test_location is None:
        print "Using default test location."
        test_location = os.path.join(coreclr_repo_location, "bin", "tests", "%s.%s.%s" % (host_os, arch, build_type))
        print "TestLocation: %s" % test_location
        print

    if core_root is None:
        print "Using default location for core_root."
        core_root = os.path.join(test_location, "Tests", "Core_Root")
        print "Core_Root: %s" % core_root
        print

    if host_os != "Windows_NT":
        if test_native_bin_location is None:
            print "Using default location for test_native_bin_location."
            test_native_bin_location = os.path.join(os.path.join(coreclr_repo_location, "bin", "obj", "%s.%s.%s" % (host_os, arch, build_type), "tests"))
            print "Native bin location: %s" % test_native_bin_location
            print

    valid_arches = ["x64", "x86", "arm", "arm64"]
    if not arch in valid_arches:
        print "Unsupported architecture: %s." % arch
        print "Supported architectures: %s" % "[%s]" % ", ".join(valid_arches)
        sys.exit(1)

    valid_build_types = ["Debug", "Checked", "Release"]
    if not build_type in valid_build_types:
        print "Unsupported configuration: %s." % build_type
        print "Supported configurations: %s" % "[%s]" % ", ".join(valid_build_types)
        sys.exit(1)

    if not os.path.isdir(test_location):
        print "Error, test location: %s, does not exist." % test_location
        sys.exit(1)
    
    if not os.path.isdir(core_root):
        print "Error, core_root: %s, does not exist." % core_root
        sys.exit(1)

    if host_os != "Windows_NT":
        if not os.path.isdir(test_native_bin_location):
            print "Error, test_native_bin_location: %s, does not exist." % test_native_bin_location
            sys.exit(1)

    return host_os, arch, build_type, coreclr_repo_location, core_root, test_location, test_native_bin_location

def setup_tools(host_os, coreclr_repo_location):
    """ Setup the tools for the repo

    Args:
        host_os(str)                : os
        coreclr_repo_location(str)  : path to coreclr repo

    """

    # Is the tools dir setup
    setup = False
    tools_dir = os.path.join(coreclr_repo_location, "Tools")

    is_windows = host_os == "Windows_NT"

    if os.path.isfile(os.path.join(tools_dir, "msbuild.%s" % ("cmd" if is_windows else "sh"))):
        setup = True
    
    # init the tools for the repo
    if not setup:
        command = None
        if is_windows:
            command = [os.path.join(coreclr_repo_location, "init_tools.cmd")]
        else:
            command = ["sh", os.path.join(coreclr_repo_location, "init_tools.sh")]

        print " ".join(command)
        subprocess.check_output(command)
    
        setup = True

    return setup

################################################################################
# Main
################################################################################

def main(args):
    host_os, arch, build_type, coreclr_repo_location, core_root, test_location, test_native_bin_location = setup_args(args)

    # Setup the tools for the repo.
    setup_tools(host_os, coreclr_repo_location)

    env = get_environment()
    ret_code = create_and_use_test_env(host_os, 
                                       env, 
                                       lambda path: run_tests(host_os, 
                                                              arch,
                                                              build_type,
                                                              core_root, 
                                                              coreclr_repo_location,
                                                              test_location, 
                                                              test_native_bin_location, 
                                                              test_env=path))

################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))