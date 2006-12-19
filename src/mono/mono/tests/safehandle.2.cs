using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

public class Tests {

	public class MyHandle : SafeHandle {
		public MyHandle () : base (IntPtr.Zero, true)
		{
		}

		public MyHandle (IntPtr x) : base (x, true)
		{
		}
		
		
		public override bool IsInvalid {
			get {
				return false;
			}
		}

		protected override bool ReleaseHandle ()
		{
			return true;
		}
	}

	//
	// No default public constructor here, this is so we can test
	// that proper exceptions are thrown
	//
	public class MyHandleNoCtor : SafeHandle {
		public MyHandleNoCtor (IntPtr handle) : base (handle, true)
		{
		}
		
		public override bool IsInvalid {
			get {
				return false;
			}
		}

		protected override bool ReleaseHandle ()
		{
			return true;
		}
	}
	
	[DllImport ("libtest")]
	public static extern void mono_safe_handle_ref (ref MyHandle handle);

	[DllImport ("libtest", EntryPoint="mono_safe_handle_ref")]
	public static extern void mono_safe_handle_ref2 (ref MyHandleNoCtor handle);

	public static int test_0_safehandle_ref_noctor ()
	{
		MyHandleNoCtor m = new MyHandleNoCtor ((IntPtr) 0xdead);

		try {
			mono_safe_handle_ref2 (ref m);
		} catch (MissingMethodException e){
			Console.WriteLine ("Good: got exception requried");
			return 0;
		}

		return 1;
	}
	
	public static int test_0_safehandle_ref ()
	{
		MyHandle m = new MyHandle ((IntPtr) 0xdead);

		mono_safe_handle_ref (ref m);
		
		if (m.DangerousGetHandle () != (IntPtr) 0x800d){
			Console.WriteLine ("test_0_safehandle_ref: fail; Expected 0x800d, got: {0:x}", m.DangerousGetHandle ());
			return 1;
		}
		Console.WriteLine ("test_0_safehandle_ref: pass");
		return 0;
	}

	[DllImport ("libtest")]
	public static extern int mono_xr (SafeHandle sh);
	
	public static int test_0_marshal_safehandle_argument ()
	{
		SafeHandle s = new SafeFileHandle ((IntPtr) 0xeadcafe, true);
		if (mono_xr (s) != (0xeadcafe + 1234))
			return 1;
		return 0;
	}

	public static int test_0_marshal_safehandle_argument_null ()
	{
		try {
			mono_xr (null);
		} catch (ArgumentNullException){
			return 0;
		}
		return 1;
	}
	

        [StructLayout (LayoutKind.Sequential)]
	public struct StringOnStruct {
		public string a;
	}

        [StructLayout (LayoutKind.Sequential)]
	public struct StructTest {
		public int a;
		public SafeHandle handle1;
		public SafeHandle handle2;
		public int b;
	}

        [StructLayout (LayoutKind.Sequential)]
	public struct StructTest1 {
		public SafeHandle a;
	}
	
	[DllImport ("libtest")]
	public static extern int mono_safe_handle_struct_ref (ref StructTest test);

	[DllImport ("libtest")]
	public static extern int mono_safe_handle_struct (StructTest test);

	[DllImport ("libtest")]
	public static extern int mono_safe_handle_struct_simple (StructTest1 test);

	[DllImport ("libtest", EntryPoint="mono_safe_handle_return")]
	public static extern SafeHandle mono_safe_handle_return_1 ();

	[DllImport ("libtest", EntryPoint="mono_safe_handle_return")]
	public static extern MyHandle mono_safe_handle_return ();

	[DllImport ("libtest", EntryPoint="mono_safe_handle_return")]
	public static extern MyHandleNoCtor mono_safe_handle_return_2 ();
	
	static StructTest x = new StructTest ();

	public static int test_0_safehandle_return_noctor ()
	{
		try {
			MyHandleNoCtor m = mono_safe_handle_return_2 ();
		} catch (MissingMethodException e){
			Console.WriteLine ("GOOD: got exception required: " + e);

			return 0;
		}
		Console.WriteLine ("Failed, expected an exception because there is no paramterless ctor");
		return 1;
	}
	
	public static int test_0_safehandle_return_exc ()
	{
		try {
			SafeHandle x = mono_safe_handle_return_1 ();
		} catch (MarshalDirectiveException){
			Console.WriteLine ("GOOD: got exception required");
			return 0;
		}

		Console.WriteLine ("Error: should have generated an exception, since SafeHandle is abstract");
		return 1;
	}

	public static int test_0_safehandle_return ()
	{
		SafeHandle x = mono_safe_handle_return ();
		Console.WriteLine ("Got the following handle: {0}", x.DangerousGetHandle ());
		return x.DangerousGetHandle () == (IntPtr) 0x1000f00d ? 0 : 1;
	}
	
	public static int test_0_marshal_safehandle_field ()
	{
		x.a = 1234;
		x.b = 8743;
		x.handle1 = new SafeFileHandle ((IntPtr) 0x7080feed, false);
		x.handle2 = new SafeFileHandle ((IntPtr) 0x1234abcd, false);

		if (mono_safe_handle_struct (x) != 0xf00f)
			return 1;

		return 0;
	}

	public static int test_0_marshal_safehandle_field_ref ()
	{
		x.a = 1234;
		x.b = 8743;
		x.handle1 = new SafeFileHandle ((IntPtr) 0x7080feed, false);
		x.handle2 = new SafeFileHandle ((IntPtr) 0x1234abcd, false);
		
		if (mono_safe_handle_struct_ref (ref x) != 0xf00d)
			return 1;

		return 0;
	}
	
	public static int test_0_simple ()
	{
		StructTest1 s = new StructTest1 ();
		s.a = new SafeFileHandle ((IntPtr)1234, false);

		return mono_safe_handle_struct_simple (s) == 2468 ? 0 : 1;
	}

	public static int test_0_struct_empty ()
	{
		StructTest1 s = new StructTest1 ();

		try {
			mono_safe_handle_struct_simple (s);
		} catch (ArgumentNullException){
			return 0;
		}
		return 1;
	}
	
	public static int test_0_sf_dispose ()
	{
		SafeFileHandle sf = new SafeFileHandle ((IntPtr) 0x0d00d, false);
		sf.Dispose ();
		try {
			mono_xr (sf);
		} catch (ObjectDisposedException){
			return 0;
		}
		return 1;
	}


	static int Error (string msg)
	{
		Console.WriteLine ("Error: " + msg);
		return 1;
	}
	
	static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}
}

