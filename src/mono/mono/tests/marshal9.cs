//
// marshal9.cs: tests for custom marshalling
//

using System;
using System.Runtime.InteropServices;

public class Marshal1 : ICustomMarshaler
{
	int param;

	public static int cleanup_managed_count = 0;

	public static int cleanup_native_count = 0;

	public Marshal1 (int param) {
		this.param = param;
	}

	public static ICustomMarshaler GetInstance (string s) {
		int param = Int32.Parse (s);
		return new Marshal1 (param);
	}

	public void CleanUpManagedData (object managedObj)
	{
		//Console.WriteLine ("CleanUpManagedData called");
		cleanup_managed_count ++;
	}

	public void CleanUpNativeData (IntPtr pNativeData)
	{
		//Console.WriteLine("CleanUpNativeData:" + pNativeData);
		/* Might be allocated in libtest.c using g_new0 so dont free it */
		int alloc_type = Marshal.ReadInt32 (pNativeData);
		if (alloc_type == 1)
			Marshal.FreeHGlobal (pNativeData);
		cleanup_native_count ++;
	}

	// I really do not understand the purpose of this method
	// or went it would be called. In fact, Rotor never seems
	// to call it.
	public int GetNativeDataSize ()
	{
		//Console.WriteLine("GetNativeDataSize() called");
		return 4;
	}

	public IntPtr MarshalManagedToNative (object managedObj)
	{
		int number;
		IntPtr ptr;

		number = Convert.ToInt32 (managedObj);
		ptr = Marshal.AllocHGlobal (8);
		Marshal.WriteInt32 (ptr, 1);  /* Allocated by AllocHGlobal */
		Marshal.WriteInt32(new IntPtr (ptr.ToInt64 () + 4), number);

		//Console.WriteLine ("ToNative: " + ptr);
		return ptr;
 	}

	public object MarshalNativeToManaged (IntPtr pNativeData)
	{
		//Console.WriteLine ("ToManaged: " + pNativeData);
		return param + Marshal.ReadInt32 (new IntPtr (pNativeData.ToInt64 () + 4));
	}
}

public class Tests
{
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests));
	}

	[DllImport ("libtest")]
	[return : MarshalAs(UnmanagedType.CustomMarshaler,MarshalTypeRef = typeof
						(Marshal1), MarshalCookie = "5")]
	private static extern object mono_test_marshal_pass_return_custom (int i,  
																	[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5")] object number, int j);

	public static int test_0_pass_return () {

		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;

		int res = (int)mono_test_marshal_pass_return_custom (5, 10, 5);

		if (Marshal1.cleanup_managed_count != 0)
			return 1;
		if (Marshal1.cleanup_native_count != 2)
			return 2;

		return res == 15 ? 0 : 3;
	}

	[return : MarshalAs(UnmanagedType.CustomMarshaler,MarshalTypeRef = typeof
(Marshal1), MarshalCookie = "5")] public delegate object pass_return_int_delegate ([MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5")] object o);

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_pass_return_custom_in_delegate (pass_return_int_delegate del);

	private static object pass_return_int (object i) {
		return (int)i;
	}

	public static int test_0_pass_return_delegate () {

		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;

		int res = mono_test_marshal_pass_return_custom_in_delegate (new pass_return_int_delegate (pass_return_int));

		if (Marshal1.cleanup_managed_count != 2)
			return 1;
		if (Marshal1.cleanup_native_count != 0)
			return 2;

		return res == 15 ? 0 : 3;
	}
}
