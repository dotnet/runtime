//
// marshal.cs: tests for the System.Runtime.InteropServices.Marshal class
//

using System;
using System.Reflection;
using System.Runtime.InteropServices;

public class Tests {

	[AttributeUsage (AttributeTargets.Method)]
	sealed class MonoPInvokeCallbackAttribute : Attribute {
		public MonoPInvokeCallbackAttribute (Type t) {}
	}

	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public delegate int SimpleDelegate (int a);

	[MonoPInvokeCallback (typeof (SimpleDelegate))]
	public static int delegate_test (int a)
	{
		return a + 1;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate")]
	public static extern int mono_test_marshal_delegate (IntPtr ptr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_delegate")]
	public static extern IntPtr mono_test_marshal_return_delegate (SimpleDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_delegate_2")]
	public static extern IntPtr mono_test_marshal_return_delegate_2 ();

	static int test_0_get_function_pointer_for_delegate () {
		IntPtr fnPtr = Marshal.GetFunctionPointerForDelegate (new SimpleDelegate (delegate_test));

		if (mono_test_marshal_delegate (fnPtr) != 3)
			return 1;

		return 0;
	}

	static int test_0_get_delegate_for_function_pointer () {
		IntPtr ptr = mono_test_marshal_return_delegate (new SimpleDelegate (delegate_test));

		SimpleDelegate d = (SimpleDelegate)Marshal.GetDelegateForFunctionPointer (ptr, typeof (SimpleDelegate));

		return d (5) == 6 ? 0 : 1;
	}

	/* Obtain a delegate from a native function pointer */
	static int test_0_get_delegate_for_ftnptr_native () {
		IntPtr ptr = mono_test_marshal_return_delegate_2 ();

		SimpleDelegate d = (SimpleDelegate)Marshal.GetDelegateForFunctionPointer (ptr, typeof (SimpleDelegate));

		return d (5) == 6 ? 0 : 1;
	}
}
