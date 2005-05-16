using System;
using System.Diagnostics;
using System.IO;
using System.Drawing.Printing;
using System.Net;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

class Program {

	static PermissionSet CreatePermissionSet (string name)
	{
		return new NamedPermissionSet (name, PermissionState.None);
	}

	static void Save (string filename, PermissionSet ps)
	{
		using (StreamWriter sw = new StreamWriter (filename)) {
			sw.WriteLine (ps.ToXml ().ToString ());
			sw.Close ();
		}
	}

	public static void FullTrust ()
	{
		PermissionSet ps = new NamedPermissionSet ("FullTrust", PermissionState.Unrestricted);
		Save ("fulltrust.xml", ps);
	}

	public static void LocalIntranet ()
	{
		PermissionSet ps = CreatePermissionSet ("LocalIntranet");

		ps.AddPermission (new EnvironmentPermission (EnvironmentPermissionAccess.Read, "USERNAME;USER"));

		ps.AddPermission (new FileDialogPermission (PermissionState.Unrestricted));

		IsolatedStorageFilePermission isfp = new IsolatedStorageFilePermission (PermissionState.None);
		isfp.UsageAllowed = IsolatedStorageContainment.AssemblyIsolationByUser;
		isfp.UserQuota = Int64.MaxValue;
		ps.AddPermission (isfp);

		ps.AddPermission (new ReflectionPermission (ReflectionPermissionFlag.ReflectionEmit));

		SecurityPermissionFlag spf = SecurityPermissionFlag.Execution | SecurityPermissionFlag.Assertion;
		ps.AddPermission (new SecurityPermission (spf));

		ps.AddPermission (new UIPermission (PermissionState.Unrestricted));

		ps.AddPermission (new DnsPermission (PermissionState.Unrestricted));

		ps.AddPermission (new PrintingPermission (PrintingPermissionLevel.DefaultPrinting));

		ps.AddPermission (new EventLogPermission (EventLogPermissionAccess.Instrument, "."));

		Save ("intranet.xml", ps);
	}

	public static void Internet ()
	{
		PermissionSet ps = CreatePermissionSet ("Internet");

		ps.AddPermission (new FileDialogPermission (FileDialogPermissionAccess.Open));

		IsolatedStorageFilePermission isfp = new IsolatedStorageFilePermission (PermissionState.None);
		isfp.UsageAllowed = IsolatedStorageContainment.DomainIsolationByUser;
		isfp.UserQuota = 10240;
		ps.AddPermission (isfp);

		ps.AddPermission (new SecurityPermission (SecurityPermissionFlag.Execution));

		ps.AddPermission (new UIPermission (UIPermissionWindow.SafeTopLevelWindows, UIPermissionClipboard.OwnClipboard));

		ps.AddPermission (new PrintingPermission (PrintingPermissionLevel.SafePrinting));

		Save ("internet.xml", ps);
	}

	public static void Execution ()
	{
		PermissionSet ps = CreatePermissionSet ("Execution");

		ps.AddPermission (new SecurityPermission (SecurityPermissionFlag.Execution));

		Save ("execution.xml", ps);
	}

	public static void Nothing ()
	{
		PermissionSet ps = CreatePermissionSet ("Nothing");
		Save ("nothing.xml", ps);
	}

	static int Main (string[] args)
	{
		Console.WriteLine ("NOTE: All files are for test purposes only!");
		Console.WriteLine ("Creating the FullTrust default permissions file...");
		FullTrust ();
		Console.WriteLine ("Creating the Local Intranet default permissions file...");
		LocalIntranet ();
		Console.WriteLine ("Creating the Internet default permissions file...");
		Internet ();
		Console.WriteLine ("Creating the Execution default permissions file...");
		Execution ();
		Console.WriteLine ("Creating the Nothing default permissions file...");
		Nothing ();
		Console.WriteLine ("Completed.");
		return 0;
	}
}
