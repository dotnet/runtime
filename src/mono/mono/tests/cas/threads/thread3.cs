using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;

class Program {

	static void ThreadProc ()
	{
		if (debug)
			Console.WriteLine (Environment.StackTrace);

		Thread.Sleep (1000);
		try {
			// this will work
			Environment.Exit (0);
			Console.WriteLine ("UNEXPECTED execution");
		}
		catch (SecurityException se) {
			Console.WriteLine ("UNEXPECTED SecurityException {0}", se);
		}
	}

	static bool debug;

	// this Deny _WONT_ prevent Environment.Exit from working
	// even if the Deny is executed prior to the call
	[SecurityPermission (SecurityAction.Deny, UnmanagedCode = true)]
	static int End ()
	{
		Thread.Sleep (2000);
		return 1;
	}

	static int Main (string[] args)
	{
		debug = (args.Length > 0);
		Thread t = new Thread (new ThreadStart (ThreadProc));
		t.Start ();

		return End ();
	}
}
