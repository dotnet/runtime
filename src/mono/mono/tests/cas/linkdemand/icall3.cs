using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System {

	// this is a "public" internal call for both Mono and Microsoft
	// http://groups.google.ca/groups?q=MethodImplAttribute+InternalCall&hl=en&lr=&selm=udngxsETCHA.1468%40tkmsftngp11&rnum=10
	public class Math {

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public static extern double Sin (double a);
	}
}

public class Program {

	static int TestICall ()
	{
		return (int) System.Math.Sin (0);
	}

	static int Main ()
	{
		try {
			Console.WriteLine ("*0* System.Math.Sin(0) == {0}", TestICall ());
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
