using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size=32)]
public class A {
   int a;
}

public class X {
	public static int Main () {
		int size = Marshal.SizeOf(typeof(A));

		if (size != 32) {
			Console.WriteLine ("wrong size: " + size);
			return 1;
		}
		
		return 0;
	}
}
