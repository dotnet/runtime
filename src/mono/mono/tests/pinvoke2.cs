using System;
using System.Text;
using System.Runtime.InteropServices;

public class Tests {

	public static int delegate_test (int a)
	{
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

	[StructLayout (LayoutKind.Sequential, Size=0)]
	public struct EmptyStruct {
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

	[StructLayout (LayoutKind.Sequential)]
	public class SimpleClass {
		public bool a;
		public bool b;
		public bool c;
		public string d;
		public byte e;
		public double f;
		public byte g;
		public long h;
	}

	[StructLayout (LayoutKind.Sequential)]
	public class EmptyClass {
	}

	[DllImport ("libnot-found", EntryPoint="not_found")]
	public static extern int mono_library_not_found ();

	[DllImport ("libtest", EntryPoint="not_found")]
	public static extern int mono_entry_point_not_found ();

	[DllImport ("libtest.dll", EntryPoint="mono_test_marshal_char")]
	public static extern int mono_test_marshal_char_2 (char a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_char")]
	public static extern int mono_test_marshal_char (char a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_byref")]
	public static extern int mono_test_marshal_bool_byref (int a, ref bool b, int c);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array")]
	public static extern int mono_test_marshal_array (int [] a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_inout_array")]
	public static extern int mono_test_marshal_inout_array ([In, Out] int [] a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_inout_nonblittable_array", CharSet = CharSet.Unicode)]
	public static extern int mono_test_marshal_inout_nonblittable_array ([In, Out] char [] a1);
	
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

	[DllImport ("libtest", EntryPoint="mono_test_empty_struct")]
	public static extern int mono_test_empty_struct (int a, EmptyStruct es, int b);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct_array")]
	public static extern int mono_test_marshal_struct_array (SimpleStruct2[] ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_class")]
	public static extern SimpleClass mono_test_marshal_class (int i, int j, int k, SimpleClass ss, int l);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_byref_class")]
	public static extern int mono_test_marshal_byref_class (ref SimpleClass ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate")]
	public static extern int mono_test_marshal_delegate (SimpleDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_return_vtype")]
	public static extern SimpleStruct mono_test_return_vtype (IntPtr i);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder")]
	public static extern void mono_test_marshal_stringbuilder (StringBuilder sb, int len);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_unicode", CharSet=CharSet.Unicode)]
	public static extern void mono_test_marshal_stringbuilder_unicode (StringBuilder sb, int len);

	[DllImport ("libtest", EntryPoint="mono_test_last_error", SetLastError=true)]
	public static extern void mono_test_last_error (int err);

	public delegate int SimpleDelegate (int a);

	public static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	static int test_0_marshal_char () {
		return mono_test_marshal_char ('a');
	}

	static int test_1225_marshal_array () {
		int [] a1 = new int [50];
		for (int i = 0; i < 50; i++)
			a1 [i] = i;

		return mono_test_marshal_array (a1);
	}

	static int test_1225_marshal_inout_array () {
		int [] a1 = new int [50];
		for (int i = 0; i < 50; i++)
			a1 [i] = i;

		int res = mono_test_marshal_inout_array (a1);

		for (int i = 0; i < 50; i++)
			if (a1 [i] != 50 - i) {
				Console.WriteLine ("X: " + i + " " + a1 [i]);
				return 2;
			}

		return res;
	}

	static int test_0_marshal_inout_nonblittable_array () {
		char [] a1 = new char [10];
		for (int i = 0; i < 10; i++)
			a1 [i] = "Hello, World" [i];

		int res = mono_test_marshal_inout_nonblittable_array (a1);

		for (int i = 0; i < 10; i++)
			if (a1 [i] != 'F')
				return 2;

		return res;
	}

	static int test_0_marshal_struct () {
		SimpleStruct ss = new  SimpleStruct ();
		ss.b = true;
		ss.d = "TEST";
		
		return mono_test_marshal_struct (ss);
	}

	static int test_0_marshal_struct2 () {
		SimpleStruct2 ss2 = new  SimpleStruct2 ();
		ss2.b = true;
		ss2.d = "TEST";
		ss2.e = 99;
		ss2.f = 1.5;
		ss2.g = 42;
		ss2.h = 123L;

		return mono_test_marshal_struct2 (ss2);
	}

	static int test_0_marshal_struct3 () {
		SimpleStruct2 ss2 = new  SimpleStruct2 ();
		ss2.b = true;
		ss2.d = "TEST";
		ss2.e = 99;
		ss2.f = 1.5;
		ss2.g = 42;
		ss2.h = 123L;

		return mono_test_marshal_struct2_2 (10, 11, 12, ss2);
	}

	static int test_0_marshal_empty_struct () {
		EmptyStruct es = new EmptyStruct ();

		if (mono_test_empty_struct (1, es, 2) != 0)
			return 1;
		
		return 0;
	}

	static int test_0_marshal_struct_array () {
		SimpleStruct2[] ss_arr = new SimpleStruct2 [2];

		SimpleStruct2 ss2 = new SimpleStruct2 ();
		ss2.b = true;
		ss2.d = "TEST";
		ss2.e = 99;
		ss2.f = 1.5;
		ss2.g = 42;
		ss2.h = 123L;

		ss_arr [0] = ss2;

		ss2.b = false;
		ss2.d = "TEST2";
		ss2.e = 100;
		ss2.f = 2.5;
		ss2.g = 43;
		ss2.h = 124L;

		ss_arr [1] = ss2;

		return mono_test_marshal_struct_array (ss_arr);
	}

	/* Test classes as arguments and return values */
	static int test_0_marshal_class () {
		SimpleClass ss = new  SimpleClass ();
		ss.b = true;
		ss.d = "TEST";
		ss.e = 99;
		ss.f = 1.5;
		ss.g = 42;
		ss.h = 123L;

		SimpleClass res = mono_test_marshal_class (10, 11, 12, ss, 14);
		if (res == null)
			return 1;
		if  (! (res.a == ss.a && res.b == ss.b && res.c == ss.c && 
				res.d == ss.d && res.e == ss.e && res.f == ss.f &&
				res.g == ss.g && res.h == ss.h))
			return 2;

		/* Test null arguments and results */
		res = mono_test_marshal_class (10, 11, 12, null, 14);
		if (res != null)
			return 3;

		return 0;
	}

	static int test_0_marshal_byref_class () {
		SimpleClass ss = new  SimpleClass ();
		ss.b = true;
		ss.d = "TEST";
		ss.e = 99;
		ss.f = 1.5;
		ss.g = 42;
		ss.h = 123L;

		int res = mono_test_marshal_byref_class (ref ss);
		if (ss.d != "TEST-RES")
			return 1;

		return 0;
	}

	static int test_0_marshal_delegate () {
		SimpleDelegate d = new SimpleDelegate (delegate_test);

		return mono_test_marshal_delegate (d);
	}

	static int test_0_marshal_point () {
		Point pt = new Point();
		pt.x = 1.25;
		pt.y = 3.5;
		
		return mono_test_marshal_point(pt);
	}

	static int test_0_marshal_mixed_point () {
		MixedPoint mpt = new MixedPoint();
		mpt.x = 5;
		mpt.y = 6.75;
		
		return mono_test_marshal_mixed_point(mpt);
	}

	static int test_0_marshal_bool_byref () {
		bool b = true;
		if (mono_test_marshal_bool_byref (99, ref b, 100) != 1)
			return 1;
		b = false;
		if (mono_test_marshal_bool_byref (99, ref b, 100) != 0)
			return 12;
		if (b != true)
			return 13;

		return 0;
	}

	static int test_0_return_vtype () {
		SimpleStruct ss = mono_test_return_vtype (new IntPtr (5));

		if (!ss.a && ss.b && !ss.c && ss.d == "TEST")
			return 0;
		
		return 1;
	}

	static int test_0_marshal_stringbuilder () {
		StringBuilder sb = new StringBuilder(255);
		mono_test_marshal_stringbuilder (sb, sb.Capacity);
		String res = sb.ToString();

		if (res != "This is my message.  Isn't it nice?")
			return 1;  
		
		return 0;
	}

	static int test_0_marshal_stringbuilder_unicode () {
		StringBuilder sb = new StringBuilder(255);
		mono_test_marshal_stringbuilder_unicode (sb, sb.Capacity);
		String res = sb.ToString();

		if (res != "This is my message.  Isn't it nice?")
			return 1;  
		
		return 0;
	}

	static int test_0_last_error () {
		mono_test_last_error (5);
		if (Marshal.GetLastWin32Error () == 5)
			return 0;
		else
			return 1;
	}

	static int test_0_library_not_found () {

		try {
			mono_entry_point_not_found ();
			return 1;
		}
		catch (EntryPointNotFoundException) {
		}

		return 0;
	}

	static int test_0_entry_point_not_found () {

		try {
			mono_library_not_found ();
			return 1;
		}
		catch (DllNotFoundException) {
		}

		return 0;
	}

	/* Check that the runtime trims .dll from the library name */
	static int test_0_trim_dll_from_name () {

		mono_test_marshal_char_2 ('A');

		return 0;
	}
}
