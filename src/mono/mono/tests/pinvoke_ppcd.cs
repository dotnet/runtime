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
// (C) {Copyright holder}
//

using System;
using System.Runtime.InteropServices;


public class Test_double {

	[DllImport ("libtest", EntryPoint="mono_return_double1")]
	public static extern double mono_return_double1 (double1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double1 {
		public double f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double2")]
	public static extern double mono_return_double2 (double2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double2 {
		public double f1,f2;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_double3")]
	public static extern double mono_return_double3 (double3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double3 {
		public double f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double4")]
	public static extern double mono_return_double4 (double4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double4 {
		public double f1,f2,f3,f4;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double5")]
	public static extern double mono_return_double5 (double5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double5 {
		public double f1,f2,f3,f4,f5;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double6")]
	public static extern double mono_return_double6 (double6 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double6 {
		public double f1,f2,f3,f4,f5,f6;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double7")]
	public static extern double mono_return_double7 (double7 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double7 {
		public double f1,f2,f3,f4,f5,f6,f7;
	}
	[DllImport ("libtest", EntryPoint="mono_return_double8")]
	public static extern double mono_return_double8 (double8 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct double8 {
		public double f1,f2,f3,f4,f5,f6,f7,f8;
	}
	// This structure is 1 element too large to use the special parameter
	//  passing rules.
	[DllImport ("libtest", EntryPoint="mono_return_double9")]
	public static extern double mono_return_double9 (double9 s, int addend);
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

	public static int Main (string[] args) {

		double1 s1;
		s1.f1 = 1;
		double retval1 = mono_return_double1(s1, 9);
		if (retval1 != 2*9) {
			Console.WriteLine("   double1 retval1: got {0} but expected {1}", retval1, 2*9);
			return 1;
		}

		double2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		double retval2 = mono_return_double2(s2, 9);
		if (retval2 != 2*9) {
			Console.WriteLine("   double2 retval2: got {0} but expected {1}", retval2, 2*9);
			return 1;
		}

		double3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		double retval3 = mono_return_double3(s3, 9);
		if (retval3 != 2*9) {
			Console.WriteLine("   double3 retval3: got {0} but expected {1}", retval3, 2*9);
			return 1;
		}

		double4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		double retval4 = mono_return_double4(s4, 9);
		if (retval4 != 2*9) {
			Console.WriteLine("   double4 retval4: got {0} but expected {1}", retval4, 2*9);
			return 1;
		}

		double5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		double retval5 = mono_return_double5(s5, 9);
		if (retval5 != 2*9) {
			Console.WriteLine("   double5 retval5: got {0} but expected {1}", retval5, 2*9);
			return 1;
		}

		double6 s6;
		s6.f1 = 1;
		s6.f2 = 2;
		s6.f3 = 3;
		s6.f4 = 4;
		s6.f5 = 5;
		s6.f6 = 6;
		double retval6 = mono_return_double6(s6, 9);
		if (retval6 != 2*9) {
			Console.WriteLine("   double6 retval6: got {0} but expected {1}", retval6, 2*9);
			return 1;
		}

		double7 s7;
		s7.f1 = 1;
		s7.f2 = 2;
		s7.f3 = 3;
		s7.f4 = 4;
		s7.f5 = 5;
		s7.f6 = 6;
		s7.f7 = 7;
		double retval7 = mono_return_double7(s7, 9);
		if (retval7 != 2*9) {
			Console.WriteLine("   double7 retval7: got {0} but expected {1}", retval7, 2*9);
			return 1;
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
		double retval8 = mono_return_double8(s8, 9);
		if (retval8 != 2*9) {
			Console.WriteLine("   double8 retval8: got {0} but expected {1}", retval8, 2*9);
			return 1;
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
		double retval9 = mono_return_double9(s9, 9);
		if (retval9 != 2*9) {
			Console.WriteLine("   double9 retval9: got {0} but expected {1}", retval9, 2*9);
			return 1;
		}


		return 0;
	} // end Main
} // end class Test_double





