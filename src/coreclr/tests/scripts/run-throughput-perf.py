#!/usr/bin/env python
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
##########################################################################
##########################################################################
#
# Module: run-throughput-tests.py
#
# Notes: runs throughput testing for coreclr and uploads the timing results
#        to benchview
#
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
import time
import timeit
import stat
import csv

##########################################################################
# Globals
##########################################################################

# List of dlls we want to exclude
dll_exclude_list = {
    'All': [
        # Require Newtonsoft.Json
        "Microsoft.DotNet.ProjectModel.dll",
        "Microsoft.Extensions.DependencyModel.dll",
        # Require System.Security.Principal.Windows
        "System.Net.Requests.dll",
        "System.Net.Security.dll",
        "System.Net.Sockets.dll",
        # Moving target. Not a reliable measurement
        "System.Private.CoreLib.dll",
        # Reference and forwarding assemblies
        "System.AppContext.dll",
        "System.Diagnostics.Contracts.dll",
        "System.Dynamic.Runtime.dll",
        "System.Globalization.dll",
        "System.Globalization.Calendars.dll",
        "System.IO.dll",
        "System.IO.FileSystem.Primitives.dll",
        "System.Reflection.dll",
        "System.Reflection.Emit.dll",
        "System.Reflection.Emit.ILGeneration.dll",
        "System.Reflection.Emit.Lightweight.dll",
        "System.Reflection.Extensions.dll",
        "System.Reflection.Primitives.dll",
        "System.Resources.ResourceManager.dll",
        "System.Runtime.Handles.dll",
        "System.Runtime.Loader.dll",
        "System.Runtime.Serialization.Json.dll",
        "System.Runtime.Serialization.Xml.dll",
        "System.Security.Principal.dll",
        "System.Text.Encoding.dll",
        "System.Text.Encoding.Extensions.dll",
        "System.Threading.ThreadPool.dll",
        "System.Threading.Timer.dll",
        "System.Xml.ReaderWriter.dll",
        "System.Xml.XDocument.dll",
        "System.Xml.XmlDocument.dll",
        "System.Xml.XmlSerializer.dll",
        "System.Xml.XPath.dll"
    ],
    'Windows_NT': [
    ],
    'Linux' : [
    ]
}

jit_list = {
    'Windows_NT': {
        'x64': 'clrjit.dll',
        'x86': 'clrjit.dll',
        'x86lb': 'legacyjit.dll'
    },
    'Linux': {
        'x64': 'libclrjit.so',
        'arm': 'libclrjit.so'
    }
}

os_group_list = {
    'Windows_NT': 'Windows_NT',
    'Ubuntu14.04': 'Linux',
    'Ubuntu16.04': 'Linux'
}

##########################################################################
# Argument Parser
##########################################################################

description = 'Tool to collect throughtput performance data'

parser = argparse.ArgumentParser(description=description)

