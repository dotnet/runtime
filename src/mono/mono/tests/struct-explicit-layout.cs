using System;
using System.Runtime.InteropServices;

namespace Test {
	[StructLayout ( LayoutKind.Explicit )]
	public struct ST0 {
		[FieldOffset(0)] public short S0;
		[FieldOffset(2)] public int I0;
		[FieldOffset(6)] public long L0;
		[FieldOffset(14)] public float F0;
		[FieldOffset(18)] public double D0;
	}

	public class Test {
		public static int Main() {
			ST0 s0, s1;
			s0 = s1 = new ST0();
			return s0.Equals (s1) ? 0 : 1;
		}
	}
}
