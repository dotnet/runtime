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

#if __MOBILE__
class BasicTests
#else
class Tests
#endif
{

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif
	
	public static int test_0_return () {
		return 0;
	}

	public static int test_100000_return_large () {
		return 100000;
	}

	public static int test_1_load_bool () {
		bool a = true;
		return a? 1: 0;
	}

	public static int test_0_load_bool_false () {
		bool a = false;
		return a? 1: 0;
	}

	public static int test_200_load_byte () {
		byte a = 200;
		return a;
	}

	public static int test_100_load_sbyte () {
		sbyte a = 100;
		return a;
	}

	public static int test_200_load_short () {
		short a = 200;
		return a;
	}

	public static int test_100_load_ushort () {
		ushort a = 100;
		return a;
	}

	public static int test_3_add_simple () {
		int a = 1; 
		int b = 2;
		return a + b;
	}

	public static int test_3_add_imm () {
		int a = 1; 
		return a + 2;
	}

	public static int test_13407573_add_largeimm () {
		int a = 1; 
		return a + 13407572;
	}

	public static int test_1_sub_simple () {
		int a = 1; 
		int b = 2;
		return b - a;
	}

	public static int test_1_sub_simple_un () {
		uint a = 1; 
		uint b = 2;
		return (int)(b - a);
	}

	public static int test_1_sub_imm () {
		int b = 2;
		return b - 1;
	}

	public static int test_2_sub_large_imm () {
		int b = 0xff0f0f;
		return b - 0xff0f0d;
	}

	public static int test_0_sub_inv_imm () {
		int b = 2;
		return 2 - b;
	}

	public static int test_2_and () {
		int b = 2;
		int a = 3;
		return b & a;
	}

	public static int test_0_and_imm () {
		int b = 2;
		return b & 0x10;
	}

	public static int test_0_and_large_imm () {
		int b = 2;
		return b & 0x10000000;
	}

	public static int test_0_and_large_imm2 () {
		int b = 2;
		return b & 0x100000f0;
	}

	public static int test_2_div () {
		int b = 6;
		int a = 3;
		return b / a;
	}

	public static int test_4_div_imm () {
		int b = 12;
		return b / 3;
	}

	public static int test_4_divun_imm () {
		uint b = 12;
		return (int)(b / 3);
	}

	public static int test_0_div_fold () {
		int b = -1;
		return b / 2;
	}

	public static int test_2_div_fold4 () {
		int b = -8;
		return -(b / 4);
	}

	public static int test_2_div_fold16 () {
		int b = 32;
		return b / 16;
	}

	public static int test_719177_div_destreg () {
		int year = 1970;
		return ((365* (year-1)) + ((year-1)/4));
	}

	public static int test_1_remun_imm () {
		uint b = 13;
		return (int)(b % 3);
	}

	public static int test_2_bigremun_imm () {
		unchecked {
			uint b = (uint)-2;
			return (int)(b % 3);
		}
	}

	public static int test_2_rem () {
		int b = 5;
		int a = 3;
		return b % a;
	}

	public static int test_4_rem_imm () {
		int b = 12;
		return b % 8;
	}

	public static int test_0_rem_imm_0 () {
		int b = 12;
		return b % 1;
	}

	public static int test_0_rem_imm_0_neg () {
		int b = -2;
		return b % 1;
	}

	public static int test_4_rem_big_imm () {
		int b = 10004;
		return b % 10000;
	}

	public static int test_9_mul () {
		int b = 3;
		int a = 3;
		return b * a;
	}

	public static int test_15_mul_imm () {
		int b = 3;
		return b * 5;
	}

	public static int test_24_mul () {
		int a = 3;
		int b = 8;
		int res;

		res = a * b;
		
		return res;
	}

	public static int test_24_mul_ovf () {
		int a = 3;
		int b = 8;
		int res;

		checked {
			res = a * b;
		}
		
		return res;
	}

	public static int test_24_mul_un () {
		uint a = 3;
		uint b = 8;
		uint res;

		res = a * b;
		
		return (int)res;
	}

	public static int test_24_mul_ovf_un () {
		uint a = 3;
		uint b = 8;
		uint res;

		checked {
			res = a * b;
		}
		
		return (int)res;
	}

	public static int test_0_add_ovf1 () {
		int i, j, k;

		checked {
			i = System.Int32.MinValue;
			j = 0;
			k = i + j;
		}

		if (k != System.Int32.MinValue)
			return 1;
		return 0;
	}

	public static int test_0_add_ovf2 () {
		int i, j, k;

		checked {
			i = System.Int32.MaxValue;
			j = 0;
			k = i + j;
		}

		if (k != System.Int32.MaxValue)
			return 2;
		return 0;
	}

	public static int test_0_add_ovf3 () {
		int i, j, k;

		checked {
			i = System.Int32.MinValue;
			j = System.Int32.MaxValue;
			k = i + j;
		}

		if (k != -1)
			return 3;
		return 0;
	}

	public static int test_0_add_ovf4 () {
		int i, j, k;

		checked {
			i = System.Int32.MaxValue;
			j = System.Int32.MinValue;
			k = i + j;
		}

		if (k != -1)
			return 4;
		return 0;
	}

	public static int test_0_add_ovf5 () {
		int i, j, k;

		checked {
			i = System.Int32.MinValue + 1234;
			j = -1234;
			k = i + j;
		}

		if (k != System.Int32.MinValue)
			return 5;
		return 0;
	}

	public static int test_0_add_ovf6 () {
		int i, j, k;

		checked {
			i = System.Int32.MaxValue - 1234;
			j = 1234;
			k = i + j;
		}

		if (k != System.Int32.MaxValue)
			return 6;

		return 0;
	}

	public static int test_0_add_un_ovf () {
		uint n = (uint)134217728 * 16;
		uint number = checked (n + (uint)0);

		return number == n ? 0 : 1;
	}

	public static int test_0_sub_ovf1 () {
		int i, j, k;

		checked {
			i = System.Int32.MinValue;
			j = 0;
			k = i - j;
		}

		if (k != System.Int32.MinValue)
			return 1;

		return 0;
	}

	public static int test_0_sub_ovf2 () {
		int i, j, k;

		checked {
			i = System.Int32.MaxValue;
			j = 0;
			k = i - j;
		}

		if (k != System.Int32.MaxValue)
			return 2;

		return 0;
	}

	public static int test_0_sub_ovf3 () {
		int i, j, k;

		checked {
			i = System.Int32.MinValue;
			j = System.Int32.MinValue + 1234;
			k = i - j;
		}

		if (k != -1234)
			return 3;

		return 0;
	}

	public static int test_0_sub_ovf4 () {
		int i, j, k;

		checked {
			i = System.Int32.MaxValue;
			j = 1234;
			k = i - j;
		}

		if (k != System.Int32.MaxValue - 1234)
			return 4;

		return 0;
	}

	public static int test_0_sub_ovf5 () {
		int i, j, k;

		checked {
			i = System.Int32.MaxValue - 1234;
			j = -1234;
			k = i - j;
		}

		if (k != System.Int32.MaxValue)
			return 5;

		return 0;
	}

	public static int test_0_sub_ovf6 () {
		int i, j, k;

		checked {
			i = System.Int32.MinValue + 1234;
			j = 1234;
			k = i - j;
		}

		if (k != System.Int32.MinValue)
			return 6;

		return 0;
	}

	public static int test_0_sub_ovf_un () {
		uint i, j, k;

		checked {
			i = System.UInt32.MaxValue;
			j = 0;
			k = i - j;
		}

		if (k != System.UInt32.MaxValue)
			return 1;

		checked {
			i = System.UInt32.MaxValue;
			j = System.UInt32.MaxValue;
			k = i - j;
		}

		if (k != 0)
			return 2;

		return 0;
	}

	public static int test_3_or () {
		int b = 2;
		int a = 3;
		return b | a;
	}

	public static int test_3_or_un () {
		uint b = 2;
		uint a = 3;
		return (int)(b | a);
	}

	public static int test_3_or_short_un () {
		ushort b = 2;
		ushort a = 3;
		return (int)(b | a);
	}

	public static int test_18_or_imm () {
		int b = 2;
		return b | 0x10;
	}

	public static int test_268435458_or_large_imm () {
		int b = 2;
		return b | 0x10000000;
	}

	public static int test_268435459_or_large_imm2 () {
		int b = 2;
		return b | 0x10000001;
	}

	public static int test_1_xor () {
		int b = 2;
		int a = 3;
		return b ^ a;
	}

	public static int test_1_xor_imm () {
		int b = 2;
		return b ^ 3;
	}

	public static int test_983041_xor_imm_large () {
		int b = 2;
		return b ^ 0xf0003;
	}

	public static int test_1_neg () {
		int b = -2;
		b++;
		return -b;
	}

	public static int test_2_not () {
		int b = ~2;
		b = ~b;
		return b;
	}

	public static int test_16_shift () {
		int b = 2;
		int a = 3;
		return b << a;
	}
	
	public static int test_16_shift_add () {
		int b = 2;
		int a = 3;
		int c = 0;
		return b << (a + c);
	}
	
	public static int test_16_shift_add2 () {
		int b = 2;
		int a = 3;
		int c = 0;
		return (b + c) << a;
	}
	
	public static int test_16_shift_imm () {
		int b = 2;
		return b << 3;
	}
	
	public static int test_524288_shift_imm_large () {
		int b = 2;
		return b << 18;
	}
	
	public static int test_12_shift_imm_inv () {
		int b = 2;
		return 3 << 2;
	}

	public static int test_12_shift_imm_inv_sbyte () {
		sbyte b = 2;
		return 3 << 2;
	}

	public static int test_1_rshift_imm () {
		int b = 8;
		return b >> 3;
	}
	
	public static int test_2_unrshift_imm () {
		uint b = 16;
		return (int)(b >> 3);
	}
	
	public static int test_0_bigunrshift_imm () {
		unchecked {
			uint b = (uint)-1;
			b = b >> 1;
			if (b != 0x7fffffff)
				return 1;
			return 0;
		}
	}
	
	public static int test_0_bigrshift_imm () {
		int b = -1;
		b = b >> 1;
		if (b != -1)
			return 1;
		return 0;
	}
	
	public static int test_1_rshift () {
		int b = 8;
		int a = 3;
		return b >> a;
	}
	
	public static int test_2_unrshift () {
		uint b = 16;
		int a = 3;
		return (int)(b >> a);
	}
	
	public static int test_0_bigunrshift () {
		unchecked {
			uint b = (uint)-1;
			int a = 1;
			b = b >> a;
			if (b != 0x7fffffff)
				return 1;
			return 0;
		}
	}
	
	public static int test_0_bigrshift () {
		int b = -1;
		int a = 1;
		b = b >> a;
		if (b != -1)
			return 1;
		return 0;
	}
	
	public static int test_2_cond () {
		int b = 2, a = 3, c;
		if (a == b)
			return 0;
		return 2;
	}
	
	public static int test_2_cond_short () {
		short b = 2, a = 3, c;
		if (a == b)
			return 0;
		return 2;
	}
	
	public static int test_2_cond_sbyte () {
		sbyte b = 2, a = 3, c;
		if (a == b)
			return 0;
		return 2;
	}
	
	public static int test_6_cascade_cond () {
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
	
	public static int test_6_cascade_short () {
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

	public static int test_0_short_sign_extend () {
		int t1 = 0xffeedd;
		short s1 = (short)t1;
		int t2 = s1;

		if ((uint)t2 != 0xffffeedd) 
			return 1;
		else
			return 0;
	}

	public static int test_127_iconv_to_i1 () {
		int i = 0x100017f;
		sbyte s = (sbyte)i;

		return s;
	}

	public static int test_384_iconv_to_i2 () {
		int i = 0x1000180;
		short s = (short)i;

		return s;
	}
	
	public static int test_15_for_loop () {
		int i;
		for (i = 0; i < 15; ++i) {
		}
		return i;
	}
	
	public static int test_11_nested_for_loop () {
		int i, j = 0; /* mcs bug here if j not set */
		for (i = 0; i < 15; ++i) {
			for (j = 200; j >= 5; --j) ;
		}
		return i - j;
	}

	public static int test_11_several_nested_for_loops () {
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

	public static int test_0_conv_ovf_i1 () {
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
	
	public static int test_0_conv_ovf_i1_un () {
		uint c;

		checked {
			c = 127;
			sbyte b = (sbyte)c;
		}
		
		return 0;
	}
	
	public static int test_0_conv_ovf_i2 () {
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
	
	public static int test_0_conv_ovf_i2_un () {
		uint c;

		checked {
			c = 32767;
			Int16 b = (Int16)c;
		}
		
		return 0;
	}
	
	public static int test_0_conv_ovf_u2 () {
		int c;

		checked {
			c = 65535;
			UInt16 b = (UInt16)c;
		}
		
		return 0;
	}
	
	public static int test_0_conv_ovf_u2_un () {
		uint c;

		checked {
			c = 65535;
			UInt16 b = (UInt16)c;
		}
		
		return 0;
	}
	
	public static int test_0_conv_ovf_u4 () {
		int c;

		checked {
			c = 0x7fffffff;
			uint b = (uint)c;
		}
		
		return 0;
	}

	public static int test_0_conv_ovf_i4_un () {
		uint c;

		checked {
			c = 0x7fffffff;
			int b = (int)c;
		}

		return 0;
	}
	
	public static int test_0_bool () {
		bool val = true;
		if (val)
			return 0;
		return 1;
	}
	
	public static int test_1_bool_inverted () {
		bool val = true;
		if (!val)
			return 0;
		return 1;
	}

	public static int test_1_bool_assign () {
		bool val = true;
		val = !val; // this should produce a ceq
		if (val)
			return 0;
		return 1;
	}

	public static int test_1_bool_multi () {
		bool val = true;
		bool val2 = true;
		val = !val;
		if ((val && !val2) && (!val2 && val))
			return 0;
		return 1;
	}

	public static int test_16_spill () {
		int a = 1;
		int b = 2;
		int c = 3;
		int d = 4;
		int e = 5;

		return (1 + (a + (b + (c + (d + e)))));
	}

	public static int test_1_switch () {
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

	public static int test_0_switch_constprop () {
		int n = -1;

		switch (n) {
		case 0: return 2;
		case 1: return 3;
		case 2: return 3;			
		default:
			return 0;
		}
		return 3;
	}

	public static int test_0_switch_constprop2 () {
		int n = 3;

		switch (n) {
		case 0: return 2;
		case 1: return 3;
		case 2: return 3;			
		default:
			return 0;
		}
		return 3;
	}

	public static int test_0_while_loop_1 () {

		int value = 255;
		
		do {
			value = value >> 4;
		} while (value != 0);
		
		return 0;
	}

	public static int test_0_while_loop_2 () {
		int value = 255;
		int position = 5;
		
		do {
			value = value >> 4;
		} while (value != 0 && position > 1);
	
		return 0;
	}

	public static int test_0_char_conv () {
		int i = 1;
		
		char tc = (char) ('0' + i);

		if (tc != '1')
			return 1;
		
		return 0;
	}

	public static int test_3_shift_regalloc () {
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
	
	public static int test_2_optimize_branches () {
		switch (E.A) {
		case E.A:
			if (E.A == E.B) {
			}
			break;
		}
		return 2;
	}

	public static int test_0_checked_byte_cast () {
		int v = 250;
		int b = checked ((byte) (v));

		if (b != 250)
			return 1;
		return 0;
	}

	public static int test_0_checked_byte_cast_un () {
		uint v = 250;
		uint b = checked ((byte) (v));

		if (b != 250)
			return 1;
		return 0;
	}

	public static int test_0_checked_short_cast () {
		int v = 250;
		int b = checked ((ushort) (v));

		if (b != 250)
			return 1;
		return 0;
	}

	public static int test_0_checked_short_cast_un () {
		uint v = 250;
		uint b = checked ((ushort) (v));

		if (b != 250)
			return 1;
		return 0;
	}
	
	public static int test_1_a_eq_b_plus_a () {
		int a = 0, b = 1;
		a = b + a;
		return a;
	}

	public static int test_0_comp () {
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

	public static int test_0_comp_unsigned () {
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
	
	public static int test_16_cmov () 
	{
		int n = 0;
		if (n == 0)
			n = 16;
		
		return n;
	}

	public static int test_0_and_cmp ()
	{
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

	public static int test_0_mul_imm_opt ()
	{
		int i;

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
	
	public static int test_0_cne ()
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

	public static int test_0_cmp_regvar_zero ()
	{
		int n = 10;
		
		if (!(n > 0 && n >= 0 && n != 0))
			return 1;
		if (n < 0 || n <= 0 || n == 0)
			return 1;
		
		return 0;
	}

	public static int test_5_div_un_cfold ()
	{
		uint i = 10;
		uint j = 2;

		return (int)(i / j);
	}

	public static int test_1_rem_un_cfold ()
	{
		uint i = 11;
		uint j = 2;

		return (int)(i % j);
	}

	public static int test_0_div_opt () {
		int i;

		// Avoid cfolding this
		i = 0;
		for (int j = 0; j < 567; ++j)
			i ++;
		i += 1234000;
		if ((i / 2) != 617283)
			return 1;
		if ((i / 4) != 308641)
			return 2;
		if ((i / 8) != 154320)
			return 3;
		if ((i / 16) != 77160)
			return 4;

		// Avoid cfolding this
		i = 0;
		for (int j = 0; j < 567; ++j)
			i --;
		i -= 1234000;
		if ((i / 2) != -617283)
			return 5;
		if ((i / 4) != -308641)
			return 6;
		if ((i / 8) != -154320)
			return 7;
		if ((i / 16) != -77160)
			return 8;

		return 0;
	}

	public static int test_0_udiv_opt () {
		uint i;

		// Avoid cfolding this
		i = 0;
		for (int j = 0; j < 567; ++j)
			i ++;
		i += 1234000;
		if ((i / 2) != 617283)
			return 1;
		if ((i / 4) != 308641)
			return 2;
		if ((i / 8) != 154320)
			return 3;
		if ((i / 16) != 77160)
			return 4;

		return 0;
	}

	public static int test_0_signed_ct_div () {
		int n = 2147483647;
		bool divide_by_zero = false;
		bool overflow = false;

		if ((n / 2147483647) != 1)
			return 1;
		if ((n / -2147483647) != -1)
			return 2;
		n = -n;
		if ((n / 2147483647) != -1)
			return 3;
		n--; /* MinValue */
		if ((n / -2147483648) != 1)
			return 4;
		if ((n / 2147483647) != -1)
			return 5;
		if ((n / 1) != n)
			return 6;

		try {
			int r = n / (-1);
		} catch (OverflowException) {
			overflow = true;
		}
		if (!overflow)
			return 7;

		try {
			int r = n / 0;
		} catch (DivideByZeroException) {
			divide_by_zero = true;
		}
		if (!divide_by_zero)
			return 8;

		if ((n / 35) != -61356675)
			return 9;
		if ((n / -35) != 61356675)
			return 10;
		n = -(n + 1);  /* MaxValue */
		if ((n / 35) != 61356675)
			return 11;
		if ((n / -35) != -61356675)
			return 12;

		return 0;
	}

	public static int test_0_unsigned_ct_div () {
		uint n = 4294967295;
		bool divide_by_zero = false;

		if ((n / 4294967295) != 1)
			return 1;
		n--;
		if ((n / 4294967295) != 0)
			return 2;
		n++;
		if ((n / 4294967294) != 1)
			return 3;
		if ((n / 1) != n)
			return 4;

		try {
			uint a = n / 0;
		} catch (DivideByZeroException) {
			divide_by_zero = true;
		}

		if (!divide_by_zero)
			return 5;

		if ((n / 35) != 122713351)
			return 9;

		return 0;
	}

	public static int test_0_rem_opt () {
		int i;

		// Avoid cfolding this
		i = 0;
		for (int j = 0; j < 29; ++j)
			i ++;
		if ((i % 2) != 1)
			return 1;
		if ((i % 4) != 1)
			return 2;
		if ((i % 8) != 5)
			return 3;
		if ((i % 16) != 13)
			return 4;

		// Avoid cfolding this
		i = 0;
		for (int j = 0; j < 29; ++j)
			i --;
		if ((i % 2) != -1)
			return 5;
		if ((i % 4) != -1)
			return 6;
		if ((i % 8) != -5)
			return 7;
		if ((i % 16) != -13)
			return 8;

		return 0;
	}

	public static int cmov (int i) {
		int j = 0;

		if (i > 0)
			j = 1;

		return j;
	}

	public static int cmov2 (int i) {
		int j = 0;

		if (i <= 0)
			;
		else
			j = 1;

		return j;
	}
		
	public static int test_0_branch_to_cmov_opt () {
		if (cmov (0) != 0)
			return 1;
		if (cmov (1) != 1)
			return 2;
		if (cmov2 (0) != 0)
			return 1;
		if (cmov2 (1) != 1)
			return 2;
		return 0;
	}

	public static unsafe int test_0_ishr_sign_extend () {
		// Check that ishr does sign extension from bit 31 on 64 bit platforms
		uint val = 0xF0000000u;

		uint *a = &val;
		uint ui = (uint)((int)(*a) >> 2);

		if (ui != 0xfc000000)
			return 1;

		// Same with non-immediates
		int amount = 2;

		ui = (uint)((int)(*a) >> amount);

		if (ui != 0xfc000000)
			return 2;

		return 0;
	}

	public static unsafe int test_0_ishr_sign_extend_cfold () {
		int i = 32768;
		int j = i << 16;
		int k = j >> 16;

		return k == -32768 ? 0 : 1;
	}
}
