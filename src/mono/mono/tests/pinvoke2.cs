using System;
using System.Runtime.InteropServices;

public class Test {

	public static int delegate_test (int a)
	{
		Console.WriteLine ("Delegate: " + a);
		
		if (a == 2)
			return 0;

		return 1;
	}
	
	[StructLayout (LayoutKind.Sequential)]
	public struct SimpleStruct {
		public bool a;
		public bool b;
		public bool c;
		public string d;
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct SimpleStruct2 {
		public bool a;
		public bool b;
		public bool c;
		public string d;
		public byte e;
		public double f;
		public byte g;
		public long h;
	}

	/* sparcv9 has complex conventions when passing structs with doubles in them 
	   by value, some simple tests for them */
	[StructLayout (LayoutKind.Sequential)]
	public struct Point {
		public double x;
		public double y;
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct MixedPoint {
		public int x;
		public double y;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_char")]
	public static extern int mono_test_marshal_char (char a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array")]
	public static extern int mono_test_marshal_array (int [] a1);
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct")]
	public static extern int mono_test_marshal_struct (SimpleStruct ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct2")]
	public static extern int mono_test_marshal_struct2 (SimpleStruct2 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct2_2")]
	public static extern int mono_test_marshal_struct2_2 (int i, int j, int k, SimpleStruct2 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_point")]
	public static extern int mono_test_marshal_point (Point p);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_mixed_point")]
	public static extern int mono_test_marshal_mixed_point (MixedPoint p);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct_array")]
	public static extern int mono_test_marshal_struct_array (SimpleStruct2[] ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate")]
	public static extern int mono_test_marshal_delegate (SimpleDelegate d);

	public delegate int SimpleDelegate (int a);

	public static int Main () {
		if (mono_test_marshal_char ('a') != 0)
			return 1;

		int [] a1 = new int [50];
		for (int i = 0; i < 50; i++)
			a1 [i] = i;

		if (mono_test_marshal_array (a1) != 1225)
			return 2;

		SimpleStruct ss = new  SimpleStruct ();
		ss.b = true;
		ss.d = "TEST";
		if (mono_test_marshal_struct (ss) != 0)
			return 3;

		SimpleStruct2 ss2 = new  SimpleStruct2 ();
		ss2.b = true;
		ss2.d = "TEST";
		ss2.e = 99;
		ss2.f = 1.5;
		ss2.g = 42;
		ss2.h = 123L;

		if (mono_test_marshal_struct2 (ss2) != 0)
			return 4;

		SimpleStruct2[] ss_arr = new SimpleStruct2 [2];
		ss_arr [0] = ss2;
		ss_arr [1] = ss2;

		if (mono_test_marshal_struct_array (ss_arr) != 0)
			return 5;
		
		if (mono_test_marshal_struct2_2 (10, 11, 12, ss2) != 0)
			return 6;
		
		SimpleDelegate d = new SimpleDelegate (delegate_test);

		if (mono_test_marshal_delegate (d) != 0)
			return 7;

		Point pt = new Point();
		pt.x = 1.25;
		pt.y = 3.5;
		if (mono_test_marshal_point(pt) != 0)
			return 8;

		MixedPoint mpt = new MixedPoint();
		mpt.x = 5;
		mpt.y = 6.75;
		if (mono_test_marshal_mixed_point(mpt) != 0)
			return 9;

		return 0;
	}
}
