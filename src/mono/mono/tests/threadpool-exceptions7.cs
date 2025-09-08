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
				AppDomain.CurrentDomain.SetData ("key", "checked");

				// This will get a ThreadAbortedException
				Thread.Sleep (10000);
			});
		});

		if (!SpinWait.SpinUntil (() => domain.GetData ("key") as string == "checked", 10000))
			Environment.Exit (4);

		AppDomain.Unload (domain);
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		Environment.Exit (3);
	}
}

