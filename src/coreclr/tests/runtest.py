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
import fnmatch
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
import zipfile

import xml.etree.ElementTree

from collections import defaultdict
from sys import platform as _platform

# Version specific imports

if sys.version_info.major < 3:
    import urllib
else:
    import urllib.request

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
parser.add_argument("-altjit_arch", dest="altjit_arch", default=None)

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
parser.add_argument("--limited_core_dumps", dest="limited_core_dumps", action="store_true", default=False)

# Only used on Unix
parser.add_argument("-test_native_bin_location", dest="test_native_bin_location", nargs='?', default=None)

################################################################################
# Globals
################################################################################

g_verbose = False
gc_stress_c = False
gc_stress = False
coredump_pattern = ""
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
    global gc_stress

    complus_vars = defaultdict(lambda: None)

    for key in env:
        value = env[key]
        if "complus" in key.lower():
            complus_vars[key] = value

    if len(list(complus_vars.keys())) > 0:
        print("Found COMPlus variables in the current environment")
        print("")

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

            print("")
            print("TestEnv: %s" % test_env.name)
            print("")
            print("Contents:")
            print("")
            print(contents)
            print("")

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
    global gc_stress

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
                 test_location,
                 host_os,
                 arch,
                 build_type, 
                 is_illink=False,
                 sequential=False,
                 limited_core_dumps=False):
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

    common_msbuild_arguments = []

    if sequential:
        common_msbuild_arguments += ["/p:ParallelRun=none"]

    logs_dir = os.path.join(coreclr_repo_location, "bin", "Logs")
    if not os.path.isdir(logs_dir):
        os.makedirs(logs_dir)
    
    command =   [dotnetcli_location,
                 "msbuild",
                 os.path.join(coreclr_repo_location, "tests", "runtest.proj"),
                 "/p:Runtests=true",
                 "/clp:showcommandline"]

    command += common_msbuild_arguments

    if is_illink:
        command += ["/p:RunTestsViaIllink=true"]

    if limited_core_dumps:
        command += ["/p:LimitedCoreDumps=true"]

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

    sys.stdout.flush() # flush output before creating sub-process
    proc = subprocess.Popen(command)

    try:
        proc.communicate()
    except:
        proc.kill()
        sys.exit(1)

    if limited_core_dumps:
        inspect_and_delete_coredump_files(host_os, arch, test_location)

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
        if sys.version_info < (3,0):

            content = None
            with open(test_location) as file_handle:
                content = file_handle.read()
     
            assert content != None
            subbed_content = content.replace(incorrect_line_ending, correct_line_ending)

            if content != subbed_content:
                with open(test_location, 'w') as file_handle:
                    file_handle.write(subbed_content)

        else:
            # Python3 will correct line endings automatically.
 
            content = None
            with open(test_location) as file_handle:
                content = file_handle.read()
     
            with open(test_location, 'w') as file_handle:
                file_handle.write(content)

def setup_coredump_generation(host_os):
    """ Configures the environment so that the current process and any child
        processes can generate coredumps.

    Args:
        host_os (String)        : os

    Notes:
        This is only support for OSX and Linux, it does nothing on Windows.
        This will print a message if setting the rlimit fails but will otherwise
        continue execution, as some systems will already be configured correctly
        and it is not necessarily a failure to not collect coredumps.
    """
    global coredump_pattern

    if host_os == "OSX":
        coredump_pattern = subprocess.check_output("sysctl -n kern.corefile", shell=True).rstrip()
    elif host_os == "Linux":
        with open("/proc/sys/kernel/core_pattern", "r") as f:
            coredump_pattern = f.read().rstrip()
    else:
        print("CoreDump generation not enabled due to unsupported OS: %s" % host_os)
        return

    # resource is only available on Unix platforms
    import resource

    if coredump_pattern != "core" and coredump_pattern != "core.%P":
        print("CoreDump generation not enabled due to unsupported coredump pattern: %s" % coredump_pattern)
        return
    else:
        print("CoreDump pattern: %s" % coredump_pattern)

    # We specify 'shell=True' as the command may otherwise fail (some systems will
    # complain that the executable cannot be found in the current directory).
    rlimit_core = subprocess.check_output("ulimit -c", shell=True).rstrip()

    if rlimit_core != "unlimited":
        try:
            # This can fail on certain platforms. ARM64 in particular gives: "ValueError: not allowed to raise maximum limit"
            resource.setrlimit(resource.RLIMIT_CORE, (resource.RLIM_INFINITY, resource.RLIM_INFINITY))
        except:
            print("Failed to enable CoreDump generation. rlimit_core: %s" % rlimit_core)
            return

        rlimit_core = subprocess.check_output("ulimit -c", shell=True).rstrip()

        if rlimit_core != "unlimited":
            print("Failed to enable CoreDump generation. rlimit_core: %s" % rlimit_core)
            return

    print("CoreDump generation enabled")

    if host_os == "Linux" and os.path.isfile("/proc/self/coredump_filter"):
        # Include memory in private and shared file-backed mappings in the dump.
        # This ensures that we can see disassembly from our shared libraries when
        # inspecting the contents of the dump. See 'man core' for details.
        with open("/proc/self/coredump_filter", "w") as f:
            f.write("0x3F")

