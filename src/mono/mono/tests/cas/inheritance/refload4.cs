using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, ControlPrincipal=true)]

public class Program {

	static int Main (string[] args)
	{
		try {
			string filename = "library2b";
			Assembly a = Assembly.Load (filename);
			if (a == null) {
				Console.WriteLine ("*1* Couldn't load assembly '{0}'.", filename);
				return 1;
			} else {
				Console.WriteLine ("*0* Assembly '{0}' loaded.", filename);
				return 0;
			}
		}
		catch (SecurityException se) {
			Console.WriteLine ("*2* Unexpected SecurityException\n{0}", se);
			return 2;
		}
		catch (Exception e) {
			Console.WriteLine ("*3* Unexpected Exception\n{0}", e);
			return 3;
		}
	}
}
