#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title:               run.py
#
# Notes:
#
# Universal script to setup and run the xunit console runner. The script relies
# on run.proj and the bash and batch wrappers. All test excludes will also
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
# Use the instructions here:
#    https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/coreclr/windows-test-instructions.md
#    https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/coreclr/unix-test-instructions.md
#
################################################################################
################################################################################

import argparse
import datetime
import fnmatch
import json
import math
import os
import shutil
import subprocess
import sys
import tempfile
import re
import string
import xml.etree.ElementTree

from collections import defaultdict

# Import coreclr_arguments.py from src\coreclr\scripts
sys.path.append(os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "coreclr", "scripts"))
from coreclr_arguments import *

################################################################################
# Argument Parser
################################################################################

description = ("""Universal script to setup and run the xunit console runner. The script relies
on run.proj and the bash and batch wrappers. All test excludes will also
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
naming conflicts.""")

parser = argparse.ArgumentParser(description=description)

parser.add_argument("-os", dest="host_os", nargs='?', default=None)
parser.add_argument("-arch", dest="arch", nargs='?', default="x64")
parser.add_argument("-build_type", dest="build_type", nargs='?', default="Debug")
parser.add_argument("-test_location", dest="test_location", nargs="?", default=None)
parser.add_argument("-core_root", dest="core_root", nargs='?', default=None)
parser.add_argument("-runtime_repo_location", dest="runtime_repo_location", default=os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))))
parser.add_argument("-test_env", dest="test_env", default=None)

# Optional arguments which change execution.

parser.add_argument("--il_link", dest="il_link", action="store_true", default=False)
parser.add_argument("--long_gc", dest="long_gc", action="store_true", default=False)
parser.add_argument("--gcsimulator", dest="gcsimulator", action="store_true", default=False)
parser.add_argument("--ilasmroundtrip", dest="ilasmroundtrip", action="store_true", default=False)
parser.add_argument("--run_crossgen2_tests", dest="run_crossgen2_tests", action="store_true", default=False)
parser.add_argument("--large_version_bubble", dest="large_version_bubble", action="store_true", default=False)
parser.add_argument("--skip_test_run", dest="skip_test_run", action="store_true", default=False, help="Does not run tests.")
parser.add_argument("--sequential", dest="sequential", action="store_true", default=False)

parser.add_argument("--analyze_results_only", dest="analyze_results_only", action="store_true", default=False)
parser.add_argument("--verbose", dest="verbose", action="store_true", default=False)
parser.add_argument("--limited_core_dumps", dest="limited_core_dumps", action="store_true", default=False)
parser.add_argument("--run_in_context", dest="run_in_context", action="store_true", default=False)
parser.add_argument("--tiering_test", dest="tiering_test", action="store_true", default=False)

################################################################################
# Globals
################################################################################

g_verbose = False
gc_stress = False
coredump_pattern = ""
file_name_cache = defaultdict(lambda: None)

################################################################################
# Classes
################################################################################

class TempFile:
    def __init__(self, extension):
        self.file = None
        self.file_name = None
        self.extension = extension

    def __enter__(self):
        self.file = tempfile.NamedTemporaryFile(delete=False, suffix=self.extension)

        self.file_name = self.file.name

        return self.file_name

    def __exit__(self, exc_type, exc_val, exc_tb):
        try:
            os.remove(self.file_name)
        except:
            print("Error failed to delete: {}.".format(self.file_name))

