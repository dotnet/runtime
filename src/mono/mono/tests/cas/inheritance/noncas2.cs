using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

[PrincipalPermission (SecurityAction.InheritanceDemand, Authenticated=false)]
class BaseInheritanceDemand {

	public void Test () 
	{
		Console.WriteLine ("*0* [this should print]");
	}
}

class InheritanceDemand : BaseInheritanceDemand {

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
