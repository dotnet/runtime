using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

class BaseInheritanceDemand {

	[PrincipalPermission (SecurityAction.InheritanceDemand, Authenticated=false)]
	public virtual void Test () 
	{
		Console.WriteLine ("*0* BaseInheritanceDemand.Test [this should print]");
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
			return 0;
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
