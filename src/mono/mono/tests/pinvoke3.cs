//
// pinvoke3.cs:
//
//  Tests for native->managed marshalling
//

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

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

	public static int delegate_test_struct_in (int a, [In] ref SimpleStruct ss, int b)
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

	public static int delegate_test_string_builder_marshalling (StringBuilder s)
	{
		if (s == null)
			return 2;
		else
			return s.ToString () == "ABC" ? 0 : 1;
	}

	[DllImport ("libtest", EntryPoint="mono_test_ref_vtype")]
	public static extern int mono_test_ref_vtype (int a, ref SimpleStruct ss, int b, TestDelegate d);

	public delegate int OutStructDelegate (int a, out SimpleStruct ss, int b);

	public delegate int InStructDelegate (int a, [In] ref SimpleStruct ss, int b);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_struct")]
	public static extern int mono_test_marshal_out_struct (int a, out SimpleStruct ss, int b, OutStructDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_in_struct")]
	public static extern int mono_test_marshal_in_struct (int a, ref SimpleStruct ss, int b, InStructDelegate d);
	
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

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate8")]
	public static extern int mono_test_marshal_delegate11 (SimpleDelegate11 d, string s);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_primitive_byref_delegate")]
	public static extern int mono_test_marshal_primitive_byref_delegate (PrimitiveByrefDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_delegate_delegate")]
	public static extern int mono_test_marshal_return_delegate_delegate (ReturnDelegateDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate_ref_delegate")]
	public static extern int mono_test_marshal_delegate_ref_delegate (DelegateByrefDelegate del);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_virtual_delegate")]
	public static extern int mono_test_marshal_virtual_delegate (VirtualDelegate del);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_icall_delegate")]
	public static extern int mono_test_marshal_icall_delegate (IcallDelegate del);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_nullable_ret_delegate")]
	public static extern int mono_test_marshal_nullable_ret_delegate (NullableReturnDelegate del);

	public delegate string IcallDelegate (IntPtr p);

	public delegate int TestDelegate (int a, ref SimpleStruct ss, int b);

	public delegate SimpleStruct SimpleDelegate2 (SimpleStruct ss);

	public delegate SimpleClass SimpleDelegate4 (SimpleClass ss);

	public delegate int SimpleDelegate5 (ref SimpleClass ss);

	public delegate int SimpleDelegate7 (out SimpleClass ss);

	public delegate int SimpleDelegate8 ([MarshalAs (UnmanagedType.LPWStr)] string s1);

	public delegate int return_int_delegate (int i);

	public delegate int SimpleDelegate9 (return_int_delegate del);

	public delegate int SimpleDelegate11 (StringBuilder s1);

	public delegate int PrimitiveByrefDelegate (ref int i);

	public delegate return_int_delegate ReturnDelegateDelegate ();

	public delegate int DelegateByrefDelegate (ref return_int_delegate del);

	public delegate int VirtualDelegate (int i);

	public delegate Nullable<int> NullableReturnDelegate ();

	public static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	/* Test structures as arguments and return values of delegates */
	public static int test_0_marshal_struct_delegate () {
		SimpleDelegate2 d = new SimpleDelegate2 (delegate_test_struct);

		return mono_test_marshal_delegate2 (d);
	}

	/* Test structures as byref arguments of delegates */
	public static int test_0_marshal_byref_struct_delegate () {
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
	public static int test_0_marshal_out_struct_delegate () {
		SimpleStruct ss = new SimpleStruct ();
		OutStructDelegate d = new OutStructDelegate (delegate_test_struct_out);

		return mono_test_marshal_out_struct (1, out ss, 2, d);
	}

	/* Test structures as in arguments of delegates */
	public static int test_0_marshal_in_struct_delegate () {
		SimpleStruct ss = new SimpleStruct () { a = true, b = false, c = true, d = "TEST2" };
		InStructDelegate d = new InStructDelegate (delegate_test_struct_in);

		return mono_test_marshal_in_struct (1, ref ss, 2, d);
	}

	/* Test classes as arguments and return values of delegates */
	public static int test_0_marshal_class_delegate () {
		SimpleDelegate4 d = new SimpleDelegate4 (delegate_test_class);

		return mono_test_marshal_delegate4 (d);
	}

	/* Test classes as byref arguments of delegates */
	public static int test_0_marshal_byref_class_delegate () {
		SimpleDelegate5 d = new SimpleDelegate5 (delegate_test_class_byref);

		return mono_test_marshal_delegate5 (d);
	}

	/* Test classes as out arguments of delegates */
	public static int test_0_marshal_out_class_delegate () {
		SimpleDelegate7 d = new SimpleDelegate7 (delegate_test_class_out);

		return mono_test_marshal_delegate7 (d);
	}

	/* Test string marshalling with delegates */
	public static int test_0_marshal_string_delegate () {
		SimpleDelegate8 d = new SimpleDelegate8 (delegate_test_string_marshalling);

		return mono_test_marshal_delegate8 (d, "ABC");
	}

	/* Test string builder marshalling with delegates */
	public static int test_0_marshal_string_builder_delegate () {
		SimpleDelegate11 d = new SimpleDelegate11 (delegate_test_string_builder_marshalling);

		if (mono_test_marshal_delegate11 (d, null) != 2)
			return 2;

		return mono_test_marshal_delegate11 (d, "ABC");
	}

	/* Test that the delegate wrapper correctly catches null byref arguments */
	public static int test_0_marshal_byref_class_delegate_null () {
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

	public static int test_55_marshal_delegate_delegate () {
		SimpleDelegate9 d = new SimpleDelegate9 (call_int_delegate);

		return mono_test_marshal_delegate9 (d, new return_int_delegate (return_self));
	}

	public static int test_0_marshal_primitive_byref_delegate () {
		PrimitiveByrefDelegate d = new PrimitiveByrefDelegate (delegate_test_primitive_byref);

		return mono_test_marshal_primitive_byref_delegate (d);
	}

	public static return_int_delegate return_delegate () {
		return new return_int_delegate (return_self);
	}

	public static int test_55_marshal_return_delegate_delegate () {
		return mono_test_marshal_return_delegate_delegate (new ReturnDelegateDelegate (return_delegate));
	}

	public static int return_plus_1 (int i) {
		return i + 1;
	}

	public static int ref_delegate_delegate (ref return_int_delegate del) {
		del = return_plus_1;
		return 0;
	}

	public static int test_55_marshal_delegate_ref_delegate () {
		var del = new DelegateByrefDelegate (ref_delegate_delegate);
		return mono_test_marshal_delegate_ref_delegate (del);
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

	public static int test_0_marshal_return_string_delegate () {
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

	public static int test_0_marshal_return_enum_delegate () {
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

	public static int test_0_marshal_blittable_struct_delegate () {
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

	public static int test_0_marshal_small_struct_delegate1 () {
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

	public static int test_0_marshal_small_struct_delegate2 () {
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

	public static int test_0_marshal_small_struct_delegate3 () {
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

	public static int test_0_marshal_small_struct_delegate4 () {
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

	public static int test_0_marshal_small_struct_delegate5 () {
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

	public static int test_0_marshal_small_struct_delegate6 () {
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

	public static int test_0_marshal_small_struct_delegate7 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate7 (delegate_test_struct));
	}

	/* TEST 8: 4 byte long FLOAT struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct8 {
		public float i;
	}

	public static SmallStruct8 delegate_test_struct (SmallStruct8 ss) {
		SmallStruct8 res;

		res.i = -ss.i;
		
		return res;
	}

	public delegate SmallStruct8 SmallStructDelegate8 (SmallStruct8 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate8")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate8 d);

	public static int test_0_marshal_small_struct_delegate8 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate8 (delegate_test_struct));
	}

	/* TEST 9: 8 byte long FLOAT struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct9 {
		public double i;
	}

	public static SmallStruct9 delegate_test_struct (SmallStruct9 ss) {
		SmallStruct9 res;

		res.i = -ss.i;
		
		return res;
	}

	public delegate SmallStruct9 SmallStructDelegate9 (SmallStruct9 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate9")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate9 d);

	public static int test_0_marshal_small_struct_delegate9 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate9 (delegate_test_struct));
	}

	/* TEST 10: 4+4 byte long FLOAT struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct10 {
		public float i;
		public float j;
	}

	public static SmallStruct10 delegate_test_struct (SmallStruct10 ss) {
		SmallStruct10 res;

		res.i = -ss.i;
		res.j = -ss.j;
		
		return res;
	}

	public delegate SmallStruct10 SmallStructDelegate10 (SmallStruct10 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate10")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate10 d);

	public static int test_0_marshal_small_struct_delegate10 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate10 (delegate_test_struct));
	}

	/* TEST 11: 4+4 byte long MIXED struct */

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallStruct11 {
		public float i;
		public int j;
	}

	public static SmallStruct11 delegate_test_struct (SmallStruct11 ss) {
		SmallStruct11 res;

		res.i = -ss.i;
		res.j = -ss.j;
		
		return res;
	}

	public delegate SmallStruct11 SmallStructDelegate11 (SmallStruct11 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_small_struct_delegate11")]
	public static extern int mono_test_marshal_small_struct_delegate (SmallStructDelegate11 d);

	public static int test_0_marshal_small_struct_delegate11 () {
		return mono_test_marshal_small_struct_delegate (new SmallStructDelegate11 (delegate_test_struct));
	}

	/*
	 * Passing arrays
	 */
	public delegate int ArrayDelegate1 (int i, 
										string j, 
										[In, MarshalAs(UnmanagedType.LPArray, 
													   ArraySubType=UnmanagedType.LPStr, SizeParamIndex=0)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate1 (string[] arr, int len, ArrayDelegate1 d);

	public static int array_delegate1 (int i, string j, string[] arr) {
		if (arr.Length != 2)
			return 1;
		if ((arr [0] != "ABC") || (arr [1] != "DEF"))
			return 2;
		return 0;
	}

	public static int test_0_marshal_array_delegate_string () {	
		string[] arr = new string [] { "ABC", "DEF" };
		return mono_test_marshal_array_delegate1 (arr, arr.Length, new ArrayDelegate1 (array_delegate1));
	}

	public static int array_delegate2 (int i, string j, string[] arr) {
		return (arr == null) ? 0 : 1;
	}

	public static int test_0_marshal_array_delegate_null () {	
		return mono_test_marshal_array_delegate1 (null, 0, new ArrayDelegate1 (array_delegate2));
	}

	public delegate int ArrayDelegateBlittable (int i, string j,
										[In, MarshalAs(UnmanagedType.LPArray,
													   ArraySubType=UnmanagedType.LPStr, SizeParamIndex=0)] int[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate1 (string[] arr, int len, ArrayDelegateBlittable d);

	public static int array_delegate_null_blittable (int i, string j, int[] arr) {
		return (arr == null) ? 0 : 1;
	}

	public static int test_0_marshal_array_delegate_null_blittable () {
		return mono_test_marshal_array_delegate1 (null, 0, new ArrayDelegateBlittable (array_delegate_null_blittable));
	}

	public delegate int ArrayDelegate3 (int i, 
										string j, 
										[In, MarshalAs(UnmanagedType.LPArray, 
													   ArraySubType=UnmanagedType.LPStr, SizeParamIndex=3)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate3 (string[] arr, int len, ArrayDelegate3 d);

	public static int array_delegate3 (int i, string j, string[] arr) {
		return (arr == null) ? 0 : 1;
	}

	public static int test_0_marshal_array_delegate_bad_paramindex () {
		try {
			mono_test_marshal_array_delegate3 (null, 0, new ArrayDelegate3 (array_delegate3));
			return 1;
		}
		catch (MarshalDirectiveException) {
			return 0;
		}
	}

	public delegate int ArrayDelegate4 (int i, 
										string j, 
										[In, MarshalAs(UnmanagedType.LPArray, 
													   ArraySubType=UnmanagedType.LPStr, SizeParamIndex=1)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate4 (string[] arr, int len, ArrayDelegate4 d);

	public static int array_delegate4 (int i, string j, string[] arr) {
		return (arr == null) ? 0 : 1;
	}

	public static int test_0_marshal_array_delegate_bad_paramtype () {
		try {
			mono_test_marshal_array_delegate4 (null, 0, new ArrayDelegate4 (array_delegate4));
			return 1;
		}
		catch (MarshalDirectiveException) {
			return 0;
		}
	}

	public delegate int ArrayDelegate4_2 (int i, 
										string j, 
										  string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate4_2 (string[] arr, int len, ArrayDelegate4_2 d);

	public static int array_delegate4_2 (int i, string j, string[] arr) {
		return (arr == null) ? 0 : 1;
	}

	public static int test_0_marshal_array_delegate_no_marshal_directive () {
		try {
			mono_test_marshal_array_delegate4_2 (null, 0, new ArrayDelegate4_2 (array_delegate4_2));
			return 1;
		}
		catch (MarshalDirectiveException) {
			return 0;
		}
	}

	public delegate int ArrayDelegate4_3 (int i, 
										string j, 
										  string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate4_3 (string[] arr, int len, ArrayDelegate4_3 d);

	public int array_delegate4_3 (int i, string j, string[] arr) {
		return (arr == null) ? 0 : 1;
	}

	public static int test_0_marshal_array_delegate_no_marshal_directive_instance () {
		try {
			Tests t = new Tests ();
			mono_test_marshal_array_delegate4_3 (null, 0, new ArrayDelegate4_3 (t.array_delegate4_3));
			return 1;
		}
		catch (MarshalDirectiveException) {
			return 0;
		}
	}

	public delegate int ArrayDelegate5 (int i, 
										string j, 
										[In, MarshalAs(UnmanagedType.LPArray, 
													   ArraySubType=UnmanagedType.LPWStr, SizeParamIndex=0)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate", CharSet=CharSet.Unicode)]
	public static extern int mono_test_marshal_array_delegate5 (string[] arr, int len, ArrayDelegate5 d);

	public static int array_delegate5 (int i, string j, string[] arr) {
		if (arr.Length != 2)
			return 1;
		if ((arr [0] != "ABC") || (arr [1] != "DEF"))
			return 2;
		return 0;
	}

	public static int test_0_marshal_array_delegate_unicode_string () {	
		string[] arr = new string [] { "ABC", "DEF" };
		return mono_test_marshal_array_delegate5 (arr, arr.Length, new ArrayDelegate5 (array_delegate5));
	}

	public delegate int ArrayDelegate6 (int i, 
										string j, 
										[In, MarshalAs(UnmanagedType.LPArray, 
													   ArraySubType=UnmanagedType.LPStr, SizeConst=2)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate6 (string[] arr, int len, ArrayDelegate6 d);

	public static int array_delegate6 (int i, string j, string[] arr) {
		if (arr.Length != 2)
			return 1;
		if ((arr [0] != "ABC") || (arr [1] != "DEF"))
			return 2;
		return 0;
	}

	public static int test_0_marshal_array_delegate_sizeconst () {	
		string[] arr = new string [] { "ABC", "DEF" };
		return mono_test_marshal_array_delegate6 (arr, 1024, new ArrayDelegate6 (array_delegate6));
	}

	public delegate int ArrayDelegate7 (int i, 
										string j, 
										[In, MarshalAs(UnmanagedType.LPArray, 
													   ArraySubType=UnmanagedType.LPStr, SizeConst=1, SizeParamIndex=0)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate7 (string[] arr, int len, ArrayDelegate7 d);

	public static int array_delegate7 (int i, string j, string[] arr) {
		if (arr.Length != 2)
			return 1;
		if ((arr [0] != "ABC") || (arr [1] != "DEF"))
			return 2;
		return 0;
	}

	public static int test_0_marshal_array_delegate_sizeconst_paramindex () {	
		string[] arr = new string [] { "ABC", "DEF" };
		return mono_test_marshal_array_delegate7 (arr, 1, new ArrayDelegate7 (array_delegate7));
	}

	public delegate int ArrayDelegate8 (int i, string j,
										[In, MarshalAs(UnmanagedType.LPArray, 
										SizeParamIndex=0)] 
										int[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate")]
	public static extern int mono_test_marshal_array_delegate8 (int[] arr, int len, ArrayDelegate8 d);

	public static int array_delegate8 (int i, string j, int[] arr) {
		if (arr.Length != 2)
			return 1;
		if ((arr [0] != 42) || (arr [1] != 43))
			return 2;
		return 0;
	}

	public static int test_0_marshal_array_delegate_blittable () {	
		int[] arr = new int [] { 42, 43 };
		return mono_test_marshal_array_delegate8 (arr, 2, new ArrayDelegate8 (array_delegate8));
	}

	/* Array with size param of type long */

	public delegate int ArrayDelegate8_2 (long i, 
										string j, 
										[In, MarshalAs(UnmanagedType.LPArray, 
													   ArraySubType=UnmanagedType.LPStr, SizeParamIndex=0)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array_delegate_long")]
	public static extern int mono_test_marshal_array_delegate8_2 (string[] arr, long len, ArrayDelegate8_2 d);

	public static int array_delegate8_2 (long i, string j, string[] arr) {
		if (arr.Length != 2)
			return 1;
		if ((arr [0] != "ABC") || (arr [1] != "DEF"))
			return 2;
		return 0;
	}

	public static int test_0_marshal_array_delegate_long_param () {	
		string[] arr = new string [] { "ABC", "DEF" };
		return mono_test_marshal_array_delegate8_2 (arr, arr.Length, new ArrayDelegate8_2 (array_delegate8_2));
	}


	/*
	 * [Out] blittable arrays
	 */

	public delegate int ArrayDelegate9 (int i, string j,
										[Out, MarshalAs(UnmanagedType.LPArray, 
										SizeParamIndex=0)] 
										int[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_array_delegate")]
	public static extern int mono_test_marshal_out_array_delegate (int[] arr, int len, ArrayDelegate9 d);

	public static int array_delegate9 (int i, string j, int[] arr) {
		if (arr.Length != 2)
			return 1;

		arr [0] = 1;
		arr [1] = 2;

		return 0;
	}

	public static int test_0_marshal_out_array_delegate () {	
		int[] arr = new int [] { 42, 43 };
		return mono_test_marshal_out_array_delegate (arr, 2, new ArrayDelegate9 (array_delegate9));
	}

	/*
	 * [Out] string arrays
	 */

	public delegate int ArrayDelegate10 (int i, 
										 string j, 
										 [Out, MarshalAs(UnmanagedType.LPArray, 
														 ArraySubType=UnmanagedType.LPStr, SizeConst=2)] string[] arr);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_string_array_delegate")]
	public static extern int mono_test_marshal_out_string_array_delegate (string[] arr, int len, ArrayDelegate10 d);

	public static int array_delegate10 (int i, string j, string[] arr) {
		if (arr.Length != 2)
			return 1;

		arr [0] = "ABC";
		arr [1] = "DEF";

		return 0;
	}

	public static int test_0_marshal_out_string_array_delegate () {	
		string[] arr = new string [] { "", "" };
		return mono_test_marshal_out_string_array_delegate (arr, 2, new ArrayDelegate10 (array_delegate10));
	}

	/*
	 * [In, Out] classes
	 */

	public delegate int InOutByvalClassDelegate ([In, Out] SimpleClass ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_inout_byval_class_delegate")]
	public static extern int mono_test_marshal_inout_byval_class_delegate (InOutByvalClassDelegate d);

	public static int delegate_test_byval_class_inout (SimpleClass ss) {
		if ((ss.a != false) || (ss.b != true) || (ss.c != false) || (ss.d != "FOO"))
			return 1;

		ss.a = true;
		ss.b = false;
		ss.c = true;
		ss.d = "RES";

		return 0;
	}

	public static int test_0_marshal_inout_byval_class_delegate () {
		return mono_test_marshal_inout_byval_class_delegate (new InOutByvalClassDelegate (delegate_test_byval_class_inout));
	}

	/*
	 * Returning unicode strings
	 */
	[return: MarshalAs(UnmanagedType.LPWStr)]
	public delegate string ReturnUnicodeStringDelegate([MarshalAs(UnmanagedType.LPWStr)] string message);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_unicode_string_delegate")]
	public static extern int mono_test_marshal_return_unicode_string_delegate (ReturnUnicodeStringDelegate d);

	public static String return_unicode_string_delegate (string message) {
		return message;
	}

	public static int test_0_marshal_return_unicode_string_delegate () {	
		return mono_test_marshal_return_unicode_string_delegate (new ReturnUnicodeStringDelegate (return_unicode_string_delegate));
	}

	/*
	 * Returning string arrays
	 */
	public delegate string[] ReturnArrayDelegate (int i);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_string_array_delegate")]
	public static extern int mono_test_marshal_return_string_array_delegate (ReturnArrayDelegate d);

	public static String[] return_array_delegate (int i) {
		String[] arr = new String [2];

		arr [0] = "ABC";
		arr [1] = "DEF";

		return arr;
	}

	public static String[] return_array_delegate_null (int i) {
		return null;
	}

	public static int test_0_marshal_return_string_array_delegate () {	
		return mono_test_marshal_return_string_array_delegate (new ReturnArrayDelegate (return_array_delegate));
	}

	public static int test_3_marshal_return_string_array_delegate_null () {	
		return mono_test_marshal_return_string_array_delegate (new ReturnArrayDelegate (return_array_delegate_null));
	}

	/*
	 * Byref string marshalling
	 */
	public delegate int ByrefStringDelegate (ref string s);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_byref_string_delegate")]
	public static extern int mono_test_marshal_byref_string_delegate (ByrefStringDelegate d);

	public static int byref_string_delegate (ref string s) {
		if (s != "ABC")
			return 1;

		s = "DEF";

		return 0;
	}

	public static int test_0_marshal_byref_string_delegate () {	
		return mono_test_marshal_byref_string_delegate (new ByrefStringDelegate (byref_string_delegate));
	}

	/*
	 * Thread attach
	 */

	public delegate int SimpleDelegate (int i);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_thread_attach")]
	public static extern int mono_test_marshal_thread_attach (SimpleDelegate d);

	public static int test_43_thread_attach () {
		int res = mono_test_marshal_thread_attach (delegate (int i) {
				if (!Thread.CurrentThread.IsBackground)
					return 0;
				return i + 1;
			});
		return res;
	}

	public struct LargeStruct {
            public Int16 s;
            public Int16 v;
            public UInt32 p;
            public UInt32 e;
            public Int32 l;
            public Int32 ll;
            public UInt16 h;
            public Int16 r;
            public Int16 pp;
            public Int32 hh;
            public Int32 bn;
            public Int32 dn;
            public Int32 dr;
            public Int32 sh;
            public Int32 ra;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Int32[] angle;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Int32[] width;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public Int32[] edge;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3 * 1024)]
            public byte[] echo;
	}

	public delegate int LargeStructDelegate (ref LargeStruct s);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_thread_attach_large_vt")]
	public static extern int mono_test_marshal_thread_attach_large_vt (LargeStructDelegate d);

	public static int test_43_thread_attach_large_vt () {
		int res = mono_test_marshal_thread_attach_large_vt (delegate (ref LargeStruct s) {
				return 43;
			});
		return res;
	}

	class Worker {
		volatile bool stop = false;
		public void Stop () {
			stop = true;
		}

		public void Work () {
			while (!stop) {
				for (int i = 0; i < 100; i++) {
					var a = new double[80000];
					Thread.Sleep (1);
				}
				GC.Collect ();
			}
		}
	}

	public static int test_43_thread_attach_detach_contested () {
		// Test plan: we want to create a race between the GC
		// and native threads detaching.  When a native thread
		// calls a managed delegate, it's attached to the
		// runtime by the wrapper.  It is detached when the
		// thread is destroyed and the TLS key destructor for
		// MonoThreadInfo runs.  That destructor wants to take
		// the GC lock.  So we create a lot of native threads
		// while at the same time running a worker that
		// allocates garbage and invokes the collector.
		var w = new Worker ();
		Thread t = new Thread(new ThreadStart (w.Work));
		t.Start ();

		for (int count = 0; count < 500; count++) {
			int res = mono_test_marshal_thread_attach (delegate (int i) {
					Thread.Sleep (0);
					return i + 1;
				});
		}
		Thread.Sleep (1000);
		w.Stop ();
		t.Join ();
		return 43; 

	}
	/*
	 * Appdomain save/restore
	 */
    static Func<int> callback;

	[DllImport ("libtest", EntryPoint="mono_test_marshal_set_callback")]
	public static extern int mono_test_marshal_set_callback (Func<int> a);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_call_callback")]
	public static extern int mono_test_marshal_call_callback ();

	public static int test_0_appdomain_switch () {
		// FIXME: The appdomain unload hangs
		//return 0;
        AppDomain ad = AppDomain.CreateDomain ("foo");
		var c = (CallbackClass)ad.CreateInstanceAndUnwrap (
				typeof (CallbackClass).Assembly.FullName, "Tests/CallbackClass");
		c.SetCallback ();
		int domain_id = AppDomain.CurrentDomain.Id;
		int new_id = mono_test_marshal_call_callback ();
		int res = 0;
		if (new_id == domain_id)
			res = 1;
		if (AppDomain.CurrentDomain.Id != domain_id)
			res = 2;
		AppDomain.Unload (ad);
		return res;
    }

	static int domain_callback () {
		return AppDomain.CurrentDomain.Id;
	}

	class CallbackClass : MarshalByRefObject {
		public int SetCallback () {
			mono_test_marshal_set_callback (domain_callback);
			return 0;
		}
    }

	class Base {
		public VirtualDelegate get_del () {
			return delegate_test;
		}

		public virtual int delegate_test (int i) {
			return i;
		}
	}

	class Derived : Base {
		public override int delegate_test (int i) {
			return i + 1;
		}
	}

	public static int test_43_virtual () {
		Base b = new Derived ();

		return mono_test_marshal_virtual_delegate (b.get_del ());
	}

	public static int test_0_icall_delegate () {
		var m = typeof (Marshal).GetMethod ("PtrToStringAnsi", new Type[] { typeof (IntPtr) });

		return mono_test_marshal_icall_delegate ((IcallDelegate)Delegate.CreateDelegate (typeof (IcallDelegate), m));
	}

	private static Nullable<int> nullable_ret_cb () {
		return 0;
	}

	public static int test_0_generic_return () {
		try {
			Marshal.GetFunctionPointerForDelegate<NullableReturnDelegate> (nullable_ret_cb);
			return 1;
		} catch (MarshalDirectiveException) {
		}
		try {
			mono_test_marshal_nullable_ret_delegate (nullable_ret_cb);
			return 2;
		} catch (MarshalDirectiveException) {
		}

		return 0;
	}
}
