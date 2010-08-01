using System;
using System.Threading;

class Test {
	static int return_value = 2;
	static int Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		WaitCallback wcb = new WaitCallback ((a) => {
			Thread.CurrentThread.Abort();
		});
		wcb.BeginInvoke (wcb, OnCBFinished, null);
		Thread.Sleep (1000);
		return 1;
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		string str = e.ExceptionObject.ToString ();
		if (str.IndexOf ("From the threadpool") != -1)
			return_value = 3;
		Environment.Exit (return_value);
	}

	static void OnCBFinished (object arg)
	{
		return_value = 0;
		throw new Exception ("From OnCBFinished");
	}
}

