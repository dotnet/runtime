using System;
using System.Runtime.InteropServices;

public class marshalbool
{
	[AttributeUsage (AttributeTargets.Method)]
	sealed class MonoPInvokeCallbackAttribute : Attribute {
		public MonoPInvokeCallbackAttribute (Type t) {}
	}

	[DllImport ("libtest")]
	static extern int mono_test_marshal_bool_in (int arg, uint expected,
						     bool bDefaultMarsh,
						     [MarshalAs (UnmanagedType.Bool)] bool bBoolCustMarsh,
						     [MarshalAs (UnmanagedType.I1)] bool bI1CustMarsh,
						     [MarshalAs (UnmanagedType.U1)] bool bU1CustMarsh,
						     [MarshalAs (UnmanagedType.VariantBool)] bool bVBCustMarsh);

	[DllImport ("libtest")]
	static extern int mono_test_marshal_bool_out (int arg, uint testVal,
						     out bool bDefaultMarsh,
						     [MarshalAs (UnmanagedType.Bool)] out bool bBoolCustMarsh,
						     [MarshalAs (UnmanagedType.I1)] out bool bI1CustMarsh,
						     [MarshalAs (UnmanagedType.U1)] out bool bU1CustMarsh,
						     [MarshalAs (UnmanagedType.VariantBool)] out bool bVBCustMarsh);

	[DllImport ("libtest")]
	static extern int mono_test_marshal_bool_ref (int arg, uint expected, uint testVal,
						     ref bool bDefaultMarsh,
						     [MarshalAs (UnmanagedType.Bool)] ref bool bBoolCustMarsh,
						     [MarshalAs (UnmanagedType.I1)] ref bool bI1CustMarsh,
						     [MarshalAs (UnmanagedType.U1)] ref bool bU1CustMarsh,
						     [MarshalAs (UnmanagedType.VariantBool)] ref bool bVBCustMarsh);

	delegate int MarshalBoolInDelegate (int arg, uint expected,
					    bool bDefaultMarsh,
					    [MarshalAs (UnmanagedType.Bool)] bool bBoolCustMarsh,
					    [MarshalAs (UnmanagedType.I1)] bool bI1CustMarsh,
					    [MarshalAs (UnmanagedType.U1)] bool bU1CustMarsh,
					    [MarshalAs (UnmanagedType.VariantBool)] bool bVBCustMarsh);

	delegate int MarshalBoolOutDelegate (int arg, uint testVal,
					     out bool bDefaultMarsh,
					     [MarshalAs (UnmanagedType.Bool)] out bool bBoolCustMarsh,
					     [MarshalAs (UnmanagedType.I1)] out bool bI1CustMarsh,
					     [MarshalAs (UnmanagedType.U1)] out bool bU1CustMarsh,
					     [MarshalAs (UnmanagedType.VariantBool)] out bool bVBCustMarsh);

	delegate int MarshalBoolRefDelegate (int arg, uint expected, uint testVal,
					     ref bool bDefaultMarsh,
					     [MarshalAs (UnmanagedType.Bool)] ref bool bBoolCustMarsh,
					     [MarshalAs (UnmanagedType.I1)] ref bool bI1CustMarsh,
					     [MarshalAs (UnmanagedType.U1)] ref bool bU1CustMarsh,
					     [MarshalAs (UnmanagedType.VariantBool)] ref bool bVBCustMarsh);

	[DllImport ("libtest")]
	static extern int mono_test_managed_marshal_bool_in (int arg, uint expected, uint testVal, MarshalBoolInDelegate fcn);

	[DllImport ("libtest")]
	static extern int mono_test_managed_marshal_bool_out (int arg, uint expected, uint testVal, MarshalBoolOutDelegate fcn);

	[DllImport ("libtest")]
	static extern int mono_test_managed_marshal_bool_ref (int arg, uint expected, uint testVal,
							      uint outExpected, uint outTestVal, MarshalBoolRefDelegate fcn);

