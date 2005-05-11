using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;

public class Program {

	delegate void Test ();
	
	public void TestInstance ()
	{
		if (debug) {
			string name = Thread.CurrentThread.Name;
			if (name == null)
				name = "[unnamed]";

			Console.WriteLine ("\tDelegate: {0} (from pool: {1})\n{2}", name, 
				Thread.CurrentThread.IsThreadPoolThread, Environment.StackTrace);
		}

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
	public static int Main (string[] args)
	{
		debug = (args.Length > 0);
		if (debug) {
			Thread.CurrentThread.Name = "Main";
			Console.WriteLine (">Thread.Name: {0}", Thread.CurrentThread.Name);
		}

		Program p = new Program ();
		Test t = new Test (p.TestInstance);
		IAsyncResult ar = t.BeginInvoke (null, null);

		if (debug)
			Console.WriteLine ("\tIAsyncResult type is {0}", ar.GetType ());

		t.EndInvoke (ar);

		if (debug)
			Console.WriteLine ("<Thread.Name: {0}", Thread.CurrentThread.Name);
		
		return 0;
	}
}
