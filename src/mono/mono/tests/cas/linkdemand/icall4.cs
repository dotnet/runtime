using System;
using System.Reflection;
using System.Security;

public class Program {

	// Math.Sin is a "public" internal call for both Mono and Microsoft
	private const string icall = "Sin";

	static int TestReflectedICall ()
	{
		MethodInfo mi = typeof (System.Math).GetMethod (icall);
		if (mi == null) {
			Console.WriteLine ("*3* Couldn't reflect on internalcall {0}", icall);
			return 3;
		}

		return (int) (double) mi.Invoke (null, new object [1] { 0.0 });
	}

	static int Main ()
	{
		try {
			int result = TestReflectedICall ();
			Console.WriteLine ("*{0}* [Reflected]System.Math.Sin(0) == {0}", result);
			return result;
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