	static int Main (string[] args)
        {
                return TestDriver.RunTests (typeof (marshalbool), args);
        }

	unsafe public static int test_0_Default_In_Native ()
	{
		int ret;

		ret = mono_test_marshal_bool_in (1, 0, false, false, false, false, false);
		if (ret != 0)
			return 0x0100 + ret;
		ret = mono_test_marshal_bool_in (1, 1, true, false, false, false, false);
		if (ret != 0)
			return 0x0200 + ret;

		bool testVal = false;
		bool* ptestVal = &testVal;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_in (1, 1, testVal, false, false, false, false);
		if (ret != 0)
			return 0x0300 + ret;

		return 0;
	}

	unsafe public static int test_0_Bool_In_Native ()
	{
		int ret;

		ret = mono_test_marshal_bool_in (2, 0, false, false, false, false, false);
		if (ret != 0)
			return 0x0100 + ret;
		ret = mono_test_marshal_bool_in (2, 1, false, true, false, false, false);
		if (ret != 0)
			return 0x0200 + ret;

		bool testVal = false;
		bool* ptestVal = &testVal;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_in (2, 1, false, testVal, false, false, false);
		if (ret != 0)
			return 0x0300 + ret;

		return 0;
	}

	unsafe public static int test_0_I1_In_Native ()
	{
		int ret;

		ret = mono_test_marshal_bool_in (3, 0, false, false, false, false, false);
		if (ret != 0)
			return 0x0100 + ret;
		ret = mono_test_marshal_bool_in (3, 1, false, false, true, false, false);
		if (ret != 0)
			return 0x0200 + ret;

		bool testVal = false;
		bool* ptestVal = &testVal;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_in (3, 1, false, false, testVal, false, false);
		if (ret != 0)
			return 0x0300 + ret;

		return 0;
	}

	unsafe public static int test_0_U1_In_Native ()
	{
		int ret;

		ret = mono_test_marshal_bool_in (4, 0, false, false, false, false, false);
		if (ret != 0)
			return 0x0100 + ret;
		ret = mono_test_marshal_bool_in (4, 1, false, false, false, true, false);
		if (ret != 0)
			return 0x0200 + ret;

		bool testVal = false;
		bool* ptestVal = &testVal;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_in (4, 1, false, false, false, testVal, false);
		if (ret != 0)
			return 0x0300 + ret;

		return 0;
	}

	unsafe public static int test_0_VariantBool_In_Native ()
	{
		int ret;
		int m1 = -1;

		ret = mono_test_marshal_bool_in (5, 0, false, false, false, false, false);
		if (ret != 0)
			return 0x0100 + ret;
		ret = mono_test_marshal_bool_in (5, (uint)m1, false, false, false, false, true);
		if (ret != 0)
			return 0x0200 + ret;

		bool testVal = false;
		bool* ptestVal = &testVal;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_in (5, (uint)m1, false, false, false, false, testVal);
		if (ret != 0)
			return 0x0300 + ret;

		return 0;
	}

