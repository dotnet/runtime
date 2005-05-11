using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

class Program {

	static void ShowStackTrace (object o, EventArgs e)
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

		counter++;
	}

	static bool debug;
	static int counter;
	static int result = 0;

	// this Deny will prevent the Assembly.Evidence property from working
	[SecurityPermission (SecurityAction.Deny, ControlEvidence = true)]
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

		Timer t = new Timer ();
		t.Tick += new EventHandler (ShowStackTrace);
		t.Interval = 1000;
		t.Start ();
		
		while (counter <= 5)
			Application.DoEvents ();

		return result;
	}
}
