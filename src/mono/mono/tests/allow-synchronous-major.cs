using System;
using System.Reflection;

public class Test
{
	public static int Main ()
	{
		var t = Type.GetType("Mono.Runtime");
		if (t == null)
			return 1;
		var setGCAllowSynchronousMajor = (Func<bool,bool>) Delegate.CreateDelegate(typeof(Func<bool,bool>),
											   t,
											   "SetGCAllowSynchronousMajor",
											   ignoreCase:false,
											   throwOnBindFailure:false);
		if (setGCAllowSynchronousMajor == null)
			return 1;

		if (!setGCAllowSynchronousMajor (false))
			Console.WriteLine ("could not disable synchronous major");
		if (!setGCAllowSynchronousMajor (true))
			return 1;

		return 0;
	}
}
