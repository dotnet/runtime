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

    # Download dotnetcli
    dotnetcliUrl = ""
    dotnetcliFilename = ""
    dotnetcliPath = os.path.join(coreclr, 'Tools', 'dotnetcli-jitutils')

    # Try to make the dotnetcli-jitutils directory if it doesn't exist
    try: 
        os.makedirs(dotnetcliPath)
    except OSError:
        if not os.path.isdir(dotnetcliPath):
            raise

    if platform == 'Linux' or platform == 'OSX':
        dotnetcliUrl = "https://go.microsoft.com/fwlink/?LinkID=809118"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.tar.gz')
    elif platform == 'Windows_NT':
        dotnetcliUrl = "https://go.microsoft.com/fwlink/?LinkID=809115"
        dotnetcliFilename = os.path.join(dotnetcliPath, 'dotnetcli-jitutils.zip')
    else:
        print('Unknown os ', os)
        return -1

    response = urllib2.urlopen(dotnetcliUrl)
    request_url = response.geturl()
    print(request_url)
    testfile = urllib.URLopener()
    testfile.retrieve(request_url, dotnetcliFilename)

    # Install dotnetcli

    if platform == 'Linux' or platform == 'OSX':
        tar = tarfile.open(dotnetcliFilename)
        tar.extractall(dotnetcliPath)
        tar.close()
    elif platform == 'Windows_NT':
        with zipfile.ZipFile(dotnetcliFilename, "r") as z:
            z.extractall(dotnetcliPath)

    # Download bootstrap
    bootstrapFilename = ""

    jitUtilsPath = os.path.join(coreclr, "jitutils")

    if os.path.isdir(jitUtilsPath):
        shutil.rmtree(dest, ignore_errors=True)

    if platform == 'Linux' or platform == 'OSX':
        bootstrapFilename = "bootstrap.sh"
    elif platform == 'Windows_NT':
        bootstrapFilename = "bootstrap.cmd"

    bootstrapUrl = "https://raw.githubusercontent.com/dotnet/jitutils/master/" + bootstrapFilename

    testfile.retrieve(bootstrapUrl, bootstrapFilename)

    # On Linux platforms, we need to make the bootstrap file executable
    if platform == 'Linux' or platform == 'OSX':
        os.chmod(bootstrapFilename, 0751)

    # Run bootstrap
    os.environ["PATH"] += os.pathsep + dotnetcliPath
    proc = subprocess.Popen([os.path.join(coreclr, bootstrapFilename)], shell=True)

    output,error = proc.communicate()
    print(output)
    print(error)

    # Run jit-format
    returncode = 0
    os.environ["PATH"] += os.pathsep + os.path.join(coreclr, "jitutils", "bin")

    for build in ["Checked", "Debug", "Release"]:
        for project in ["dll", "standalone", "crossgen"]:
            proc = subprocess.Popen(["jit-format", "-a", arch, "-b", build, "-o", platform,
                "-c", coreclr, "--verbose", "--projects", project], shell=True)
            output,error = proc.communicate()
            errorcode = proc.returncode

            print(output)
            print(error)

            if errorcode != 0:
                returncode = errorcode

    return returncode

if __name__ == '__main__':
    return_code = main(sys.argv[1:])
    sys.exit(return_code)
