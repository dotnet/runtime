import logging as log
import sys
import getopt
import os
import subprocess
import shutil
import compile
import tests
import util

target = ""
arch = ""
platform = ""
workspace = ""
fullbuilddirpath = ""
cleanUp = True

def ParseArgs(argv):
    global target
    global platform
    global arch
    global cleanUp

    returncode,target,platform,arch,cleanUp = util.ParseArgs(argv)
    return returncode

def Initialize():
    global workspace
    workspace = util.Initialize(platform)
    return 0

def SetupDirectories():
    global fullbuilddirpath
    fullbuilddirpath = util.SetupDirectories(target, arch, platform)

    return 0

def CopyBinaries():
    print "\n==================================================\n"
    print "Stub for copying binaries"
    print "\n==================================================\n"

    return 0

def main(argv):
    returncode = ParseArgs(argv)
    if returncode != 0:
        return returncode
    
    returncode += Initialize()
    if returncode != 0:
        return returncode
    
    returncode += SetupDirectories()
    if returncode != 0:
        return returncode
    
    returncode += compile.Compile(workspace, target, platform, arch)
    if returncode != 0:
        return returncode
    
    returncode += CopyBinaries()
    if returncode != 0:
        return returncode
    
    returncode += tests.RunTests(platform, fullbuilddirpath, workspace)
    if returncode != 0:
        return returncode
    
    return returncode

if __name__ == "__main__":
    returncode = main(sys.argv[1:])

    util.Cleanup(cleanUp,workspace)

    sys.exit(returncode)

