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

	static void dummy () {
	}

	static int test_0_return () {
		dummy ();
		return 0;
	}

	static int dummy1 () {
		return 1;
	}

	static int test_2_int_return () {
		int r = dummy1 ();
		if (r == 1)
			return 2;
		return 0;
	}

	static int add1 (int val) {
		return val + 1;
	}

	static int test_1_int_pass () {
		int r = add1 (5);
		if (r == 6)
			return 1;
		return 0;
	}

	static int add_many (int val, short t, byte b, int da) {
		return val + t + b + da;
	}

	static int test_1_int_pass_many () {
		byte b = 6;
		int r = add_many (5, 2, b, 1);
		if (r == 14)
			return 1;
		return 0;
	}

}

