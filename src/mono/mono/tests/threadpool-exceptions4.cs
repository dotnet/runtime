using System;
using System.Threading;

class Test {
	static int Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		WaitCallback wcb = new WaitCallback ((a) => {
			throw new Exception ("From the threadpoool");
		});
		wcb.BeginInvoke (wcb, OnCBFinished, null);

		// Should not finish, OnUnhandledException exit path is expected to be executed
		Thread.Sleep (10000);

		return 2;
	}

	static void OnCBFinished (object arg)
	{
		throw new Exception ("From OnCBFinished");
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		Environment.Exit (0);
	}
}

