using System;
using System.Security;
using System.Security.Permissions;
using System.Timers;

class Program {

	static void ShowStackTrace (object o, ElapsedEventArgs e)
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
			t.AutoReset = false;
			t.Enabled = false;
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
		if (debug) {
			SecurityManager.SecurityEnabled = (args [0] != "off");
		}

		if (SecurityManager.SecurityEnabled) {
			Console.WriteLine ("SecurityManager.SecurityEnabled: true");
			ShowStackTrace (null, null);
		} else {
			Console.WriteLine ("SecurityManager.SecurityEnabled: false");
		}

		t = new Timer (500);
		t.Elapsed += new ElapsedEventHandler (ShowStackTrace);
		t.AutoReset = true;
		t.Enabled = true;
		
		System.Threading.Thread.Sleep (5000);
		return 0;
	}
}
