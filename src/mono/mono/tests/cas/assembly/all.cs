using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestMinimum, Execution=true)]
//[assembly: PermissionSet (SecurityAction.RequestOptional, Unrestricted=true)]
[assembly: SecurityPermission (SecurityAction.RequestOptional, Flags=SecurityPermissionFlag.AllFlags)]
[assembly: SecurityPermission (SecurityAction.RequestRefuse, SkipVerification=true)]

public class Program {

	static public int Main (string[] args)
	{
		object[] attrs = Assembly.GetExecutingAssembly ().GetCustomAttributes (false);
		for (int i = 0; i < attrs.Length; i++) {
			if (attrs [i] is PermissionSetAttribute) {
				PermissionSetAttribute psa = (attrs [i] as PermissionSetAttribute);
				Console.WriteLine ("{0} - {1}", psa.Action, psa.CreatePermissionSet ());
			} else if (attrs [i] is SecurityAttribute) {
				SecurityAttribute sa = (attrs [i] as SecurityAttribute);
				IPermission p = sa.CreatePermission ();
				PermissionSet ps = new PermissionSet (PermissionState.None);
				ps.AddPermission (p);
				Console.WriteLine ("{0} - {1}", sa.Action, ps);
			} else {
				Console.WriteLine (attrs [i]);
			}
		}
		return 0;
	}
}
