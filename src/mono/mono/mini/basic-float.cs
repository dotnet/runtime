using System;
using System.Reflection;

/*
 * Regression tests for the mono JIT.
 *
 * Each test needs to be of the form:
 *
 * public static int test_<result>_<name> ();
 *
 * where <result> is an integer (the value that needs to be returned by
 * the method to make it pass.
 * <name> is a user-displayed name used to identify the test.
 *
 * The tests can be driven in two ways:
 * *) running the program directly: Main() uses reflection to find and invoke
 * 	the test methods (this is useful mostly to check that the tests are correct)
 * *) with the --regression switch of the jit (this is the preferred way since
 * 	all the tests will be run with optimizations on and off)
 *
 * The reflection logic could be moved to a .dll since we need at least another
 * regression test file written in IL code to have better control on how
 * the IL code looks.
 */

/* A comparison made to same variable. */
#pragma warning disable 1718

#if __MOBILE__
class FloatTests
#else
class Tests
#endif
{

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif
	
	public static int test_0_beq () {
		double a = 2.0;
		if (a != 2.0)
			return 1;
		return 0;
	}

	public static int test_0_bne_un () {
		double a = 2.0;
		if (a == 1.0)
			return 1;
		return 0;
	}

	public static int test_0_conv_r8 () {
		double a = 2;
		if (a != 2.0)
			return 1;
		return 0;
	}

	public static int test_0_conv_i () {
		double a = 2.0;
		int i = (int)a;
		if (i != 2)
			return 1;
		uint ui = (uint)a;
		if (ui != 2)
			return 2;
		short s = (short)a;
		if (s != 2)
			return 3;
		ushort us = (ushort)a;
		if (us != 2)
			return 4;
		byte b = (byte)a;
		if (b != 2)
			return 5;
		sbyte sb = (sbyte)a;
		if (sb != 2)
			return 6;
		/* MS.NET special cases these */
		double d = Double.NaN;
		ui = (uint)d;
		if (ui != 0)
			return 7;
		d = Double.PositiveInfinity;
		ui = (uint)d;
		if (ui != 0)
			return 8;
		d = Double.NegativeInfinity;
		ui = (uint)d;
		if (ui != 0)
			return 9;
		/* FIXME: This fails with llvm and with gcc -O2 on osx/linux */
		/*
		d = Double.MaxValue;
		i = (int)d;
		if (i != -2147483648)
			return 10;
		*/

		return 0;
	}

	public static int test_5_conv_r4 () {
		int i = 5;
		float f = (float)i;
		return (int)f;
	}

	public static int test_0_conv_r4_m1 () {
		int i = -1;
		float f = (float)i;
		return (int)f + 1;
	}

	public static int test_5_double_conv_r4 () {
		double d = 5.0;
		float f = (float)d;
		return (int)f;
	}

	public static int test_5_float_conv_r8 () {
		float f = 5.0F;
		double d = (double)f;
		return (int)d;
	}

	public static int test_5_conv_r8 () {
		int i = 5;
		double f = (double)i;
		return (int)f;
	}

	public static int test_5_add () {
		double a = 2.0;
		double b = 3.0;		
		return (int)(a + b);
	}

	public static int test_5_sub () {
		double a = 8.0;
		double b = 3.0;		
		return (int)(a - b);
	}	

	public static int test_24_mul () {
		double a = 8.0;
		double b = 3.0;		
		return (int)(a * b);
	}	

	public static int test_4_div () {
		double a = 8.0;
		double b = 2.0;		
		return (int)(a / b);
	}	

	public static int test_2_rem () {
		double a = 8.0;
		double b = 3.0;		
		return (int)(a % b);
	}	

	public static int test_2_neg () {
		double a = -2.0;		
		return (int)(-a);
	}
	
	public static int test_46_float_add_spill () {
		// we overflow the FP stack
		double a = 1;
		double b = 2;
		double c = 3;
		double d = 4;
		double e = 5;
		double f = 6;
		double g = 7;
		double h = 8;
		double i = 9;

		return (int)(1.0 + (a + (b + (c + (d + (e + (f + (g + (h + i)))))))));
	}

	public static int test_4_float_sub_spill () {
		// we overflow the FP stack
		double a = 1;
		double b = 2;
		double c = 3;
		double d = 4;
		double e = 5;
		double f = 6;
		double g = 7;
		double h = 8;
		double i = 9;

		return -(int)(1.0 - (a - (b - (c - (d - (e - (f - (g - (h - i)))))))));
		////// -(int)(1.0 - (1 - (2 - (3 - (4 - (5 - (6 - (7 - (8 - 9)))))))));
	}

	public static int test_362880_float_mul_spill () {
		// we overflow the FP stack
		double a = 1;
		double b = 2;
		double c = 3;
		double d = 4;
		double e = 5;
		double f = 6;
		double g = 7;
		double h = 8;
		double i = 9;

		return (int)(1.0 * (a * (b * (c * (d * (e * (f * (g * (h * i)))))))));
	}

	public static int test_4_long_cast () {
		long a = 1000;
		double d = (double)a;
		long b = (long)d;
		if (b != 1000)
			return 0;
		a = -1;
		d = (double)a;
		b = (long)d;
		if (b != -1)
			return 1;
		return 4;
	}

	public static int test_4_ulong_cast () {
		ulong a = 1000;
		double d = (double)a;
		ulong b = (ulong)d;
		if (b != 1000)
			return 0;
		a = 0xffffffffffffffff;
		float f = (float)a;
		if (!(f > 0f))
			return 1;
		return 4;
	}

	public static int test_4_single_long_cast () {
		long a = 1000;
		float d = (float)a;
		long b = (long)d;
		if (b != 1000)
			return 0;
		a = -1;
		d = (float)a;
		b = (long)d;
		if (b != -1)
			return 1;
		return 4;
	}

	public static int test_0_lconv_to_r8 () {
		long a = 150;
		double b = (double) a;

		if (b != 150.0)
			return 1;
		return 0;
	}

	public static int test_0_lconv_to_r4 () {
		long a = 3000;
		float b = (float) a;

		if (b != 3000.0F)
			return 1;
		return 0;
	}

	static void doit (double value, out long m) {
		m = (long) value;
	}

	public static int test_0_ftol_clobber () {
		long m;
		doit (1.3, out m);
		if (m != 1)
			return 2;
		return 0;
	}

	public static int test_0_rounding () {
		long ticks = 631502475130080000L;
		long ticksperday = 864000000000L;

		double days = (double) ticks / ticksperday;

		if ((int)days != 730905)
			return 1;

		return 0;
	}

	/* FIXME: This only works on little-endian machines */
	/*
	static unsafe int test_2_negative_zero () {
		int result = 0;
		double d = -0.0;
		float f = -0.0f;

		byte *ptr = (byte*)&d;
		if (ptr [7] == 0)
			return result;
		result ++;

		ptr = (byte*)&f;
		if (ptr [3] == 0)
			return result;
		result ++;

		return result;
	}
	*/

	public static int test_16_float_cmp () {
		double a = 2.0;
		double b = 1.0;
		int result = 0;
		bool val;
		
		val = a == a;
		if (!val)
			return result;
		result++;

		val = (a != a);
		if (val)
			return result;
		result++;

		val = a < a;
		if (val)
			return result;
		result++;

		val = a > a;
		if (val)
			return result;
		result++;

		val = a <= a;
		if (!val)
			return result;
		result++;

		val = a >= a;
		if (!val)
			return result;
		result++;

		val = b == a;
		if (val)
			return result;
		result++;

		val = b < a;
		if (!val)
			return result;
		result++;

		val = b > a;
		if (val)
			return result;
		result++;

		val = b <= a;
		if (!val)
			return result;
		result++;

		val = b >= a;
		if (val)
			return result;
		result++;

		val = a == b;
		if (val)
			return result;
		result++;

		val = a < b;
		if (val)
			return result;
		result++;

		val = a > b;
		if (!val)
			return result;
		result++;

		val = a <= b;
		if (val)
			return result;
		result++;

		val = a >= b;
		if (!val)
			return result;
		result++;

		return result;
	}

	public static int test_15_float_cmp_un () {
		double a = Double.NaN;
		double b = 1.0;
		int result = 0;
		bool val;
		
		val = a == a;
		if (val)
			return result;
		result++;

		val = a < a;
		if (val)
			return result;
		result++;

		val = a > a;
		if (val)
			return result;
		result++;

		val = a <= a;
		if (val)
			return result;
		result++;

		val = a >= a;
		if (val)
			return result;
		result++;

		val = b == a;
		if (val)
			return result;
		result++;

		val = b < a;
		if (val)
			return result;
		result++;

		val = b > a;
		if (val)
			return result;
		result++;

		val = b <= a;
		if (val)
			return result;
		result++;

		val = b >= a;
		if (val)
			return result;
		result++;

		val = a == b;
		if (val)
			return result;
		result++;

		val = a < b;
		if (val)
			return result;
		result++;

		val = a > b;
		if (val)
			return result;
		result++;

		val = a <= b;
		if (val)
			return result;
		result++;

		val = a >= b;
		if (val)
			return result;
		result++;

		return result;
	}

	public static int test_15_float_branch () {
		double a = 2.0;
		double b = 1.0;
		int result = 0;
		
		if (!(a == a))
			return result;
		result++;

		if (a < a)
			return result;
		result++;

		if (a > a)
			return result;
		result++;

		if (!(a <= a))
			return result;
		result++;

		if (!(a >= a))
			return result;
		result++;

		if (b == a)
			return result;
		result++;

		if (!(b < a))
			return result;
		result++;

		if (b > a)
			return result;
		result++;

		if (!(b <= a))
			return result;
		result++;

		if (b >= a)
			return result;
		result++;

		if (a == b)
			return result;
		result++;

		if (a < b)
			return result;
		result++;

		if (!(a > b))
			return result;
		result++;

		if (a <= b)
			return result;
		result++;

		if (!(a >= b))
			return result;
		result++;

		return result;
	}

	public static int test_15_float_branch_un () {
		double a = Double.NaN;
		double b = 1.0;
		int result = 0;
		
		if (a == a)
			return result;
		result++;

		if (a < a)
			return result;
		result++;

		if (a > a)
			return result;
		result++;

		if (a <= a)
			return result;
		result++;

		if (a >= a)
			return result;
		result++;

		if (b == a)
			return result;
		result++;

		if (b < a)
			return result;
		result++;

		if (b > a)
			return result;
		result++;

		if (b <= a)
			return result;
		result++;

		if (b >= a)
			return result;
		result++;

		if (a == b)
			return result;
		result++;

		if (a < b)
			return result;
		result++;

		if (a > b)
			return result;
		result++;

		if (a <= b)
			return result;
		result++;

		if (a >= b)
			return result;
		result++;

		return result;
	}

	public static int test_0_float_precision () {
		float f1 = 3.40282346638528859E+38f;
		float f2 = 3.40282346638528859E+38f;		
		float PositiveInfinity =  (float)(1.0f / 0.0f);
		float f = (float)(f1 + f2);

		return f == PositiveInfinity ? 0 : 1;
	}

	static double VALUE = 0.19975845134874831D;

	public static int test_0_float_conversion_reduces_double_precision () {
		double d = (float)VALUE;
		if (d != 0.19975845515727997d)
			return 1;

		return 0;
	}

	/* This doesn't work with llvm */
	/*
    public static int test_0_long_to_double_conversion ()
    {
		long l = 9223372036854775807L;
		long conv = (long)((double)l);
		if (conv != -9223372036854775808L)
			return 1;

		return 0;
    }
	*/

	public static int INT_VAL = 0x13456799;

	public static int test_0_int4_to_float_convertion ()
    {
		double d = (double)(float)INT_VAL;

		if (d != 323315616)
			return 1;
		return 0;
	}

	public static int test_0_int8_to_float_convertion ()
    {
		double d = (double)(float)(long)INT_VAL;

		if (d != 323315616)
			return 1;
		return 0;
	}

	public static int test_5_r4_fadd () {
		float f1 = 3.0f;
		float f2 = 2.0f;
		return (int)(f1 + f2);
	}

	public static int test_1_r4_fsub () {
		float f1 = 3.0f;
		float f2 = 2.0f;
		return (int)(f1 - f2);
	}

	public static int test_6_fmul_r4 () {
		float f1 = 2.0f;
		float f2 = 3.0f;
		return (int)(f1 * f2);
	}

	public static int test_3_fdiv_r4 () {
		float f1 = 6.0f;
		float f2 = 2.0f;
		return (int)(f1 / f2);
	}

	public static int test_1_frem_r4 () {
		float f1 = 7.0f;
		float f2 = 2.0f;
		return (int)(f1 % f2);
	}

	public static int test_0_fcmp_eq_r4 () {
		float f1 = 1.0f;
		float f2 = 1.0f;
		return f1 == f2 ? 0 : 1;
	}

	public static int test_0_fcmp_eq_2_r4 () {
		float f1 = 1.0f;
		float f2 = 2.0f;
		return f1 == f2 ? 1 : 0;
	}

	public static int test_0_fcmp_eq_r4_mixed () {
		float f1 = 1.0f;
		double f2 = 1.0;
		return f1 == f2 ? 0 : 1;
	}

	public static int test_3_iconv_to_r4 () {
		int i = 3;
		float f = (float)i;
		return (int)f;
	}

	public static int test_2_neg_r4 () {
		float a = -2.0f;
		return (int)(-a);
	}

	public static int test_0_fceq_r4 () {
		float f1 = 1.0f;
		float f2 = 1.0f;
		bool res = f1 == f2;
		return res ? 0 : 1;
	}

	public static int test_0_fcgt_r4 () {
		float f1 = 2.0f;
		float f2 = 1.0f;
		bool res = f1 > f2;
		bool res2 = f2 > f1;
		return res && !res2 ? 0 : 1;
	}

	public static int test_0_fclt_r4 () {
		float f1 = 1.0f;
		float f2 = 2.0f;
		bool res = f1 < f2;
		bool res2 = f2 < f1;
		return res && !res2 ? 0 : 1;
	}

	public static int test_0_fclt_un_r4 () {
		float f1 = 2.0f;
		float f2 = 1.0f;
		bool res = f1 >= f2;
		bool res2 = f2 >= f1;
		return res && !res2 ? 0 : 1;
	}

	public static int test_0_fcgt_un_r4 () {
		float f1 = 1.0f;
		float f2 = 2.0f;
		bool res = f1 <= f2;
		bool res2 = f2 <= f1;
		return res && !res2 ? 0 : 1;
	}

	public static int test_0_fconv_to_u4_r4 () {
		float a = 10.0f;

		uint b = (uint)a;
		return b == 10 ? 0 : 1;
	}

	public static int test_0_fconv_to_u1_r4 () {
		float a = 10.0f;

		byte b = (byte)a;
		return b == 10 ? 0 : 1;
	}

	public static int test_0_fconv_to_i1_r4 () {
		float a = 127.0f;

		sbyte b = (sbyte)a;
		return b == 127 ? 0 : 1;
	}

	public static int test_0_fconv_to_u2_r4 () {
		float a = 10.0f;

		ushort b = (ushort)a;
		return b == 10 ? 0 : 1;
	}

	public static int test_0_fconv_to_i2_r4 () {
		float a = 127.0f;

		short b = (short)a;
		return b == 127 ? 0 : 1;
	}

	public static int test_10_rconv_to_u8 () {
		ulong l = 10;
		float f = (float)l;
		l = (ulong)f;
		return (int)l;
	}

}

