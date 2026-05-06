using System;
using System.Runtime.InteropServices;

namespace Test {
	[StructLayout ( LayoutKind.Explicit )]
	public struct Doh {
		[ FieldOffset(0) ] public int a;
		[ FieldOffset(0) ] public byte a1;
		[ FieldOffset(1) ] public byte a2;
		[ FieldOffset(2) ] public byte a3;
		[ FieldOffset(3) ] public byte a4;
	}
	[StructLayout ( LayoutKind.Explicit )]
	public class Doh2 {
		[ FieldOffset(0) ] public int a;
		[ FieldOffset(0) ] public byte a1;
		[ FieldOffset(1) ] public byte a2;
		[ FieldOffset(2) ] public byte a3;
		[ FieldOffset(3) ] public byte a4;
	}
	[StructLayout ( LayoutKind.Explicit )]
	public class Doh3: Doh2 {
		[ FieldOffset(0) ] public int b;
	}
	public class Test {
		public static int Main() {
			Doh doh;
			Doh3 doh2 = new Doh3 ();
			bool success = false;
			// shut up the compiler
			doh.a1 = doh.a2 = doh.a3 = doh.a4 = 0;
			doh.a = 1;
			if (doh.a1 == 1 && doh.a2 == 0 && doh.a3 == 0 && doh.a4 == 0) {
				System.Console.WriteLine ("Little endian");
				success = true;
			} else if (doh.a1 == 0 && doh.a2 == 0 && doh.a3 == 0 && doh.a4 == 1) {
				System.Console.WriteLine ("Big endian");
				success = true;
			}
			if (!success)
				return 1;
			// shut up the compiler
			doh2.a1 = doh2.a2 = doh2.a3 = doh2.a4 = 0;
			doh2.a = 1;
			if (doh2.a1 == 1 && doh2.a2 == 0 && doh2.a3 == 0 && doh2.a4 == 0) {
				success = true;
			} else if (doh2.a1 == 0 && doh2.a2 == 0 && doh2.a3 == 0 && doh2.a4 == 1) {
				success = true;
			}
			doh2.b = 3;
			if (doh2.a != 3)
				success = false;
			if (!success)
				return 1;
			return 0;

		}
	}
}
