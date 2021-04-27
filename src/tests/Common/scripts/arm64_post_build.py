################################################################################
################################################################################
#
# Module: arm64_post_build.py
#
# Notes:
#
# This script is responsible for starting the x64 dotnet client. In order to
# do this it has to pass along the core_root that was built in the previous
# build steps using build.cmd.
#
# After everything has run, the dotnet client will dump a bunch of information
# to the console. It will be captured, parsed, and a series of xunit xml files 
# will be created so that jenkins can parse it to display results.
#
################################################################################
################################################################################

import argparse
import errno
import os
import urllib
import urllib2
import shutil
import subprocess
import sys
import zipfile

from collections import defaultdict

################################################################################
# Globals
################################################################################

g_arm64ci_path = os.path.join(os.environ["USERPROFILE"], "bin")
g_dotnet_url = "https://clrjit.blob.core.windows.net/arm64ci/dotnet-sdk.zip"
g_x64_client_url = "https://clrjit.blob.core.windows.net/arm64ci/x64_client_arm_proxy_change.zip"

################################################################################
# Argument Parser
################################################################################

description = """Python script to facilitate running an arm64/arm test run using
                 the cloud.
              """

parser = argparse.ArgumentParser(description=description)

parser.add_argument("--force_update", dest="force_update", action="store_true", default=False)

parser.add_argument("-repo_root", dest="repo_root", nargs='?', default=None)
parser.add_argument("-arch", dest="arch", nargs='?', default=None)
parser.add_argument("-testarch", dest="testarch", nargs='?', default=None)
parser.add_argument("-build_type", dest="build_type", nargs='?', default=None)
parser.add_argument("-scenario", dest="scenario", nargs='?', default=None)
parser.add_argument("-key_location", dest="key_location", nargs='?', default=None)
parser.add_argument("-priority", dest="priority", nargs='?', default="1")

################################################################################
# Helper Functions
################################################################################

def add_item_to_path(location):
   """ Add the dotnet install to the path
   """

   os.environ["PATH"] = location + ";" + os.environ["PATH"]

def copy_core_root(core_root):
   """ Copy the core root directory to the current dir as "build"
   Args:
      core_root (str): location of the core_root directory
   Returns:
      copy_location (str): name of the location, for now hardcoded to build
                         : for backcompat in the old system
   """

   new_location = "build"

   # Delete used instances.
   if os.path.isdir(new_location):
      try:
         shutil.rmtree(new_location)
      except:
         assert not os.path.isdir(new_location)

   try:
      shutil.copytree(core_root, new_location)

   except OSError as error:
      log("Core Root not copied. Error: %s" % error)
      sys.exit(1)

   return new_location

def copy_tests(test_location):
   """ Copy the test directory to the current dir as "tests"
   Args:
      test_location (str): location of the tests directory
   Returns:
      copy_location (str): name of the location, for now hardcoded to tests
                         : for backcompat in the old system
   """

   new_location = "tests"

   # Delete used instances.
   if os.path.isdir(new_location):
      try:
         shutil.rmtree(new_location)
      except:
         assert not os.path.isdir(new_location)

   try:
      shutil.copytree(test_location, new_location)

   except OSError as error:
      log("Test location not copied. Error: %s" % error)
      sys.exit(1)

   return new_location

def log(message):
   """ Helper function to print logging information
   Args:
      message (str): message to be printed
   """

   print "[arm64_post_build]: %s" % (message)

def setup_cli(force_update=False):
   """ Install the dotnet cli onto the machine
   Args:
      force_update (bool): whether or not to force an update. 
   Return:
      install_location (str): location of the installed cli
   Notes:
   This will be installed to %USERPROFILE%\dotnet. If force update is False
   then we will not install the cli if it already exists.
   
   """
   global g_dotnet_url

   install_path = os.path.join(os.environ["USERPROFILE"], "dotnet")

   # Only install if the cli doesn't exist or we are forcing an update
   if not os.path.isdir(install_path) or force_update:
      log("Downloading the .NET CLI")
      if os.path.isdir(install_path):
         try:
            shutil.rmtree(install_path)
         except:
            assert not os.path.isdir(install_path)

      os.mkdir(install_path)

      filename = os.path.join(install_path, 'dotnet-cli.zip')
      urllib.urlretrieve(g_dotnet_url, filename)

      if not os.path.isfile(filename):
         raise Exception("Error failed to download cli.")

      with zipfile.ZipFile(filename, 'r') as file_handle:
         file_handle.extractall(install_path)

   return install_path

