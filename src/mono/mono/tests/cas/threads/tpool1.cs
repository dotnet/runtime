using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;

class Program {

	static void ShowStackTrace (object o)
	{
		if (debug)
			Console.WriteLine ((int)o);

		try {
			Console.WriteLine (Environment.UserName);
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
	static int result = 0;

	// this Deny will prevent the Environment.UserName property from working
	[EnvironmentPermission (SecurityAction.Deny, Read = "USERNAME")]
	static int Main (string[] args)
	{
		debug = (args.Length > 0);
		if (debug) {
			SecurityManager.SecurityEnabled = (args [0] != "off");
		}

		if (SecurityManager.SecurityEnabled) {
			Console.WriteLine ("SecurityManager.SecurityEnabled: true");
			ShowStackTrace ((object)-1);
		} else {
			Console.WriteLine ("SecurityManager.SecurityEnabled: false");
		}

		result = 0;
		for (int i=0; i < 5; i++)
			ThreadPool.QueueUserWorkItem (new WaitCallback (ShowStackTrace), i);
		
		System.Threading.Thread.Sleep (5000);
		return result;
	}
}
