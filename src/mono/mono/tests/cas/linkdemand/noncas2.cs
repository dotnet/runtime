using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;

// Note: this should also fail if you replace "me" with your username

[PrincipalPermission (SecurityAction.LinkDemand, Name="me")]
public class LinkDemand {

	static public int Test () 
	{
		Console.WriteLine ("*1* [this should not print]");
		return 1;
	}
}

class LinkDemandTest {

	static int Test ()
	{
		Console.WriteLine ("[this should not print - as JIT will reject the LinkDemand]");

		GenericIdentity identity = new GenericIdentity ("me");
		string[] roles = new string [1] { "mono hacker" };
		Thread.CurrentPrincipal = new GenericPrincipal (identity, roles);

		// Note: if the next line is commented then no exception will 
		// be thrown as the JIT will never reach the LinkDemand class
		return LinkDemand.Test ();
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
