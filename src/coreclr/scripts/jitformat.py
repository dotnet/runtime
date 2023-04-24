#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               :jitformat.py
#
################################################################################
# Script to install and run jit-format over jit source for all configurations.
################################################################################


import argparse
import jitutil
import logging
import os
import shutil
import subprocess
import sys
import tarfile
import tempfile
import zipfile

class ChangeDir:
    def __init__(self, dir):
        self.dir = dir
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.dir)

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)

class TempDir:
    def __init__(self, path=None):
        self.dir = tempfile.mkdtemp() if path is None else path
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.dir)

        return self.dir

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)

def expandPath(path):
    return os.path.abspath(os.path.expanduser(path))

def del_rw(action, name, exc):
    os.chmod(name, 0o651)
    os.remove(name)

def cleanup(jitUtilsPath, bootstrapPath):
    if os.path.isdir(jitUtilsPath):
        logging.info("Deleting " + jitUtilsPath)
        shutil.rmtree(jitUtilsPath, onerror=del_rw)

    if os.path.isfile(bootstrapPath):
        logging.info("Deleting " + bootstrapPath)
        os.remove(bootstrapPath)

def main(argv):
    logging.basicConfig(format="[%(asctime)s] %(message)s", datefmt="%H:%M:%S")
    logger = logging.getLogger()
    logger.setLevel(logging.INFO)

    parser = argparse.ArgumentParser()
    required = parser.add_argument_group('required arguments')
    required.add_argument('-a', '--arch', type=str,
            default=None, help='architecture to run jit-format on')
    required.add_argument('-o', '--os', type=str,
            default=None, help='operating system')
    required.add_argument('-c', '--coreclr', type=str,
            default=None, help='full path to coreclr')

    args, unknown = parser.parse_known_args(argv)

    if unknown:
        logging.warning('Ignoring argument(s): {}'.format(','.join(unknown)))

    if args.coreclr is None:
        logging.error('Specify --coreclr')
        return -1
    if args.os is None:
        logging.error('Specify --os')
        return -1
    if args.arch is None:
        logging.error('Specify --arch')
        return -1

    if not os.path.isdir(expandPath(args.coreclr)):
        logging.error('Bad path to coreclr')
        return -1

    coreclr = args.coreclr.replace('/', os.sep)

    platform = args.os
    arch = args.arch

    my_env = os.environ

    # Download formatting tools
    repoRoot = os.path.dirname(os.path.dirname(coreclr))
    formattingScriptFolder = os.path.join(repoRoot, "eng", "formatting")
    formattingDownloadScriptCommand = []
    if platform == 'linux' or platform == 'osx':
        formattingDownloadScriptCommand = [os.path.join(formattingScriptFolder, "download-tools.sh")]
    elif platform == 'windows':
        formattingDownloadScriptCommand = ["powershell", os.path.join(formattingScriptFolder, "download-tools.ps1")]

    proc = subprocess.Popen(formattingDownloadScriptCommand)

    if proc.wait() != 0:
        logging.error("Formatting tool download failed")
        return -1

    my_env["PATH"] = os.path.join(repoRoot, "artifacts", "tools") + os.pathsep + my_env["PATH"]

    # Download bootstrap

    bootstrapFilename = ""

    jitUtilsPath = os.path.join(coreclr, "jitutils")

    cleanup(jitUtilsPath, '')

    if platform == 'linux' or platform == 'osx':
        bootstrapFilename = "bootstrap.sh"
    elif platform == 'windows':
        bootstrapFilename = "bootstrap.cmd"

    bootstrapUrl = "https://raw.githubusercontent.com/dotnet/jitutils/main/" + bootstrapFilename

    with TempDir() as temp_location:
        bootstrapPath = os.path.join(temp_location, bootstrapFilename)

        assert len(os.listdir(os.path.dirname(bootstrapPath))) == 0

        if not jitutil.download_one_url(bootstrapUrl, bootstrapPath):
            logging.error("Did not download bootstrap!")
            return -1

        if platform == 'windows':
            # Need to ensure we have Windows line endings on the downloaded script file,
            # which is downloaded with Unix line endings.
            logging.info('Convert {} to Windows line endings'.format(bootstrapPath))

            content = None
            with open(bootstrapPath, 'rb') as open_file:
                content = open_file.read()

            content = content.replace(b'\n', b'\r\n')

            with open(bootstrapPath, 'wb') as open_file:
                open_file.write(content)

        # On *nix platforms, we need to make the bootstrap file executable

        if platform == 'linux' or platform == 'osx':
            logging.info("Making bootstrap executable")
            os.chmod(bootstrapPath, 0o751)

        # Run bootstrap
        if platform == 'linux' or platform == 'osx':
            logging.info('Running: bash {}'.format(bootstrapPath))
            proc = subprocess.Popen(['bash', bootstrapPath], env=my_env)
            output,error = proc.communicate()
        elif platform == 'windows':
            logging.info('Running: {}'.format(bootstrapPath))
            proc = subprocess.Popen([bootstrapPath], env=my_env)
            output,error = proc.communicate()

        if proc.returncode != 0:
            cleanup('', bootstrapPath)
            logging.error("Bootstrap failed")
            return -1

        # Run jit-format

        returncode = 0
        jitutilsBin = os.path.join(os.path.dirname(bootstrapPath), "jitutils", "bin")
        my_env["PATH"] = jitutilsBin + os.pathsep + my_env["PATH"]

        if not os.path.isdir(jitutilsBin):
            logging.error("Jitutils not built!")
            return -1

        jitformat = jitutilsBin

        if platform == 'linux' or platform == 'osx':
            jitformat = os.path.join(jitformat, "jit-format")
        elif platform == 'windows':
            jitformat = os.path.join(jitformat,"jit-format.exe")
        errorMessage = ""

        builds = ["Checked", "Debug", "Release"]
        projects = ["dll", "standalone", "crossgen"]

        for build in builds:
            for project in projects:
                command = jitformat + " -a " + arch + " -b " + build + " -o " + platform + " -c " + coreclr + " --verbose --projects " + project
                logging.info('Running: {}'.format(command))
                proc = subprocess.Popen([jitformat, "-a", arch, "-b", build, "-o", platform, "-c", coreclr, "--verbose", "--projects", project], env=my_env)
                output,error = proc.communicate()
                errorcode = proc.returncode

                if errorcode != 0:
                    errorMessage += "\tjit-format -a " + arch + " -b " + build + " -o " + platform
                    errorMessage += " -c <absolute-path-to-coreclr> --verbose --fix --projects " + project +"\n"
                    returncode = errorcode

                    # Fix mode doesn't return an error, so we have to run the build, then run with
                    # --fix to generate the patch. This means that it is likely only the first run
                    # of jit-format will return a formatting failure.
                    if errorcode == -2:
                        # If errorcode was -2, no need to run clang-tidy again
                        proc = subprocess.Popen([jitformat, "--fix", "--untidy", "-a", arch, "-b", build, "-o", platform, "-c", coreclr, "--verbose", "--projects", project], env=my_env)
                        output,error = proc.communicate()
                    else:
                        # Otherwise, must run both
                        proc = subprocess.Popen([jitformat, "--fix", "-a", arch, "-b", build, "-o", platform, "-c", coreclr, "--verbose", "--projects", project], env=my_env)
                        output,error = proc.communicate()

    patchFilePath = os.path.join(coreclr, "format.patch")

    if returncode != 0:
        # Create a patch file
        logging.info("Creating patch file {}".format(patchFilePath))
        jitSrcPath = os.path.join(coreclr, "jit")
        patchFile = open(patchFilePath, "w")
        proc = subprocess.Popen(["git", "diff", "--patch", "-U20", "--", jitSrcPath], env=my_env, stdout=patchFile)
        output,error = proc.communicate()

    cleanup(jitUtilsPath, bootstrapPath)

    if returncode != 0:
        logging.info("There were errors in formatting. Please run jit-format locally with: \n")
        logging.info(errorMessage)
        logging.info("\nOr download and apply generated patch:")
        logging.info("1. From the GitHub 'Checks' page on the Pull Request, with the failing Formatting")
        logging.info("   job selected (e.g., 'Formatting Linux x64'), click the 'View more details on")
        logging.info("   Azure Pipelines' link.")
        logging.info("2. Select the '1 artifact produced' at the end of the log.")
        logging.info("3. Artifacts are located in alphabetical order, target artifact name is")
        logging.info("   'format.<OS>.<architecture>.patch.'. Find appropriate format patch artifact.")
        logging.info("4. On the right side of the artifact there is a 'More actions' menu shown by a")
        logging.info("   vertical three-dot symbol. Click on it and select 'Download artifacts' option.")
        logging.info("5. Unzip the patch file.")
        logging.info("6. git apply format.patch")

    if (returncode != 0) and (os.environ.get("TF_BUILD") == "True"):
        print("##vso[task.logissue type=error](NETCORE_ENGINEERING_TELEMETRY=Build) Format job found errors, please apply the format patch.")

    return returncode

if __name__ == '__main__':
    return_code = main(sys.argv[1:])
    sys.exit(return_code)
