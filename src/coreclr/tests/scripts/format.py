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


import urllib
import argparse
import os
import sys
import tarfile
import zipfile
import subprocess
import urllib2
import shutil

def expandPath(path):
    return os.path.abspath(os.path.expanduser(path))

def del_rw(action, name, exc):
    os.chmod(name, 0651)
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
        print('Ignorning argument(s): ', ','.join(unknown))

    if args.coreclr is None:
        print('Specify --coreclr')
        return -1
    if args.os is None:
        print('Specifiy --os')
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

    dotnetcliPath = os.path.join(coreclr, 'dotnetcli-jitutils')

    # Try to make the dotnetcli-jitutils directory if it doesn't exist

    try:
        os.makedirs(dotnetcliPath)
    except OSError:
        if not os.path.isdir(dotnetcliPath):
            raise

    print("Downloading .Net CLI")
    if platform == 'Linux':
        dotnetcliUrl = "https://go.microsoft.com/fwlink/?LinkID=809129"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')
    elif platform == 'OSX':
        dotnetcliUrl = "https://go.microsoft.com/fwlink/?LinkID=809128"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')
    elif platform == 'Windows_NT':
        dotnetcliUrl = "https://go.microsoft.com/fwlink/?LinkID=809126"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.zip')
    else:
        print('Unknown os ', os)
        return -1

    response = urllib2.urlopen(dotnetcliUrl)
    request_url = response.geturl()
    testfile = urllib.URLopener()
    testfile.retrieve(request_url, dotnetcliFilename)

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
    testfile.retrieve(bootstrapUrl, bootstrapPath)

    if not os.path.isfile(bootstrapPath):
        print("Did not download bootstrap!")
        return -1

    # On *nix platforms, we need to make the bootstrap file executable

    if platform == 'Linux' or platform == 'OSX':
        print("Making bootstrap executable")
        os.chmod(bootstrapPath, 0751)

    print(bootstrapPath)

    # Run bootstrap

    my_env["PATH"] += os.pathsep + dotnetcliPath
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
    my_env["PATH"] += os.pathsep + jitutilsBin
    current_dir = os.getcwd()

    if os.path.isdir(jitutilsBin):
        os.chdir(jitutilsBin)
    else:
        print("Jitutils not built!")
        return -1

    jitformat = ""

    if platform == 'Linux' or platform == 'OSX':
        jitformat = "jit-format"
    elif platform == 'Windows_NT':
        jitformat = "jit-format.cmd"

    for build in ["Checked", "Debug", "Release"]:
        for project in ["dll", "standalone", "crossgen"]:
            proc = subprocess.Popen([jitformat, "-a", arch, "-b", build, "-o", platform, "-c", coreclr, "--verbose", "--projects", project], env=my_env)
            output,error = proc.communicate()
            errorcode = proc.returncode

            if errorcode != 0:
                returncode = errorcode

    os.chdir(current_dir)

    if os.path.isdir(jitUtilsPath):
        print("Deleting " + jitUtilsPath)
        shutil.rmtree(jitUtilsPath, onerror=del_rw)

    if os.path.isdir(dotnetcliPath):
        print("Deleting " + dotnetcliPath)
        shutil.rmtree(dotnetcliPath, onerror=del_rw)

    if os.path.isfile(bootstrapPath):
        print("Deleting " + bootstrapPath)
        os.remove(bootstrapPath)

    return returncode

if __name__ == '__main__':
    return_code = main(sys.argv[1:])
    sys.exit(return_code)
