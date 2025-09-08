using System;
using System.Threading;

class Test {
	static int Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		WaitCallback wcb = new WaitCallback ((a) => {
			Thread.CurrentThread.Abort ();
		});
		wcb.BeginInvoke (wcb, null, null);
		Thread.Sleep (1000);
		return 0;
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		Environment.Exit (1);
	}
}

