
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
	static void Main ()
	{
		for (int i = 0; i < 1000; ++i) {
			ProcessStartInfo psi = new ProcessStartInfo () {
				FileName = "echo",
				Arguments = "hello",
				RedirectStandardOutput = true,
				UseShellExecute = false
			};

			Process p = Process.Start (psi);

			ManualResetEvent mre = new ManualResetEvent (false);

			Task t = Task.Run (() => {
				mre.Set ();
				p.BeginOutputReadLine ();
				if (!p.WaitForExit (10000))
					Environment.Exit (1);
			});

			if (!mre.WaitOne (10000))
				Environment.Exit (2);
			if (!p.WaitForExit (10000))
				Environment.Exit (3);

			if (!t.Wait (10000))
				Environment.Exit (4);
		}
	}
}