class DebugEnv:
    def __init__(self,
                 args,
                 env,
                 test):
        """ Go through the failing tests and create repros for them

        Args:
            args
            env                     : env for the repro
            test ({})               : The test metadata

        """
        self.unique_name = "%s_%s_%s_%s" % (test["name"],
                                            args.host_os,
                                            args.arch,
                                            args.build_type)

        self.args = args
        self.env = env
        self.test = test
        self.test_location = test["test_path"]

        self.__create_repro_wrapper__()

        self.path = None
        
        if self.args.host_os == "windows":
            self.path = self.unique_name + ".cmd"
        else:
            self.path = self.unique_name + ".sh"

        repro_location = os.path.join(self.args.artifacts_location, "repro", "%s.%s.%s" % (self.args.host_os, self.args.arch, self.args.build_type))
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

        dbg_type = "cppvsdbg" if self.host_os == "windows" else ""

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
            "program": self.args.corerun_path,
            "args": [self.exe_location],
            "stopAtEntry": False,
            "cwd": os.path.join("${workspaceFolder}", "..", ".."),
            "environment": environment,
            "externalConsole": True
        })

        if self.args.build_type.lower() != "release":
            symbol_path = os.path.join(self.args.core_root, "PDB")
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

        if self.args.host_os == "windows":
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
REM This wrapper is automatically generated by run.py. It includes the
REM necessary environment to reproduce a failure that occured during running
REM the tests.
REM
REM In order to change how this wrapper is generated, see
REM run.py:__create_batch_wrapper__(). Please note that it is possible
REM to recreate this file by running src/tests/run.py --analyze_results_only
REM with the appropriate environment set and the correct arch and build_type
REM passed.
REM
REM ============================================================================

REM Set Core_Root if it has not been already set.
if "%%CORE_ROOT%%"=="" set CORE_ROOT=%s

echo Core_Root is set to: "%%CORE_ROOT%%"

