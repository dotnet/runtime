
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
	static IEnumerable<int> UntilTimeout (uint ms)
	{
		DateTime start = DateTime.UtcNow;
		for (int i = 0; (DateTime.UtcNow - start).TotalMilliseconds < ms ; i++)
			yield return i;
	}

	public static void Main ()
	{
		object count_lock = new object ();
		int count = 0;

		ParallelOptions options = new ParallelOptions {
			MaxDegreeOfParallelism = Environment.ProcessorCount * 4,
		};

		Thread t1 = new Thread (() => {
			ProcessStartInfo psi = new ProcessStartInfo () {
				FileName = "echo",
				Arguments = "hello",
				RedirectStandardOutput = true,
				UseShellExecute = false
			};

			Parallel.ForEach (UntilTimeout (15 * 1000), options, _ => {
				using (Process p = Process.Start (psi)) {
					p.BeginOutputReadLine ();
					p.WaitForExit ();
				}

				lock (count_lock) {
					count += 1;

					if (count % (10) == 0)
						Console.Write (".");
					if (count % (10 * 50) == 0)
						Console.WriteLine ();
				}
			});
		});

		t1.Start ();

		while (!t1.Join (0)) {
			try {
				using (Process p = Process.GetProcessById (1));
			} catch (ArgumentException) {
			}
		}
	}
}