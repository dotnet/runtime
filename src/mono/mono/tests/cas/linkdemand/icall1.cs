using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System {

	// private internal call for Mono runtime
	public class Environment {

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static string internalGetGacPath ();
	}

	// private internal call for MS runtime
	// http://msdn.microsoft.com/msdnmag/issues/04/11/NETMatters/
	public class Guid {

		public Guid ()
		{
		}

		[MethodImpl (MethodImplOptions.InternalCall)]
		internal extern bool CompleteGuid ();
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
				result = g.CompleteGuid ().ToString ();
			}
			Console.WriteLine ("*1* Unexcepted internal call: {0}", result);
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
}
