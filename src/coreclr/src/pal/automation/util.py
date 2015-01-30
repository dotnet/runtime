import sys
import getopt
import os
import subprocess
import shutil
import logging as log

def Initialize(platform):
    print "Initializing Workspace"
    global workspace
    workspace = os.environ['WORKSPACE']
    if platform == "windows":
        # Jenkins puts quotes in the path, which is wrong. Remove quotes.
        os.environ['PATH'] = os.environ['PATH'].replace('"','')

    return workspace

def ParseArgs(argv):
    print "Parsing arguments for compile"
    try:
        opts, args = getopt.getopt(argv, "t:p:a:v", ["target=", "platform=", "arch=", "verbose","noclean"])
    except getopt.GetoptError:
        print "ERROR: \n\t usage: python compile.py --target <target> --platform <windows|linux> --arch <arch> [--verbose] [--noclean]"
        return 2,"","","",True

    verbose = False
    cleanUp = True

    acceptedPlatforms = ['windows','linux']

    for opt, arg in opts:
        if opt in ("-t", "--target"):
            target = arg
        elif opt in ("-p", "--platform"):
            if arg.lower() not in acceptedPlatforms:
                print "ERROR: " + arg + "not an accepted platform. Use windows or linux."
                sys.exit(2)
            platform = arg.lower()
        elif opt in ("-a", "--arch"):
            arch = arg
        elif opt in ("-v", "--verbose"):
            verbose = True
        elif opt in ("-c", "--noclean"):
            cleanUp = False

    if verbose:
        log.basicConfig(format="%(levelname)s: %(message)s", level=log.DEBUG)
        log.info("In verbose mode.")
    else:
        log.basicConfig(format="%(levelname)s: %(message)s")

    if target == "" or platform == "" or arch == "":
        # must specify target, project and arch
        log.error("Must specify target, project and arch")
        return 2,"","","",True

    return 0,target,platform,arch,cleanUp

def SetupDirectories(target, arch, platform):
    log.info("Setting up directories")

    global rootdir
    global builddir
    global fullBuildDirPath

    rootdir = "build"
    if not os.path.isdir(rootdir):
        os.mkdir(rootdir)
    os.chdir(rootdir)
    
    builddir = "build-" + platform

    if platform == "windows":
        builddir = builddir + "-" + arch + "-" + target

    if os.path.isdir(builddir):
        shutil.rmtree(builddir)
    os.mkdir(builddir)
    os.chdir(builddir)

    fullbuilddirpath = workspace + "/" + rootdir + "/" + builddir

    return fullbuilddirpath

def Cleanup(cleanUp,workspace):
    print "\n==================================================\n"
    print "Cleaning Up."
    print "\n==================================================\n"
    
    if cleanUp:
        os.chdir(workspace + "/" + rootdir)
        shutil.rmtree(builddir)
        os.chdir("..")
        shutil.rmtree(rootdir)
    
    log.shutdown()
    return 0