def setup_x64_client(key_location, force_update=True):
   """ Setup the x64 client which will be used to communicate to the proxy
   Args:
      force_update (bool): whether or not to force an update, defaults to true
   Return:
      install_location (str): location of the installed x64_client
   Notes:
   Assume that the package has changed, so that every run will trigger an
   update. If there is no update then the install will be fairly quick either 
   way.
   """
   global g_x64_client_url
   install_path = os.path.join(os.environ["USERPROFILE"], "bin")

   # If installed and force update is not set. Just return
   if os.path.isdir(install_path) and not force_update:
      return install_path

   log("Downloading the x64_client")

   if os.path.isdir(install_path):
      # Delete the old location
      try:
         shutil.rmtree(install_path)
      except:
         assert not os.path.isdir(install_path)
   os.mkdir(install_path)

   filename = os.path.join(install_path, 'x64_client.zip')
   urllib.urlretrieve(g_x64_client_url, filename)

   if not os.path.isfile(filename):
      raise Exception("Error failed to download the x64_client.")

   with zipfile.ZipFile(filename, 'r') as file_handle:
      file_handle.extractall(install_path)

   # Copy key_location
   shutil.copy2(key_location, install_path)

   return install_path

def validate_args(args):
   """ Validate all of the arguments parsed.
   Args:
      args (argparser.ArgumentParser): Args parsed by the argument parser.
   Returns:
      (workspace, arch, testarch, build_type, scenario, force_update): (str, 
                                                                        str,
                                                                        str,
                                                                        str,
                                                                        str, 
                                                                        bool)
   Notes:
   If the arguments are valid then return them all in a tuple. If not, raise
   an exception stating x argument is incorrect.
   """

   repo_root = args.repo_root
   arch = args.arch
   testarch = args.testarch
   build_type = args.build_type
   scenario = args.scenario
   key_location = args.key_location
   priority = args.priority
   force_update = True

   def validate_arg(arg, check):
      """ Validate an individual arg
      Args:
         arg (str|bool): argument to be validated
         check (lambda: x-> bool): check that returns either True or False
                                 : based on whether the check works.
      
      Returns:
         is_valid (bool): Is the argument valid?
      """

      helper = lambda item: item is not None and check(item)

      if not helper(arg):
         raise Exception("Argument: %s is not valid." % (arg))

   valid_arches = ["arm", "arm64"]
   valid_testarches = ["arm", "armlb", "arm64"]
   valid_build_types = ["debug", "checked", "release"]
   valid_priorities = ["0", "1"]

   # Use the same naming scheme as netci.groovy, so, e.g., "0x10" instead of just "10".
   valid_jit_stress_regs_numbers = ["1", "2", "3", "4", "8", "0x10", "0x80", "0x1000"]

   jit_stress_scenarios = ["jitstress1", "jitstress2"]
   jitstressregs_scenarios = ["jitstressregs" + item for item in valid_jit_stress_regs_numbers]
   gc_stress_scenarios = ["gcstress0x3", "gcstress0xc"]
   
   combo_scenarios = []

   # GCStress 3 + JitStressX
   combo_scenarios += [gc_stress_scenarios[0] + "_" + item for item in jit_stress_scenarios]

   # GCStress C + JitStressX
   combo_scenarios += [gc_stress_scenarios[1] + "_" + item for item in jit_stress_scenarios]
   
   # GCStress 3 + JitStressRegsX
   combo_scenarios += [gc_stress_scenarios[0] + "_" + item for item in jitstressregs_scenarios]

   # GCStress C + JitStressRegsX
   combo_scenarios += [gc_stress_scenarios[1] + "_" + item for item in jitstressregs_scenarios]

   combo_scenarios.append("minopts_zapdisable")
   combo_scenarios.append("tailcallstress_zapdisable")

   valid_scenarios = ["default",
                      "pri1r2r",
                      "minopts",
                      "tailcallstress",
                      "zapdisable"]

   valid_scenarios += jitstressregs_scenarios
   valid_scenarios += jit_stress_scenarios
   valid_scenarios += gc_stress_scenarios
   valid_scenarios += combo_scenarios

   # Add the ryujit scenarios
   valid_scenarios += ["ryujit_" + item for item in valid_scenarios]
   
   validate_arg(repo_root, lambda item: os.path.isdir(item))
   validate_arg(arch, lambda item: item.lower() in valid_arches)
   validate_arg(testarch, lambda item: item.lower() in valid_testarches)
   validate_arg(build_type, lambda item: item.lower() in valid_build_types)
   validate_arg(scenario, lambda item: item.lower() in valid_scenarios)
   validate_arg(key_location, lambda item: os.path.isfile(item))
   validate_arg(force_update, lambda item: isinstance(item, bool))
   validate_arg(priority, lambda item: item in valid_priorities)

   arch = arch.lower()
   testarch = testarch.lower()
   build_type = build_type.lower()
   scenario = scenario.lower()

   args = (repo_root, arch, testarch, build_type, scenario, key_location, priority, force_update)

   log("Passed args: "
       "Repo Root: %s, "
       "Build Arch: %s, "
       "Test Arch: %s, "
       "Config: %s, "
       "Scenario: %s, "
       "Priority: %s "
       "Key Location: %s" % (repo_root, arch, testarch, build_type, scenario, priority, key_location))

   return args

