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
class LongTests
#else
class Tests
#endif
{

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif

	public static int test_10_simple_cast () {
		long a = 10;
		return (int)a;
	}

	public static int test_1_bigmul1 () {
		int a;
		int b;
		long c;
		a = 10;
		b = 10;
		c = (long)a * (long)b;
		if (c == 100)
			return 1;
		return 0;
	}

	public static int test_1_bigmul2 () {
                int a = System.Int32.MaxValue, b = System.Int32.MaxValue;
                long s = System.Int64.MinValue;
                long c;
                c = s + (long) a * (long) b;
		if (c == -4611686022722355199)
			return 1;
		return 0;
	}
	
	public static int test_1_bigmul3 () {
                int a = 10, b = 10;
                ulong c;
                c = (ulong) a * (ulong) b;
		if (c == 100)
			return 1;
		return 0;
	}

	public static int test_1_bigmul4 () {
                int a = System.Int32.MaxValue, b = System.Int32.MaxValue;
                ulong c;
                c = (ulong) a * (ulong) b;
		if (c == 4611686014132420609)
			return 1;
		return 0;
	}
	
	public static int test_1_bigmul5 () {
                int a = System.Int32.MaxValue, b = System.Int32.MinValue;
                long c;
                c = (long) a * (long) b;
		if (c == -4611686016279904256)
			return 1;
		return 0;
	}
	
	public static int test_1_bigmul6 () {
                uint a = System.UInt32.MaxValue, b = System.UInt32.MaxValue/(uint)2;
                ulong c;
                c = (ulong) a * (ulong) b;
		if (c == 9223372030412324865)
			return 1;
		return 0;
	}
	
	public static int test_0_beq () {
		long a = 0xffffffffff;
		if (a != 0xffffffffff)
			return 1;
		return 0;
	}

	public static int test_0_bne_un () {
		long a = 0xffffffffff;
		if (a == 0xfffffffffe)
			return 1;
		if (a == 0xfeffffffff)
			return 2;
		return 0;
	}

	public static int test_0_ble () {
		long a = 0xffffffffff;
		if (a > 0xffffffffff)
			return 1;

		if (a > 0x1ffffffffff)
			return 2;

		if (a > 0xff00000000) {} else
			return 3;

		if (a > 0xfeffffffff) {} else
			return 4;

		a = 0xff00000000;
		if (a > 0xffffffffff)
			return 5;

		return 0;
	}

	public static int test_0_ble_un () {
		ulong a = 0xffffffffff;
		if (a > 0xffffffffff)
			return 1;

		if (a > 0x1ffffffffff)
			return 2;

		if (a > 0xff00000000) {} else
			return 3;

		if (a > 0xfeffffffff) {} else
			return 4;

		a = 0xff00000000;
		if (a > 0xffffffffff)
			return 5;

		return 0;
	}

	public static int test_0_bge () {
		long a = 0xffffffffff;
		if (a < 0xffffffffff)
			return 1;

		if (a < 0x1ffffffffff) {} else
			return 2;

		if (a < 0xff00000000)
			return 3;

		if (a < 0xfeffffffff)
			return 4;

		a = 0xff00000000;
		if (a < 0xffffffffff) {} else
			return 5;

		return 0;
	}

	public static int test_0_bge_un () {
		ulong a = 0xffffffffff;
		if (a < 0xffffffffff)
			return 1;

		if (a < 0x1ffffffffff) {} else
			return 2;

		if (a < 0xff00000000)
			return 3;

		if (a < 0xfeffffffff)
			return 4;

		a = 0xff00000000;
		if (a < 0xffffffffff) {} else
			return 5;

		return 0;
	}

	public static int test_0_blt () {
		long a = 0xfffffffffe;
		if (a >= 0xffffffffff)
			return 1;

		if (a >= 0x1fffffffffe)
			return 2;

		if (a >= 0xff00000000) {} else
			return 3;

		if (a >= 0xfefffffffe) {} else
			return 4;

		a = 0xff00000000;
		if (a >= 0xffffffffff)
			return 5;

		return 0;
	}

	public static int test_0_blt_un () {
		ulong a = 0xfffffffffe;
		if (a >= 0xffffffffff)
			return 1;

		if (a >= 0x1fffffffffe)
			return 2;

		if (a >= 0xff00000000) {} else
			return 3;

		if (a >= 0xfefffffffe) {} else
			return 4;

		a = 0xff00000000;
		if (a >= 0xffffffffff)
			return 5;

		return 0;
	}

	public static int test_0_bgt () {
		long a = 0xffffffffff;
		if (a <= 0xfffffffffe)
			return 1;

		if (a <= 0x1ffffffffff) {} else
			return 2;

		if (a <= 0xff00000000)
			return 3;

		if (a <= 0xfeffffffff)
			return 4;

		a = 0xff00000000;
		if (a <= 0xffffffffff) {} else
			return 5;

		return 0;
	}

	public static int test_0_bgt_un () {
		ulong a = 0xffffffffff;
		if (a <= 0xfffffffffe)
			return 1;

		if (a <= 0x1ffffffffff) {} else
			return 2;

		if (a <= 0xff00000000)
			return 3;

		if (a <= 0xfeffffffff)
			return 4;

		a = 0xff00000000;
		if (a <= 0xffffffffff) {} else
			return 5;

		return 0;
	}

	public static int test_0_conv_to_i4 () {
		long a = 0;

		return (int)a;
	}

	public static int test_32_conv_to_u4 () {
		long a = 32;

		return (int)(uint)a;
	}

	public static int test_15_conv_to_u4_2 () {
		long a = 0x10000000f;

		return (int)(uint)a;
	}

	public static int test_0_conv_from_i4 () {
		long a = 2;
		if (a != 2)
			return 1;

		int b = 2;

		if (a != b)
		    return 2;
		return 0;
	}

	public static int test_0_conv_from_i4_negative () {
		long a = -2;
		if (a != -2)
			return 1;

		int b = -2;

		if (a != b)
		    return 2;
		return 0;
	}

	/*
	public static int test_0_conv_from_r8 () {
		double b = 2.0;
		long a = (long)b;

		if (a != 2)
			return 1;
		return 0;
	}

	public static int test_0_conv_from_r4 () {
		float b = 2.0F;
		long a = (long)b;

		if (a != 2)
			return 1;
		return 0;
	}
	*/
	
	public static int test_8_and () {
		long a = 0xffffffffff;
		long b = 8;		
		return (int)(a & b);
	}

	public static int test_8_and_imm () {
		long a = 0xffffffffff;
		return (int)(a & 8);
	}

	public static int get_high_bit (ulong a) {
		if ((a & 0x8000000000000000) != 0)
			return 1;
		return 0;
	}

	public static int test_1_and () {
		ulong a = 0xabcd1234deadbeef;
		return get_high_bit (a);
	}

	public static int test_10_or () {
		long a = 8;
		long b = 2;		
		return (int)(a | b);
	}

	public static int test_10_or_imm () {
		long a = 8;
		return (int)(a | 2);
	}

	public static int test_5_xor () {
		long a = 7;
		long b = 2;		
		return (int)(a ^ b);
	}

	public static int test_5_xor_imm () {
		long a = 7;
		return (int)(a ^ 2);
	}

	public static int test_5_add () {
		long a = 2;
		long b = 3;		
		return (int)(a + b);
	}

	public static int test_5_add_imm () {
		long a = 2;
		return (int)(a + 3);
	}

	public static int test_0_add_imm_carry () {
		long a = -1;
		return (int)(a + 1);
	}

	public static int test_0_add_imm_no_inc () {
		// we can't blindly convert an add x, 1 to an inc x
		long a = 0x1ffffffff;
		long c;
		c = a + 2;
		if (c == ((a + 1) + 1))
			return 0;
		return 1;
	}

	public static int test_4_addcc_imm () {
		long a = 3;
		long b = 0;
		return (int)(a - b + 1);
	}

	public static int test_5_sub () {
		long a = 8;
		long b = 3;		
		return (int)(a - b);
	}

	public static int test_5_sub_imm () {
		long a = 8;
		return (int)(a - 3);
	}

	public static int test_0_sub_imm_carry () {
		long a = 0;
		return (int)((a - 1) + 1);
	}

	public static int test_0_add_ovf () {
		long i, j, k;

		checked {
			i = System.Int64.MinValue;
			j = 0;
			k = i + j;
		}

		if (k != System.Int64.MinValue)
			return 1;

		checked {
			i = System.Int64.MaxValue;
			j = 0;
			k = i + j;
		}

		if (k != System.Int64.MaxValue)
			return 2;

		checked {
			i = System.Int64.MinValue;
			j = System.Int64.MaxValue;
			k = i + j;
		}

		if (k != -1)
			return 3;

		checked {
			i = System.Int64.MaxValue;
			j = System.Int64.MinValue;
			k = i + j;
		}

		if (k != -1)
			return 4;

		checked {
			i = System.Int64.MinValue + 1234;
			j = -1234;
			k = i + j;
		}

		if (k != System.Int64.MinValue)
			return 5;

		checked {
			i = System.Int64.MaxValue - 1234;
			j = 1234;
			k = i + j;
		}

		if (k != System.Int64.MaxValue)
			return 6;

		return 0;
	}

	public static int test_0_add_un_ovf () {
		ulong n = (ulong)134217728 * 16;
		ulong number = checked (n + (uint)0);

		return number == n ? 0 : 1;
	}

	public static int test_0_sub_ovf () {
		long i, j, k;

		checked {
			i = System.Int64.MinValue;
			j = 0;
			k = i - j;
		}

		if (k != System.Int64.MinValue)
			return 1;

		checked {
			i = System.Int64.MaxValue;
			j = 0;
			k = i - j;
		}

		if (k != System.Int64.MaxValue)
			return 2;

		checked {
			i = System.Int64.MinValue;
			j = System.Int64.MinValue + 1234;
			k = i - j;
		}

		if (k != -1234)
			return 3;

		checked {
			i = System.Int64.MaxValue;
			j = 1234;
			k = i - j;
		}

		if (k != System.Int64.MaxValue - 1234)
			return 4;

		checked {
			i = System.Int64.MaxValue - 1234;
			j = -1234;
			k = i - j;
		}

		if (k != System.Int64.MaxValue)
			return 5;

		checked {
			i = System.Int64.MinValue + 1234;
			j = 1234;
			k = i - j;
		}

		if (k != System.Int64.MinValue)
			return 6;

		return 0;
	}

	public static int test_0_sub_ovf_un () {
		ulong i, j, k;

		checked {
			i = System.UInt64.MaxValue;
			j = 0;
			k = i - j;
		}

		if (k != System.UInt64.MaxValue)
			return 1;

		checked {
			i = System.UInt64.MaxValue;
			j = System.UInt64.MaxValue;
			k = i - j;
		}

		if (k != 0)
			return 2;

		return 0;
	}

	public static int test_2_neg () {
		long a = -2;		
		return (int)(-a);
	}	

	public static int test_0_neg_large () {
		long min = -9223372036854775808;
		unchecked {
			ulong ul = (ulong)min;
			return (min == -(long)ul) ? 0 : 1;
		}
	}	

	public static int test_5_shift ()
	{
		long a = 9;
		int b = 1;
		int count = 0;
		
		if ((a >> b) != 4)
			return count;
		count++;

		if ((a >> 63) != 0)
			return count;
		count++;

		if ((a << 1) != 18)
			return count;
		count++;

		if ((a << b) != 18)
			return count;
		count++;

		a = -9;
		if ((a >> b) != -5)
			return count;
		count++;

		return count;
	}

	public static int test_1_shift_u ()
	{
		ulong a;
		int count = 0;

		// The JIT optimizes this
		a = 8589934592UL;
		if ((a >> 32) != 2)
			return 0;
		count ++;

		return count;
	}

	public static int test_1_shift_u_32 ()
	{
		ulong a;
		int count = 0;

		a = UInt64.MaxValue;
		// Avoid constant folding
		for (int i = 0; i < 32; ++i)
			count ++;

		if ((a >> count) != 0xFFFFFFFFUL)
			return 0;
		else
			return 1;
	}

	public static int test_1_simple_neg () {
		long a = 9;
		
		if (-a != -9)
			return 0;
		return 1;
	}

	public static int test_2_compare () {
		long a = 1;
		long b = 1;
		
		if (a != b)
			return 0;
		return 2;
	}

	public static int test_9_alu ()
	{
		long a = 9, b = 6;
		int count = 0;
		
		if ((a + b) != 15)
			return count;
		count++;
		
		if ((a - b) != 3)
			return count;
		count++;

		if ((a & 8) != 8)
			return count;
		count++;

		if ((a | 2) != 11)
			return count;
		count++;

		if ((a * b) != 54)
			return count;
		count++;
		
		if ((a / 4) != 2)
			return count;
		count++;
		
		if ((a % 4) != 1)
			return count;
		count++;

		if (-a != -9)
			return count;
		count++;

		b = -1;
		if (~b != 0)
			return count;
		count++;

		return count;
	}
	
	public static int test_24_mul () {
		long a = 8;
		long b = 3;		
		return (int)(a * b);
	}	
	
	public static int test_24_mul_ovf () {
		long a = 8;
		long b = 3;
		long res;
		
		checked {
			res = a * b;
		}
		return (int)res;
	}	

	public static int test_24_mul_un () {
		ulong a = 8;
		ulong b = 3;		
		return (int)(a * b);
	}	
	
	public static int test_24_mul_ovf_un () {
		ulong a = 8;
		ulong b = 3;
		ulong res;
		
		checked {
			res = a * b;
		}
		return (int)res;
	}	

	public static int test_0_mul_imm () {
	    long i = 4;

		if ((i * 0) != 0)
			return 1;
		if ((i * 1) != 4)
			return 2;
		if ((i * 2) != 8)
			return 3;
		if ((i * 3) != 12)
			return 4;
		if ((i * 1234) != 4936)
			return 5;
		if ((i * -1) != -4)
			return 6;
		if ((i * -2) != -8)
			return 7;
		if ((i * -3) != -12)
			return 8;
		if ((i * -1234) != -4936)
			return 9;

		return 0;
	}

	public static int test_0_mul_imm_opt ()
	{
		long i;

		i = 1;
		if ((i * 2) != 2)
			return 1;
		i = -1;
		if ((i * 2) != -2)
			return 2;
		i = 1;
		if ((i * 3) != 3)
			return 3;
		i = -1;
		if ((i * 3) != -3)
			return 4;
		i = 1;
		if ((i * 5) != 5)
			return 5;
		i = -1;
		if ((i * 5) != -5)
			return 6;
		i = 1;
		if ((i * 6) != 6)
			return 7;
		i = -1;
		if ((i * 6) != -6)
			return 8;
		i = 1;
		if ((i * 9) != 9)
			return 9;
		i = -1;
		if ((i * 9) != -9)
			return 10;
		i = 1;
		if ((i * 10) != 10)
			return 11;
		i = -1;
		if ((i * 10) != -10)
			return 12;
		i = 1;
		if ((i * 12) != 12)
			return 13;
		i = -1;
		if ((i * 12) != -12)
			return 14;
		i = 1;
		if ((i * 25) != 25)
			return 15;
		i = -1;
		if ((i * 25) != -25)
			return 16;
		i = 1;
		if ((i * 100) != 100)
			return 17;
		i = -1;
		if ((i * 100) != -100)
			return 18;
		
		return 0;
	}
	
	public static int test_4_divun () {
		uint b = 12;
		int a = 3;
		return (int)(b / a);
	}

	public static int test_1431655764_bigdivun_imm () {
		unchecked {
			uint b = (uint)-2;
			return (int)(b / 3);
		}
	}

	public static int test_1431655764_bigdivun () {
		unchecked {
			uint b = (uint)-2;
			int a = 3;
			return (int)(b / a);
		}
	}

	public static int test_1_remun () {
		uint b = 13;
		int a = 3;
		return (int)(b % a);
	}

	public static int test_2_bigremun () {
		unchecked {
			uint b = (uint)-2;
			int a = 3;
			return (int)(b % a);
		}
	}

	public static int test_0_ceq () {
		long a = 2;
		long b = 2;
		long c = 3;
		long d = 0xff00000002;
		
		bool val = (a == b); // this should produce a ceq
		if (!val)
			return 1;
		
		val = (a == c); // this should produce a ceq
		if (val)
			return 2;
		
		val = (a == d); // this should produce a ceq
		if (val)
			return 3;
		
		return 0;
	}

	public static int test_0_ceq_complex () {
                long l = 1, ll = 2;

                if (l < 0 != ll < 0)
                        return 1;

                return 0;
	}
	
	public static int test_0_clt () {
		long a = 2;
		long b = 2;
		long c = 3;
		long d = 0xff00000002L;
		long e = -1;
		
		bool val = (a < b); // this should produce a clt
		if (val)
			return 1;
		
		val = (a < c); // this should produce a clt
		if (!val)
			return 2;
		
		val = (c < a); // this should produce a clt
		if (val)
			return 3;
		
		val = (e < d); // this should produce a clt
		if (!val)
			return 4;
		
		val = (d < e); // this should produce a clt
		if (val)
			return 5;
		
		return 0;
	}
	
	public static int test_0_clt_un () {
		ulong a = 2;
		ulong b = 2;
		ulong c = 3;
		ulong d = 0xff00000002;
		ulong e = 0xffffffffffffffff;
		
		bool val = (a < b); // this should produce a clt_un
		if (val)
			return 1;
		
		val = (a < c); // this should produce a clt_un
		if (!val)
			return 1;
		
		val = (d < e); // this should produce a clt_un
		if (!val)
			return 1;
		
		val = (e < d); // this should produce a clt_un
		if (val)
			return 1;
		
		return 0;
	}

	public static int test_0_cgt () {
		long a = 2;
		long b = 2;
		long c = 3;
		long d = 0xff00000002L;
		long e = -1;
		
		bool val = (a > b); // this should produce a cgt
		if (val)
			return 1;
		
		val = (a > c); // this should produce a cgt
		if (val)
			return 2;
		
		val = (c > a); // this should produce a cgt
		if (!val)
			return 3;
		
		val = (e > d); // this should produce a cgt
		if (val)
			return 4;
		
		val = (d > e); // this should produce a cgt
		if (!val)
			return 5;
		
		return 0;
	}

	public static int test_0_cgt_un () {
		ulong a = 2;
		ulong b = 2;
		ulong c = 3;
		ulong d = 0xff00000002;
		ulong e = 0xffffffffffffffff;
		
		bool val = (a > b); // this should produce a cgt_un
		if (val)
			return 1;
		
		val = (a > c); // this should produce a cgt_un
		if (val)
			return 1;
		
		val = (d > e); // this should produce a cgt_un
		if (val)
			return 1;
		
		val = (e > d); // this should produce a cgt_un
		if (!val)
			return 1;
		
		return 0;
	}

	public static int test_3_byte_cast () {
		ulong val = 0xff00ff00f0f0f0f0;
		byte b;
		b = (byte) (val & 0xFF);
		if (b != 0xf0)
			return 1;

		return 3;
	}

	public static int test_4_ushort_cast () {
		ulong val = 0xff00ff00f0f0f0f0;
		ushort b;
		b = (ushort) (val & 0xFFFF);
		if (b != 0xf0f0)
			return 1;
		return 4;
	}

	public static int test_500_mul_div () {
		long val = 1000;
		long exp = 10;
		long maxexp = 20;
		long res = val * exp / maxexp;

		return (int)res;
	}
	
	public static int test_3_checked_cast_un () {
                ulong i = 2;
                long j;

                checked { j = (long)i; }

		if (j != 2)
			return 0;
		return 3;
	}
	
	public static int test_4_checked_cast () {
                long i = 3;
                ulong j;

                checked { j = (ulong)i; }

		if (j != 3)
			return 0;
		return 4;
	}

	public static int test_12_checked_i1_cast () {
		long l = 12;

		checked {
			return (sbyte)l;
		}
	}

	public static int test_127_checked_i1_cast_un () {
		ulong l = 127;

		checked {
			return (sbyte)l;
		}
	}

	public static int test_1234_checked_i2_cast () {
		long l = 1234;

		checked {
			return (short)l;
		}
	}

	public static int test_32767_checked_i2_cast_un () {
		ulong l = 32767;

		checked {
			return (ushort)l;
		}
	}

	public static int test_1234_checked_i4_cast () {
		ulong ul = 1234;

		checked {
			return (int)ul;
		}
	}

	public static int test_10_int_uint_compare () {
		uint size = 10;
		int j = 0;
		for (int i = 0; i < size; ++i) {
			j++;
		}
		return j;
	}

	public static int test_0_ulong_regress () {
		ulong u = 4257145737;
		u --;
		return (u == 4257145736) ? 0 : 1;
	}

	public static int test_0_ulong_regress2 () {
		int p2 = 31;
		ulong sum_p = 2897079476 + (ulong) (1 << p2);
		return (sum_p == 749595828) ? 0 : 1;
	}
	
	public static int test_0_assemble_long ()
	{
		uint a = 5;
		ulong x = 0x12345678;
		ulong y = 1;
		
		
		ulong z = ((x - y) << 32) | a;
		
		if (z != 0x1234567700000005)
			return 1;
		
		return 0;
	}
	
	public static int test_0_hash ()
	{
		ulong x = 0x1234567887654321;
		int h = (int)(x & 0xffffffff) ^ (int)(x >> 32);
		if (h != unchecked ((int)(0x87654321 ^ 0x12345678)))
			return h;
		return 0;
				
	}

	public static int test_0_shift_regress () {
		long a = 0; 
		int b = 6; 
		UInt16 c = 3;

		return ((a >> (b - c)) == 0) ? 0 : 1;
	}

	public static int test_1234_conv_ovf_u8 () {
		int i = 1234;

		checked {
			ulong l = (ulong)i;
			return (int)l;
		}
	}

	public static int test_0_regress_cprop_80738 () {
		int hours = Int32.MinValue;
		int hrssec = (hours * 3600);
		long t = ((long)(hrssec) * 1000L);

		return t == 0 ? 0 : 1;
	}

	public static int test_0_conv_u () {
		unsafe {
			int** dead = (int**) 0xdeadbeaf;
			long i = (long)dead;
			return (i == 0xdeadbeaf) ? 0 : 1;
		}
	}

	public static int test_0_lconv_to_u2 () {
		unchecked { 
			ulong value = (ulong)(short)-10;
			value = (ushort)value;
		    return (value == 65526) ? 0 : 1;
		}
	}

	public static int test_0_lneg_regress_10320 () {
		long a = 0x100000000;
		ulong c = ((ulong) (-(-a))) >> 32;
		return c == 1 ? 0 : 1;
	}

	public static int test_6_lrem_un () {
		ulong r2 = 4294967296;
		uint d = 10;
		ulong res = r2 % d;

		return (int)res;
	}

	public static int test_0_lrem_imm_1 () {
		long b = 12L;
		return (int)(b % 1L);
	}

	public static int test_0_lrem_imm_1_neg () {
		long b = -2L;
		return (int)(b % 1L);
	}

	public static int test_0_lrem_imm_2 ()
	{
		long x = 245345634L;
		return (int)(x % 2L);
	}

	public static int test_1_lrem_imm_2 ()
	{
		long x = 24534553245L;
		return (int)(x % 2L);
	}

	public static int test_1_lrem_imm_2_neg ()
	{
		long x = -24534553245L;
		return -(int)(x % 2L);
	}

	public static int test_13_lrem_imm_32 ()
	{
		long x = 17389L;
		return (int)(x % 32L);
	}

	public static int test_27_lrem_imm_32_neg ()
	{
		long x = -2435323L;
		return -(int)(x % 32L);
	}

	public static int test_5_lrem_imm_large ()
	{
		long x = 0x1000000005L;
		return (int)(x % 0x40000000L);
	}

	public static int test_5_lrem_imm_too_large ()
	{
		long x = 0x1000000005L;
		return (int)(x % 0x80000000L);
	}
}

