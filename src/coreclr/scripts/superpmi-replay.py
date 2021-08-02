
jitstressregs_values = ['1', '2', '3', '4', '8', '0x10', '0x80', '0x1000']

import argparse
from os import path, system
import os
import shutil
import stat
import subprocess
import tempfile
import re

from os.path import isfile, join
from os import listdir
from coreclr_arguments import *
from superpmi_setup import copy_directory, copy_files, set_pipeline_variable
from superpmi_setup import run_command

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-arch", help="Architecture")
parser.add_argument("-partition_index", help="Partition index for logs")
parser.add_argument("-jit_directory", help="path to the directory containing clrjit binaries")
parser.add_argument("-mch_directory", help="path to the directory containing mch files")
parser.add_argument("-log_directory", help="path to the directory containing superpmi log files")


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
                        "partition_index",
                        lambda partition_index: partition_index.isnumeric(),
                        "Unable to set partition_index")

    coreclr_args.verify(args,
                        "jit_directory",
                        lambda jit_directory: os.path.isdir(jit_directory),
                        "jit_directory doesn't exist")

    coreclr_args.verify(args,
                        "mch_directory",
                        lambda mch_directory: os.path.isdir(mch_directory),
                        "mch_directory doesn't exist")

    coreclr_args.verify(args,
                        "log_directory",
                        lambda log_directory: os.path.isdir(log_directory),
                        "log_directory doesn't exist")

    return coreclr_args

def main(main_args):
    """Main entrypoint

    Args:
        main_args ([type]): Arguments to the script
    """

    python_path = sys.executable
    cwd = os.path.dirname(os.path.realpath(__file__))
    coreclr_args = setup_args(main_args)
    mch_directory = coreclr_args.mch_directory
    log_directory = coreclr_args.log_directory
    mch_filename = ''

    for f in listdir(mch_directory):
        if f.endswith(".mch.zip"):
            mch_filename = f
    
    match_mch_elems = re.search('(.*)\.(windows|Linux)\.(x64|x86|arm|arm64)\.', mch_filename)
    collection_name = os_name = match_mch_elems.group(1).replace('.','_')
    os_name = "win" if match_mch_elems.group(2) == "windows" else "unix"
    target_arch_name = match_mch_elems.group(3)
    run_name = "{}_{}_{}".format(os_name, target_arch_name, coreclr_args.arch)

    print("=============> Running superpmi.py download")
    run_command([python_path, path.join(cwd, "superpmi.py"), "download", "-f", "benchmarks"])

    # populate based on zip file name and jit_directory
    jit_path = path.join(coreclr_args.jit_directory, 'clrjit_{}.dll'.format(run_name))
    run_id = 0
    for jitstressregs in jitstressregs_values:
        # TODO: This should be DownloadFilesFromResults
        log_file = path.join(log_directory, 'superpmi_{}_{}_flag_{}.log'.format(collection_name, run_name, jitstressregs))

        # print(' '.join([
        #         python_path, path.join(cwd, "superpmi.py"), "replay", "-core_root", cwd,
        #         "-jitoption", "JitStressRegs=" + jitstressregs, "-jitoption", "TieredCompilation=0",
        #         "-jit_path", jit_path, "-mch_files", mch_directory, "-spmi_location", mch_directory,
        #         "-log_level", "debug", "-log_file", log_path]))

        # In first iteration, mch files are unzipped and placed in "mch" folder. Going forward, use the
        # uncompressed mch files
        if run_id == 1:
            mch_directory = path.join(mch_directory, "mch")

        run_command([
                python_path, path.join(cwd, "superpmi.py"), "replay", "-core_root", cwd,
                "-jitoption", "JitStressRegs=" + jitstressregs, "-jitoption", "TieredCompilation=0",
                "-jit_path", jit_path, "-mch_files", mch_directory, "-spmi_location", mch_directory,
                "-log_level", "debug", "-log_file", log_file],
                _exit_on_fail=True)

        run_id += 1

    # Consolidate all superpmi.logs in superpmi_partition_index.log
    final_log_name = path.join(log_directory, "superpmi_{}.log".format(coreclr_args.partition_index))
    with open(final_log_name, "a") as final_superpmi_log:
        for superpmi_log in listdir(log_directory):
            if not f.startswith("superpmi_") and not f.endswith(".log"):
                continue

            final_superpmi_log.write("======================================================={}".format(os.linesep))
            final_superpmi_log.write("Contents from {}{}".format(superpmi_log, os.linesep))
            final_superpmi_log.write("======================================================={}".format(os.linesep))
            with open(path.join(log_directory, superpmi_log), "r") as current_superpmi_log:
                contents = current_superpmi_log.read()
                final_superpmi_log.write(contents)

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
