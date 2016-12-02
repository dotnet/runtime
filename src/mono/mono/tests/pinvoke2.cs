//
// Copyright 2011 Xamarin Inc (http://www.xamarin.com).
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;

public unsafe class Tests {

	public int int_field;

	public static int delegate_test (int a)
	{
		if (a == 2)
			return 0;

		return 1;
	}

	public int delegate_test_instance (int a)
	{
		return int_field + a;
	}
	
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
	public struct SimpleStructGen<T> {
		public bool a;
		public bool b;
		public bool c;
		public string d;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string d2;
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

	[StructLayout (LayoutKind.Sequential, Size=1)]
	public struct EmptyStructCpp {
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct DelegateStruct {
		public int a;
		public SimpleDelegate del;
		[MarshalAs(UnmanagedType.FunctionPtr)] 
		public SimpleDelegate del2;
		[MarshalAs(UnmanagedType.FunctionPtr)] 
		public SimpleDelegate del3;
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct SingleDoubleStruct {
		public double d;
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
	public struct TinyStruct {
		public TinyStruct (int i)
		{
			this.i = i;
		}
		public int i;
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

	[StructLayout (LayoutKind.Sequential)]
	public struct LongAlignStruct {
		public int a;
		public long b;
		public long c;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class BlittableClass
	{
		public int a = 1;
		public int b = 2;
	}

	[StructLayout (LayoutKind.Sequential)]
	class SimpleObj
	{
		public string str;
		public int i;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct AsAnyStruct
	{
		public int i;
		public int j;
		public int k;
		public String s;

		public AsAnyStruct(int i, int j, int k, String s) {
			this.i = i;
			this.j = j;
			this.k = k;
			this.s = s;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	class AsAnyClass
	{
		public int i;
		public int j;
		public int k;
		public String s;

		public AsAnyClass(int i, int j, int k, String s) {
			this.i = i;
			this.j = j;
			this.k = k;
		}
	}

	[DllImport ("libnot-found", EntryPoint="not_found")]
	public static extern int mono_library_not_found ();

	[DllImport ("libtest", EntryPoint="not_found")]
	public static extern int mono_entry_point_not_found ();

	[DllImport ("libtest.dll", EntryPoint="mono_test_marshal_char")]
	public static extern int mono_test_marshal_char_2 (char a1);

	[DllImport ("test", EntryPoint="mono_test_marshal_char")]
	public static extern int mono_test_marshal_char_3 (char a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_char")]
	public static extern int mono_test_marshal_char (char a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_char_array", CharSet=CharSet.Unicode)]
	public static extern int mono_test_marshal_char_array (char[] a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_byref")]
	public static extern int mono_test_marshal_bool_byref (int a, ref bool b, int c);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_in_as_I1_U1")]
	public static extern int mono_test_marshal_bool_in_as_I1 ([MarshalAs (UnmanagedType.I1)] bool bTrue, [MarshalAs (UnmanagedType.I1)] bool bFalse);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_in_as_I1_U1")]
	public static extern int mono_test_marshal_bool_in_as_U1 ([MarshalAs (UnmanagedType.U1)] bool bTrue, [MarshalAs (UnmanagedType.U1)] bool bFalse);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_out_as_I1_U1")]
	public static extern int mono_test_marshal_bool_out_as_I1 ([MarshalAs (UnmanagedType.I1)] out bool bTrue, [MarshalAs (UnmanagedType.I1)] out bool bFalse);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_out_as_I1_U1")]
	public static extern int mono_test_marshal_bool_out_as_U1 ([MarshalAs (UnmanagedType.U1)] out bool bTrue, [MarshalAs (UnmanagedType.U1)] out bool bFalse);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_ref_as_I1_U1")]
	public static extern int mono_test_marshal_bool_ref_as_I1 ([MarshalAs (UnmanagedType.I1)] ref bool bTrue, [MarshalAs (UnmanagedType.I1)] ref bool bFalse);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_bool_ref_as_I1_U1")]
	public static extern int mono_test_marshal_bool_ref_as_U1 ([MarshalAs (UnmanagedType.U1)] ref bool bTrue, [MarshalAs (UnmanagedType.U1)] ref bool bFalse);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_array")]
	public static extern int mono_test_marshal_array (int [] a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_empty_string_array")]
	public static extern int mono_test_marshal_empty_string_array (string [] a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_string_array")]
	public static extern int mono_test_marshal_string_array (string [] a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_unicode_string_array", CharSet=CharSet.Unicode)]
	public static extern int mono_test_marshal_unicode_string_array (string [] a1, [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStr)]string [] a2);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_array")]
	public static extern int mono_test_marshal_stringbuilder_array (StringBuilder [] a1);	

	[DllImport ("libtest", EntryPoint="mono_test_marshal_inout_array")]
	public static extern int mono_test_marshal_inout_array ([In, Out] int [] a1);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_array")]
	public static extern int mono_test_marshal_out_array ([Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int [] a1, int n);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_byref_array_out_size_param")]
	public static extern int mono_test_marshal_out_byref_array_out_size_param ([Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out int [] a1, out int n);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_lparray_out_size_param")]
	public static extern int mono_test_marshal_out_lparray_out_size_param ([Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int [] a1, out int n);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_inout_nonblittable_array", CharSet = CharSet.Unicode)]
	public static extern int mono_test_marshal_inout_nonblittable_array ([In, Out] char [] a1);
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct")]
	public static extern int mono_test_marshal_struct (SimpleStruct ss);
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct")]
	public static extern int mono_test_marshal_struct_gen (SimpleStructGen<string> ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct2")]
	public static extern int mono_test_marshal_struct2 (SimpleStruct2 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct2_2")]
	public static extern int mono_test_marshal_struct2_2 (int i, int j, int k, SimpleStruct2 ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_byref_struct")]
	public static extern int mono_test_marshal_byref_struct (ref SimpleStruct ss, bool a, bool b, bool c, String d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_byref_struct")]
	public static extern int mono_test_marshal_byref_struct_in ([In] ref SimpleStruct ss, bool a, bool b, bool c, String d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_byref_struct")]
	public static extern int mono_test_marshal_byref_struct_inout ([In, Out] ref SimpleStruct ss, bool a, bool b, bool c, String d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_point")]
	public static extern int mono_test_marshal_point (Point p);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_mixed_point")]
	public static extern int mono_test_marshal_mixed_point (MixedPoint p);

	[DllImport ("libtest", EntryPoint="mono_test_empty_struct")]
	public static extern int mono_test_empty_struct (int a, EmptyStruct es, int b);

	[DllImport ("libtest", EntryPoint="mono_test_return_empty_struct")]
	public static extern EmptyStruct mono_test_return_empty_struct (int a);

	[DllImport ("libtest", EntryPoint="mono_test_return_empty_struct")]
	public static extern EmptyStructCpp mono_test_return_empty_struct_cpp (int a);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_lpstruct")]
	public static extern int mono_test_marshal_lpstruct ([In, MarshalAs(UnmanagedType.LPStruct)] SimpleStruct ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_lpstruct_blittable")]
	public static extern int mono_test_marshal_lpstruct_blittable ([In, MarshalAs(UnmanagedType.LPStruct)] Point p);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct_array")]
	public static extern int mono_test_marshal_struct_array (SimpleStruct2[] ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_long_align_struct_array")]
	public static extern int mono_test_marshal_long_align_struct_array (LongAlignStruct[] ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_class")]
	public static extern SimpleClass mono_test_marshal_class (int i, int j, int k, SimpleClass ss, int l);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_byref_class")]
	public static extern int mono_test_marshal_byref_class (ref SimpleClass ss);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate")]
	public static extern int mono_test_marshal_delegate (SimpleDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_delegate_struct")]
	public static extern DelegateStruct mono_test_marshal_delegate_struct (DelegateStruct d);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_return_delegate")]
	public static extern SimpleDelegate mono_test_marshal_return_delegate (SimpleDelegate d);

	[DllImport ("libtest", EntryPoint="mono_test_return_vtype")]
	public static extern SimpleStruct mono_test_return_vtype (IntPtr i);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder")]
	public static extern void mono_test_marshal_stringbuilder (StringBuilder sb, int len);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_default")]
	public static extern void mono_test_marshal_stringbuilder_default (StringBuilder sb, int len);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_append")]
	public static extern void mono_test_marshal_stringbuilder_append (StringBuilder sb, int len);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_unicode", CharSet=CharSet.Unicode)]
	public static extern void mono_test_marshal_stringbuilder_unicode (StringBuilder sb, int len);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_out")]
	public static extern void mono_test_marshal_stringbuilder_out (out StringBuilder sb);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_ref")]
	public static extern int mono_test_marshal_stringbuilder_ref (ref StringBuilder sb);

	[DllImport ("libtest", EntryPoint="mono_test_marshal_stringbuilder_out_unicode", CharSet=CharSet.Unicode)]
	public static extern void mono_test_marshal_stringbuilder_out_unicode (out StringBuilder sb);

	[DllImport ("libtest", EntryPoint="mono_test_last_error", SetLastError=true)]
	public static extern void mono_test_last_error (int err);

	[DllImport ("libtest", EntryPoint="mono_test_asany")]
	public static extern int mono_test_asany ([MarshalAs (UnmanagedType.AsAny)] object o, int what);

	[DllImport ("libtest", EntryPoint="mono_test_asany", CharSet=CharSet.Unicode)]
	public static extern int mono_test_asany_unicode ([MarshalAs (UnmanagedType.AsAny)] object o, int what);

	[DllImport("libtest", EntryPoint="mono_test_marshal_asany_in")]
	static extern void mono_test_asany_in ([MarshalAs(UnmanagedType.AsAny)][In] object obj); 

	[DllImport("libtest", EntryPoint="mono_test_marshal_asany_out")]
	static extern void mono_test_asany_out ([MarshalAs(UnmanagedType.AsAny)][Out] object obj); 
	[DllImport("libtest", EntryPoint="mono_test_marshal_asany_inout")]
	static extern void mono_test_asany_inout ([MarshalAs(UnmanagedType.AsAny)][In, Out] object obj); 

	[DllImport ("libtest")]
        static extern int class_marshal_test0 (SimpleObj obj);

	[DllImport ("libtest")]
        static extern void class_marshal_test1 (out SimpleObj obj);

	[DllImport ("libtest")]
        static extern int class_marshal_test4 (SimpleObj obj);
	
	[DllImport ("libtest")]
        static extern int string_marshal_test0 (string str);

	[DllImport ("libtest")]
        static extern void string_marshal_test1 (out string str);

	[DllImport ("libtest")]
        static extern int string_marshal_test2 (ref string str);

	[DllImport ("libtest")]
        static extern int string_marshal_test3 (string str);

	public delegate int SimpleDelegate (int a);

	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_marshal_char () {
		return mono_test_marshal_char ('a');
	}

	public static int test_0_marshal_char_array () {
		// a unicode char[] is implicitly marshalled as [Out]
		char[] buf = new char [32];
		mono_test_marshal_char_array (buf);
		string s = new string (buf);
		if (s.StartsWith ("abcdef"))
			return 0;
		else
			return 1;
	}

	public static int test_1225_marshal_array () {
		int [] a1 = new int [50];
		for (int i = 0; i < 50; i++)
			a1 [i] = i;

		return mono_test_marshal_array (a1);
	}

	public static int test_1225_marshal_inout_array () {
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

	public static int test_0_marshal_out_array () {
		int [] a1 = new int [50];

		int res = mono_test_marshal_out_array (a1, 0);

		for (int i = 0; i < 50; i++)
			if (a1 [i] != i) {
				Console.WriteLine ("X: " + i + " " + a1 [i]);
				return 2;
			}

		return 0;
	}

	public static int test_0_marshal_out_byref_array_out_size_param () {
		int [] a1 = null;
		int len;

		int res = mono_test_marshal_out_byref_array_out_size_param (out a1, out len);
		if (len != 4)
			return 1;
		for (int i = 0; i < len; i++)
			if (a1 [i] != i)
				return 2;
		return 0;
	}

	public static int test_0_marshal_out_lparray_out_size_param () {
		int [] a1 = null;
		int len;

		a1 = new int [10];
		int res = mono_test_marshal_out_lparray_out_size_param (a1, out len);
		// Check that a1 was not overwritten
		a1.GetHashCode ();
		if (len != 4)
			return 1;
		for (int i = 0; i < len; i++)
			if (a1 [i] != i)
				return 2;
		return 0;
	}

	public static int test_0_marshal_inout_nonblittable_array () {
		char [] a1 = new char [10];
		for (int i = 0; i < 10; i++)
			a1 [i] = "Hello, World" [i];

		int res = mono_test_marshal_inout_nonblittable_array (a1);

		for (int i = 0; i < 10; i++)
			if (a1 [i] != 'F')
				return 2;

		return res;
	}

	public static int test_0_marshal_struct () {
		SimpleStruct ss = new  SimpleStruct ();
		ss.b = true;
		ss.d = "TEST";
		
		return mono_test_marshal_struct (ss);
	}

	public static int test_0_marshal_struct_gen () {
		SimpleStructGen<string> ss = new  SimpleStructGen<string> ();
		ss.b = true;
		ss.d = "TEST";
		
		return mono_test_marshal_struct_gen (ss);
	}

	public static int test_0_marshal_struct2 () {
		SimpleStruct2 ss2 = new  SimpleStruct2 ();
		ss2.b = true;
		ss2.d = "TEST";
		ss2.e = 99;
		ss2.f = 1.5;
		ss2.g = 42;
		ss2.h = 123L;

		return mono_test_marshal_struct2 (ss2);
	}

	public static int test_0_marshal_struct3 () {
		SimpleStruct2 ss2 = new  SimpleStruct2 ();
		ss2.b = true;
		ss2.d = "TEST";
		ss2.e = 99;
		ss2.f = 1.5;
		ss2.g = 42;
		ss2.h = 123L;

		return mono_test_marshal_struct2_2 (10, 11, 12, ss2);
	}

	public static int test_0_marshal_empty_struct () {
		EmptyStruct es = new EmptyStruct ();

		if (mono_test_empty_struct (1, es, 2) != 0)
			return 1;

		mono_test_return_empty_struct (42);

		return 0;
	}

	/* FIXME: This doesn't work on all platforms */
	/*
	public static int test_0_marshal_empty_struct_cpp () {
		EmptyStructCpp es = new EmptyStructCpp ();

		mono_test_return_empty_struct_cpp (42);
		
		return 0;
	}
	*/

	public static int test_0_marshal_lpstruct () {
		SimpleStruct ss = new  SimpleStruct ();
		ss.b = true;
		ss.d = "TEST";
		
		return mono_test_marshal_lpstruct (ss);
	}

	public static int test_0_marshal_lpstruct_blittable () {
		Point p = new Point ();
		p.x = 1.0;
		p.y = 2.0;
		
		return mono_test_marshal_lpstruct_blittable (p);
	}

	public static int test_0_marshal_struct_array () {
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

	public static int test_105_marshal_long_align_struct_array () {
		LongAlignStruct[] ss_arr = new LongAlignStruct [2];

		LongAlignStruct ss = new LongAlignStruct ();
		ss.a = 5;
		ss.b = 10;
		ss.c = 15;

		ss_arr [0] = ss;

		ss.a = 20;
		ss.b = 25;
		ss.c = 30;

		ss_arr [1] = ss;

		return mono_test_marshal_long_align_struct_array (ss_arr);
	}

	/* Test classes as arguments and return values */
	public static int test_0_marshal_class () {
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

	public static int test_0_marshal_byref_class () {
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

	public static int test_0_marshal_delegate () {
		SimpleDelegate d = new SimpleDelegate (delegate_test);

		return mono_test_marshal_delegate (d);
	}

	public static int test_34_marshal_instance_delegate () {
		Tests t = new Tests ();
		t.int_field = 32;
		SimpleDelegate d = new SimpleDelegate (t.delegate_test_instance);

		return mono_test_marshal_delegate (d);
	}

	/* Static delegates closed over their first argument */
	public static int closed_delegate (Tests t, int a) {
		return t.int_field + a;
	}

	public static int test_34_marshal_closed_static_delegate () {
		Tests t = new Tests ();
		t.int_field = 32;
		SimpleDelegate d = (SimpleDelegate)Delegate.CreateDelegate (typeof (SimpleDelegate), t, typeof (Tests).GetMethod ("closed_delegate"));

		return mono_test_marshal_delegate (d);
	}

	public static int test_0_marshal_return_delegate () {
		SimpleDelegate d = new SimpleDelegate (delegate_test);

		SimpleDelegate d2 = mono_test_marshal_return_delegate (d);

		return d2 (2);
	}

	public static int test_0_marshal_delegate_struct () {
		DelegateStruct s = new DelegateStruct ();

		s.a = 2;
		s.del = new SimpleDelegate (delegate_test);
		s.del2 = new SimpleDelegate (delegate_test);
		s.del3 = null;

		DelegateStruct res = mono_test_marshal_delegate_struct (s);

		if (res.a != 0)
			return 1;
		if (res.del (2) != 0)
			return 2;
		if (res.del2 (2) != 0)
			return 3;
		if (res.del3 != null)
			return 4;

		return 0;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_out_delegate")]
	public static extern int mono_test_marshal_out_delegate (out SimpleDelegate d);

	public static int test_3_marshal_out_delegate () {
		SimpleDelegate d = null;

		mono_test_marshal_out_delegate (out d);

		return d (2);
	}

	public static int test_0_marshal_byref_struct () {
		SimpleStruct s = new SimpleStruct ();
		s.a = true;
		s.b = false;
		s.c = true;
		s.d = "ABC";
		s.d2 = "DEF";

		int res = mono_test_marshal_byref_struct (ref s, true, false, true, "ABC");
		if (res != 0)
			return 1;
		if (s.a != false || s.b != true || s.c != false || s.d != "DEF")
			return 2;
		return 0;
	}

	public static int test_0_marshal_byref_struct_in () {
		SimpleStruct s = new SimpleStruct ();
		s.a = true;
		s.b = false;
		s.c = true;
		s.d = "ABC";
		s.d2 = "DEF";

		int res = mono_test_marshal_byref_struct_in (ref s, true, false, true, "ABC");
		if (res != 0)
			return 1;
		if (s.a != true || s.b != false || s.c != true || s.d != "ABC")
			return 2;
		return 0;
	}

	public static int test_0_marshal_byref_struct_inout () {
		SimpleStruct s = new SimpleStruct ();
		s.a = true;
		s.b = false;
		s.c = true;
		s.d = "ABC";
		s.d2 = "DEF";

		int res = mono_test_marshal_byref_struct_inout (ref s, true, false, true, "ABC");
		if (res != 0)
			return 1;
		if (s.a != false || s.b != true || s.c != false || s.d != "DEF")
			return 2;
		return 0;
	}

	public static int test_0_marshal_point () {
		Point pt = new Point();
		pt.x = 1.25;
		pt.y = 3.5;
		
		return mono_test_marshal_point(pt);
	}

	public static int test_0_marshal_mixed_point () {
		MixedPoint mpt = new MixedPoint();
		mpt.x = 5;
		mpt.y = 6.75;
		
		return mono_test_marshal_mixed_point(mpt);
	}

	public static int test_0_marshal_bool_byref () {
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

	public static int test_0_marshal_bool_as_I1 () {

		int ret;
		bool bTrue, bFalse;
		if ((ret = mono_test_marshal_bool_in_as_I1 (true, false)) != 0)
			return ret;

		if ((ret = mono_test_marshal_bool_out_as_I1 (out bTrue, out bFalse)) != 0)
			return ret;

		if(!bTrue)
			return 10;

		if(bFalse)
			return 11;

		if ((ret = mono_test_marshal_bool_ref_as_I1 (ref bTrue, ref bFalse)) != 0)
			return ret;

		if(bTrue)
			return 12;

		if(!bFalse)
			return 13;

		return 0;
	}

	public static int test_0_marshal_bool_as_U1 () {

		int ret;
		bool bTrue, bFalse;
		if ((ret = mono_test_marshal_bool_in_as_U1 (true, false)) != 0)
			return ret;

		if ((ret = mono_test_marshal_bool_out_as_U1 (out bTrue, out bFalse)) != 0)
			return ret;

		if(!bTrue)
			return 10;

		if(bFalse)
			return 11;

		if ((ret = mono_test_marshal_bool_ref_as_U1 (ref bTrue, ref bFalse)) != 0)
			return ret;

		if(bTrue)
			return 12;

		if(!bFalse)
			return 13;

		return 0;
	}

	public static int test_0_return_vtype () {
		SimpleStruct ss = mono_test_return_vtype (new IntPtr (5));

		if (!ss.a && ss.b && !ss.c && ss.d == "TEST" && ss.d2 == "TEST2")
			return 0;

		return 1;
	}

	public static int test_0_marshal_stringbuilder () {
		StringBuilder sb = new StringBuilder(255);
		sb.Append ("ABCD");
		mono_test_marshal_stringbuilder (sb, sb.Capacity);
		String res = sb.ToString();

		if (res != "This is my message.  Isn't it nice?")
			return 1;  

		// Test StringBuilder with default capacity (16)
		StringBuilder sb2 = new StringBuilder();
		mono_test_marshal_stringbuilder_default (sb2, sb2.Capacity);
		if (sb2.ToString () != "This is my messa")
			return 3;

		return 0;
	}

	public static int test_0_marshal_stringbuilder_append () {
		const String in_sentinel = "MONO_";
		const String out_sentinel = "CSHARP_";
		const int iterations = 100;
		StringBuilder sb = new StringBuilder(255);
		StringBuilder check = new StringBuilder(255);

		for (int i = 0; i < iterations; i++) {
			sb.Append (in_sentinel[i % in_sentinel.Length]);
			check.Append (out_sentinel[i % out_sentinel.Length]);

			mono_test_marshal_stringbuilder_append (sb, sb.Length);

			String res = sb.ToString();
			String checkRev = check.ToString();
			if (res != checkRev)
				return 1;
		}

		// Test StringBuilder with default capacity (16)
		StringBuilder sb2 = new StringBuilder();
		mono_test_marshal_stringbuilder_default (sb2, sb2.Capacity);
		if (sb2.ToString () != "This is my messa")
			return 3;

		return 0;
	}

	public static int test_0_marshal_stringbuilder_unicode () {
		StringBuilder sb = new StringBuilder(255);
		mono_test_marshal_stringbuilder_unicode (sb, sb.Capacity);
		String res = sb.ToString();

		if (res != "This is my message.  Isn't it nice?")
			return 1;  

		// Test StringBuilder with default capacity (16)
		StringBuilder sb2 = new StringBuilder();
		mono_test_marshal_stringbuilder_unicode (sb2, sb2.Capacity);
		if (sb2.ToString () != "This is my messa")
			return 2;
		
		return 0;
	}

	public static int test_0_marshal_stringbuilder_out () {
		StringBuilder sb;
		mono_test_marshal_stringbuilder_out (out sb);
		
		if (sb.ToString () != "This is my message.  Isn't it nice?")
			return 1;  
		return 0;
	}

	public static int test_0_marshal_stringbuilder_out_unicode () {
		StringBuilder sb;
		mono_test_marshal_stringbuilder_out_unicode (out sb);

		if (sb.ToString () != "This is my message.  Isn't it nice?")
			return 1;  
		return 0;
	}

	public static int test_0_marshal_stringbuilder_ref () {
		StringBuilder sb = new StringBuilder ();
		sb.Append ("ABC");
		int res = mono_test_marshal_stringbuilder_ref (ref sb);
		if (res != 0)
			return 1;
		
		if (sb.ToString () != "This is my message.  Isn't it nice?")
			return 2;  
		return 0;
	}

	public static int test_0_marshal_empty_string_array () {
		return mono_test_marshal_empty_string_array (null);
	}

	public static int test_0_marshal_string_array () {
		return mono_test_marshal_string_array (new String [] { "ABC", "DEF" });
	}

	public static int test_0_marshal_unicode_string_array () {
		return mono_test_marshal_unicode_string_array (new String [] { "ABC", "DEF" }, new String [] { "ABC", "DEF" });
	}

	public static int test_0_marshal_stringbuilder_array () {
		StringBuilder sb1 = new StringBuilder ("ABC");
		StringBuilder sb2 = new StringBuilder ("DEF");

		int res = mono_test_marshal_stringbuilder_array (new StringBuilder [] { sb1, sb2 });
		if (res != 0)
			return res;
		if (sb1.ToString () != "DEF")
			return 5;
		if (sb2.ToString () != "ABC")
			return 6;
		return 0;
	}

	public static int test_0_last_error () {
		mono_test_last_error (5);
		if (Marshal.GetLastWin32Error () == 5)
			return 0;
		else
			return 1;
	}

	public static int test_0_entry_point_not_found () {

		try {
			mono_entry_point_not_found ();
			return 1;
		}
		catch (EntryPointNotFoundException) {
		}

		return 0;
	}

	public static int test_0_library_not_found () {

		try {
			mono_library_not_found ();
			return 1;
		}
		catch (DllNotFoundException) {
		}

		return 0;
	}

	/* Check that the runtime trims .dll from the library name */
	public static int test_0_trim_dll_from_name () {

		mono_test_marshal_char_2 ('A');

		return 0;
	}

	/* Check that the runtime adds lib to to the library name */
	public static int test_0_add_lib_to_name () {

		mono_test_marshal_char_3 ('A');

		return 0;
	}

	class C {
		public int i;
	}

	public static int test_0_asany () {
		if (mono_test_asany (5, 1) != 0)
			return 1;

		if (mono_test_asany ("ABC", 2) != 0)
			return 2;

		SimpleStruct2 ss2 = new  SimpleStruct2 ();
		ss2.b = true;
		ss2.d = "TEST";
		ss2.e = 99;
		ss2.f = 1.5;
		ss2.g = 42;
		ss2.h = 123L;

		if (mono_test_asany (ss2, 3) != 0)
			return 3;

		if (mono_test_asany_unicode ("ABC", 4) != 0)
			return 4;

		try {
			C c = new C ();
			c.i = 5;
			mono_test_asany (c, 0);
			return 5;
		}
		catch (ArgumentException) {
		}

		try {
			mono_test_asany (new Object (), 0);
			return 6;
		}
		catch (ArgumentException) {
		}

		return 0;
	}

	/* AsAny marshalling + [In, Out] */

	public static int test_0_asany_in () {
		// Struct
		AsAnyStruct str = new AsAnyStruct(1,2,3, "ABC");
		mono_test_asany_in (str);

		// Formatted Class
		AsAnyClass cls = new AsAnyClass(1,2,3, "ABC");
		mono_test_asany_in (cls);
		if ((cls.i != 1) || (cls.j != 2) || (cls.k != 3))
			return 1;

		// Boxed Struct
		object obj = new AsAnyStruct(1,2,3, "ABC");
		mono_test_asany_in (obj);
		str = (AsAnyStruct)obj;
		if ((str.i != 1) || (str.j != 2) || (str.k != 3))
			return 2;

		return 0;
	}

	public static int test_0_asany_out () {
		// Struct
		AsAnyStruct str = new AsAnyStruct(1,2,3, "ABC");
		mono_test_asany_out (str);

		// Formatted Class
		AsAnyClass cls = new AsAnyClass(1,2,3, "ABC");
		mono_test_asany_out (cls);
		if ((cls.i != 10) || (cls.j != 20) || (cls.k != 30))
			return 1;

		// Boxed Struct
		object obj = new AsAnyStruct(1,2,3, "ABC");
		mono_test_asany_out (obj);
		str = (AsAnyStruct)obj;
		if ((str.i != 10) || (str.j != 20) || (str.k != 30))
			return 2;

		return 0;
	}

	public static int test_0_asany_inout () {
		// Struct
		AsAnyStruct str = new AsAnyStruct(1,2,3, "ABC");
		mono_test_asany_inout (str);

		// Formatted Class
		AsAnyClass cls = new AsAnyClass(1,2,3, "ABC");
		mono_test_asany_inout (cls);
		if ((cls.i != 10) || (cls.j != 20) || (cls.k != 30))
			return 1;

		// Boxed Struct
		object obj = new AsAnyStruct(1,2,3, "ABC");
		mono_test_asany_inout (obj);
		str = (AsAnyStruct)obj;
		if ((str.i != 10) || (str.j != 20) || (str.k != 30))
			return 2;

		return 0;
	}

	/* Byref String Array */

	[DllImport ("libtest", EntryPoint="mono_test_marshal_byref_string_array")]
	public static extern int mono_test_marshal_byref_string_array (ref string[] data);

	public static int test_0_byref_string_array () {

		string[] arr = null;

		if (mono_test_marshal_byref_string_array (ref arr) != 0)
			return 1;

		arr = new string[] { "Alpha", "Beta", "Gamma" };

		if (mono_test_marshal_byref_string_array (ref arr) != 1)
			return 2;

		/* FIXME: Test returned array and out case */

		return 0;
	}

	/*
	 * AMD64 small structs-by-value tests.
	 */

	/* TEST 1: 16 byte long INTEGER struct */

	[StructLayout(LayoutKind.Sequential)]
	public struct Amd64Struct1 {
		public int i;
		public int j;
		public int k;
		public int l;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_amd64_pass_return_struct1")]
	public static extern Amd64Struct1 mono_test_marshal_amd64_pass_return_struct1 (Amd64Struct1 s);
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_amd64_pass_return_struct1_many_args")]
	public static extern Amd64Struct1 mono_test_marshal_amd64_pass_return_struct1_many_args (Amd64Struct1 s, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8);

	public static int test_0_amd64_struct1 () {
		Amd64Struct1 s = new Amd64Struct1 ();
		s.i = 5;
		s.j = -5;
		s.k = 0xffffff;
		s.l = 0xfffffff;

		Amd64Struct1 s2 = mono_test_marshal_amd64_pass_return_struct1 (s);

		return ((s2.i == 6) && (s2.j == -4) && (s2.k == 0x1000000) && (s2.l == 0x10000000)) ? 0 : 1;
	}

	public static int test_0_amd64_struct1_many_args () {
		Amd64Struct1 s = new Amd64Struct1 ();
		s.i = 5;
		s.j = -5;
		s.k = 0xffffff;
		s.l = 0xfffffff;

		Amd64Struct1 s2 = mono_test_marshal_amd64_pass_return_struct1_many_args (s, 1, 2, 3, 4, 5, 6, 7, 8);

		return ((s2.i == 6) && (s2.j == -4) && (s2.k == 0x1000000) && (s2.l == 0x10000000 + 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8)) ? 0 : 1;
	}

	/* TEST 2: 8 byte long INTEGER struct */

	[StructLayout(LayoutKind.Sequential)]
	public struct Amd64Struct2 {
		public int i;
		public int j;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_amd64_pass_return_struct2")]
	public static extern Amd64Struct2 mono_test_marshal_amd64_pass_return_struct2 (Amd64Struct2 s);

	public static int test_0_amd64_struct2 () {
		Amd64Struct2 s = new Amd64Struct2 ();
		s.i = 5;
		s.j = -5;

		Amd64Struct2 s2 = mono_test_marshal_amd64_pass_return_struct2 (s);

		return ((s2.i == 6) && (s2.j == -4)) ? 0 : 1;
	}

	/* TEST 3: 4 byte long INTEGER struct */

	[StructLayout(LayoutKind.Sequential)]
	public struct Amd64Struct3 {
		public int i;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_amd64_pass_return_struct3")]
	public static extern Amd64Struct3 mono_test_marshal_amd64_pass_return_struct3 (Amd64Struct3 s);

	public static int test_0_amd64_struct3 () {
		Amd64Struct3 s = new Amd64Struct3 ();
		s.i = -5;

		Amd64Struct3 s2 = mono_test_marshal_amd64_pass_return_struct3 (s);

		return (s2.i == -4) ? 0 : 1;
	}

	/* Test 4: 16 byte long FLOAT struct */

	[StructLayout(LayoutKind.Sequential)]
	public struct Amd64Struct4 {
		public double d1, d2;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_amd64_pass_return_struct4")]
	public static extern Amd64Struct4 mono_test_marshal_amd64_pass_return_struct4 (Amd64Struct4 s);

	public static int test_0_amd64_struct4 () {
		Amd64Struct4 s = new Amd64Struct4 ();
		s.d1 = 5.0;
		s.d2 = -5.0;

		Amd64Struct4 s2 = mono_test_marshal_amd64_pass_return_struct4 (s);

		return (s2.d1 == 6.0 && s2.d2 == -4.0) ? 0 : 1;
	}

	/*
	 * IA64 struct tests
	 */

	/* Test 5: Float HFA */

	[StructLayout(LayoutKind.Sequential)]
	public struct TestStruct5 {
		public float d1, d2;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_ia64_pass_return_struct5")]
	public static extern TestStruct5 mono_test_marshal_ia64_pass_return_struct5 (double d1, double d2, TestStruct5 s, int i, double f3, double f4);

	public static int test_0_ia64_struct5 () {
		TestStruct5 s = new TestStruct5 ();
		s.d1 = 5.0f;
		s.d2 = -5.0f;

		TestStruct5 s2 = mono_test_marshal_ia64_pass_return_struct5 (1.0, 2.0, s, 5, 3.0, 4.0);

		return (s2.d1 == 13.0 && s2.d2 == 7.0) ? 0 : 1;
	}

	/* Test 6: Double HFA */

	[StructLayout(LayoutKind.Sequential)]
	public struct TestStruct6 {
		public double d1, d2;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_ia64_pass_return_struct6")]
	public static extern TestStruct6 mono_test_marshal_ia64_pass_return_struct6 (double d1, double d2, TestStruct6 s, int i, double f3, double f4);

	public static int test_0_ia64_struct6 () {
		TestStruct6 s = new TestStruct6 ();
		s.d1 = 6.0;
		s.d2 = -6.0;

		TestStruct6 s2 = mono_test_marshal_ia64_pass_return_struct6 (1.0, 2.0, s, 3, 4.0, 5.0);

		return (s2.d1 == 12.0 && s2.d2 == 3.0) ? 0 : 1;
	}
	
	/* Blittable class */
	[DllImport("libtest")]
	private static extern BlittableClass TestBlittableClass (BlittableClass vl);

	public static int test_0_marshal_blittable_class () {
		BlittableClass v1 = new BlittableClass ();

		/* Since it is blittable, it looks like it is passed as in/out */
		BlittableClass v2 = TestBlittableClass (v1);

		if (v1.a != 2 || v1.b != 3)
			return 1;
		
		if (v2.a != 2 || v2.b != 3)
			return 2;

		// Test null
		BlittableClass v3 = TestBlittableClass (null);

		if (v3.a != 42 || v3.b != 43)
			return 3;
		
		return 0;
	}

	/*
	 * Generic structures
	 */

	[StructLayout(LayoutKind.Sequential)]
	public struct Amd64Struct1Gen<T> {
		public T i;
		public T j;
		public T k;
		public T l;
	}
	
	[DllImport ("libtest", EntryPoint="mono_test_marshal_amd64_pass_return_struct1")]
	public static extern Amd64Struct1Gen<int> mono_test_marshal_amd64_pass_return_struct1_gen (Amd64Struct1Gen<int> s);

	public static int test_0_amd64_struct1_gen () {
		Amd64Struct1Gen<int> s = new Amd64Struct1Gen<int> ();
		s.i = 5;
		s.j = -5;
		s.k = 0xffffff;
		s.l = 0xfffffff;

		Amd64Struct1Gen<int> s2 = mono_test_marshal_amd64_pass_return_struct1_gen (s);

		return ((s2.i == 6) && (s2.j == -4) && (s2.k == 0x1000000) && (s2.l == 0x10000000)) ? 0 : 1;
	}

	/*
	 * Other tests
	 */

	public static int test_0_marshal_byval_class () {
		SimpleObj obj0 = new SimpleObj ();
		obj0.str = "T1";
		obj0.i = 4;
		
		if (class_marshal_test0 (obj0) != 0)
			return 1;

		return 0;
	}

	public static int test_0_marshal_byval_class_null () {
		if (class_marshal_test4 (null) != 0)
			return 1;

		return 0;
	}

	public static int test_0_marshal_out_class () {
		SimpleObj obj1;

		class_marshal_test1 (out obj1);

		if (obj1.str != "ABC")
			return 1;

		if (obj1.i != 5)
			return 2;

		return 0;
	}

	public static int test_0_marshal_string () {
		return string_marshal_test0 ("TEST0");
	}

	public static int test_0_marshal_out_string () {
		string res;
		
		string_marshal_test1 (out res);

		if (res != "TEST1")
			return 1;

		return 0;
	}

	public static int test_0_marshal_byref_string () {
		string res = "TEST1";

		int r = string_marshal_test2 (ref res);
		if (r != 0)
			return 1;
		if (res != "TEST2")
			return 2;
		return 0;
	}

	public static int test_0_marshal_null_string () {
		return string_marshal_test3 (null);
	}

#if FALSE
	[DllImport ("libtest", EntryPoint="mono_test_stdcall_mismatch_1", CallingConvention=CallingConvention.StdCall)]
	public static extern int mono_test_stdcall_mismatch_1 (int a, int b, int c);

	/* Test mismatched called conventions, the native function is cdecl */
	public static int test_0_stdcall_mismatch_1 () {
		mono_test_stdcall_mismatch_1 (0, 1, 2);
		return 0;
	}

	[DllImport ("libtest", EntryPoint="mono_test_stdcall_mismatch_2", CallingConvention=CallingConvention.Cdecl)]
	public static extern int mono_test_stdcall_mismatch_2 (int a, int b, int c);

	/* Test mismatched called conventions, the native function is stdcall */
	public static int test_0_stdcall_mismatch_2 () {
		mono_test_stdcall_mismatch_2 (0, 1, 2);
		return 0;
	}
#endif

	[DllImport ("libtest", EntryPoint="mono_test_stdcall_name_mangling", CallingConvention=CallingConvention.StdCall)]
	public static extern int mono_test_stdcall_name_mangling (int a, int b, int c);

	public static int test_0_stdcall_name_mangling () {
		return mono_test_stdcall_name_mangling (0, 1, 2) == 3 ? 0 : 1;
	}

	/* Test multiple calls to stdcall wrapper, xamarin bug 30146 */
	public static int test_0_stdcall_many_calls () {
		for (int i=0; i<256; i++)
			mono_test_stdcall_name_mangling (0, 0, 0);
		return 0;
	}

	/* Float test */

	[DllImport ("libtest", EntryPoint="mono_test_marshal_pass_return_float")]
	public static extern float mono_test_marshal_pass_return_float (float f);

	public static int test_0_pass_return_float () {
		float f = mono_test_marshal_pass_return_float (1.5f);

		return (f == 2.5f) ? 0 : 1;
	}

	/*
	 * Pointers to structures can not be passed
	 */

	/* This seems to be allowed by MS in some cases */
	/*
	public struct CharInfo {
		public char Character;
		public short Attributes;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_struct")]
	public static unsafe extern int mono_test_marshal_ptr_to_struct (CharInfo *ptr);

	public static unsafe int test_0_marshal_ptr_to_struct () {
		CharInfo [] buffer = new CharInfo [1];
		fixed (CharInfo *ptr = &buffer [0]) {
			try {
				mono_test_marshal_ptr_to_struct (ptr);
				return 1;
			}
			catch (MarshalDirectiveException) {
				return 0;
			}
		}
		return 1;
	}
	*/

	/*
	 * LPWStr marshalling
	 */

	[DllImport("libtest", EntryPoint="test_lpwstr_marshal")]
	[return: MarshalAs(UnmanagedType.LPWStr)]
	private static extern string mono_test_marshal_lpwstr_marshal(
		[MarshalAs(UnmanagedType.LPWStr)] string s,
		int length );

	[DllImport("libtest", EntryPoint="test_lpwstr_marshal", CharSet=CharSet.Unicode)]
	private static extern string mono_test_marshal_lpwstr_marshal2(
		string s,
		int length );

	[DllImport("libtest", EntryPoint="test_lpwstr_marshal_out")]
	private static extern void mono_test_marshal_lpwstr_out_marshal(
		[MarshalAs(UnmanagedType.LPWStr)] out string s);

	[DllImport("libtest", EntryPoint="test_lpwstr_marshal_out", CharSet=CharSet.Unicode)]
	private static extern void mono_test_marshal_lpwstr_out_marshal2(
		out string s);

	public static int test_0_pass_return_lwpstr () {
		string s;
		
		mono_test_marshal_lpwstr_out_marshal (out s);

		if (s != "ABC")
			return 1;

		s = null;
		mono_test_marshal_lpwstr_out_marshal2 (out s);

		if (s != "ABC")
			return 2;
		
		return 0;		
	}

	public static int test_0_out_lwpstr () {
		string s = "ABC";
		
		string res = mono_test_marshal_lpwstr_marshal (s, s.Length);

		if (res != "ABC")
			return 1;

		string res2 = mono_test_marshal_lpwstr_marshal2 (s, s.Length);

		if (res2 != "ABC")
			return 2;
		
		return 0;		
	}

	/*
	 * Byref bool marshalling
	 */

	[DllImport("libtest")]
	extern static int marshal_test_ref_bool
	(
		int i, 
		[MarshalAs(UnmanagedType.I1)] ref bool b1, 
		[MarshalAs(UnmanagedType.VariantBool)] ref bool b2, 
		ref bool b3
	);

	public static int test_0_pass_byref_bool () {
		for (int i = 0; i < 8; i++)
		{
			bool b1 = (i & 4) != 0;
			bool b2 = (i & 2) != 0;
			bool b3 = (i & 1) != 0;
			bool orig_b1 = b1, orig_b2 = b2, orig_b3 = b3;
			if (marshal_test_ref_bool(i, ref b1, ref b2, ref b3) != 0)
				return 4 * i + 1;
			if (b1 != !orig_b1)
				return 4 * i + 2;
			if (b2 != !orig_b2)
				return 4 * i + 3;
			if (b3 != !orig_b3)
				return 4 * i + 4;
		}

		return 0;
	}

	/*
	 * Bool struct field marshalling
	 */

	struct BoolStruct
	{
		public int i;
		[MarshalAs(UnmanagedType.I1)] public bool b1;
		[MarshalAs(UnmanagedType.VariantBool)] public bool b2;
		public bool b3;
	}

	[DllImport("libtest")]
	extern static int marshal_test_bool_struct(ref BoolStruct s);

	public static int test_0_pass_bool_in_struct () {
		for (int i = 0; i < 8; i++)
		{
			BoolStruct s = new BoolStruct();
			s.i = i;
			s.b1 = (i & 4) != 0;
			s.b2 = (i & 2) != 0;
			s.b3 = (i & 1) != 0;
			BoolStruct orig = s;
			if (marshal_test_bool_struct(ref s) != 0)
				return 4 * i + 33;
			if (s.b1 != !orig.b1)
				return 4 * i + 34;
			if (s.b2 != !orig.b2)
				return 4 * i + 35;
			if (s.b3 != !orig.b3)
				return 4 * i + 36;
		}

		return 0;
	}

	/*
	 * Alignment of structs containing longs
	 */

	struct LongStruct2 {
		public long l;
	}

	struct LongStruct {
		public int i;
		public LongStruct2 l;
	}

	[DllImport("libtest")]
	extern static int mono_test_marshal_long_struct (ref LongStruct s);

	public static int test_47_pass_long_struct () {
		LongStruct s = new LongStruct ();
		s.i = 5;
		s.l = new LongStruct2 ();
		s.l.l = 42;

		return mono_test_marshal_long_struct (ref s);
	}

	/*
	 * Invoking pinvoke methods through delegates
	 */

	delegate int MyDelegate (string name);

	[DllImport ("libtest", EntryPoint="mono_test_puts_static")]
	public static extern int puts_static (string name);

	public static int test_0_invoke_pinvoke_through_delegate () {
		puts_static ("A simple Test for PInvoke 1");

		MyDelegate d = new MyDelegate (puts_static);
		d ("A simple Test for PInvoke 2");

		object [] args = {"A simple Test for PInvoke 3"};
		d.DynamicInvoke (args);

		return 0;
	}

	/*
	 * Missing virtual pinvoke methods
	 */

	public class T {

		public virtual object MyClone ()
		{
			return null;
		}
	}

	public class T2 : T {
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public override extern object MyClone ();
	}

	public static int test_0_missing_virtual_pinvoke_method () {
		T2 t = new T2 ();

		try {
			t.MyClone ();
		} catch (Exception ex) {
			return 0;
		}
		
		return 1;
	}

	/*
	 * Marshalling of type 'object'
	 */

	[DllImport ("libtest", EntryPoint="mono_test_marshal_class")]
	public static extern SimpleClass mono_test_marshal_object (int i, int j, int k, object ss, int l);

	public static int test_0_marshal_object () {
		try {
			mono_test_marshal_object (0, 0, 0, null, 0);
			return 1;
		} catch (Exception) {
			return 0;
		}
	}

	/*
	 * Marshalling of DateTime to OLE DATE (double)
	 */
	[DllImport ("libtest", EntryPoint="mono_test_marshal_date_time")]
	public static extern double mono_test_marshal_date_time (DateTime d, out DateTime d2);

	public static int test_0_marshal_date_time () {
		DateTime d = new DateTime (2009, 12, 6);
		DateTime d2;
		double d3 = mono_test_marshal_date_time (d, out d2);
		if (d3 != 40153.0)
			return 1;
		if (d2 != d)
			return 2;
		return 0;
	}

	/*
	 * Calling pinvoke functions dynamically using calli
	 */
	
	[DllImport("libtest")]
	private static extern IntPtr mono_test_marshal_lookup_symbol (string fileName);

	delegate void CalliDel (IntPtr a, int[] f);

	public static int test_0_calli_dynamic () {
		/* we need the cdecl version because the icall convention demands it under Windows */
		IntPtr func = mono_test_marshal_lookup_symbol ("mono_test_marshal_inout_array_cdecl");

		DynamicMethod dm = new DynamicMethod ("calli", typeof (void), new Type [] { typeof (IntPtr), typeof (int[]) });

		var il = dm.GetILGenerator ();
		var signature = SignatureHelper.GetMethodSigHelper (CallingConvention.Cdecl, typeof (void));

		il.Emit (OpCodes.Ldarg, 1);
		signature.AddArgument (typeof (byte[]));

		il.Emit (OpCodes.Ldarg_0);

		il.Emit (OpCodes.Calli, signature);
		il.Emit (OpCodes.Ret);

		var f = (CalliDel)dm.CreateDelegate (typeof (CalliDel));

		int[] arr = new int [1000];
		for (int i = 0; i < 50; ++i)
			arr [i] = (int)i;
		f (func, arr);
		if (arr.Length != 1000)
			return 1;
		for (int i = 0; i < 50; ++i)
			if (arr [i] != 50 - i)
				return 2;

		return 0;
	}


	/*char array marshaling */
	[DllImport ("libtest", EntryPoint="mono_test_marshal_ansi_char_array", CharSet=CharSet.Ansi)]
	public static extern int mono_test_marshal_ansi_char_array (char[] a1);

	public static int test_0_marshal_ansi_char_array () {
		char[] buf = new char [32];
		buf [0] = 'q';
		buf [1] = 'w';
		buf [2] = 'e';
		buf [3] = 'r';

		if (mono_test_marshal_ansi_char_array (buf) != 0)
			return 1;

		string s = new string (buf);
		if (s.StartsWith ("qwer"))
			return 0;
		else
			return 2;
	}

	/*char array marshaling */
	[DllImport ("libtest", EntryPoint="mono_test_marshal_unicode_char_array", CharSet=CharSet.Unicode)]
	public static extern int mono_test_marshal_unicode_char_array (char[] a1);

	public static int test_0_marshal_unicode_char_array () {
		char[] buf = new char [32];
		buf [0] = 'q';
		buf [1] = 'w';
		buf [2] = 'e';
		buf [3] = 'r';

		if (mono_test_marshal_unicode_char_array (buf) != 0)
			return 1;

		string s = new string (buf);
		if (s.StartsWith ("abcdef"))
			return 0;
		else
			return 2;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_lpstr")]
	public static extern int mono_test_marshal_lpstr ([MarshalAs(UnmanagedType.LPStr)] string str);

	public static int test_0_mono_test_marshal_lpstr () {
		string str = "ABC";

		if (mono_test_marshal_lpstr (str) != 0)
			return 1;

		return 0;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_lpwstr")]
	public static extern int mono_test_marshal_lpwstr ([MarshalAs(UnmanagedType.LPWStr)] string str);

	public static int test_0_mono_test_marshal_lpwstr () {
		string str = "ABC";

		if (mono_test_marshal_lpwstr (str) != 0)
			return 1;

		return 0;
	}


	[method: DllImport ("libtest", EntryPoint="mono_test_marshal_return_lpstr")]
	[return: MarshalAs(UnmanagedType.LPStr)]
	public static extern string mono_test_marshal_return_lpstr ();

	public static int test_0_mono_test_marshal_return_lpstr () {
		string str = mono_test_marshal_return_lpstr ();
		if ("XYZ" == str)
			return 0;

		return 1;
	}

	[method: DllImport ("libtest", EntryPoint="mono_test_marshal_return_lpwstr")]
	[return: MarshalAs(UnmanagedType.LPWStr)]
	public static extern string mono_test_marshal_return_lpwstr ();

	public static int test_0_mono_test_marshal_return_lpwstr () {
		string str = mono_test_marshal_return_lpwstr ();
		if ("XYZ" == str)
			return 0;

		return 1;
	}

	[DllImport ("libtest", EntryPoint="mono_test_has_thiscall")]
	public static extern int mono_test_has_thiscall ();

	[DllImport ("libtest", EntryPoint = "_mono_test_native_thiscall1", CallingConvention=CallingConvention.ThisCall)]
	public static extern int mono_test_native_thiscall (int a);

	[DllImport ("libtest", EntryPoint = "_mono_test_native_thiscall2", CallingConvention=CallingConvention.ThisCall)]
	public static extern int mono_test_native_thiscall (int a, int b);

	[DllImport ("libtest", EntryPoint = "_mono_test_native_thiscall3", CallingConvention=CallingConvention.ThisCall)]
	public static extern int mono_test_native_thiscall (int a, int b, int c);

	[DllImport ("libtest", EntryPoint = "_mono_test_native_thiscall1", CallingConvention=CallingConvention.ThisCall)]
	public static extern int mono_test_native_thiscall (TinyStruct a);

	[DllImport ("libtest", EntryPoint = "_mono_test_native_thiscall2", CallingConvention=CallingConvention.ThisCall)]
	public static extern int mono_test_native_thiscall (TinyStruct a, int b);

	[DllImport ("libtest", EntryPoint = "_mono_test_native_thiscall3", CallingConvention=CallingConvention.ThisCall)]
	public static extern int mono_test_native_thiscall (TinyStruct a, int b, int c);

	public static int test_0_native_thiscall ()
	{
		if (mono_test_has_thiscall () == 0)
			return 0;

		if (mono_test_native_thiscall (1968329802) != 1968329802)
			return 1;

		if (mono_test_native_thiscall (268894549, 1212675791) != 1481570339)
			return 2;

		if (mono_test_native_thiscall (1288082683, -421187449, -1733670329) != -866775098)
			return 3;

		if (mono_test_native_thiscall (new TinyStruct(1968329802)) != 1968329802)
			return 4;

		if (mono_test_native_thiscall (new TinyStruct(268894549), 1212675791) != 1481570339)
			return 5;

		if (mono_test_native_thiscall (new TinyStruct(1288082683), -421187449, -1733670329) != -866775098)
			return 6;

		return 0;
	}

	[DllImport ("libtest", EntryPoint = "mono_test_marshal_return_single_double_struct")]
	public static extern SingleDoubleStruct mono_test_marshal_return_single_double_struct ();

	public static int test_0_x86_single_double_struct_ret () {
		double d = mono_test_marshal_return_single_double_struct ().d;
		if (d != 3.0)
			return 1;
		else
			return 0;
	}

    [StructLayout(LayoutKind.Explicit, Size = 12)]
	public struct FixedArrayStruct {
        [FieldOffset(0)]
        public fixed int array[3];
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_fixed_array")]
	public static extern int mono_test_marshal_fixed_array (FixedArrayStruct s);

	public static unsafe int test_6_fixed_array_struct () {
		var s = new FixedArrayStruct ();
		s.array [0] = 1;
		s.array [1] = 2;
		s.array [2] = 3;

		return mono_test_marshal_fixed_array (s);
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_pointer_array")]
	public static extern int mono_test_marshal_pointer_array (int*[] arr);

	public static unsafe int test_0_pointer_array () {
		var arr = new int [10];
		for (int i = 0; i < arr.Length; i++)
			arr [i] = -1;
		var arr2 = new int*[10];
		for (int i = 0; i < arr2.Length; i++) {
			GCHandle handle = GCHandle.Alloc(arr[i], GCHandleType.Pinned);
			fixed (int *ptr = &arr[i]) {
				arr2[i] = ptr;
			}
		}
		return mono_test_marshal_pointer_array (arr2);
	}

    [StructLayout(LayoutKind.Sequential)]
	public struct FixedBufferChar {
        public fixed char array[16];
		public char c;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_fixed_buffer_char")]
	public static extern int mono_test_marshal_fixed_buffer_char (ref FixedBufferChar s);

	public static unsafe int test_0_fixed_buffer_char () {
		var s = new FixedBufferChar ();
		s.array [0] = 'A';
		s.array [1] = 'B';
		s.array [2] = 'C';
		s.c = 'D';

		int res = mono_test_marshal_fixed_buffer_char (ref s);
		if (res != 0)
			return 1;
		if (s.array [0] != 'E' || s.array [1] != 'F' || s.c != 'G')
			return 2;
		return 0;
	}

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct FixedBufferUnicode {
        public fixed char array[16];
		public char c;
	}

	[DllImport ("libtest", EntryPoint="mono_test_marshal_fixed_buffer_unicode")]
	public static extern int mono_test_marshal_fixed_buffer_unicode (ref FixedBufferUnicode s);

	public static unsafe int test_0_fixed_buffer_unicode () {
		var s = new FixedBufferUnicode ();
		s.array [0] = 'A';
		s.array [1] = 'B';
		s.array [2] = 'C';
		s.c = 'D';

		int res = mono_test_marshal_fixed_buffer_unicode (ref s);
		if (res != 0)
			return 1;
		if (s.array [0] != 'E' || s.array [1] != 'F' || s.c != 'G')
			return 2;
		return 0;
	}
}

