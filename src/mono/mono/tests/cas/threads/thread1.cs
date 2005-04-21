using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;

class Program {

	static void ThreadProc ()
	{
		if (debug)
			Console.WriteLine (Environment.StackTrace);

		try {
			Environment.Exit (1);
		}
		catch (SecurityException se) {
			if (debug)
				Console.WriteLine ("EXPECTED SecurityException {0}", se);
		}
	}

	static bool debug;

	// this Deny will prevent Environment.Exit from working
	[SecurityPermission (SecurityAction.Deny, UnmanagedCode = true)]
	static int Main (string[] args)
	{
		debug = (args.Length > 0);
		Thread t = new Thread (new ThreadStart (ThreadProc));
		t.Start ();
		Thread.Sleep (1000);
		return 0;
	}
}