""" % (self.unique_name, self.args.core_root)

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
# This wrapper is automatically generated by run.py. It includes the
# necessary environment to reproduce a failure that occured during running
# the tests.
#
# In order to change how this wrapper is generated, see
# run.py:__create_bash_wrapper__(). Please note that it is possible
# to recreate this file by running src/tests/run.py --analyze_results_only
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

""" % (self.unique_name, self.args.core_root)

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
            it will be put into an "unstable" folder under artifacts/repro.
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
        test_env to be passed for run.cmd. Note that this only happens
        on windows, until xunit is used on unix there is no managed code run
        in run.sh.
    """
    global gc_stress

    ret_code = 0

    complus_vars = defaultdict(lambda: None)

    for key in env:
        value = env[key]
        if "complus" in key.lower() or "superpmi" in key.lower():
            complus_vars[key] = value

    if len(list(complus_vars.keys())) > 0:
        print("Found COMPlus variables in the current environment")
        print("")

        contents = ""

        # We can't use:
        #
        #   with tempfile.NamedTemporaryFile() as test_env:
        #       ...
        #       return func(...)
        #
        # because on Windows Python locks the file, and trying to use it give you:
        #
        #    The process cannot access the file because it is being used by another process.
        #
        # errors.

        tempfile_suffix = ".bat" if _os == "windows" else ""
        test_env = tempfile.NamedTemporaryFile(mode="w", suffix=tempfile_suffix, delete=False)
        try:
            file_header = None

            if _os == "windows":
                file_header = \
"""@REM Temporary test env for test run.
@echo on
"""
            else:
                file_header = \
"""# Temporary test env for test run.
"""

            test_env.write(file_header)
            contents += file_header

            for key in complus_vars:
                value = complus_vars[key]
                command = None
                if _os == "windows":
                    command = "set"
                else:
                    command = "export"

                if key.lower() == "complus_gcstress":
                    gc_stress = True

                print("Unset %s" % key)
                os.environ[key] = ""

                # \n below gets converted to \r\n on Windows because the file is opened in text (not binary) mode

                line = "%s %s=%s\n" % (command, key, value)
                test_env.write(line)

                contents += line

            if _os == "windows":
                file_suffix = \
"""@echo off
"""
                test_env.write(file_suffix)
                contents += file_suffix

            test_env.close()

            print("")
            print("TestEnv: %s" % test_env.name)
            print("")
            print("Contents:")
            print("")
            print(contents)
            print("")

            ret_code = func(test_env.name)

        finally:
            os.remove(test_env.name)

    else:
        ret_code = func(None)

    return ret_code

def get_environment(test_env=None):
    """ Get all the COMPlus_* Environment variables

    Notes:
        All COMPlus variables need to be captured as a test_env script to avoid
        influencing the test runner.

        On Windows, os.environ keys (the environment variable names) are all upper case,
        and map lookup is case-insensitive on the key.
    """
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

    return complus_vars

def call_msbuild(args):
    """ Call msbuild to run the tests built.

    Args:
        args

    Notes:
        At this point the environment should be setup correctly, including
        the test_env, should it need to be passed.

    """
    global g_verbose

    common_msbuild_arguments = []

    if args.sequential:
        common_msbuild_arguments += ["/p:ParallelRun=none"]

    if not os.path.isdir(args.logs_dir):
        os.makedirs(args.logs_dir)

    # Set up the directory for MSBuild debug logs.
    msbuild_debug_logs_dir = os.path.join(args.logs_dir, "MsbuildDebugLogs")
    if not os.path.isdir(msbuild_debug_logs_dir):
        os.makedirs(msbuild_debug_logs_dir)
    os.environ["MSBUILDDEBUGPATH"] = msbuild_debug_logs_dir

    command =   [args.dotnetcli_script_path,
                 "msbuild",
                 os.path.join(args.coreclr_tests_src_dir, "run.proj"),
                 "/p:Runtests=true",
                 "/clp:showcommandline"]

    command += common_msbuild_arguments

    if args.il_link:
        command += ["/p:RunTestsViaIllink=true"]

    if args.limited_core_dumps:
        command += ["/p:LimitedCoreDumps=true"]

    log_path = os.path.join(args.logs_dir, "TestRunResults_%s_%s_%s" % (args.host_os, args.arch, args.build_type))
    build_log = log_path + ".log"
    wrn_log = log_path + ".wrn"
    err_log = log_path + ".err"

    command += ["/fileloggerparameters:\"Verbosity=normal;LogFile=%s\"" % build_log,
                "/fileloggerparameters1:\"WarningsOnly;LogFile=%s\"" % wrn_log,
                "/fileloggerparameters2:\"ErrorsOnly;LogFile=%s\"" % err_log,
                "/consoleloggerparameters:Summary"]

    if g_verbose:
        command += ["/verbosity:diag"]

    command += ["/p:TargetOS=%s" % args.host_os,
                "/p:TargetArchitecture=%s" % args.arch,
                "/p:Configuration=%s" % args.build_type,
                "/p:__LogsDir=%s" % args.logs_dir]

    command += ["/bl:%s.binlog" % (log_path)]

    print(" ".join(command))

    sys.stdout.flush() # flush output before creating sub-process
    proc = subprocess.Popen(command)

    try:
        proc.communicate()
    except:
        proc.kill()
        sys.exit(1)

    if args.limited_core_dumps:
        inspect_and_delete_coredump_files(args.host_os, args.arch, args.test_location)

    return proc.returncode

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

    if isinstance(coredump_pattern, bytes):
        print("Binary data found. Decoding.")
        coredump_pattern = coredump_pattern.decode('ascii')

    print("CoreDump Pattern: {}".format(coredump_pattern))

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
    # directory and deletes them immediately.

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

def run_tests(args,
              test_env_script_path=None):
    """ Run the coreclr tests

    Args:
        args
        test_env_script_path  : Path to script to use to set the test environment, if any.
    """

    if args.skip_test_run:
        return

    # Set default per-test timeout to 15 minutes (in milliseconds).
    per_test_timeout = 15*60*1000

    # Setup the environment
    if args.long_gc:
        print("Running Long GC Tests, extending timeout to 20 minutes.")
        per_test_timeout = 20*60*1000
        print("Setting RunningLongGCTests=1")
        os.environ["RunningLongGCTests"] = "1"

    if args.gcsimulator:
        print("Running GCSimulator tests, extending timeout to one hour.")
        per_test_timeout = 60*60*1000
        print("Setting RunningGCSimulatorTests=1")
        os.environ["RunningGCSimulatorTests"] = "1"

    if args.ilasmroundtrip:
        print("Running ILasm round trip.")
        print("Setting RunningIlasmRoundTrip=1")
        os.environ["RunningIlasmRoundTrip"] = "1"

    if args.run_crossgen2_tests:
        print("Running tests R2R (Crossgen2)")
        print("Setting RunCrossGen2=true")
        os.environ["RunCrossGen2"] = "true"

    if args.large_version_bubble:
        print("Large Version Bubble enabled")
        os.environ["LargeVersionBubble"] = "true"

    if gc_stress:
        print("Running GCStress, extending timeout to 120 minutes.")
        per_test_timeout = 120*60*1000

    if args.limited_core_dumps:
        setup_coredump_generation(args.host_os)

    if args.run_in_context:
        print("Running test in an unloadable AssemblyLoadContext")
        os.environ["CLRCustomTestLauncher"] = args.runincontext_script_path
        os.environ["RunInUnloadableContext"] = "1";
        per_test_timeout = 20*60*1000

    if args.tiering_test:
        print("Running test repeatedly to promote methods to tier1")
        os.environ["CLRCustomTestLauncher"] = args.tieringtest_script_path

    # Set __TestTimeout environment variable, which is the per-test timeout in milliseconds.
    # This is read by the test wrapper invoker, in src\tests\Common\Coreclr.TestWrapper\CoreclrTestWrapperLib.cs.
    print("Setting __TestTimeout=%s" % str(per_test_timeout))
    os.environ["__TestTimeout"] = str(per_test_timeout)

    # Set CORE_ROOT
    print("Setting CORE_ROOT=%s" % args.core_root)
    os.environ["CORE_ROOT"] = args.core_root

    # Set __TestDotNetCmd so tests which need to run dotnet can use the repo-local script on dev boxes
    os.environ["__TestDotNetCmd"] = args.dotnetcli_script_path

    # Set test env script path if it is set.
    if test_env_script_path is not None:
        print("Setting __TestEnv=%s" % test_env_script_path)
        os.environ["__TestEnv"] = test_env_script_path

    return call_msbuild(args)

def setup_args(args):
    """ Setup the args based on the argparser obj

    Args:
        args(ArgParser): Parsed arguments

    Notes:
        If there is no core_root, or test location passed, create a default
        location using the build type and the arch.
    """

    requires_coreroot = args.host_os != "Browser" and args.host_os != "Android"
    coreclr_setup_args = CoreclrArguments(args,
                                          require_built_test_dir=True,
                                          require_built_core_root=requires_coreroot,
                                          require_built_product_dir=False)

    normal_location = os.path.join(coreclr_setup_args.artifacts_location, "tests", "coreclr", "%s.%s.%s" % (coreclr_setup_args.host_os, coreclr_setup_args.arch, coreclr_setup_args.build_type))

    # If we have supplied our own test location then we need to create a test location
    # that the scripting will expect. As it is now, there is a dependency on the
    # test location being under test/<os>.<build_type>.<arch>

    # Make sure that we are using the correct build_type. This is a test drop, it is possible
    # that we are inferring the build type to be Debug incorrectly.
    if coreclr_setup_args.build_type not in coreclr_setup_args.test_location:
            # Remove punctuation
            corrected_build_type = re.sub("[%s]" % string.punctuation, "", coreclr_setup_args.test_location.split(".")[-1])
            coreclr_setup_args.verify(corrected_build_type,
                                      "build_type",
                                      coreclr_setup_args.check_build_type,
                                      "Unsupported configuration: %s.\nSupported configurations: %s" % (corrected_build_type, ", ".join(coreclr_setup_args.valid_build_types)))

    if coreclr_setup_args.test_location is not None and coreclr_setup_args.test_location != normal_location:
        print("Error, msbuild currently expects tests in {} (got test_location {})".format(normal_location, coreclr_setup_args.test_location))
        raise Exception("Error, msbuild currently expects tests in artifacts/tests/...")

    coreclr_setup_args.verify(args,
                              "test_env",
                              lambda arg: True,
                              "Error setting test_env")

    coreclr_setup_args.verify(args,
                              "analyze_results_only",
                              lambda arg: True,
                              "Error setting analyze_results_only")

    coreclr_setup_args.verify(args,
                              "rid",
                              lambda arg: True,
                              "Error setting rid")

    coreclr_setup_args.verify(args,
                              "il_link",
                              lambda arg: True,
                              "Error setting il_link")

    coreclr_setup_args.verify(args,
                              "long_gc",
                              lambda arg: True,
                              "Error setting long_gc")

    coreclr_setup_args.verify(args,
                              "gcsimulator",
                              lambda arg: True,
                              "Error setting gcsimulator")

    coreclr_setup_args.verify(args,
                              "ilasmroundtrip",
                              lambda arg: True,
                              "Error setting ilasmroundtrip")

    coreclr_setup_args.verify(args,
                              "large_version_bubble",
                              lambda arg: True,
                              "Error setting large_version_bubble")

    coreclr_setup_args.verify(args,
                              "run_crossgen2_tests",
                              lambda unused: True,
                              "Error setting run_crossgen2_tests")

    coreclr_setup_args.verify(args,
                              "skip_test_run",
                              lambda arg: True,
                              "Error setting skip_test_run")

    coreclr_setup_args.verify(args,
                              "sequential",
                              lambda arg: True,
                              "Error setting sequential")

    coreclr_setup_args.verify(args,
                              "verbose",
                              lambda arg: True,
                              "Error setting verbose")

    coreclr_setup_args.verify(args,
                              "limited_core_dumps",
                              lambda arg: True,
                              "Error setting limited_core_dumps")

    coreclr_setup_args.verify(args,
                              "run_in_context",
                              lambda arg: True,
                              "Error setting run_in_context")

    coreclr_setup_args.verify(args,
                              "tiering_test",
                              lambda arg: True,
                              "Error setting tiering_test")

    print("host_os                  : %s" % coreclr_setup_args.host_os)
    print("arch                     : %s" % coreclr_setup_args.arch)
    print("build_type               : %s" % coreclr_setup_args.build_type)
    print("runtime_repo_location    : %s" % coreclr_setup_args.runtime_repo_location)
    print("core_root                : %s" % coreclr_setup_args.core_root)
    print("test_location            : %s" % coreclr_setup_args.test_location)

    coreclr_setup_args.corerun_path = os.path.join(coreclr_setup_args.core_root, "corerun%s" % (".exe" if coreclr_setup_args.host_os == "windows" else ""))
    coreclr_setup_args.dotnetcli_script_path = os.path.join(coreclr_setup_args.runtime_repo_location, "dotnet%s" % (".cmd" if coreclr_setup_args.host_os == "windows" else ".sh"))
    coreclr_setup_args.coreclr_tests_dir = os.path.join(coreclr_setup_args.coreclr_dir, "tests")
    coreclr_setup_args.coreclr_tests_src_dir = os.path.join(coreclr_setup_args.runtime_repo_location, "src", "tests")
    coreclr_setup_args.runincontext_script_path = os.path.join(coreclr_setup_args.coreclr_tests_src_dir, "Common", "scripts", "runincontext%s" % (".cmd" if coreclr_setup_args.host_os == "windows" else ".sh"))
    coreclr_setup_args.tieringtest_script_path = os.path.join(coreclr_setup_args.coreclr_tests_src_dir, "Common", "scripts", "tieringtest%s" % (".cmd" if coreclr_setup_args.host_os == "windows" else ".sh"))
    coreclr_setup_args.logs_dir = os.path.join(coreclr_setup_args.artifacts_location, "log")

    return coreclr_setup_args

if sys.version_info.major < 3:
    def to_unicode(s):
        return unicode(s, "utf-8")
else:
    def to_unicode(s):
        return s

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
        print("Warning: couldn't find test: %s" % test_name)
        return None

    assert(os.path.isfile(location))

    return location

def parse_test_results(args):
    """ Parse the test results for test execution information

    Args:
        args                 : arguments
    """
    log_path = os.path.join(args.logs_dir, "TestRunResults_%s_%s_%s" % (args.host_os, args.arch, args.build_type))
    print("Parsing test results from (%s)" % log_path)

    test_run_location = os.path.join(args.logs_dir, "testRun.xml")

    if not os.path.isfile(test_run_location):
        # Check if this is a casing issue

        found = False
        for item in os.listdir(args.logs_dir):
            item_lower = item.lower()
            if item_lower == "testrun.xml":
                # Correct the name.
                os.rename(os.path.join(args.logs_dir, item), test_run_location)
                found = True
                break

        if not found:
            print("Unable to find testRun.xml. This normally means the tests did not run.")
            print("It could also mean there was a problem logging. Please run the tests again.")
            return

    print("Analyzing {}".format(test_run_location))
    assemblies = xml.etree.ElementTree.parse(test_run_location).getroot()

    tests = defaultdict(lambda: None)
    for assembly in assemblies:
        for collection in assembly:
            if collection.tag == "errors" and collection.text != None:
                # Something went wrong during running the tests.
                print("Error running the tests, please run run.py again.")
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
                    test_output = failure_info.text

                test_location_on_filesystem = find_test_from_name(args.host_os, args.test_location, test_name)
                if test_location_on_filesystem is not None:
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
            #test_output = test_output.replace("\\r", "\r")
            #test_output = test_output.replace("\\n", "\n")
            #test_output = test_output.replace("/r", "\r")
            #test_output = test_output.replace("/n", "\n")

            # Replace CR/LF by just LF; Python "print", below, will map as necessary on the platform.
            # If we don't do this, then Python on Windows will convert \r\n to \r\r\n on output.
            test_output = test_output.replace("\r\n", "\n")

            unicode_output = None
            if sys.version_info < (3,0):
                # Handle unicode characters in output in python2.*
                try:
                    unicode_output = unicode(test_output, "utf-8")
                except:
                    print("Error: failed to convert Unicode output")
            else:
                unicode_output = test_output

            if unicode_output is not None:
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

def create_repro(args, env, tests):
    """ Go through the failing tests and create repros for them

    Args:
        args
        env
        tests (defaultdict[String]: { }): The tests that were reported by
                                        : xunit

    """
    assert tests is not None

    failed_tests = [tests[item] for item in tests if tests[item]["failed"] == "1"]
    if len(failed_tests) == 0:
        return

    repro_location = os.path.join(args.artifacts_location, "repro", "%s.%s.%s" % (args.host_os, args.arch, args.build_type))
    if os.path.isdir(repro_location):
        shutil.rmtree(repro_location)

    print("")
    print("Creating repro files at: %s" % repro_location)

    os.makedirs(repro_location)
    assert os.path.isdir(repro_location)

    # Now that the repro_location exists under <runtime>/artifacts/repro
    # create wrappers which will simply run the test with the correct environment
    for test in failed_tests:
        debug_env = DebugEnv(args, env, test)
        debug_env.write_repro()

    print("Repro files written.")

################################################################################
# Main
################################################################################

def main(args):
    global g_verbose
    g_verbose = args.verbose
    ret_code = 0

    args = setup_args(args)

    env = get_environment(test_env=args.test_env)
    if not args.analyze_results_only:
        if args.test_env is not None:
            ret_code = run_tests(args, args.test_env)
        else:
            ret_code = create_and_use_test_env(args.host_os,
                                               env,
                                               lambda test_env_script_path: run_tests(args, test_env_script_path))
        print("Test run finished.")

    if not args.skip_test_run:
        tests = parse_test_results(args)
        if tests is not None:
            print_summary(tests)
            create_repro(args, env, tests)

    return ret_code

################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
