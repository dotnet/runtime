using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;

class LinkDemandTest {

	// Note: this should also fail if you replace "me" with your username

	[PrincipalPermission (SecurityAction.LinkDemand, Name="me")]
	static int LinkDemand () 
	{
		Console.WriteLine ("*1* [this should not print]");
		return 1;
	}

	static int Test ()
	{
		Console.WriteLine ("[this should not print - as JIT will reject the LinkDemand]");

		GenericIdentity identity = new GenericIdentity ("me");
		string[] roles = new string [1] { "mono hacker" };
		Thread.CurrentPrincipal = new GenericPrincipal (identity, roles);

		return LinkDemand ();
	}

	[STAThread]
	static int Main (string[] args)
	{
		try {
			return Test ();
		}
		catch (SecurityException se) {
			Console.WriteLine ("*0* Expected SecurityException\n{0}", se);
			return 0;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected Exception\n{0}", e);
			return 2;
		}
	}
}

