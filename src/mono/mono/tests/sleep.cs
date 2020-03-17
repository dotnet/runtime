using System;
using System.Threading;
using System.Diagnostics;

public class Tests
{
	static bool finished;

	public static int Main (String[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_time_drift () {
		// Test the Thread.Sleep () is able to deal with time drifting due to interrupts
		Thread t = new Thread (delegate () {
				while (!finished) {
					GC.Collect ();
					Thread.Yield ();
				}
			});
		t.Start ();

		var sw = Stopwatch.StartNew ();
		Thread.Sleep (1000);
		finished = true;
		sw.Stop ();
		if (sw.ElapsedMilliseconds > 2000) {
			Console.WriteLine (sw.ElapsedMilliseconds);
			return 1;
		} else {
			return 0;
		}
	}
}
