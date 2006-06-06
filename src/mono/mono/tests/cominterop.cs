//
// cominterop.cs:
//
//  Tests for COM Interop related features
//

using System;
using System.Runtime.InteropServices;

public class Tests
{

	[DllImport("libtest")]
	public static extern int mono_test_marshal_bstr_in([MarshalAs(UnmanagedType.BStr)]string str);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_bstr_out([MarshalAs(UnmanagedType.BStr)] out string str);

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
	public static extern int mono_test_marshal_variant_in_bstr([MarshalAs(UnmanagedType.Struct)]object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_sbyte([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_byte([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_short([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_ushort([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_int([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_uint([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_long([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_ulong([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_float([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_double([MarshalAs(UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_bstr([MarshalAs(UnmanagedType.Struct)]out object obj);

	public static int Main() {

		if (((int)Environment.OSVersion.Platform == 4) || ((int)Environment.OSVersion.Platform == 128))
			return 0;

		#region BSTR Tests

		string str;
		if (mono_test_marshal_bstr_in("mono_test_marshal_bstr_in") != 0)
			return 1;
		if (mono_test_marshal_bstr_out(out str) != 0 || str != "mono_test_marshal_bstr_out")
			return 2;

		#endregion // BSTR Tests

		#region VARIANT Tests

		object obj;
		if (mono_test_marshal_variant_in_sbyte((sbyte)100) != 0)
			return 3;
		if (mono_test_marshal_variant_in_byte((byte)100) != 0)
			return 4;
		if (mono_test_marshal_variant_in_short((short)314) != 0)
			return 5;
		if (mono_test_marshal_variant_in_ushort((ushort)314) != 0)
			return 6;
		if (mono_test_marshal_variant_in_int((int)314) != 0)
			return 7;
		if (mono_test_marshal_variant_in_uint((uint)314) != 0)
			return 8;
		if (mono_test_marshal_variant_in_long((long)314) != 0)
			return 9;
		if (mono_test_marshal_variant_in_ulong((ulong)314) != 0)
			return 10;
		if (mono_test_marshal_variant_in_float((float)3.14) != 0)
			return 11;
		if (mono_test_marshal_variant_in_double((double)3.14) != 0)
			return 12;
		if (mono_test_marshal_variant_in_bstr("PI") != 0)
			return 13;
		if (mono_test_marshal_variant_out_sbyte(out obj) != 0 || (sbyte)obj != 100)
			return 14;
		if (mono_test_marshal_variant_out_byte(out obj) != 0 || (byte)obj != 100)
			return 15;
		if (mono_test_marshal_variant_out_short(out obj) != 0 || (short)obj != 314)
			return 16;
		if (mono_test_marshal_variant_out_ushort(out obj) != 0 || (ushort)obj != 314)
			return 17;
		if (mono_test_marshal_variant_out_int(out obj) != 0 || (int)obj != 314)
			return 18;
		if (mono_test_marshal_variant_out_uint(out obj) != 0 || (uint)obj != 314)
			return 19;
		if (mono_test_marshal_variant_out_long(out obj) != 0 || (long)obj != 314)
			return 20;
		if (mono_test_marshal_variant_out_ulong(out obj) != 0 || (ulong)obj != 314)
			return 21;
		if (mono_test_marshal_variant_out_float(out obj) != 0 || ((float)obj - 3.14) / 3.14 > .001)
			return 22;
		if (mono_test_marshal_variant_out_double(out obj) != 0 || ((double)obj - 3.14) / 3.14 > .001)
			return 23;
		if (mono_test_marshal_variant_out_bstr(out obj) != 0 || (string)obj != "PI")
			return 24;

		#endregion // VARIANT Tests

		return 0;
	}
}