//
// pinvoke-detach-1.cs:
//
//   Test attaching and detaching a new thread from native.
//  If everything is working, this should not hang on shutdown.
using System;
using System.Threading;
using System.Runtime.InteropServices;

public class Tests {
	public static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}

	static bool was_called;

	private static void MethodInvokedFromNative ()
	{
		was_called = true;
	}

	[DllImport ("libtest", EntryPoint="mono_test_attach_invoke_foreign_thread")]
	public static extern bool mono_test_attach_invoke_foreign_thread (string assm_name, string name_space, string class_name, string method_name);

	public static int test_0_attach_invoke_foreign_thread ()
	{
		was_called = false;
		bool skipped = mono_test_attach_invoke_foreign_thread (typeof (Tests).Assembly.Location, "", "Tests", "MethodInvokedFromNative");
		return skipped || was_called ? 0 : 1;
	}

	static SemaphoreSlim sema;

	[DllImport ("libtest", EntryPoint="mono_test_attach_invoke_repeat_foreign_thread")]
	public static extern bool mono_test_attach_invoke_repeat_foreign_thread (string assm_name, string name_space, string class_name, string method_name);

	private static void MethodInvokedRepeatedlyFromNative ()
	{
		if (sema != null)
			sema.Release (1);
		sema = null;
	}

	public static int test_0_attach_invoke_repeat_foreign_thread ()
	{
		sema = new SemaphoreSlim (0, 1);
		bool skipped = mono_test_attach_invoke_repeat_foreign_thread (typeof (Tests).Assembly.Location, "", "Tests", "MethodInvokedRepeatedlyFromNative");
		if (!skipped)
			sema.Wait ();
		return 0; // really we succeed if the app can shut down without hanging
	}

	private static void MethodInvokedFromNative2 ()
	{
	}

	[DllImport ("libtest", EntryPoint="mono_test_attach_invoke_block_foreign_thread")]
	public static extern bool mono_test_attach_invoke_block_foreign_thread (string assm_name, string name_space, string class_name, string method_name);

	public static int test_0_attach_invoke_block_foreign_thread ()
	{
		bool skipped = mono_test_attach_invoke_block_foreign_thread (typeof (Tests).Assembly.Location, "", "Tests", "MethodInvokedFromNative2");
		return 0; // really we succeed if the app can shut down without hanging
	}


}
