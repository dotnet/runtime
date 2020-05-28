
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
			FileName = "find",
			Arguments = ". -maxdepth 3", // this test should be run from mono/tests, so that will list all test files
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		foreach (Action<Process> test in tests) {
			for (int i = 0; i < 200; ++i) {
				test (new Process () { StartInfo = psi });
			}
		}
	}

	static void Test1 (Process p)
	{
		ManualResetEvent mre_exit = new ManualResetEvent (false);
		ManualResetEvent mre_output = new ManualResetEvent (false);
		ManualResetEvent mre_error = new ManualResetEvent (false);

		p.EnableRaisingEvents = true;
		p.Exited += (s, a) => mre_exit.Set ();

		p.Start ();

		p.OutputDataReceived += (s, a) => {
			if (a.Data == null) {
				mre_output.Set ();
				return;
			}
		};

		p.ErrorDataReceived += (s, a) => {
			if (a.Data == null) {
				mre_error.Set ();
				return;
			}
		};

		p.BeginOutputReadLine ();
		p.BeginErrorReadLine ();

		if (!mre_exit.WaitOne (10000))
			Environment.Exit (1);
		if (!mre_output.WaitOne (10000))
			Environment.Exit (2);
		if (!mre_error.WaitOne (10000))
			Environment.Exit (3);
	}

	static void Test2 (Process p)
	{
		ManualResetEvent mre_output = new ManualResetEvent (false);
		ManualResetEvent mre_error = new ManualResetEvent (false);

		p.Start ();

		p.OutputDataReceived += (s, a) => {
			if (a.Data == null) {
				mre_output.Set ();
				return;
			}
		};

		p.ErrorDataReceived += (s, a) => {
			if (a.Data == null) {
				mre_error.Set ();
				return;
			}
		};

		p.BeginOutputReadLine ();
		p.BeginErrorReadLine ();

		if (!p.WaitForExit (10000))
			Environment.Exit (4);
		if (!mre_output.WaitOne (1000))
			Environment.Exit (5);
		if (!mre_error.WaitOne (1000))
			Environment.Exit (6);
	}
}
