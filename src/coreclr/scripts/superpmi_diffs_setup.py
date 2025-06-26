#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : superpmi_diffs_setup.py
#
# Notes:
#
# Script to setup the directory structure required to perform base-diff JIT
# measurements in CI. It creates `correlation_payload_directory` with `base`
# and `diff` directories # that contain clrjit*.dll. It figures out the baseline
# commit hash to use for a particular GitHub pull request, and downloads the
# JIT rolling build for that commit hash. It downloads the jitutils repo and
# builds the jit-analyze tool. It downloads a version of `git` to be used by
# jit-analyze.
#
################################################################################
################################################################################

import argparse
import json
import logging
import os
import urllib
import xml.etree.ElementTree as ET

from coreclr_arguments import *
from jitutil import copy_directory, set_pipeline_variable, run_command, TempDir, download_files

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", required=True, help="Architecture")
parser.add_argument("-platform", required=True, help="OS platform")
parser.add_argument("-type", required=True, help="Type of diff (asmdiffs, tpdiff, all)")
parser.add_argument("-source_directory", required=True, help="Path to the root directory of the dotnet/runtime source tree")
parser.add_argument("-checked_directory", help="Path to the directory containing built checked binaries (e.g., <source_directory>/artifacts/bin/coreclr/windows.x64.Checked)")
parser.add_argument("-release_directory", help="Path to the directory containing built release binaries (e.g., <source_directory>/artifacts/bin/coreclr/windows.x64.Release)")

is_windows = platform.system() == "Windows"
is_macos = platform.system() == "Darwin"
target_windows = True


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
                        "arch",
                        lambda unused: True,
                        "Unable to set arch")

    coreclr_args.verify(args,
                        "platform",
                        lambda unused: True,
                        "Unable to set platform")

    coreclr_args.verify(args,
                        "type",
                        lambda type: type in ["asmdiffs", "tpdiff", "all"],
                        "Invalid type \"{}\"".format)

    coreclr_args.verify(args,
                        "source_directory",
                        os.path.isdir,
                        "source_directory doesn't exist")

    coreclr_args.verify(args,
                        "checked_directory",
                        lambda unused: True,
                        "Unable to set checked_directory")

    coreclr_args.verify(args,
                        "release_directory",
                        lambda unused: True,
                        "Unable to set release_directory")

    do_asmdiffs = False
    do_tpdiff = False
    if coreclr_args.type == 'asmdiffs':
        do_asmdiffs = True
    if coreclr_args.type == 'tpdiff':
        do_tpdiff = True
    if coreclr_args.type == 'all':
        do_asmdiffs = True
        do_tpdiff = True

    use_checked = False
    use_release = False
    if do_asmdiffs:
        use_checked = True
    if do_tpdiff:
        use_release = True

    if use_checked:
        if not os.path.isdir(coreclr_args.checked_directory):
            print("checked_directory doesn't exist")
            sys.exit(1)

    if use_release:
        if not os.path.isdir(coreclr_args.release_directory):
            print("release_directory doesn't exist")
            sys.exit(1)

    global target_windows
    target_windows = coreclr_args.platform.lower() == "windows"

    return coreclr_args


def match_jit_files(full_path):
    """ Match all the JIT files that we want to copy and use.
        We don't match osx cross-compile files.
        We also don't copy the "default" clrjit.dll, since we always use the fully specified
        JITs, e.g., clrjit_win_x86_x86.dll.
        On non-Windows, don't bother copying Windows cross-targeting or arm32 cross-bitness compilers
        (we assume here everything is running on 64-bit).
    """
    file_name = os.path.basename(full_path)

    if target_windows:
        if file_name.startswith("clrjit_") and file_name.endswith(".dll"):
            return True
    else:
        if file_name.startswith("libclrjit_") and (file_name.endswith(".so") or file_name.endswith(".dylib")):
            return True

    return False


def match_superpmi_tool_files(full_path):
    """ Match all the SuperPMI tool files that we want to copy and use.
    """
    file_name = os.path.basename(full_path)

    if target_windows:
        if file_name == "superpmi.exe" or file_name == "mcs.exe":
            return True
    else:
        if file_name == "superpmi" or file_name == "mcs":
            return True

    return False