parser.add_argument('-arch', dest='arch', default='x64')
parser.add_argument('-configuration', dest='build_type', default='Release')
parser.add_argument('-run_type', dest='run_type', default='rolling')
parser.add_argument('-os', dest='operating_system', default='Windows_NT')
parser.add_argument('-clr_root', dest='clr_root', default=None)
parser.add_argument('-assembly_root', dest='assembly_root', default=None)
parser.add_argument('-benchview_path', dest='benchview_path', default=None)
parser.add_argument('-iterations', dest='iterations', default=5, type=int)
parser.add_argument('-opt_level', dest='opt_level', default='full_opt')
parser.add_argument('-jit_name', dest='jit_name', default='ryujit')
parser.add_argument('-nopgo', dest='no_pgo', default=False, action='store_true')

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
    run_type = args.run_type
    operating_system = args.operating_system
    clr_root = args.clr_root
    assembly_root = args.assembly_root
    benchview_path = args.benchview_path
    iterations = args.iterations
    opt_level = args.opt_level.lower()
    jit_name = args.jit_name.lower()
    no_pgo = args.no_pgo

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

    valid_archs = {'Windows_NT': ['x86', 'x64'], 'Linux': ['x64', 'arm']}
    valid_build_types = ['Release']
    valid_run_types = ['rolling', 'private']
    valid_os = ['Windows_NT', 'Ubuntu14.04', 'Ubuntu16.04']
    valid_opt_levels = ['full_opt', 'min_opt']
    valid_jit_names = ['ryujit']

    arch = next((a for a in valid_archs if a.lower() == arch.lower()), arch)
    build_type = next((b for b in valid_build_types if b.lower() == build_type.lower()), build_type)

    validate_arg(operating_system, lambda item: item in valid_os)

    os_group = os_group_list[operating_system]

    validate_arg(arch, lambda item: item in valid_archs[os_group])
    validate_arg(build_type, lambda item: item in valid_build_types)
    validate_arg(run_type, lambda item: item in valid_run_types)
    validate_arg(iterations, lambda item: item > 0)
    validate_arg(opt_level, lambda item: item in valid_opt_levels)
    validate_arg(jit_name, lambda item: item in valid_jit_names)

    if clr_root is None:
        raise Exception('--clr_root must be set')
    else:
        clr_root = os.path.normpath(clr_root)
        validate_arg(clr_root, lambda item: os.path.isdir(clr_root))

    if assembly_root is None:
        raise Exception('--assembly_root must be set')
    else:
        assembly_root = os.path.normpath(assembly_root)
        validate_arg(assembly_root, lambda item: os.path.isdir(assembly_root))

    if not benchview_path is None:
        benchview_path = os.path.normpath(benchview_path)
        validate_arg(benchview_path, lambda item: os.path.isdir(benchview_path))

    args = (arch, operating_system, os_group, build_type, run_type, clr_root, assembly_root, benchview_path, iterations, opt_level, jit_name, no_pgo)

    # Log configuration
    log('Configuration:')
    log(' arch: %s' % arch)
    log(' os: %s' % operating_system)
    log(' os_group: %s' % os_group)
    log(' build_type: %s' % build_type)
    log(' opt_level: %s' % opt_level)
    log(' jit_name: %s' % jit_name)
    log(' no_pgo: %s' % no_pgo)
    log(' run_type: %s' % run_type)
    log(' iterations: %d' % iterations)
    log(' clr_root: %s' % clr_root)
    log(' assembly_root: %s' % assembly_root)
    if not benchview_path is None:
        log('benchview_path : %s' % benchview_path)

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

def del_rw(action, name, exc):
    os.chmod(name, stat.S_IWRITE)
    os.remove(name)

def log(message):
    """ Print logging information
    Args:
        message (str): message to be printed
    """

    print('[%s]: %s' % (sys.argv[0], message))

def generateCSV(dll_name, dll_runtimes):
    """ Write throuput performance data to a csv file to be consumed by measurement.py
    Args:
        dll_name (str): the name of the dll
        dll_runtimes (float[]): A list of runtimes for each iteration of the performance test
    """

    csv_file_name = "throughput-%s.csv" % (dll_name)
    csv_file_path = os.path.join(os.getcwd(), csv_file_name)

    with open(csv_file_path, 'w') as csvfile:
        output_file = csv.writer(csvfile, delimiter=',', lineterminator='\n')

        for iteration in dll_runtimes:
            output_file.writerow(["default", "coreclr-crossgen-tp", dll_name, iteration])

    return csv_file_name

def runIterations(dll_name, dll_path, iterations, crossgen_path, jit_path, assemblies_path, opt_level, jit_name):
    """ Run throughput testing for a given dll
    Args:
        dll_name: the name of the dll
        dll_path: the path to the dll
        iterations: the number of times to run crossgen on the dll
        crossgen_path: the path to crossgen
        jit_path: the path to the jit
        assemblies_path: the path to the assemblies that may be needed for the crossgen run
    Returns:
        dll_elapsed_times: a list of the elapsed times for the dll
    """

    dll_elapsed_times = []

    # Set up arguments for running crossgen
    run_args = [crossgen_path,
            '/JITPath',
            jit_path,
            '/Platform_Assemblies_Paths',
            assemblies_path,
            dll_path
            ]

    my_env = os.environ

    if opt_level == 'min_opt':
        my_env['COMPlus_JITMinOpts'] = '1'

    log(" ".join(run_args))

    # Time.clock() returns seconds, with a resolution of 0.4 microseconds, so multiply by the multiplier to get milliseconds
    multiplier = 1000

    for iteration in range(0,iterations + 1):
        proc = subprocess.Popen(run_args, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=my_env)

        start_time = timeit.default_timer()
        (out, err) = proc.communicate()
        end_time = timeit.default_timer()

        if proc.returncode == 0:
            # Calculate the runtime
            elapsed_time = (end_time - start_time) * multiplier
            dll_elapsed_times.append(elapsed_time)
        else:
            log("Error in %s" % (dll_name))
            log(err.decode("utf-8"))

    return dll_elapsed_times

##########################################################################
# Main
##########################################################################

