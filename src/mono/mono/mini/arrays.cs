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
	
	static int test_10_create () {
		int[] a = new int [10];
		return a.Length;
	}

	static int test_0_unset_value () {
		int[] a = new int [10];
		return a [5];
	}

	static int test_3_set_value () {
		int[] a = new int [10];
		a [5] = 3;
		return a [5];
	}

	static int test_0_char_array_1 () {
		int value = -30;
		char[] tmp = new char [20];
		char[] digitLowerTable = new char[16];
		tmp[0] = digitLowerTable[-(value % 10)];
		return 0;
	}
	
	static int test_0_char_array_2 () {
		int value = 5;
		char[] tmp = new char [20];
		char[] digitLowerTable = new char[16];
		tmp[0] = digitLowerTable[value % 10];
		return 0;
	}

	static int test_0_char_array_3 () {
		int value = -1;
		char[] tmp = new char [20];
		char[] digitLowerTable = new char[16];
		tmp [0] = digitLowerTable[value & 15];		
		return 0;
	}

	unsafe static int test_0_byte_array () {
		byte [] src = new byte [8];
		double ret;
		byte *dst = (byte *)&ret;
		int start = 0;

		dst[0] = src[4 + start];
		
		return 0;
	}
	
	public static int test_0_set_after_shift () {
		int [] n = new int [1];
		int b = 16;
                   
		n [0] = 100 + (1 << (16 - b));

		if (n [0] != 101)
			return 1;

		return 0;
	}

	/* Regression test for #30073 */
	public static int test_0_newarr_emulation () {
		double d = 500;
		checked {
			double [] arr = new double [(int)d];
		}
		return 0;
	}

	private Int32[] m_array = new int [10];
	
	void setBit (int bitIndex, bool value) {
		int index = bitIndex/32;
		int shift = bitIndex%32;

		Int32 theBit = 1 << shift;
		if (value)
			m_array[index] |= theBit;
		else
			m_array[index] &= ~theBit;
	}
	
	bool getBit (int bitIndex) {
		int index = bitIndex/32;
		int shift = bitIndex%32;

		Int32 theBit = m_array[index] & (1 << shift);
		return (theBit == 0) ? false : true;

	}
	
	public static int test_1_bit_index () {
		Tests t = new Tests ();
		t.setBit (0, true);
		t.setBit (3, true);
		if (t.getBit (1))
			return 4;
		if (!t.getBit (0))
			return 5;
		if (!t.getBit (3))
			return 6;
		return 1;
	}

	class helper1 {

		int [] ma = new int [56];
		const int MBIG = int.MaxValue;

		public helper1 () {
			for (int k = 1; k < 5; k++) {
				for (int i = 1; i < 56; i++) {
					ma [i] -= ma [1 + (i + 30) % 55];
					if (ma [i] < 0)
						ma [i] += MBIG;
				}
			}
		}
	}

	public static int test_2_regalloc () {
		helper1 h = new helper1 ();
		return 2;
	}
}

