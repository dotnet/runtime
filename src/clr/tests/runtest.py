#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
#
##
# Title               :runtest.py
#
# Notes:
#  
# Universal script to setup and run the xunit console runner. The script relies 
# on runtest.proj and the bash and batch wrappers. All test excludes will also 
# come from issues.targets. If there is a jit stress or gc stress exclude, 
# please add GCStressIncompatible or JitOptimizationSensitive to the test's 
# ilproj or csproj.
#
# The xunit runner currently relies on tests being built on the same host as the
# target platform. This requires all tests run on linux x64 to be built by the
# same platform and arch. If this is not done, the tests will run correctly;
# however, expect failures due to incorrect exclusions in the xunit 
# wrappers setup at build time.
#
# Note that for linux targets the native components to the tests are still built
# by the product build. This requires all native components to be either copied
# into the Core_Root directory or the test's managed directory. The latter is
# prone to failure; however, copying into the Core_Root directory may create
# naming conflicts.
#
# If you are running tests on a different target than the host that built, the
# native tests components must be copied from:
# bin/obj/<Host>.<Arch>.<BuildType/tests to the target. If the location is not
# standard please pass the -test_native_bin_location flag to the script.
#
# Use the instructions here:
#    https://github.com/dotnet/coreclr/blob/master/Documentation/building/windows-test-instructions.md 
#    https://github.com/dotnet/coreclr/blob/master/Documentation/building/unix-test-instructions.md
#
################################################################################
################################################################################

import argparse
import datetime
import json
import math
import os
import platform
import shutil
import subprocess
import sys
import tempfile
import time
import re
import string

import xml.etree.ElementTree

from collections import defaultdict
from sys import platform as _platform

################################################################################
# Argument Parser
################################################################################

description = ("""Universal script to setup and run the xunit console runner. The script relies 
on runtest.proj and the bash and batch wrappers. All test excludes will also 
come from issues.targets. If there is a jit stress or gc stress exclude, 
please add GCStressIncompatible or JitOptimizationSensitive to the test's 
ilproj or csproj.

The xunit runner currently relies on tests being built on the same host as the
target platform. This requires all tests run on linux x64 to be built by the
same platform and arch. If this is not done, the tests will run correctly;
however, expect failures due to incorrect exclusions in the xunit 
wrappers setup at build time.

Note that for linux targets the native components to the tests are still built
by the product build. This requires all native components to be either copied
into the Core_Root directory or the test's managed directory. The latter is
prone to failure; however, copying into the Core_Root directory may create
naming conflicts.

If you are running tests on a different target than the host that built, the
native tests components must be copied from:
bin/obj/<Host>.<Arch>.<BuildType/tests to the target. If the location is not
standard please pass the -test_native_bin_location flag to the script.""")

parser = argparse.ArgumentParser(description=description)

