using System;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, ControlPrincipal=true)]

namespace System {

	public class Program {

		[SecurityPermission (SecurityAction.LinkDemand, ControlPrincipal=true)]
		static int Main ()
		{
			Console.WriteLine ("*0* LinkDemand is ignored on entrypoint.");
			return 0;
		}
	}
}
