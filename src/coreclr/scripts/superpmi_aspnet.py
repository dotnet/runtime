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
import shutil
import sys
import zipfile

from os import path
from coreclr_arguments import *
from superpmi import TempDir, determine_mcs_tool_path, determine_superpmi_tool_path, is_nonzero_length_file
from jitutil import run_command

# Start of parser object creation.
is_windows = platform.system() == "Windows"
parser = argparse.ArgumentParser(description="description")

parser.add_argument("-source_directory", help="path to source directory")
parser.add_argument("-output_mch_path", help="Absolute path to the mch file to produce")
parser.add_argument("-arch", help="Architecture")

def setup_args(args):
    """ Setup the args for SuperPMI to use.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    coreclr_args = CoreclrArguments(args, require_built_core_root=True, require_built_product_dir=True,
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
                        "arch",
                        lambda arch: arch.lower() in ["x64", "arm64"],
                        "Unable to set arch")

    return coreclr_args


def determine_native_name(coreclr_args, base_lib_name, target_os):
    """ Determine the name of the native lib based on the OS.

    Args:
        coreclr_args (CoreclrArguments): parsed args
        base_lib_name (str) : root name of the lib
        target_os (str) : os to run tests on
    Return:
        (str) : name of the native lib for this OS
    """

    if target_os == "OSX":
        return "lib" + base_lib_name + ".dylib"
    elif target_os == "Linux":
        return "lib" + base_lib_name + ".so"
    elif target_os == "windows":
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
#            return "aspnet-perf-win"
            return "aspnet-citrine-win"
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
    source_directory = coreclr_args.source_directory
    target_arch = coreclr_args.arch
    target_os = coreclr_args.host_os

    checked_root = path.join(source_directory, "artifacts", "bin", "coreclr", target_os + "." + coreclr_args.arch + ".Checked")
    release_root = path.join(source_directory, "artifacts", "bin", "coreclr", target_os + "." + coreclr_args.arch + ".Release")

    # We'll use repo script to install dotnet
    dotnet_install_script_name = "dotnet-install.cmd" if is_windows else "dotnet-install.sh"
    dotnet_install_script_path = path.join(source_directory, "eng", "common", dotnet_install_script_name)

    with TempDir(skip_cleanup=True) as temp_location:

        print ("Executing in " + temp_location)

        # install dotnet 6.0
        run_command([dotnet_install_script_path, "-Version", "6.0.4"], temp_location, _exit_on_fail=True)
        os.environ['DOTNET_MULTILEVEL_LOOKUP'] = '0'
        os.environ['DOTNET_SKIP_FIRST_TIME_EXPERIENCE'] = '1'
        dotnet_path = path.join(source_directory, ".dotnet")
        dotnet_exe = path.join(dotnet_path, "dotnet.exe") if is_windows else path.join(dotnet_path, "dotnet")
        run_command([dotnet_exe, "--info"], temp_location, _exit_on_fail=True)
        os.environ['DOTNET_ROOT'] = dotnet_path

        ## install crank as local tool
        run_command(
            [dotnet_exe, "tool", "install", "Microsoft.Crank.Controller", "--version", "0.2.0-*", "--tool-path", temp_location], _exit_on_fail=True)

        ## ideally just do sparse clone, but this doesn't work locally
        ## git clone --filter=blob:none --no-checkout https://github.com/aspnet/benchmarks
        ## cd benchmarks
        ## git sparse-checkout init --cone
        ## git sparse-checkout set scenarios

        ## could probably just pass a URL and avoid this

        run_command(
            ["git", "clone", "--quiet", "--depth", "1", "https://github.com/aspnet/benchmarks"], temp_location, _exit_on_fail=True)

        crank_app = path.join(temp_location, "crank")
        mcs_path = determine_mcs_tool_path(coreclr_args)
        superpmi_path = determine_superpmi_tool_path(coreclr_args)

        # todo: add grpc/signalr, perhaps

        configname_scenario_list = [
                                    ("platform", "plaintext"),
                                    ("json", "json"),
                                    ("plaintext", "mvc"),
                                    ("database", "fortunes_dapper"),
                                    ("database", "fortunes_ef_mvc_https"),
                                    ("database", "updates"),
                                    ("proxy", "proxy-yarp"),
                                    ("staticfiles", "static"),
                                    ("websocket", "websocket"),
                                    ("orchard", "about-sqlite")
                                    ]

        # configname_scenario_list = [("platform", "plaintext")]

        # note tricks to get one element tuples

        runtime_options_list = [("Dummy=0",), ("TieredCompilation=0", ), ("TieredPGO=1",), ("TieredPGO=1", "ReadyToRun=0"),
            ("ReadyToRun=0", "OSR_HitLimit=0", "TC_OnStackReplacement_InitialCounter=10"),
            ("TieredPGO=1", "ReadyToRun=0", "OSR_HitLimit=0", "TC_OnStackReplacement_InitialCounter=10")]

        # runtime_options_list = [("TieredCompilation=0", )]

        mch_file = path.join(coreclr_args.output_mch_path, "aspnet.run." + target_os + "." + target_arch + ".checked.mch")
        benchmark_machine = determine_benchmark_machine(coreclr_args)

        jitname = determine_native_name(coreclr_args, "clrjit", target_os)
        coreclrname = determine_native_name(coreclr_args, "coreclr", target_os)
        spminame = determine_native_name(coreclr_args, "superpmi-shim-collector", target_os)
        corelibname = "System.Private.CoreLib.dll"

        jitpath = path.join(".", jitname)
        jitlib  = path.join(checked_root, jitname)
        coreclr = path.join(release_root, coreclrname)
        corelib = path.join(release_root, corelibname)
        spmilib = path.join(checked_root, spminame)

        for (configName, scenario) in configname_scenario_list:
            configYml = configName + ".benchmarks.yml"
            configFile = path.join(temp_location, "benchmarks", "scenarios", configYml)

            crank_arguments = ["--config", configFile,
                               "--profile", benchmark_machine,
                               "--scenario", scenario,
                               "--application.framework", "net7.0",
                               "--application.channel", "edge",
                               "--application.sdkVersion", "latest",
                               "--application.environmentVariables", "COMPlus_JitName=" + spminame,
                               "--application.environmentVariables", "SuperPMIShimLogPath=.",
                               "--application.environmentVariables", "SuperPMIShimPath=" + jitpath,
                               "--application.environmentVariables", "COMPlus_EnableExtraSuperPmiQueries=1",
                               "--application.options.downloadFiles", "*.mc",
                               "--application.options.displayOutput", "true",
#                               "--application.options.dumpType", "full",
#                               "--application.options.fetch", "true",
                               "--application.options.outputFiles", spmilib,
                               "--application.options.outputFiles", jitlib,
                               "--application.options.outputFiles", coreclr,
                               "--application.options.outputFiles", corelib]

            for runtime_options in runtime_options_list:
                runtime_arguments = []
                for runtime_option in runtime_options:
                    runtime_arguments.append("--application.environmentVariables")
                    runtime_arguments.append("COMPlus_" + runtime_option)

                print("")
                print("================================")
                print("Config: " + configName + " scenario: " + scenario + " options: " + " ".join(runtime_options))
                print("================================")
                print("")

                description = ["--description", configName + "-" + scenario + "-" + "-".join(runtime_options)]
                subprocess.run([crank_app] + crank_arguments + description + runtime_arguments, cwd=temp_location)

        # merge
        command = [mcs_path, "-merge", "temp.mch", "*.mc", "-dedup", "-thin"]
        run_command(command, temp_location)

        # clean
        command = [superpmi_path, "-v", "ewmi", "-f", "fail.mcl", jitlib, "temp.mch"]
        run_command(command, temp_location)

        # strip
        if is_nonzero_length_file("fail.mcl"):
            print("Replay had failures, cleaning...");
            fail_file = path.join(coreclr_args.output_mch_path, "fail.mcl");
            command = [mcs_path, "-strip", "fail.mcl", "temp.mch", mch_file]
            run_command(command, temp_location)
        else:
            print("Replay was clean...");
            shutil.copy2("temp.mch", mch_file)

        # index
        command = [mcs_path, "-toc", mch_file]
        run_command(command, temp_location)

        # overall summary
        print("Merged summary for " + mch_file)
        command = [mcs_path, "-jitflags", mch_file]
        run_command(command, temp_location)

def main(main_args):
    """ Main entry point

    Args:
        main_args ([type]): Arguments to the script
    """
    print (sys.version)
    coreclr_args = setup_args(main_args)

    build_and_run(coreclr_args)

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
