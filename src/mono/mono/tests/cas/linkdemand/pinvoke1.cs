using System;
using System.Runtime.InteropServices;
using System.Security;

// this attribute has NO effect on LinkDemand!
[SuppressUnmanagedCodeSecurity]
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
		try {
			uint u = (RunningOnMono ()) ? getuid () : GetTickCount ();
			Console.WriteLine ("*0* P/Invoke: {0}", u);
			return 0;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*1* Unexpected SecurityException\n{0}", se);
			return 1;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}

	static int Main ()
	{
		return Test ();
	}
}
