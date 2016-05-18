import unittest
import argparse
import re
import tempfile
import subprocess
import threading
import os
import os.path
import sys

assemblyName=''
clrArgs=''
fail_flag='/tmp/fail_flag'

# helper functions

def prepareScenarioFile(moduleName):
	global assemblyName
	#create a temporary scenario file
	fd, scenarioFileName = tempfile.mkstemp()
	scenarioFile = open(scenarioFileName, 'w')
	scenarioFile.write('script from runprocess import run\n')
	scenarioFile.write('script run("'+assemblyName+'", "'+moduleName+'")\n')
	scenarioFile.write('quit\n')
	scenarioFile.close()
	os.close(fd)
	return scenarioFileName

def runWithTimeout(cmd, timeout):
	d = {'process': None}
	def run():
		d['process'] = subprocess.Popen(cmd, shell=True)
		d['process'].communicate()

	thread = threading.Thread(target=run)
	thread.start()

	thread.join(timeout)
	if thread.is_alive():
		d['process'].terminate()
		thread.join()

# Test class
class TestSosCommands(unittest.TestCase):

	def do_test(self, command):
		global clrArgs
		global fail_flag
		filename = prepareScenarioFile(command)
		cmd = "lldb --source "+filename+" -b -K \"OnCrash.do\" -- "+clrArgs+" > "+command+".log 2>"+command+".log.2"
		runWithTimeout(cmd, 120)
		self.assertFalse(os.path.isfile(fail_flag))
		os.unlink(filename)

	def test_dumpmodule(self):
		self.do_test("dumpmodule")

	def test_dumpil(self):
		self.do_test("dumpil")
	

if __name__ == '__main__':
	parser = argparse.ArgumentParser()
	parser.add_argument('--clr-args', default='')
	parser.add_argument('unittest_args', nargs='*')

	args = parser.parse_args()

	clrArgs = args.clr_args
	print("ClrArgs: " + clrArgs)
	# find assembly name among lldb arguments
	assembly_regexp = re.compile("([^\s]+\.exe)")
	assemblyMatch = assembly_regexp.search(clrArgs)
	if assemblyMatch is not None:
		assemblyName = assemblyMatch.group(1)
	else:
		print("Assembly not recognized")
		exit(1)

	print("Assembly name: "+assemblyName)
	sys.argv[1:] = args.unittest_args
	suite = unittest.TestLoader().loadTestsFromTestCase(TestSosCommands)
	unittest.TextTestRunner(verbosity=2).run(suite)
	os.unlink(fail_flag)