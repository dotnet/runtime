using System;
using System.Reflection;

/*
 * Regression tests for the mono JIT.
 *
 * Each test needs to be of the form:
 *
 * static int test_<result>_<name> ();
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

class Tests {

	static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}
	
	static int test_0_sin_precision () {
		double d1 = Math.Sin (1);
                double d2 = Math.Sin (1) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	static int test_0_cos_precision () {
		double d1 = Math.Cos (1);
                double d2 = Math.Cos (1) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	static int test_0_tan_precision () {
		double d1 = Math.Tan (1);
                double d2 = Math.Tan (1) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	static int test_0_atan_precision () {
		double d1 = Math.Atan (double.NegativeInfinity);
                double d2 = Math.Atan (double.NegativeInfinity) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	static int test_0_sqrt_precision () {
		double d1 = Math.Sqrt (2);
                double d2 = Math.Sqrt (2) - d1;
		return (d2 == 0) ? 0 : 1;
	}

	static int test_2_sqrt () {
		return (int) Math.Sqrt (4);
	}
	static int test_0_sqrt_precision_and_not_spill () {
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
	
	static int test_0_sqrt_precision_and_spill () {
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
	
	static int test_0_div_precision_and_spill () {
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
	
	static int test_0_sqrt_nan () {
		return Double.IsNaN (Math.Sqrt (Double.NaN)) ? 0 : 1;
	}
	
	static int test_0_sin_nan () {
		return Double.IsNaN (Math.Sin (Double.NaN)) ? 0 : 1;
	}
	
	static int test_0_cos_nan () {
		return Double.IsNaN (Math.Cos (Double.NaN)) ? 0 : 1;
	}
	
	static int test_0_tan_nan () {
		return Double.IsNaN (Math.Tan (Double.NaN)) ? 0 : 1;
	}
	
	static int test_0_atan_nan () {
		return Double.IsNaN (Math.Atan (Double.NaN)) ? 0 : 1;
	}
}
