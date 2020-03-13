using System;
using System.Runtime.InteropServices;

public class Tests {

	//
	// This is not permitted, should throw an exception
	//
	[DllImport ("libtest")]
	public static extern void mono_safe_handle_ref (ref HandleRef handle);

	[DllImport ("libtest", EntryPoint="mono_xr_as_handle")]
	public static extern HandleRef mono_xr_as_handle (HandleRef r);

	[DllImport ("libtest")]
	public static extern int mono_xr (HandleRef sh);

	//
	// Mono should throw exceptions on ref HandleRefs
	//
	public static int test_0_ref_handleref ()
	{
		object o = new object ();
		HandleRef s = new HandleRef (o, (IntPtr) 0xeadcafe);
		try {
			mono_safe_handle_ref (ref s);
		} catch (MarshalDirectiveException){
			return 0;
		}
		// failed
		return 1;
	}

	//
	// Mono should throw excentions on return HandleRefs
	//
	public static int test_0_handleref_return ()
	{
		object o = new object ();
		HandleRef s = new HandleRef (o, (IntPtr) 0xeadcafe);
		try {
			HandleRef ret = mono_xr_as_handle (s);
		} catch (MarshalDirectiveException){
			return 0;
		}
		// failed
		return 1;
	}
	
	public static int test_0_marshal_handleref_argument ()
	{
		object o = new object ();
		Console.WriteLine ("BEFORE");
		HandleRef s = new HandleRef (o, (IntPtr) 0xeadcafe);
		if (mono_xr (s) != (0xeadcafe + 1234))
			return 1;
		Console.WriteLine ("AFTER");
		return 0;
	}

        [StructLayout (LayoutKind.Sequential)]
	public struct StructTest {
		public int a;
		public HandleRef handle1;
		public HandleRef handle2;
		public int b;
	}

	[DllImport ("libtest")]
	public static extern int mono_safe_handle_struct_ref (ref StructTest test);

	[DllImport ("libtest")]
	public static extern int mono_safe_handle_struct (StructTest test);

	static StructTest x = new StructTest ();

	public static int test_0_marshal_safehandle_field ()
	{
		x.a = 1234;
		x.b = 8743;
		object o = new object ();
		x.handle1 = new HandleRef (o, (IntPtr) 0x7080feed);
		x.handle2 = new HandleRef (o, (IntPtr) 0x1234abcd);

		if (mono_safe_handle_struct (x) != 0xf00f)
			return 1;

		return 0;
	}

	public static int test_0_marshal_safehandle_field_ref ()
	{
		x.a = 1234;
		x.b = 8743;
		object o = new object ();
		x.handle1 = new HandleRef (o, (IntPtr) 0x7080feed);
		x.handle2 = new HandleRef (o, (IntPtr) 0x1234abcd);
		
		if (mono_safe_handle_struct_ref (ref x) != 0xf00d)
			return 1;

		return 0;
	}
	
	static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}
}
