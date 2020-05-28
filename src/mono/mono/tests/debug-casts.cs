using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Tests
{
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_simple () {
		object o = new object ();
		try {
			string s = (string)o;
			return 1;
		} catch (InvalidCastException ex) {
			if (!ex.Message.Contains ("System.Object") || !ex.Message.Contains ("System.String"))
				return 2;
		}
		return 0;
	}

	public static int test_0_complex_1 () {
		object o = new object ();
		try {
			IEnumerable<object> ie = (IEnumerable<object>)o;
			return 1;
		} catch (InvalidCastException ex) {
			if (!ex.Message.Contains ("System.Object") || !ex.Message.Contains ("System.Collections.Generic.IEnumerable`1[System.Object]"))
				return 2;
		}
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static object return_null () {
		return null;
	}

	public static int test_0_complex_1_null () {
		object o = return_null ();
		IEnumerable<object> ie = (IEnumerable<object>)o;
		return 0;
	}
}
