using System;
using System.Diagnostics;

namespace wasm_test_driver
{
    class Program
    {
        static void Main(string[] args)
        {
	    string[] benchmarkAssemblyNames = new string [] { "runningmono" };

	    string runtimeJs = "";
            string wasmTestRunnerAssembly = "";


        ProcessStartInfo processStartInfo = new ProcessStartInfo("sh");
	    processStartInfo.WorkingDirectory = "/Users/naricc/workspace/runtime-webassembly-ci/src/common/wasm-test-runner/bin/Release/publish/";
	    processStartInfo.UseShellExecute = true;

	    foreach (string assemblyName in benchmarkAssemblyNames)
	    {
			processStartInfo.Arguments = $"run-v8.sh {assemblyName}";
			Process.Start(processStartInfo);
	    }
        }
    }
}
