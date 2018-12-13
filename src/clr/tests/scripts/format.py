#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
#
##
# Title               :format.py
#
################################################################################
# Script to install and run jit-format over jit source for all configurations.
################################################################################


import argparse
import os
import sys
import tarfile
import zipfile
import subprocess
import shutil

# Version specific imports

if sys.version_info.major < 3:
    import urllib
else:
    import urllib.request

def expandPath(path):
    return os.path.abspath(os.path.expanduser(path))

def del_rw(action, name, exc):
    os.chmod(name, 0o651)
    os.remove(name)

def main(argv):
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
        print('Ignoring argument(s): ', ','.join(unknown))

    if args.coreclr is None:
        print('Specify --coreclr')
        return -1
    if args.os is None:
        print('Specify --os')
        return -1
    if args.arch is None:
        print('Specify --arch')
        return -1

    if not os.path.isdir(expandPath(args.coreclr)):
        print('Bad path to coreclr')
        return -1

    coreclr = args.coreclr
    platform = args.os
    arch = args.arch

    my_env = os.environ

    # Download .Net CLI

    dotnetcliUrl = ""
    dotnetcliFilename = ""

    # build.cmd removes the Tools directory, so we need to put our version of jitutils
    # outside of the Tools directory
    # must use short path here to avoid trouble on windows

    dotnetcliPath = os.path.join(coreclr, 'dj')

    # Try to make the dotnetcli-jitutils directory if it doesn't exist

    try:
        os.makedirs(dotnetcliPath)
    except OSError:
        if not os.path.isdir(dotnetcliPath):
            raise

    print("Downloading .Net CLI")
    if platform == 'Linux':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-linux-x64.tar.gz"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')
    elif platform == 'OSX':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-osx-x64.tar.gz"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')
    elif platform == 'Windows_NT':
        dotnetcliUrl = "https://dotnetcli.azureedge.net/dotnet/Sdk/2.1.402/dotnet-sdk-2.1.402-win-x64.zip"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.zip')
    else:
        print('Unknown os ', os)
        return -1

    urlretrieve = urllib.urlretrieve if sys.version_info.major < 3 else urllib.request.urlretrieve
    urlretrieve(dotnetcliUrl, dotnetcliFilename)

    if not os.path.isfile(dotnetcliFilename):
        print("Did not download .Net CLI!")
        return -1

    # Install .Net CLI

    if platform == 'Linux' or platform == 'OSX':
        tar = tarfile.open(dotnetcliFilename)
        tar.extractall(dotnetcliPath)
        tar.close()
    elif platform == 'Windows_NT':
        with zipfile.ZipFile(dotnetcliFilename, "r") as z:
            z.extractall(dotnetcliPath)

    dotnet = ""
    if platform == 'Linux' or platform == 'OSX':
        dotnet = "dotnet"
    elif platform == 'Windows_NT':
        dotnet = "dotnet.exe"


    if not os.path.isfile(os.path.join(dotnetcliPath, dotnet)):
        print("Did not extract .Net CLI from download")
        return -1

    # Download bootstrap

    bootstrapFilename = ""

    jitUtilsPath = os.path.join(coreclr, "jitutils")

    if os.path.isdir(jitUtilsPath):
        print("Deleting " + jitUtilsPath)
        shutil.rmtree(jitUtilsPath, onerror=del_rw)

    if platform == 'Linux' or platform == 'OSX':
        bootstrapFilename = "bootstrap.sh"
    elif platform == 'Windows_NT':
        bootstrapFilename = "bootstrap.cmd"

    bootstrapUrl = "https://raw.githubusercontent.com/dotnet/jitutils/master/" + bootstrapFilename

    bootstrapPath = os.path.join(coreclr, bootstrapFilename)
    urlretrieve(bootstrapUrl, bootstrapPath)

    if not os.path.isfile(bootstrapPath):
        print("Did not download bootstrap!")
        return -1

    # On *nix platforms, we need to make the bootstrap file executable

    if platform == 'Linux' or platform == 'OSX':
        print("Making bootstrap executable")
        os.chmod(bootstrapPath, 0o751)

    print(bootstrapPath)

    # Run bootstrap

    my_env["PATH"] = dotnetcliPath + os.pathsep + my_env["PATH"]
    if platform == 'Linux' or platform == 'OSX':
        print("Running bootstrap")
        proc = subprocess.Popen(['bash', bootstrapPath], env=my_env)
        output,error = proc.communicate()
    elif platform == 'Windows_NT':
        proc = subprocess.Popen([bootstrapPath], env=my_env)
        output,error = proc.communicate()

    # Run jit-format

    returncode = 0
    jitutilsBin = os.path.join(coreclr, "jitutils", "bin")
    my_env["PATH"] = jitutilsBin + os.pathsep + my_env["PATH"]
    current_dir = os.getcwd()

    if not os.path.isdir(jitutilsBin):
        print("Jitutils not built!")
        return -1

    jitformat = jitutilsBin

    if platform == 'Linux' or platform == 'OSX':
        jitformat = os.path.join(jitformat, "jit-format")
    elif platform == 'Windows_NT':
        jitformat = os.path.join(jitformat,"jit-format.bat")
    errorMessage = ""

    builds = ["Checked", "Debug", "Release"]
    projects = ["dll", "standalone", "crossgen"]

    for build in builds:
        for project in projects:
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

    os.chdir(current_dir)

    if returncode != 0:
        # Create a patch file
        patchFile = open("format.patch", "w")
        proc = subprocess.Popen(["git", "diff", "--patch", "-U20"], env=my_env, stdout=patchFile)
        output,error = proc.communicate()

    # shutdown the dotnet build servers before cleaning things up
    proc = subprocess.Popen(["dotnet", "build-server", "shutdown"], env=my_env)
    output,error = proc.communicate()

    # shutdown all spurious dotnet processes using os shell
    if platform == 'Linux' or platform == 'OSX':
        subprocess.call(['killall', '-SIGTERM', '-qw', dotnet])
    elif platform == 'Windows_NT':
        utilpath = os.path.join(coreclr, 'tests\\scripts\\kill-all.cmd')
        subprocess.call([utilpath, dotnet])

    if os.path.isdir(jitUtilsPath):
        print("Deleting " + jitUtilsPath)
        shutil.rmtree(jitUtilsPath, onerror=del_rw)

    if os.path.isdir(dotnetcliPath):
        print("Deleting " + dotnetcliPath)
        shutil.rmtree(dotnetcliPath, onerror=del_rw)

    if os.path.isfile(bootstrapPath):
        print("Deleting " + bootstrapPath)
        os.remove(bootstrapPath)

    if returncode != 0:
        buildUrl = my_env["BUILD_URL"]
        print("There were errors in formatting. Please run jit-format locally with: \n")
        print(errorMessage)
        print("\nOr download and apply generated patch:")
        print("wget " + buildUrl + "artifact/format.patch")
        print("git apply format.patch")

    return returncode

if __name__ == '__main__':
    return_code = main(sys.argv[1:])
    sys.exit(return_code)
