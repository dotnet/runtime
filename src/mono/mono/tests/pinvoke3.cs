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
		[MarshalAs(UnmanagedType.LPWStr)]
		public string d2;
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
		res.d2 = ss.d2 + "-RES";

		return res;
	}

	public static int delegate_test_struct_byref (int a, ref SimpleStruct ss, int b)
	{
		if (a == 1 && b == 2 && ss.a && !ss.b && ss.c && ss.d == "TEST2") {
			ss.a = true;
			ss.b = true;
			ss.c = true;
			ss.d = "TEST3";
			return 0;
		}

		return 1;
	}

	public static int delegate_test_struct_out (int a, out SimpleStruct ss, int b)
	{
		ss.a = true;
		ss.b = true;
		ss.c = true;
		ss.d = "TEST3";
		ss.d2 = "TEST4";

		return 0;
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

	public static int delegate_test_primitive_byref (ref int i)
	{
		if (i != 1)
			return 1;
		
		i = 2;
		return 0;
	}

	public static int delegate_test_string_marshalling (string s)
	{
		return s == "ABC" ? 0 : 1;
	}

	[DllImport ("libtest", EntryPoint="mono_test_ref_vtype")]
	public static extern int mono_test_ref_vtype (int a, ref SimpleStruct ss, int b, TestDelegate d);

	public delegate int OutStructDelegate (int a, out SimpleStruct ss, int b);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_struct")]
	public static extern int mono_test_marshal_out_struct (int a, out SimpleStruct ss, int b, OutStructDelegate d);
	
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

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate8", CharSet=CharSet.Unicode)]
	public static extern int mono_test_marshal_delegate8 (SimpleDelegate8 d, string s);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate9")]
	public static extern int mono_test_marshal_delegate9 (SimpleDelegate9 d, return_int_delegate d2);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate10")]
	public static extern int mono_test_marshal_delegate10 (SimpleDelegate9 d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_primitive_byref_delegate")]
	public static extern int mono_test_marshal_primitive_byref_delegate (PrimitiveByrefDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_delegate_delegate")]
	public static extern int mono_test_marshal_return_delegate_delegate (ReturnDelegateDelegate d);

	public delegate int TestDelegate (int a, ref SimpleStruct ss, int b);

	public delegate SimpleStruct SimpleDelegate2 (SimpleStruct ss);

	public delegate SimpleClass SimpleDelegate4 (SimpleClass ss);

	public delegate int SimpleDelegate5 (ref SimpleClass ss);

	public delegate int SimpleDelegate7 (out SimpleClass ss);

	public delegate int SimpleDelegate8 ([MarshalAs (UnmanagedType.LPWStr)] string s1);

	public delegate int return_int_delegate (int i);

	public delegate int SimpleDelegate9 (return_int_delegate del);

	public delegate int PrimitiveByrefDelegate (ref int i);

	public delegate return_int_delegate ReturnDelegateDelegate ();

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

		if (! (ss.a && ss.b && ss.c && ss.d == "TEST3"))
			return 2;
		
		return 0;
	}

	/* Test structures as out arguments of delegates */
	static int test_0_marshal_out_struct_delegate () {
		SimpleStruct ss = new SimpleStruct ();
		OutStructDelegate d = new OutStructDelegate (delegate_test_struct_out);

		return mono_test_marshal_out_struct (1, out ss, 2, d);
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

	/* Test string marshalling with delegates */
	static int test_0_marshal_string_delegate () {
		SimpleDelegate8 d = new SimpleDelegate8 (delegate_test_string_marshalling);

		return mono_test_marshal_delegate8 (d, "ABC");
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

	static int return_self (int i) {
		return i;
	}

	static int call_int_delegate (return_int_delegate d) {
		return d (55);
	}

	static int test_55_marshal_delegate_delegate () {
		SimpleDelegate9 d = new SimpleDelegate9 (call_int_delegate);

		return mono_test_marshal_delegate9 (d, new return_int_delegate (return_self));
	}

	static int test_0_marshal_primitive_byref_delegate () {
		PrimitiveByrefDelegate d = new PrimitiveByrefDelegate (delegate_test_primitive_byref);

		return mono_test_marshal_primitive_byref_delegate (d);
	}

	public static return_int_delegate return_delegate () {
		return new return_int_delegate (return_self);
	}

	static int test_55_marshal_return_delegate_delegate () {
		return mono_test_marshal_return_delegate_delegate (new ReturnDelegateDelegate (return_delegate));
	}

	/* Passing and returning strings */

	public delegate String ReturnStringDelegate (String s);

	[DllImport ("libtest", EntryPoint="mono_test_return_string")]
	public static extern String mono_test_marshal_return_string_delegate (ReturnStringDelegate d);

	public static String managed_return_string (String s) {
		if (s != "TEST")
			return "";
		else
			return "12345";
	}

	static int test_0_marshal_return_string_delegate () {
		ReturnStringDelegate d = new ReturnStringDelegate (managed_return_string);
		String s = mono_test_marshal_return_string_delegate (d);

		return (s == "12345") ? 0 : 1;
	}

	/* Passing and returning enums */

	public enum FooEnum {
		Foo1,
		Foo2,
		Foo3
	};

	public delegate FooEnum ReturnEnumDelegate (FooEnum e);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_enum_delegate")]
	public static extern int mono_test_marshal_return_enum_delegate (ReturnEnumDelegate d);

	public static FooEnum managed_return_enum (FooEnum e) {
		return (FooEnum)((int)e + 1);
	}

	static int test_0_marshal_return_enum_delegate () {
		ReturnEnumDelegate d = new ReturnEnumDelegate (managed_return_enum);
		FooEnum e = (FooEnum)mono_test_marshal_return_enum_delegate (d);

		return e == FooEnum.Foo3 ? 0 : 1;
	}

	/* Passing and returning blittable structs */

	[StructLayout (LayoutKind.Sequential)]
	public struct BlittableStruct {
		public int a, b, c;
		public long d;
	}

	public static BlittableStruct delegate_test_blittable_struct (BlittableStruct ss)
	{
		BlittableStruct res;

		res.a = -ss.a;
		res.b = -ss.b;
		res.c = -ss.c;
		res.d = -ss.d;

		return res;
	}

	public delegate BlittableStruct SimpleDelegate10 (BlittableStruct ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_blittable_struct_delegate")]
	public static extern int mono_test_marshal_blittable_struct_delegate (SimpleDelegate10 d);

	static int test_0_marshal_blittable_struct_delegate () {
		return mono_test_marshal_blittable_struct_delegate (new SimpleDelegate10 (delegate_test_blittable_struct));
	}

	/*
	 * Passing and returning small structs
	 */

	/* TEST 1: 4 byte long INTEGER struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct1 {
		public int i;
	}

	public static SmallStruct1 delegate_test_struct (SmallStruct1 ss) {
		SmallStruct1 res;

		res.i = -ss.i;
		
		return res;
	}

	public delegate SmallStruct1 SmallStructDelegate1 (SmallStruct1 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate1")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate1 d);

	static int test_0_marshal_small_struct_delegate1 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate1 (delegate_test_struct));
	}

	/* TEST 2: 2+2 byte long INTEGER struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct2 {
		public short i, j;
	}

	public static SmallStruct2 delegate_test_struct (SmallStruct2 ss) {
		SmallStruct2 res;

		res.i = (short)-ss.i;
		res.j = (short)-ss.j;
		
		return res;
	}

	public delegate SmallStruct2 SmallStructDelegate2 (SmallStruct2 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate2")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate2 d);

	static int test_0_marshal_small_struct_delegate2 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate2 (delegate_test_struct));
	}

	/* TEST 3: 2+1 byte long INTEGER struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct3 {
		public short i;
		public byte j;
	}

	public static SmallStruct3 delegate_test_struct (SmallStruct3 ss) {
		SmallStruct3 res;

		res.i = (short)-ss.i;
		res.j = (byte)-ss.j;
		
		return res;
	}

	public delegate SmallStruct3 SmallStructDelegate3 (SmallStruct3 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate3")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate3 d);

	static int test_0_marshal_small_struct_delegate3 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate3 (delegate_test_struct));
	}

	/* TEST 4: 2 byte long INTEGER struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct4 {
		public short i;
	}

	public static SmallStruct4 delegate_test_struct (SmallStruct4 ss) {
		SmallStruct4 res;

		res.i = (short)-ss.i;
		
		return res;
	}

	public delegate SmallStruct4 SmallStructDelegate4 (SmallStruct4 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate4")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate4 d);

	static int test_0_marshal_small_struct_delegate4 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate4 (delegate_test_struct));
	}

	/* TEST 5: 8 byte long INTEGER struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct5 {
		public long l;
	}

	public static SmallStruct5 delegate_test_struct (SmallStruct5 ss) {
		SmallStruct5 res;

		res.l = -ss.l;
		
		return res;
	}

	public delegate SmallStruct5 SmallStructDelegate5 (SmallStruct5 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate5")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate5 d);

	static int test_0_marshal_small_struct_delegate5 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate5 (delegate_test_struct));
	}

	/* TEST 6: 4+4 byte long INTEGER struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct6 {
		public int i, j;
	}

	public static SmallStruct6 delegate_test_struct (SmallStruct6 ss) {
		SmallStruct6 res;

		res.i = -ss.i;
		res.j = -ss.j;
		
		return res;
	}

	public delegate SmallStruct6 SmallStructDelegate6 (SmallStruct6 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate6")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate6 d);

	static int test_0_marshal_small_struct_delegate6 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate6 (delegate_test_struct));
	}

	/* TEST 7: 4+2 byte long INTEGER struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct7 {
		public int i;
		public short j;
	}

	public static SmallStruct7 delegate_test_struct (SmallStruct7 ss) {
		SmallStruct7 res;

		res.i = -ss.i;
		res.j = (short)-ss.j;
		
		return res;
	}

	public delegate SmallStruct7 SmallStructDelegate7 (SmallStruct7 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate7")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate7 d);

	static int test_0_marshal_small_struct_delegate7 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate7 (delegate_test_struct));
	}


}
