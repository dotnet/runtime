using System;
using System.Security;
using System.Security.Permissions;

public class Program {

	[SecurityPermission (SecurityAction.PermitOnly, UnmanagedCode=true)]
	[SecurityPermission (SecurityAction.Demand, ControlAppDomain=true)]
	static int Test ()
	{
		Console.WriteLine ("*0* Expected call to Test()");
		return 0;
	}

	static int Main ()
	{
		return Test ();
	}
}
