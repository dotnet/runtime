using System;
using System.Security;
using System.Security.Permissions;

public class Program {

	[SecurityPermission (SecurityAction.Assert, UnmanagedCode=true)]
	[SecurityPermission (SecurityAction.Demand, UnmanagedCode=true)]
	static int Test ()
	{
		return 1;
	}

	[SecurityPermission (SecurityAction.Deny, UnmanagedCode=true)]
	static int Main ()
	{
		int result = 2;
		try {
			result = Test ();
			Console.WriteLine ("*1* Unexpected call to Test");
		}
		catch (SecurityException se) {
			result = 0;
			Console.WriteLine ("*0* Expected SecurityException\n{0}", se);
		}
		return result;
	}
}
