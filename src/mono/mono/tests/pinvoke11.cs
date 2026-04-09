using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct ss
{
        public int i1;
}

[StructLayout(LayoutKind.Explicit)]
public struct su
{
        [FieldOffset(0)] public int i1;
        [FieldOffset(0)] public int i2;
}

[StructLayout(LayoutKind.Sequential)]
public struct cs
{
	public bool b1;
	public int i1;
	public su u1;
}

/* various small structs for testing struct-by-value where they are handled specially 
   on some platforms.
*/
[StructLayout(LayoutKind.Sequential)]
public struct sc1
{
	public byte c0;
}

[StructLayout(LayoutKind.Sequential)]
public struct sc3
{
	public byte c0;
	public byte c1;
	public byte c2;
}

[StructLayout(LayoutKind.Sequential)]
public struct sc5
{
	public byte c0;
	public byte c1;
	public byte c2;
	public byte c3;
	public byte c4;
}

public struct FI {
	public float f1;
	public float f2;
	public float f3;
}

public struct NestedFloat {
	public FI fi;
	public float f4;
}

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Rectangle
{
	public int X;
	public int Y;
	public int Width;
	public int Height;

	public Rectangle(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}
}

[Serializable]
public struct Scalar4 {
	public double Val0;
	public double Val1;
	public double Val2;
	public double Val3;

	public Scalar4 (double v0, double v1, double v2, double v3) {
		Val0 = v0;
		Val1 = v1;
		Val2 = v2;
		Val3 = v3;
	}
}

public class Test
{
	[DllImport ("libtest")]
	public static extern int mono_union_test_1 (cs a);

	[DllImport ("libtest")]
	public static extern int mono_return_int (int a);

	[DllImport ("libtest", EntryPoint="mono_return_int_ss")]
	public static extern int mono_return_int_ss (ss a);

	[DllImport ("libtest", EntryPoint="mono_return_ss")]
	public static extern ss mono_return_ss (ss a);

	[DllImport ("libtest", EntryPoint="mono_return_sc1")]
	public static extern sc1 mono_return_sc1 (sc1 a);

	[DllImport ("libtest", EntryPoint="mono_return_sc3")]
	public static extern sc3 mono_return_sc3 (sc3 a);

	[DllImport ("libtest", EntryPoint="mono_return_sc5")]
	public static extern sc5 mono_return_sc5 (sc5 a);

	[DllImport ("libtest", EntryPoint="mono_return_int_su")]
	public static extern int mono_return_int_su (su a);

	[DllImport ("libtest", EntryPoint="mono_return_nested_float")]
	public static extern NestedFloat mono_return_nested_float ();

	[DllImport("libtest", EntryPoint="mono_return_struct_4_double")]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string mono_return_struct_4_double (IntPtr ptr, Rectangle rect, Scalar4 sc4, int a, int b, int c);

        static int Main()
        {
		if (mono_return_int (5) != 5)
			return 1;

		ss s1;
		s1.i1 = 4;
		if (mono_return_int_ss (s1) != 4)
			return 2;

		s1 = mono_return_ss (s1);
		if (s1.i1 != 5)
			return 3;
		
		su s2;
		s2.i1 = 2;
		s2.i2 = 3;
		if (mono_return_int_su (s2) != 3)
			return 4;
		
		s2.i1 = 2;
		if (mono_return_int_su (s2) != 2)
			return 5;


		cs s3;
		s3.b1 = false;
		s3.i1 = 12;
		s3.u1.i1 = 2;
		s3.u1.i2 = 1;
		
		if (mono_union_test_1 (s3) != 13)
			return 6;

		s3.u1.i1 = 2;
		if (mono_union_test_1 (s3) != 14)
			return 7;

		s3.b1 = true;
		if (mono_union_test_1 (s3) != 15)
			return 8;

		sc1 s4;
		s4.c0 = 3;
		s4 = mono_return_sc1(s4);
		if (s4.c0 != 4)
			return 9;

		sc3 s5;
		s5.c0 = 4;
		s5.c1 = 5;
		s5.c2 = 6;
		s5 = mono_return_sc3(s5);
		if (s5.c0 != 5 || s5.c1 != 7 || s5.c2 != 9)
			return 10;

		sc5 s6;
		s6.c0 = 4;
		s6.c1 = 5;
		s6.c2 = 6;
		s6.c3 = 7;
		s6.c4 = 8;
		s6 = mono_return_sc5(s6);
		if (s6.c0 != 5 || s6.c1 != 7 || s6.c2 != 9 || s6.c3 != 11 || s6.c4 != 13)
			return 11;

		var f = mono_return_nested_float ();
		if (f.fi.f1 != 1.0)
			return 12;

		Rectangle rect = new Rectangle (10, 10, 100, 20);
		Scalar4 sc4 = new Scalar4 (32, 64, 128, 256);
		var sc4_ret = mono_return_struct_4_double (IntPtr.Zero, rect, sc4, 0x1337, 0x1234, 0x9876);
		if (sc4_ret != "sc4 = {32.0, 64.0, 128.0, 256.0 }, a=1337, b=1234, c=9876\n") {
			Console.WriteLine ("sc4_ret = " + sc4_ret);
			return 13;
		}

		return 0;
        }
}

