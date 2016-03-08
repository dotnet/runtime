
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
	static void Main ()
	{
		Action<Process>[] tests = new Action<Process> [] {
			new Action<Process> (Test1),
			new Action<Process> (Test2),
		};

		ProcessStartInfo psi = new ProcessStartInfo () {
			FileName = "echo",
			Arguments = "hello",
			UseShellExecute = false,
			RedirectStandardOutput = true,
		};

		foreach (Action<Process> test in tests) {
			for (int i = 0; i < 500; ++i) {
				test (new Process () { StartInfo = psi });
			}
		}
	}

	static void Test1 (Process p)
	{
		StringBuilder sb = new StringBuilder ();
		ManualResetEvent mre_exit = new ManualResetEvent (false);
		ManualResetEvent mre_output = new ManualResetEvent (false);

		p.EnableRaisingEvents = true;
		p.Exited += (s, a) => mre_exit.Set ();

		p.Start ();

		p.OutputDataReceived += (s, a) => {
			if (a.Data == null) {
				mre_output.Set ();
				return;
			}
			sb.Append (a.Data);
		};

		p.BeginOutputReadLine ();

		if (!mre_exit.WaitOne (1000))
			Environment.Exit (1);
		if (!mre_output.WaitOne (1000))
			Environment.Exit (2);

		if (sb.ToString () != "hello") {
			Console.WriteLine ("process output = '{0}'", sb.ToString ());
			Environment.Exit (3);
		}
	}

	static void Test2 (Process p)
	{
		StringBuilder sb = new StringBuilder ();
		ManualResetEvent mre_output = new ManualResetEvent (false);

		p.Start ();

		p.OutputDataReceived += (s, a) => {
			if (a.Data == null) {
				mre_output.Set ();
				return;
			}

			sb.Append (a.Data);
		};

		p.BeginOutputReadLine ();

		if (!p.WaitForExit (1000))
			Environment.Exit (4);
		if (!mre_output.WaitOne (1000))
			Environment.Exit (5);

		if (sb.ToString () != "hello") {
			Console.WriteLine ("process output = '{0}'", sb.ToString ());
			Environment.Exit (6);
		}
	}
}
