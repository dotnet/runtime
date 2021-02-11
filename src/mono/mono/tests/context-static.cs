using System;
using System.Runtime.Remoting.Contexts;

[Synchronization (SynchronizationAttribute.REQUIRES_NEW)]
class CBO: ContextBoundObject
{
	public bool Test () {
		Console.WriteLine ("start value: {0}", T.var);
		if (T.var != 0) return true;
		T.var = 100;
		Console.WriteLine ("end value: {0}", T.var);
		return (T.var != 100);
	}
}

class T {
	[ContextStatic]
	public static int var = 5;

	static int Main () {
		bool failed = false;
		var = 10;
		
		CBO cbo = new CBO();
		failed = cbo.Test ();
		
		if (var != 10)
			failed = true;
			
		Console.WriteLine ("value in main context: {0}", var);

		if (failed)
			return 1;
		return 0;
	}
}