parser.add_argument("-arch", dest="arch", nargs='?', default="x64")
parser.add_argument("-build_type", dest="build_type", nargs='?', default="Debug")
parser.add_argument("-test_location", dest="test_location", nargs="?", default=None)
parser.add_argument("-core_root", dest="core_root", nargs='?', default=None)
parser.add_argument("-product_location", dest="product_location", nargs='?', default=None)
parser.add_argument("-coreclr_repo_location", dest="coreclr_repo_location", default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
parser.add_argument("-test_env", dest="test_env", default=None)
parser.add_argument("-crossgen_altjit", dest="crossgen_altjit", default=None)

# Optional arguments which change execution.

# Rid is used only for restoring packages. This is a unspecified and undocumented
# environment variable that needs to be passed to build.proj. Do not use this
# unless you are attempting to target package restoration for another host/arch/os
parser.add_argument("-rid", dest="rid", nargs="?", default=None)

parser.add_argument("--il_link", dest="il_link", action="store_true", default=False)
parser.add_argument("--long_gc", dest="long_gc", action="store_true", default=False)
parser.add_argument("--gcsimulator", dest="gcsimulator", action="store_true", default=False)
parser.add_argument("--jitdisasm", dest="jitdisasm", action="store_true", default=False)
parser.add_argument("--ilasmroundtrip", dest="ilasmroundtrip", action="store_true", default=False)
parser.add_argument("--run_crossgen_tests", dest="run_crossgen_tests", action="store_true", default=False)
parser.add_argument("--precompile_core_root", dest="precompile_core_root", action="store_true", default=False)
parser.add_argument("--sequential", dest="sequential", action="store_true", default=False)

parser.add_argument("--build_xunit_test_wrappers", dest="build_test_wrappers", action="store_true", default=False)
parser.add_argument("--generate_layout", dest="generate_layout", action="store_true", default=False)
parser.add_argument("--generate_layout_only", dest="generate_layout_only", action="store_true", default=False)
parser.add_argument("--analyze_results_only", dest="analyze_results_only", action="store_true", default=False)
parser.add_argument("--verbose", dest="verbose", action="store_true", default=False)

# Only used on Unix
parser.add_argument("-test_native_bin_location", dest="test_native_bin_location", nargs='?', default=None)

################################################################################
# Globals
################################################################################

g_verbose = False
gc_stress_c = False
gc_stress = False
file_name_cache = defaultdict(lambda: None)

################################################################################
# Classes
################################################################################

class DebugEnv:
    def __init__(self, 
                 host_os, 
                 arch, 
                 build_type, 
                 env, 
                 core_root,
                 coreclr_repo_location, 
                 test):
        """ Go through the failing tests and create repros for them

        Args:
            host_os (String)        : os
            arch (String)           : architecture
            build_type (String)     : build configuration (debug, checked, release)
            env                     : env for the repro
            core_root (String)      : Core_Root path
            coreclr_repo_location   : coreclr repo location
            test ({})               : The test metadata
        
        """
        self.unique_name = "%s_%s_%s_%s" % (test["name"],
                                            host_os,
                                            arch,
                                            build_type)

        self.host_os = host_os
        self.arch = arch
        self.build_type = build_type
        self.env = env
        self.core_root = core_root
        self.test = test
        self.test_location = test["test_path"]
        self.coreclr_repo_location = coreclr_repo_location

        self.__create_repro_wrapper__()

        self.path = None
        
        if self.host_os == "Windows_NT":
            self.path = self.unique_name + ".cmd"
        else:
            self.path = self.unique_name + ".sh"

        repro_location = os.path.join(coreclr_repo_location, "bin", "repro", "%s.%s.%s" % (self.host_os, arch, build_type))
        assert os.path.isdir(repro_location)

        self.repro_location = repro_location

        self.path = os.path.join(repro_location, self.path)
        
        exe_location = os.path.splitext(self.test_location)[0] + ".exe"
        if os.path.isfile(exe_location):
            self.exe_location = exe_location
            self.__add_configuration_to_launch_json__()

    def __add_configuration_to_launch_json__(self):
        """ Add to or create a launch.json with debug information for the test

        Notes:
            This will allow debugging using the cpp extension in vscode.
        """

        repro_location = self.repro_location
        assert os.path.isdir(repro_location)

        vscode_dir = os.path.join(repro_location, ".vscode")
        if not os.path.isdir(vscode_dir):
            os.mkdir(vscode_dir)

        assert os.path.isdir(vscode_dir)

        launch_json_location = os.path.join(vscode_dir, "launch.json")
        if not os.path.isfile(launch_json_location):
            initial_json = {
                "version": "0.2.0",
                "configurations": []
            }

            json_str = json.dumps(initial_json, 
                                  indent=4, 
                                  separators=(',', ': '))

            with open(launch_json_location, 'w') as file_handle:
                file_handle.write(json_str)

        launch_json = None
        with open(launch_json_location) as file_handle:
            launch_json = file_handle.read()
        
        launch_json = json.loads(launch_json)

        configurations = launch_json["configurations"]

        dbg_type = "cppvsdbg" if self.host_os == "Windows_NT" else ""
        core_run = os.path.join(self.core_root, "corerun")

        env = {
            "COMPlus_AssertOnNYI": "1",
            "COMPlus_ContinueOnAssert": "0"
        }

        if self.env is not None:
            # Convert self.env to a defaultdict
            self.env = defaultdict(lambda: None, self.env)
            for key, value in env.items():
                self.env[key] = value
            
        else:
            self.env = env

        environment = []
        for key, value in self.env.items():
            env = {
                "name": key,
                "value": value
            }

            environment.append(env)

        configuration = defaultdict(lambda: None, {
            "name": self.unique_name,
            "type": dbg_type,
            "request": "launch",
            "program": core_run,
            "args": [self.exe_location],
            "stopAtEntry": False,
            "cwd": os.path.join("${workspaceFolder}", "..", ".."),
            "environment": environment,
            "externalConsole": True
        })

        if self.build_type.lower() != "release":
            symbol_path = os.path.join(self.core_root, "PDB")
            configuration["symbolSearchPath"] = symbol_path

        # Update configuration if it already exists.
        config_exists = False
        for index, config in enumerate(configurations):
            if config["name"] == self.unique_name:
                configurations[index] = configuration
                config_exists = True

        if not config_exists:
            configurations.append(configuration)
        json_str = json.dumps(launch_json,
                              indent=4, 
                              separators=(',', ': '))

        with open(launch_json_location, 'w') as file_handle:
            file_handle.write(json_str)

    def __create_repro_wrapper__(self):
        """ Create the repro wrapper
        """

        if self.host_os == "Windows_NT":
            self.__create_batch_wrapper__()
        else:
            self.__create_bash_wrapper__()

    def __create_batch_wrapper__(self):
        """ Create a windows batch wrapper
        """
    
        wrapper = \
"""@echo off
REM ============================================================================
REM Repro environment for %s
REM 
REM Notes:
REM 
REM This wrapper is automatically generated by runtest.py. It includes the
REM necessary environment to reproduce a failure that occured during running
REM the tests.
REM
REM In order to change how this wrapper is generated, see
REM runtest.py:__create_batch_wrapper__(). Please note that it is possible
REM to recreate this file by running tests/runtest.py --analyze_results_only
REM with the appropriate environment set and the correct arch and build_type
REM passed.
REM
REM ============================================================================

REM Set Core_Root if it has not been already set.
if "%%CORE_ROOT%%"=="" set CORE_ROOT=%s

echo Core_Root is set to: "%%CORE_ROOT%%"

""" % (self.unique_name, self.core_root)

        line_sep = os.linesep

        if self.env is not None:
            for key, value in self.env.items():
                wrapper += "echo set %s=%s%s" % (key, value, line_sep)
                wrapper += "set %s=%s%s" % (key, value, line_sep)

        wrapper += "%s" % line_sep
        wrapper += "echo call %s%s" % (self.test_location, line_sep) 
        wrapper += "call %s%s" % (self.test_location, line_sep) 

        self.wrapper = wrapper
    
    def __create_bash_wrapper__(self):
        """ Create a unix bash wrapper
        """
    
        wrapper = \
"""
#============================================================================
# Repro environment for %s
# 
# Notes:
#
# This wrapper is automatically generated by runtest.py. It includes the
# necessary environment to reproduce a failure that occured during running
# the tests.
#
# In order to change how this wrapper is generated, see
# runtest.py:__create_bash_wrapper__(). Please note that it is possible
# to recreate this file by running tests/runtest.py --analyze_results_only
# with the appropriate environment set and the correct arch and build_type
# passed.
#
# ============================================================================

# Set Core_Root if it has not been already set.
if [ \"${CORE_ROOT}\" = \"\" ] || [ ! -z \"${CORE_ROOT}\" ]; then 
    export CORE_ROOT=%s 
else 
    echo \"CORE_ROOT set to ${CORE_ROOT}\"
fi

""" % (self.unique_name, self.core_root)

        line_sep = os.linesep

        if self.env is not None:
            for key, value in self.env.items():
                wrapper += "echo export %s=%s%s" % (key, value, line_sep)
                wrapper += "export %s=%s%s" % (key, value, line_sep)

        wrapper += "%s" % line_sep
        wrapper += "echo bash %s%s" % (self.test_location, line_sep) 
        wrapper += "bash %s%s" % (self.test_location, line_sep) 

        self.wrapper = wrapper

    def write_repro(self):
        """ Write out the wrapper

        Notes:
            This will check if the wrapper repros or not. If it does not repro
            it will be put into an "unstable" folder under bin/repro.
            Else it will just be written out.

        """

        with open(self.path, 'w') as file_handle:
            file_handle.write(self.wrapper)


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
    global gc_stress_c

    complus_vars = defaultdict(lambda: None)

    for key in env:
        value = env[key]
        if "complus" in key.lower():
            complus_vars[key] = value

    if len(list(complus_vars.keys())) > 0:
        print("Found COMPlus variables in the current environment")
        print()

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

        contents = ""

        with tempfile.NamedTemporaryFile(mode="w") as test_env:
            test_env.write(file_header)
            contents += file_header
            
            for key in complus_vars:
                value = complus_vars[key]
                command = None
                if _os == "Windows_NT":
                    command = "set"
                else:
                    command = "export"

                print("Unset %s" % key)
                if key.lower() == "complus_gcstress" and "c" in value.lower():
                    gc_stress_c = True

                if key.lower() == "complus_gcstress":
                    gc_stress = True

                os.environ[key] = ""

                line = "%s %s=%s%s" % (command, key, value, os.linesep)
                test_env.write(line)
                contents += line

            print()
            print("TestEnv: %s" % test_env.name)
            print() 
            print("Contents:")
            print()
            print(contents)
            print()

            return func(test_env.name)

    else:
        return func(None)

def get_environment(test_env=None):
    """ Get all the COMPlus_* Environment variables
    
    Notes:
        All COMPlus variables need to be captured as a test_env script to avoid
        influencing the test runner.
    """
    global gc_stress_c

    complus_vars = defaultdict(lambda: "")
    
    for key in os.environ:
        if "complus" in key.lower():
            complus_vars[key] = os.environ[key]
            os.environ[key] = ''
        elif "superpmi" in key.lower():
            complus_vars[key] = os.environ[key]
            os.environ[key] = ''

    # Get the env from the test_env
    if test_env is not None:
        with open(test_env) as file_handle:
            for item in file_handle.readlines():
                key_split = item.split("=")

                if len(key_split) == 1:
                    continue

                key = key_split[0]
                value = key_split[1]

                key = key.split(" ")[-1]
                value = value.strip()

                try:
                    value = value.split(" ")[0]
                except:
                    pass

                complus_vars[key] = value

                # Supoort looking up case insensitive.
                complus_vars[key.lower()] = value

        if "complus_gcstress" in complus_vars:
            gc_stress = True

        if "c" in complus_vars["COMPlus_GCStress"].lower():
            gc_stress_c = True

    return complus_vars

def call_msbuild(coreclr_repo_location,
                 dotnetcli_location,
                 host_os,
                 arch,
                 build_type, 
                 is_illink=False,
                 sequential=False):
    """ Call msbuild to run the tests built.

    Args:
        coreclr_repo_location(str)  : path to coreclr repo
        dotnetcli_location(str)     : path to the dotnet cli in the tools dir
        sequential(bool)            : run sequentially if True

        host_os(str)                : os
        arch(str)                   : architecture
        build_type(str)             : configuration

    Notes:
        At this point the environment should be setup correctly, including
        the test_env, should it need to be passed.

    """
    global g_verbose

    common_msbuild_arguments = ["/nologo", "/nodeReuse:false", "/p:Platform=%s" % arch]

    if sequential:
        common_msbuild_arguments += ["/p:ParallelRun=false"]
    else:
        common_msbuild_arguments += ["/maxcpucount"]

    logs_dir = os.path.join(coreclr_repo_location, "bin", "Logs")
    if not os.path.isdir(logs_dir):
        os.makedirs(logs_dir)
    
    command =   [dotnetcli_location,
                 "msbuild",
                 os.path.join(coreclr_repo_location, "tests", "runtest.proj"),
                 "/p:Runtests=true",
                 "/clp:showcommandline"]

    if is_illink:
        command += ["/p:RunTestsViaIllink=true"]

    log_path = os.path.join(logs_dir, "TestRunResults_%s_%s_%s" % (host_os, arch, build_type))
    build_log = log_path + ".log"
    wrn_log = log_path + ".wrn"
    err_log = log_path + ".err"

    msbuild_log_args = ["/fileloggerparameters:\"Verbosity=normal;LogFile=%s\"" % build_log,
                        "/fileloggerparameters1:\"WarningsOnly;LogFile=%s\"" % wrn_log,
                        "/fileloggerparameters2:\"ErrorsOnly;LogFile=%s\"" % err_log,
                        "/consoleloggerparameters:Summary"]

    if g_verbose:
        msbuild_log_args += ["/verbosity:diag"]

    command += msbuild_log_args

    command += ["/p:__BuildOS=%s" % host_os,
                "/p:__BuildArch=%s" % arch,
                "/p:__BuildType=%s" % build_type,
                "/p:__LogsDir=%s" % logs_dir]

    print(" ".join(command))
    proc = subprocess.Popen(command)

    try:
        proc.communicate()
    except:
        proc.kill()
        sys.exit(1)

    return proc.returncode

def running_in_ci():
    """ Check if running in ci

    Returns:
        bool
    """

    is_ci = False

    try:
        jenkins_build_number = os.environ["BUILD_NUMBER"]

        is_ci = True
    except:
        pass

    return is_ci

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
        print("cp -p %s %s" % (path, core_root))
        shutil.copy2(path, core_root)

def correct_line_endings(host_os, test_location, root=True):
    """ Recursively correct all .sh/.cmd files to the correct line ending

    Args:
        host_os(str)        : os
        test_location(str)  : location of the tests
    """
    if root:
        print("Correcting line endings...")

    assert os.path.isdir(test_location) or os.path.isfile(test_location)

    extension = "cmd" if host_os == "Windows_NT" else ".sh"
    incorrect_line_ending = '\n' if host_os == "Windows_NT" else '\r\n'
    correct_line_ending = os.linesep

    if os.path.isdir(test_location):
        for item in os.listdir(test_location):
            correct_line_endings(host_os, os.path.join(test_location, item), False)
    elif test_location.endswith(extension):
        content = None
        with open(test_location) as file_handle:
            content = file_handle.read()
        
        assert content != None
        subbed_content = content.replace(incorrect_line_ending, correct_line_ending)

        if content != subbed_content:
            with open(test_location, 'w') as file_handle:
                file_handle.write(subbed_content)

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
              is_illink=False,
              run_crossgen_tests=False,
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
    global gc_stress
    
    # Setup the dotnetcli location
    dotnetcli_location = os.path.join(coreclr_repo_location, "Tools", "dotnetcli", "dotnet%s" % (".exe" if host_os == "Windows_NT" else ""))

    # Default timeout for unix is 15 minutes
    os.environ["__TestTimeout"] = str(15*60*1000) # 900,000 ms

    # Setup the environment
    if is_long_gc:
        print("Running Long GC Tests, extending timeout to 20 minutes.")
        os.environ["__TestTimeout"] = str(20*60*1000) # 1,200,000 ms
        os.environ["RunningLongGCTests"] = "1"
    
    if is_gcsimulator:
        print("Running GCSimulator tests, extending timeout to one hour.")
        os.environ["__TestTimeout"] = str(60*60*1000) # 3,600,000 ms
        os.environ["RunningGCSimulatorTests"] = "1"

    if is_jitdasm:
        print("Running jit disasm and tests.")
        os.environ["RunningJitDisasm"] = "1"

    if is_ilasm:
        print("Running ILasm round trip.")
        os.environ["RunningIlasmRoundTrip"] = "1"

    if run_crossgen_tests:
        print("Running tests R2R")
        os.environ["RunCrossGen"] = "true"

    if gc_stress:
        print("Running GCStress, extending timeout to 120 minutes.")
        os.environ["__TestTimeout"] = str(120*60*1000) # 1,800,000 ms

    # Set Core_Root
    os.environ["CORE_ROOT"] = core_root

    # Set test env if exists
    if test_env is not None:
        os.environ["__TestEnv"] = test_env

    # Call msbuild.
    return call_msbuild(coreclr_repo_location,
                        dotnetcli_location,
                        host_os,
                        arch,
                        build_type,
                        is_illink=is_illink,
                        sequential=run_sequential)

def setup_args(args):
    """ Setup the args based on the argparser obj

    Args:
        args(ArgParser): Parsed arguments

    Notes:
        If there is no core_root, or test location passed, create a default
        location using the build type and the arch.
    """

    if args.generate_layout_only:
        args.generate_layout = True

    host_os = None
    arch = args.arch.lower()
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
        print("Unknown OS: %s" % host_os)
        sys.exit(1)

    assert os.path.isdir(coreclr_repo_location)

    valid_arches = ["x64", "x86", "arm", "arm64"]
    if not arch in valid_arches:
        print("Unsupported architecture: %s." % arch)
        print("Supported architectures: %s" % "[%s]" % ", ".join(valid_arches))
        sys.exit(1)

    def check_build_type(build_type):
        valid_build_types = ["Debug", "Checked", "Release"]

        if build_type != None and len(build_type) > 0:
            # Force the build type to be capitalized
            build_type = build_type.capitalize()

        if not build_type in valid_build_types:
            print("Unsupported configuration: %s." % build_type)
            print("Supported configurations: %s" % "[%s]" % ", ".join(valid_build_types))
            sys.exit(1)

        return build_type

    build_type = check_build_type(build_type)

    if test_location is None:
        default_test_location = os.path.join(coreclr_repo_location, "bin", "tests", "%s.%s.%s" % (host_os, arch, build_type))
        
        if os.path.isdir(default_test_location):
            test_location = default_test_location

            print("Using default test location.")
            print("TestLocation: %s" % default_test_location)
            print()

        else:
            # The tests for the default location have not been built.
            print("Error, unable to find the tests at %s" % default_test_location)

            suggested_location = None
            possible_test_locations = [item for item in os.listdir(os.path.join(coreclr_repo_location, "bin", "tests")) if host_os in item and arch in item]
            if len(possible_test_locations) > 0:
                print("Tests are built for the following:")
                for item in possible_test_locations:
                    print(item.replace(".", " "))
                
                print("Please run runtest.py again with the correct build-type by passing -build_type")
            else:
                print("No tests have been built for this host and arch. Please run build-test.%s" % ("cmd" if host_os == "Windows_NT" else "sh"))
            
            sys.exit(1)
    else:
        # If we have supplied our own test location then we need to create a test location
        # that the scripting will expect. As it is now, there is a dependency on the
        # test location being under test/<os>.<build_type>.<arch>

        # Make sure that we are using the correct build_type. This is a test drop, it is possible
        # that we are inferring the build type to be Debug incorrectly.

        if build_type not in test_location:
            # Remove punctuation
            corrected_build_type = re.sub("[%s]" % string.punctuation, "", test_location.split(".")[-1])
            build_type = check_build_type(corrected_build_type)

        default_test_location = os.path.join(coreclr_repo_location, "bin", "tests", "%s.%s.%s" % (host_os, arch, build_type))

        # Remove optional end os.path.sep
        if test_location[-1] == os.path.sep:
            test_location = test_location[:-1]

        if test_location != default_test_location and os.path.isdir(default_test_location):
            # Remove the existing directory if there is one.
            shutil.rmtree(default_test_location)

            print("Non-standard test location being used.")
            print("Overwrite the standard location with these tests.")
            print("TODO: Change runtest.proj to allow running from non-standard test location.")
            print("")

            print("cp -r %s %s" % (test_location, default_test_location))
            shutil.copytree(test_location, default_test_location)

            test_location = default_test_location

            # unset core_root so it can be put in the default location
            core_root = None

            # Force the core_root to be setup again.
            args.generate_layout = True

        else:
            test_location = default_test_location

            print("Using default test location.")
            print("TestLocation: %s" % default_test_location)
            print()

    if core_root is None:
        default_core_root = os.path.join(test_location, "Tests", "Core_Root")

        if os.path.isdir(default_core_root):
            core_root = default_core_root

            print("Using default location for core_root.")
            print("Core_Root: %s" % core_root)
            print()

        elif args.generate_layout is False:
            # CORE_ROOT has not been setup correctly.
            print("Error, unable to find CORE_ROOT at %s" % default_core_root)
            print("Please run runtest.py with --generate_layout specified.")

            sys.exit(1)

        else:
            print("--generate_layout passed. Core_Root will be populated at: %s" % default_core_root)
            core_root = default_core_root
    else:
        print("Core_Root: %s" % core_root)

    if host_os != "Windows_NT":
        if test_native_bin_location is None:
            print("Using default location for test_native_bin_location.")
            test_native_bin_location = os.path.join(os.path.join(coreclr_repo_location, "bin", "obj", "%s.%s.%s" % (host_os, arch, build_type), "tests"))
            print("Native bin location: %s" % test_native_bin_location)
            print()
            
        if not os.path.isdir(test_native_bin_location):
            print("Error, test_native_bin_location: %s, does not exist." % test_native_bin_location)
            sys.exit(1)

    if args.product_location is None and args.generate_layout:
        product_location = os.path.join(coreclr_repo_location, "bin", "Product", "%s.%s.%s" % (host_os, arch, build_type))
        if not os.path.isdir(product_location):
            print("Error, unable to determine the product location. This is most likely because build_type was")
            print("incorrectly passed. Or the product is not built. Please explicitely pass -product_location")

            sys.exit(1)

    else:
        product_location = args.product_location

    return host_os, arch, build_type, coreclr_repo_location, product_location, core_root, test_location, test_native_bin_location

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

    dotnetcli_location = os.path.join(coreclr_repo_location, "Tools", "dotnetcli", "dotnet%s" % (".exe" if host_os == "Windows_NT" else ""))

    if os.path.isfile(dotnetcli_location):
        setup = True
    
    # init the tools for the repo
    if not setup:
        command = None
        if is_windows:
            command = [os.path.join(coreclr_repo_location, "init-tools.cmd")]
        else:
            command = ["bash", os.path.join(coreclr_repo_location, "init-tools.sh")]

        print(" ".join(command))
        subprocess.check_output(command)
    
        setup = True

    return setup

def setup_coredis_tools(coreclr_repo_location, host_os, arch, core_root):
    """ Setup CoreDisTools if needed

    Args:
        coreclr_repo_location(str)  : coreclr repo location
        host_os(str)                : os
        arch(str)                   : arch
        core_root(str)              : core_root
    """

    test_location = os.path.join(coreclr_repo_location, "tests")

    def is_coredis_tools_supported(host_os, arch):
        """ Is coredis tools supported on this os/arch

        Args:
            host_os(str): os
            arch(str)   : arch

        """
        unsupported_unix_arches = ["arm", "arm64"]

        if host_os.lower() == "osx":
            return False
        
        return True

        if host_os != "Windows_NT" and arch in unsupported_unix_arches:
            return False

        return True

    if is_coredis_tools_supported(host_os, arch):
        command = None
        if host_os == "Windows_NT":
            command = [os.path.join(test_location, "setup-stress-dependencies.cmd"), "/arch", arch, "/outputdir", core_root]
        else:
            command = [os.path.join(test_location, "setup-stress-dependencies.sh"), "--outputDir=%s" % core_root]

        proc = subprocess.Popen(command)
        proc.communicate()

        if proc.returncode != 0:
            print("setup_stress_dependencies.sh failed.")
            sys.exit(1)
    else:
        print("GCStress C is not supported on your platform.")
        sys.exit(1)

def precompile_core_root(test_location,
                         host_os,
                         arch,
                         core_root, 
                         use_jit_disasm=False, 
                         altjit_name=False):
    """ Precompile all of the assemblies in the core_root directory

    Args:
        test_location(str)      : test location
        host_os(str)            : os
        core_root(str)          : location of core_root
        use_jit_disasm(Bool)    : use jit disasm
        altjit_name(str)        : name of the altjit

    """

    skip_list = [
        ".*xunit.*",
        ".*api-ms-win-core.*",
        ".*api-ms-win.*",
        ".*System.Private.CoreLib.*"
    ]

    unix_skip_list = [
        ".*mscorlib.*",
        ".*System.Runtime.WindowsRuntime.*",
        ".*System.Runtime.WindowsRuntime.UI.Xaml.*",
        ".*R2RDump.dll.*"
    ]

    arm64_unix_skip_list = [
        ".*Microsoft.CodeAnalysis.VisualBasic.*",
        ".*System.Net.NameResolution.*",
        ".*System.Net.Sockets.*",
        ".*System.Net.Primitives.*"
    ]

    if host_os != "Windows_NT":
        skip_list += unix_skip_list
    
        if arch == "arm64":
            skip_list += arm64_unix_skip_list

    assert os.path.isdir(test_location)
    assert os.path.isdir(core_root)

    crossgen = os.path.join(core_root, "crossgen%s" % (".exe" if host_os == "Windows_NT" else ""))
    assert os.path.isfile(crossgen)

    def call_crossgen(file, env):
        assert os.path.isfile(crossgen)
        command = [crossgen, "/Platform_Assemblies_Paths", core_root, file]

        if use_jit_disasm:
            core_run = os.path.join(core_root, "corerun%s" % (".exe" if host_os == "Windows_NT" else ""))
            assert os.path.isfile(core_run)

            command = [core_run, 
                       os.path.join(core_root, "jit-dasm.dll"),
                       "--crossgen",
                       crossgen,
                       "--platform", 
                       core_root, 
                       "--output",
                       os.path.join(test_location, "dasm"),
                       file]

        proc = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env)
        proc.communicate()

        return_code = proc.returncode

        passed = False
        if return_code == -2146230517:
            print("%s is not a managed assembly." % file)
            return passed

        if return_code != 0:
            print("Unable to precompile %s" % file)
            return passed

        print("Successfully precompiled %s" % file)
        passed = True

        return passed

    print("Precompiling all assemblies in %s" % core_root)
    print()

    env = os.environ.copy()

    if not altjit_name is None:
        env["COMPlus_AltJit"]="*"
        env["COMPlus_AltJitNgen"]="*"
        env["COMPlus_AltJitName"]=altjit_name
        env["COMPlus_AltJitAssertOnNYI"]="1"
        env["COMPlus_NoGuiOnAssert"]="1"
        env["COMPlus_ContinueOnAssert"]="0"

    dlls = [os.path.join(core_root, item) for item in os.listdir(core_root) if item.endswith("dll") and "mscorlib" not in item]

    def in_skip_list(item):
        found = False
        for skip_re in skip_list: 
            if re.match(skip_re, item.lower()) is not None: 
                found = True
        return found

    dlls = [dll for dll in dlls if not in_skip_list(dll)]

    for dll in dlls:
        call_crossgen(dll, env)

    print()

