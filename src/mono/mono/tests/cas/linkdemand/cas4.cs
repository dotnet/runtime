using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

public class Program {

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	public static int Test ()
	{
		return 0;
	}

	static int TestReflectedCall ()
	{
		MethodInfo mi = typeof (Program).GetMethod ("Test", BindingFlags.Static | BindingFlags.Public);
		if (mi == null) {
			Console.WriteLine ("*3* Couldn't reflect on call Test");
			return 3;
		}

		return (int) mi.Invoke (null, null);
	}

	static int Main ()
	{
		try {
			int result = TestReflectedCall ();
			if (result == 0)
				Console.WriteLine ("*0* Calling thru reflection.");
			return result;
		}
		catch (SecurityException) {
			Console.WriteLine ("*1* Unexpected Unhandled SecurityException.");
			return 1;
		}
	}
}
