using System;
using System.Threading;

class Test {
	static int Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		ThreadPool.QueueUserWorkItem ((a) => {
			throw new Exception ("From the threadpoool");
			});
		Thread.Sleep (1000);
		return 1;
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		Environment.Exit (0);
	}
}