def setup_core_root(host_os, 
                    arch, 
                    build_type, 
                    coreclr_repo_location, 
                    test_native_bin_location,
                    product_location,
                    test_location,
                    core_root,
                    is_corefx=False,
                    generate_layout=True):
    """ Setup the core root

    Args:
        host_os(str)                : os
        arch(str)                   : architecture
        build_type(str)             : build configuration
        coreclr_repo_location(str)  : coreclr repo location
        product_location(str)       : Product location
        core_root(str)              : Location for core_root
        is_corefx                   : Building corefx core_root

    Optional Args:
        is_corefx(Bool)             : Pass if planning on running corex
                                    : tests

    """
    global g_verbose

    assert os.path.isdir(product_location)

    # Create core_root if it does not exist
    if os.path.isdir(core_root):
        shutil.rmtree(core_root)
        
    os.makedirs(core_root)

    # Setup the dotnetcli location
    dotnetcli_location = os.path.join(coreclr_repo_location, "Tools", "dotnetcli", "dotnet%s" % (".exe" if host_os == "Windows_NT" else ""))

    # Set global env variables.
    os.environ["__BuildLogRootName"] = "Restore_Product"

    if host_os != "Windows_NT":
        os.environ["__DistroRid"] = "%s-%s" % ("osx" if sys.platform == "darwin" else "linux", arch)

    command = [os.path.join(coreclr_repo_location, "run.%s" % ("cmd" if host_os == "Windows_NT" else "sh")),
               "build",
               "-Project=%s" % os.path.join(coreclr_repo_location, "tests", "build.proj")]

    logs_dir = os.path.join(coreclr_repo_location, "bin", "Logs")
    if not os.path.isdir(logs_dir):
        os.makedirs(logs_dir)

    log_path = os.path.join(logs_dir, "Restore_Product%s_%s_%s" % (host_os, arch, build_type))
    build_log = log_path + ".log"
    wrn_log = log_path + ".wrn"
    err_log = log_path + ".err"

    msbuild_log_params = "/fileloggerparameters:\"Verbosity=normal;LogFile=%s\"" % build_log
    msbuild_wrn_params = "/fileloggerparameters1:\"WarningsOnly;LogFile=%s\"" % wrn_log
    msbuild_err_params = "/fileloggerparameters2:\"ErrorsOnly;LogFile=%s\"" % err_log

    command += ["-MsBuildLog=%s" % msbuild_log_params,
                "-MsBuildWrn=%s" % msbuild_wrn_params,
                "-MsBuildErr=%s" % msbuild_err_params]

    if host_os != "Windows_NT":
        command = ["bash"] + command
        command += ["-MsBuildEventLogging=\"/l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log\""]

    if g_verbose:
        command += ["-verbose"]

    command += [ "-BatchRestorePackages",
                 "-BuildType=%s" % build_type,
                 "-BuildArch=%s" % arch,
                 "-BuildOS=%s" % host_os]

    print("Restoring packages...")
    print(" ".join(command))

    if not g_verbose:
        proc = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    else:
        proc = subprocess.Popen(command)

    try:
        proc.communicate()
    except KeyboardInterrupt:
        proc.kill()
        sys.exit(1)

    if proc.returncode == 1:
        "Error test dependency resultion failed."
        return False

    os.environ["__BuildLogRootName"] = ""

    # Copy restored packages to core_root
    # Set global env variables.
    os.environ["__BuildLogRootName"] = "Tests_Overlay_Managed"

    if host_os != "Windows_NT":
        os.environ["__DistroRid"] = "%s-%s" % ("osx" if sys.platform == "darwin" else "linux", arch)
        os.environ["__RuntimeId"] = os.environ["__DistroRid"]

    os.environ["Core_Root"] = core_root
    os.environ["xUnitTestBinBase"] = os.path.dirname(os.path.dirname(core_root))

    command = [os.path.join(coreclr_repo_location, "run.%s" % ("cmd" if host_os == "Windows_NT" else "sh")),
               "build",
               "-Project=%s" % os.path.join(coreclr_repo_location, "tests", "runtest.proj")]

    logs_dir = os.path.join(coreclr_repo_location, "bin", "Logs")
    if not os.path.isdir(logs_dir):
        os.makedirs(logs_dir)

    log_path = os.path.join(logs_dir, "Tests_Overlay_Managed%s_%s_%s" % (host_os, arch, build_type))
    build_log = log_path + ".log"
    wrn_log = log_path + ".wrn"
    err_log = log_path + ".err"

    msbuild_log_params = "/fileloggerparameters:\"Verbosity=normal;LogFile=%s\"" % build_log
    msbuild_wrn_params = "/fileloggerparameters1:\"WarningsOnly;LogFile=%s\"" % wrn_log
    msbuild_err_params = "/fileloggerparameters2:\"ErrorsOnly;LogFile=%s\"" % err_log

    command += ["-MsBuildLog=%s" % msbuild_log_params,
                "-MsBuildWrn=%s" % msbuild_wrn_params,
                "-MsBuildErr=%s" % msbuild_err_params]

    if host_os != "Windows_NT":
        command = ["bash"] + command
        command += ["-MsBuildEventLogging=\"/l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log\""]

    if g_verbose:
        command += ["-verbose"]

    command += [ "-testOverlay",
                 "-BuildType=%s" % build_type,
                 "-BuildArch=%s" % arch,
                 "-BuildOS=%s" % host_os]

    print("")
    print("Creating Core_Root...")
    print(" ".join(command))

    if not g_verbose:
        proc = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    else:
        proc = subprocess.Popen(command)

    try:
        proc.communicate()
    except KeyboardInterrupt:
        proc.kill()
        sys.exit(1)

    if proc.returncode == 1:
        "Error test dependency resultion failed."
        return False

    os.environ["__BuildLogRootName"] = ""
    os.environ["xUnitTestBinBase"] = ""
    os.environ["__RuntimeId"] = ""

    def copy_tree(src, dest):
        """ Simple copy from src to dest
        """
        assert os.path.isdir(src)
        assert os.path.isdir(dest)

        for item in os.listdir(src):
            if ".nuget" in item:
                pass
            item = os.path.join(src, item)

            if os.path.isfile(item):
                shutil.copy2(item, dest)

                if host_os != "Windows_NT":
                    # Set executable bit
                    os.chmod(os.path.join(dest, item), 0o774)
            else:
                new_dir = os.path.join(dest, os.path.basename(item))
                if os.path.isdir(new_dir):
                    shutil.rmtree(new_dir)
                
                shutil.copytree(item, new_dir)

    # Copy the product dir to the core_root directory
    print()
    print("Copying Product Bin to Core_Root:")
    print("cp -r %s%s* %s" % (product_location, os.path.sep, core_root))
    copy_tree(product_location, core_root)
    print("---------------------------------------------------------------------")
    print()

    if is_corefx:
        corefx_utility_setup = os.path.join(coreclr_repo_location,
                                            "src",
                                            "Common",
                                            "CoreFX",
                                            "TestFileSetup",
                                            "CoreFX.TestUtils.TestFileSetup.csproj")

        os.environ["__BuildLogRootName"] = "Tests_GenerateTestHost"
        msbuild_command = [dotnetcli_location,
                           "msbuild",
                           os.path.join(coreclr_repo_location, "tests", "runtest.proj"),
                           "/p:GenerateRuntimeLayout=true"]
        proc = subprocess.Popen(msbuild_command)
        proc.communicate()

        if not proc.returncode == 0:
            "Error test dependency resultion failed."
            return False

        os.environ["__BuildLogRootName"] = ""

        msbuild_command = [dotnetcli_location,
                           "msbuild",
                           "/t:Restore",
                           corefx_utility_setup]

        proc = subprocess.Popen(msbuild_command)
        proc.communicate()

        if proc.returncode == 1:
            "Error test dependency resultion failed."
            return False

        corefx_logpath = os.path.join(coreclr_repo_location, 
                                      "bin", 
                                      "tests", 
                                      "%s.%s.%s" % (host_os, arch, build_type), 
                                      "CoreFX",
                                      "CoreFXTestUtilities")

        msbuild_command = [dotnetcli_location,
                           "msbuild",
                           "/p:Configuration=%s" % build_type,
                           "/p:OSGroup=%s" % host_os,
                           "/p:Platform=%s" % arch,
                           "/p:OutputPath=%s" % corefx_logpath,
                           corefx_utility_setup]

        proc = subprocess.Popen(msbuild_command)
        proc.communicate()

        if proc.returncode == 1:
            "Error test dependency resultion failed."
            return False

    print("Core_Root setup.")
    print("")

    return True

