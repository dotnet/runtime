//
// marshal.cs: tests for the System.Runtime.InteropServices.Marshal class
//

using System;
using System.Reflection;
using System.Runtime.InteropServices;

public class Tests {

	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public delegate int SimpleDelegate (int a);

	public static int delegate_test (int a)
	{
		return a + 1;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate")]
	public static extern int mono_test_marshal_delegate (IntPtr ptr);

	static int test_0_get_function_pointer_for_delegate () {
		// This is a 2.0 feature
		MethodInfo mi = typeof (Marshal).GetMethod ("GetFunctionPointerForDelegate");
		if (mi == null)
			return 0;

		IntPtr fnPtr = (IntPtr)mi.Invoke (null, new object [] { new SimpleDelegate (delegate_test)});

		if (mono_test_marshal_delegate (fnPtr) != 3)
			return 1;

		return 0;
	}
}
