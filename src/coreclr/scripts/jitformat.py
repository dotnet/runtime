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
import subprocess
import sys

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
    required.add_argument('-r', '--runtime', type=str,
            default=None, help='full path to runtime repo root')

    optional = parser.add_argument_group('optional arguments')
    optional.add_argument('--cross', action="store_true",
            default=None, help='do cross builds on Linux')
    optional.add_argument('-j', '--jitutils', type=str,
            default=None, help='full path to built jitutils repo root. Uses this instead of downloading bootstrap.sh/cmd and cloning/building jitutils.')

    args, unknown = parser.parse_known_args(argv)

    if unknown:
        logging.warning('Ignoring argument(s): {}'.format(','.join(unknown)))

    if args.runtime is None:
        logging.error('Specify --runtime')
        return -1
    if args.os is None:
        logging.error('Specify --os')
        return -1
    if args.arch is None:
        logging.error('Specify --arch')
        return -1
    if args.cross:
        if args.os != "linux":
            logging.error('--cross is only valid with -os linux')
            return -1
    if args.jitutils is not None:
        jitutilsRoot = os.path.abspath(os.path.expanduser(args.jitutils))
        if not os.path.isdir(jitutilsRoot):
            logging.error('Bad path to jitutils')
            return -1

    runtime = os.path.abspath(os.path.expanduser(args.runtime)).replace('/', os.sep)
    if not os.path.isdir(runtime):
        logging.error('Bad runtime path')
        return -1

    platform = args.os
    arch = args.arch

    my_env = os.environ

    # Download formatting tools clang-format and clang-tidy and add them to PATH
    formattingScriptFolder = os.path.join(runtime, "eng", "formatting")
    if not os.path.isdir(formattingScriptFolder):
        logging.error('Bad runtime path: eng/formatting directory not found')
        return -1

    if platform == 'windows':
        formattingDownloadScriptCommand = ["powershell", os.path.join(formattingScriptFolder, "download-tools.ps1")]
    else:
        formattingDownloadScriptCommand = [os.path.join(formattingScriptFolder, "download-tools.sh")]

    proc = subprocess.Popen(formattingDownloadScriptCommand)
    if proc.wait() != 0:
        logging.error("Formatting tool download failed")
        return -1

    my_env["PATH"] = os.path.join(runtime, "artifacts", "tools") + os.pathsep + my_env["PATH"]

    with jitutil.TempDir() as temp_location:
        assert len(os.listdir(temp_location)) == 0

        if args.jitutils is not None:
            logging.info('--jitutils passed: not downloading bootstrap.cmd/sh and cloning/building jitutils repo')

        else:

            # Download bootstrap
            if platform == 'windows':
                bootstrapFilename = "bootstrap.cmd"
            else:
                bootstrapFilename = "bootstrap.sh"

            bootstrapUrl = "https://raw.githubusercontent.com/dotnet/jitutils/main/" + bootstrapFilename
            bootstrapPath = os.path.join(temp_location, bootstrapFilename)
            if not jitutil.download_one_url(bootstrapUrl, bootstrapPath) or not os.path.isfile(bootstrapPath):
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
            if platform == 'windows':
                command = [bootstrapPath]
            else:
                command = ['bash', bootstrapPath]

            command_string = " ".join(command)
            logging.info('Running: {}'.format(command_string))
            proc = subprocess.Popen(command, env=my_env)
            output,error = proc.communicate()
            if proc.returncode != 0:
                logging.error("Bootstrap failed")
                return -1

            jitutilsRoot = os.path.join(temp_location, "jitutils")

            # end of 'if args.jitutils is None'

        # Run jit-format

        jitutilsBin = os.path.join(jitutilsRoot, "bin")
        if not os.path.isdir(jitutilsBin):
            logging.error("jitutils not built!")
            return -1

        my_env["PATH"] = jitutilsBin + os.pathsep + my_env["PATH"]

        if platform == 'windows':
            jitformat = os.path.join(jitutilsBin, "jit-format.exe")
        else:
            jitformat = os.path.join(jitutilsBin, "jit-format")

        if not os.path.isfile(jitformat):
            logging.error("jit-format not found")
            return -1

        errorMessage = ""

        builds = ["Checked", "Debug", "Release"]
        projects = ["dll", "standalone", "crossgen"]

        returncode = 0
        for build in builds:
            for project in projects:
                command = [jitformat, "-a", arch, "-b", build, "-o", platform, "-r", runtime, "--verbose", "--projects", project]
                if args.cross:
                    command += ["--cross"]

                command_string = " ".join(command)
                logging.info('Running: {}'.format(command_string))
                proc = subprocess.Popen(command, env=my_env)
                output,error = proc.communicate()
                errorcode = proc.returncode

                if errorcode != 0:
                    errorMessage += "\tjit-format -a " + arch + " -b " + build + " -o " + platform
                    errorMessage += " -r <absolute-path-to-runtime-root> --verbose --fix --projects " + project + "\n"
                    returncode = errorcode

                    # Fix mode doesn't return an error, so we have to run the build, then run with
                    # --fix to generate the patch. This means that it is likely only the first run
                    # of jit-format will return a formatting failure.
                    if errorcode == -2:
                        # If errorcode was -2, no need to run clang-tidy again
                        proc = subprocess.Popen([jitformat, "--fix", "--untidy", "-a", arch, "-b", build, "-o", platform, "-r", runtime, "--verbose", "--projects", project], env=my_env)
                        output,error = proc.communicate()
                    else:
                        # Otherwise, must run both
                        proc = subprocess.Popen([jitformat, "--fix", "-a", arch, "-b", build, "-o", platform, "-r", runtime, "--verbose", "--projects", project], env=my_env)
                        output,error = proc.communicate()

    patchFilePath = os.path.join(runtime, "format.patch")

    if returncode != 0:
        # Create a patch file
        logging.info("Creating patch file {}".format(patchFilePath))
        jitSrcPath = os.path.join(runtime, "src", "coreclr", "jit")
        patchFile = open(patchFilePath, "w")
        proc = subprocess.Popen(["git", "diff", "--patch", "-U20", "--", jitSrcPath], env=my_env, stdout=patchFile)
        output,error = proc.communicate()

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
