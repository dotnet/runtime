using System;
using System.Security.Permissions;

public class Program {

	[SecurityPermission (SecurityAction.Deny, UnmanagedCode=true)]
	[SecurityPermission (SecurityAction.Demand, UnmanagedCode=true)]
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
