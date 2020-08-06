using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Diagnostics;

namespace wasm_test_driver
{
    class Program
    {
        public static void Main(string[] args)
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
	

	    List<Tuple<AssemblyName, string>> benchmarkAssemblyNames = File.ReadAllLines(testAssemblyFile).Select( file => Tuple.Create(AssemblyName.GetAssemblyName(file), file)).ToList();

            ProcessStartInfo processStartInfo = new ProcessStartInfo("sh");

	    processStartInfo.WorkingDirectory = testRunnerPath;
	    processStartInfo.UseShellExecute = true;

	    foreach (Tuple<AssemblyName, string> assemblyNameFile in benchmarkAssemblyNames)
	    {
		        AssemblyName assemblyName = assemblyNameFile.Item1;
			string fileName = assemblyNameFile.Item2;

			if (hasMain(fileName) )
			 {

				processStartInfo.Arguments = $"run-v8.sh {assemblyName.Name}";

				Console.WriteLine($"---- Starting test {assemblyName.Name} ----");
				Process testProcess = Process.Start(processStartInfo);
				testProcess.WaitForExit();

				Console.WriteLine($"WasmTestDriver: {fileName} {assemblyName} exit code: {testProcess.ExitCode}");
			}

	    }
        }

	static bool hasMain(string assemblyFile)
	{
		var stream = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

	        return new PEReader(stream).PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress > 0;
	}
    }
}