def delete_existing_wrappers(test_location):
    """ Delete the existing xunit wrappers

    Args:
        test_location(str)          : location of the test
    """

    assert os.path.isdir(test_location) or os.path.isfile(test_location)

    extension = "dll"

    if os.path.isdir(test_location):
        for item in os.listdir(test_location):
            delete_existing_wrappers(os.path.join(test_location, item))
    elif test_location.endswith(extension) and "xunitwrapper" in test_location.lower():
        # Delete the test wrapper.

        print("rm %s" % test_location)
        os.remove(test_location)

def build_test_wrappers(host_os, 
                        arch, 
                        build_type, 
                        coreclr_repo_location,
                        test_location):
    """ Build the coreclr test wrappers

    Args:
        host_os(str)                : os
        arch(str)                   : architecture
        build_type(str)             : build configuration
        coreclr_repo_location(str)  : coreclr repo location
        test_location(str)          : location of the test

    Notes:
        Build the xUnit test wrappers. Note that this will have been done as a
        part of build-test.cmd/sh. It is possible that the host has a different
        set of dependencies from the target or the exclude list has changed
        after building.

    """
    global g_verbose

    delete_existing_wrappers(test_location)

    # Setup the dotnetcli location
    dotnetcli_location = os.path.join(coreclr_repo_location, "Tools", "dotnetcli", "dotnet%s" % (".exe" if host_os == "Windows_NT" else ""))

    # Set global env variables.
    os.environ["__BuildLogRootName"] = "Tests_XunitWrapper"
    os.environ["__Exclude"] = os.path.join(coreclr_repo_location, "tests", "issues.targets")

    command = [dotnetcli_location,
               "msbuild",
               os.path.join(coreclr_repo_location, "tests", "runtest.proj"),
               "/p:RestoreAdditionalProjectSources=https://dotnet.myget.org/F/dotnet-core/",
               "/p:BuildWrappers=true",
               "/p:TargetsWindows=%s" % ("true" if host_os == "Windows_NT" else "false")]

    logs_dir = os.path.join(coreclr_repo_location, "bin", "Logs")
    if not os.path.isdir(logs_dir):
        os.makedirs(logs_dir)

    log_path = os.path.join(logs_dir, "Tests_XunitWrapper%s_%s_%s" % (host_os, arch, build_type))
    build_log = log_path + ".log"
    wrn_log = log_path + ".wrn"
    err_log = log_path + ".err"

    command += ["/fileloggerparameters:\"Verbosity=normal;LogFile=%s\"" % build_log,
                "/fileloggerparameters1:\"WarningsOnly;LogFile=%s\"" % wrn_log,
                "/fileloggerparameters2:\"ErrorsOnly;LogFile=%s\"" % err_log,
                "/consoleloggerparameters:Summary"]

    command += ["/p:__BuildOS=%s" % host_os,
                "/p:__BuildArch=%s" % arch,
                "/p:__BuildType=%s" % build_type,
                "/p:__LogsDir=%s" % logs_dir]

    print("Creating test wrappers...")
    print(" ".join(command))

    if not g_verbose:
        proc = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

        if not running_in_ci():
            try:
                expected_time_to_complete = 60*5 # 5 Minutes
                estimated_time_running = 0

                time_delta = 1

                while True:
                    time_remaining = expected_time_to_complete - estimated_time_running
                    time_in_minutes = math.floor(time_remaining / 60)
                    remaining_seconds = time_remaining % 60

                    sys.stdout.write("\rEstimated time remaining: %d minutes %d seconds" % (time_in_minutes, remaining_seconds))
                    sys.stdout.flush()

                    time.sleep(time_delta)
                    estimated_time_running += time_delta

                    if estimated_time_running == expected_time_to_complete:
                        break
                    if proc.poll() is not None:
                        break

            except KeyboardInterrupt:
                proc.kill()
                sys.exit(1)
    else:
        proc = subprocess.Popen(command)

    try:
        proc.communicate()
    except KeyboardInterrupt:
        proc.kill()
        sys.exit(1)

    if proc.returncode == 1:
        "Error test dependency resultion failed."
        return False

