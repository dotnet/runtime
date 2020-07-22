using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

using System.Diagnostics;

namespace wasm_test_driver
{
    class Program
    {
        static void Main(string[] args)
        {
	    string testAssemblyFile;

	    foreach (string arg in args) 
	    {
		Console.WriteLine($"!! Arg: {arg}");
	    }
	    
	    testAssemblyFile = args[0];
	    string testRunnerPath = args[1];

	    // testAssemblyFile = "/Users/naricc/workspace/runtime-webassembly-ci//artifacts/tests/coreclr/browser.wasm.Debug/test_assemblies.txt";
            // "/Users/naricc/workspace/runtime-webassembly-ci/src/common/wasm-test-runner/bin/Release/publish/";
	

	    List<AssemblyName> benchmarkAssemblyNames = File.ReadAllLines(testAssemblyFile).Select( file => AssemblyName.GetAssemblyName(file)).ToList();

            ProcessStartInfo processStartInfo = new ProcessStartInfo("sh");

	    processStartInfo.WorkingDirectory = testRunnerPath;
	    processStartInfo.UseShellExecute = true;

	    foreach (AssemblyName assemblyName in benchmarkAssemblyNames)
	    {
			processStartInfo.Arguments = $"run-v8.sh {assemblyName.Name}";

			Console.WriteLine($"---- Starting test {assemblyName.Name} ----");
			Process testProcess = Process.Start(processStartInfo);
			testProcess.WaitForExit();

			Console.WriteLine($"{assemblyName} exit code: {testProcess.ExitCode}");
	    }
        }
    }
}