def main(args):
    global dll_exclude_list
    global jit_list
    global os_group_list

    architecture, operating_system, os_group, build_type, run_type, clr_root, assembly_root, benchview_path, iterations, opt_level, jit_name, no_pgo = validate_args(args)
    arch = architecture

    pgo_string = 'pgo'

    if no_pgo:
        pgo_string = 'nopgo'

    current_dir = os.getcwd()
    jit = jit_list[os_group][architecture]
    crossgen = 'crossgen'

    if os_group == 'Windows_NT':
        crossgen += '.exe'

    # Make sandbox
    sandbox_path = os.path.join(clr_root, "sandbox")
    if os.path.isdir(sandbox_path):
        shutil.rmtree(sandbox_path, onerror=del_rw)

    os.makedirs(sandbox_path)
    os.chdir(sandbox_path)

    # Set up paths
    bin_path = os.path.join(clr_root, 'bin', 'Product',  os_group + '.' + arch + '.' + build_type)

    crossgen_path = os.path.join(bin_path,crossgen)
    jit_path = os.path.join(bin_path, jit)

    # Replace assembly_root's System.Private.CoreLib with built System.Private.CoreLib.
    shutil.copyfile(os.path.join(bin_path, 'System.Private.CoreLib.dll'), os.path.join(assembly_root, 'System.Private.CoreLib.dll'))

    python_exe = sys.executable

    # Run throughput testing
    for dll_file_name in os.listdir(assembly_root):
        # Find all framework dlls in the assembly_root dir, which we will crossgen
        if (dll_file_name.endswith(".dll") and
                (not ".ni." in dll_file_name) and
                ("Microsoft" in dll_file_name or "System" in dll_file_name) and
                (not dll_file_name in dll_exclude_list[os_group]) and
                (not dll_file_name in dll_exclude_list["All"])):
            dll_name = dll_file_name.replace(".dll", "")
            dll_path = os.path.join(assembly_root, dll_file_name)
            dll_elapsed_times = runIterations(dll_file_name, dll_path, iterations, crossgen_path, jit_path, assembly_root, opt_level, jit_name)

            if len(dll_elapsed_times) != 0:
                if not benchview_path is None:
                    # Generate the csv file
                    csv_file_name = generateCSV(dll_name, dll_elapsed_times)
                    shutil.copy(csv_file_name, clr_root)

                    # For each benchmark, call measurement.py
                    measurement_args = [python_exe,
                            os.path.join(benchview_path, "measurement.py"),
                            "csv",
                            os.path.join(os.getcwd(), csv_file_name),
                            "--metric",
                            "execution_time",
                            "--unit",
                            "milliseconds",
                            "--better",
                            "desc",
                            "--drop-first-value",
                            "--append"]
                    log(" ".join(measurement_args))
                    proc = subprocess.Popen(measurement_args)
                    proc.communicate()
                else:
                    # Write output to console if we are not publishing
                    log("%s" % (dll_name))
                    log("Duration: [%s]" % (", ".join(str(x) for x in dll_elapsed_times[1:])))

    # Upload the data
    if not benchview_path is None:
        # Call submission.py
        submission_args = [python_exe,
                os.path.join(benchview_path, "submission.py"),
                "measurement.json",
                "--build",
                os.path.join(clr_root, "build.json"),
                "--machine-data",
                os.path.join(clr_root, "machinedata.json"),
                "--metadata",
                os.path.join(clr_root, "submission-metadata.json"),
                "--group",
                "CoreCLR-throughput",
                "--type",
                run_type,
                "--config-name",
                build_type,
                "--config",
                "Configuration",
                build_type,
                "--config",
                "OS",
                operating_system,
                "--config",
                "OptLevel",
                opt_level,
                "--config",
                "JitName",
                jit_name,
                "--config",
                "PGO",
                pgo_string,
                "--architecture",
                architecture,
                "--machinepool",
                "PerfSnake"
                ]
        log(" ".join(submission_args))
        proc = subprocess.Popen(submission_args)
        proc.communicate()
        if proc.returncode != 0:
            os.chdir(current_dir)
            return -1

        # Call upload.py
        upload_args = [python_exe,
                os.path.join(benchview_path, "upload.py"),
                "submission.json",
                "--container",
                "coreclr"
                ]
        log(" ".join(upload_args))
        proc = subprocess.Popen(upload_args)
        proc.communicate()
        if proc.returncode != 0:
            os.chdir(current_dir)
            return -1

    os.chdir(current_dir)

    return 0

if __name__ == "__main__":
    Args = parser.parse_args(sys.argv[1:])
    main(Args)