def find_test_from_name(host_os, test_location, test_name):
    """ Given a test's name return the location on disk

    Args:
        host_os (str)       : os
        test_location (str) :path to the coreclr tests
        test_name (str)     : Name of the test, all special characters will have
                            : been replaced with underscores.
    
    Return:
        test_path (str): Path of the test based on its name
    """

    location = test_name

    # Lambdas and helpers
    is_file_or_dir = lambda path : os.path.isdir(path) or os.path.isfile(path)
    def match_filename(test_path):
        # Scan through the test directory looking for a similar
        # file
        global file_name_cache

        if not os.path.isdir(os.path.dirname(test_path)):
            pass

        assert os.path.isdir(os.path.dirname(test_path))
        size_of_largest_name_file = 0

        dir_contents = file_name_cache[os.path.dirname(test_path)]

        if dir_contents is None:
            dir_contents = defaultdict(lambda: None)
            for item in os.listdir(os.path.dirname(test_path)):
                dir_contents[re.sub("[%s]" % string.punctuation, "_", item)] = item

            file_name_cache[os.path.dirname(test_path)] = dir_contents

        # It is possible there has already been a match
        # therefore we need to remove the punctuation again.
        basename_to_match = re.sub("[%s]" % string.punctuation, "_", os.path.basename(test_path))
        if basename_to_match in dir_contents:
            test_path = os.path.join(os.path.dirname(test_path), dir_contents[basename_to_match])

        size_of_largest_name_file = len(max(dir_contents, key=len))

        return test_path, size_of_largest_name_file

    def dir_has_nested_substrings(test_path, test_item):
        """ A directory has multiple paths where one path is a substring of another
        """

        dir_contents = file_name_cache[os.path.dirname(test_path)]

        if dir_contents is None:
            dir_contents = defaultdict(lambda: None)
            for item in os.listdir(os.path.dirname(test_path)):
                dir_contents[re.sub("[%s]" % string.punctuation, "_", item)] = item

            file_name_cache[os.path.dirname(test_path)] = dir_contents

        test_item = re.sub("[%s]" % string.punctuation, "_", test_item)

        count = 0
        for item in dir_contents:
            if test_item in item:
                count += 1
        
        return count > 1

    # Find the test by searching down the directory list.
    starting_path = test_location
    loc_split = location.split("_")
    append = False
    for index, item in enumerate(loc_split):
        if not append:
            test_path = os.path.join(starting_path, item)
        else:
            append = False
            test_path, size_of_largest_name_file = match_filename(starting_path + "_" + item)

        if not is_file_or_dir(test_path):
            append = True

        # It is possible that there is another directory that is named
        # without an underscore.
        elif index + 1 < len(loc_split) and os.path.isdir(test_path):
            next_test_path = os.path.join(test_path, loc_split[index + 1])

            if not is_file_or_dir(next_test_path) or dir_has_nested_substrings(test_path, item):
                added_path = test_path
                for forward_index in range(index + 1, len(loc_split)):
                    added_path, size_of_largest_name_file = match_filename(added_path + "_" + loc_split[forward_index])
                    if is_file_or_dir(added_path):
                        append = True
                        break
                    elif size_of_largest_name_file < len(os.path.basename(added_path)):
                        break
        
        starting_path = test_path

    location = starting_path
    if not os.path.isfile(location):
        pass
    
    assert(os.path.isfile(location))

    return location

