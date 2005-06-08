using System;
using System.Security;
using System.Security.Permissions;

class Program {

	static void Main (string[] args) 
	{
		long quota = 0;
		if (args.Length > 0)
			quota = Convert.ToInt64 (args [0]);

		IsolatedStorageFilePermission isfp = new IsolatedStorageFilePermission (PermissionState.None);
		isfp.UserQuota = quota;
		try {
			isfp.Demand ();
			Console.WriteLine ("Quota accepted for {0}.", quota);
		}
		catch (SecurityException se) {
			Console.WriteLine ("Quota refused for {0}\n{1}", quota, se);
		}
		catch (Exception e) {
			Console.WriteLine ("Error checking quota for {0}\n{1}", quota, e);
		}
	}
}
