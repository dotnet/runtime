using System;
using System.Diagnostics;
using System.Threading;

class Modules {
	static void Run() {
		Process proc = new Process();
		bool ret;

		proc.StartInfo.FileName="wibble";
		proc.StartInfo.Arguments="arg1    arg2\targ3 \"arg4a arg4b\"";
		proc.StartInfo.UseShellExecute=false;
		ret=proc.Start();

		Console.WriteLine("Start returns " + ret);
		Console.WriteLine("Process is " + proc.ToString());
		Console.WriteLine("Pid is " + proc.Id);
		Console.WriteLine("Handle is " + proc.Handle);
		Console.WriteLine("HandleCount is " + proc.HandleCount);

		Console.WriteLine("Waiting for exit...");
		proc.WaitForExit();
		Console.WriteLine("Wait returned");
		Console.WriteLine("Exit code is " + proc.ExitCode);
		Console.WriteLine("Process started at " + proc.StartTime);
		Console.WriteLine("Process ended at " + proc.ExitTime);
	}

	static void Main() {
		Run();
		System.Threading.Thread.Sleep(10000);
		Run();
	}
}

