using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

[assembly: SecurityPermission (SecurityAction.RequestRefuse, UnmanagedCode=true)]

public class Program {

	// for Mono
	[DllImport ("libc", SetLastError=true)]
	public static extern uint getuid ();

	// for Microsoft
	[DllImport ("kernel32.dll", SetLastError=true)]
	public static extern uint GetTickCount ();

	static bool RunningOnMono ()
	{
		bool mono = (Type.GetType ("Mono.Math.BigInteger") != null); 
		Console.WriteLine ("Running on {0} runtime...", mono ? "Mono" : "Microsoft");
		return mono;
	}

	static int Test ()
	{
		uint u = (RunningOnMono ()) ? getuid () : GetTickCount ();
		Console.WriteLine ("*1* Unexpected P/Invoke success: {0}", u);
		return 1;
	}

	static int Main ()
	{
		try {
			return Test ();
		}
		catch (SecurityException se) {
			Console.WriteLine ("*0* SecurityException\n{0}", se);
			return 0;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}
}
