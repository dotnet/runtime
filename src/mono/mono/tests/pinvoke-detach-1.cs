//
// pinvoke-detach-1.cs:
//
//   Test attaching and detaching a new thread from native.
//  If everything is working, this should not hang on shutdown.
using System;
using System.Threading;
using System.Runtime.InteropServices;

public class MonoPInvokeCallbackAttribute : Attribute {
	public MonoPInvokeCallbackAttribute (Type delegateType) { }
}

public class Tests {
	public static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}

	public delegate void VoidVoidDelegate ();

	static int was_called;

	[MonoPInvokeCallback (typeof (VoidVoidDelegate))]
	private static void MethodInvokedFromNative ()
	{
		was_called++;
	}

	[DllImport ("libtest", EntryPoint="mono_test_attach_invoke_foreign_thread")]
	public static extern bool mono_test_attach_invoke_foreign_thread (string assm_name, string name_space, string class_name, string method_name);

	public static int test_0_attach_invoke_foreign_thread ()
	{
		was_called = 0;
		bool skipped = mono_test_attach_invoke_foreign_thread (typeof (Tests).Assembly.Location, "", "Tests", "MethodInvokedFromNative");
		return skipped || was_called == 5 ? 0 : 1;
	}

	[MonoPInvokeCallback (typeof (VoidVoidDelegate))]
	private static void MethodInvokedFromNative2 ()
	{
	}

	[DllImport ("libtest", EntryPoint="mono_test_attach_invoke_block_foreign_thread")]
	public static extern bool mono_test_attach_invoke_block_foreign_thread (string assm_name, string name_space, string class_name, string method_name);

	public static int test_0_attach_invoke_block_foreign_thread ()
	{
		bool skipped = mono_test_attach_invoke_block_foreign_thread (typeof (Tests).Assembly.Location, "", "Tests", "MethodInvokedFromNative2");
		GC.Collect (); // should not hang waiting for the foreign thread
		return 0; // really we succeed if the app can shut down without hanging
	}


}
