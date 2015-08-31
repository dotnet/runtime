// pinvoke_ppci.cs - Test cases for passing structures to and and returning
//                   structures from functions.  This particular test is for
//                   structures consisting wholy of 4 byte fields.
//
//                   The Power ABI version 2 allows for special parameter
//                   passing and returning optimizations for certain
//                   structures of homogenous composition (like all ints).
//                   This set of tests checks all the possible combinations
//                   that use the special parm/return rules and one beyond.
//
// Bill Seurer (seurer@linux.vnet.ibm.com)
//
// (C) {Copyright holder}
//

using System;
using System.Runtime.InteropServices;


public class Test_int {

	[DllImport ("libtest", EntryPoint="mono_return_int1")]
	public static extern int mono_return_int1 (int1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int1 {
		public int f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_int2")]
	public static extern int mono_return_int2 (int2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int2 {
		public int f1,f2;
	}
	[DllImport ("libtest", EntryPoint="mono_return_int3")]
	public static extern int mono_return_int3 (int3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int3 {
		public int f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_int4")]
	public static extern int mono_return_int4 (int4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int4 {
		public int f1,f2,f3,f4;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_int5")]
	public static extern int mono_return_int5 (int5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int5 {
		public int f1,f2,f3,f4,f5;
	}

	// This structure has nested structures within it but they are
	//  homogenous and thus should still use the special rules.
	public struct int4_nested1 {
		public int f1;
	};
	public struct int4_nested2 {
		public int f4;
	};
	[DllImport ("libtest", EntryPoint="mono_return_int4_nested")]
	public static extern int4_nested mono_return_int4_nested (int4_nested s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int4_nested {
		public int4_nested1 nested1;
		public int f2,f3;
		public int4_nested2 nested2;
	}

	public static int Main (string[] args) {

		int1 s1;
		s1.f1 = 1;
		int retval1 = mono_return_int1(s1, 906);
		if (retval1 != 2*906) {
			Console.WriteLine("   int1 retval1: got {0} but expected {1}", retval1, 2*906);
			return 1;
		}

		int2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		int retval2 = mono_return_int2(s2, 906);
		if (retval2 != 2*906) {
			Console.WriteLine("   int2 retval2: got {0} but expected {1}", retval2, 2*906);
			return 1;
		}

		int3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		int retval3 = mono_return_int3(s3, 906);
		if (retval3 != 2*906) {
			Console.WriteLine("   int3 retval3: got {0} but expected {1}", retval3, 2*906);
			return 1;
		}

		int4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		int retval4 = mono_return_int4(s4, 906);
		if (retval4 != 2*906) {
			Console.WriteLine("   int4 retval4: got {0} but expected {1}", retval4, 2*906);
			return 1;
		}

		int5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		int retval5 = mono_return_int5(s5, 906);
		if (retval5 != 2*906) {
			Console.WriteLine("   int5 retval5: got {0} but expected {1}", retval5, 2*906);
			return 1;
		}


		return 0;
	} // end Main
} // end class Test_int





