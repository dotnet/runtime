using System;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, ControlPrincipal=true)]

public class Program {

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	public static int Test ()
	{
		return 0;
	}

	static int Main ()
	{
		// this isn't catchable as the exception occurs when compiling Main
		try {
			Test ();
			Console.WriteLine ("*0* Expected Unhandled SecurityException.");
			return 0;
		}
		catch (SecurityException) {
			Console.WriteLine ("*0* This SecurityException shouldn't be catched.");
			return 0;
		}
	}
}
