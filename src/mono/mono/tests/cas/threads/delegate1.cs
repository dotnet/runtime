using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;

public class Program {

	delegate void Test ();
	
	public static void TestStatic ()
	{
		if (debug) {
			string name = Thread.CurrentThread.Name;
			if (name == null)
				name = "[unnamed]";

			Console.WriteLine ("\tDelegate: {0}\n{1}", name, Environment.StackTrace);
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

		Test t = new Test (TestStatic);
		IAsyncResult ar = t.BeginInvoke (null, null);
		t.EndInvoke (ar);

		if (debug)
			Console.WriteLine ("<Thread.Name: {0}", Thread.CurrentThread.Name);
		
		return 0;
	}
}
