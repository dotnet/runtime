//
// pinvoke3.cs:
//
//  Tests for native->managed marshalling
//

using System;
using System.Runtime.InteropServices;

public class Tests {

	[StructLayout (LayoutKind.Sequential)]
	public struct SimpleStruct {
		public bool a;
		public bool b;
		public bool c;
		public string d;
	}

	[StructLayout (LayoutKind.Sequential)]
	public class SimpleClass {
		public bool a;
		public bool b;
		public bool c;
		public string d;
	}

	public static SimpleStruct delegate_test_struct (SimpleStruct ss)
	{
		SimpleStruct res;

		res.a = !ss.a;
		res.b = !ss.b;
		res.c = !ss.c;
		res.d = ss.d + "-RES";

		return res;
	}

	public static int delegate_test_struct_byref (int a, ref SimpleStruct ss, int b)
	{
		if (a == 1 && b == 2 && ss.a && !ss.b && ss.c && ss.d == "TEST2")
			return 0;

		return 1;
	}

	public static SimpleClass delegate_test_class (SimpleClass ss)
	{
		if (ss == null)
			return null;

		if (! (!ss.a && ss.b && !ss.c && ss.d == "TEST"))
			return null;

		SimpleClass res = ss;

		return res;
	}

	public static int delegate_test_class_byref (ref SimpleClass ss)
	{
		if (ss == null)
			return -1;

		if (!ss.a && ss.b && !ss.c && ss.d == "TEST") {
			ss.a = true;
			ss.b = false;
			ss.c = true;
			ss.d = "RES";

			return 0;
		}

		return 1;
	}

	public static int delegate_test_class_out (out SimpleClass ss)
	{
		ss = new SimpleClass ();
		ss.a = true;
		ss.b = false;
		ss.c = true;
		ss.d = "RES";

		return 0;
	}

	[DllImport ("libtest", EntryPoint="mono_test_ref_vtype")]
	public static extern int mono_test_ref_vtype (int a, ref SimpleStruct ss, int b, TestDelegate d);
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate2")]
	public static extern int mono_test_marshal_delegate2 (SimpleDelegate2 d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate4")]
	public static extern int mono_test_marshal_delegate4 (SimpleDelegate4 d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate5")]
	public static extern int mono_test_marshal_delegate5 (SimpleDelegate5 d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate6")]
	public static extern int mono_test_marshal_delegate6 (SimpleDelegate5 d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate7")]
	public static extern int mono_test_marshal_delegate7 (SimpleDelegate7 d);

	public delegate int TestDelegate (int a, ref SimpleStruct ss, int b);

	public delegate SimpleStruct SimpleDelegate2 (SimpleStruct ss);

	public delegate SimpleClass SimpleDelegate4 (SimpleClass ss);

	public delegate int SimpleDelegate5 (ref SimpleClass ss);

	public delegate int SimpleDelegate7 (out SimpleClass ss);

	public static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	/* Test structures as arguments and return values of delegates */
	static int test_0_marshal_struct_delegate () {
		SimpleDelegate2 d = new SimpleDelegate2 (delegate_test_struct);

		return mono_test_marshal_delegate2 (d);
	}

	/* Test structures as byref arguments of delegates */
	static int test_0_marshal_byref_struct_delegate () {
		SimpleStruct ss = new SimpleStruct ();
		TestDelegate d = new TestDelegate (delegate_test_struct_byref);
		
		ss.b = true;
		ss.d = "TEST1";

		if (mono_test_ref_vtype (1, ref ss, 2, d) != 0)
			return 1;
		
		if (! (ss.a && !ss.b && ss.c && ss.d == "TEST2"))
			return 2;
		
		return 0;
	}

	/* Test classes as arguments and return values of delegates */
	static int test_0_marshal_class_delegate () {
		SimpleDelegate4 d = new SimpleDelegate4 (delegate_test_class);

		return mono_test_marshal_delegate4 (d);
	}

	/* Test classes as byref arguments of delegates */
	static int test_0_marshal_byref_class_delegate () {
		SimpleDelegate5 d = new SimpleDelegate5 (delegate_test_class_byref);

		return mono_test_marshal_delegate5 (d);
	}

	/* Test classes as out arguments of delegates */
	static int test_0_marshal_out_class_delegate () {
		SimpleDelegate7 d = new SimpleDelegate7 (delegate_test_class_out);

		return mono_test_marshal_delegate7 (d);
	}

	/* Test that the delegate wrapper correctly catches null byref arguments */
	static int test_0_marshal_byref_class_delegate_null () {
		SimpleDelegate5 d = new SimpleDelegate5 (delegate_test_class_byref);
		
		try {
			mono_test_marshal_delegate6 (d);
			return 1;
		}
		catch (ArgumentNullException ex) {
			return 0;
		}
	}
}