def parse_test_results(host_os, arch, build_type, coreclr_repo_location, test_location):
    """ Parse the test results for test execution information

    Args:
        host_os                 : os
        arch                    : architecture run on
        build_type              : build configuration (debug, checked, release)
        coreclr_repo_location   : coreclr repo location
        test_location           : path to coreclr tests

    """
    logs_dir = os.path.join(coreclr_repo_location, "bin", "Logs")
    log_path = os.path.join(logs_dir, "TestRunResults_%s_%s_%s" % (host_os, arch, build_type))
    print("Parsing test results from (%s)" % log_path)

    test_run_location = os.path.join(coreclr_repo_location, "bin", "Logs", "testRun.xml")

    if not os.path.isfile(test_run_location):
        # Check if this is a casing issue

        found = False
        for item in os.listdir(os.path.dirname(test_run_location)):
            item_lower = item.lower()
            if item_lower == "testrun.xml":
                # Correct the name.
                os.rename(os.path.join(coreclr_repo_location, "bin", "Logs", item), test_run_location)
                found = True
                break

        if not found:
            print("Unable to find testRun.xml. This normally means the tests did not run.")
            print("It could also mean there was a problem logging. Please run the tests again.")

            return

    if host_os != "Windows_NT" and running_in_ci():
        # Huge hack.
        # TODO change netci to parse testRun.xml
        shutil.copy2(test_run_location, os.path.join(os.path.dirname(test_run_location), "coreclrtests.xml"))

    assemblies = xml.etree.ElementTree.parse(test_run_location).getroot()

    tests = defaultdict(lambda: None)
    for assembly in assemblies:
        for collection in assembly:
            if collection.tag == "errors" and collection.text != None:
                # Something went wrong during running the tests.
                print("Error running the tests, please run runtest.py again.")
                sys.exit(1)
            elif collection.tag != "errors":
                test_name = None
                for test in collection:
                    type = test.attrib["type"]
                    method = test.attrib["method"]

                    type = type.split("._")[0]
                    test_name = type + method

                assert test_name != None

                failed = collection.attrib["failed"]
                skipped = collection.attrib["skipped"]
                passed = collection.attrib["passed"]
                time = float(collection.attrib["time"])

                test_output = None

                if failed == "1":
                    failure_info = collection[0][0]

                    test_output = failure_info[0].text

                test_location_on_filesystem = find_test_from_name(host_os, test_location, test_name)

                assert os.path.isfile(test_location_on_filesystem)
                
                assert tests[test_name] == None
                tests[test_name] = defaultdict(lambda: None, {
                    "name": test_name,
                    "test_path": test_location_on_filesystem,
                    "failed": failed,
                    "skipped": skipped,
                    "passed": passed,
                    "time": time,
                    "test_output": test_output
                })

    return tests

