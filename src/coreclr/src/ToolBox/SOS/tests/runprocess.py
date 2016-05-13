import os
import lldb
import sys
import importlib
from test_libsosplugin import fail_flag

def run(assemblyName, moduleName):
	global fail_flag

	print(fail_flag)
	# set the flag, if it is not set
	if not os.access(fail_flag, os.R_OK):
		open(fail_flag, "a").close()


	debugger = lldb.debugger

	debugger.SetAsync(False)
	target = lldb.target

	debugger.HandleCommand("process launch -s")
	debugger.HandleCommand("breakpoint set -n LoadLibraryExW")

	target.GetProcess().Continue()

	debugger.HandleCommand("breakpoint delete 1")
	#run the scenario
	print("starting scenario...")
	i = importlib.import_module(moduleName)
	scenarioResult = i.runScenario(os.path.basename(assemblyName), debugger, target)

	# clear the failed flag if the exit status is OK
	if scenarioResult is True and target.GetProcess().GetExitStatus() == 0:
		os.unlink(fail_flag)
