using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;

[PrincipalPermission (SecurityAction.LinkDemand, Authenticated=false)]
public class LinkDemand {

	static public int Test () 
	{
		Console.WriteLine ("*0* [this should print]");
		return 0;
	}
}

class LinkDemandTest {

	static int Test ()
	{
		GenericIdentity identity = new GenericIdentity ("me");
		string[] roles = new string [1] { "mono hacker" };
		Thread.CurrentPrincipal = new GenericPrincipal (identity, roles);

		return LinkDemand.Test ();
	}

	[STAThread]
	static int Main (string[] args)
	{
		try {
			return Test ();
		}
		catch (SecurityException se) {
			Console.WriteLine ("*1* Unexpected SecurityException\n{0}", se);
			return 1;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected Exception\n{0}", e);
			return 2;
		}
	}
}

