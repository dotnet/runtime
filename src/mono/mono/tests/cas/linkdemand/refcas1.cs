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
			Console.WriteLine ("*1* Couldn't reflect on call Test");
			return 1;
		}
		return (int) mi.Invoke (null, null);
	}

	static int Main ()
	{
		try {
			int result = TestReflectedCall ();
			if (result == 0)
				Console.WriteLine ("*0* Could reflection on method (normal).");
			return result;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*2* Unexpected SecurityException.\n{0}", se);
			return 2;
		}
		catch (Exception e) {
			Console.WriteLine ("*3* Unexpected Exception.\n{0}", e);
			return 3;
		}
	}
}
