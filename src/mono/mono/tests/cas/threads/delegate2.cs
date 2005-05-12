using System;
using System.Reflection;
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

			Console.WriteLine ("\tDelegate running on thread: {0} (from pool: {1})\n{2}", name, 
				Thread.CurrentThread.IsThreadPoolThread, Environment.StackTrace);
		}

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
	}

	static bool debug;
	static int result;

	// this Deny will prevent the Assembly.Evidence property from working
	[SecurityPermission (SecurityAction.Deny, ControlEvidence = true)]
	public static int Main (string[] args)
	{
		result = 0;
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
		
		return result;
	}
}
