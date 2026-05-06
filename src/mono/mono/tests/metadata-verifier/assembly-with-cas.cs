using System;
using System.Security;
using System.Security.Permissions;


public class Class
{
	[SecurityPermission (SecurityAction.LinkDemand)]
	public static void Method () 
	{
	}

	public static void Main ()
	{
	}
}
