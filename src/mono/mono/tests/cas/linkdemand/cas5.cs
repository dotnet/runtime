using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, ControlPrincipal=true)]

public class Program {

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	public static int Test ()
	{
		return 1;
	}

	static int TestReflectedCall ()
	{
		MethodInfo mi = typeof (Program).GetMethod ("Test", BindingFlags.Static | BindingFlags.Public);
		if (mi == null) {
			Console.WriteLine ("*0* Couldn't reflect on call Test (normal).");
			return 0;
		}

		return (int) mi.Invoke (null, null);
	}

	static int Main ()
	{
		try {
			int result = TestReflectedCall ();
			if (result != 0)
				Console.WriteLine ("*{0}* Unexpected calling thru reflection.", result);
			return result;
		}
		catch (SecurityException) {
			Console.WriteLine ("*2* Unexpected Unhandled SecurityException.");
			return 2;
		}
	}
}