################################################################################
# Main
################################################################################

def main(args):
   global g_arm64ci_path

   repo_root, arch, testarch, build_type, scenario, key_location, priority, force_update = validate_args(args)

   cwd = os.getcwd()
   os.chdir(repo_root)

   runtest_location = os.path.join(repo_root, "tests", "runtest.cmd")
   args = [runtest_location, "GenerateLayoutOnly", arch, build_type]
   subprocess.check_call(args)

   os.chdir(cwd)

   core_root = os.path.join(repo_root,
                            "bin",
                            "Product",
                            "windows.%s.%s" % (arch, build_type))

   test_location = os.path.join(repo_root,
                                "bin",
                                "tests",
                                "windows.%s.%s" % (arch, build_type))

   cli_location = setup_cli(force_update=force_update)
   add_item_to_path(cli_location)

   g_arm64ci_path = setup_x64_client(key_location)

   cwd = os.getcwd()
   os.chdir(g_arm64ci_path)

   core_root = copy_core_root(core_root)
   log("Copied core_root to %s." % core_root)

   test_location = copy_tests(test_location)
   log("Copied test location to %s." % test_location)

   # Make sure the lst file is copied into the core_root
   lst_file = os.path.join(repo_root, "tests", arch, "Tests.lst")
   shutil.copy2(lst_file, core_root)
   log("Copied %s to %s." % (lst_file, core_root))

   # The x64_client has logic to set the appropriate AltJit environment variables for
   # RyuJIT/arm32, set by prepending the scenario name with "ryujit_".
   if testarch == "arm":
      scenario = "ryujit_" + scenario

   scenario = priority + scenario

   args = ["dotnet", 
           os.path.join(g_arm64ci_path, "x64_client.dll"), 
           arch, 
           build_type, 
           scenario, 
           core_root, 
           test_location]

   log(" ".join(args))
   proc = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
   std_out, std_err = proc.communicate()

   # Restore directory
   os.chdir(cwd)

   if std_out == "":
      print std_err
   else:
      print std_out

   if std_out is not None and isinstance(std_out, str):
      if len(std_out.split("TestID")) > 1:
         sys.exit(1)

   # This run has been successful.
   elif len(std_out) > 0:
      sys.exit(0)

################################################################################
# setup for Main
################################################################################

if __name__ == "__main__":
   args = parser.parse_args(sys.argv[1:])
   main(args)