	unsafe public static int test_0_Default_Out_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_out (1, 0, out testVal, out d, out d, out d, out d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_out (1, 1, out testVal, out d, out d, out d, out d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_out (1, 0x22000000, out testVal, out d, out d, out d, out d);
		if (ret != 0)
			return 0x0500 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0600;

		return 0;
	}

	unsafe public static int test_0_Bool_Out_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_out (2, 0, out d, out testVal, out d, out d, out d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_out (2, 1, out d, out testVal, out d, out d, out d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_out (2, 0x22000000, out d, out testVal, out d, out d, out d);
		if (ret != 0)
			return 0x0500 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0600;

		return 0;
	}

	unsafe public static int test_0_I1_Out_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_out (3, 0, out d, out d, out testVal, out d, out d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_out (3, 1, out d, out d, out testVal, out d, out d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_out (3, 0x22, out d, out d, out testVal, out d, out d);
		if (ret != 0)
			return 0x0500 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0600;

		return 0;
	}

	unsafe public static int test_0_U1_Out_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_out (4, 0, out d, out d, out d, out testVal, out d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_out (4, 1, out d, out d, out d, out testVal, out d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_out (4, 0x22, out d, out d, out d, out testVal, out d);
		if (ret != 0)
			return 0x0500 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0600;

		return 0;
	}

	unsafe public static int test_0_VariantBool_Out_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_out (5, 0, out d, out d, out d, out d, out testVal);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_out (5, 1, out d, out d, out d, out d, out testVal);
		if (ret != 0)
			return 0x0100 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0200;

		ret = mono_test_marshal_bool_out (5, 0x2200, out d, out d, out d, out d, out testVal);
		if (ret != 0)
			return 0x0100 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0200;

		return 0;
	}

	unsafe public static int test_0_Default_Ref_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_ref (1, 0, 0, ref testVal, ref d, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_ref (1, 0, 1, ref testVal, ref d, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_ref (1, 1, 0, ref testVal, ref d, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0500 + ret;
		if (testVal)
			return 0x0600;

		testVal = true;
		ret = mono_test_marshal_bool_ref (1, 1, 1, ref testVal, ref d, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0700 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x08800;

		testVal = false;
		ret = mono_test_marshal_bool_ref (1, 0, 0x22000000, ref testVal, ref d, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0900 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1000;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (1, 1, 0, ref testVal, ref d, ref d, ref d, ref d);
		if (ret != 0)
			return 0x1100 + ret;
		if (testVal)
			return 0x1200;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (1, 1, 0x22000000, ref testVal, ref d, ref d, ref d, ref d);
		if (ret != 0)
			return 0x1300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1400;

		return 0;
	}

	unsafe public static int test_0_Bool_Ref_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_ref (2, 0, 0, ref d, ref testVal, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_ref (2, 0, 1, ref d, ref testVal, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_ref (2, 1, 0, ref d, ref testVal, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0500 + ret;
		if (testVal)
			return 0x0600;

		testVal = true;
		ret = mono_test_marshal_bool_ref (2, 1, 1, ref d, ref testVal, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0700 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0800;

		testVal = false;
		ret = mono_test_marshal_bool_ref (2, 0, 0x22000000, ref d, ref testVal, ref d, ref d, ref d);
		if (ret != 0)
			return 0x0900 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1000;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (2, 1, 0, ref d, ref testVal, ref d, ref d, ref d);
		if (ret != 0)
			return 0x1100 + ret;
		if (testVal)
			return 0x1200;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (2, 1, 0x22000000, ref d, ref testVal, ref d, ref d, ref d);
		if (ret != 0)
			return 0x1300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1400;

		return 0;
	}

	unsafe public static int test_0_I1_Ref_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_ref (3, 0, 0, ref d, ref d, ref testVal, ref d, ref d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_ref (3, 0, 1, ref d, ref d, ref testVal, ref d, ref d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_ref (3, 1, 0, ref d, ref d, ref testVal, ref d, ref d);
		if (ret != 0)
			return 0x0500 + ret;
		if (testVal)
			return 0x0600;

		testVal = true;
		ret = mono_test_marshal_bool_ref (3, 1, 1, ref d, ref d, ref testVal, ref d, ref d);
		if (ret != 0)
			return 0x0700 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0800;

		testVal = false;
		ret = mono_test_marshal_bool_ref (3, 0, 0x22, ref d, ref d, ref testVal, ref d, ref d);
		if (ret != 0)
			return 0x0900 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1000;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (3, 1, 0, ref d, ref d, ref testVal, ref d, ref d);
		if (ret != 0)
			return 0x1100 + ret;
		if (testVal)
			return 0x1200;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (3, 1, 0x22, ref d, ref d, ref testVal, ref d, ref d);
		if (ret != 0)
			return 0x1300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1400;

		return 0;
	}

	unsafe public static int test_0_U1_Ref_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_ref (4, 0, 0, ref d, ref d, ref d, ref testVal, ref d);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_ref (4, 0, 1, ref d, ref d, ref d, ref testVal, ref d);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_ref (4, 1, 0, ref d, ref d, ref d, ref testVal, ref d);
		if (ret != 0)
			return 0x0500 + ret;
		if (testVal)
			return 0x0600;

		testVal = true;
		ret = mono_test_marshal_bool_ref (4, 1, 1, ref d, ref d, ref d, ref testVal, ref d);
		if (ret != 0)
			return 0x0700 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0800;

		testVal = false;
		ret = mono_test_marshal_bool_ref (4, 0, 0x22, ref d, ref d, ref d, ref testVal, ref d);
		if (ret != 0)
			return 0x0900 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1000;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (4, 1, 0, ref d, ref d, ref d, ref testVal, ref d);
		if (ret != 0)
			return 0x1100 + ret;
		if (testVal)
			return 0x1200;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (4, 1, 0x22, ref d, ref d, ref d, ref testVal, ref d);
		if (ret != 0)
			return 0x1300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1400;

		return 0;
	}

	unsafe public static int test_0_VariantBool_Ref_Native ()
	{
		bool testVal = false;
		bool* ptestVal = &testVal;
		bool d = false;
		int ret;

		ret = mono_test_marshal_bool_ref (5, 0, 0, ref d, ref d, ref d, ref d, ref testVal);
		if (ret != 0)
			return 0x0100 + ret;
		if (testVal)
			return 0x0200;

		ret = mono_test_marshal_bool_ref (5, 0, 1, ref d, ref d, ref d, ref d, ref testVal);
		if (ret != 0)
			return 0x0300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0400;

		ret = mono_test_marshal_bool_ref (5, 0xFFFF, 0, ref d, ref d, ref d, ref d, ref testVal);
		if (ret != 0)
			return 0x0500 + ret;
		if (testVal)
			return 0x0600;

		testVal = true;
		ret = mono_test_marshal_bool_ref (5, 0xFFFF, 1, ref d, ref d, ref d, ref d, ref testVal);
		if (ret != 0)
			return 0x0700 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x0800;

		testVal = false;
		ret = mono_test_marshal_bool_ref (5, 0, 0x2200, ref d, ref d, ref d, ref d, ref testVal);
		if (ret != 0)
			return 0x0900 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1000;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (5, 0xFFFF, 0, ref d, ref d, ref d, ref d, ref testVal);
		if (ret != 0)
			return 0x1100 + ret;
		if (testVal)
			return 0x1200;

		Marshal.WriteByte ((IntPtr)ptestVal, 0x22);
		ret = mono_test_marshal_bool_ref (5, 0xFFFF, 0x2200, ref d, ref d, ref d, ref d, ref testVal);
		if (ret != 0)
			return 0x1300 + ret;
		if (1 != Marshal.ReadByte ((IntPtr)ptestVal))
			return 0x1400;

		return 0;
	}

	public static int test_0_Default_In_Managed ()
	{
		MarshalBoolInDelegate fcn = new MarshalBoolInDelegate (MarshalBoolInHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_in (1, 0, 0, fcn);
		if (ret != 0)
			return 0x0100 + ret;
		ret =  mono_test_managed_marshal_bool_in (1, 1, 1, fcn);
		if (ret != 0)
			return 0x0200 + ret;
		ret =  mono_test_managed_marshal_bool_in (1, 1, 0x22000000, fcn);
		if (ret != 0)
			return 0x0300 + ret;
		return 0;
	}

	public static int test_0_Bool_In_Managed ()
	{
		MarshalBoolInDelegate fcn = new MarshalBoolInDelegate (MarshalBoolInHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_in (2, 0, 0, fcn);
		if (ret != 0)
			return 0x0100 + ret;
		ret =  mono_test_managed_marshal_bool_in (2, 1, 1, fcn);
		if (ret != 0)
			return 0x0200 + ret;
		ret =  mono_test_managed_marshal_bool_in (2, 1, 0x22000000, fcn);
		if (ret != 0)
			return 0x0300 + ret;
		return 0;
	}

	public static int test_0_I1_In_Managed ()
	{
		MarshalBoolInDelegate fcn = new MarshalBoolInDelegate (MarshalBoolInHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_in (3, 0, 0, fcn);
		if (ret != 0)
			return 0x0100 + ret;
		ret =  mono_test_managed_marshal_bool_in (3, 1, 1, fcn);
		if (ret != 0)
			return 0x0200 + ret;
		ret =  mono_test_managed_marshal_bool_in (3, 1, 0x22, fcn);
		if (ret != 0)
			return 0x0300 + ret;
		return 0;
	}

	public static int test_0_U1_In_Managed ()
	{
		MarshalBoolInDelegate fcn = new MarshalBoolInDelegate (MarshalBoolInHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_in (4, 0, 0, fcn);
		if (ret != 0)
			return 0x0100 + ret;
		ret = mono_test_managed_marshal_bool_in (4, 1, 1, fcn);
		if (ret != 0)
			return 0x0200 + ret;
		ret = mono_test_managed_marshal_bool_in (4, 1, 0x22, fcn);
		if (ret != 0)
			return 0x0300 + ret;
		return 0;
	}

	public static int test_0_VariantBool_In_Managed ()
	{
		MarshalBoolInDelegate fcn = new MarshalBoolInDelegate (MarshalBoolInHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_in (5, 0, 0, fcn);
		if (ret != 0)
			return 0x0100 + ret;
		ret = mono_test_managed_marshal_bool_in (5, 1, 0xFFFF, fcn);
		if (ret != 0)
			return 0x0200 + ret;
		ret = mono_test_managed_marshal_bool_in (5, 1, 0x22, fcn);
		if (ret != 0)
			return 0x0300 + ret;
		return 0;
	}

	public static int test_0_Default_Out_Managed ()
	{
		MarshalBoolOutDelegate fcn = new MarshalBoolOutDelegate (MarshalBoolOutHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_out (1, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_out (1, 1, 1, fcn);
		if (ret != 0)
			return 0x020000 + ret;
		ret = mono_test_managed_marshal_bool_out (1, 1, 0x22, fcn);
		if (ret != 0)
			return 0x030000 + ret;
		return 0;
	}

	public static int test_0_Bool_Out_Managed ()
	{
		MarshalBoolOutDelegate fcn = new MarshalBoolOutDelegate (MarshalBoolOutHelper);
		int ret;
		
		ret = mono_test_managed_marshal_bool_out (2, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_out (2, 1, 1, fcn);
		if (ret != 0)
			return 0x020000 + ret;
		ret = mono_test_managed_marshal_bool_out (2, 1, 0x22, fcn);
		if (ret != 0)
			return 0x030000 + ret;
		return 0;
	}

	public static int test_0_I1_Out_Managed ()
	{
		MarshalBoolOutDelegate fcn = new MarshalBoolOutDelegate (MarshalBoolOutHelper);
		int ret;
		
		ret = mono_test_managed_marshal_bool_out (3, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_out (3, 1, 1, fcn);
		if (ret != 0)
			return 0x020000 + ret;
		ret = mono_test_managed_marshal_bool_out (3, 1, 0x22, fcn);
		if (ret != 0)
			return 0x030000 + ret;
		return 0;
	}

	public static int test_0_U1_Out_Managed ()
	{
		MarshalBoolOutDelegate fcn = new MarshalBoolOutDelegate (MarshalBoolOutHelper);
		int ret;
		
		ret = mono_test_managed_marshal_bool_out (4, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_out (4, 1, 1, fcn);
		if (ret != 0)
			return 0x020000 + ret;
		ret = mono_test_managed_marshal_bool_out (4, 1, 0x22, fcn);
		if (ret != 0)
			return 0x030000 + ret;
		return 0;
	}

	public static int test_0_VariantBool_Out_Managed ()
	{
		MarshalBoolOutDelegate fcn = new MarshalBoolOutDelegate (MarshalBoolOutHelper);
		int ret;
		
		ret = mono_test_managed_marshal_bool_out (5, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_out (5, 0xFFFF, 1, fcn);
		if (ret != 0)
			return 0x020000 + ret;
		ret = mono_test_managed_marshal_bool_out (5, 0xFFFF, 0x22, fcn);
		if (ret != 0)
			return 0x030000 + ret;
		return 0;
	}

	public static int test_0_Default_Ref_Managed ()
	{
		MarshalBoolRefDelegate fcn = new MarshalBoolRefDelegate (MarshalBoolRefHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_ref (1, 0, 0, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (1, 1, 1, 0, 0, fcn);
		if (ret != 0)
			return 0x020000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (1, 0, 0, 1, 1, fcn);
		if (ret != 0)
			return 0x030000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (1, 1, 1, 1, 1, fcn);
		if (ret != 0)
			return 0x040000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (1, 1, 0x22000000, 0, 0, fcn);
		if (ret != 0)
			return 0x050000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (1, 0, 0, 1, 0x22, fcn);
		if (ret != 0)
			return 0x060000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (1, 1, 0x22000000, 1, 0x22, fcn);
		if (ret != 0)
			return 0x070000 + ret; 
		return 0;
	}

	public static int test_0_Bool_Ref_Managed ()
	{
		MarshalBoolRefDelegate fcn = new MarshalBoolRefDelegate (MarshalBoolRefHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_ref (2, 0, 0, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (2, 1, 1, 0, 0, fcn);
		if (ret != 0)
			return 0x020000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (2, 0, 0, 1, 1, fcn);
		if (ret != 0)
			return 0x030000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (2, 1, 1, 1, 1, fcn);
		if (ret != 0)
			return 0x040000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (2, 1, 0x22000000, 0, 0, fcn);
		if (ret != 0)
			return 0x050000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (2, 0, 0, 1, 0x22, fcn);
		if (ret != 0)
			return 0x060000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (2, 1, 0x22000000, 1, 0x22, fcn);
		if (ret != 0)
			return 0x070000 + ret; 
		return 0;
	}

	public static int test_0_I1_Ref_Managed ()
	{
		MarshalBoolRefDelegate fcn = new MarshalBoolRefDelegate (MarshalBoolRefHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_ref (3, 0, 0, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (3, 1, 1, 0, 0, fcn);
		if (ret != 0)
			return 0x020000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (3, 0, 0, 1, 1, fcn);
		if (ret != 0)
			return 0x030000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (3, 1, 1, 1, 1, fcn);
		if (ret != 0)
			return 0x040000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (3, 1, 0x22, 0, 0, fcn);
		if (ret != 0)
			return 0x050000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (3, 0, 0, 1, 0x22, fcn);
		if (ret != 0)
			return 0x060000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (3, 1, 0x22, 1, 0x22, fcn);
		if (ret != 0)
			return 0x070000 + ret; 
		return 0;
	}

	public static int test_0_U1_Ref_Managed ()
	{
		MarshalBoolRefDelegate fcn = new MarshalBoolRefDelegate (MarshalBoolRefHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_ref (4, 0, 0, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (4, 1, 1, 0, 0, fcn);
		if (ret != 0)
			return 0x020000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (4, 0, 0, 1, 1, fcn);
		if (ret != 0)
			return 0x030000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (4, 1, 1, 1, 1, fcn);
		if (ret != 0)
			return 0x040000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (4, 1, 0x22, 0, 0, fcn);
		if (ret != 0)
			return 0x050000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (4, 0, 0, 1, 0x22, fcn);
		if (ret != 0)
			return 0x060000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (4, 1, 0x22, 1, 0x22, fcn);
		if (ret != 0)
			return 0x070000 + ret; 
		return 0;
	}

	public static int test_0_VariantBool_Ref_Managed ()
	{
		MarshalBoolRefDelegate fcn = new MarshalBoolRefDelegate (MarshalBoolRefHelper);
		int ret;

		ret = mono_test_managed_marshal_bool_ref (5, 0, 0, 0, 0, fcn);
		if (ret != 0)
			return 0x010000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (5, 1, 0xFFFF, 0, 0, fcn);
		if (ret != 0)
			return 0x020000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (5, 0, 0, 0xFFFF, 1, fcn);
		if (ret != 0)
			return 0x030000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (5, 1, 0xFFFF, 0xFFFF, 1, fcn);
		if (ret != 0)
			return 0x040000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (5, 1, 0x2200, 0, 0, fcn);
		if (ret != 0)
			return 0x050000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (5, 0, 0, 0xFFFF, 0x22, fcn);
		if (ret != 0)
			return 0x060000 + ret; 
		ret = mono_test_managed_marshal_bool_ref (5, 1, 0x2200, 0xFFFF, 0x22, fcn);
		if (ret != 0)
			return 0x070000 + ret; 
		return 0;
	}

///////////////////////////////////////////////////////////////////

	[MonoPInvokeCallback (typeof (MarshalBoolInDelegate))]
	unsafe static int MarshalBoolInHelper (int arg, uint expected, bool bDefaultMarsh, bool bBoolCustMarsh, bool bI1CustMarsh,
										   bool bU1CustMarsh, bool bVBCustMarsh)
	{
		bool* ptestVal;
		switch (arg) {
		case 1 :
			ptestVal = &bDefaultMarsh;
			if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
				return 1;
			break;
		case 2 :
			ptestVal = &bBoolCustMarsh;
			if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
				return 2;
			break;
		case 3 :
			ptestVal = &bI1CustMarsh;
			if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
				return 3;
			break;
		case 4 :
			ptestVal = &bU1CustMarsh;
			if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
				return 4;
			break;
		case 5 :
			ptestVal = &bVBCustMarsh;
			if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
				return 5;
			break;
		default :
			return 99;	
		}
		return 0;
	}

	[MonoPInvokeCallback (typeof (MarshalBoolOutDelegate))]
	unsafe static int MarshalBoolOutHelper (int arg, uint testVal, out bool bDefaultMarsh, out bool bBoolCustMarsh,
											out bool bI1CustMarsh, out bool bU1CustMarsh, out bool bVBCustMarsh)
	{
		bDefaultMarsh = bBoolCustMarsh = bI1CustMarsh = bU1CustMarsh = bVBCustMarsh = false;
		switch (arg) {
		case 1:
			fixed (bool*ptestVal = &bDefaultMarsh)
			{
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 2:
			fixed (bool*ptestVal = &bBoolCustMarsh)
			{
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 3:
			fixed (bool*ptestVal = &bI1CustMarsh)
			{
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 4:
			fixed (bool*ptestVal = &bU1CustMarsh)
			{
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 5:
			fixed (bool*ptestVal = &bVBCustMarsh)
			{
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		default :
			return 99;
		}
		return 0;
	}

	[MonoPInvokeCallback (typeof (MarshalBoolRefDelegate))]
	unsafe static int MarshalBoolRefHelper (int arg, uint expected, uint testVal, ref bool bDefaultMarsh, ref bool bBoolCustMarsh,
											ref bool bI1CustMarsh, ref bool bU1CustMarsh, ref bool bVBCustMarsh)
	{
		switch (arg) {
		case 1:
			fixed (bool*ptestVal = &bDefaultMarsh)
			{
				if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
					return 1;
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 2:
			fixed (bool*ptestVal = &bBoolCustMarsh)
			{
				if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
					return 2;
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 3:
			fixed (bool*ptestVal = &bI1CustMarsh)
			{
				if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
					return 3;
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 4:
			fixed (bool*ptestVal = &bU1CustMarsh)
			{
				if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
					return 4;
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		case 5:
			fixed (bool*ptestVal = &bVBCustMarsh)
			{
				if (expected != Marshal.ReadByte ((IntPtr)ptestVal))
					return 5;
				Marshal.WriteByte ((IntPtr)ptestVal, (byte)testVal);
			}
			break;
		default :
			return 99;
		}
		return 0;
	}

}

