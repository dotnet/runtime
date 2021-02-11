using System;
using System.IO;
using System.Diagnostics;

public class Test {
	public static int Main(string[] args)
	{
		// Only run this test on Unix
		int pl = (int) Environment.OSVersion.Platform;
		if ((pl != 4) && (pl != 6) && (pl != 128)) {
			return 0;
		}

		// Try to invoke the helper assembly
		// Return 0 only if it is successful
		try
		{
			var name = "bug-17537-helper.exe";
			Console.WriteLine ("Launching subprocess: {0}", name);
			var p = new Process();
			p.StartInfo.FileName = Path.Combine (AppDomain.CurrentDomain.BaseDirectory + name);
			p.StartInfo.UseShellExecute = false;

			var result = p.Start();
			p.WaitForExit(1000);
			if (result) {
				Console.WriteLine ("Subprocess started successfully");
				return 0;
			} else {
				Console.WriteLine ("Subprocess failure");
				return 1;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine ("Subprocess exception");
			Console.WriteLine (e.Message);
			return 1;
		}
	}
}
