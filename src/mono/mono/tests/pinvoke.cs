using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport("cygwin1.dll", EntryPoint="puts", CharSet=CharSet.Ansi)]
	public static extern int puts (string name);

	[DllImport ("libtest.so", EntryPoint="mono_test_many_int_arguments")]
	public static extern int mono_test_many_int_arguments (int a, int b, int c, int d, int e,
							       int f, int g, int h, int i, int j);
	[DllImport ("libtest.so", EntryPoint="mono_test_many_short_arguments")]
	public static extern int mono_test_many_short_arguments (short a, short b, short c, short d, short e,
								 short f, short g, short h, short i, short j);
	[DllImport ("libtest.so", EntryPoint="mono_test_many_byte_arguments")]
	public static extern int mono_test_many_byte_arguments (byte a, byte b, byte c, byte d, byte e,
								byte f, byte g, byte h, byte i, byte j);

	public static int Main () {
		puts ("A simple Test for PInvoke");
		
		if (Math.Cos (Math.PI) != -1)
			return 1;
		if (Math.Acos (1) != 0)
			return 1;
		if (mono_test_many_int_arguments (1, 1, 1, 1, 1, 1, 1, 1, 1, 1) != 10)
			return 1;
		if (mono_test_many_short_arguments (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) != 55)
			return 1;
		if (mono_test_many_byte_arguments (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) != 55)
			return 1;
		
		return 0;
	}
}
