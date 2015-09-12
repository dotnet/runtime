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
// (C) {Copyright holder}
//

using System;
using System.Runtime.InteropServices;


public class Test_sbyte {

	[DllImport ("libtest", EntryPoint="mono_return_sbyte1")]
	public static extern sbyte mono_return_sbyte1 (sbyte1 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte1 {
		public sbyte f1;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte2")]
	public static extern sbyte mono_return_sbyte2 (sbyte2 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte2 {
		public sbyte f1,f2;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte3")]
	public static extern sbyte mono_return_sbyte3 (sbyte3 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte3 {
		public sbyte f1,f2,f3;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte4")]
	public static extern sbyte mono_return_sbyte4 (sbyte4 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte4 {
		public sbyte f1,f2,f3,f4;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte5")]
	public static extern sbyte mono_return_sbyte5 (sbyte5 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte5 {
		public sbyte f1,f2,f3,f4,f5;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte6")]
	public static extern sbyte mono_return_sbyte6 (sbyte6 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte6 {
		public sbyte f1,f2,f3,f4,f5,f6;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte7")]
	public static extern sbyte mono_return_sbyte7 (sbyte7 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte7 {
		public sbyte f1,f2,f3,f4,f5,f6,f7;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte8")]
	public static extern sbyte mono_return_sbyte8 (sbyte8 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte8 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte9")]
	public static extern sbyte mono_return_sbyte9 (sbyte9 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte9 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte10")]
	public static extern sbyte mono_return_sbyte10 (sbyte10 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte10 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte11")]
	public static extern sbyte mono_return_sbyte11 (sbyte11 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte11 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte12")]
	public static extern sbyte mono_return_sbyte12 (sbyte12 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte12 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte13")]
	public static extern sbyte mono_return_sbyte13 (sbyte13 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte13 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte14")]
	public static extern sbyte mono_return_sbyte14 (sbyte14 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte14 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte15")]
	public static extern sbyte mono_return_sbyte15 (sbyte15 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte15 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15;
	}
	[DllImport ("libtest", EntryPoint="mono_return_sbyte16")]
	public static extern sbyte mono_return_sbyte16 (sbyte16 s, int addend);
	[StructLayout(LayoutKind.Sequential)]
	public struct sbyte16 {
		public sbyte f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15,f16;
	}
	// This structure is 1 element too large to use the special return
	//  rules.
	[DllImport ("libtest", EntryPoint="mono_return_sbyte17")]
	public static extern sbyte mono_return_sbyte17 (sbyte17 s, int addend);
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
		sbyte retval1 = mono_return_sbyte1(s1, 9);
		if (retval1 != 2*9) {
			Console.WriteLine("   sbyte1 retval1: got {0} but expected {1}", retval1, 2*9);
			return 1;
		}

		sbyte2 s2;
		s2.f1 = 1;
		s2.f2 = 2;
		sbyte retval2 = mono_return_sbyte2(s2, 9);
		if (retval2 != 2*9) {
			Console.WriteLine("   sbyte2 retval2: got {0} but expected {1}", retval2, 2*9);
			return 1;
		}

		sbyte3 s3;
		s3.f1 = 1;
		s3.f2 = 2;
		s3.f3 = 3;
		sbyte retval3 = mono_return_sbyte3(s3, 9);
		if (retval3 != 2*9) {
			Console.WriteLine("   sbyte3 retval3: got {0} but expected {1}", retval3, 2*9);
			return 1;
		}

		sbyte4 s4;
		s4.f1 = 1;
		s4.f2 = 2;
		s4.f3 = 3;
		s4.f4 = 4;
		sbyte retval4 = mono_return_sbyte4(s4, 9);
		if (retval4 != 2*9) {
			Console.WriteLine("   sbyte4 retval4: got {0} but expected {1}", retval4, 2*9);
			return 1;
		}

		sbyte5 s5;
		s5.f1 = 1;
		s5.f2 = 2;
		s5.f3 = 3;
		s5.f4 = 4;
		s5.f5 = 5;
		sbyte retval5 = mono_return_sbyte5(s5, 9);
		if (retval5 != 2*9) {
			Console.WriteLine("   sbyte5 retval5: got {0} but expected {1}", retval5, 2*9);
			return 1;
		}

		sbyte6 s6;
		s6.f1 = 1;
		s6.f2 = 2;
		s6.f3 = 3;
		s6.f4 = 4;
		s6.f5 = 5;
		s6.f6 = 6;
		sbyte retval6 = mono_return_sbyte6(s6, 9);
		if (retval6 != 2*9) {
			Console.WriteLine("   sbyte6 retval6: got {0} but expected {1}", retval6, 2*9);
			return 1;
		}

		sbyte7 s7;
		s7.f1 = 1;
		s7.f2 = 2;
		s7.f3 = 3;
		s7.f4 = 4;
		s7.f5 = 5;
		s7.f6 = 6;
		s7.f7 = 7;
		sbyte retval7 = mono_return_sbyte7(s7, 9);
		if (retval7 != 2*9) {
			Console.WriteLine("   sbyte7 retval7: got {0} but expected {1}", retval7, 2*9);
			return 1;
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
		sbyte retval8 = mono_return_sbyte8(s8, 9);
		if (retval8 != 2*9) {
			Console.WriteLine("   sbyte8 retval8: got {0} but expected {1}", retval8, 2*9);
			return 1;
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
		sbyte retval9 = mono_return_sbyte9(s9, 9);
		if (retval9 != 2*9) {
			Console.WriteLine("   sbyte9 retval9: got {0} but expected {1}", retval9, 2*9);
			return 1;
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
		sbyte retval10 = mono_return_sbyte10(s10, 9);
		if (retval10 != 2*9) {
			Console.WriteLine("   sbyte10 retval10: got {0} but expected {1}", retval10, 2*9);
			return 1;
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
		sbyte retval11 = mono_return_sbyte11(s11, 9);
		if (retval11 != 2*9) {
			Console.WriteLine("   sbyte11 retval11: got {0} but expected {1}", retval11, 2*9);
			return 1;
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
		sbyte retval12 = mono_return_sbyte12(s12, 9);
		if (retval12 != 2*9) {
			Console.WriteLine("   sbyte12 retval12: got {0} but expected {1}", retval12, 2*9);
			return 1;
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
		sbyte retval13 = mono_return_sbyte13(s13, 9);
		if (retval13 != 2*9) {
			Console.WriteLine("   sbyte13 retval13: got {0} but expected {1}", retval13, 2*9);
			return 1;
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
		sbyte retval14 = mono_return_sbyte14(s14, 9);
		if (retval14 != 2*9) {
			Console.WriteLine("   sbyte14 retval14: got {0} but expected {1}", retval14, 2*9);
			return 1;
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
		sbyte retval15 = mono_return_sbyte15(s15, 9);
		if (retval15 != 2*9) {
			Console.WriteLine("   sbyte15 retval15: got {0} but expected {1}", retval15, 2*9);
			return 1;
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
		sbyte retval16 = mono_return_sbyte16(s16, 9);
		if (retval16 != 2*9) {
			Console.WriteLine("   sbyte16 retval16: got {0} but expected {1}", retval16, 2*9);
			return 1;
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
		sbyte retval17 = mono_return_sbyte17(s17, 9);
		if (retval17 != 2*9) {
			Console.WriteLine("   sbyte17 retval17: got {0} but expected {1}", retval17, 2*9);
			return 1;
		}


		return 0;
	} // end Main
} // end class Test_sbyte





