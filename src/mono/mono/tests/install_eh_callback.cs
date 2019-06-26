using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class MonoPInvokeCallbackAttribute : Attribute {
	public MonoPInvokeCallbackAttribute (Type delegateType) { }
}

public class Tests {

	[DllImport ("libtest")]
	public static extern void mono_test_setjmp_and_call (VoidVoidDelegate del, out IntPtr handle);

	[DllImport ("libtest")]
	public static extern void mono_test_setup_ftnptr_eh_callback (VoidVoidDelegate del, VoidHandleHandleOutDelegate inside_eh_callback);

	[DllImport ("libtest")]
	public static extern void mono_test_cleanup_ftptr_eh_callback ();
	
	public delegate void VoidVoidDelegate ();
	public delegate void VoidHandleHandleOutDelegate (uint handle, out int exception_handle);

	public class SpecialExn : Exception {
	}

	public class SomeOtherExn : Exception {
	}

	[MethodImpl (MethodImplOptions.NoInlining)]
	private static void callee (ref bool called) {
		called = true;
		throw new SpecialExn ();
	}

	public class Caller {
		public static bool called;
		public static bool finally_called;
		
		public static void Setup () {
			called = false;
			finally_called = false;
		}

		[MonoPInvokeCallback (typeof (VoidVoidDelegate))]
		public static void M1 () {
			try {
				callee (ref called);
				throw new Exception ("unexpected return from callee");
			} catch (SomeOtherExn) {
			} finally {
				finally_called = true;
			}
		}

		[MonoPInvokeCallback (typeof (VoidVoidDelegate))]
		public static void M2 () {
			try {
				callee (ref called);
				throw new Exception ("unexpected return from callee");
			} catch (SomeOtherExn) {
			}
		}
	}

	public static int test_0_setjmp_exn_handler ()
	{
		IntPtr res;
		Caller.Setup ();
		VoidVoidDelegate f = new VoidVoidDelegate (Caller.M1);
			
		try {
			mono_test_setjmp_and_call (f, out res);
		} catch (SpecialExn) {
			Console.Error.WriteLine ("should not have caught a SpecialExn");
			return 1;
		}
		if (!Caller.called) {
			Console.Error.WriteLine ("delegate not even called");
			return 2;
		}
		if (!Caller.finally_called) {
			Console.Error.WriteLine ("finally not reached");
			return 3;
		}
		if (res == IntPtr.Zero) {
			Console.Error.WriteLine ("res should be a GCHandle, was 0");
			return 4;
		}
		GCHandle h = GCHandle.FromIntPtr (res);
		object o = h.Target;
		h.Free ();
		if (o == null) {
			Console.Error.WriteLine ("GCHandle target was null");
			return 5;
		}
		else if (o is SpecialExn)
			return 0;
		else {
			Console.Error.WriteLine ("o was not a SpecialExn, it is {0}", o);
			return 6;
		}
	}

	public class Caller2 {
		public static bool rethrow_called;
		public static bool exception_caught;
		public static bool return_from_inner_managed_callback;

		public static void Setup () {
			rethrow_called = false;
			exception_caught = false;
			return_from_inner_managed_callback = false;
		}

		public static void RethrowException (uint original_exception) {
			var e = (Exception) GCHandle.FromIntPtr ((IntPtr) original_exception).Target;
			rethrow_called = true;
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture (e).Throw ();
		}

		[MonoPInvokeCallback (typeof (VoidHandleHandleOutDelegate))]
		public static void Del2 (uint original_exception, out int exception_handle) {
			exception_handle = 0;
			try {
				RethrowException (original_exception);
			} catch (Exception ex) {
				var handle = GCHandle.Alloc (ex, GCHandleType.Normal);
				exception_handle = GCHandle.ToIntPtr (handle).ToInt32 ();
				exception_caught = true;
			}
			return_from_inner_managed_callback = true;
		}
	}
		
	public static int test_0_throw_and_raise_exception ()
	{
		Caller.Setup ();
		Caller2.Setup ();
		VoidVoidDelegate f = new VoidVoidDelegate (Caller.M2);
		VoidHandleHandleOutDelegate del2 = new VoidHandleHandleOutDelegate (Caller2.Del2);
		bool outer_managed_callback = false;
		try {
			mono_test_setup_ftnptr_eh_callback (f, del2);
		} catch (Exception e) {
			outer_managed_callback = true;
		}

		if (!outer_managed_callback) {
			Console.Error.WriteLine ("outer managed callback did not throw exception");
			return 1;
		}
		if (!Caller2.rethrow_called) {
			Console.Error.WriteLine ("exception was not rethrown by eh callback");
			return 2;
		}
		if (!Caller2.exception_caught) {
			Console.Error.WriteLine ("rethrown exception was not caught");
			return 3;
		}
		if (!Caller2.return_from_inner_managed_callback) {
			Console.Error.WriteLine ("managed callback called from native eh callback did not return");
			return 4;
		}

		mono_test_cleanup_ftptr_eh_callback ();
		return 0;
	}

	static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}
}
