#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
#
# Title: superpmi_aspnet.py
#
# Notes:
#
# Script to perform the superpmi collection for Techempower Benchmarks
# via "crank" (https://github.com/dotnet/crank)

import argparse
import logging
import sys
import zipfile

from os import path
from coreclr_arguments import *
from superpmi import TempDir, determine_mcs_tool_path, run_and_log
from superpmi_setup import run_command

# Start of parser object creation.
is_windows = platform.system() == "Windows"
parser = argparse.ArgumentParser(description="description")

parser.add_argument("-source_directory", help="path to source directory")
parser.add_argument("-core_root", help="Path to Core_Root directory")
parser.add_argument("-output_mch_path", help="Absolute path to the mch file to produce")
parser.add_argument("-log_file", help="Name of the log file")
parser.add_argument("-arch", help="Architecture")

def setup_args(args):
    """ Setup the args for SuperPMI to use.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    coreclr_args = CoreclrArguments(args, require_built_core_root=False, require_built_product_dir=False,
                                    require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "source_directory",
                        lambda source_directory: os.path.isdir(source_directory),
                        "source_directory doesn't exist")

    coreclr_args.verify(args,
                        "output_mch_path",
                        lambda output_mch_path: not os.path.isfile(output_mch_path),
                        "output_mch_path already exist")

    coreclr_args.verify(args,
                        "log_file",
                        lambda log_file: True,  # not os.path.isfile(log_file),
                        "log_file already exist")

    coreclr_args.verify(args,
                        "core_root",
                        lambda core_root: os.path.isdir(core_root),
                        "core_root doesn't exist")

    coreclr_args.verify(args,
                        "arch",
                        lambda arch: arch.lower() in ["x64", "arm64"],
                        "Unable to set arch")

    return coreclr_args


def determine_native_name(coreclr_args, base_lib_name):
    """ Determine the name of the native lib based on the OS.

    Args:
        coreclr_args (CoreclrArguments): parsed args
        base_lib_name (str) : root name of the lib

    Return:
        (str) : name of the native lib for this OS
    """

    if coreclr_args.host_os == "OSX":
        return "lib" + base_lib_name + ".dylib"
    elif coreclr_args.host_os == "Linux":
        return "lib" + base_lib_name + ".so"
    elif coreclr_args.host_os == "windows":
        return base_lib_name + ".dll"
    else:
        raise RuntimeError("Unknown OS.")

# Where there is an option, we generally target the less performant machines
# See https://github.com/aspnet/Benchmarks/tree/master/scenarios
#
def determine_benchmark_machine(coreclr_args):
    """ Determine the name of the benchmark machine to use

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        (str) : name of the benchmnark machine
    """

    if coreclr_args.arch == "x64":
        if coreclr_args.host_os == "windows":
            return "aspnet-perf-win"
        elif coreclr_args.host_os == "Linux":
            return "aspnet-perf-lin"
        else:
            raise RuntimeError("Invalid OS for x64.")
    elif coreclr_args.arch == "arm64":
        if coreclr_args.host_os == "Linux":
            return "aspnet-citrine-arm"
        else:
            raise RuntimeError("Invalid OS for arm64.")
    else:
        raise RuntimeError("Invalid arch.")

def build_and_run(coreclr_args):
    """Run perf scenarios under crank and collect data with SPMI"

    Args:
        coreclr_args (CoreClrArguments): Arguments use to drive
        output_mch_name (string): Name of output mch file name
    """
    core_root = coreclr_args.core_root
    # log_file = coreclr_args.log_file
    source_directory = coreclr_args.source_directory

    # Make sure ".dotnet" directory exists, by running the script at least once
    dotnet_script_name = "dotnet.cmd" if is_windows else "dotnet.sh"
    dotnet_script_path = path.join(source_directory, dotnet_script_name)
    run_command([dotnet_script_path, "--info"])

    with TempDir(skip_cleanup=True) as temp_location:

        print (f'Executing in {temp_location}')

        ## install crank as local tool

        run_command(
            [dotnet_script_path, "tool", "install", "Microsoft.Crank.Controller", "--version", "0.2.0-*", "--tool-path", temp_location], _exit_on_fail=True)

        ## ideally just do sparse clone, but this doesn't work locally
        ## git clone --filter=blob:none --no-checkout https://github.com/aspnet/benchmarks
        ## cd benchmarks
        ## git sparse-checkout init --cone
        ## git sparse-checkout set scenarios

        ## could probably just pass a URL and avoid this

        run_command(
            ["git.exe", "clone", "https://github.com/aspnet/benchmarks"], temp_location, _exit_on_fail=True)

        configName = "json"
        scenario = "json"
        configYml = f'{configName}.benchmarks.yml'
        configFile = path.join(temp_location, "benchmarks", "scenarios", configYml)

        # Run the scenario(s), overlaying the core runtime bits, installing SPMI, and having it write to the runtime dir.
        # and ask crank to send back the runtime directory
        #
        # crank --config {configFile}
        #       --profile {machine}
        #       --scenario {scenario}
        #       --application.framework net6.0
        #       --application.channel edge
        #       --description SPMI
        #       --application.environmentVariables COMPlus_JitName=libsuperpmi-shim-collector.so
        #       --application.environmentVariables SuperPMIShimLogPath=.
        #       --application.environmentVariables SuperPMIShimPath=./libclrjit.so
        #       --application.options.fetch true
        #       --application.options.outputFiles {build}/{superpmi-shim-collector}
        #       --application.options.outputFiles {build}/{jit}
        #       --application.options.outputFiles {build}/{coreclr}
        #       --application.options.outputFiles {build}/{SPC}

        jitname = determine_native_name(coreclr_args, "clrjit")
        coreclrname = determine_native_name(coreclr_args, "coreclr")
        spminame = determine_native_name(coreclr_args, "superpmi-shim-collector")
        corelibname = "System.Private.CoreLib.dll"

        jitpath = path.join(".", jitname)
        jitlib  = path.join(core_root, jitname)
        coreclr = path.join(core_root, coreclrname)
        corelib = path.join(core_root, corelibname)
        spmi    = path.join(core_root, spminame)

        benchmark_machine = determine_benchmark_machine(coreclr_args)

        crank_arguments = ['--config', configFile,
                           '--profile', benchmark_machine,
                           '--scenario', scenario,
                           '--application.framework', 'net6.0',
                           '--application.channel', 'edge',
                           '--application.environmentVariables', f'COMPlus_JitName={spminame}',
                           '--application.environmentVariables', 'SuperPMIShimLogPath=.',
                           '--application.environmentVariables', f'SuperPMIShimPath={jitpath}',
                           '--application.options.fetch', 'true',
                           '--application.options.outputFiles', spmi,
                           '--application.options.outputFiles', jitlib,
                           '--application.options.outputFiles', coreclr,
                           '--application.options.outputFiles', corelib]

        crank_app = path.join(temp_location, "crank")

        run_command(
            [crank_app] + crank_arguments, temp_location, _exit_on_fail=True)

        crankZipFiles = [os.path.join(temp_location, item) for item in os.listdir(temp_location) if item.endswith(".zip")]

        if len(crankZipFiles) > 0:
            for zipFile in crankZipFiles:
                with zipfile.ZipFile(zipFile, "r") as zipObject:
                    listOfFileNames = zipObject.namelist()
                    for zippedFileName in listOfFileNames:
                        if zippedFileName.endswith('.mc'):
                            zipObject.extract(zippedFileName, temp_location)

        mcs_path = determine_mcs_tool_path(coreclr_args)
        mch_file = path.join(coreclr_args.output_mch_path, f'aspnet-{configName}-{scenario}.mch')
        command = [mcs_path, "-merge", mch_file, coreclr_args.pattern, "-recursive", "-dedup", "-thin"]
        return_code = run_and_log(command)
        if return_code != 0:
            logging.error("mcs -merge Failed with code %s", return_code)

        logging.info("Creating MCT file for %s", coreclr_args.output_mch_path)
        command = [mcs_path, "-toc", coreclr_args.output_mch_path]
        return_code = run_and_log(command)
        if return_code != 0:
            logging.error("mcs -toc Failed with code %s", return_code)

def main(main_args):
    """ Main entry point

    Args:
        main_args ([type]): Arguments to the script
    """
    print (sys.version)
    coreclr_args = setup_args(main_args)

    # all_output_mch_name = path.join(coreclr_args.output_mch_path + "_all.mch")
    build_and_run(coreclr_args)
    # if os.path.isfile(all_output_mch_name):
    #     pass
    # else:
    #     print("No mch file generated.")

    # strip_unrelated_mc(coreclr_args, all_output_mch_name, coreclr_args.output_mch_path)


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
