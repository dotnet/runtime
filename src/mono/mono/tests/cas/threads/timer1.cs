using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Threading;

class Program {

	static void ShowStackTrace (object o)
	{
		if (debug)
			Console.WriteLine ("{0}: {1}", counter, Environment.StackTrace);

		try {
			Console.WriteLine (Assembly.GetExecutingAssembly ().Evidence.Count);
			result = 1;
		}
		catch (SecurityException se) {
			if (debug)
				Console.WriteLine ("EXPECTED SecurityException {0}", se);
		}
		catch (Exception ex) {
			Console.WriteLine ("UNEXPECTED {0}", ex);
			result = 1;
		}

		if (counter++ > 5) {
			t.Dispose ();
		}
	}

	static bool debug;
	static int counter = 0;
	static int result = 0;
	static Timer t;

	// this Deny will prevent the Assembly.Evidence property from working
	[SecurityPermission (SecurityAction.Deny, ControlEvidence = true)]
	static int Main (string[] args)
	{
		debug = (args.Length > 0);
		if (debug) {
			SecurityManager.SecurityEnabled = (args [0] != "off");
		}

		ShowStackTrace (null);

		TimerCallback cb = new TimerCallback (ShowStackTrace);
		t = new Timer (cb, null, 500, 1000);

		Thread.Sleep (5000);
		return result;
	}
}
