import lldb
import lldbutil
import re
import os
import testutils

def runScenario(assemblyName, debugger, target):
	process = target.GetProcess()
	res = lldb.SBCommandReturnObject()
	ci = debugger.GetCommandInterpreter()
	
	testutils.stop_in_main(ci, process, assemblyName)
	addr = testutils.exec_and_find(ci, "name2ee " + assemblyName + " Program.Main", "Module:\s+([0-9a-fA-F]+)")

	result = False
	if addr is not None:
		ci.HandleCommand("dumpmodule " + addr, res)
		if res.Succeeded():
			result = True
		else:
			print("DumpModule failed:")
			print(res.GetOutput())
			print(res.GetError())

	process.Continue()
	return result