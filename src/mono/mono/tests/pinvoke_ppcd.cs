// pinvoke_ppcd.cs - Test cases for passing structures to and and returning
//                   structures from functions.  This particular test is for
//                   structures consisting wholy of 8 byte fields.
//
//                   The Power ABI version 2 allows for special parameter
//                   passing and returning optimizations for certain
//                   structures of homogenous composition (like all ints).
//                   This set of tests checks all the possible combinations
//                   that use the special parm/return rules and one beyond.
//
// Bill Seurer (seurer@linux.vnet.ibm.com)
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;


public class Test_double {

	[DllImport ("libtest", EntryPoint="mono_return_double1")]
	public static extern double1 mono_return_double1 (double1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double1 {
		public double f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double2")]
	public static extern double2 mono_return_double2 (double2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double2 {
		public double f1,f2;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_double3")]
	public static extern double3 mono_return_double3 (double3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double3 {
		public double f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double4")]
	public static extern double4 mono_return_double4 (double4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double4 {
		public double f1,f2,f3,f4;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double5")]
	public static extern double5 mono_return_double5 (double5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double5 {
		public double f1,f2,f3,f4,f5;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double6")]
	public static extern double6 mono_return_double6 (double6 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double6 {
		public double f1,f2,f3,f4,f5,f6;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double7")]
	public static extern double7 mono_return_double7 (double7 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double7 {
		public double f1,f2,f3,f4,f5,f6,f7;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double8")]
	public static extern double8 mono_return_double8 (double8 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double8 {
		public double f1,f2,f3,f4,f5,f6,f7,f8;
	}
	// This structure is 1 element too large to use the special parameter
	//  passing rules.
	[DllImport ("libtest", EntryPoint="mono_return_double9")]
	public static extern double9 mono_return_double9 (double9 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double9 {
		public double f1,f2,f3,f4,f5,f6,f7,f8,f9;
	}

	// This structure has nested structures within it but they are
	//  homogenous and thus should still use the special rules.
	public struct double2_nested1 {
		public double f1;
	};
	public struct double2_nested2 {
		public double f2;
	};
	[DllImport ("libtest", EntryPoint="mono_return_double2_nested")]
	public static extern double2_nested mono_return_double2_nested (double2_nested s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double2_nested {
		public double2_nested1 nested1;
		public double2_nested2 nested2;
	}

	[DllImport ("libtest", EntryPoint="mono_return_double_array4")]
	public static extern double_array4 mono_return_double_array4 (double_array4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct double_array4 {
		public fixed double f1[4];
	}


	public static int Main (string[] args) {
		double1 s1;
		s1.f1 = 1;
		s1 = mono_return_double1(s1, 9);
		if (s1.f1 != 1+9) {
			Console.WriteLine("   double1 s1.f1: got {0} but expected {1}", s1.f1, 1+9);
			return 1;
		}

		double2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		s2 = mono_return_double2(s2, 9);
		if (s2.f1 != 1+9) {
			Console.WriteLine("   double2 s2.f1: got {0} but expected {1}", s2.f1, 1+9);
			return 1;
		}
		if (s2.f2 != 2+9) {
			Console.WriteLine("   double2 s2.f2: got {0} but expected {1}", s2.f2, 2+9);
			return 2;
		}

		double3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		s3 = mono_return_double3(s3, 9);
		if (s3.f1 != 1+9) {
			Console.WriteLine("   double3 s3.f1: got {0} but expected {1}", s3.f1, 1+9);
			return 1;
		}
		if (s3.f2 != 2+9) {
			Console.WriteLine("   double3 s3.f2: got {0} but expected {1}", s3.f2, 2+9);
			return 2;
		}
		if (s3.f3 != 3+9) {
			Console.WriteLine("   double3 s3.f3: got {0} but expected {1}", s3.f3, 3+9);
			return 3;
		}

		double4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		s4 = mono_return_double4(s4, 9);
		if (s4.f1 != 1+9) {
			Console.WriteLine("   double4 s4.f1: got {0} but expected {1}", s4.f1, 1+9);
			return 1;
		}
		if (s4.f2 != 2+9) {
			Console.WriteLine("   double4 s4.f2: got {0} but expected {1}", s4.f2, 2+9);
			return 2;
		}
		if (s4.f3 != 3+9) {
			Console.WriteLine("   double4 s4.f3: got {0} but expected {1}", s4.f3, 3+9);
			return 3;
		}
		if (s4.f4 != 4+9) {
			Console.WriteLine("   double4 s4.f4: got {0} but expected {1}", s4.f4, 4+9);
			return 4;
		}

		double5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		s5 = mono_return_double5(s5, 9);
		if (s5.f1 != 1+9) {
			Console.WriteLine("   double5 s5.f1: got {0} but expected {1}", s5.f1, 1+9);
			return 1;
		}
		if (s5.f2 != 2+9) {
			Console.WriteLine("   double5 s5.f2: got {0} but expected {1}", s5.f2, 2+9);
			return 2;
		}
		if (s5.f3 != 3+9) {
			Console.WriteLine("   double5 s5.f3: got {0} but expected {1}", s5.f3, 3+9);
			return 3;
		}
		if (s5.f4 != 4+9) {
			Console.WriteLine("   double5 s5.f4: got {0} but expected {1}", s5.f4, 4+9);
			return 4;
		}
		if (s5.f5 != 5+9) {
			Console.WriteLine("   double5 s5.f5: got {0} but expected {1}", s5.f5, 5+9);
			return 5;
		}

		double6 s6;
		s6.f1 = 1;
		s6.f2 = 2;
		s6.f3 = 3;
		s6.f4 = 4;
		s6.f5 = 5;
		s6.f6 = 6;
		s6 = mono_return_double6(s6, 9);
		if (s6.f1 != 1+9) {
			Console.WriteLine("   double6 s6.f1: got {0} but expected {1}", s6.f1, 1+9);
			return 1;
		}
		if (s6.f2 != 2+9) {
			Console.WriteLine("   double6 s6.f2: got {0} but expected {1}", s6.f2, 2+9);
			return 2;
		}
		if (s6.f3 != 3+9) {
			Console.WriteLine("   double6 s6.f3: got {0} but expected {1}", s6.f3, 3+9);
			return 3;
		}
		if (s6.f4 != 4+9) {
			Console.WriteLine("   double6 s6.f4: got {0} but expected {1}", s6.f4, 4+9);
			return 4;
		}
		if (s6.f5 != 5+9) {
			Console.WriteLine("   double6 s6.f5: got {0} but expected {1}", s6.f5, 5+9);
			return 5;
		}
		if (s6.f6 != 6+9) {
			Console.WriteLine("   double6 s6.f6: got {0} but expected {1}", s6.f6, 6+9);
			return 6;
		}

		double7 s7;
		s7.f1 = 1;
		s7.f2 = 2;
		s7.f3 = 3;
		s7.f4 = 4;
		s7.f5 = 5;
		s7.f6 = 6;
		s7.f7 = 7;
		s7 = mono_return_double7(s7, 9);
		if (s7.f1 != 1+9) {
			Console.WriteLine("   double7 s7.f1: got {0} but expected {1}", s7.f1, 1+9);
			return 1;
		}
		if (s7.f2 != 2+9) {
			Console.WriteLine("   double7 s7.f2: got {0} but expected {1}", s7.f2, 2+9);
			return 2;
		}
		if (s7.f3 != 3+9) {
			Console.WriteLine("   double7 s7.f3: got {0} but expected {1}", s7.f3, 3+9);
			return 3;
		}
		if (s7.f4 != 4+9) {
			Console.WriteLine("   double7 s7.f4: got {0} but expected {1}", s7.f4, 4+9);
			return 4;
		}
		if (s7.f5 != 5+9) {
			Console.WriteLine("   double7 s7.f5: got {0} but expected {1}", s7.f5, 5+9);
			return 5;
		}
		if (s7.f6 != 6+9) {
			Console.WriteLine("   double7 s7.f6: got {0} but expected {1}", s7.f6, 6+9);
			return 6;
		}
		if (s7.f7 != 7+9) {
			Console.WriteLine("   double7 s7.f7: got {0} but expected {1}", s7.f7, 7+9);
			return 7;
		}

		double8 s8;
		s8.f1 = 1;
		s8.f2 = 2;
		s8.f3 = 3;
		s8.f4 = 4;
		s8.f5 = 5;
		s8.f6 = 6;
		s8.f7 = 7;
		s8.f8 = 8;
		s8 = mono_return_double8(s8, 9);
		if (s8.f1 != 1+9) {
			Console.WriteLine("   double8 s8.f1: got {0} but expected {1}", s8.f1, 1+9);
			return 1;
		}
		if (s8.f2 != 2+9) {
			Console.WriteLine("   double8 s8.f2: got {0} but expected {1}", s8.f2, 2+9);
			return 2;
		}
		if (s8.f3 != 3+9) {
			Console.WriteLine("   double8 s8.f3: got {0} but expected {1}", s8.f3, 3+9);
			return 3;
		}
		if (s8.f4 != 4+9) {
			Console.WriteLine("   double8 s8.f4: got {0} but expected {1}", s8.f4, 4+9);
			return 4;
		}
		if (s8.f5 != 5+9) {
			Console.WriteLine("   double8 s8.f5: got {0} but expected {1}", s8.f5, 5+9);
			return 5;
		}
		if (s8.f6 != 6+9) {
			Console.WriteLine("   double8 s8.f6: got {0} but expected {1}", s8.f6, 6+9);
			return 6;
		}
		if (s8.f7 != 7+9) {
			Console.WriteLine("   double8 s8.f7: got {0} but expected {1}", s8.f7, 7+9);
			return 7;
		}
		if (s8.f8 != 8+9) {
			Console.WriteLine("   double8 s8.f8: got {0} but expected {1}", s8.f8, 8+9);
			return 8;
		}

		double9 s9;
		s9.f1 = 1;
		s9.f2 = 2;
		s9.f3 = 3;
		s9.f4 = 4;
		s9.f5 = 5;
		s9.f6 = 6;
		s9.f7 = 7;
		s9.f8 = 8;
		s9.f9 = 9;
		s9 = mono_return_double9(s9, 9);
		if (s9.f1 != 1+9) {
			Console.WriteLine("   double9 s9.f1: got {0} but expected {1}", s9.f1, 1+9);
			return 1;
		}
		if (s9.f2 != 2+9) {
			Console.WriteLine("   double9 s9.f2: got {0} but expected {1}", s9.f2, 2+9);
			return 2;
		}
		if (s9.f3 != 3+9) {
			Console.WriteLine("   double9 s9.f3: got {0} but expected {1}", s9.f3, 3+9);
			return 3;
		}
		if (s9.f4 != 4+9) {
			Console.WriteLine("   double9 s9.f4: got {0} but expected {1}", s9.f4, 4+9);
			return 4;
		}
		if (s9.f5 != 5+9) {
			Console.WriteLine("   double9 s9.f5: got {0} but expected {1}", s9.f5, 5+9);
			return 5;
		}
		if (s9.f6 != 6+9) {
			Console.WriteLine("   double9 s9.f6: got {0} but expected {1}", s9.f6, 6+9);
			return 6;
		}
		if (s9.f7 != 7+9) {
			Console.WriteLine("   double9 s9.f7: got {0} but expected {1}", s9.f7, 7+9);
			return 7;
		}
		if (s9.f8 != 8+9) {
			Console.WriteLine("   double9 s9.f8: got {0} but expected {1}", s9.f8, 8+9);
			return 8;
		}
		if (s9.f9 != 9+9) {
			Console.WriteLine("   double9 s9.f9: got {0} but expected {1}", s9.f9, 9+9);
			return 9;
		}


		double2_nested sn2;
		sn2.nested1.f1 = 1;
		sn2.nested2.f2 = 2;
		sn2 = mono_return_double2_nested(sn2, 9);
		if (sn2.nested1.f1 != 1+9) {
			Console.WriteLine("   double2_nested sn2.nested1.f1: got {0} but expected {1}", sn2.nested1.f1, 1+9);
			return 1;
		}
		if (sn2.nested2.f2 != 2+9) {
			Console.WriteLine("   double2_nested sn2.nested2.f2: got {0} but expected {1}", sn2.nested2.f2, 2+9);
			return 2;
		}

/*
//  NOTE: this test does not work properly because mini_type_is_hfa in mini-codegen.c does not handle arrays.
//        Uncomment this when mini_type_is_hfa is fixed.
		unsafe {
		double_array4 sa4;
		sa4.f1[0] = 1;
		sa4.f1[1] = 2;
		sa4 = mono_return_double_array4(sa4, 9);
		if (sa4.f1[0] != 1+9) {
			Console.WriteLine("   double_array4 sa4.f1[0]: got {0} but expected {1}", sa4.f1[0], 1+9);
			return 1;
		}
		if (sa4.f1[1] != 2+9) {
			Console.WriteLine("   double_array4 sa4.f1[1]: got {0} but expected {1}", sa4.f1[1], 2+9);
			return 2;
		}
		if (sa4.f1[2] != 3+9) {
			Console.WriteLine("   double_array4 sa4.f1[2]: got {0} but expected {1}", sa4.f1[2], 3+9);
			return 3;
		}
		if (sa4.f1[3] != 4+9) {
			Console.WriteLine("   double_array4 sa4.f1[3]: got {0} but expected {1}", sa4.f1[3], 4+9);
			return 4;
		}
		}
*/

		return 0;
	} // end Main
} // end class Test_double

