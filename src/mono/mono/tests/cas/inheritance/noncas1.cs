using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

[PrincipalPermission (SecurityAction.InheritanceDemand, Name="me", Role="mono hacker")]
class BaseInheritanceDemand {

	public void Test () 
	{
		Console.WriteLine ("*1* [this should NOT print]");
	}
}

class InheritanceDemand : BaseInheritanceDemand {

	[STAThread]
	static int Main (string[] args)
	{
		try {
			new InheritanceDemand ().Test ();
			// this makes unhandled fails in the Makefile
			return 0;
		}
		catch (SecurityException se) {
			// actually we'll get an unhandled exception unless the
			// user is called "me" and part of the "mono hacker" group
			Console.WriteLine ("*2* Unexpected SecurityException\n{0}", se);
			return 2;
		}
		catch (Exception e) {
			Console.WriteLine ("*3* Unexpected Exception\n{0}", e);
			return 3;
		}
	}
}