def print_summary(tests):
    """ Print a summary of the test results

    Args:
        tests (defaultdict[String]: { }): The tests that were reported by 
                                        : xunit
    
    """

    assert tests is not None

    failed_tests = []
    passed_tests = []
    skipped_tests = []

    for test in tests:
        test = tests[test]

        if test["failed"] == "1":
            failed_tests.append(test)
        elif test["passed"] == "1":
            passed_tests.append(test)
        else:
            skipped_tests.append(test)

    print()
    print("Total tests run: %d" % len(tests))
    print()
    print("Total passing tests: %d" % len(passed_tests))
    print("Total failed tests: %d" % len(failed_tests))
    print("Total skipped tests: %d" % len(skipped_tests))
    print()

    failed_tests.sort(key=lambda item: item["time"], reverse=True)
    passed_tests.sort(key=lambda item: item["time"], reverse=True)
    skipped_tests.sort(key=lambda item: item["time"], reverse=True)

    def print_tests_helper(tests, stop_count):
        for index, item in enumerate(tests):
            time = item["time"]
            unit = "seconds"
            time_remainder = ""
            second_unit = ""
            saved_time = time
            remainder_str = ""

            # If it can be expressed in hours
            if time > 60**2:
                time = saved_time / (60**2)
                time_remainder = saved_time % (60**2)
                time_remainder /= 60
                time_remainder = math.floor(time_remainder)
                unit = "hours"
                second_unit = "minutes"

                remainder_str = " %s %s" % (int(time_remainder), second_unit)

            elif time > 60 and time < 60**2:
                time = saved_time / 60
                time_remainder = saved_time % 60
                time_remainder = math.floor(time_remainder)
                unit = "minutes"
                second_unit = "seconds"

                remainder_str = " %s %s" % (int(time_remainder), second_unit)

            print("%s (%d %s%s)" % (item["test_path"], time, unit, remainder_str))

            if stop_count != None:
                if index >= stop_count:
                    break

    if len(failed_tests) > 0:
        print("Failed tests:")
        print()
        print_tests_helper(failed_tests, None)
        

    if len(passed_tests) > 50:
        print()
        print("50 slowest passing tests:")
        print()
        print_tests_helper(passed_tests, 50)

    if len(failed_tests) > 0:
        print()
        print("#################################################################")
        print("Output of failing tests:")
        print()

        for item in failed_tests:
            print("[%s]: " % item["test_path"])
            print()
            
            test_output = item["test_output"]

            # XUnit results are captured as escaped, escaped characters.
            test_output = test_output.replace("\\r", "\r")
            test_output = test_output.replace("\\n", "\n")

            print(test_output)
            print()

        print()
        print("#################################################################")
        print("End of output of failing tests")
        print("#################################################################")
        print()

