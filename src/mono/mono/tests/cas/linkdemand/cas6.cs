using System;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, ControlPrincipal=true)]

public class Program {

	int rc;

	[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
	public Program ()
	{
		Console.WriteLine ("*1* Constructor call expected to fail!");
		rc = 1;
	}

	public int InstanceTest ()
	{
		return rc;
	}

	static int Test ()
	{
		return new Program ().InstanceTest ();
	}

	static int Main ()
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
