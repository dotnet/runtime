using System;
using System.Security;
using System.Security.Permissions;

namespace System {

	public class Program {

		[SecurityPermission (SecurityAction.LinkDemand, Unrestricted=true)]
		static void Test ()
		{
			Console.WriteLine ("[SecurityPermission (SecurityAction.LinkDemand, Unrestricted=true)]");
		}

		static int Main ()
		{
			IPermission p = (IPermission) new SecurityPermission (PermissionState.Unrestricted);
			bool granted = SecurityManager.IsGranted (p);
			int retval = 3;

			try {
				Test ();
				retval = (granted ? 0 : 1);
				Console.WriteLine ("*{0}* LinkDemand", retval);
			}
			catch (SecurityException se) {
				retval = (granted ? 1 : 0);
				Console.WriteLine ("*{0}* SecurityException\n{1}", retval, se);
			}
			catch (Exception e) {
				Console.WriteLine ("*2* Unexpected exception\n{0}", e);
				return 2;
			}

			return retval;
		}
	}
}