def build_jit_analyze(coreclr_args, source_directory, jit_analyze_build_directory):
    """ Build and publish jit-analyze for use by asmdiffs
    """
    try:
        with TempDir() as jitutils_directory:
            run_command(
                ["git", "clone", "--quiet", "--depth", "1", "https://github.com/dotnet/jitutils", jitutils_directory])

            # Make sure ".dotnet" directory exists, by running the script at least once
            dotnet_script_name = "dotnet.cmd" if is_windows else "dotnet.sh"
            dotnet_script_path = os.path.join(source_directory, dotnet_script_name)
            run_command([dotnet_script_path, "--info"], jitutils_directory)

            # Build jit-analyze only, and build it as a self-contained app (not framework-dependent).
            # What target RID are we building? It depends on where we're going to run this code.
            # The RID catalog is here: https://learn.microsoft.com/dotnet/core/rid-catalog.
            #   Windows x64 => win-x64
            #   Windows x86 => win-x86
            #   Windows arm64 => win-arm64
            #   Linux x64 => linux-x64
            #   Linux arm32 => linux-arm
            #   Linux arm64 => linux-arm64
            #   macOS x64 => osx-x64

            # NOTE: we currently only support running on Windows x86/x64 (we don't pass the target OS)
            RID = None
            rid_platform = coreclr_args.platform.lower()
            if rid_platform == "windows":
                rid_platform = "win"

            rid_arch = coreclr_args.arch
            RID = f"{rid_platform}-{rid_arch}"

            # Set dotnet path to run build
            os.environ["PATH"] = os.path.join(source_directory, ".dotnet") + os.pathsep + os.environ["PATH"]

            run_command([
                "dotnet",
                "publish",
                "-c", "Release",
                "--runtime", RID,
                "--self-contained",
                "--output", jit_analyze_build_directory,
                os.path.join(jitutils_directory, "src", "jit-analyze", "jit-analyze.csproj")],
                jitutils_directory)
    except PermissionError as pe_error:
        # Details: https://bugs.python.org/issue26660
        print('Ignoring PermissionError: {0}'.format(pe_error))

    jit_analyze_tool = os.path.join(jit_analyze_build_directory, "jit-analyze.exe" if is_windows else "jit-analyze")
    if not os.path.isfile(jit_analyze_tool):
        print('Error: {} not found'.format(jit_analyze_tool))
        return 1

def build_partitions(partitions_dir, do_asmdiffs, bin_path, host_bitness):
    mcs_path = os.path.join(bin_path, "mcs.exe" if is_windows else "mcs")
    if is_macos:
        # Hack: the target is arm64, but the build machine is x64. We build SPMI for x64 because of that,
        # but it exists at a different path.
        mcs_path = os.path.join(bin_path, "..", "osx.x64.Checked", "mcs")
    assert(os.path.exists(mcs_path))

    command = [mcs_path, "-printJITEEVersion"]
    proc = subprocess.Popen(command, stdout=subprocess.PIPE)
    stdout_jit_ee_version, _ = proc.communicate()
    return_code = proc.returncode
    if return_code == 0:
        jit_ee_version = stdout_jit_ee_version.decode('utf-8').strip()
        jit_ee_version = jit_ee_version.lower()
    else:
        raise Exception("Could not determine JIT-EE version")

    print("JIT-EE version determined to be {}".format(jit_ee_version))

    az_account_name = "clrjit2"
    az_superpmi_container_name = "superpmi"
    az_blob_storage_account_uri = "https://" + az_account_name + ".blob.core.windows.net/"
    az_blob_storage_superpmi_container_uri = az_blob_storage_account_uri + az_superpmi_container_name
    az_collections_root_folder = "collections"
    prefix = az_collections_root_folder + "/" + jit_ee_version
    prefix_urlencoded = urllib.parse.quote(prefix)
    list_superpmi_container_uri = az_blob_storage_superpmi_container_uri + "?restype=container&comp=list&prefix=" + prefix_urlencoded + "/"

    try:
        contents = urllib.request.urlopen(list_superpmi_container_uri).read().decode('utf-8')
    except Exception as exception:
        raise Exception("Didn't find any collections using %s", list_superpmi_container_uri)

    elem = ET.fromstring(contents)

    if not target_windows and not do_asmdiffs:
        targets = [("linux", "x64")]
    elif host_bitness == 64:
        targets = [("windows", "x64"), ("windows", "arm64"), ("linux", "x64"), ("linux", "arm64"), ("osx", "arm64")]
    else:
        targets = [("windows", "x86"), ("linux", "arm")]

    targets = [(target_os, arch, []) for (target_os, arch) in targets]

    for blob in elem.findall(".//Blob"):
        name = blob.find("Name").text
        for (target_os, arch, collections) in targets:
            name_pref = prefix + "/" + target_os + "/" + arch + "/"
            if name.startswith(name_pref) and name.removesuffix(".zip").endswith(".mch"):
                url = blob.find("Url").text
                col_name = name[len(name_pref):].removesuffix(".zip")
                collections.append({ "target_os": target_os, "target_arch": arch, "col_name": col_name, "col_url": url })

    if not os.path.exists(partitions_dir):
        os.makedirs(partitions_dir)

    for (target_os, arch, collections) in targets:
        partition_index = 0
        for col in sorted(collections, key=lambda col: col["col_name"]):
            json_path = os.path.join(partitions_dir, "{}-{}-{}.json".format(target_os, arch, partition_index))
            print("Partition {}-{}-{}: {}".format(target_os, arch, partition_index, col["col_name"]))
            partition_index += 1
            with open(json_path, "w") as file:
                file.write(json.dumps(col))

