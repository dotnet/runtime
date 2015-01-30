import sys
import getopt
import os
import subprocess
import shutil
import logging as log

def RunPalTests(fullbuilddirpath, workspace):
    print "\n==================================================\n"
    print "Running PAL Tests."
    print "\n==================================================\n"

    print "Running: " + workspace + "/ProjectK/NDP/clr/src/pal/tests/palsuite/runpaltests.sh " + fullbuilddirpath + " " + fullbuilddirpath + "/PalTestOutput"
    print "\n==================================================\n"
    sys.stdout.flush()
    returncode = subprocess.call(workspace + "/ProjectK/NDP/clr/src/pal/tests/palsuite/runpaltests.sh " + fullbuilddirpath + " " + fullbuilddirpath + "/PalTestOutput", shell=True)

    if returncode != 0:
        print "ERROR: there were errors failed with exit code " + str(returncode)

    return returncode


def RunTests(platform, fullbuilddirpath, workspace):
    returncode = 0

    if platform == "linux":
        # Execute PAL tests
        returncode = RunPalTests(fullbuilddirpath, workspace)

    return returncode


