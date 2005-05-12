using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Timers;
using System.Windows.Forms;

class Program {

	static void ShowStackTrace (object o, ElapsedEventArgs e)
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
			t.AutoReset = false;
			t.Enabled = false;
		}
	}

	static bool debug;
	static int counter = 0;
	static int result = 0;
	static System.Timers.Timer t;

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

		Label label = new Label ();

		t = new System.Timers.Timer (500);
		t.SynchronizingObject = label;
		t.Elapsed += new ElapsedEventHandler (ShowStackTrace);
		t.AutoReset = true;
		t.Enabled = true;

		// Mono SWF async support requires the message loop in action		
		while (counter <= 5)
			Application.DoEvents ();

		return result;
	}
}