def print_info_from_coredump_file(host_os, arch, coredump_name, executable_name):
    """ Prints information from the specified coredump to the console

    Args:
        host_os (String)         : os
        arch (String)            : architecture
        coredump_name (String)   : name of the coredump to print
        executable_name (String) : name of the executable that generated the coredump

    Notes:
        This is only support for OSX and Linux, it does nothing on Windows.
        This defaults to lldb on OSX and gdb on Linux.
        For both lldb and db, it backtraces all threads. For gdb, it also prints local
        information for every frame. This option is not available as a built-in for lldb.
    """
    if not os.path.isfile(executable_name):
        print("Not printing coredump due to missing executable: %s" % executable_name)
        return

    if not os.path.isfile(coredump_name):
        print("Not printing coredump due to missing coredump: %s" % coredump_name)
        return

    command = ""

    if host_os == "OSX":
        command = "lldb -c %s -b -o 'bt all' -o 'disassemble -b -p'" % coredump_name
    elif host_os == "Linux":
        command = "gdb --batch -ex \"thread apply all bt full\" -ex \"disassemble /r $pc\" -ex \"quit\" %s %s" % (executable_name, coredump_name)
    else:
        print("Not printing coredump due to unsupported OS: %s" % host_os)
        return

    print("Printing info from coredump: %s" % coredump_name)

    proc_failed = False

    try:
        sys.stdout.flush() # flush output before creating sub-process

        # We specify 'shell=True' as the command may otherwise fail (some systems will
        # complain that the executable cannot be found in the current directory).
        proc = subprocess.Popen(command, shell=True)
        proc.communicate()

        if proc.returncode != 0:
            proc_failed = True
    except:
        proc_failed = True

    if proc_failed:
        print("Failed to print coredump: %s" % coredump_name)

def preserve_coredump_file(coredump_name, root_storage_location="/tmp/coredumps_coreclr"):
    """ Copies the specified coredump to a new randomly named temporary directory under
        root_storage_location to ensure it is accessible after the workspace is cleaned.

    Args:
        coredump_name (String)         : name of the coredump to print
        root_storage_location (String) : the directory under which to copy coredump_name

    Notes:
        root_storage_location defaults to a folder under /tmp to ensure that it is cleaned
        up on next reboot (or after the OS configured time elapses for the folder).
    """
    if not os.path.exists(root_storage_location):
        os.mkdir(root_storage_location)

    # This creates a temporary directory under `root_storage_location` to ensure it doesn'tag
    # conflict with any coredumps from past runs.
    storage_location = tempfile.mkdtemp('', '', root_storage_location)

    # Only preserve the dump if the directory is empty. Otherwise, do nothing.
    # This is a way to prevent us from storing/uploading too many dumps.
    if os.path.isfile(coredump_name) and not os.listdir(storage_location):
        print("Copying coredump file %s to %s" % (coredump_name, storage_location))
        shutil.copy2(coredump_name, storage_location)
        # TODO: Support uploading to dumpling

def inspect_and_delete_coredump_file(host_os, arch, coredump_name):
    """ Prints information from the specified coredump and creates a backup of it

    Args:
        host_os (String)         : os
        arch (String)            : architecture
        coredump_name (String)   : name of the coredump to print
    """
    print_info_from_coredump_file(host_os, arch, coredump_name, "%s/corerun" % os.environ["CORE_ROOT"])
    preserve_coredump_file(coredump_name)
    os.remove(coredump_name)

