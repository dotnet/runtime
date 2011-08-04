using System;
using System.Threading;

class Test {
	static object monitor;

	static int Main ()
	{
		monitor = new object ();
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		WaitCallback wcb = new WaitCallback ((a) => {
			throw new Exception ("From the threadpoool");
		});
		wcb.BeginInvoke (wcb, OnCBFinished, null);
		lock (monitor) {
			Monitor.Wait (monitor);
		}
		Thread.Sleep (1000);
		return 1;
	}

	static void OnCBFinished (object arg)
	{
		throw new Exception ("From OnCBFinished");
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		lock (monitor) {
			Monitor.Pulse (monitor);
		}
		Environment.Exit (0);
	}
}

