// pinvoke_ppci.cs - Test cases for passing structures to and and returning
//                   structures from functions.  This particular test is for
//                   structures consisting wholy of 4 byte fields.
//
//                   The Power ABI version 2 allows for special parameter
//                   passing and returning optimizations for certain
//                   structures of homogeneous composition (like all ints).
//                   This set of tests checks all the possible combinations
//                   that use the special parm/return rules and one beyond.
//
// Bill Seurer (seurer@linux.vnet.ibm.com)
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;


public class Test_int {

	[DllImport ("libtest", EntryPoint="mono_return_int1")]
	public static extern int1 mono_return_int1 (int1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int1 {
		public int f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_int2")]
	public static extern int2 mono_return_int2 (int2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int2 {
		public int f1,f2;
	}
	[DllImport ("libtest", EntryPoint="mono_return_int3")]
	public static extern int3 mono_return_int3 (int3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int3 {
		public int f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_int4")]
	public static extern int4 mono_return_int4 (int4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int4 {
		public int f1,f2,f3,f4;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_int5")]
	public static extern int5 mono_return_int5 (int5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct int5 {
		public int f1,f2,f3,f4,f5;
	}

	// This structure has nested structures within it but they are
	//  homogeneous and thus should still use the special rules.
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
		s1 = mono_return_int1(s1, 906);
		if (s1.f1 != 1+906) {
			Console.WriteLine("   int1 s1.f1: got {0} but expected {1}", s1.f1, 1+906);
			return 1;
		}

		int2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		s2 = mono_return_int2(s2, 906);
		if (s2.f1 != 1+906) {
			Console.WriteLine("   int2 s2.f1: got {0} but expected {1}", s2.f1, 1+906);
			return 1;
		}
		if (s2.f2 != 2+906) {
			Console.WriteLine("   int2 s2.f2: got {0} but expected {1}", s2.f2, 2+906);
			return 2;
		}

		int3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		s3 = mono_return_int3(s3, 906);
		if (s3.f1 != 1+906) {
			Console.WriteLine("   int3 s3.f1: got {0} but expected {1}", s3.f1, 1+906);
			return 1;
		}
		if (s3.f2 != 2+906) {
			Console.WriteLine("   int3 s3.f2: got {0} but expected {1}", s3.f2, 2+906);
			return 2;
		}
		if (s3.f3 != 3+906) {
			Console.WriteLine("   int3 s3.f3: got {0} but expected {1}", s3.f3, 3+906);
			return 3;
		}

		int4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		s4 = mono_return_int4(s4, 906);
		if (s4.f1 != 1+906) {
			Console.WriteLine("   int4 s4.f1: got {0} but expected {1}", s4.f1, 1+906);
			return 1;
		}
		if (s4.f2 != 2+906) {
			Console.WriteLine("   int4 s4.f2: got {0} but expected {1}", s4.f2, 2+906);
			return 2;
		}
		if (s4.f3 != 3+906) {
			Console.WriteLine("   int4 s4.f3: got {0} but expected {1}", s4.f3, 3+906);
			return 3;
		}
		if (s4.f4 != 4+906) {
			Console.WriteLine("   int4 s4.f4: got {0} but expected {1}", s4.f4, 4+906);
			return 4;
		}

		int5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		s5 = mono_return_int5(s5, 906);
		if (s5.f1 != 1+906) {
			Console.WriteLine("   int5 s5.f1: got {0} but expected {1}", s5.f1, 1+906);
			return 1;
		}
		if (s5.f2 != 2+906) {
			Console.WriteLine("   int5 s5.f2: got {0} but expected {1}", s5.f2, 2+906);
			return 2;
		}
		if (s5.f3 != 3+906) {
			Console.WriteLine("   int5 s5.f3: got {0} but expected {1}", s5.f3, 3+906);
			return 3;
		}
		if (s5.f4 != 4+906) {
			Console.WriteLine("   int5 s5.f4: got {0} but expected {1}", s5.f4, 4+906);
			return 4;
		}
		if (s5.f5 != 5+906) {
			Console.WriteLine("   int5 s5.f5: got {0} but expected {1}", s5.f5, 5+906);
			return 5;
		}


		int4_nested sn4;
		sn4.nested1.f1 = 1;
		sn4.f2 = 2;
		sn4.f3 = 3;
		sn4.nested2.f4 = 4;
		sn4 = mono_return_int4_nested(sn4, 906);
		if (sn4.nested1.f1 != 1+906) {
			Console.WriteLine("   int4_nested sn4.nested1.f1: got {0} but expected {1}", sn4.nested1.f1, 1+906);
			return 1;
		}
		if (sn4.f2 != 2+906) {
			Console.WriteLine("   int4_nested sn4.f2: got {0} but expected {1}", sn4.f2, 2+906);
			return 2;
		}
		if (sn4.f3 != 3+906) {
			Console.WriteLine("   int4_nested sn4.f3: got {0} but expected {1}", sn4.f3, 3+906);
			return 3;
		}
		if (sn4.nested2.f4 != 4+906) {
			Console.WriteLine("   int4_nested sn4.nested2.f4: got {0} but expected {1}", sn4.nested2.f4, 4+906);
			return 4;
		}

		return 0;
	} // end Main
} // end class Test_int

