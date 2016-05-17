import lldb
import re

def checkResult(res):
	if not res.Succeeded():
		print(res.GetOutput())
		print(res.GetError())
		exit(1)

def exec_and_find(commandInterpreter, cmd, regexp):
	res = lldb.SBCommandReturnObject()
	commandInterpreter.HandleCommand(cmd, res)
	checkResult(res)

	expr = re.compile(regexp)
	addr = None

	print(res.GetOutput())
	lines = res.GetOutput().splitlines()
	for line in lines:
		match = expr.match(line)
		if match is not None:
			addr = match.group(1)
			break

	print("Found addr: " + str(addr))
	return addr

def stop_in_main(commandInterpreter, process, assemblyName):
	res = lldb.SBCommandReturnObject()
	commandInterpreter.HandleCommand("bpmd " + assemblyName + " Program.Main", res)
	checkResult(res)
	print(res.GetOutput())
	print(res.GetError())
	res.Clear()


	# Use Python API to continue the process.  The listening thread should be
	# able to receive the state changed events.
	process.Continue()