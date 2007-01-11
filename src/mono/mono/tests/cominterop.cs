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
	public static extern int mono_test_marshal_variant_out_bstr ([MarshalAs (UnmanagedType.Struct)]out object obj);

	[DllImport("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_true ([MarshalAs (UnmanagedType.Struct)]out object obj);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_variant_out_bool_false ([MarshalAs (UnmanagedType.Struct)]out object obj);


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
    public static extern int mono_test_marshal_com_object_destroy (IntPtr pUnk);

	[DllImport ("libtest")]
	public static extern int mono_test_marshal_com_object_ref_count (IntPtr pUnk);

	public static int Main() {

        bool isWindows = !(((int)Environment.OSVersion.Platform == 4) || 
            ((int)Environment.OSVersion.Platform == 128));
		if (isWindows) {
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

			#endregion // VARIANT Tests

			#region Marshal COM Interop Tests

			IntPtr pUnk;
			if (mono_test_marshal_com_object_create (out pUnk) != 0)
				return 65;

			if (mono_test_marshal_com_object_ref_count (pUnk) != 1)
				return 46;

			if (Marshal.AddRef (pUnk) != 2)
				return 47;

			if (mono_test_marshal_com_object_ref_count (pUnk) != 2)
				return 48;

			if (Marshal.Release (pUnk) != 1)
				return 49;

			if (mono_test_marshal_com_object_ref_count (pUnk) != 1)
				return 50;

			object com_obj = Marshal.GetObjectForIUnknown (pUnk);

			if (com_obj == null)
				return 51;

			IMath imath = com_obj as IMath;

			if (imath == null)
				return 52;

			if (imath.Add (20, 10) != 30)
				return 53;

			if (imath.Subtract (20, 10) != 10)
				return 54;

			IMath same1, same2;
			imath.Same (out same1);
			imath.Same (out same2);
			if (same1 != same2)
				return 55;

			if (!same1.Equals (same2))
				return 56;

			IMath diff1, diff2;
			imath.Different (out diff1);
			imath.Different (out diff2);
			if (diff1 == diff2)
				return 57;

			if (diff1.Equals (diff2))
				return 58;

			// same1 & same2 share a RCW
			if (Marshal.ReleaseComObject (same1) != 1)
				return 59;

			if (Marshal.ReleaseComObject (same2) != 0)
				return 60;


			if (Marshal.ReleaseComObject (diff1) != 0 ||
				Marshal.ReleaseComObject (diff2) != 0)
				return 61;

			IntPtr pUnk2 = Marshal.GetIUnknownForObject (imath);
			if (pUnk2 == IntPtr.Zero)
				return 70;

			if (pUnk != pUnk2)
				return 71;

			IntPtr pDisp = Marshal.GetIDispatchForObject (imath);
			if (pDisp == IntPtr.Zero)
				return 72;

			if (pUnk != pDisp)
				return 73;


			//if (mono_test_marshal_com_object_destroy (pUnk) != 0)
			//    return 71;
			#endregion // Marshal COM Interop Tests
		}

        return 0;
	}

    [ComImport()]
    [Guid ("00000000-0000-0000-0000-000000000001")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMath
    {
        [MethodImplAttribute (MethodImplOptions.InternalCall,MethodCodeType=MethodCodeType.Runtime)]
        int Add (int a, int b);
        [MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		int Subtract (int a, int b);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		int Same ([MarshalAs(UnmanagedType.Interface)] out IMath imath);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		int Different ([MarshalAs (UnmanagedType.Interface)] out IMath imath);
    }

	[ComImport ()]
	[Guid ("00000000-0000-0000-0000-000000000002")]
	public class Foo : IMath
	{
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public extern int Add (int a, int b);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public extern int Subtract (int a, int b);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public extern int Same ([MarshalAs (UnmanagedType.Interface)] out IMath imath);
		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public extern int Different ([MarshalAs (UnmanagedType.Interface)] out IMath imath);
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
}
