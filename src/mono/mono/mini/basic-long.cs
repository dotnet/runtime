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

	static int test_10_simple_cast () {
		long a = 10;
		return (int)a;
	}

	static int test_1_bigmul1 () {
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

	static int test_1_bigmul2 () {
                int a = System.Int32.MaxValue, b = System.Int32.MaxValue;
                long s = System.Int64.MinValue;
                long c;
                c = s + (long) a * (long) b;
		if (c == -4611686022722355199)
			return 1;
		return 0;
	}
	
	static int test_1_bigmul3 () {
                int a = 10, b = 10;
                ulong c;
                c = (ulong) a * (ulong) b;
		if (c == 100)
			return 1;
		return 0;
	}

	static int test_1_bigmul4 () {
                int a = System.Int32.MaxValue, b = System.Int32.MaxValue;
                ulong c;
                c = (ulong) a * (ulong) b;
		if (c == 4611686014132420609)
			return 1;
		return 0;
	}
	
	static int test_1_bigmul5 () {
                int a = System.Int32.MaxValue, b = System.Int32.MinValue;
                long c;
                c = (long) a * (long) b;
		if (c == -4611686016279904256)
			return 1;
		return 0;
	}
	
	static int test_1_bigmul6 () {
                uint a = System.UInt32.MaxValue, b = System.UInt32.MaxValue/(uint)2;
                ulong c;
                c = (ulong) a * (ulong) b;
		if (c == 9223372030412324865)
			return 1;
		return 0;
	}
	
	static int test_0_beq () {
		long a = 0xffffffffff;
		if (a != 0xffffffffff)
			return 1;
		return 0;
	}

	static int test_0_bne_un () {
		long a = 0xffffffffff;
		if (a == 0xfffffffffe)
			return 1;
		return 0;
	}

	static int test_0_ble () {
		long a = 0xffffffffff;
		if (a > 0xffffffffff)
			return 1;
		return 0;
	}

	static int test_0_ble_un () {
		ulong a = 0xffffffffff;
		if (a > 0xffffffffff)
			return 1;
		return 0;
	}

	static int test_0_bge () {
		long a = 0xffffffffff;
		if (a < 0xffffffffff)
			return 1;
		return 0;
	}

	static int test_0_bge_un () {
		ulong a = 0xffffffffff;
		if (a < 0xffffffffff)
			return 1;
		return 0;
	}

	static int test_0_blt () {
		long a = 0xfffffffffe;
		if (a >= 0xffffffffff)
			return 1;
		return 0;
	}

	static int test_0_blt_un () {
		ulong a = 0xfffffffffe;
		if (a >= 0xffffffffff)
			return 1;
		return 0;
	}

	static int test_0_bgt () {
		long a = 0xffffffffff;
		if (a <= 0xfffffffffe)
			return 1;
		return 0;
	}

	static int test_0_conv_to_i4 () {
		long a = 0;

		return (int)a;
	}
	static int test_0_conv_from_i4 () {
		long a = 2;
		if (a != 2)
			return 1;

		int b = 2;

		if (a != b)
		    return 2;
		return 0;
	}

	static int test_0_conv_from_i4_negative () {
		long a = -2;
		if (a != -2)
			return 1;

		int b = -2;

		if (a != b)
		    return 2;
		return 0;
	}

	/*
	static int test_0_conv_from_r8 () {
		double b = 2.0;
		long a = (long)b;

		if (a != 2)
			return 1;
		return 0;
	}

	static int test_0_conv_from_r4 () {
		float b = 2.0F;
		long a = (long)b;

		if (a != 2)
			return 1;
		return 0;
	}
	*/
	
	static int test_8_and () {
		long a = 0xffffffffff;
		long b = 8;		
		return (int)(a & b);
	}

	static int test_8_and_imm () {
		long a = 0xffffffffff;
		return (int)(a & 8);
	}

	static int test_10_or () {
		long a = 8;
		long b = 2;		
		return (int)(a | b);
	}

	static int test_10_or_imm () {
		long a = 8;
		return (int)(a | 2);
	}

	static int test_5_xor () {
		long a = 7;
		long b = 2;		
		return (int)(a ^ b);
	}

	static int test_5_xor_imm () {
		long a = 7;
		return (int)(a ^ 2);
	}

	static int test_5_add () {
		long a = 2;
		long b = 3;		
		return (int)(a + b);
	}

	static int test_5_add_imm () {
		long a = 2;
		return (int)(a + 3);
	}

	static int test_0_add_imm_carry () {
		long a = -1;
		return (int)(a + 1);
	}

	static int test_0_add_imm_no_inc () {
		// we can't blindly convert an add x, 1 to an inc x
		long a = 0x1ffffffff;
		long c;
		c = a + 2;
		if (c == ((a + 1) + 1))
			return 0;
		return 1;
	}

	static int test_5_sub () {
		long a = 8;
		long b = 3;		
		return (int)(a - b);
	}

	static int test_5_sub_imm () {
		long a = 8;
		return (int)(a - 3);
	}

	static int test_0_sub_imm_carry () {
		long a = 0;
		return (int)((a - 1) + 1);
	}

	static int test_2_neg () {
		long a = -2;		
		return (int)(-a);
	}	

	static int test_0_neg_large () {
		long min = -9223372036854775808;
		unchecked {
			ulong ul = (ulong)min;
			return (min == -(long)ul) ? 0 : 1;
		}
	}	

	static int test_0_shl () {
		long a = 9;
		int b = 1;
		
		if ((a >> b) != 4)
			return 1;


		return 0;
	}
	
	static int test_1_rshift ()
	{
		long a = 9;
		int b = 1;
		a = -9;
		if ((a >> b) != -5)
			return 0;
		return 1;
	}

	static int test_5_shift ()
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

	static int test_1_shift_u ()
	{
		ulong a;
		int b;
		int count = 0;

		// The JIT optimizes this
		a = 8589934592UL;
		if ((a >> 32) != 2)
			return 0;
		count ++;

		return count;
	}

	static int test_1_simple_neg () {
		long a = 9;
		
		if (-a != -9)
			return 0;
		return 1;
	}

	static int test_2_compare () {
		long a = 1;
		long b = 1;
		
		if (a != b)
			return 0;
		return 2;
	}

	static int test_9_alu ()
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
	
	static int test_24_mul () {
		long a = 8;
		long b = 3;		
		return (int)(a * b);
	}	
	
	static int test_24_mul_ovf () {
		long a = 8;
		long b = 3;
		long res;
		
		checked {
			res = a * b;
		}
		return (int)res;
	}	

	static int test_24_mul_un () {
		ulong a = 8;
		ulong b = 3;		
		return (int)(a * b);
	}	
	
	static int test_24_mul_ovf_un () {
		ulong a = 8;
		ulong b = 3;
		ulong res;
		
		checked {
			res = a * b;
		}
		return (int)res;
	}	
	
	static int test_4_divun () {
		uint b = 12;
		int a = 3;
		return (int)(b / a);
	}

	static int test_1431655764_bigdivun_imm () {
		unchecked {
			uint b = (uint)-2;
			return (int)(b / 3);
		}
	}

	static int test_1431655764_bigdivun () {
		unchecked {
			uint b = (uint)-2;
			int a = 3;
			return (int)(b / a);
		}
	}

	static int test_1_remun () {
		uint b = 13;
		int a = 3;
		return (int)(b % a);
	}

	static int test_2_bigremun () {
		unchecked {
			uint b = (uint)-2;
			int a = 3;
			return (int)(b % a);
		}
	}

	static int test_0_ceq () {
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

	static int test_0_ceq_complex () {
                long l = 1, ll = 2;

                if (l < 0 != ll < 0)
                        return 1;

                return 0;
	}
	
	static int test_0_clt () {
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
	
	static int test_0_clt_un () {
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

	static int test_0_cgt () {
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

	static int test_0_cgt_un () {
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

	static long return_5low () {
		return 5;
	}
	
	static long return_5high () {
		return 0x500000000;
	}

	static int test_3_long_ret () {
		long val = return_5low ();
		return (int) (val - 2);
	}

	static int test_1_long_ret2 () {
		long val = return_5high ();
		if (val > 0xffffffff)
			return 1;
		return 0;
	}

	static int test_3_byte_cast () {
		ulong val = 0xff00ff00f0f0f0f0;
		byte b;
		b = (byte) (val & 0xFF);
		if (b != 0xf0)
			return 1;

		return 3;
	}

	static int test_4_ushort_cast () {
		ulong val = 0xff00ff00f0f0f0f0;
		ushort b;
		b = (ushort) (val & 0xFFFF);
		if (b != 0xf0f0)
			return 1;
		return 4;
	}

	static int test_500_mul_div () {
		long val = 1000;
		long exp = 10;
		long maxexp = 20;
		long res = val * exp / maxexp;

		return (int)res;
	}

	static long position = 0;

	static int test_4_static_inc_long () {

		int count = 4;

		position = 0;

		position += count;

		return (int)position;
	}
	
	static void doit (double value, out long m) {
		m = (long) value;
	}
	
	static int test_3_checked_cast_un () {
                ulong i = 2;
                long j;

                checked { j = (long)i; }

		if (j != 2)
			return 0;
		return 3;
	}
	
	static int test_4_checked_cast () {
                long i = 3;
                ulong j;

                checked { j = (ulong)i; }

		if (j != 3)
			return 0;
		return 4;
	}

	static int test_10_int_uint_compare () {
		uint size = 10;
		int j = 0;
		for (int i = 0; i < size; ++i) {
			j++;
		}
		return j;
	}

	static int test_0_ftol_clobber () {
		long m;
		doit (1.3, out m);
		if (m != 1)
			return 2;
		return 0;
	}

	static int test_71_long_shift_right () {
		ulong value = 38654838087;
		int x = 0;
		byte [] buffer = new byte [1];
		buffer [x] = ((byte)(value >> x));
		return buffer [x];
	}

	static int test_0_ulong_regress () {
		ulong u = 4257145737;
		u --;
		return (u == 4257145736) ? 0 : 1;
	}
	
	static long x;
	static int test_0_addsub_mem ()
	{
		x = 0;
		x += 5;
		
		if (x != 5)
			return 1;
		
		x -= 10;
		
		if (x != -5)
			return 2;
		
		return 0;
	}
	
	static ulong y;
	static int test_0_sh32_mem ()
	{
		y = 0x0102130405060708;
		y >>= 32;
		
		if (y != 0x01021304)
			return 1;
		
		y = 0x0102130405060708;
		y <<= 32;
		
		if (y != 0x0506070800000000)
			return 2;
		
		x = 0x0102130405060708;
		x <<= 32;
		
		if (x != 0x0506070800000000)
			return 2;
		
		return 0;
	}
	
	static int test_0_assemble_long ()
	{
		uint a = 5;
		ulong x = 0x12345678;
		ulong y = 1;
		
		
		ulong z = ((x - y) << 32) | a;
		
		if (z != 0x1234567700000005)
			return 1;
		
		return 0;
	}
	
	static int test_0_hash ()
	{
		ulong x = 0x1234567887654321;
		int h = (int)(x & 0xffffffff) ^ (int)(x >> 32);
		if (h != unchecked ((int)(0x87654321 ^ 0x12345678)))
			return h;
		return 0;
				
	}

	static int test_0_shift_regress () {
		long a = 0; 
		int b = 6; 
		UInt16 c = 3;

		return ((a >> (b - c)) == 0) ? 0 : 1;
	}
}

