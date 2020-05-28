using System;
using System.Runtime.InteropServices;

[AttributeUsage (AttributeTargets.Method)]
sealed class MonoPInvokeCallbackAttribute : Attribute {
	public MonoPInvokeCallbackAttribute (Type t) {}
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct1
{
	public winx64_struct1 (byte ia)
	{
		a = ia;
	}
	public byte a;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct2
{
	public winx64_struct2 (byte ia, byte ib)
	{
		a = ia;
		b = ib;
	}
	
	public byte a;
	public byte b;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct3
{
	public winx64_struct3 (byte ia, byte ib, short ic)
	{
		a = ia;
		b = ib;
		c = ic;
	}
	
	public byte a;
	public byte b;
	public short c;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct4
{
	public winx64_struct4 (byte ia, byte ib, short ic, uint id)
	{
		a = ia;
		b = ib;
		c = ic;
		d = id;
	}
	
	public byte a;
	public byte b;
	public short c;
	public uint d;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct5
{
	public winx64_struct5 (byte ia, byte ib, byte ic)
	{
		a = ia;
		b = ib;
		c = ic;
	}
	
	public byte a;
	public byte b;
	public byte c;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_struct6
{
	public winx64_struct6 (winx64_struct1 ia, short ib, byte ic)
	{
		a = ia;
		b = ib;
		c = ic;
	}
	
	public winx64_struct1 a;
	public short b;
	public byte c;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_floatStruct
{
	public winx64_floatStruct (float ia, float ib)
	{
		a = ia;
		b = ib;
	}
	
	public float a;
	public float b;
}

[StructLayout (LayoutKind.Sequential)]
struct winx64_doubleStruct
{
	public winx64_doubleStruct (double ia)
	{
		a = ia;
	}
	
	public double a;
}

[StructLayout (LayoutKind.Sequential)]
public struct winx64_vector3Struct
{
	public winx64_vector3Struct (float ix, float iy, float iz)
	{
		x=ix;
		y=iy;
		z=iz;
	}

	public static winx64_vector3Struct Add (winx64_vector3Struct a, winx64_vector3Struct b)
	{
		Add(ref a, ref b, out a);
		return a;
	}

	static void Add (ref winx64_vector3Struct a, ref winx64_vector3Struct b, out winx64_vector3Struct result)
	{
		result.x = a.x + b.x;
		result.y = a.y + b.y;
		result.z = a.z + b.z;
	}

	public float x;
	public float y;
	public float z;
}

public struct winx64_vector3PairStruct
{
	public winx64_vector3Struct first;
	public winx64_vector3Struct second;
}

class winx64structs
{
	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct1_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct2_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct2 var);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct3_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct3 var);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct4_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct4 var);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct5_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct5 var);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct6_in ([MarshalAs (UnmanagedType.Struct)] winx64_struct6 var);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_structs_in1 ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var1,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct2 var2,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct3 var3,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct4 var4);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_structs_in2 ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var1,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var2,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var3,
					           [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var4,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var5);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_structs_in3 ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var1,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct5 var2,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var3,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct5 var4,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct1 var5,
						   [MarshalAs (UnmanagedType.Struct)] winx64_struct5 var6);

	[DllImport ("libtest")]
	[return:MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct1 mono_test_Winx64_struct1_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct2 mono_test_Winx64_struct2_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct3 mono_test_Winx64_struct3_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct4 mono_test_Winx64_struct4_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct5 mono_test_Winx64_struct5_ret ();

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct1 mono_test_Winx64_struct1_ret_5_args (byte a, byte b, byte c, byte d, byte e);

	[DllImport ("libtest")]
	[return: MarshalAs (UnmanagedType.Struct)]
	static extern winx64_struct5 mono_test_Winx64_struct5_ret6_args (byte a, byte b, byte c, byte d, byte e	); 

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_floatStruct ([MarshalAs (UnmanagedType.Struct)] winx64_floatStruct var);

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_doubleStruct ([MarshalAs (UnmanagedType.Struct)] winx64_doubleStruct var);

	public delegate int managed_struct1_delegate ([MarshalAs (UnmanagedType.Struct)] winx64_struct1 var);

	[DllImport ("libtest")]
	static extern int mono_test_managed_Winx64_struct1_in (managed_struct1_delegate func);

	public delegate int managed_struct5_delegate ([MarshalAs (UnmanagedType.Struct)] winx64_struct5 var);

	[DllImport ("libtest")]
	static extern int mono_test_managed_Winx64_struct5_in (managed_struct5_delegate func);

	public delegate int managed_struct1_struct5_delegate (winx64_struct1 var1, winx64_struct5 var2,
							      winx64_struct1 var3, winx64_struct5 var4,
							      winx64_struct1 var5, winx64_struct5 var6);

	[DllImport ("libtest")]
	static extern int mono_test_managed_Winx64_struct1_struct5_in (managed_struct1_struct5_delegate func);

	[return:MarshalAs (UnmanagedType.Struct)]
	public delegate winx64_struct1 mono_test_Winx64_struct1_ret_delegate ();

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct1_ret_managed (mono_test_Winx64_struct1_ret_delegate func);

	[return: MarshalAs (UnmanagedType.Struct)]
	public delegate winx64_struct5 mono_test_Winx64_struct5_ret_delegate ();

	[DllImport ("libtest")]
	static extern int mono_test_Winx64_struct5_ret_managed (mono_test_Winx64_struct5_ret_delegate func);
	

	private static bool enableBroken = false;
	
	static int Main (string[] args)
	{
		return TestDriver.RunTests (typeof (winx64structs), args);
	}

	public static int test_0_In_Args_Value_In_RCX ()
	{
		int retCode;

		winx64_struct1 t_winx64_struct1 = new winx64_struct1 (123);

		if ((retCode = mono_test_Winx64_struct1_in (t_winx64_struct1)) != 0)
			return 100 + retCode;

		winx64_struct2 t_winx64_struct2 = new winx64_struct2 (4, 5);

		if ((retCode = mono_test_Winx64_struct2_in (t_winx64_struct2)) != 0)
			return 200 + retCode;

		winx64_struct3 t_winx64_struct3 = new winx64_struct3 (4, 5, 0x1234);

		if ((retCode = mono_test_Winx64_struct3_in (t_winx64_struct3)) != 0)
			return 300 + retCode;

		winx64_struct4 t_winx64_struct4 = new winx64_struct4 (4, 5, 0x1234, 0x87654321);

		if ((retCode = mono_test_Winx64_struct4_in (t_winx64_struct4)) != 0)
			return 400 + retCode;

		winx64_floatStruct t_winx64_floatStruct = new winx64_floatStruct (5.5F, 9.5F);

		if ((retCode = mono_test_Winx64_floatStruct (t_winx64_floatStruct)) != 0)
			return 500 + retCode;

		winx64_doubleStruct t_winx64_doubleStruct = new winx64_doubleStruct (5.5F);

		if ((retCode = mono_test_Winx64_doubleStruct (t_winx64_doubleStruct)) != 0)
			return 600 + retCode;

		return 0;
	}

	public static int test_0_In_Args_Values_In_Multiple_Registers ()
	{
		int retCode; 
		
		winx64_struct1 t_winx64_struct1 = new winx64_struct1 (123);
		winx64_struct2 t_winx64_struct2 = new winx64_struct2 (4, 5);
		winx64_struct3 t_winx64_struct3 = new winx64_struct3 (4, 5, 0x1234);
		winx64_struct4 t_winx64_struct4 = new winx64_struct4 (4, 5, 0x1234, 0x87654321);
		
		if ((retCode = mono_test_Winx64_structs_in1 (t_winx64_struct1, t_winx64_struct2,
							t_winx64_struct3, t_winx64_struct4)) != 0)
			return 100 + retCode;

		
		return 0;
	}

	public static int test_0_Ret_In_RAX ()
	{
		winx64_struct1 t_winx64_struct1 = mono_test_Winx64_struct1_ret ();
		if (t_winx64_struct1.a != 123)
			return 101;

		winx64_struct2 t_winx64_struct2 = mono_test_Winx64_struct2_ret ();
		if (t_winx64_struct2.a != 4)
			return 201;
		if (t_winx64_struct2.b != 5)
			return 202;

		winx64_struct3 t_winx64_struct3 = mono_test_Winx64_struct3_ret ();
		if (t_winx64_struct3.a != 4)
			return 301;
		if (t_winx64_struct3.b != 5)
			return 302;
		if (t_winx64_struct3.c != 0x1234)
			return 303;
		
		winx64_struct4 t_winx64_struct4 = mono_test_Winx64_struct4_ret ();
		if (t_winx64_struct4.a != 4)
			return 401;
		if (t_winx64_struct4.b != 5)
			return 402;
		if (t_winx64_struct4.c != 0x1234)
			return 403;
		if (t_winx64_struct4.d != 0x87654321)
			return 404;

		t_winx64_struct1 = mono_test_Winx64_struct1_ret_5_args (0x1, 0x0, 0x4, 0x10, 0x40);
		if (t_winx64_struct1.a != 0x55)
			return 501;

		return 0;
	}

	public static int test_0_Ret_In_Address ()
	{
		winx64_struct5 t_winx64_struct5 = mono_test_Winx64_struct5_ret ();
		if (t_winx64_struct5.a != 4)
			return 101;
		if (t_winx64_struct5.b != 5)
			return 102;
		if (t_winx64_struct5.c != 6)
			return 103;

		t_winx64_struct5 = mono_test_Winx64_struct5_ret6_args (0x1, 0x4, 0x2, 0x8, 0x30);
		if (t_winx64_struct5.a != 0x5)
			return 201;
		if (t_winx64_struct5.b != 0xa)
			return 202;
		if (t_winx64_struct5.c != 0x30)
			return 203;

		return 0;
	}

	public static int test_0_In_Args_Values_In_Registers_and_Stack ()
	{
		int retCode;

		winx64_struct1 var1 = new winx64_struct1 (1);
		winx64_struct1 var2 = new winx64_struct1 (2);
		winx64_struct1 var3 = new winx64_struct1 (3);
		winx64_struct1 var4 = new winx64_struct1 (4);
		winx64_struct1 var5 = new winx64_struct1 (5);

		if ((retCode = mono_test_Winx64_structs_in2 (var1, var2, var3, var4, var5)) != 0)
			return 100 + retCode;

		return 0;
	}

	public static int test_0_In_Args_Values_In_Registers_with_Stack_and_On_Stack ()
	{
		int retCode;

		winx64_struct1 var1 = new winx64_struct1 (1);
		winx64_struct5 var2 = new winx64_struct5 (2, 3, 4);
		winx64_struct1 var3 = new winx64_struct1 (5);
		winx64_struct5 var4 = new winx64_struct5 (6, 7, 8);
		winx64_struct1 var5 = new winx64_struct1 (9);
		winx64_struct5 var6 = new winx64_struct5 (10, 11, 12);

		if ((retCode = mono_test_Winx64_structs_in3 (var1, var2, var3, var4, var5, var6)) != 0)
			return 100 + retCode;

		return 0;
	}

	public static int test_0_In_Args_Value_On_Stack_ADDR_In_RCX ()
	{
		int retCode;

		winx64_struct5 t_winx64_struct5 = new winx64_struct5 (4, 5, 6);
		t_winx64_struct5.a = 4;
		t_winx64_struct5.b = 5;
		t_winx64_struct5.c = 6;

		if ((retCode = mono_test_Winx64_struct5_in (t_winx64_struct5)) != 0)
			return 100 + retCode;

		winx64_struct6 t_winx64_struct6 = new winx64_struct6 (new winx64_struct1 (4), 5, 6);		

		if ((retCode = mono_test_Winx64_struct6_in (t_winx64_struct6)) != 0)
			return 200 + retCode;

		return 0;
	}

	public static int test_0_In_Args_Value_In_RCX_Managed ()
	{
		int retCode;

		managed_struct1_delegate s1Del = new managed_struct1_delegate (managed_struct1_test);

		if ((retCode = mono_test_managed_Winx64_struct1_in (s1Del)) != 0)
				return 100 + retCode;

		return 0;
	}

	public static int test_0_In_Args_Value_On_Stack_ADDR_In_RCX_Managed ()
	{
		int retCode;

		managed_struct5_delegate s1Del = new managed_struct5_delegate (managed_struct5_test);

		if ((retCode = mono_test_managed_Winx64_struct5_in (s1Del)) != 0)
			return 100 + retCode;

		return 0;
	}

	public static int test_0_In_Args_Values_In_Registers_with_Stack_and_On_Stack_Managed ()
	{
		int retCode;

		managed_struct1_struct5_delegate s1Del = 
			new managed_struct1_struct5_delegate (managed_struct1_struct5_test);

		if ((retCode = mono_test_managed_Winx64_struct1_struct5_in (s1Del)) != 0)
			return 100 + retCode;

		return 0;
	}

	public static int test_0_Ret_In_RAX_managed ()
	{
		int retCode;

		mono_test_Winx64_struct1_ret_delegate s1Del =
			new mono_test_Winx64_struct1_ret_delegate (mono_test_Winx64_struct1_ret_test);

		if ((retCode = mono_test_Winx64_struct1_ret_managed (s1Del)) != 0)
			return 100 + retCode;

		return 0;
	}

	public static int test_0_Ret_In_Address_managed ()
	{
		int retCode;

		mono_test_Winx64_struct5_ret_delegate s1Del =
			new mono_test_Winx64_struct5_ret_delegate (mono_test_Winx64_struct5_ret_test);

		if ((retCode = mono_test_Winx64_struct5_ret_managed (s1Del)) != 0)
			return 100 + retCode;

		return 0;
	}

	public static int test_0_Value_On_Stack_Local_Copy_Managed ()
	{
		var vector3Pair = new winx64_vector3PairStruct
		{
			first = new winx64_vector3Struct (1, 2, 3)
		};

		var local2 = new winx64_vector3Struct (1, 1, 1);
		var local1 = vector3Pair.first;

		vector3Pair.first = winx64_vector3Struct.Add (local1, local2);
		vector3Pair.second = winx64_vector3Struct.Add (local1, local2);

		return (vector3Pair.second.x == 2 && vector3Pair.second.y == 3 && vector3Pair.second.z == 4) ? 0 : 1;
	}

	[MonoPInvokeCallback (typeof (managed_struct1_delegate))]
	public static int managed_struct1_test (winx64_struct1 var)
	{
		if (var.a != 5)
			return 1;

		return 0;
	}

	[MonoPInvokeCallback (typeof (managed_struct5_delegate))]
	public static int managed_struct5_test (winx64_struct5 var)
	{
		if (var.a != 5)
			return 1;
		if (var.b != 0x10)
			return 2;
		if (var.c != 0x99)
			return 3;

		return 0;
	}

	[MonoPInvokeCallback (typeof (managed_struct1_struct5_delegate))]
	public static int managed_struct1_struct5_test (winx64_struct1 var1, winx64_struct5 var2,
							winx64_struct1 var3, winx64_struct5 var4,
							winx64_struct1 var5, winx64_struct5 var6)
	{
		if (var1.a != 1 || var3.a != 5)
			return 1;
		if (var2.a != 2 || var2.b != 3 || var2.c != 4 ||
		    var4.a != 6 || var4.b != 7 || var4.c != 8)
			return 2;
		if (var5.a != 9)
			return 3;
		if (var6.a != 10 || var6.b != 11 || var6.c != 12)
			return 4;

		return 0;
	}

	[MonoPInvokeCallback (typeof (mono_test_Winx64_struct1_ret_delegate))]
	public static winx64_struct1 mono_test_Winx64_struct1_ret_test ()
	{
		return new winx64_struct1 (0x45);
	}

	[MonoPInvokeCallback (typeof (mono_test_Winx64_struct5_ret_delegate))]
	public static winx64_struct5 mono_test_Winx64_struct5_ret_test ()
	{
		return new winx64_struct5 (0x12, 0x34, 0x56);
	}
	
}
