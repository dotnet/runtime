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

	// we throw as soon as we try to compile this
	static void DeepTest ()
	{
		Test ();
	}

	// no problem compiling Test as linking to DeepTest is possible
	static void Test (int i)
	{
		if (i == 200) {
			DeepTest ();
		}
	}

	static int Main ()
	{
		int err = 2;
		int i = 0;
		try {
			for (i=0; i < 256; i++) {
				Test (i);
			}
			Console.WriteLine ("*{0}* Iteration Completed", err);
		}
		catch (SecurityException) {
			err = (i == 200) ? 0 : 1;
			Console.WriteLine ("*{0}* Iteration Count: {1}", err, i);
		}
		return err;
	}
}
