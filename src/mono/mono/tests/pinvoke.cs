using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport ("libtest", EntryPoint="mono_test_many_int_arguments")]
	public static extern int mono_test_many_int_arguments (int a, int b, int c, int d, int e,
							       int f, int g, int h, int i, int j);
	[DllImport ("libtest", EntryPoint="mono_test_many_short_arguments")]
	public static extern int mono_test_many_short_arguments (short a, short b, short c, short d, short e,
								 short f, short g, short h, short i, short j);
	[DllImport ("libtest", EntryPoint="mono_test_many_byte_arguments")]
	public static extern int mono_test_many_byte_arguments (byte a, byte b, byte c, byte d, byte e,
								byte f, byte g, byte h, byte i, byte j);
	[DllImport ("libtest", EntryPoint="mono_test_many_float_arguments")]
	public static extern float mono_test_many_float_arguments (float a, float b, float c, float d, float e,
								float f, float g, float h, float i, float j);
	[DllImport ("libtest", EntryPoint="mono_test_many_double_arguments")]
	public static extern double mono_test_many_double_arguments (double a, double b, double c, double d, double e,
								double f, double g, double h, double i, double j);
	[DllImport ("libtest", EntryPoint="mono_test_split_double_arguments")]
	public static extern double mono_test_split_double_arguments (double a, double b, float c, double d, double e);

	public static int Main () {
		if (Math.Cos (Math.PI) != -1)
			return 1;
		if (Math.Acos (1) != 0)
			return 2;
		if (mono_test_many_int_arguments (1, 1, 1, 1, 1, 1, 1, 1, 1, 1) != 10)
			return 3;
		if (mono_test_many_short_arguments (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) != 55)
			return 4;
		if (mono_test_many_byte_arguments (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) != 55)
			return 5;
		if (mono_test_many_float_arguments (1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f) != 55.0f)
			return 6;
		if (mono_test_many_double_arguments (1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0) != 55.0)
			return 7;

		/* Test Sparc V8 split register/stack double parameter passing */
		if (mono_test_split_double_arguments (1.0, 2.0, 3.0f, 4.0, 5.0) != 15.0)
			return 8;
		
		return 0;
	}
}
