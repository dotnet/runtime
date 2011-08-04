using System;
using System.Threading;

class Test {
	static object monitor;

	static int Main ()
	{
		monitor = new object ();
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		ThreadPool.QueueUserWorkItem ((a) => {
			throw new Exception ("From the threadpoool");
			});
		lock (monitor) {
			Monitor.Wait (monitor);
		}
		Thread.Sleep (1000);
		return 1;
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		lock (monitor) {
			Monitor.Pulse (monitor);
		}
		Environment.Exit (0);
	}
}

