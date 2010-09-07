
using System;
using System.Threading;

class Test {
	static int Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		OtherDomain ();
		return 0;
	}

	static void OtherDomain ()
	{
		AppDomain domain = AppDomain.CreateDomain ("test");
		ThreadPool.QueueUserWorkItem (unused => {
			domain.DoCallBack (() => {
				// This will get a ThreadAbortedException
				Thread.Sleep (10000);
				});
			});
		Thread.Sleep (1000);
		AppDomain.Unload (domain);
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		Environment.Exit (1);
	}
}

