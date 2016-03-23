// pinvoke_ppcc.cs - Test cases for passing structures to and and returning
//                   structures from functions.  This particular test is for
//                   structures consisting wholy of 1 byte fields.
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


public class Test_sbyte {

	[DllImport ("libtest", EntryPoint="mono_return_sbyte1")]
	public static extern sbyte1 mono_return_sbyte1 (sbyte1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte1 {
		public sbyte f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte2")]
	public static extern sbyte2 mono_return_sbyte2 (sbyte2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte2 {
		public sbyte f1,f2;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte3")]
	public static extern sbyte3 mono_return_sbyte3 (sbyte3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte3 {
		public sbyte f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte4")]
	public static extern sbyte4 mono_return_sbyte4 (sbyte4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte4 {
		public sbyte f1,f2,f3,f4;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte5")]
	public static extern sbyte5 mono_return_sbyte5 (sbyte5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte5 {
		public sbyte f1,f2,f3,f4,f5;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte6")]
	public static extern sbyte6 mono_return_sbyte6 (sbyte6 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte6 {
		public sbyte f1,f2,f3,f4,f5,f6;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte7")]
	public static extern sbyte7 mono_return_sbyte7 (sbyte7 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte7 {
		public sbyte f1,f2,f3,f4,f5,f6,f7;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte8")]
	public static extern sbyte8 mono_return_sbyte8 (sbyte8 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte8 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte9")]
	public static extern sbyte9 mono_return_sbyte9 (sbyte9 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte9 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte10")]
	public static extern sbyte10 mono_return_sbyte10 (sbyte10 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte10 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte11")]
	public static extern sbyte11 mono_return_sbyte11 (sbyte11 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte11 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte12")]
	public static extern sbyte12 mono_return_sbyte12 (sbyte12 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte12 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte13")]
	public static extern sbyte13 mono_return_sbyte13 (sbyte13 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte13 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte14")]
	public static extern sbyte14 mono_return_sbyte14 (sbyte14 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte14 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte15")]
	public static extern sbyte15 mono_return_sbyte15 (sbyte15 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte15 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte16")]
	public static extern sbyte16 mono_return_sbyte16 (sbyte16 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte16 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15,f16;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_sbyte17")]
	public static extern sbyte17 mono_return_sbyte17 (sbyte17 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte17 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15,f16,f17;
	}

	// This structure has nested structures within it but they are
	//  homogenous and thus should still use the special rules.
	public struct sbyte16_nested1 {
		public sbyte f1;
	};
	public struct sbyte16_nested2 {
		public sbyte f16;
	};
	[DllImport ("libtest", EntryPoint="mono_return_sbyte16_nested")]
	public static extern sbyte16_nested mono_return_sbyte16_nested (sbyte16_nested s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte16_nested {
		public sbyte16_nested1 nested1;
		public sbyte f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15;
		public sbyte16_nested2 nested2;
	}

	public static int Main (string[] args) {

		sbyte1 s1;
		s1.f1 = 1;
		s1 = mono_return_sbyte1(s1, 9);

		if (s1.f1 != 1+9) {
			Console.WriteLine("   sbyte1 s1.f1: got {0} but expected {1}", s1.f1, 1+9);
			return 1;
		}

		sbyte2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		s2 = mono_return_sbyte2(s2, 9);
		if (s2.f1 != 1+9) {
			Console.WriteLine("   sbyte2 s2.f1: got {0} but expected {1}", s2.f1, 1+9);
			return 1;
		}
		if (s2.f2 != 2+9) {
			Console.WriteLine("   sbyte2 s2.f2: got {0} but expected {1}", s2.f2, 2+9);
			return 2;
		}

		sbyte3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		s3 = mono_return_sbyte3(s3, 9);
		if (s3.f1 != 1+9) {
			Console.WriteLine("   sbyte3 s3.f1: got {0} but expected {1}", s3.f1, 1+9);
			return 1;
		}
		if (s3.f2 != 2+9) {
			Console.WriteLine("   sbyte3 s3.f2: got {0} but expected {1}", s3.f2, 2+9);
			return 2;
		}
		if (s3.f3 != 3+9) {
			Console.WriteLine("   sbyte3 s3.f3: got {0} but expected {1}", s3.f3, 3+9);
			return 3;
		}

		sbyte4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		s4 = mono_return_sbyte4(s4, 9);
		if (s4.f1 != 1+9) {
			Console.WriteLine("   sbyte4 s4.f1: got {0} but expected {1}", s4.f1, 1+9);
			return 1;
		}
		if (s4.f2 != 2+9) {
			Console.WriteLine("   sbyte4 s4.f2: got {0} but expected {1}", s4.f2, 2+9);
			return 2;
		}
		if (s4.f3 != 3+9) {
			Console.WriteLine("   sbyte4 s4.f3: got {0} but expected {1}", s4.f3, 3+9);
			return 3;
		}
		if (s4.f4 != 4+9) {
			Console.WriteLine("   sbyte4 s4.f4: got {0} but expected {1}", s4.f4, 4+9);
			return 4;
		}

		sbyte5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		s5 = mono_return_sbyte5(s5, 9);
		if (s5.f1 != 1+9) {
			Console.WriteLine("   sbyte5 s5.f1: got {0} but expected {1}", s5.f1, 1+9);
			return 1;
		}
		if (s5.f2 != 2+9) {
			Console.WriteLine("   sbyte5 s5.f2: got {0} but expected {1}", s5.f2, 2+9);
			return 2;
		}
		if (s5.f3 != 3+9) {
			Console.WriteLine("   sbyte5 s5.f3: got {0} but expected {1}", s5.f3, 3+9);
			return 3;
		}
		if (s5.f4 != 4+9) {
			Console.WriteLine("   sbyte5 s5.f4: got {0} but expected {1}", s5.f4, 4+9);
			return 4;
		}
		if (s5.f5 != 5+9) {
			Console.WriteLine("   sbyte5 s5.f5: got {0} but expected {1}", s5.f5, 5+9);
			return 5;
		}

		sbyte6 s6;
		s6.f1 = 1;
		s6.f2 = 2;
		s6.f3 = 3;
		s6.f4 = 4;
		s6.f5 = 5;
		s6.f6 = 6;
		s6 = mono_return_sbyte6(s6, 9);
		if (s6.f1 != 1+9) {
			Console.WriteLine("   sbyte6 s6.f1: got {0} but expected {1}", s6.f1, 1+9);
			return 1;
		}
		if (s6.f2 != 2+9) {
			Console.WriteLine("   sbyte6 s6.f2: got {0} but expected {1}", s6.f2, 2+9);
			return 2;
		}
		if (s6.f3 != 3+9) {
			Console.WriteLine("   sbyte6 s6.f3: got {0} but expected {1}", s6.f3, 3+9);
			return 3;
		}
		if (s6.f4 != 4+9) {
			Console.WriteLine("   sbyte6 s6.f4: got {0} but expected {1}", s6.f4, 4+9);
			return 4;
		}
		if (s6.f5 != 5+9) {
			Console.WriteLine("   sbyte6 s6.f5: got {0} but expected {1}", s6.f5, 5+9);
			return 5;
		}
		if (s6.f6 != 6+9) {
			Console.WriteLine("   sbyte6 s6.f6: got {0} but expected {1}", s6.f6, 6+9);
			return 6;
		}

		sbyte7 s7;
		s7.f1 = 1;
		s7.f2 = 2;
		s7.f3 = 3;
		s7.f4 = 4;
		s7.f5 = 5;
		s7.f6 = 6;
		s7.f7 = 7;
		s7 = mono_return_sbyte7(s7, 9);
		if (s7.f1 != 1+9) {
			Console.WriteLine("   sbyte7 s7.f1: got {0} but expected {1}", s7.f1, 1+9);
			return 1;
		}
		if (s7.f2 != 2+9) {
			Console.WriteLine("   sbyte7 s7.f2: got {0} but expected {1}", s7.f2, 2+9);
			return 2;
		}
		if (s7.f3 != 3+9) {
			Console.WriteLine("   sbyte7 s7.f3: got {0} but expected {1}", s7.f3, 3+9);
			return 3;
		}
		if (s7.f4 != 4+9) {
			Console.WriteLine("   sbyte7 s7.f4: got {0} but expected {1}", s7.f4, 4+9);
			return 4;
		}
		if (s7.f5 != 5+9) {
			Console.WriteLine("   sbyte7 s7.f5: got {0} but expected {1}", s7.f5, 5+9);
			return 5;
		}
		if (s7.f6 != 6+9) {
			Console.WriteLine("   sbyte7 s7.f6: got {0} but expected {1}", s7.f6, 6+9);
			return 6;
		}
		if (s7.f7 != 7+9) {
			Console.WriteLine("   sbyte7 s7.f7: got {0} but expected {1}", s7.f7, 7+9);
			return 7;
		}

		sbyte8 s8;
		s8.f1 = 1;
		s8.f2 = 2;
		s8.f3 = 3;
		s8.f4 = 4;
		s8.f5 = 5;
		s8.f6 = 6;
		s8.f7 = 7;
		s8.f8 = 8;
		s8 = mono_return_sbyte8(s8, 9);
		if (s8.f1 != 1+9) {
			Console.WriteLine("   sbyte8 s8.f1: got {0} but expected {1}", s8.f1, 1+9);
			return 1;
		}
		if (s8.f2 != 2+9) {
			Console.WriteLine("   sbyte8 s8.f2: got {0} but expected {1}", s8.f2, 2+9);
			return 2;
		}
		if (s8.f3 != 3+9) {
			Console.WriteLine("   sbyte8 s8.f3: got {0} but expected {1}", s8.f3, 3+9);
			return 3;
		}
		if (s8.f4 != 4+9) {
			Console.WriteLine("   sbyte8 s8.f4: got {0} but expected {1}", s8.f4, 4+9);
			return 4;
		}
		if (s8.f5 != 5+9) {
			Console.WriteLine("   sbyte8 s8.f5: got {0} but expected {1}", s8.f5, 5+9);
			return 5;
		}
		if (s8.f6 != 6+9) {
			Console.WriteLine("   sbyte8 s8.f6: got {0} but expected {1}", s8.f6, 6+9);
			return 6;
		}
		if (s8.f7 != 7+9) {
			Console.WriteLine("   sbyte8 s8.f7: got {0} but expected {1}", s8.f7, 7+9);
			return 7;
		}
		if (s8.f8 != 8+9) {
			Console.WriteLine("   sbyte8 s8.f8: got {0} but expected {1}", s8.f8, 8+9);
			return 8;
		}

		sbyte9 s9;
		s9.f1 = 1;
		s9.f2 = 2;
		s9.f3 = 3;
		s9.f4 = 4;
		s9.f5 = 5;
		s9.f6 = 6;
		s9.f7 = 7;
		s9.f8 = 8;
		s9.f9 = 9;
		s9 = mono_return_sbyte9(s9, 9);
		if (s9.f1 != 1+9) {
			Console.WriteLine("   sbyte9 s9.f1: got {0} but expected {1}", s9.f1, 1+9);
			return 1;
		}
		if (s9.f2 != 2+9) {
			Console.WriteLine("   sbyte9 s9.f2: got {0} but expected {1}", s9.f2, 2+9);
			return 2;
		}
		if (s9.f3 != 3+9) {
			Console.WriteLine("   sbyte9 s9.f3: got {0} but expected {1}", s9.f3, 3+9);
			return 3;
		}
		if (s9.f4 != 4+9) {
			Console.WriteLine("   sbyte9 s9.f4: got {0} but expected {1}", s9.f4, 4+9);
			return 4;
		}
		if (s9.f5 != 5+9) {
			Console.WriteLine("   sbyte9 s9.f5: got {0} but expected {1}", s9.f5, 5+9);
			return 5;
		}
		if (s9.f6 != 6+9) {
			Console.WriteLine("   sbyte9 s9.f6: got {0} but expected {1}", s9.f6, 6+9);
			return 6;
		}
		if (s9.f7 != 7+9) {
			Console.WriteLine("   sbyte9 s9.f7: got {0} but expected {1}", s9.f7, 7+9);
			return 7;
		}
		if (s9.f8 != 8+9) {
			Console.WriteLine("   sbyte9 s9.f8: got {0} but expected {1}", s9.f8, 8+9);
			return 8;
		}
		if (s9.f9 != 9+9) {
			Console.WriteLine("   sbyte9 s9.f9: got {0} but expected {1}", s9.f9, 9+9);
			return 9;
		}

		sbyte10 s10;
		s10.f1 = 1;
		s10.f2 = 2;
		s10.f3 = 3;
		s10.f4 = 4;
		s10.f5 = 5;
		s10.f6 = 6;
		s10.f7 = 7;
		s10.f8 = 8;
		s10.f9 = 9;
		s10.f10 = 10;
		s10 = mono_return_sbyte10(s10, 9);
		if (s10.f1 != 1+9) {
			Console.WriteLine("   sbyte10 s10.f1: got {0} but expected {1}", s10.f1, 1+9);
			return 1;
		}
		if (s10.f2 != 2+9) {
			Console.WriteLine("   sbyte10 s10.f2: got {0} but expected {1}", s10.f2, 2+9);
			return 2;
		}
		if (s10.f3 != 3+9) {
			Console.WriteLine("   sbyte10 s10.f3: got {0} but expected {1}", s10.f3, 3+9);
			return 3;
		}
		if (s10.f4 != 4+9) {
			Console.WriteLine("   sbyte10 s10.f4: got {0} but expected {1}", s10.f4, 4+9);
			return 4;
		}
		if (s10.f5 != 5+9) {
			Console.WriteLine("   sbyte10 s10.f5: got {0} but expected {1}", s10.f5, 5+9);
			return 5;
		}
		if (s10.f6 != 6+9) {
			Console.WriteLine("   sbyte10 s10.f6: got {0} but expected {1}", s10.f6, 6+9);
			return 6;
		}
		if (s10.f7 != 7+9) {
			Console.WriteLine("   sbyte10 s10.f7: got {0} but expected {1}", s10.f7, 7+9);
			return 7;
		}
		if (s10.f8 != 8+9) {
			Console.WriteLine("   sbyte10 s10.f8: got {0} but expected {1}", s10.f8, 8+9);
			return 8;
		}
		if (s10.f9 != 9+9) {
			Console.WriteLine("   sbyte10 s10.f9: got {0} but expected {1}", s10.f9, 9+9);
			return 9;
		}
		if (s10.f10 != 10+9) {
			Console.WriteLine("   sbyte10 s10.f10: got {0} but expected {1}", s10.f10, 10+9);
			return 10;
		}

		sbyte11 s11;
		s11.f1 = 1;
		s11.f2 = 2;
		s11.f3 = 3;
		s11.f4 = 4;
		s11.f5 = 5;
		s11.f6 = 6;
		s11.f7 = 7;
		s11.f8 = 8;
		s11.f9 = 9;
		s11.f10 = 10;
		s11.f11 = 11;
		s11 = mono_return_sbyte11(s11, 9);
		if (s11.f1 != 1+9) {
			Console.WriteLine("   sbyte11 s11.f1: got {0} but expected {1}", s11.f1, 1+9);
			return 1;
		}
		if (s11.f2 != 2+9) {
			Console.WriteLine("   sbyte11 s11.f2: got {0} but expected {1}", s11.f2, 2+9);
			return 2;
		}
		if (s11.f3 != 3+9) {
			Console.WriteLine("   sbyte11 s11.f3: got {0} but expected {1}", s11.f3, 3+9);
			return 3;
		}
		if (s11.f4 != 4+9) {
			Console.WriteLine("   sbyte11 s11.f4: got {0} but expected {1}", s11.f4, 4+9);
			return 4;
		}
		if (s11.f5 != 5+9) {
			Console.WriteLine("   sbyte11 s11.f5: got {0} but expected {1}", s11.f5, 5+9);
			return 5;
		}
		if (s11.f6 != 6+9) {
			Console.WriteLine("   sbyte11 s11.f6: got {0} but expected {1}", s11.f6, 6+9);
			return 6;
		}
		if (s11.f7 != 7+9) {
			Console.WriteLine("   sbyte11 s11.f7: got {0} but expected {1}", s11.f7, 7+9);
			return 7;
		}
		if (s11.f8 != 8+9) {
			Console.WriteLine("   sbyte11 s11.f8: got {0} but expected {1}", s11.f8, 8+9);
			return 8;
		}
		if (s11.f9 != 9+9) {
			Console.WriteLine("   sbyte11 s11.f9: got {0} but expected {1}", s11.f9, 9+9);
			return 9;
		}
		if (s11.f10 != 10+9) {
			Console.WriteLine("   sbyte11 s11.f10: got {0} but expected {1}", s11.f10, 10+9);
			return 10;
		}
		if (s11.f11 != 11+9) {
			Console.WriteLine("   sbyte11 s11.f11: got {0} but expected {1}", s11.f11, 11+9);
			return 11;
		}

		sbyte12 s12;
		s12.f1 = 1;
		s12.f2 = 2;
		s12.f3 = 3;
		s12.f4 = 4;
		s12.f5 = 5;
		s12.f6 = 6;
		s12.f7 = 7;
		s12.f8 = 8;
		s12.f9 = 9;
		s12.f10 = 10;
		s12.f11 = 11;
		s12.f12 = 12;
		s12 = mono_return_sbyte12(s12, 9);
		if (s12.f1 != 1+9) {
			Console.WriteLine("   sbyte12 s12.f1: got {0} but expected {1}", s12.f1, 1+9);
			return 1;
		}
		if (s12.f2 != 2+9) {
			Console.WriteLine("   sbyte12 s12.f2: got {0} but expected {1}", s12.f2, 2+9);
			return 2;
		}
		if (s12.f3 != 3+9) {
			Console.WriteLine("   sbyte12 s12.f3: got {0} but expected {1}", s12.f3, 3+9);
			return 3;
		}
		if (s12.f4 != 4+9) {
			Console.WriteLine("   sbyte12 s12.f4: got {0} but expected {1}", s12.f4, 4+9);
			return 4;
		}
		if (s12.f5 != 5+9) {
			Console.WriteLine("   sbyte12 s12.f5: got {0} but expected {1}", s12.f5, 5+9);
			return 5;
		}
		if (s12.f6 != 6+9) {
			Console.WriteLine("   sbyte12 s12.f6: got {0} but expected {1}", s12.f6, 6+9);
			return 6;
		}
		if (s12.f7 != 7+9) {
			Console.WriteLine("   sbyte12 s12.f7: got {0} but expected {1}", s12.f7, 7+9);
			return 7;
		}
		if (s12.f8 != 8+9) {
			Console.WriteLine("   sbyte12 s12.f8: got {0} but expected {1}", s12.f8, 8+9);
			return 8;
		}
		if (s12.f9 != 9+9) {
			Console.WriteLine("   sbyte12 s12.f9: got {0} but expected {1}", s12.f9, 9+9);
			return 9;
		}
		if (s12.f10 != 10+9) {
			Console.WriteLine("   sbyte12 s12.f10: got {0} but expected {1}", s12.f10, 10+9);
			return 10;
		}
		if (s12.f11 != 11+9) {
			Console.WriteLine("   sbyte12 s12.f11: got {0} but expected {1}", s12.f11, 11+9);
			return 11;
		}
		if (s12.f12 != 12+9) {
			Console.WriteLine("   sbyte12 s12.f12: got {0} but expected {1}", s12.f12, 12+9);
			return 12;
		}

		sbyte13 s13;
		s13.f1 = 1;
		s13.f2 = 2;
		s13.f3 = 3;
		s13.f4 = 4;
		s13.f5 = 5;
		s13.f6 = 6;
		s13.f7 = 7;
		s13.f8 = 8;
		s13.f9 = 9;
		s13.f10 = 10;
		s13.f11 = 11;
		s13.f12 = 12;
		s13.f13 = 13;
		s13 = mono_return_sbyte13(s13, 9);
		if (s13.f1 != 1+9) {
			Console.WriteLine("   sbyte13 s13.f1: got {0} but expected {1}", s13.f1, 1+9);
			return 1;
		}
		if (s13.f2 != 2+9) {
			Console.WriteLine("   sbyte13 s13.f2: got {0} but expected {1}", s13.f2, 2+9);
			return 2;
		}
		if (s13.f3 != 3+9) {
			Console.WriteLine("   sbyte13 s13.f3: got {0} but expected {1}", s13.f3, 3+9);
			return 3;
		}
		if (s13.f4 != 4+9) {
			Console.WriteLine("   sbyte13 s13.f4: got {0} but expected {1}", s13.f4, 4+9);
			return 4;
		}
		if (s13.f5 != 5+9) {
			Console.WriteLine("   sbyte13 s13.f5: got {0} but expected {1}", s13.f5, 5+9);
			return 5;
		}
		if (s13.f6 != 6+9) {
			Console.WriteLine("   sbyte13 s13.f6: got {0} but expected {1}", s13.f6, 6+9);
			return 6;
		}
		if (s13.f7 != 7+9) {
			Console.WriteLine("   sbyte13 s13.f7: got {0} but expected {1}", s13.f7, 7+9);
			return 7;
		}
		if (s13.f8 != 8+9) {
			Console.WriteLine("   sbyte13 s13.f8: got {0} but expected {1}", s13.f8, 8+9);
			return 8;
		}
		if (s13.f9 != 9+9) {
			Console.WriteLine("   sbyte13 s13.f9: got {0} but expected {1}", s13.f9, 9+9);
			return 9;
		}
		if (s13.f10 != 10+9) {
			Console.WriteLine("   sbyte13 s13.f10: got {0} but expected {1}", s13.f10, 10+9);
			return 10;
		}
		if (s13.f11 != 11+9) {
			Console.WriteLine("   sbyte13 s13.f11: got {0} but expected {1}", s13.f11, 11+9);
			return 11;
		}
		if (s13.f12 != 12+9) {
			Console.WriteLine("   sbyte13 s13.f12: got {0} but expected {1}", s13.f12, 12+9);
			return 12;
		}
		if (s13.f13 != 13+9) {
			Console.WriteLine("   sbyte13 s13.f13: got {0} but expected {1}", s13.f13, 13+9);
			return 13;
		}

		sbyte14 s14;
		s14.f1 = 1;
		s14.f2 = 2;
		s14.f3 = 3;
		s14.f4 = 4;
		s14.f5 = 5;
		s14.f6 = 6;
		s14.f7 = 7;
		s14.f8 = 8;
		s14.f9 = 9;
		s14.f10 = 10;
		s14.f11 = 11;
		s14.f12 = 12;
		s14.f13 = 13;
		s14.f14 = 14;
		s14 = mono_return_sbyte14(s14, 9);
		if (s14.f1 != 1+9) {
			Console.WriteLine("   sbyte14 s14.f1: got {0} but expected {1}", s14.f1, 1+9);
			return 1;
		}
		if (s14.f2 != 2+9) {
			Console.WriteLine("   sbyte14 s14.f2: got {0} but expected {1}", s14.f2, 2+9);
			return 2;
		}
		if (s14.f3 != 3+9) {
			Console.WriteLine("   sbyte14 s14.f3: got {0} but expected {1}", s14.f3, 3+9);
			return 3;
		}
		if (s14.f4 != 4+9) {
			Console.WriteLine("   sbyte14 s14.f4: got {0} but expected {1}", s14.f4, 4+9);
			return 4;
		}
		if (s14.f5 != 5+9) {
			Console.WriteLine("   sbyte14 s14.f5: got {0} but expected {1}", s14.f5, 5+9);
			return 5;
		}
		if (s14.f6 != 6+9) {
			Console.WriteLine("   sbyte14 s14.f6: got {0} but expected {1}", s14.f6, 6+9);
			return 6;
		}
		if (s14.f7 != 7+9) {
			Console.WriteLine("   sbyte14 s14.f7: got {0} but expected {1}", s14.f7, 7+9);
			return 7;
		}
		if (s14.f8 != 8+9) {
			Console.WriteLine("   sbyte14 s14.f8: got {0} but expected {1}", s14.f8, 8+9);
			return 8;
		}
		if (s14.f9 != 9+9) {
			Console.WriteLine("   sbyte14 s14.f9: got {0} but expected {1}", s14.f9, 9+9);
			return 9;
		}
		if (s14.f10 != 10+9) {
			Console.WriteLine("   sbyte14 s14.f10: got {0} but expected {1}", s14.f10, 10+9);
			return 10;
		}
		if (s14.f11 != 11+9) {
			Console.WriteLine("   sbyte14 s14.f11: got {0} but expected {1}", s14.f11, 11+9);
			return 11;
		}
		if (s14.f12 != 12+9) {
			Console.WriteLine("   sbyte14 s14.f12: got {0} but expected {1}", s14.f12, 12+9);
			return 12;
		}
		if (s14.f13 != 13+9) {
			Console.WriteLine("   sbyte14 s14.f13: got {0} but expected {1}", s14.f13, 13+9);
			return 13;
		}
		if (s14.f14 != 14+9) {
			Console.WriteLine("   sbyte14 s14.f14: got {0} but expected {1}", s14.f14, 14+9);
			return 14;
		}

		sbyte15 s15;
		s15.f1 = 1;
		s15.f2 = 2;
		s15.f3 = 3;
		s15.f4 = 4;
		s15.f5 = 5;
		s15.f6 = 6;
		s15.f7 = 7;
		s15.f8 = 8;
		s15.f9 = 9;
		s15.f10 = 10;
		s15.f11 = 11;
		s15.f12 = 12;
		s15.f13 = 13;
		s15.f14 = 14;
		s15.f15 = 15;
		s15 = mono_return_sbyte15(s15, 9);
		if (s15.f1 != 1+9) {
			Console.WriteLine("   sbyte15 s15.f1: got {0} but expected {1}", s15.f1, 1+9);
			return 1;
		}
		if (s15.f2 != 2+9) {
			Console.WriteLine("   sbyte15 s15.f2: got {0} but expected {1}", s15.f2, 2+9);
			return 2;
		}
		if (s15.f3 != 3+9) {
			Console.WriteLine("   sbyte15 s15.f3: got {0} but expected {1}", s15.f3, 3+9);
			return 3;
		}
		if (s15.f4 != 4+9) {
			Console.WriteLine("   sbyte15 s15.f4: got {0} but expected {1}", s15.f4, 4+9);
			return 4;
		}
		if (s15.f5 != 5+9) {
			Console.WriteLine("   sbyte15 s15.f5: got {0} but expected {1}", s15.f5, 5+9);
			return 5;
		}
		if (s15.f6 != 6+9) {
			Console.WriteLine("   sbyte15 s15.f6: got {0} but expected {1}", s15.f6, 6+9);
			return 6;
		}
		if (s15.f7 != 7+9) {
			Console.WriteLine("   sbyte15 s15.f7: got {0} but expected {1}", s15.f7, 7+9);
			return 7;
		}
		if (s15.f8 != 8+9) {
			Console.WriteLine("   sbyte15 s15.f8: got {0} but expected {1}", s15.f8, 8+9);
			return 8;
		}
		if (s15.f9 != 9+9) {
			Console.WriteLine("   sbyte15 s15.f9: got {0} but expected {1}", s15.f9, 9+9);
			return 9;
		}
		if (s15.f10 != 10+9) {
			Console.WriteLine("   sbyte15 s15.f10: got {0} but expected {1}", s15.f10, 10+9);
			return 10;
		}
		if (s15.f11 != 11+9) {
			Console.WriteLine("   sbyte15 s15.f11: got {0} but expected {1}", s15.f11, 11+9);
			return 11;
		}
		if (s15.f12 != 12+9) {
			Console.WriteLine("   sbyte15 s15.f12: got {0} but expected {1}", s15.f12, 12+9);
			return 12;
		}
		if (s15.f13 != 13+9) {
			Console.WriteLine("   sbyte15 s15.f13: got {0} but expected {1}", s15.f13, 13+9);
			return 13;
		}
		if (s15.f14 != 14+9) {
			Console.WriteLine("   sbyte15 s15.f14: got {0} but expected {1}", s15.f14, 14+9);
			return 14;
		}
		if (s15.f15 != 15+9) {
			Console.WriteLine("   sbyte15 s15.f15: got {0} but expected {1}", s15.f15, 15+9);
			return 15;
		}

		sbyte16 s16;
		s16.f1 = 1;
		s16.f2 = 2;
		s16.f3 = 3;
		s16.f4 = 4;
		s16.f5 = 5;
		s16.f6 = 6;
		s16.f7 = 7;
		s16.f8 = 8;
		s16.f9 = 9;
		s16.f10 = 10;
		s16.f11 = 11;
		s16.f12 = 12;
		s16.f13 = 13;
		s16.f14 = 14;
		s16.f15 = 15;
		s16.f16 = 16;
		s16 = mono_return_sbyte16(s16, 9);
		if (s16.f1 != 1+9) {
			Console.WriteLine("   sbyte16 s16.f1: got {0} but expected {1}", s16.f1, 1+9);
			return 1;
		}
		if (s16.f2 != 2+9) {
			Console.WriteLine("   sbyte16 s16.f2: got {0} but expected {1}", s16.f2, 2+9);
			return 2;
		}
		if (s16.f3 != 3+9) {
			Console.WriteLine("   sbyte16 s16.f3: got {0} but expected {1}", s16.f3, 3+9);
			return 3;
		}
		if (s16.f4 != 4+9) {
			Console.WriteLine("   sbyte16 s16.f4: got {0} but expected {1}", s16.f4, 4+9);
			return 4;
		}
		if (s16.f5 != 5+9) {
			Console.WriteLine("   sbyte16 s16.f5: got {0} but expected {1}", s16.f5, 5+9);
			return 5;
		}
		if (s16.f6 != 6+9) {
			Console.WriteLine("   sbyte16 s16.f6: got {0} but expected {1}", s16.f6, 6+9);
			return 6;
		}
		if (s16.f7 != 7+9) {
			Console.WriteLine("   sbyte16 s16.f7: got {0} but expected {1}", s16.f7, 7+9);
			return 7;
		}
		if (s16.f8 != 8+9) {
			Console.WriteLine("   sbyte16 s16.f8: got {0} but expected {1}", s16.f8, 8+9);
			return 8;
		}
		if (s16.f9 != 9+9) {
			Console.WriteLine("   sbyte16 s16.f9: got {0} but expected {1}", s16.f9, 9+9);
			return 9;
		}
		if (s16.f10 != 10+9) {
			Console.WriteLine("   sbyte16 s16.f10: got {0} but expected {1}", s16.f10, 10+9);
			return 10;
		}
		if (s16.f11 != 11+9) {
			Console.WriteLine("   sbyte16 s16.f11: got {0} but expected {1}", s16.f11, 11+9);
			return 11;
		}
		if (s16.f12 != 12+9) {
			Console.WriteLine("   sbyte16 s16.f12: got {0} but expected {1}", s16.f12, 12+9);
			return 12;
		}
		if (s16.f13 != 13+9) {
			Console.WriteLine("   sbyte16 s16.f13: got {0} but expected {1}", s16.f13, 13+9);
			return 13;
		}
		if (s16.f14 != 14+9) {
			Console.WriteLine("   sbyte16 s16.f14: got {0} but expected {1}", s16.f14, 14+9);
			return 14;
		}
		if (s16.f15 != 15+9) {
			Console.WriteLine("   sbyte16 s16.f15: got {0} but expected {1}", s16.f15, 15+9);
			return 15;
		}
		if (s16.f16 != 16+9) {
			Console.WriteLine("   sbyte16 s16.f16: got {0} but expected {1}", s16.f16, 16+9);
			return 16;
		}

		sbyte17 s17;
		s17.f1 = 1;
		s17.f2 = 2;
		s17.f3 = 3;
		s17.f4 = 4;
		s17.f5 = 5;
		s17.f6 = 6;
		s17.f7 = 7;
		s17.f8 = 8;
		s17.f9 = 9;
		s17.f10 = 10;
		s17.f11 = 11;
		s17.f12 = 12;
		s17.f13 = 13;
		s17.f14 = 14;
		s17.f15 = 15;
		s17.f16 = 16;
		s17.f17 = 17;
		s17 = mono_return_sbyte17(s17, 9);
		if (s17.f1 != 1+9) {
			Console.WriteLine("   sbyte17 s17.f1: got {0} but expected {1}", s17.f1, 1+9);
			return 1;
		}
		if (s17.f2 != 2+9) {
			Console.WriteLine("   sbyte17 s17.f2: got {0} but expected {1}", s17.f2, 2+9);
			return 2;
		}
		if (s17.f3 != 3+9) {
			Console.WriteLine("   sbyte17 s17.f3: got {0} but expected {1}", s17.f3, 3+9);
			return 3;
		}
		if (s17.f4 != 4+9) {
			Console.WriteLine("   sbyte17 s17.f4: got {0} but expected {1}", s17.f4, 4+9);
			return 4;
		}
		if (s17.f5 != 5+9) {
			Console.WriteLine("   sbyte17 s17.f5: got {0} but expected {1}", s17.f5, 5+9);
			return 5;
		}
		if (s17.f6 != 6+9) {
			Console.WriteLine("   sbyte17 s17.f6: got {0} but expected {1}", s17.f6, 6+9);
			return 6;
		}
		if (s17.f7 != 7+9) {
			Console.WriteLine("   sbyte17 s17.f7: got {0} but expected {1}", s17.f7, 7+9);
			return 7;
		}
		if (s17.f8 != 8+9) {
			Console.WriteLine("   sbyte17 s17.f8: got {0} but expected {1}", s17.f8, 8+9);
			return 8;
		}
		if (s17.f9 != 9+9) {
			Console.WriteLine("   sbyte17 s17.f9: got {0} but expected {1}", s17.f9, 9+9);
			return 9;
		}
		if (s17.f10 != 10+9) {
			Console.WriteLine("   sbyte17 s17.f10: got {0} but expected {1}", s17.f10, 10+9);
			return 10;
		}
		if (s17.f11 != 11+9) {
			Console.WriteLine("   sbyte17 s17.f11: got {0} but expected {1}", s17.f11, 11+9);
			return 11;
		}
		if (s17.f12 != 12+9) {
			Console.WriteLine("   sbyte17 s17.f12: got {0} but expected {1}", s17.f12, 12+9);
			return 12;
		}
		if (s17.f13 != 13+9) {
			Console.WriteLine("   sbyte17 s17.f13: got {0} but expected {1}", s17.f13, 13+9);
			return 13;
		}
		if (s17.f14 != 14+9) {
			Console.WriteLine("   sbyte17 s17.f14: got {0} but expected {1}", s17.f14, 14+9);
			return 14;
		}
		if (s17.f15 != 15+9) {
			Console.WriteLine("   sbyte17 s17.f15: got {0} but expected {1}", s17.f15, 15+9);
			return 15;
		}
		if (s17.f16 != 16+9) {
			Console.WriteLine("   sbyte17 s17.f16: got {0} but expected {1}", s17.f16, 16+9);
			return 16;
		}
		if (s17.f17 != 17+9) {
			Console.WriteLine("   sbyte17 s17.f17: got {0} but expected {1}", s17.f17, 17+9);
			return 17;
		}


		sbyte16_nested sn16;
		sn16.nested1.f1 = 1;
		sn16.f2 = 2;
		sn16.f3 = 3;
		sn16.f4 = 4;
		sn16.f5 = 5;
		sn16.f6 = 6;
		sn16.f7 = 7;
		sn16.f8 = 8;
		sn16.f9 = 9;
		sn16.f10 = 10;
		sn16.f11 = 11;
		sn16.f12 = 12;
		sn16.f13 = 13;
		sn16.f14 = 14;
		sn16.f15 = 15;
		sn16.nested2.f16 = 16;
		sn16 = mono_return_sbyte16_nested(sn16, 9);
		if (sn16.nested1.f1 != 1+9) {
			Console.WriteLine("   sbyte16_nested sn16.nested1.f1: got {0} but expected {1}", sn16.nested1.f1, 1+9);
			return 1;
		}
		if (sn16.f2 != 2+9) {
			Console.WriteLine("   sbyte16_nested sn16.f2: got {0} but expected {1}", sn16.f2, 2+9);
			return 2;
		}
		if (sn16.f3 != 3+9) {
			Console.WriteLine("   sbyte16_nested sn16.f3: got {0} but expected {1}", sn16.f3, 3+9);
			return 3;
		}
		if (sn16.f4 != 4+9) {
			Console.WriteLine("   sbyte16_nested sn16.f4: got {0} but expected {1}", sn16.f4, 4+9);
			return 4;
		}
		if (sn16.f5 != 5+9) {
			Console.WriteLine("   sbyte16_nested sn16.f5: got {0} but expected {1}", sn16.f5, 5+9);
			return 5;
		}
		if (sn16.f6 != 6+9) {
			Console.WriteLine("   sbyte16_nested sn16.f6: got {0} but expected {1}", sn16.f6, 6+9);
			return 6;
		}
		if (sn16.f7 != 7+9) {
			Console.WriteLine("   sbyte16_nested sn16.f7: got {0} but expected {1}", sn16.f7, 7+9);
			return 7;
		}
		if (sn16.f8 != 8+9) {
			Console.WriteLine("   sbyte16_nested sn16.f8: got {0} but expected {1}", sn16.f8, 8+9);
			return 8;
		}
		if (sn16.f9 != 9+9) {
			Console.WriteLine("   sbyte16_nested sn16.f9: got {0} but expected {1}", sn16.f9, 9+9);
			return 9;
		}
		if (sn16.f10 != 10+9) {
			Console.WriteLine("   sbyte16_nested sn16.f10: got {0} but expected {1}", sn16.f10, 10+9);
			return 10;
		}
		if (sn16.f11 != 11+9) {
			Console.WriteLine("   sbyte16_nested sn16.f11: got {0} but expected {1}", sn16.f11, 11+9);
			return 11;
		}
		if (sn16.f12 != 12+9) {
			Console.WriteLine("   sbyte16_nested sn16.f12: got {0} but expected {1}", sn16.f12, 12+9);
			return 12;
		}
		if (sn16.f13 != 13+9) {
			Console.WriteLine("   sbyte16_nested sn16.f13: got {0} but expected {1}", sn16.f13, 13+9);
			return 13;
		}
		if (sn16.f14 != 14+9) {
			Console.WriteLine("   sbyte16_nested sn16.f14: got {0} but expected {1}", sn16.f14, 14+9);
			return 14;
		}
		if (sn16.f15 != 15+9) {
			Console.WriteLine("   sbyte16_nested sn16.f15: got {0} but expected {1}", sn16.f15, 15+9);
			return 15;
		}
		if (sn16.nested2.f16 != 16+9) {
			Console.WriteLine("   sbyte16_nested sn16.nested2.f16: got {0} but expected {1}", sn16.nested2.f16, 16+9);
			return 16;
		}

		return 0;
	} // end Main
} // end class Test_sbyte

