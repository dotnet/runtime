using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

public class Program {

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	public static int Test ()
	{
		return 3;
	}

	[SecurityPermission (SecurityAction.Deny, ControlPrincipal=true)]
	static int TestReflectedCall ()
	{
		MethodInfo mi = typeof (Program).GetMethod ("Test", BindingFlags.Static | BindingFlags.Public);
		if (mi == null) {
			Console.WriteLine ("*1* Couldn't reflect on call Test (abnormal).");
			return 1;
		}
		Console.WriteLine ("*0* Reflected on call Test (normal).");
		// but invoking would throw a SecurityException!
		return 0;
	}

	[SecurityPermission (SecurityAction.Deny, ControlPrincipal=true)]
	static int Main ()
	{
		try {
			return TestReflectedCall ();
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
