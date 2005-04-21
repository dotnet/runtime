using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;

class Program {

	static void ShowStackTrace (object o)
	{
		if (debug)
			Console.WriteLine ("{0}: {1}", counter, Environment.StackTrace);

		try {
			Environment.Exit (1);
		}
		catch (SecurityException se) {
			if (debug)
				Console.WriteLine ("EXPECTED SecurityException {0}", se);
		}

		if (counter++ > 5) {
			t.Dispose ();
		}
	}

	static bool debug;
	static int counter = 0;
	static Timer t;

	// this Deny will prevent Environment.Exit from working
	[SecurityPermission (SecurityAction.Deny, UnmanagedCode = true)]
	static int Main (string[] args)
	{
		debug = (args.Length > 0);

		ShowStackTrace (null);

		TimerCallback cb = new TimerCallback (ShowStackTrace);
		t = new Timer (cb, null, 500, 1000);

		Thread.Sleep (5000);
		return 0;
	}
}
