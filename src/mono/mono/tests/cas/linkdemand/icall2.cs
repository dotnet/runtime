using System;
using System.Reflection;
using System.Security;

public class Program {

	static bool RunningOnMono ()
	{
		bool mono = (Type.GetType ("Mono.Math.BigInteger") != null); 
		Console.WriteLine ("Running on {0} runtime...", mono ? "Mono" : "Microsoft");
		return mono;
	}

	static int Main ()
	{
		string icall = null;
		MethodInfo mi = null;
		bool mono = RunningOnMono ();

		if (mono) {
			icall = "internalGetGacPath";
			mi = typeof (System.Environment).GetMethod (icall, BindingFlags.Static | BindingFlags.NonPublic);
		} else {
			// private internal call for MS runtime
			// http://msdn.microsoft.com/msdnmag/issues/04/11/NETMatters/
			icall = "CompleteGuid";
			mi = typeof (System.Guid).GetMethod (icall, BindingFlags.Instance | BindingFlags.NonPublic);
		}

		if (mi == null) {
			Console.WriteLine ("*3* Couldn't reflect on internalcall {0}", icall);
				return 3;
		}

		try {
			string result = null;
			if (mono) {
				result = (string) mi.Invoke (null, null);
			} else {
				System.Guid g = new System.Guid ();
#if NET_2_0
				mi.Invoke (g, null);
				result = "completed";
#else
				result = ((bool) mi.Invoke (g, null)).ToString ();
#endif
			}
			Console.WriteLine ("*0* [Reflected]{0}: {1}", icall, result);
			return 0;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*1* SecurityException\n{0}", se);
			return 1;
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}
}