def main(main_args):
    """ Prepare the Helix data for SuperPMI diffs Azure DevOps pipeline.

    The Helix correlation payload directory is created and populated as follows:

    <source_directory>/payload -- the correlation payload directory
        -- contains the *.py scripts from <source_directory>/src/coreclr/scripts
        -- contains superpmi.exe, mcs.exe from the target-specific build
    <source_directory>/payload/base
        -- contains the baseline JITs (under checked and release folders)
    <source_directory>/payload/diff
        -- contains the diff JITs (under checked and release folders)
    For `type == asmdiffs`:
        <source_directory>/payload/jit-analyze
            -- contains the self-contained jit-analyze build (from dotnet/jitutils)
        <source_directory>/payload/git
            -- contains a Portable ("xcopy installable") `git` tool, downloaded from:
            https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/git/Git-2.32.0-64-bit.zip
            This is needed by jit-analyze to do `git diff` on the generated asm. The `<source_directory>/payload/git/cmd`
            directory is added to the PATH.
            NOTE: this only runs on Windows.

    Then, AzDO pipeline variables are set.

    Note:
    1. asmdiffs uses Checked JITs, tpdiff uses Release JITs. Only the one needed is copied to the payload directory.
    2. Only asmdiffs needs jit-analyze and git
    3. tpdiff can run on Linux, but asmdiffs is not implemented to run on Linux

    Args:
        main_args ([type]): Arguments to the script

    Returns:
        0 on success, otherwise a failure code
    """

    # Set up logging.
    logger = logging.getLogger()
    logger.setLevel(logging.INFO)
    stream_handler = logging.StreamHandler(sys.stdout)
    stream_handler.setLevel(logging.INFO)
    logger.addHandler(stream_handler)

    coreclr_args = setup_args(main_args)

    arch = coreclr_args.arch
    platform_name = coreclr_args.platform.lower()
    source_directory = coreclr_args.source_directory
    checked_directory = coreclr_args.checked_directory
    release_directory = coreclr_args.release_directory

    python_path = sys.executable

    do_asmdiffs = False
    do_tpdiff = False
    if coreclr_args.type == 'asmdiffs':
        do_asmdiffs = True
    if coreclr_args.type == 'tpdiff':
        do_tpdiff = True
    if coreclr_args.type == 'all':
        do_asmdiffs = True
        do_tpdiff = True

    use_checked = False
    use_release = False
    if do_asmdiffs:
        use_checked = True
    if do_tpdiff:
        use_release = True

    # CorrelationPayload directories
    correlation_payload_directory = os.path.join(source_directory, "payload")
    superpmi_scripts_directory = os.path.join(source_directory, 'src', 'coreclr', 'scripts')
    base_jit_directory = os.path.join(correlation_payload_directory, "base")
    diff_jit_directory = os.path.join(correlation_payload_directory, "diff")

    if use_checked:
        base_jit_checked_directory = os.path.join(base_jit_directory, "checked")
        diff_jit_checked_directory = os.path.join(diff_jit_directory, "checked")
    if use_release:
        base_jit_release_directory = os.path.join(base_jit_directory, "release")
        diff_jit_release_directory = os.path.join(diff_jit_directory, "release")

    if do_asmdiffs:
        jit_analyze_build_directory = os.path.join(correlation_payload_directory, "jit-analyze")
        git_directory = os.path.join(correlation_payload_directory, "git")

    ######## Get the portable `git` package

    if do_asmdiffs:
        git_url = "https://netcorenativeassets.blob.core.windows.net/resource-packages/external/windows/git/Git-2.32.0-64-bit.zip"

        print('Downloading {} -> {}'.format(git_url, git_directory))

        urls = [ git_url ]
        # There are too many files to be verbose in the download and copy.
        download_files(urls, git_directory, verbose=False, display_progress=False)
        git_exe_tool = os.path.join(git_directory, "cmd", "git.exe")
        if not os.path.isfile(git_exe_tool):
            print('Error: `git` not found at {}'.format(git_exe_tool))
            return 1

    ######## Get SuperPMI python scripts

    # Copy *.py to CorrelationPayload
    print('Copying {} -> {}'.format(superpmi_scripts_directory, correlation_payload_directory))
    copy_directory(superpmi_scripts_directory, correlation_payload_directory, verbose_copy=True,
                   match_func=lambda path: any(path.endswith(extension) for extension in [".py"]))

    ######## Get baseline JITs

    print("Fetching history of `main` branch so we can find the baseline JIT")
    run_command(["git", "fetch", "--depth=500", "origin", "main"], source_directory, _exit_on_fail=True)

    if use_checked:
        if not os.path.exists(base_jit_checked_directory):
            os.makedirs(base_jit_checked_directory)

        print("Running jitrollingbuild.py download to get baseline checked JIT")
        jit_rolling_build_script = os.path.join(superpmi_scripts_directory, "jitrollingbuild.py")
        _, _, return_code = run_command([
            python_path,
            jit_rolling_build_script,
            "download",
            "-arch", arch,
            "-host_os", platform_name,
            "-build_type", "checked",
            "-target_dir", base_jit_checked_directory],
            source_directory)
        if return_code != 0:
            print('{} failed with {}'.format(jit_rolling_build_script, return_code))
            return return_code

    if use_release:
        if not os.path.exists(base_jit_release_directory):
            os.makedirs(base_jit_release_directory)

        print("Running jitrollingbuild.py download to get baseline release JIT")
        jit_rolling_build_script = os.path.join(superpmi_scripts_directory, "jitrollingbuild.py")
        _, _, return_code = run_command([
            python_path,
            jit_rolling_build_script,
            "download",
            "-arch", arch,
            "-host_os", platform_name,
            "-build_type", "release",
            "-target_dir", base_jit_release_directory],
            source_directory)
        if return_code != 0:
            print('{} failed with {}'.format(jit_rolling_build_script, return_code))
            return return_code

    ######## Get diff JITs

    if use_checked:
        print('Copying checked diff binaries {} -> {}'.format(checked_directory, diff_jit_checked_directory))
        copy_directory(checked_directory, diff_jit_checked_directory, verbose_copy=True, match_func=match_jit_files)

    if use_release:
        print('Copying release diff binaries {} -> {}'.format(release_directory, diff_jit_release_directory))
        copy_directory(release_directory, diff_jit_release_directory, verbose_copy=True, match_func=match_jit_files)

    ######## Get SuperPMI tools

    # Put the SuperPMI tools directly in the root of the correlation payload directory.
    # If both use_checked and use_release are set, use the checked SuperPMI tools.

    if use_checked:
        print('Copying SuperPMI tools {} -> {}'.format(checked_directory, correlation_payload_directory))
        copy_directory(checked_directory, correlation_payload_directory, verbose_copy=True, match_func=match_superpmi_tool_files)

    elif use_release:
        print('Copying SuperPMI tools {} -> {}'.format(release_directory, correlation_payload_directory))
        copy_directory(release_directory, correlation_payload_directory, verbose_copy=True, match_func=match_superpmi_tool_files)

    ######## Clone and build jitutils: we only need jit-analyze

    if do_asmdiffs:
        build_jit_analyze(coreclr_args, source_directory, jit_analyze_build_directory)

    ######## Generate partition information

    partitions_dir = os.path.join(correlation_payload_directory, "partitions")
    build_partitions(partitions_dir, do_asmdiffs, checked_directory if use_checked else release_directory, 64 if coreclr_args.arch in ["x64", "arm64"] else 32)

    ######## Set pipeline variables

    helix_source_prefix = "official"
    creator = ""

    print('Setting pipeline variables:')
    set_pipeline_variable("CorrelationPayloadDirectory", correlation_payload_directory)
    set_pipeline_variable("Architecture", arch)
    set_pipeline_variable("Creator", creator)
    set_pipeline_variable("HelixSourcePrefix", helix_source_prefix)

    return 0


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
