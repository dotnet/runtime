using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, ControlPrincipal=true)]

public class Program {

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	public static int Test ()
	{
		return 2;
	}

	static int TestReflectedCall ()
	{
		MethodInfo mi = typeof (Program).GetMethod ("Test", BindingFlags.Static | BindingFlags.Public);
		if (mi == null) {
			Console.WriteLine ("*0* Couldn't reflect on call Test (normal).");
			return 0;
		} else {
			Console.WriteLine ("*1* Reflected on call Test (abnormal).");
			return 1;
		}
	}

	static int Main ()
	{
		try {
			int result = TestReflectedCall ();
			if (result == 2)
				Console.WriteLine ("*{0}* Unexpected calling thru reflection.", result);
			return result;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*3* Unexpected SecurityException.\n{0}", se);
			return 3;
		}
		catch (Exception e) {
			Console.WriteLine ("*4* Unexpected Exception.\n{0}", e);
			return 4;
		}
	}
}
