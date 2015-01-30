import logging as log
import sys
import getopt
import os
import subprocess
import shutil

def RunCMake(workspace, target, platform):
    # run CMake
    print "\n==================================================\n"

    returncode = 0
    if platform == "windows":
        print "Running: vcvarsall.bat x86_amd64 && " + workspace + "\ProjectK\NDP\clr\src\pal\\tools\gen-buildsys-win.bat " + workspace + "\ProjectK\NDP\clr"
        print "\n==================================================\n"
        sys.stdout.flush()
        returncode = subprocess.call(["vcvarsall.bat", "x86_amd64", "&&", workspace + "\ProjectK\NDP\clr\src\pal\\tools\gen-buildsys-win.bat", workspace + "\ProjectK\NDP\clr"])
    elif platform == "linux":
        print "Running: " + workspace + "/ProjectK/NDP/clr/src/pal/tools/gen-buildsys-clang.sh " + workspace + "/ProjectK/NDP/clr DEBUG"
        print "\n==================================================\n"
        sys.stdout.flush()
        returncode = subprocess.call(workspace + "/ProjectK/NDP/clr/src/pal/tools/gen-buildsys-clang.sh " + workspace + "/ProjectK/NDP/clr " + target, shell=True)

    if returncode != 0:
        print "ERROR: cmake failed with exit code " + str(returncode)

    return returncode

def RunBuild(target, platform, arch):
    if platform == "windows":
        return RunMsBuild(target, arch)
    elif platform == "linux":
        return RunMake()

def RunMsBuild(target, arch):
    # run MsBuild
    print "\n==================================================\n"
    print "Running: vcvarsall.bat x86_amd64 && msbuild CoreCLR.sln /p:Configuration=" + target + " /p:Platform=" + arch
    print "\n==================================================\n"
    sys.stdout.flush()
    
    returncode = subprocess.call(["vcvarsall.bat","x86_amd64","&&","msbuild","CoreCLR.sln","/p:Configuration=" + target,"/p:Platform=" + arch])
    
    if returncode != 0:
        print "ERROR: vcvarsall.bat failed with exit code " + str(returncode)
    
    return returncode

def RunMake():
    print "\n==================================================\n"
    print "Running: make"
    print "\n==================================================\n"
    sys.stdout.flush()
    returncode = subprocess.call(["make"])

    if returncode != 0:
        print "ERROR: make failed with exit code " + str(returncode)

    return returncode

def Compile(workspace, target, platform, arch):
    returncode = RunCMake(workspace, target, platform)
    if returncode != 0:
        return returncode
    
    returncode += RunBuild(target, platform, arch)
    if returncode != 0:
        return returncode

    return returncode
