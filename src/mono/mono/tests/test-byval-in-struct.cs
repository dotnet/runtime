using System;
using System.Runtime.InteropServices;

[StructLayout (LayoutKind.Explicit)]
struct TestStructure {
	[FieldOffset (0)]
	internal int number;
	[FieldOffset (8)]
	[MarshalAs(UnmanagedType.ByValArray, SizeConst=1024)]
	internal byte[] stuff;

	static int Main () {
		int size = Marshal.SizeOf(typeof(TestStructure));
		Console.WriteLine("Size of t: {0}", size);
		if (size != 1032)
			return 1;

		size = Marshal.SizeOf(typeof(TestStructure2));
		Console.WriteLine("Size of t2: {0}", size);
		if (size != 8)
			return 2;

		return 0;
	}
}

[StructLayout (LayoutKind.Explicit)]
struct TestStructure2 {
	[FieldOffset (0)]
	byte val;
	[FieldOffset (2)]
	int val2;
}
