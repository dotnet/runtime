using System;
using System.Runtime.InteropServices;

public class Test {

	[StructLayout (LayoutKind.Sequential, Size=0)]
	public struct EmptyStruct {
	}

	[DllImport ("libtest.so", EntryPoint="mono_test_empty_struct")]
	public static extern int mono_test_empty_struct (int a, EmptyStruct es, int b);

	public static int Main () {
		EmptyStruct es = new EmptyStruct ();

		if (mono_test_empty_struct (1, es, 2) != 0)
			return 1;
		
		return 0;
	}
}
