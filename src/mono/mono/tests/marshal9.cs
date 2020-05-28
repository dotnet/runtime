//
// marshal9.cs: tests for custom marshalling
//

using System;
using System.Runtime.InteropServices;

[AttributeUsage (AttributeTargets.Method)]
sealed class MonoPInvokeCallbackAttribute : Attribute {
	public MonoPInvokeCallbackAttribute (Type t) {}
}

public class Marshal1 : ICustomMarshaler
{
	int param;

	public static int cleanup_managed_count = 0;

	public static int cleanup_native_count = 0;

	public static int native_to_managed_count = 0;

	public static object native_to_managed_result = null;

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
	// or when it would be called. In fact, Rotor never seems
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
		native_to_managed_count ++;
		native_to_managed_result = param + Marshal.ReadInt32 (new IntPtr (pNativeData.ToInt64 () + 4));
		return native_to_managed_result;
	}
}

public class Tests
{
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests));
	return 0;
	}

	[DllImport ("libtest")]
	[return : MarshalAs(UnmanagedType.CustomMarshaler,MarshalTypeRef = typeof
						(Marshal1), MarshalCookie = "5")]
	private static extern object mono_test_marshal_pass_return_custom (int i,  
																	[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5")] object number, int j);

	public static int test_0_pass_return () {

		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;
		Marshal1.native_to_managed_count = 0;

		int res = (int)mono_test_marshal_pass_return_custom (5, 10, 5);

		if (Marshal1.cleanup_managed_count != 0)
			return 1;
		if (Marshal1.cleanup_native_count != 2)
			return 2;
		if (Marshal1.native_to_managed_count != 1)
			return 3;

		return res == 15 ? 0 : 3;
	}

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_pass_out_custom (int i,  
															[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5"), Out] out object number, int j);

	public static int test_0_pass_out () {

		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;

		object o = 0;
		int res = mono_test_marshal_pass_out_custom (5, out o, 5);

		/* 10 + 5 + 5 + 5 */
		if ((int)o != 25)
			return 1;

		if (Marshal1.cleanup_managed_count != 0)
			return 2;
		if (Marshal1.cleanup_native_count != 1)
			return 3;

		return 0;
	}

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_pass_inout_custom (int i,
															[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5"), In, Out] object number, int j);

	public static int test_0_pass_inout () {
		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;
		Marshal1.native_to_managed_result = null;

		int res = mono_test_marshal_pass_inout_custom (5, 5, 5);

		// The changes made by the [Out] custom marshaller are not visible to the caller
		if ((int)Marshal1.native_to_managed_result != 20)
			return 1;

		if (Marshal1.cleanup_managed_count != 0)
			return 2;
		if (Marshal1.cleanup_native_count != 1)
			return 3;

		return 0;
	}

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_pass_out_byval_custom (int i,
															[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5"), Out] object number, int j);

	public static int test_0_pass_out_byval () {
		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;
		Marshal1.native_to_managed_result = null;

		// MS.NET passes NULL and does no marshalling in this case
		int res = mono_test_marshal_pass_out_byval_custom (5, 5, 5);

		if (res != 0)
			return 1;

		if (Marshal1.native_to_managed_result != null)
			return 2;

		if (Marshal1.cleanup_managed_count != 0)
			return 3;
		if (Marshal1.cleanup_native_count != 0)
			return 4;

		return 0;
	}

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_pass_byref_custom (int i,  
															[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5")] ref object number, int j);

	public static int test_0_pass_ref () {

		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;

		object o = 10;
		int res = mono_test_marshal_pass_byref_custom (5, ref o, 5);

		/* 10 + 5 + 5 + 5 */
		if ((int)o != 25)
			return 1;

		if (Marshal1.cleanup_managed_count != 0)
			return 2;
		if (Marshal1.cleanup_native_count != 1)
			return 3;

		return 0;
	}

	[DllImport ("libtest")]
	[return : MarshalAs(UnmanagedType.CustomMarshaler,MarshalTypeRef = typeof
						(Marshal1), MarshalCookie = "5")]
	private static extern object mono_test_marshal_pass_return_custom_null (int i,  
																	[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5")] object number, int j);

	public static int test_0_pass_return_null () {

		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;

		object o = mono_test_marshal_pass_return_custom_null (5, null, 5);

		if (Marshal1.cleanup_managed_count != 0)
			return 1;
		if (Marshal1.cleanup_native_count != 0)
			return 2;

		return (o == null) ? 0 : 1;
	}

	[return : MarshalAs(UnmanagedType.CustomMarshaler,MarshalTypeRef = typeof
(Marshal1), MarshalCookie = "5")] public delegate object pass_return_int_delegate ([MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal1), MarshalCookie = "5")] object o);

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_pass_return_custom_in_delegate (pass_return_int_delegate del);

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_pass_return_custom_null_in_delegate (pass_return_int_delegate del);

	[MonoPInvokeCallback (typeof (pass_return_int_delegate))]
	private static object pass_return_int (object i) {
		return (int)i;
	}

	[MonoPInvokeCallback (typeof (pass_return_int_delegate))]
	private static object pass_return_null (object i) {
		return (i == null) ? null : new Object ();
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

	public static int test_0_pass_return_null_delegate () {

		Marshal1.cleanup_managed_count = 0;
		Marshal1.cleanup_native_count = 0;

		int res = mono_test_marshal_pass_return_custom_null_in_delegate (new pass_return_int_delegate (pass_return_null));

		if (Marshal1.cleanup_managed_count != 0)
			return 1;
		if (Marshal1.cleanup_native_count != 0)
			return 2;

		return res == 15 ? 0 : 3;
	}

	/*
	 * Test custom marshaller class not implementing ICustomMarshaler
	 */

	public class Marshal2 {
	}

	[DllImport ("libtest")]
	private static extern IntPtr mono_test_marshal_pass_return_custom2 (int i,  
																	[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal3), MarshalCookie = "5")] object number, int j);

	public static int test_0_not_icustommarshaller () {
		try {
			mono_test_marshal_pass_return_custom2 (5, 10, 5);
		}
		catch (ApplicationException) {
			return 0;
		}
		return 1;
	}


	/*
	 * Test custom marshaller class missing GetInstance method
	 */

	public class Marshal3 : ICustomMarshaler {
		public void CleanUpManagedData (object managedObj)
		{
		}

		public void CleanUpNativeData (IntPtr pNativeData)
		{
		}

		public int GetNativeDataSize ()
		{
			return 4;
		}

		public IntPtr MarshalManagedToNative (object managedObj)
		{
			return IntPtr.Zero;
		}

		public object MarshalNativeToManaged (IntPtr pNativeData)
		{
			return null;
		}
	}

	public class Marshal4 : Marshal3 {
		public static object GetInstance (string s) {
			return null;
		}
	}

	public class Marshal5 : Marshal3 {
		public static ICustomMarshaler GetInstance (object s) {
			return null;
		}
	}

	[DllImport ("libtest", EntryPoint = "mono_test_marshal_pass_return_custom2")]
	private static extern IntPtr mono_test_marshal_pass_return_custom3 (int i,  
																	[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal3), MarshalCookie = "5")] object number, int j);

	[DllImport ("libtest", EntryPoint = "mono_test_marshal_pass_return_custom2")]
	private static extern IntPtr mono_test_marshal_pass_return_custom4 (int i,  
																	[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal4), MarshalCookie = "5")] object number, int j);

	[DllImport ("libtest", EntryPoint = "mono_test_marshal_pass_return_custom2")]
	private static extern IntPtr mono_test_marshal_pass_return_custom5 (int i,  
																	[MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal5), MarshalCookie = "5")] object number, int j);

	public static int test_0_missing_getinstance1 () {
		try {
			mono_test_marshal_pass_return_custom3 (5, 10, 5);
		}
		catch (ApplicationException) {
			return 0;
		}
		return 1;
	}

	public static int test_0_missing_getinstance2 () {
		try {
			mono_test_marshal_pass_return_custom4 (5, 10, 5);
		}
		catch (ApplicationException) {
			return 0;
		}
		return 1;
	}

	public static int test_0_missing_getinstance3 () {
		try {
			mono_test_marshal_pass_return_custom5 (5, 10, 5);
		}
		catch (ApplicationException) {
			return 0;
		}
		return 1;
	}

	public class Marshal6 : ICustomMarshaler {
		public static int managed_to_native_count = 0;
		public static int native_to_managed_count = 0;

		public static ICustomMarshaler GetInstance (string s) 
		{
			return new Marshal6 ();
		}

		public void CleanUpManagedData (object managedObj)
		{
		}

		public void CleanUpNativeData (IntPtr pNativeData)
		{
		}

		public int GetNativeDataSize ()
		{
			return 4;
		}

		public IntPtr MarshalManagedToNative (object managedObj)
		{
			managed_to_native_count++;
			return IntPtr.Zero;
		}

		public object MarshalNativeToManaged (IntPtr pNativeData)
		{
			native_to_managed_count++;
			return null;
		}
	}

	public delegate void custom_out_param_delegate ([MarshalAs (UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Marshal6), MarshalCookie = "5")] out object o);

	[DllImport ("libtest")]
	private static extern int mono_test_marshal_custom_out_param_delegate (custom_out_param_delegate del);

	// ICustomMarshaler.MarshalNativeToManaged should not be called when the 
	// parameter is marked as an out.
	public static int test_0_native_to_managed_custom_parameter () 
	{
		Marshal6.managed_to_native_count = 0;
		Marshal6.native_to_managed_count = 0;

		int res = mono_test_marshal_custom_out_param_delegate (new custom_out_param_delegate (custom_out_param));

		if (Marshal6.managed_to_native_count != 1)
			return 1;
		if (Marshal6.native_to_managed_count != 0)
			return 2;

		return 0;
	}

	[MonoPInvokeCallback (typeof (custom_out_param_delegate))]
	private static void custom_out_param (out object i) 
	{
		i = new object();	
	}

}
