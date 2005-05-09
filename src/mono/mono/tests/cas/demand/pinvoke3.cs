using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

public class Program {

	// for Mono
	[DllImport ("libc", SetLastError=true)]
	public static extern uint getuid ();

	// for Microsoft
	[DllImport ("kernel32.dll", SetLastError=true)]
	public static extern uint GetTickCount ();

	static bool RunningOnWindows ()
	{
		int p = (int) Environment.OSVersion.Platform;
		bool win = ((p != 4) && (p != 128));
		Console.WriteLine ("Running on {0}...", Environment.OSVersion);
		return win;
	}

	static int Test ()
	{
		try {
			uint u = (RunningOnWindows () ? GetTickCount () : getuid ());
			Console.WriteLine ("*1* P/Invoke: {0}", u);
			return 1;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*0* Expected SecurityException\n{0}", se);
			return 0;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}

	[SecurityPermission (SecurityAction.Deny, UnmanagedCode=true)]
	static int Main ()
	{
		return Test ();
	}
}
