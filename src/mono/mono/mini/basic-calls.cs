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
class CallsTests
#else
class Tests
#endif
{

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif

	static void dummy () {
	}

	public static int test_0_return () {
		dummy ();
		return 0;
	}

	static int dummy1 () {
		return 1;
	}

	public static int test_2_int_return () {
		int r = dummy1 ();
		if (r == 1)
			return 2;
		return 0;
	}

	static int add1 (int val) {
		return val + 1;
	}

	public static int test_1_int_pass () {
		int r = add1 (5);
		if (r == 6)
			return 1;
		return 0;
	}

	static int add_many (int val, short t, byte b, int da) {
		return val + t + b + da;
	}

	public static int test_1_int_pass_many () {
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
	public static int test_2_inline_saved_arg_type () {
		float f = 100.0f;
		return GetFloat (f) == f? 2: 1;
	}

	static int pass_many_types (int a, long b, int c, long d) {
		return a + (int)b + c + (int)d;
	}

	public static int test_5_pass_longs () {
		return pass_many_types (1, 2, -5, 7);
	}

	static int overflow_registers (int a, int b, int c, int d, int e, int f, int g, int h, int i, int j) {
		return a+b+c+d+e+f+g+h+i+j;
	}

	public static int test_55_pass_even_more () {
		return overflow_registers (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
	}

	static int pass_ints_longs (int a, long b, long c, long d, long e, int f, long g) {
		return (int)(a + b + c + d + e + f + g);
	}

	public static int test_1_sparc_argument_passing () {
		// The 4. argument tests split reg/mem argument passing
		// The 5. argument tests mem argument passing
		// The 7. argument tests passing longs in misaligned memory
		// The MaxValues are needed so the MS word of the long is not 0
		return pass_ints_longs (1, 2, System.Int64.MaxValue, System.Int64.MinValue, System.Int64.MaxValue, 0, System.Int64.MinValue);
	}

	static int pass_bytes (byte a, byte b, byte c, byte d, byte e, byte f, byte g) {
		return (int)(a + b + c + d + e + f + g);
	}

	public static int test_21_sparc_byte_argument_passing () {
		return pass_bytes (0, 1, 2, 3, 4, 5, 6);
	}

	static int pass_sbytes (sbyte a, sbyte b, sbyte c, sbyte d, sbyte e, sbyte f, sbyte g, sbyte h1, sbyte h2, sbyte h3, sbyte h4) {
		return (int)(a + b + c + d + e + f + g + h1 + h2 + h3 + h4);
	}

	public static int test_55_sparc_sbyte_argument_passing () {
		return pass_sbytes (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
	}

	static int pass_shorts (short a, short b, short c, short d, short e, short f, short g) {
		return (int)(a + b + c + d + e + f + g);
	}

	public static int test_21_sparc_short_argument_passing () {
		return pass_shorts (0, 1, 2, 3, 4, 5, 6);
	}

	static int pass_floats_doubles (float a, double b, double c, double d, double e, float f, double g) {
		return (int)(a + b + c + d + e + f + g);
	}

	public static int test_721_sparc_float_argument_passing () {
		return pass_floats_doubles (100.0f, 101.0, 102.0, 103.0, 104.0, 105.0f, 106.0);
	}

	static float pass_floats (float a, float b, float c, float d, float e, float f, float g, float h, float i, float j) {
		return a + b + c + d + e + f + g + h + i + j;
	}

	public static int test_55_sparc_float_argument_passing2 () {
		return (int)pass_floats (1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f);
	}

	public static bool is_small (float value) {
		double d = (double)value;
		double d2 = 7.183757E-41;
		return d - d2 < 0.000001;
	}
		
	public static int test_0_float_argument_passing_precision () {
		float f = 7.183757E-41f;
		return is_small (f) ? 0 : 1;
	}

	// The first argument must be passed on a dword aligned stack location
	static int pass_byref_ints_longs (ref long a, ref int b, ref byte c, ref short d, ref long e, ref int f, ref long g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int pass_takeaddr_ints_longs (long a, int b, byte c, short d, long e, int f, long g) {
		return pass_byref_ints_longs (ref a, ref b, ref c, ref d, ref e, ref f, ref g);
	}

	// Test that arguments are moved to the stack from incoming registers
	// when the argument must reside in the stack because its address is taken
	public static int test_2_sparc_takeaddr_argument_passing () {
		return pass_takeaddr_ints_longs (1, 2, 253, -253, System.Int64.MaxValue, 0, System.Int64.MinValue);
	}

	static int pass_byref_floats_doubles (ref float a, ref double b, ref double c, ref double d, ref double e, ref float f, ref double g) {
		return (int)(a + b + c + d + e + f + g);
	}

	static int pass_takeaddr_floats_doubles (float a, double b, double c, double d, double e, float f, double g) {
		return pass_byref_floats_doubles (ref a, ref b, ref c, ref d, ref e, ref f, ref g);
	}

	public static int test_721_sparc_takeaddr_argument_passing2 () {
		return pass_takeaddr_floats_doubles (100.0f, 101.0, 102.0, 103.0, 104.0, 105.0f, 106.0);
	}

	static void pass_byref_double (out double d) {
		d = 5.0;
	}

	// Test byref double argument passing
	public static int test_0_sparc_byref_double_argument_passing () {
		double d;
		pass_byref_double (out d);
		return (d == 5.0) ? 0 : 1;
	}

	static void shift_un_arg (ulong value) {
		do {
			value = value >> 4;
		} while (value != 0);
	}

	// Test that assignment to long arguments work
	public static int test_0_long_arg_assign ()
	{
		ulong c = 0x800000ff00000000;
			
		shift_un_arg (c >> 4);

		return 0;
	}

	static unsafe void* ptr_return (void *ptr)
	{
		return ptr;
	}

	public static unsafe int test_0_ptr_return ()
	{
		void *ptr = new IntPtr (55).ToPointer ();

		if (ptr_return (ptr) == ptr)
			return 0;
		else
			return 1;
	}

	static bool isnan (float f) {
		return (f != f);
	}

	public static int test_0_isnan () {
		float f = 1.0f;
		return isnan (f) ? 1 : 0;
	}

	static int first_is_zero (int v1, int v2) {
		if (v1 != 0)
			return -1;
		return v2;
	}
	public static int test_1_handle_dup_stloc () {
		int index = 0;
		int val = first_is_zero (index, ++index);
		if (val != 1)
			return 2;
		return 1;
	}

	static long return_5low () {
		return 5;
	}
	
	static long return_5high () {
		return 0x500000000;
	}

	public static int test_3_long_ret () {
		long val = return_5low ();
		return (int) (val - 2);
	}

	public static int test_1_long_ret2 () {
		long val = return_5high ();
		if (val > 0xffffffff)
			return 1;
		return 0;
	}

	public static void use_long_arg (ulong l) {
		for (int i = 0; i < 10; ++i)
			l ++;
	}

	public static ulong return_long_arg (object o, ulong perm) {
		use_long_arg (perm);

        perm = 0x8000000000000FFF;

		use_long_arg (perm);

		return perm;
	}

    public static int test_0_sparc_long_ret_regress_541577 () {
        ulong perm = 0x8000000000000FFF;

        ulong work = return_long_arg (null, perm);

		return work == perm ? 0 : 1;
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

	public static bool arm64_stack_arg_reg_bool (object o1, object o2, object o3, object o4, object o5, object o6, object o7,
												 bool foo, bool bar) {
		bool res1 = bar || foo;
		bool res2 = bar || foo;
		return res1 | res2;
	}

	public static int arm64_stack_arg_reg_sbyte (object o1, object o2, object o3, object o4, object o5, object o6, object o7,
												 sbyte foo, sbyte bar) {
		int res1 = bar + foo;
		int res2 = bar + foo;
		return res1 + res2;
	}

	// bool argument passed on the stack and promoted to a register
	public static int test_0_arm64_stack_arg_reg_bool () {
	    bool res = arm64_stack_arg_reg_bool (null, null, null, null, null, null, null, false, false);
		return res ? 1 : 0;
	}

	public static int test_0_arm64_stack_arg_reg_sbyte () {
	    int res = arm64_stack_arg_reg_sbyte (null, null, null, null, null, null, null, -4, -7);
		return res == -22 ? 0 : 1;
	}
}
