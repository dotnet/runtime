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

public class Test
{
	[DllImport ("libtest.so")]
	public static extern int mono_union_test_1 (cs a);

	[DllImport ("libtest.so")]
	public static extern int mono_return_int (int a);

	[DllImport ("libtest.so", EntryPoint="mono_return_int")]
	public static extern int mono_return_int_ss (ss a);

	[DllImport ("libtest.so", EntryPoint="mono_return_int")]
	public static extern int mono_return_int_su (su a);

        static int Main()
        {
		if (mono_return_int (5) != 5)
			return 1;

		ss s1;
		s1.i1 = 4;
		if (mono_return_int_ss (s1) != 4)
			return 2;
		
		su s2;
		s2.i1 = 2;
		s2.i2 = 3;
		if (mono_return_int_su (s2) != 3)
			return 3;
		
		s2.i1 = 2;
		if (mono_return_int_su (s2) != 2)
			return 4;


		cs s3;
		s3.b1 = false;
		s3.i1 = 12;
		s3.u1.i1 = 2;
		s3.u1.i2 = 1;
		
		if (mono_union_test_1 (s3) != 13)
			return 5;

		s3.u1.i1 = 2;
		if (mono_union_test_1 (s3) != 14)
			return 6;

		s3.b1 = true;
		if (mono_union_test_1 (s3) != 15)
			return 7;
		
		return 0;
        }
}

