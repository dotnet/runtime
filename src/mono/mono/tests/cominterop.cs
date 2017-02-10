//
// cominterop.cs:
//
//  Tests for COM Interop related features
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


public class Tests
{

	[DllImport("libtest")]
	public static extern int mono_test_marshal_bstr_in([MarshalAs(UnmanagedType.BStr)]string str);

	[DllImport("libtest")]
    public static extern int mono_test_marshal_bstr_out([MarshalAs(UnmanagedType.BStr)] out string str);

    [DllImport("libtest")]
    public static extern int mono_test_marshal_bstr_in_null([MarshalAs(UnmanagedType.BStr)]string str);

    [DllImport("libtest")]
    public static extern int mono_test_marshal_bstr_out_null([MarshalAs(UnmanagedType.BStr)] out string str);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_sbyte([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_byte([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_short([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_ushort([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_int([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_uint([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_long([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_ulong([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_float([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_double([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_in_bstr ([MarshalAs (UnmanagedType.Struct)]object obj);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_bool_true ([MarshalAs (UnmanagedType.Struct)]object obj);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_bool_false ([MarshalAs (UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_sbyte([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_sbyte_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_byte([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_byte_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_short([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_short_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_ushort([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_ushort_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_int([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_int_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_uint([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_uint_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_long([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_long_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_ulong([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_ulong_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_float([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_float_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_double([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_double_byref([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_bstr ([MarshalAs (UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_bstr_byref ([MarshalAs (UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_true ([MarshalAs (UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_true_byref ([MarshalAs (UnmanagedType.Struct)]out object obj);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_false ([MarshalAs (UnmanagedType.Struct)]out object obj);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_false_byref ([MarshalAs (UnmanagedType.Struct)]out object obj);


	public delegate int VarFunc (VarEnum vt, [MarshalAs (UnmanagedType.Struct)] object obj);

	public delegate int VarRefFunc (VarEnum vt, [MarshalAs (UnmanagedType.Struct)] ref object obj);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_sbyte_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_byte_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_short_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_ushort_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_int_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_uint_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_long_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_ulong_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_float_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_double_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_bstr_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_bool_true_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_in_bool_false_unmanaged (VarFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_sbyte_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_byte_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_short_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_ushort_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_int_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_uint_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_long_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_ulong_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_float_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_double_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_bstr_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_true_unmanaged (VarRefFunc func);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_false_unmanaged (VarRefFunc func);

    [DllImport ("libtest")]
	public static extern int mono_test_marshal_com_object_create (out IntPtr pUnk);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_com_object_same (out IntPtr pUnk);

    [DllImport ("libtest")]
    public static extern int mono_test_marshal_com_object_destroy (IntPtr pUnk);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_com_object_ref_count (IntPtr pUnk);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_ccw_identity ([MarshalAs (UnmanagedType.Interface)]ITest itest);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_ccw_reflexive ([MarshalAs (UnmanagedType.Interface)]ITest itest);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_ccw_transitive ([MarshalAs (UnmanagedType.Interface)]ITest itest);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_ccw_itest ([MarshalAs (UnmanagedType.Interface)]ITest itest);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_ccw_itest ([MarshalAs (UnmanagedType.Interface)]ITestPresSig itest);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_array_ccw_itest (int count, [MarshalAs (UnmanagedType.LPArray, SizeParamIndex=0)] ITest[] ppUnk);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_out_1dim_vt_bstr_empty ([MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]out Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_out_1dim_vt_bstr ([MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]out Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_out_2dim_vt_i4 ([MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]out Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_out_4dim_vt_i4 ([MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)]out Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_byval_1dim_empty ([In, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_byval_1dim_vt_i4 ([In, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_byval_1dim_vt_mixed ([In, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_byval_2dim_vt_i4 ([In, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_byval_3dim_vt_bstr ([In, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_byref_3dim_vt_bstr ([In, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_out_byref_1dim_empty ([In, Out, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_out_byref_3dim_vt_bstr ([In, Out, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_out_byref_1dim_vt_i4 ([In, Out, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_out_byval_1dim_vt_i4 ([In, Out, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_in_out_byval_3dim_vt_bstr ([In, Out, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_safearray_mixed (
		[In, Out, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array1,
		[MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] out Array array2,
		[In, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] Array array3,
		[In, Out, MarshalAs (UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref Array array4);

	[DllImport("libtest")]
	public static extern bool mono_cominterop_is_supported ();

	public static int Main ()
	{

		bool isWindows = !(((int)Environment.OSVersion.Platform == 4) ||
			((int)Environment.OSVersion.Platform == 128));

		if (mono_cominterop_is_supported () || isWindows)
		{
			#region BSTR Tests

			string str;
			if (mono_test_marshal_bstr_in ("mono_test_marshal_bstr_in") != 0)
				return 1;
			if (mono_test_marshal_bstr_out (out str) != 0 || str != "mono_test_marshal_bstr_out")
				return 2;
			if (mono_test_marshal_bstr_in_null (null) != 0)
				return 1;
			if (mono_test_marshal_bstr_out_null (out str) != 0 || str != null)
				return 2;

			#endregion // BSTR Tests

			#region VARIANT Tests

			object obj;
			if (mono_test_marshal_variant_in_sbyte ((sbyte)100) != 0)
				return 13;
			if (mono_test_marshal_variant_in_byte ((byte)100) != 0)
				return 14;
			if (mono_test_marshal_variant_in_short ((short)314) != 0)
				return 15;
			if (mono_test_marshal_variant_in_ushort ((ushort)314) != 0)
				return 16;
			if (mono_test_marshal_variant_in_int ((int)314) != 0)
				return 17;
			if (mono_test_marshal_variant_in_uint ((uint)314) != 0)
				return 18;
			if (mono_test_marshal_variant_in_long ((long)314) != 0)
				return 19;
			if (mono_test_marshal_variant_in_ulong ((ulong)314) != 0)
				return 20;
			if (mono_test_marshal_variant_in_float ((float)3.14) != 0)
				return 21;
			if (mono_test_marshal_variant_in_double ((double)3.14) != 0)
				return 22;
			if (mono_test_marshal_variant_in_bstr ("PI") != 0)
				return 23;
			if (mono_test_marshal_variant_out_sbyte (out obj) != 0 || (sbyte)obj != 100)
				return 24;
			if (mono_test_marshal_variant_out_byte (out obj) != 0 || (byte)obj != 100)
				return 25;
			if (mono_test_marshal_variant_out_short (out obj) != 0 || (short)obj != 314)
				return 26;
			if (mono_test_marshal_variant_out_ushort (out obj) != 0 || (ushort)obj != 314)
				return 27;
			if (mono_test_marshal_variant_out_int (out obj) != 0 || (int)obj != 314)
				return 28;
			if (mono_test_marshal_variant_out_uint (out obj) != 0 || (uint)obj != 314)
				return 29;
			if (mono_test_marshal_variant_out_long (out obj) != 0 || (long)obj != 314)
				return 30;
			if (mono_test_marshal_variant_out_ulong (out obj) != 0 || (ulong)obj != 314)
				return 31;
			if (mono_test_marshal_variant_out_float (out obj) != 0 || ((float)obj - 3.14) / 3.14 > .001)
				return 32;
			if (mono_test_marshal_variant_out_double (out obj) != 0 || ((double)obj - 3.14) / 3.14 > .001)
				return 33;
			if (mono_test_marshal_variant_out_bstr (out obj) != 0 || (string)obj != "PI")
				return 34;

			VarFunc func = new VarFunc (mono_test_marshal_variant_in_callback);
			if (mono_test_marshal_variant_in_sbyte_unmanaged (func) != 0)
				return 35;
			if (mono_test_marshal_variant_in_byte_unmanaged (func) != 0)
				return 36;
			if (mono_test_marshal_variant_in_short_unmanaged (func) != 0)
				return 37;
			if (mono_test_marshal_variant_in_ushort_unmanaged (func) != 0)
				return 38;
			if (mono_test_marshal_variant_in_int_unmanaged (func) != 0)
				return 39;
			if (mono_test_marshal_variant_in_uint_unmanaged (func) != 0)
				return 40;
			if (mono_test_marshal_variant_in_long_unmanaged (func) != 0)
				return 41;
			if (mono_test_marshal_variant_in_ulong_unmanaged (func) != 0)
				return 42;
			if (mono_test_marshal_variant_in_float_unmanaged (func) != 0)
				return 43;
			if (mono_test_marshal_variant_in_double_unmanaged (func) != 0)
				return 44;
			if (mono_test_marshal_variant_in_bstr_unmanaged (func) != 0)
				return 45;
			if (mono_test_marshal_variant_in_bool_true_unmanaged (func) != 0)
				return 46;

			VarRefFunc reffunc = new VarRefFunc (mono_test_marshal_variant_out_callback);
			if (mono_test_marshal_variant_out_sbyte_unmanaged (reffunc) != 0)
				return 50;
			if (mono_test_marshal_variant_out_byte_unmanaged (reffunc) != 0)
				return 51;
			if (mono_test_marshal_variant_out_short_unmanaged (reffunc) != 0)
				return 52;
			if (mono_test_marshal_variant_out_ushort_unmanaged (reffunc) != 0)
				return 53;
			if (mono_test_marshal_variant_out_int_unmanaged (reffunc) != 0)
				return 54;
			if (mono_test_marshal_variant_out_uint_unmanaged (reffunc) != 0)
				return 55;
			if (mono_test_marshal_variant_out_long_unmanaged (reffunc) != 0)
				return 56;
			if (mono_test_marshal_variant_out_ulong_unmanaged (reffunc) != 0)
				return 57;
			if (mono_test_marshal_variant_out_float_unmanaged (reffunc) != 0)
				return 58;
			if (mono_test_marshal_variant_out_double_unmanaged (reffunc) != 0)
				return 59;
			if (mono_test_marshal_variant_out_bstr_unmanaged (reffunc) != 0)
				return 60;
			if (mono_test_marshal_variant_out_bool_true_unmanaged (reffunc) != 0)
				return 61;

			if (mono_test_marshal_variant_out_sbyte_byref (out obj) != 0 || (sbyte)obj != 100)
				return 97;
			if (mono_test_marshal_variant_out_byte_byref (out obj) != 0 || (byte)obj != 100)
				return 98;
			if (mono_test_marshal_variant_out_short_byref (out obj) != 0 || (short)obj != 314)
				return 99;
			if (mono_test_marshal_variant_out_ushort_byref (out obj) != 0 || (ushort)obj != 314)
				return 100;
			if (mono_test_marshal_variant_out_int_byref (out obj) != 0 || (int)obj != 314)
				return 101;
			if (mono_test_marshal_variant_out_uint_byref (out obj) != 0 || (uint)obj != 314)
				return 102;
			if (mono_test_marshal_variant_out_long_byref (out obj) != 0 || (long)obj != 314)
				return 103;
			if (mono_test_marshal_variant_out_ulong_byref (out obj) != 0 || (ulong)obj != 314)
				return 104;
			if (mono_test_marshal_variant_out_float_byref (out obj) != 0 || ((float)obj - 3.14) / 3.14 > .001)
				return 105;
			if (mono_test_marshal_variant_out_double_byref (out obj) != 0 || ((double)obj - 3.14) / 3.14 > .001)
				return 106;
			if (mono_test_marshal_variant_out_bstr_byref (out obj) != 0 || (string)obj != "PI")
				return 107;

			#endregion // VARIANT Tests

			#region Runtime Callable Wrapper Tests

#if !MOBILE

			IntPtr pUnk;
			if (mono_test_marshal_com_object_create (out pUnk) != 0)
				return 145;

			if (mono_test_marshal_com_object_ref_count (pUnk) != 1)
				return 146;

			if (Marshal.AddRef (pUnk) != 2)
				return 147;

			if (mono_test_marshal_com_object_ref_count (pUnk) != 2)
				return 148;

			if (Marshal.Release (pUnk) != 1)
				return 149;

			if (mono_test_marshal_com_object_ref_count (pUnk) != 1)
				return 150;

			object com_obj = Marshal.GetObjectForIUnknown (pUnk);

			if (com_obj == null)
				return 151;

			ITest itest = com_obj as ITest;

			if (itest == null)
				return 152;

			IntPtr pUnk2;
			if (mono_test_marshal_com_object_same (out pUnk2) != 0)
				return 153;

			object com_obj2 = Marshal.GetObjectForIUnknown (pUnk2);
			
			if (com_obj != com_obj2)
				return 154;

			if (!com_obj.Equals (com_obj2))
				return 155;

			IntPtr pUnk3;
			if (mono_test_marshal_com_object_create (out pUnk3) != 0)
				return 156;

			object com_obj3 = Marshal.GetObjectForIUnknown (pUnk3);
			if (com_obj == com_obj3)
				return 157;

			if (com_obj.Equals (com_obj3))
				return 158;

			// com_obj & com_obj2 share a RCW
			if (Marshal.ReleaseComObject (com_obj2) != 1)
				return 159;

			// com_obj3 should only have one RCW
			if (Marshal.ReleaseComObject (com_obj3) != 0)
				return 160;

			IntPtr iunknown = Marshal.GetIUnknownForObject (com_obj);
			if (iunknown == IntPtr.Zero)
				return 170;

			if (pUnk != iunknown)
				return 171;

			if (TestITest (itest) != 0)
				return 172;

			if (TestITestPresSig (itest as ITestPresSig) != 0)
				return 173;

			if (TestITestDelegate (itest) != 0)
				return 174;

			if (TestIfaceNoIcall (itest as ITestPresSig) != 0)
				return 201;

			itest = new TestClass ();

			if (TestITest (itest) != 0)
				return 175;

			itest = (ITest)System.Activator.CreateInstance (typeof(TestActivatorClass));

			if (TestITest (itest) != 0)
				return 176;


#endif

			#endregion // Runtime Callable Wrapper Tests

			#region COM Callable Wrapper Tests

			ManagedTest test = new ManagedTest ();

			mono_test_marshal_ccw_itest (test);

			if (test.Status != 0)
				return 200;

			ManagedTestPresSig test_pres_sig = new ManagedTestPresSig ();

			mono_test_marshal_ccw_itest (test_pres_sig);

			// test for Xamarin-47560
			var tests = new[] { test.Test };
			if (mono_test_marshal_array_ccw_itest (1, tests) != 0)
				return 201;

			#endregion // COM Callable Wrapper Tests

			#region SAFEARRAY tests
			
			if (isWindows) {

				/* out */

				Array array;
				if ((mono_test_marshal_safearray_out_1dim_vt_bstr_empty (out array) != 0) || (array.Rank != 1) || (array.Length != 0))
					return 62;

				if ((mono_test_marshal_safearray_out_1dim_vt_bstr (out array) != 0) || (array.Rank != 1) || (array.Length != 10))
					return 63;
				for (int i = 0; i < 10; ++i) {
					if (i != Convert.ToInt32 (array.GetValue (i)))
						return 64;
				}

				if ((mono_test_marshal_safearray_out_2dim_vt_i4 (out array) != 0) || (array.Rank != 2))
					return 65;
				if (   (array.GetLowerBound (0) != 0) || (array.GetUpperBound (0) != 3)
					|| (array.GetLowerBound (1) != 0) || (array.GetUpperBound (1) != 2))
					return 66;
				for (int i = array.GetLowerBound (0); i <= array.GetUpperBound (0); ++i)
				{
					for (int j = array.GetLowerBound (1); j <= array.GetUpperBound (1); ++j) {
						if ((i + 1) * 10 + (j + 1) != (int)array.GetValue (new long[] { i, j }))
							return 67;
					}
				}

				if ((mono_test_marshal_safearray_out_4dim_vt_i4 (out array) != 0) || (array.Rank != 4))
					return 68;
				if (   (array.GetLowerBound (0) != 15) || (array.GetUpperBound (0) != 24)
					|| (array.GetLowerBound (1) != 20) || (array.GetUpperBound (1) != 22)
					|| (array.GetLowerBound (2) !=  5) || (array.GetUpperBound (2) != 10)
					|| (array.GetLowerBound (3) != 12) || (array.GetUpperBound (3) != 18) )
					return 69;

				int index = 0;
				for (int i = array.GetLowerBound (3); i <= array.GetUpperBound (3); ++i) {
					for (int j = array.GetLowerBound (2); j <= array.GetUpperBound (2); ++j) {
						for (int k = array.GetLowerBound (1); k <= array.GetUpperBound (1); ++k) {
							for (int l = array.GetLowerBound (0); l <= array.GetUpperBound (0); ++l) {
								if (index != (int)array.GetValue (new long[] { l, k, j, i }))
									return 70;
								++index;
							}
						}
					}
				}

				/* in */

				array = new object[] { };
				if (mono_test_marshal_safearray_in_byval_1dim_empty (array) != 0)
					return 71;

				array = new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
				if (mono_test_marshal_safearray_in_byval_1dim_vt_i4 (array) != 0)
					return 72;

				array = new object[] { 0, "1", 2, "3", 4, "5", 6, "7", 8, "9", 10, "11", 12 };
				if (mono_test_marshal_safearray_in_byval_1dim_vt_mixed (array) != 0)
					return 73;
				if ((int)array.GetValue (0) != 0)
					return 74;

				array = new object[,] { { 11, 12, 13, 14 }, { 21, 22, 23, 24 } };
				if (mono_test_marshal_safearray_in_byval_2dim_vt_i4 (array) != 0)
					return 75;
				if ((int)array.GetValue (new int[] { 0, 0 }) != 11)
					return 76;

				array = new object[,,] { { { "111", "112", "113" }, { "121", "122", "123" } }, { { "211", "212", "213" }, { "221", "222", "223" } } };
				if (mono_test_marshal_safearray_in_byval_3dim_vt_bstr (array) != 0)
					return 77;
				if ((string)array.GetValue (new int[] { 0, 0, 0 }) != "111")
					return 78;

				array = new object[,,] { { { "111", "112", "113" }, { "121", "122", "123" } }, { { "211", "212", "213" }, { "221", "222", "223" } } };
				if ((mono_test_marshal_safearray_in_byref_3dim_vt_bstr (ref array) != 0) || (array.Rank != 3) || (array.Length != 12))
					return 79;
				if ((string)array.GetValue (new int[] { 0, 0, 0 }) != "111")
					return 80;

				/* in, out, byref */

				array = new object[] { };
				if ((mono_test_marshal_safearray_in_out_byref_1dim_empty (ref array) != 0) || (array.Rank != 1) || (array.Length != 8))
					return 81;
				for (int i = 0; i < 8; ++i)
				{
					if (i != Convert.ToInt32 (array.GetValue (i)))
						return 82;
				}

				array = new object[,,] { { { "111", "112", "113" }, { "121", "122", "123" } }, { { "211", "212", "213" }, { "221", "222", "223" } } };
				if ((mono_test_marshal_safearray_in_out_byref_3dim_vt_bstr (ref array) != 0) || (array.Rank != 1) || (array.Length != 8))
					return 83;
				for (int i = 0; i < 8; ++i)
				{
					if (i != Convert.ToInt32 (array.GetValue (i)))
						return 84;
				}

				array = new object[] { 1 };
				if ((mono_test_marshal_safearray_in_out_byref_1dim_vt_i4 (ref array) != 0) || (array.Rank != 1) || (array.Length != 1))
				{
				    return 85;
				}
				if (Convert.ToInt32 (array.GetValue (0)) != -1)
				    return 86;

				/* in, out, byval */

				array = new object[] { 1 };
				if ((mono_test_marshal_safearray_in_out_byval_1dim_vt_i4 (array) != 0) || (array.Rank != 1) || (array.Length != 1))
				{
					return 87;
				}
				if (Convert.ToInt32 (array.GetValue (0)) != 12345)
					return 88;

				array = new object[,,] { { { "111", "112", "113" }, { "121", "122", "123" } }, { { "211", "212", "213" }, { "221", "222", "223" } } };
				if ((mono_test_marshal_safearray_in_out_byval_3dim_vt_bstr (array) != 0) || (array.Rank != 3) || (array.Length != 12))
				{
				    return 89;
				}
				if (Convert.ToInt32 (array.GetValue (new int[] { 1, 1, 1 })) != 111)
					return 90;
				if (Convert.ToInt32 (array.GetValue (new int[] { 1, 1, 2 })) != 333)
					return 91;
				if (Convert.ToString(array.GetValue (new int[] { 0, 1, 0 })) != "ABCDEFG")
					return 92;

				/* Multiple safearray parameters with various types and options */

				Array array1 = new object[] { 1 };
				Array array2 = new object[,] { { 11, 12, 13, 14 }, { 21, 22, 23, 24 } };
				Array array3 = new object[] { 0, "1", 2, "3", 4, "5", 6, "7", 8, "9", 10, "11", 12 };
				Array array4 = new object[,,] { { { "111", "112", "113" }, { "121", "122", "123" } }, { { "211", "212", "213" }, { "221", "222", "223" } } };
				if (    (mono_test_marshal_safearray_mixed (array1, out array2, array3, ref array4) != 0)
					 || (array1.Rank != 1) || (array1.Length != 1) || (Convert.ToInt32 (array1.GetValue (0)) != 12345)
					 || (array2.Rank != 1) || (array2.Length != 10)
					 || (array4.Rank != 1) || (array4.Length != 8)
					)
				{
					return 93;
				}
				for (int i = 0; i < 10; ++i)
				{
					if (i != Convert.ToInt32 (array2.GetValue (i)))
						return 94;
				}
				if ((int)array3.GetValue (0) != 0)
					return 95;
				for (int i = 0; i < 8; ++i)
				{
					if (i != Convert.ToInt32 (array4.GetValue (i)))
						return 96;
				}
			}
			#endregion // SafeArray Tests

			#region COM Visible Test
			TestVisible test_vis = new TestVisible();
			IntPtr pDisp = Marshal.GetIDispatchForObject(test_vis);
			if (pDisp == IntPtr.Zero)
				return 200;
			#endregion 
		}

        return 0;
	}


	[ComImport ()]
	[Guid ("00000000-0000-0000-0000-000000000001")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	public interface ITest
	{
		// properties need to go first since mcs puts them there
		ITest Test
		{
			[return: MarshalAs (UnmanagedType.Interface)]
			[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId (5242884)]
			get;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SByteIn (sbyte val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ByteIn (byte val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ShortIn (short val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void UShortIn (ushort val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void IntIn (int val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void UIntIn (uint val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void LongIn (long val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ULongIn (ulong val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void FloatIn (float val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void DoubleIn (double val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ITestIn ([MarshalAs (UnmanagedType.Interface)]ITest val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ITestOut ([MarshalAs (UnmanagedType.Interface)]out ITest val);
		int Return22NoICall();
	}

	[ComImport ()]
	[Guid ("00000000-0000-0000-0000-000000000001")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	public interface ITestPresSig
	{
		// properties need to go first since mcs puts them there
		ITestPresSig Test
		{
			[return: MarshalAs (UnmanagedType.Interface)]
			[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId (5242884)]
			get;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int SByteIn (sbyte val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int ByteIn (byte val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int ShortIn (short val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int UShortIn (ushort val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int IntIn (int val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int UIntIn (uint val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int LongIn (long val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int ULongIn (ulong val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int FloatIn (float val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int DoubleIn (double val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int ITestIn ([MarshalAs (UnmanagedType.Interface)]ITestPresSig val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig ()]
		int ITestOut ([MarshalAs (UnmanagedType.Interface)]out ITestPresSig val);
		[PreserveSig ()]
		int Return22NoICall();
	}

	[System.Runtime.InteropServices.GuidAttribute ("00000000-0000-0000-0000-000000000002")]
	[System.Runtime.InteropServices.ComImportAttribute ()]
	[System.Runtime.InteropServices.ClassInterfaceAttribute (ClassInterfaceType.None)]
	public class _TestClass : ITest
	{
		// properties need to go first since mcs puts them there
		public virtual extern ITest Test
		{
			[return: MarshalAs (UnmanagedType.Interface)]
			[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId (5242884)]
			get;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void SByteIn (sbyte val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ByteIn (byte val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ShortIn (short val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void UShortIn (ushort val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void IntIn (int val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void UIntIn (uint val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void LongIn (long val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ULongIn (ulong val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void FloatIn (float val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void DoubleIn (double val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ITestIn ([MarshalAs (UnmanagedType.Interface)]ITest val);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ITestOut ([MarshalAs (UnmanagedType.Interface)]out ITest val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern int Return22NoICall();
	}

	[System.Runtime.InteropServices.GuidAttribute ("00000000-0000-0000-0000-000000000002")]
	public class TestClass : _TestClass
	{
		static TestClass ()
		{
			ExtensibleClassFactory.RegisterObjectCreationCallback (new ObjectCreationDelegate (CreateObject)); ;
		}
		private static System.IntPtr CreateObject (System.IntPtr aggr)
		{
			IntPtr pUnk3;
			mono_test_marshal_com_object_create (out pUnk3);
			return pUnk3;
		}
	}

	[System.Runtime.InteropServices.GuidAttribute ("00000000-0000-0000-0000-000000000003")]
	public class TestActivatorClass : _TestClass
	{
		static TestActivatorClass ()
		{
			ExtensibleClassFactory.RegisterObjectCreationCallback (new ObjectCreationDelegate (CreateObject)); ;
		}
		private static System.IntPtr CreateObject (System.IntPtr aggr)
		{
			IntPtr pUnk3;
			mono_test_marshal_com_object_create (out pUnk3);
			return pUnk3;
		}
	}

	delegate void SByteInDelegate (sbyte val);
	delegate void ByteInDelegate (byte val);
	delegate void ShortInDelegate (short val);
	delegate void UShortInDelegate (ushort val);
	delegate void IntInDelegate (int val);
	delegate void UIntInDelegate (uint val);
	delegate void LongInDelegate (long val);
	delegate void ULongInDelegate (ulong val);
	delegate void FloatInDelegate (float val);
	delegate void DoubleInDelegate (double val);
	delegate void ITestInDelegate (ITest val);
	delegate void ITestOutDelegate (out ITest val);

	public class ManagedTestPresSig : ITestPresSig
	{		// properties need to go first since mcs puts them there
		public ITestPresSig Test
		{
			get
			{
				return new ManagedTestPresSig ();
			}
		}

		public int SByteIn (sbyte val)
		{
			if (val != -100)
				return 1;
			return 0;
		}

		public int ByteIn (byte val)
		{
			if (val != 100)
				return 2;
			return 0;
		}

		public int ShortIn (short val)
		{
			if (val != -100)
				return 3;
			return 0;
		}

		public int UShortIn (ushort val)
		{
			if (val != 100)
				return 4;
			return 0;
		}

		public int IntIn (int val)
		{
			if (val != -100)
				return 5;
			return 0;
		}

		public int UIntIn (uint val)
		{
			if (val != 100)
				return 6;
			return 0;
		}

		public int LongIn (long val)
		{
			if (val != -100)
				return 7;
			return 0;
		}

		public int ULongIn (ulong val)
		{
			if (val != 100)
				return 8;
			return 0;
		}

		public int FloatIn (float val)
		{
			if (Math.Abs (val - 3.14f) > .000001)
				return 9;
			return 0;
		}

		public int DoubleIn (double val)
		{
			if (Math.Abs (val - 3.14f) > .000001)
				return 10;
			return 0;
		}

		public int ITestIn ([MarshalAs (UnmanagedType.Interface)]ITestPresSig val)
		{
			if (val == null)
				return 11;
			if (null == val as ManagedTestPresSig)
				return 12;
			return 0;
		}

		public int ITestOut ([MarshalAs (UnmanagedType.Interface)]out ITestPresSig val)
		{
			val = new ManagedTestPresSig ();
			return 0;
		}

		public int Return22NoICall()
		{
			return 88;
		}
	}

	public class ManagedTest : ITest
	{
		private int status = 0;
		public int Status
		{
			get { return status; }
		}
		public void SByteIn (sbyte val)
		{
			if (val != -100)
				status = 1;
		}

		public void ByteIn (byte val)
		{
			if (val != 100)
				status = 2;
		}

		public void ShortIn (short val)
		{
			if (val != -100)
				status = 3;
		}

		public void UShortIn (ushort val)
		{
			if (val != 100)
				status = 4;
		}

		public void IntIn (int val)
		{
			if (val != -100)
				status = 5;
		}

		public void UIntIn (uint val)
		{
			if (val != 100)
				status = 6;
		}

		public void LongIn (long val)
		{
			if (val != -100)
				status = 7;
		}

		public void ULongIn (ulong val)
		{
			if (val != 100)
				status = 8;
		}

		public void FloatIn (float val)
		{
			if (Math.Abs (val - 3.14f) > .000001)
				status = 9;
		}

		public void DoubleIn (double val)
		{
			if (Math.Abs (val - 3.14) > .000001)
				status = 10;
		}

		public void ITestIn (ITest val)
		{
			if (val == null)
				status = 11;
			if (null == val as ManagedTest)
				status = 12;
		}

		public void ITestOut (out ITest val)
		{
			val = new ManagedTest ();
		}

		public ITest Test
		{
			get
			{
				return new ManagedTest ();
			}
		}

		public int Return22NoICall()
		{
			return 99;
		}
	}

	public static int mono_test_marshal_variant_in_callback (VarEnum vt, object obj)
	{
		switch (vt)
		{
		case VarEnum.VT_I1:
			if (obj.GetType () != typeof (sbyte))
				return 1;
			if ((sbyte)obj != -100)
				return 2;
			break;
		case VarEnum.VT_UI1:
			if (obj.GetType () != typeof (byte))
				return 1;
			if ((byte)obj != 100)
				return 2;
			break;
		case VarEnum.VT_I2:
			if (obj.GetType () != typeof (short))
				return 1;
			if ((short)obj != -100)
				return 2;
			break;
		case VarEnum.VT_UI2:
			if (obj.GetType () != typeof (ushort))
				return 1;
			if ((ushort)obj != 100)
				return 2;
			break;
		case VarEnum.VT_I4:
			if (obj.GetType () != typeof (int))
				return 1;
			if ((int)obj != -100)
				return 2;
			break;
		case VarEnum.VT_UI4:
			if (obj.GetType () != typeof (uint))
				return 1;
			if ((uint)obj != 100)
				return 2;
			break;
		case VarEnum.VT_I8:
			if (obj.GetType () != typeof (long))
				return 1;
			if ((long)obj != -100)
				return 2;
			break;
		case VarEnum.VT_UI8:
			if (obj.GetType () != typeof (ulong))
				return 1;
			if ((ulong)obj != 100)
				return 2;
			break;
		case VarEnum.VT_R4:
			if (obj.GetType () != typeof (float))
				return 1;
			if (Math.Abs ((float)obj - 3.14f) > 1e-10)
				return 2;
			break;
		case VarEnum.VT_R8:
			if (obj.GetType () != typeof (double))
				return 1;
			if (Math.Abs ((double)obj - 3.14) > 1e-10)
				return 2;
			break;
		case VarEnum.VT_BSTR:
			if (obj.GetType () != typeof (string))
				return 1;
			if ((string)obj != "PI")
				return 2;
			break;
		case VarEnum.VT_BOOL:
			if (obj.GetType () != typeof (bool))
				return 1;
			if ((bool)obj != true)
				return 2;
			break;
		}
		return 0;
	}

	public static int mono_test_marshal_variant_out_callback (VarEnum vt, ref object obj)
	{
		switch (vt) {
		case VarEnum.VT_I1:
			obj = (sbyte)-100;
			break;
		case VarEnum.VT_UI1:
			obj = (byte)100;
			break;
		case VarEnum.VT_I2:
			obj = (short)-100;
			break;
		case VarEnum.VT_UI2:
			obj = (ushort)100;
			break;
		case VarEnum.VT_I4:
			obj = (int)-100;
			break;
		case VarEnum.VT_UI4:
			obj = (uint)100;
			break;
		case VarEnum.VT_I8:
			obj = (long)-100;
			break;
		case VarEnum.VT_UI8:
			obj = (ulong)100;
			break;
		case VarEnum.VT_R4:
			obj = (float)3.14f;
			break;
		case VarEnum.VT_R8:
			obj = (double)3.14;
			break;
		case VarEnum.VT_BSTR:
			obj = "PI";
			break;
		case VarEnum.VT_BOOL:
			obj = true;
			break;
		}
		return 0;
	}

	public static int TestITest (ITest itest)
	{
		try {
			ITest itest2;
			itest.SByteIn (-100);
			itest.ByteIn (100);
			itest.ShortIn (-100);
			itest.UShortIn (100);
			itest.IntIn (-100);
			itest.UIntIn (100);
			itest.LongIn (-100);
			itest.ULongIn (100);
			itest.FloatIn (3.14f);
			itest.DoubleIn (3.14);
			itest.ITestIn (itest);
			itest.ITestOut (out itest2);
		}
		catch (Exception ex) {
			return 1;
		}
		return 0;
	}

	public static int TestITestPresSig (ITestPresSig itest)
	{
		ITestPresSig itest2;
		if (itest.SByteIn (-100) != 0)
			return 1000;
		if (itest.ByteIn (100) != 0)
			return 1001;
		if (itest.ShortIn (-100) != 0)
			return 1002;
		if (itest.UShortIn (100) != 0)
			return 1003;
		if (itest.IntIn (-100) != 0)
			return 1004;
		if (itest.UIntIn (100) != 0)
			return 1005;
		if (itest.LongIn (-100) != 0)
			return 1006;
		if (itest.ULongIn (100) != 0)
			return 1007;
		if (itest.FloatIn (3.14f) != 0)
			return 1008;
		if (itest.DoubleIn (3.14) != 0)
			return 1009;
		if (itest.ITestIn (itest) != 0)
			return 1010;
		if (itest.ITestOut (out itest2) != 0)
			return 1011;
		return 0;
	}

	public static int TestITestDelegate (ITest itest)
	{
		try {
			ITest itest2;

			SByteInDelegate SByteInFcn= itest.SByteIn;
			ByteInDelegate ByteInFcn = itest.ByteIn;
			UShortInDelegate UShortInFcn = itest.UShortIn;
			IntInDelegate IntInFcn = itest.IntIn;
			UIntInDelegate UIntInFcn = itest.UIntIn;
			LongInDelegate LongInFcn = itest.LongIn;

			ULongInDelegate ULongInFcn = itest.ULongIn;
			FloatInDelegate FloatInFcn = itest.FloatIn;
			DoubleInDelegate DoubleInFcn = itest.DoubleIn;
			ITestInDelegate ITestInFcn = itest.ITestIn;
			ITestOutDelegate ITestOutFcn = itest.ITestOut;

			SByteInFcn (-100);
			ByteInFcn (100);
			UShortInFcn (100);
			IntInFcn (-100);
			UIntInFcn (100);
			LongInFcn (-100);
			ULongInFcn (100);
			FloatInFcn (3.14f);
			DoubleInFcn (3.14);
			ITestInFcn (itest);
			ITestOutFcn (out itest2);
		}
		catch (Exception) {
			return 1;
		}
		return 0;
	}

	public static int TestIfaceNoIcall (ITestPresSig itest) {
		return itest.Return22NoICall () == 22 ? 0 : 1;
	}
}

public class TestVisible
{
}
