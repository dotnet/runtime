using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport("cygwin1.dll", EntryPoint="puts", CharSet=CharSet.Ansi)]
	public static extern int puts (string name);

	[DllImport (".libs/libtest.so", EntryPoint="mono_test_many_int_arguments")]
	public static extern int mono_test_many_int_arguments (int a, int b, int c, int d, int e,
							       int f, int g, int h, int i, int j);

	public static int Main () {
		puts ("A simple Test for PInvoke");
		
		if (Math.Cos (Math.PI) != -1)
			return 1;
		if (Math.Acos (1) != 0)
			return 1;
		if (mono_test_many_int_arguments (1, 1, 1, 1, 1, 1, 1, 1, 1, 1) != 10)
			return 1;
		
		return 0;
	}
}
