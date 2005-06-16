using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System {

	// private internal call for Mono runtime
	public class Environment {

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static string internalGetGacPath ();
	}

	// private internal call for MS runtime
	// http://msdn.microsoft.com/msdnmag/issues/04/11/NETMatters/
	public class Guid {

		public Guid ()
		{
		}

		[MethodImpl (MethodImplOptions.InternalCall)]
#if NET_2_0
		public extern void CompleteGuid ();
#else
		public extern bool CompleteGuid ();
#endif
	}
}

public class Program {

	static bool RunningOnMono ()
	{
		bool mono = (Type.GetType ("Mono.Math.BigInteger") != null); 
		Console.WriteLine ("Running on {0} runtime...", mono ? "Mono" : "Microsoft");
		return mono;
	}

	static int Main ()
	{
		try {
			string result = null;
			if (RunningOnMono ()) {
				result = Environment.internalGetGacPath ();
			} else {
				System.Guid g = new System.Guid ();
#if NET_2_0
				g.CompleteGuid ();
				result = "completed";
#else
				result = g.CompleteGuid ().ToString ();
#endif
			}
#if NET_2_0
			Console.WriteLine ("*0* Expected internal call: {0}", result);
#else
			Console.WriteLine ("*0* Unexpected (1.x) but accepted (like 2.x) internal call: {0}", result);
#endif
			return 0;
		}
		catch (SecurityException se) {
#if NET_2_0
			Console.WriteLine ("*1* Unexpected SecurityException\n{0}", se);
			return 1;
#else
			Console.WriteLine ("*0* Expected (1.x) SecurityException\n{0}", se);
			return 0;
#endif
		}
		catch (Exception e) {
			Console.WriteLine ("*2* Unexpected exception\n{0}", e);
			return 2;
		}
	}
}
