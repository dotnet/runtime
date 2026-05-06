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

#if __MOBILE__
class MathTests
#else
class Tests
#endif
{

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif
	
	public static int test_0_sin_precision () {
		double d1 = Math.Sin (1);
                double d2 = Math.Sin (1) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	public static int test_0_cos_precision () {
		double d1 = Math.Cos (1);
                double d2 = Math.Cos (1) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	public static int test_0_tan_precision () {
		double d1 = Math.Tan (1);
                double d2 = Math.Tan (1) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	public static int test_0_atan_precision () {
		double d1 = Math.Atan (double.NegativeInfinity);
                double d2 = Math.Atan (double.NegativeInfinity) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	public static int test_0_sqrt_precision () {
		double d1 = Math.Sqrt (2);
                double d2 = Math.Sqrt (2) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	public static int test_2_sqrt () {
		return (int) Math.Sqrt (4);
	}
	public static int test_0_sqrt_precision_and_not_spill () {
		double expected = 0;
		double[] operands = new double[3];
		double[] temporaries = new double[3];
		for (int i = 0; i < 3; i++) {
			operands [i] = (i+1) * (i+1) * (i+1);
			if (i == 0) {
				expected = operands [0];
			} else {
				temporaries [i] =  operands [i] / expected;
				temporaries [i] = Math.Sqrt (temporaries [i]);
				expected = temporaries [i];
			}
			
			//Console.Write( "{0}: {1}\n", i, temporaries [i] );
		}
		expected = temporaries [2];
		
		double result = Math.Sqrt (operands [2] / Math.Sqrt (operands [1] / operands [0]));
		
		//Console.Write( "result: {0,20:G}\n", result );
		
		return (result == expected) ? 0 : 1;
	}
	
	public static int test_0_sqrt_precision_and_spill () {
		double expected = 0;
		double[] operands = new double[9];
		double[] temporaries = new double[9];
		for (int i = 0; i < 9; i++) {
			operands [i] = (i+1) * (i+1) * (i+1);
			if (i == 0) {
				expected = operands [0];
			} else {
				temporaries [i] =  operands [i] / expected;
				temporaries [i] = Math.Sqrt (temporaries [i]);
				expected = temporaries [i];
			}
			
			//Console.Write( "{0}: {1}\n", i, temporaries [i] );
		}
		expected = temporaries [8];
		
		double result = Math.Sqrt (operands [8] / Math.Sqrt (operands [7] / Math.Sqrt (operands [6] / Math.Sqrt (operands [5] / Math.Sqrt (operands [4] / Math.Sqrt (operands [3] / Math.Sqrt (operands [2] / Math.Sqrt (operands [1] / operands [0]))))))));
		
		//Console.Write( "result: {0,20:G}\n", result );
		
		return (result == expected) ? 0 : 1;
	}
	
	public static int test_0_div_precision_and_spill () {
		double expected = 0;
		double[] operands = new double[9];
		double[] temporaries = new double[9];
		for (int i = 0; i < 9; i++) {
			operands [i] = (i+1) * (i+1);
			if (i == 0) {
				expected = operands [0];
			} else {
				temporaries [i] =  operands [i] / expected;
				expected = temporaries [i];
			}
			
			//Console.Write( "{0}: {1}\n", i, temporaries [i] );
		}
		expected = temporaries [8];
		
		double result = (operands [8] / (operands [7] / (operands [6] / (operands [5] / (operands [4] / (operands [3] / (operands [2] / (operands [1] / operands [0]))))))));
		
		//Console.Write( "result: {0,20:G}\n", result );
		
		return (result == expected) ? 0 : 1;
	}
	
	public static int test_0_sqrt_nan () {
		return Double.IsNaN (Math.Sqrt (Double.NaN)) ? 0 : 1;
	}
	
	public static int test_0_sin_nan () {
		return Double.IsNaN (Math.Sin (Double.NaN)) ? 0 : 1;
	}
	
	public static int test_0_cos_nan () {
		return Double.IsNaN (Math.Cos (Double.NaN)) ? 0 : 1;
	}
	
	public static int test_0_tan_nan () {
		return Double.IsNaN (Math.Tan (Double.NaN)) ? 0 : 1;
	}
	
	public static int test_0_atan_nan () {
		return Double.IsNaN (Math.Atan (Double.NaN)) ? 0 : 1;
	}

	public static int test_0_min () {
		if (Math.Min (5, 6) != 5)
			return 1;
		if (Math.Min (6, 5) != 5)
			return 2;
		if (Math.Min (-100, -101) != -101)
			return 3;
		if (Math.Min ((long)5, (long)6) != 5)
			return 4;
		if (Math.Min ((long)6, (long)5) != 5)
			return 5;
		if (Math.Min ((long)-100, (long)-101) != -101)
			return 6;
		// this will trip if Min is accidentally using unsigned/logical comparison
		if (Math.Min((long)-100000000000L, (long)0L) != (long)-100000000000L)
			return 7;
		return 0;
	}

	public static int test_0_max () {
		if (Math.Max (5, 6) != 6)
			return 1;
		if (Math.Max (6, 5) != 6)
			return 2;
		if (Math.Max (-100, -101) != -100)
			return 3;
		if (Math.Max ((long)5, (long)6) != 6)
			return 4;
		if (Math.Max ((long)6, (long)5) != 6)
			return 5;
		if (Math.Max ((long)-100, (long)-101) != -100)
			return 6;
		// this will trip if Max is accidentally using unsigned/logical comparison
		if (Math.Max((long)-100000000000L, (long)0L) != (long)0L)
			return 7;
		return 0;
	}

	public static int test_0_min_un () {
		uint a = (uint)int.MaxValue + 10;

		for (uint b = 7; b <= 10; ++b) {
			if (Math.Min (a, b) != b)
				return (int)b;
			if (Math.Min (b, a) != b)
				return (int)b;
		}

		if (Math.Min ((ulong)5, (ulong)6) != 5)
			return 4;
		if (Math.Min ((ulong)6, (ulong)5) != 5)
			return 5;

		ulong la = (ulong)long.MaxValue + 10;

		for (ulong b = 7; b <= 10; ++b) {
			if (Math.Min (la, b) != b)
				return (int)b;
			if (Math.Min (b, la) != b)
				return (int)b;
		}

		return 0;
	}

	public static int test_0_max_un () {
		uint a = (uint)int.MaxValue + 10;

		for (uint b = 7; b <= 10; ++b) {
			if (Math.Max (a, b) != a)
				return (int)b;
			if (Math.Max (b, a) != a)
				return (int)b;
		}

		if (Math.Max ((ulong)5, (ulong)6) != 6)
			return 4;
		if (Math.Max ((ulong)6, (ulong)5) != 6)
			return 5;

		ulong la = (ulong)long.MaxValue + 10;

		for (ulong b = 7; b <= 10; ++b) {
			if (Math.Max (la, b) != la)
				return (int)b;
			if (Math.Max (b, la) != la)
				return (int)b;
		}

		return 0;
	}

	public static int test_0_abs () {
		double d = -5.0;

		if (Math.Abs (d) != 5.0)
			return 1;
		return 0;
	}

	public static int test_0_float_abs () {
		float f = -1.0f;

		if (Math.Abs (f) != 1.0f)
			return 1;
		return 0;
	}

	public static int test_0_round () {
		if (Math.Round (5.0) != 5.0)
			return 1;

		if (Math.Round (5.000000000000001) != 5.0)
			return 2;

		if (Math.Round (5.499999999999999) != 5.0)
			return 3;

		if (Math.Round (5.5) != 6.0)
			return 4;

		if (Math.Round (5.999999999999999) != 6.0)
			return 5;

		if (Math.Round (Double.Epsilon) != 0)
			return 6;

		if (!Double.IsNaN (Math.Round (Double.NaN)))
			return 7;

		if (!Double.IsPositiveInfinity (Math.Round (Double.PositiveInfinity)))
			return 8;

		if (!Double.IsNegativeInfinity (Math.Round (Double.NegativeInfinity)))
			return 9;

		if (Math.Round (Double.MinValue) != Double.MinValue)
			return 10;

		if (Math.Round (Double.MaxValue) != Double.MaxValue)
			return 11;

		return 0;
	}

	public static int test_0_mathf_sin () {
		float f = MathF.Sin (3.14159f);
		return f < 0.01f ? 0 : 1;
	}

	public static int test_0_mathf_cos () {
		float f = MathF.Cos (3.14159f);
		return f - -1f < 0.01f ? 0 : 1;
	}

	public static int test_0_mathf_abs () {
		float f;

		f = MathF.Abs (2.25f) - 2.25f;
		if (f > 0.01f || f < -0.01f)
			return 1;
		f = MathF.Abs (-2.25f) - 2.25f;
		if (f > 0.01f || f < -0.01f)
			return 2;
		return 0;
	}

	public static int test_0_mathf_sqrt () {
		float f;

		f = MathF.Sqrt (16.0f) - 4.0f;
		if (f > 0.01f || f < -0.01f)
			return 1;
		return 0;
	}

	public static int test_0_mathf_max () {
		float f;

		f = MathF.Max (1.0f, 2.0f) - 2.0f;
		if (f > 0.01f || f < -0.01f)
			return 1;
		f = MathF.Max (2.0f, 1.0f) - 2.0f;
		if (f > 0.01f || f < -0.01f)
			return 2;
		return 0;
	}

	public static int test_0_mathf_pow () {
		float f;

		f = MathF.Pow (2.0f, 4.0f) - 16.0f;
		if (f > 0.01f || f < -0.01f)
			return 1;
		return 0;
	}
}
