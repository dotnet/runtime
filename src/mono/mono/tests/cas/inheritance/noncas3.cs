using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

class BaseInheritanceDemand {

	[PrincipalPermission (SecurityAction.InheritanceDemand, Name="me", Role="mono hacker")]
	public virtual void Test () 
	{
		Console.WriteLine ("*1* BaseInheritanceDemand.Test [this should NOT print]");
	}
}

class InheritanceDemand : BaseInheritanceDemand {

	public override void Test () 
	{
		base.Test ();
	}

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
