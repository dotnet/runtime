// pinvoke_ppcs.cs - Test cases for passing structures to and and returning
//                   structures from functions.  This particular test is for
//                   structures consisting wholy of 2 byte fields.
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


public class Test_short {

	[DllImport ("libtest", EntryPoint="mono_return_short1")]
	public static extern short1 mono_return_short1 (short1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short1 {
		public short f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_short2")]
	public static extern short2 mono_return_short2 (short2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short2 {
		public short f1,f2;
	}
	[DllImport ("libtest", EntryPoint="mono_return_short3")]
	public static extern short3 mono_return_short3 (short3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short3 {
		public short f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_short4")]
	public static extern short4 mono_return_short4 (short4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short4 {
		public short f1,f2,f3,f4;
	}
	[DllImport ("libtest", EntryPoint="mono_return_short5")]
	public static extern short5 mono_return_short5 (short5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short5 {
		public short f1,f2,f3,f4,f5;
	}
	[DllImport ("libtest", EntryPoint="mono_return_short6")]
	public static extern short6 mono_return_short6 (short6 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short6 {
		public short f1,f2,f3,f4,f5,f6;
	}
	[DllImport ("libtest", EntryPoint="mono_return_short7")]
	public static extern short7 mono_return_short7 (short7 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short7 {
		public short f1,f2,f3,f4,f5,f6,f7;
	}
	[DllImport ("libtest", EntryPoint="mono_return_short8")]
	public static extern short8 mono_return_short8 (short8 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short8 {
		public short f1,f2,f3,f4,f5,f6,f7,f8;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_short9")]
	public static extern short9 mono_return_short9 (short9 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short9 {
		public short f1,f2,f3,f4,f5,f6,f7,f8,f9;
	}

	// This structure has nested structures within it but they are
	//  homogenous and thus should still use the special rules.
	public struct short8_nested1 {
		public short f1;
	};
	public struct short8_nested2 {
		public short f8;
	};
	[DllImport ("libtest", EntryPoint="mono_return_short8_nested")]
	public static extern short8_nested mono_return_short8_nested (short8_nested s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct short8_nested {
		public short8_nested1 nested1;
		public short f2,f3,f4,f5,f6,f7;
		public short8_nested2 nested2;
	}

	public static int Main (string[] args) {

		short1 s1;
		s1.f1 = 1;
		s1 = mono_return_short1(s1, 90);
		if (s1.f1 != 1+90) {
			Console.WriteLine("   short1 s1.f1: got {0} but expected {1}", s1.f1, 1+90);
			return 1;
		}

		short2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		s2 = mono_return_short2(s2, 90);
		if (s2.f1 != 1+90) {
			Console.WriteLine("   short2 s2.f1: got {0} but expected {1}", s2.f1, 1+90);
			return 1;
		}
		if (s2.f2 != 2+90) {
			Console.WriteLine("   short2 s2.f2: got {0} but expected {1}", s2.f2, 2+90);
			return 2;
		}

		short3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		s3 = mono_return_short3(s3, 90);
		if (s3.f1 != 1+90) {
			Console.WriteLine("   short3 s3.f1: got {0} but expected {1}", s3.f1, 1+90);
			return 1;
		}
		if (s3.f2 != 2+90) {
			Console.WriteLine("   short3 s3.f2: got {0} but expected {1}", s3.f2, 2+90);
			return 2;
		}
		if (s3.f3 != 3+90) {
			Console.WriteLine("   short3 s3.f3: got {0} but expected {1}", s3.f3, 3+90);
			return 3;
		}

		short4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		s4 = mono_return_short4(s4, 90);
		if (s4.f1 != 1+90) {
			Console.WriteLine("   short4 s4.f1: got {0} but expected {1}", s4.f1, 1+90);
			return 1;
		}
		if (s4.f2 != 2+90) {
			Console.WriteLine("   short4 s4.f2: got {0} but expected {1}", s4.f2, 2+90);
			return 2;
		}
		if (s4.f3 != 3+90) {
			Console.WriteLine("   short4 s4.f3: got {0} but expected {1}", s4.f3, 3+90);
			return 3;
		}
		if (s4.f4 != 4+90) {
			Console.WriteLine("   short4 s4.f4: got {0} but expected {1}", s4.f4, 4+90);
			return 4;
		}

		short5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		s5 = mono_return_short5(s5, 90);
		if (s5.f1 != 1+90) {
			Console.WriteLine("   short5 s5.f1: got {0} but expected {1}", s5.f1, 1+90);
			return 1;
		}
		if (s5.f2 != 2+90) {
			Console.WriteLine("   short5 s5.f2: got {0} but expected {1}", s5.f2, 2+90);
			return 2;
		}
		if (s5.f3 != 3+90) {
			Console.WriteLine("   short5 s5.f3: got {0} but expected {1}", s5.f3, 3+90);
			return 3;
		}
		if (s5.f4 != 4+90) {
			Console.WriteLine("   short5 s5.f4: got {0} but expected {1}", s5.f4, 4+90);
			return 4;
		}
		if (s5.f5 != 5+90) {
			Console.WriteLine("   short5 s5.f5: got {0} but expected {1}", s5.f5, 5+90);
			return 5;
		}

		short6 s6;
		s6.f1 = 1;
		s6.f2 = 2;
		s6.f3 = 3;
		s6.f4 = 4;
		s6.f5 = 5;
		s6.f6 = 6;
		s6 = mono_return_short6(s6, 90);
		if (s6.f1 != 1+90) {
			Console.WriteLine("   short6 s6.f1: got {0} but expected {1}", s6.f1, 1+90);
			return 1;
		}
		if (s6.f2 != 2+90) {
			Console.WriteLine("   short6 s6.f2: got {0} but expected {1}", s6.f2, 2+90);
			return 2;
		}
		if (s6.f3 != 3+90) {
			Console.WriteLine("   short6 s6.f3: got {0} but expected {1}", s6.f3, 3+90);
			return 3;
		}
		if (s6.f4 != 4+90) {
			Console.WriteLine("   short6 s6.f4: got {0} but expected {1}", s6.f4, 4+90);
			return 4;
		}
		if (s6.f5 != 5+90) {
			Console.WriteLine("   short6 s6.f5: got {0} but expected {1}", s6.f5, 5+90);
			return 5;
		}
		if (s6.f6 != 6+90) {
			Console.WriteLine("   short6 s6.f6: got {0} but expected {1}", s6.f6, 6+90);
			return 6;
		}

		short7 s7;
		s7.f1 = 1;
		s7.f2 = 2;
		s7.f3 = 3;
		s7.f4 = 4;
		s7.f5 = 5;
		s7.f6 = 6;
		s7.f7 = 7;
		s7 = mono_return_short7(s7, 90);
		if (s7.f1 != 1+90) {
			Console.WriteLine("   short7 s7.f1: got {0} but expected {1}", s7.f1, 1+90);
			return 1;
		}
		if (s7.f2 != 2+90) {
			Console.WriteLine("   short7 s7.f2: got {0} but expected {1}", s7.f2, 2+90);
			return 2;
		}
		if (s7.f3 != 3+90) {
			Console.WriteLine("   short7 s7.f3: got {0} but expected {1}", s7.f3, 3+90);
			return 3;
		}
		if (s7.f4 != 4+90) {
			Console.WriteLine("   short7 s7.f4: got {0} but expected {1}", s7.f4, 4+90);
			return 4;
		}
		if (s7.f5 != 5+90) {
			Console.WriteLine("   short7 s7.f5: got {0} but expected {1}", s7.f5, 5+90);
			return 5;
		}
		if (s7.f6 != 6+90) {
			Console.WriteLine("   short7 s7.f6: got {0} but expected {1}", s7.f6, 6+90);
			return 6;
		}
		if (s7.f7 != 7+90) {
			Console.WriteLine("   short7 s7.f7: got {0} but expected {1}", s7.f7, 7+90);
			return 7;
		}

		short8 s8;
		s8.f1 = 1;
		s8.f2 = 2;
		s8.f3 = 3;
		s8.f4 = 4;
		s8.f5 = 5;
		s8.f6 = 6;
		s8.f7 = 7;
		s8.f8 = 8;
		s8 = mono_return_short8(s8, 90);
		if (s8.f1 != 1+90) {
			Console.WriteLine("   short8 s8.f1: got {0} but expected {1}", s8.f1, 1+90);
			return 1;
		}
		if (s8.f2 != 2+90) {
			Console.WriteLine("   short8 s8.f2: got {0} but expected {1}", s8.f2, 2+90);
			return 2;
		}
		if (s8.f3 != 3+90) {
			Console.WriteLine("   short8 s8.f3: got {0} but expected {1}", s8.f3, 3+90);
			return 3;
		}
		if (s8.f4 != 4+90) {
			Console.WriteLine("   short8 s8.f4: got {0} but expected {1}", s8.f4, 4+90);
			return 4;
		}
		if (s8.f5 != 5+90) {
			Console.WriteLine("   short8 s8.f5: got {0} but expected {1}", s8.f5, 5+90);
			return 5;
		}
		if (s8.f6 != 6+90) {
			Console.WriteLine("   short8 s8.f6: got {0} but expected {1}", s8.f6, 6+90);
			return 6;
		}
		if (s8.f7 != 7+90) {
			Console.WriteLine("   short8 s8.f7: got {0} but expected {1}", s8.f7, 7+90);
			return 7;
		}
		if (s8.f8 != 8+90) {
			Console.WriteLine("   short8 s8.f8: got {0} but expected {1}", s8.f8, 8+90);
			return 8;
		}

		short9 s9;
		s9.f1 = 1;
		s9.f2 = 2;
		s9.f3 = 3;
		s9.f4 = 4;
		s9.f5 = 5;
		s9.f6 = 6;
		s9.f7 = 7;
		s9.f8 = 8;
		s9.f9 = 9;
		s9 = mono_return_short9(s9, 90);
		if (s9.f1 != 1+90) {
			Console.WriteLine("   short9 s9.f1: got {0} but expected {1}", s9.f1, 1+90);
			return 1;
		}
		if (s9.f2 != 2+90) {
			Console.WriteLine("   short9 s9.f2: got {0} but expected {1}", s9.f2, 2+90);
			return 2;
		}
		if (s9.f3 != 3+90) {
			Console.WriteLine("   short9 s9.f3: got {0} but expected {1}", s9.f3, 3+90);
			return 3;
		}
		if (s9.f4 != 4+90) {
			Console.WriteLine("   short9 s9.f4: got {0} but expected {1}", s9.f4, 4+90);
			return 4;
		}
		if (s9.f5 != 5+90) {
			Console.WriteLine("   short9 s9.f5: got {0} but expected {1}", s9.f5, 5+90);
			return 5;
		}
		if (s9.f6 != 6+90) {
			Console.WriteLine("   short9 s9.f6: got {0} but expected {1}", s9.f6, 6+90);
			return 6;
		}
		if (s9.f7 != 7+90) {
			Console.WriteLine("   short9 s9.f7: got {0} but expected {1}", s9.f7, 7+90);
			return 7;
		}
		if (s9.f8 != 8+90) {
			Console.WriteLine("   short9 s9.f8: got {0} but expected {1}", s9.f8, 8+90);
			return 8;
		}
		if (s9.f9 != 9+90) {
			Console.WriteLine("   short9 s9.f9: got {0} but expected {1}", s9.f9, 9+90);
			return 9;
		}


		short8_nested sn8;
		sn8.nested1.f1 = 1;
		sn8.f2 = 2;
		sn8.f3 = 3;
		sn8.f4 = 4;
		sn8.f5 = 5;
		sn8.f6 = 6;
		sn8.f7 = 7;
		sn8.nested2.f8 = 8;
		sn8 = mono_return_short8_nested(sn8, 90);
		if (sn8.nested1.f1 != 1+90) {
			Console.WriteLine("   short8_nested sn8.nested1.f1: got {0} but expected {1}", sn8.nested1.f1, 1+90);
			return 1;
		}
		if (sn8.f2 != 2+90) {
			Console.WriteLine("   short8_nested sn8.f2: got {0} but expected {1}", sn8.f2, 2+90);
			return 2;
		}
		if (sn8.f3 != 3+90) {
			Console.WriteLine("   short8_nested sn8.f3: got {0} but expected {1}", sn8.f3, 3+90);
			return 3;
		}
		if (sn8.f4 != 4+90) {
			Console.WriteLine("   short8_nested sn8.f4: got {0} but expected {1}", sn8.f4, 4+90);
			return 4;
		}
		if (sn8.f5 != 5+90) {
			Console.WriteLine("   short8_nested sn8.f5: got {0} but expected {1}", sn8.f5, 5+90);
			return 5;
		}
		if (sn8.f6 != 6+90) {
			Console.WriteLine("   short8_nested sn8.f6: got {0} but expected {1}", sn8.f6, 6+90);
			return 6;
		}
		if (sn8.f7 != 7+90) {
			Console.WriteLine("   short8_nested sn8.f7: got {0} but expected {1}", sn8.f7, 7+90);
			return 7;
		}
		if (sn8.nested2.f8 != 8+90) {
			Console.WriteLine("   short8_nested sn8.nested2.f8: got {0} but expected {1}", sn8.nested2.f8, 8+90);
			return 8;
		}

		return 0;
	} // end Main
} // end class Test_short

