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
	
	static int test_0_return () {
		return 0;
	}

	static int test_100000_return_large () {
		return 100000;
	}

	static int test_1_load_bool () {
		bool a = true;
		return a? 1: 0;
	}

	static int test_0_load_bool_false () {
		bool a = false;
		return a? 1: 0;
	}

	static int test_200_load_byte () {
		byte a = 200;
		return a;
	}

	static int test_100_load_sbyte () {
		sbyte a = 100;
		return a;
	}

	static int test_200_load_short () {
		short a = 200;
		return a;
	}

	static int test_100_load_ushort () {
		ushort a = 100;
		return a;
	}

	static int test_3_add_simple () {
		int a = 1; 
		int b = 2;
		return a + b;
	}

	static int test_3_add_imm () {
		int a = 1; 
		return a + 2;
	}

	static int test_13407573_add_largeimm () {
		int a = 1; 
		return a + 13407572;
	}

	static int test_1_sub_simple () {
		int a = 1; 
		int b = 2;
		return b - a;
	}

	static int test_1_sub_simple_un () {
		uint a = 1; 
		uint b = 2;
		return (int)(b - a);
	}

	static int test_1_sub_imm () {
		int b = 2;
		return b - 1;
	}

	static int test_2_sub_large_imm () {
		int b = 0xff0f0f;
		return b - 0xff0f0d;
	}

	static int test_0_sub_inv_imm () {
		int b = 2;
		return 2 - b;
	}

	static int test_2_and () {
		int b = 2;
		int a = 3;
		return b & a;
	}

	static int test_0_and_imm () {
		int b = 2;
		return b & 0x10;
	}

	static int test_0_and_large_imm () {
		int b = 2;
		return b & 0x10000000;
	}

	static int test_0_and_large_imm2 () {
		int b = 2;
		return b & 0x100000f0;
	}

	static int test_2_div () {
		int b = 6;
		int a = 3;
		return b / a;
	}

	static int test_4_div_imm () {
		int b = 12;
		return b / 3;
	}

	static int test_4_divun_imm () {
		uint b = 12;
		return (int)(b / 3);
	}

	static int test_0_div_fold () {
		int b = -1;
		return b / 2;
	}

	static int test_719177_div_destreg () {
		int year = 1970;
		return ((365* (year-1)) + ((year-1)/4));
	}

	static int test_1_remun_imm () {
		uint b = 13;
		return (int)(b % 3);
	}

	static int test_2_bigremun_imm () {
		unchecked {
			uint b = (uint)-2;
			return (int)(b % 3);
		}
	}

	static int test_2_rem () {
		int b = 5;
		int a = 3;
		return b % a;
	}

	static int test_4_rem_imm () {
		int b = 12;
		return b % 8;
	}

	static int test_4_rem_big_imm () {
		int b = 10004;
		return b % 10000;
	}

	static int test_9_mul () {
		int b = 3;
		int a = 3;
		return b * a;
	}

	static int test_15_mul_imm () {
		int b = 3;
		return b * 5;
	}

	static int test_24_mul () {
		int a = 3;
		int b = 8;
		int res;

		res = a * b;
		
		return res;
	}

	static int test_24_mul_ovf () {
		int a = 3;
		int b = 8;
		int res;

		checked {
			res = a * b;
		}
		
		return res;
	}

	static int test_24_mul_un () {
		uint a = 3;
		uint b = 8;
		uint res;

		res = a * b;
		
		return (int)res;
	}

	static int test_24_mul_ovf_un () {
		uint a = 3;
		uint b = 8;
		uint res;

		checked {
			res = a * b;
		}
		
		return (int)res;
	}

	static int test_0_add_un_ovf () {
		uint n = (uint)134217728 * 16;
		uint number = checked (n + (uint)0);

		return number == n ? 0 : 1;
	}

	static int test_3_or () {
		int b = 2;
		int a = 3;
		return b | a;
	}

	static int test_3_or_un () {
		uint b = 2;
		uint a = 3;
		return (int)(b | a);
	}

	static int test_3_or_short_un () {
		ushort b = 2;
		ushort a = 3;
		return (int)(b | a);
	}

	static int test_18_or_imm () {
		int b = 2;
		return b | 0x10;
	}

	static int test_268435458_or_large_imm () {
		int b = 2;
		return b | 0x10000000;
	}

	static int test_268435459_or_large_imm2 () {
		int b = 2;
		return b | 0x10000001;
	}

	static int test_1_xor () {
		int b = 2;
		int a = 3;
		return b ^ a;
	}

	static int test_1_xor_imm () {
		int b = 2;
		return b ^ 3;
	}

	static int test_983041_xor_imm_large () {
		int b = 2;
		return b ^ 0xf0003;
	}

	static int test_1_neg () {
		int b = -2;
		b++;
		return -b;
	}

	static int test_2_not () {
		int b = ~2;
		b = ~b;
		return b;
	}

	static int test_16_shift () {
		int b = 2;
		int a = 3;
		return b << a;
	}
	
	static int test_16_shift_add () {
		int b = 2;
		int a = 3;
		int c = 0;
		return b << (a + c);
	}
	
	static int test_16_shift_add2 () {
		int b = 2;
		int a = 3;
		int c = 0;
		return (b + c) << a;
	}
	
	static int test_16_shift_imm () {
		int b = 2;
		return b << 3;
	}
	
	static int test_524288_shift_imm_large () {
		int b = 2;
		return b << 18;
	}
	
	static int test_12_shift_imm_inv () {
		int b = 2;
		return 3 << 2;
	}

	static int test_12_shift_imm_inv_sbyte () {
		sbyte b = 2;
		return 3 << 2;
	}

	static int test_1_rshift_imm () {
		int b = 8;
		return b >> 3;
	}
	
	static int test_2_unrshift_imm () {
		uint b = 16;
		return (int)(b >> 3);
	}
	
	static int test_0_bigunrshift_imm () {
		unchecked {
			uint b = (uint)-1;
			b = b >> 1;
			if (b != 0x7fffffff)
				return 1;
			return 0;
		}
	}
	
	static int test_0_bigrshift_imm () {
		int b = -1;
		b = b >> 1;
		if (b != -1)
			return 1;
		return 0;
	}
	
	static int test_1_rshift () {
		int b = 8;
		int a = 3;
		return b >> a;
	}
	
	static int test_2_unrshift () {
		uint b = 16;
		int a = 3;
		return (int)(b >> a);
	}
	
	static int test_0_bigunrshift () {
		unchecked {
			uint b = (uint)-1;
			int a = 1;
			b = b >> a;
			if (b != 0x7fffffff)
				return 1;
			return 0;
		}
	}
	
	static int test_0_bigrshift () {
		int b = -1;
		int a = 1;
		b = b >> a;
		if (b != -1)
			return 1;
		return 0;
	}
	
	static int test_2_cond () {
		int b = 2, a = 3, c;
		if (a == b)
			return 0;
		return 2;
	}
	
	static int test_2_cond_short () {
		short b = 2, a = 3, c;
		if (a == b)
			return 0;
		return 2;
	}
	
	static int test_2_cond_sbyte () {
		sbyte b = 2, a = 3, c;
		if (a == b)
			return 0;
		return 2;
	}
	
	static int test_6_cascade_cond () {
		int b = 2, a = 3, c;
		if (a == b)
			return 0;
		else if (b > a)
			return 1;
		else if (b != b)
			return 2;
		else {
			c = 1;
		}
		return a + b + c;
	}
	
	static int test_6_cascade_short () {
		short b = 2, a = 3, c;
		if (a == b)
			return 0;
		else if (b > a)
			return 1;
		else if (b != b)
			return 2;
		else {
			c = 1;
		}
		return a + b + c;
	}

	static int test_0_short_sign_extend () {
		int t1 = 0xffeedd;
		short s1 = (short)t1;
		int t2 = s1;

		if ((uint)t2 != 0xffffeedd) 
			return 1;
		else
			return 0;
	}		
	
	static int test_15_for_loop () {
		int i;
		for (i = 0; i < 15; ++i) {
		}
		return i;
	}
	
	static int test_11_nested_for_loop () {
		int i, j = 0; /* mcs bug here if j not set */
		for (i = 0; i < 15; ++i) {
			for (j = 200; j >= 5; --j) ;
		}
		return i - j;
	}

	static int test_11_several_nested_for_loops () {
		int i, j = 0; /* mcs bug here if j not set */
		for (i = 0; i < 15; ++i) {
			for (j = 200; j >= 5; --j) ;
		}
		i = j = 0;
		for (i = 0; i < 15; ++i) {
			for (j = 200; j >= 5; --j) ;
		}
		return i - j;
	}

	static int test_0_conv_ovf_i1 () {
		int c;

		//for (int j = 0; j < 10000000; j++)
		checked {
			c = 127;
			sbyte b = (sbyte)c;
			c = -128;
			b = (sbyte)c;
		}

		return 0;
	}
	
	static int test_0_conv_ovf_i1_un () {
		uint c;

		checked {
			c = 127;
			sbyte b = (sbyte)c;
		}
		
		return 0;
	}
	
	static int test_0_conv_ovf_i2 () {
		int c;

		checked {
			c = 32767;
			Int16 b = (Int16)c;
			c = -32768;
			b = (Int16)c;
			unchecked {
				uint u = 0xfffffffd;
				c = (int)u;
			}
			b = (Int16)c;
		}
		
		return 0;
	}
	
	static int test_0_conv_ovf_i2_un () {
		uint c;

		checked {
			c = 32767;
			Int16 b = (Int16)c;
		}
		
		return 0;
	}
	
	static int test_0_conv_ovf_u2 () {
		int c;

		checked {
			c = 65535;
			UInt16 b = (UInt16)c;
		}
		
		return 0;
	}
	
	static int test_0_conv_ovf_u2_un () {
		uint c;

		checked {
			c = 65535;
			UInt16 b = (UInt16)c;
		}
		
		return 0;
	}
	
	static int test_0_conv_ovf_u4 () {
		int c;

		checked {
			c = 0x7fffffff;
			uint b = (uint)c;
		}
		
		return 0;
	}
	
	static int test_0_bool () {
		bool val = true;
		if (val)
			return 0;
		return 1;
	}
	
	static int test_1_bool_inverted () {
		bool val = true;
		if (!val)
			return 0;
		return 1;
	}

	static int test_1_bool_assign () {
		bool val = true;
		val = !val; // this should produce a ceq
		if (val)
			return 0;
		return 1;
	}

	static int test_1_bool_multi () {
		bool val = true;
		bool val2 = true;
		val = !val;
		if ((val && !val2) && (!val2 && val))
			return 0;
		return 1;
	}

	static int test_16_spill () {
		int a = 1;
		int b = 2;
		int c = 3;
		int d = 4;
		int e = 5;

		return (1 + (a + (b + (c + (d + e)))));
	}

	static int test_1_switch () {
		int n = 0;

		switch (n) {
		case 0: return 1;
		case 1: return 2;
		case -1: return 3;
		default:
			return 4;
		}
		return 1;
	}


	static int test_0_while_loop_1 () {

		int value = 255;
		
		do {
			value = value >> 4;
		} while (value != 0);
		
		return 0;
	}

	static int test_0_while_loop_2 () {
		int value = 255;
		int position = 5;
		
		do {
			value = value >> 4;
		} while (value != 0 && position > 1);
	
		return 0;
	}

	static int test_0_char_conv () {
		int i = 1;
		
		char tc = (char) ('0' + i);

		if (tc != '1')
			return 1;
		
		return 0;
	}

	static unsafe int test_0_pin_string () {
		string x = "xxx";
		fixed (char *c = x) {
			if (*c != 'x')
				return 1;
		}
		return 0;
	}

	static int test_3_shift_regalloc () {
		int shift = 8;
		int orig = 1;
		byte value = 0xfe;

		orig &= ~(0xff << shift);
		orig |= value << shift;

		if (orig == 0xfe01)
			return 3;
		return 0;
	}

	enum E {A, B};
	
	static int test_2_optimize_branches () {
		switch (E.A) {
		case E.A:
			if (E.A == E.B) {
			}
			break;
		}
		return 2;
	}

	static int test_0_checked_byte_cast () {
		int v = 250;
		int b = checked ((byte) (v));

		if (b != 250)
			return 1;
		return 0;
	}

	static int test_0_checked_byte_cast_un () {
		uint v = 250;
		uint b = checked ((byte) (v));

		if (b != 250)
			return 1;
		return 0;
	}

	static int test_0_checked_short_cast () {
		int v = 250;
		int b = checked ((ushort) (v));

		if (b != 250)
			return 1;
		return 0;
	}

	static int test_0_checked_short_cast_un () {
		uint v = 250;
		uint b = checked ((ushort) (v));

		if (b != 250)
			return 1;
		return 0;
	}
	
	static int test_1_a_eq_b_plus_a () {
		int a = 0, b = 1;
		a = b + a;
		return a;
	}

	static int test_0_comp () {
		int a = 0;
		int b = -1;
		int error = 1;
		bool val;

		val = a < b;
		if (val)
			return error;
		error++;

		val = a > b;
		if (!val)
			return error;
		error ++;

		val = a == b;
		if (val)
			return error;
		error ++;

		val = a == a;
		if (!val)
			return error;
		error ++;

		return 0;
	}

	static int test_0_comp_unsigned () {
		uint a = 1;
		uint b = 0xffffffff;
		int error = 1;
		bool val;

		val = a < b;
		if (!val)
			return error;
		error++;

		val = a <= b;
		if (!val)
			return error;
		error++;

		val = a == b;
		if (val)
			return error;
		error++;

		val = a >= b;
		if (val)
			return error;
		error++;

		val = a > b;
		if (val)
			return error;
		error++;

		val = b < a;
		if (val)
			return error;
		error++;

		val = b <= a;
		if (val)
			return error;
		error++;

		val = b == a;
		if (val)
			return error;
		error++;

		val = b > a;
		if (!val)
			return error;
		error++;

		val = b >= a;
		if (!val)
			return error;
		error++;

		return 0;
	}
	
	static int test_16_cmov () 
	{
		int n = 0;
		if (n == 0)
			n = 16;
		
		return n;
	}
	
	static int my_flags;
	static int test_0_and_cmp ()
	{
		
		/* various forms of test [mem], imm */
		
		my_flags = 0x01020304;
		
		if ((my_flags & 0x01020304) == 0)
			return 1;
		
		if ((my_flags & 0x00000304) == 0)
			return 2;
		
		if ((my_flags & 0x00000004) == 0)
			return 3;
		
		if ((my_flags & 0x00000300) == 0)
			return 4;
		
		if ((my_flags & 0x00020000) == 0)
			return 5;
		
		if ((my_flags & 0x01000000) == 0)
			return 6;
		
		/* test esi, imm */
		int local = 0x01020304;
		
		if ((local & 0x01020304) == 0)
			return 7;
		
		if ((local & 0x00000304) == 0)
			return 8;
		
		if ((local & 0x00000004) == 0)
			return 9;
		
		if ((local & 0x00000300) == 0)
			return 10;
		
		if ((local & 0x00020000) == 0)
			return 11;
		
		if ((local & 0x01000000) == 0)
			return 12;
		
		return 0;
	}
	
	static int test_0_cne ()
	{
		int x = 0;
		int y = 1;
		
		bool b = x != y;
		bool bb = x != x;
		
		if (!b)
			return 1;
		if (bb)
			return 2;
		
		return 0;
	}
	
	static byte b;
	static int test_0_byte_compares ()
	{
		b = 0xff;
		if (b == -1)
			return 1;
		b = 0;
		if (!(b < System.Byte.MaxValue))
			return 2;
		
		if (!(b <= System.Byte.MaxValue))
			return 3;
		
		return 0;
	}
	static int test_0_cmp_regvar_zero ()
	{
		int n = 10;
		
		if (!(n > 0 && n >= 0 && n != 0))
			return 1;
		if (n < 0 || n <= 0 || n == 0)
			return 1;
		
		return 0;
	}

}
