using System;
using System.Threading;

class Test {

	static ManualResetEvent mre = new ManualResetEvent (false);

	static int Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		WaitCallback wcb = new WaitCallback ((a) => {
			throw new Exception ("From the threadpoool");
		});
		wcb.BeginInvoke (wcb, OnCBFinished, null);

		if (!mre.WaitOne (10000))
			return 2;

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		/* expected exit code: 255 */
		Thread.Sleep (10000);
		return 0;
	}

	static void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
	{
		string str = e.ExceptionObject.ToString ();
		if (!str.Contains ("From OnCBFinished")) {
			Environment.Exit (3);
			return;
		}

		if (!e.IsTerminating) {
			Environment.Exit (4);
			return;
		}

		mre.Set ();
	}

	static void OnCBFinished (object arg)
	{
		throw new Exception ("From OnCBFinished");
	}
}

