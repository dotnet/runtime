using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

public class Program {

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	public static int Test ()
	{
		return 1;
	}

	static MethodInfo GetReflectedCall ()
	{
		return typeof (Program).GetMethod ("Test", BindingFlags.Static | BindingFlags.Public);
	}

	static int LinkReflectedCall (MethodInfo mi)
	{
		// if a LinkDemand is enforced this will succeed;
		// if a full Demand is enforced (stack walk) then it will fail
		return (int) mi.Invoke (null, null);
	}

	[SecurityPermission (SecurityAction.Deny, ControlPrincipal=true)]
	static int CallReflectedCall (MethodInfo mi)
	{
		return LinkReflectedCall (mi);
	}

	static int Main ()
	{
		try {
			MethodInfo mi = GetReflectedCall ();
			if (mi == null) {
				Console.WriteLine ("*2* Couldn't reflect on call Test (failure).");
				return 2;
			} else {
				int result = CallReflectedCall (mi);
				if (result == 1)
					Console.WriteLine ("*{0}* Unexpected calling thru reflection.", result);
				else
					Console.WriteLine ("*{0}* Unexpected return value from reflection.", result);

				return result;
			}
		}
		catch (SecurityException) {
			Console.WriteLine ("*0* Expected SecurityException.");
			return 0;
		}
	}
}