def inspect_and_delete_coredump_files(host_os, arch, test_location):
    """ Finds all coredumps under test_location, prints some basic information about them
        to the console, and creates a backup of the dumps for further investigation

    Args:
        host_os (String)         : os
        arch (String)            : architecture
        test_location (String)   : the folder under which to search for coredumps
    """
    # This function prints some basic information from core files in the current
    # directory and deletes them immediately. Based on the state of the system, it may
    # also upload a core file to the dumpling service.
    # (see preserve_core_file).
    
    # Depending on distro/configuration, the core files may either be named "core"
    # or "core.<PID>" by default. We will read /proc/sys/kernel/core_uses_pid to 
    # determine which one it is.
    # On OS X/macOS, we checked the kern.corefile value before enabling core dump
    # generation, so we know it always includes the PID.
    coredump_name_uses_pid=False

    print("Looking for coredumps...")
    
    if "%P" in coredump_pattern:
        coredump_name_uses_pid=True
    elif host_os == "Linux" and os.path.isfile("/proc/sys/kernel/core_uses_pid"):
        with open("/proc/sys/kernel/core_uses_pid", "r") as f:
            if f.read().rstrip() == "1":
                coredump_name_uses_pid=True

    filter_pattern = ""
    regex_pattern = ""
    matched_file_count = 0

    if coredump_name_uses_pid:
        filter_pattern = "core.*"
        regex_pattern = "core.[0-9]+"
    else:
        filter_pattern = "core"
        regex_pattern = "core"

    for dir_path, dir_names, file_names in os.walk(test_location):
        for file_name in fnmatch.filter(file_names, filter_pattern):
            if re.match(regex_pattern, file_name):
                print("Found coredump: %s in %s" % (file_name, dir_path))
                matched_file_count += 1
                inspect_and_delete_coredump_file(host_os, arch, os.path.join(dir_path, file_name))

    print("Found %s coredumps." % matched_file_count)

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
              run_sequential=False,
              limited_core_dumps=False):
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

    if limited_core_dumps:
        setup_coredump_generation(host_os)

    # Set Core_Root
    print("Setting CORE_ROOT=%s" % core_root)
    os.environ["CORE_ROOT"] = core_root

    # Set test env if exists
    if test_env is not None:
        os.environ["__TestEnv"] = test_env

    #=====================================================================================================================================================
    #
    # This is a workaround needed to unblock our CI (in particular, Linux/arm and Linux/arm64 jobs) from the following failures appearing almost in every
    # pull request (but hard to reproduce locally)
    #
    #   System.IO.FileLoadException: Could not load file or assembly 'Exceptions.Finalization.XUnitWrapper, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
    #   An operation is not legal in the current state. (Exception from HRESULT: 0x80131509 (COR_E_INVALIDOPERATION))
    #
    # COR_E_INVALIDOPERATION comes from System.InvalidOperationException that is thrown during AssemblyLoadContext.ResolveUsingResolvingEvent
    # when multiple threads attempt to modify an instance of Dictionary (managedAssemblyCache) during Xunit.DependencyContextAssemblyCache.LoadManagedDll call.
    #
    # In order to mitigate the failure we built our own xunit.console.dll with ConcurrentDictionary used for managedAssemblyCache and use this instead of
    # the one pulled from NuGet. The exact code that got built can be found at the following fork of Xunit
    #  * https://github.com/echesakovMSFT/xunit/tree/UseConcurrentDictionaryInDependencyContextAssemblyCache
    #
    # The assembly was built using Microsoft Visual Studio v15.9.0-pre.4.0 Developer Command Prompt using the following commands
    #  1) git clone https://github.com/echesakovMSFT/xunit.git --branch UseConcurrentDictionaryInDependencyContextAssemblyCache --single-branch
    #  2) cd xunit
    #  3) git submodule update --init
    #  4) powershell .\build.ps1 -target packages -buildAssemblyVersion 2.4.1 -buildSemanticVersion 2.4.1-coreclr
    #
    # Then file "xunit\src\xunit.console\bin\Release\netcoreapp2.0\xunit.console.dll" was archived and uploaded to the clrjit blob storage.
    #
    # Ideally, this code should be removed when we find a more robust way of running Xunit tests.
    #
    # References:
    #  * https://github.com/dotnet/coreclr/issues/20392
    #  * https://github.com/dotnet/coreclr/issues/20594
    #  * https://github.com/xunit/xunit/issues/1842
    #  * https://github.com/xunit/xunit/pull/1846
    #
    #=====================================================================================================================================================

    print("Download and overwrite xunit.console.dll in Core_Root")

    urlretrieve = urllib.urlretrieve if sys.version_info.major < 3 else urllib.request.urlretrieve
    zipfilename = os.path.join(tempfile.gettempdir(), "xunit.console.dll.zip")
    url = r"https://clrjit.blob.core.windows.net/xunit-console/xunit.console.dll-v2.4.1.zip"
    urlretrieve(url, zipfilename)

    with zipfile.ZipFile(zipfilename,"r") as ziparch:
        ziparch.extractall(core_root)

    os.remove(zipfilename)
    assert not os.path.isfile(zipfilename)

    # Call msbuild.
    return call_msbuild(coreclr_repo_location,
                        dotnetcli_location,
                        test_location,
                        host_os,
                        arch,
                        build_type,
                        is_illink=is_illink,
                        limited_core_dumps=limited_core_dumps,
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
            print("")

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

        if test_location.lower() != default_test_location.lower() and os.path.isdir(default_test_location):
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
            print("")

    if core_root is None:
        default_core_root = os.path.join(test_location, "Tests", "Core_Root")

        if os.path.isdir(default_core_root):
            core_root = default_core_root

            print("Using default location for core_root.")
            print("Core_Root: %s" % core_root)
            print("")

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

    is_same_os = False
    is_same_arch = False
    is_same_build_type = False

    # We will write out build information into the test directory. This is used
    # by runtest.py to determine whether we need to rebuild the test wrappers.
    if os.path.isfile(os.path.join(test_location, "build_info.json")):
        with open(os.path.join(test_location, "build_info.json")) as file_handle:
            build_info = json.load(file_handle)
        is_same_os = build_info["build_os"] == host_os
        is_same_arch = build_info["build_arch"] == arch
        is_same_build_type = build_info["build_type"] == build_type

    if host_os != "Windows_NT" and not (is_same_os and is_same_arch and is_same_build_type):
        if test_native_bin_location is None:
            print("Using default location for test_native_bin_location.")
            test_native_bin_location = os.path.join(os.path.join(coreclr_repo_location, "bin", "obj", "%s.%s.%s" % (host_os, arch, build_type), "tests"))
            print("Native bin location: %s" % test_native_bin_location)
            print("")
            
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

    if host_os.lower() == "osx":
        print("GCStress C is not supported on your platform.")
        sys.exit(1)

    unsupported_arches = ["arm", "arm64"]

    if arch in unsupported_arches:
        # Nothing to do; CoreDisTools unneeded.
        return

    command = None
    test_location = os.path.join(coreclr_repo_location, "tests")
    if host_os == "Windows_NT":
        command = [os.path.join(test_location, "setup-stress-dependencies.cmd"), "/arch", arch, "/outputdir", core_root]
    else:
        command = [os.path.join(test_location, "setup-stress-dependencies.sh"), "--outputDir=%s" % core_root]

    sys.stdout.flush() # flush output before creating sub-process
    proc = subprocess.Popen(command)
    proc.communicate()

    if proc.returncode != 0:
        print("Failed to set up stress dependencies.")
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

        if return_code == -2146230517:
            print("%s is not a managed assembly." % file)
            return False

        if return_code != 0:
            print("Unable to precompile %s (%d)" % (file, return_code))
            return False

        print("Successfully precompiled %s" % file)
        return True

    print("Precompiling all assemblies in %s" % core_root)
    print("")

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

    print("")

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

    sys.stdout.flush() # flush output before creating sub-process
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
        print("Error: package restore failed.")
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

    sys.stdout.flush() # flush output before creating sub-process
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
        print("Error: creating Core_Root failed.")
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
    print("")
    print("Copying Product Bin to Core_Root:")
    print("cp -r %s%s* %s" % (product_location, os.path.sep, core_root))
    copy_tree(product_location, core_root)
    print("---------------------------------------------------------------------")
    print("")

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

        sys.stdout.flush() # flush output before creating sub-process
        proc = subprocess.Popen(msbuild_command)
        proc.communicate()

        if not proc.returncode == 0:
            print("Error: generating test host failed.")
            return False

        os.environ["__BuildLogRootName"] = ""

        msbuild_command = [dotnetcli_location,
                           "msbuild",
                           "/t:Restore",
                           corefx_utility_setup]

        sys.stdout.flush() # flush output before creating sub-process
        proc = subprocess.Popen(msbuild_command)
        proc.communicate()

        if proc.returncode == 1:
            print("Error: msbuild failed.")
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

        sys.stdout.flush() # flush output before creating sub-process
        proc = subprocess.Popen(msbuild_command)
        proc.communicate()

        if proc.returncode == 1:
            print("Error: msbuild failed.")
            return False

    print("Core_Root setup.")
    print("")

    return True

if sys.version_info.major < 3:
    def to_unicode(s):
        return unicode(s, "utf-8")
else:
    def to_unicode(s):
        return str(s, "utf-8")

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
                        test_location,
                        altjit_arch=None):
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

    delete_existing_wrappers(to_unicode(test_location))

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

    if not altjit_arch is None:
        command += ["/p:__AltJitArch=%s" % altjit_arch]

    print("Creating test wrappers...")
    print(" ".join(command))

    sys.stdout.flush() # flush output before creating sub-process
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
        print("Error: creating test wrappers failed.")
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
        print("%d failed tests:" % len(failed_tests))
        print("")
        print_tests_helper(failed_tests, None)
        
    # The following code is currently disabled, as it produces too much verbosity in a normal
    # test run. It could be put under a switch, or else just enabled as needed when investigating
    # test slowness.
    #
    # if len(passed_tests) > 50:
    #     print("")
    #     print("50 slowest passing tests:")
    #     print("")
    #     print_tests_helper(passed_tests, 50)

    if len(failed_tests) > 0:
        print("")
        print("#################################################################")
        print("Output of failing tests:")
        print("")

        for item in failed_tests:
            print("[%s]: " % item["test_path"])
            print("")
            
            test_output = item["test_output"]

            # XUnit results are captured as escaped characters.
            test_output = test_output.replace("\\r", "\r")
            test_output = test_output.replace("\\n", "\n")

            print(test_output)
            test_output = test_output.replace("/r", "\r")
            test_output = test_output.replace("/n", "\n")
            unicode_output = None
            if sys.version_info < (3,0):
                # Handle unicode characters in output in python2.*
                unicode_output = unicode(test_output, "utf-8")
            else:
                unicode_output = test_output

            print(unicode_output)
            print("")

        print("")
        print("#################################################################")
        print("End of output of failing tests")
        print("#################################################################")
        print("")

    print("")
    print("Total tests run    : %d" % len(tests))
    print("Total passing tests: %d" % len(passed_tests))
    print("Total failed tests : %d" % len(failed_tests))
    print("Total skipped tests: %d" % len(skipped_tests))
    print("")

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

    print("")
    print("Creating repro files at: %s" % repro_location)

    os.makedirs(repro_location)
    assert os.path.isdir(repro_location)

    # Now that the repro_location exists under <coreclr_location>/bin/repro
    # create wrappers which will simply run the test with the correct environment
    for test in failed_tests:
        debug_env = DebugEnv(host_os, arch, build_type, env, core_root, coreclr_repo_location, test)
        debug_env.write_repro()

    print("Repro files written.")

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
            print("Error: GenerateLayout failed.")
            sys.exit(1)

        if unprocessed_args.generate_layout_only:
            sys.exit(0)

    if unprocessed_args.precompile_core_root:
        precompile_core_root(test_location, host_os, arch, core_root, use_jit_disasm=args.jitdisasm, altjit_name=unprocessed_args.crossgen_altjit)

    # If COMPlus_GCStress is set then we need to setup cordistools
    if gc_stress_c:
        setup_coredis_tools(coreclr_repo_location, host_os, arch, core_root)
    
    build_info = None
    is_same_os = None
    is_same_arch = None
    is_same_build_type = None

    # We will write out build information into the test directory. This is used
    # by runtest.py to determine whether we need to rebuild the test wrappers.
    if os.path.isfile(os.path.join(test_location, "build_info.json")):
        with open(os.path.join(test_location, "build_info.json")) as file_handle:
            build_info = json.load(file_handle)
        is_same_os = build_info["build_os"] == host_os
        is_same_arch = build_info["build_arch"] == arch
        is_same_build_type = build_info["build_type"] == build_type

    # Copy all the native libs to core_root
    if host_os != "Windows_NT"  and not (is_same_os and is_same_arch and is_same_build_type):
        copy_native_test_bin_to_core_root(host_os, os.path.join(test_native_bin_location, "src"), core_root)

        # Line ending only need to be corrected if this is a cross build.
        correct_line_endings(host_os, test_location)

    # If we are inside altjit scenario, we ought to re-build Xunit test wrappers to consider
    # ExcludeList items in issues.targets for both build arch and altjit arch
    is_altjit_scenario = not args.altjit_arch is None

    if unprocessed_args.build_test_wrappers:
        build_test_wrappers(host_os, arch, build_type, coreclr_repo_location, test_location)
    elif build_info is None:
        build_test_wrappers(host_os, arch, build_type, coreclr_repo_location, test_location)
    elif not (is_same_os and is_same_arch and is_same_build_type):
        build_test_wrappers(host_os, arch, build_type, coreclr_repo_location, test_location)
    elif is_altjit_scenario:
        build_test_wrappers(host_os, arch, build_type, coreclr_repo_location, test_location, args.altjit_arch)

    return run_tests(host_os, 
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
              limited_core_dumps=unprocessed_args.limited_core_dumps,
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

    ret_code = 0

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

    return ret_code

################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
