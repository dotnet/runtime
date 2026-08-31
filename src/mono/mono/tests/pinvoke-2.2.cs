//
// pinvoke-2.cs:
//
//  Tests for net 2.0 pinvoke features
//

using System;
using System.Runtime.InteropServices;

public class Tests {

	public static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	[UnmanagedFunctionPointerAttribute (CallingConvention.Cdecl)]
	public delegate int CdeclDelegate (int i, int j);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_cdecl_delegate")]
	public static extern int mono_test_marshal_cdecl_delegate (CdeclDelegate d);	

	public static int cdecl_delegate (int i, int j) {
		return i + j;
	}

	public static int test_0_marshal_cdecl_delegate () {
		CdeclDelegate d = new CdeclDelegate (cdecl_delegate);

		return mono_test_marshal_cdecl_delegate (d);
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_fnptr")]
	public static extern IntPtr mono_test_marshal_return_fnptr ();

	delegate int AddDelegate (int i, int j);

	public static int test_4_get_delegate_for_function_pointer () {
		IntPtr ptr = mono_test_marshal_return_fnptr ();

		AddDelegate d = (AddDelegate)Marshal.GetDelegateForFunctionPointer (ptr, typeof (AddDelegate));

		return d (2, 2);
	}

	[DllImport ("libtest")]
	public static extern int mono_return_int (int i);

	[DllImport ("libtest", EntryPoint="not_found")]
	public static extern int not_found (int i);

	public delegate int SimpleDelegate (int i);

	[DllImport ("libtest")]
	public static extern int mono_invoke_simple_delegate (SimpleDelegate d);

	public static int test_4_native_function_to_ftnptr () {
		return mono_invoke_simple_delegate (mono_return_int);
	}

	public static int test_0_native_function_to_ftnptr_not_found () {
		try {
			return mono_invoke_simple_delegate (not_found);
		} catch (EntryPointNotFoundException) {
			return 0;
		}
	}		

}
