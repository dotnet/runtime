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

	unsafe static float GetFloat (byte *ptr) {
		return *(float*)ptr;
	}

	unsafe public static float GetFloat(float value)
		{
			return GetFloat((byte *)&value);
		}

	/* bug #42134 */
	static int test_2_inline_saved_arg_type () {
		float f = 100.0f;
		return GetFloat (f) == f? 2: 1;
	}

	static int pass_many_types (int a, long b, int c, long d) {
		return a + (int)b + c + (int)d;
	}

	static int test_5_pass_longs () {
		return pass_many_types (1, 2, -5, 7);
	}

	static int overflow_registers (int a, int b, int c, int d, int e, int f, int g, int h, int i, int j) {
		return a+b+c+d+e+f+g+h+i+j;
	}

	static int test_55_pass_even_more () {
		return overflow_registers (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
	}

	static int pass_ints_longs (int a, long b, long c, long d, long e, int f, long g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int test_1_sparc_argument_passing () {
		// The 4. argument tests split reg/mem argument passing
		// The 5. argument tests mem argument passing
		// The 7. argument tests passing longs in misaligned memory
		// The MaxValues are needed so the MS word of the long is not 0
		return pass_ints_longs (1, 2, System.Int64.MaxValue, System.Int64.MinValue, System.Int64.MaxValue, 0, System.Int64.MinValue);
	}

	static int pass_bytes (byte a, byte b, byte c, byte d, byte e, byte f, byte g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int test_21_sparc_byte_argument_passing () {
		return pass_bytes (0, 1, 2, 3, 4, 5, 6);
	}

	static int pass_sbytes (sbyte a, sbyte b, sbyte c, sbyte d, sbyte e, sbyte f, sbyte g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int test_21_sparc_sbyte_argument_passing () {
		return pass_sbytes (0, 1, 2, 3, 4, 5, 6);
	}

	static int pass_shorts (short a, short b, short c, short d, short e, short f, short g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int test_21_sparc_short_argument_passing () {
		return pass_shorts (0, 1, 2, 3, 4, 5, 6);
	}

	static int pass_floats_doubles (float a, double b, double c, double d, double e, float f, double g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int test_721_sparc_float_argument_passing () {
		return pass_floats_doubles (100.0f, 101.0, 102.0, 103.0, 104.0, 105.0f, 106.0);
	}

	static int pass_byref_ints_longs (ref int a, ref long b, ref long c, ref long d, ref long e, ref int f, ref long g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int pass_takeaddr_ints_longs (int a, long b, long c, long d, long e, int f, long g) {
		return pass_byref_ints_longs (ref a, ref b, ref c, ref d, ref e, ref f, ref g);
	}

	// Test that arguments are moved to the stack from incoming registers
	// when the argument must reside in the stack because its address is taken
	static int test_1_sparc_takeaddr_argument_passing () {
		return pass_takeaddr_ints_longs (1, 2, System.Int64.MaxValue, System.Int64.MinValue, System.Int64.MaxValue, 0, System.Int64.MinValue);
	}

	static void shift_un_arg (ulong value) {
		do {
			value = value >> 4;
		} while (value != 0);
	}

	// Test that assignment to long arguments work
	static int test_0_long_arg_assign ()
	{
		ulong c = 0x800000ff00000000;
			
		shift_un_arg (c >> 4);

		return 0;
	}

	static unsafe void* ptr_return (void *ptr)
	{
		return ptr;
	}

	static unsafe int test_0_ptr_return ()
	{
		void *ptr = new IntPtr (55).ToPointer ();

		if (byref_return (ptr) == ptr)
			return 0;
		else
			return 1;
	}

	static bool isnan (float f) {
		return (f != f);
	}

	static int test_0_isnan () {
		float f = 1.0f;
		return isnan (f) ? 1 : 0;
	}
}

