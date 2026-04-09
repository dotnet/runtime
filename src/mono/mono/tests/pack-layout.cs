using System;
using System.Runtime.InteropServices;

[StructLayout ( LayoutKind.Sequential, Pack=2 )]
struct MYStruct  {
	short a;
	int b;
}

[StructLayout ( LayoutKind.Sequential, Pack=2 )]
struct MYStruct2  {
	int b;
	short a;
}

struct MYStruct3  {
	int b;
	short a;
}

class Test {
	static int Main() {
		int ms, ms2, ms3;
		unsafe {
			ms = sizeof (MYStruct);
			ms2 = sizeof (MYStruct2);
			ms3 = sizeof (MYStruct3);
		}
		Console.WriteLine ("MYStruct size: {0}", ms);
		Console.WriteLine ("MYStruct2 size: {0}", ms2);
		Console.WriteLine ("MYStruct3 size: {0}", ms3);
		if (ms != 6)
			return 1;
		if (ms2 != 6)
			return 2;
		if (ms3 != 8)
			return 3;
		return 0;
	}
}
