using System;
using System.Threading;

class Test {
	static int Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		ThreadPool.QueueUserWorkItem ((a) => {
			throw new Exception ("From the threadpoool");
		});

		// Should not finish, OnUnhandledException exit path is expected to be executed
		Thread.Sleep (10000);

		return 3;
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		Environment.Exit (0);
	}
}

