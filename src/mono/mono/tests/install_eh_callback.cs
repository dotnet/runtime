using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class MonoPInvokeCallbackAttribute : Attribute {
	public MonoPInvokeCallbackAttribute (Type delegateType) { }
}

public class Tests {

	[DllImport ("libtest")]
	public static extern void mono_test_setjmp_and_call (VoidVoidDelegate del, out IntPtr handle);
	
	public delegate void VoidVoidDelegate ();

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
		public static void M () {
			try {
				callee (ref called);
				throw new Exception ("unexpected return from callee");
			} catch (SomeOtherExn) {
			} finally {
				finally_called = true;
			}
		}
	}

	public static int test_0_setjmp_exn_handler ()
	{
		IntPtr res;
		Caller.Setup ();
		VoidVoidDelegate f = new VoidVoidDelegate (Caller.M);
			
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
		

	static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}
}