def create_repro(host_os, arch, build_type, env, core_root, coreclr_repo_location, tests):
    """ Go through the failing tests and create repros for them

    Args:
        host_os (String)                : os
        arch (String)                   : architecture
        build_type (String)             : build configuration (debug, checked, release)
        core_root (String)              : Core_Root path
        coreclr_repo_location (String)  : Location of coreclr git repo
        tests (defaultdict[String]: { }): The tests that were reported by 
                                        : xunit
    
    """
    assert tests is not None

    failed_tests = [tests[item] for item in tests if tests[item]["failed"] == "1"]
    if len(failed_tests) == 0:
        return
    
    bin_location = os.path.join(coreclr_repo_location, "bin")
    assert os.path.isdir(bin_location)

    repro_location = os.path.join(bin_location, "repro", "%s.%s.%s" % (host_os, arch, build_type))
    if os.path.isdir(repro_location):
        shutil.rmtree(repro_location)
    
    print("mkdir %s" % repro_location)
    os.makedirs(repro_location)

    print()
    print("Creating repo files, they can be found at: %s" % repro_location)

    assert os.path.isdir(repro_location)

    # Now that the repro_location exists under <coreclr_location>/bin/repro
    # create wrappers which will simply run the test with the correct environment
    for test in failed_tests:
        debug_env = DebugEnv(host_os, arch, build_type, env, core_root, coreclr_repo_location, test)
        debug_env.write_repro()

    print("Repro files written.")
    print("They can be found at %s" % repro_location)

def do_setup(host_os, 
             arch, 
             build_type, 
             coreclr_repo_location, 
             product_location, 
             test_location, 
             test_native_bin_location, 
             core_root, 
             unprocessed_args, 
             test_env):
    global gc_stress_c

    # Setup the tools for the repo.
    setup_tools(host_os, coreclr_repo_location)

    if unprocessed_args.generate_layout:
        success = setup_core_root(host_os, 
                                  arch, 
                                  build_type, 
                                  coreclr_repo_location, 
                                  test_native_bin_location, 
                                  product_location,
                                  test_location, 
                                  core_root)

        if not success:
            print("Error GenerateLayout has failed.")
            sys.exit(1)

        if unprocessed_args.generate_layout_only:
            sys.exit(0)

    if unprocessed_args.precompile_core_root:
        precompile_core_root(test_location, host_os, arch, core_root, use_jit_disasm=args.jitdisasm, altjit_name=unprocessed_args.crossgen_altjit)

    # If COMPlus_GCStress is set then we need to setup cordistools
    if gc_stress_c:
        setup_coredis_tools(coreclr_repo_location, host_os, arch, core_root)
    
    # Copy all the native libs to core_root
    if host_os != "Windows_NT":
        copy_native_test_bin_to_core_root(host_os, os.path.join(test_native_bin_location, "src"), core_root)

    correct_line_endings(host_os, test_location)

    if unprocessed_args.build_test_wrappers:
        build_test_wrappers(host_os, arch, build_type, coreclr_repo_location, test_location)
    else:
        # We will write out build information into the test directory. This is used
        # by runtest.py to determine whether we need to rebuild the test wrappers.
        if os.path.isfile(os.path.join(test_location, "build_info.json")):
            build_info = None
            with open(os.path.join(test_location, "build_info.json")) as file_handle:
                build_info = json.load(file_handle)

            is_same_os = build_info["build_os"] == host_os
            is_same_arch = build_info["build_arch"] == arch
            is_same_build_type = build_info["build_type"] == build_type

            # We will force a build of the test wrappers if they were cross built
            if not (is_same_os and is_same_arch and is_same_build_type):
                build_test_wrappers(host_os, arch, build_type, coreclr_repo_location, test_location)
        else:
            build_test_wrappers(host_os, arch, build_type, coreclr_repo_location, test_location)

    run_tests(host_os, 
              arch,
              build_type,
              core_root, 
              coreclr_repo_location,
              test_location, 
              test_native_bin_location,
              is_illink=unprocessed_args.il_link, 
              is_long_gc=unprocessed_args.long_gc,
              is_gcsimulator=unprocessed_args.gcsimulator,
              is_jitdasm=unprocessed_args.jitdisasm,
              is_ilasm=unprocessed_args.ilasmroundtrip,
              run_sequential=unprocessed_args.sequential,
              run_crossgen_tests=unprocessed_args.run_crossgen_tests,
              test_env=test_env)

################################################################################
# Main
################################################################################

def main(args):
    global g_verbose
    g_verbose = args.verbose

    host_os, arch, build_type, coreclr_repo_location, product_location, core_root, test_location, test_native_bin_location = setup_args(args)

    env = get_environment(test_env=args.test_env)
    if not args.analyze_results_only:
        if args.test_env is not None:
            ret_code = do_setup(host_os,
                                arch,
                                build_type,
                                coreclr_repo_location,
                                product_location,
                                test_location,
                                test_native_bin_location,
                                core_root,
                                args,
                                args.test_env)
        else:
            ret_code = create_and_use_test_env(host_os, 
                                               env, 
                                               lambda path: do_setup(host_os,
                                                                     arch,
                                                                     build_type,
                                                                     coreclr_repo_location,
                                                                     product_location,
                                                                     test_location,
                                                                     test_native_bin_location,
                                                                     core_root,
                                                                     args,
                                                                     path))
        print("Test run finished.")

    tests = parse_test_results(host_os, arch, build_type, coreclr_repo_location, test_location)

    if tests is not None:
        print_summary(tests)
        create_repro(host_os, arch, build_type, env, core_root, coreclr_repo_location, tests)

################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
