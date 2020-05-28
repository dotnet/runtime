using System;

/*
 * Register allocator tests.
 */

public class Tests {

	public static int Main (String[] args) {
		return TestDriver.RunTests (typeof (Tests));
	}

	static void call_clobber_inner () {
	}

	public static int test_15_clobber_1 () {
		int a = 0;
		int b = 0;
		for (int i = 0; i < 10; ++i)
			a ++;
		for (int i = 0; i < 5; ++i)
			b ++;

		// clob == '1' and dreg == sreg2
		a = b + a;
		return a;
	}

	public static int test_15_clobber_1_fp () {
		float a = 0;
		float b = 0;
		for (int i = 0; i < 10; ++i)
			a ++;
		for (int i = 0; i < 5; ++i)
			b ++;

		// clob == '1' and dreg == sreg2
		a = b + a;
		return (int)a;
	}

	public static int test_5_call_clobber () {
		// A call clobbers some registers so variables in those registers need to be spilled
		// and later reloaded to a register
		int a = 2;
		int b = 3;

		call_clobber_inner ();

		return a + b;
	}

	static int call_clobber_inner2 () {
		return 3;
	}

	public static int test_7_call_clobber_dreg () {
		// A call doesn't clobber its dreg
		int a = 3;
		int b = 4;

		a = call_clobber_inner2 ();

		return a + b;
	}

	public static int test_9_spill_if_then_else () {
		// Spilling variables in one branch of an if-then-else
		int a = 4;
		int b = 5;

		if (a != b) {
		} else {
			call_clobber_inner ();
		}

		return a + b;
	}

	public static int test_3_spill_reload_if_then_else () {
		// Spilling and reloading variables in one branch of an if-then-else
		int a = 4;
		int b = 5;
		int c = 3;

		if (a != b) {
		} else {
			call_clobber_inner ();
			c = a + b;
		}

		return c;
	}

	public static int test_5_spill_loop () {
		int i;

		for (i = 0; i < 5; ++i)
			call_clobber_inner ();

		return i;
	}

	public unsafe static int test_0_volatile () {
		int i = 1;
		int*p = &i;

		if (*p != 1)
			return 1;
		if (i != 1)
			return 2;
		*p = 5;
		if (i != 5)
			return 3;
		i = 2;
		if (i != 2)
			return 4;
		if (*p != 2)
			return 5;

		return 0;
	}

	public unsafe static int test_0_volatile_unused () {
		int i = 1;
		int*p = &i;

		if (*p != 1)
			return 1;

		return 0;
	}

	public unsafe static int test_0_volatile_unused_2 () {
		int i = 1;
		int *p = &i;

		i = 2;
		if (i != 2)
			return 1;

		return 0;
	}

	static int ref_int (int i, ref int b, int j) {
		int res = b;
		b = 1;
		return res;
	}

	public static int test_0_volatile_unused_3 () {
		// b's def has no use so its interval is split at a position not covered by the interval
		int b = 42;
		if (ref_int (99, ref b, 100) != 42)
			return 1;
		b = 43;
		if (ref_int (99, ref b, 100) != 43)
			return 2;
		if (b != 1)
			return 13;
		return 0;
	}

	static int ref_bool (int i, ref bool b1, ref bool b2, ref bool b3) {
		b1 = !b1;
		b2 = !b2;
		b3 = !b3;

		return 0;
	}

	public static int test_0_volatile_regress_1 () {
		// Spill stores should precede spill loads at a given position
		for (int i = 0; i < 8; i++) {
			bool b1 = (i & 4) != 0;
			bool b2 = (i & 2) != 0;
			bool b3 = (i & 1) != 0;
			bool orig_b1 = b1, orig_b2 = b2, orig_b3 = b3;
			if (ref_bool(i, ref b1, ref b2, ref b3) != 0)
				return 4 * i + 1;
			if (b1 != !orig_b1)
				return 4 * i + 2;
			if (b2 != !orig_b2)
				return 4 * i + 3;
			if (b3 != !orig_b3)
				return 4 * i + 4;
		}

		return 0;
	}

	static int decode_len (out int pos) {
		pos = 19;
		return 10;
	}

	static void clobber_all (int pos) {
		for (int i = 0; i < 10; ++i)
			for (int j = 0; j < 10; ++j)
				for (int k = 0; k < 10; ++k)
					for (int l = 0; l < 10; ++l)
						;
	}

	public static int test_29_volatile_regress_2 () {
		int pos = 0;

		int len = decode_len (out pos);
		call_clobber_inner ();
		pos += len;

		clobber_all (pos);
		return pos;
	}

	public static int test_0_clobber_regress_1 () {
		object[] a11 = new object [10];
		object o = new Object ();
		// A spill load is inserted before the backward branch, clobbering one of the
		// registers used in the comparison
		for (int i = 0; i < 10; ++i)
			a11 [i] = null;

		return 0;
	}

	static int return_arg (int i) {
		return i;
	}

	public static int test_0_spill_regress_1 () {
		int j = 5;
		for (int i = 0; i < 3; i++) {
			// i is spilled by the call, then reloaded for the loop check
			// make sure the move from its first reg to its second is inserted in the
			// if body bblock, not the for body bblock
			if (i == 0) {
			} else {
				if (return_arg (j) != 5)
					return 1;
			}
		}

		return 0;
	}

	public static int test_0_spill_regress_2 () {
		double[] temporaries = new double[3];
		for (int i = 0; i < 3; i++) {
			// i and temporaries are spilled by the call, then reloaded after the call
			// make sure the two moves inserted in the if bblock are in the proper order
			if (i == 0) {
			} else {
				temporaries [i] = return_arg (i);
			}
		}

		return 0;
	}

	static int many_args_unused (int i, int j, int k, int l, int m, int n, int p, int q) {
		return 0;
	}

	public static int test_0_unused_args () {
		return many_args_unused (0, 1, 2, 3, 4, 5, 6, 7);
	}
			
	public unsafe void ClearBuffer (byte *buffer, int i) {
		// Avoid inlining
		byte *b = stackalloc byte [4];
	}

	public unsafe bool instance_method_1 (string s, string target, int start, int length, int opt) {
		byte* alwaysMatchFlags = stackalloc byte [16];
		byte* neverMatchFlags = stackalloc byte [16];
		byte* targetSortKey = stackalloc byte [4];
		byte* sk1 = stackalloc byte [4];
		byte* sk2 = stackalloc byte [4];
		ClearBuffer (alwaysMatchFlags, 16);
		ClearBuffer (neverMatchFlags, 16);
		ClearBuffer (targetSortKey, 4);
		ClearBuffer (sk1, 4);
		ClearBuffer (sk2, 4);

		return this == null && s == target && start == length && length == opt && alwaysMatchFlags == neverMatchFlags && neverMatchFlags == targetSortKey && sk1 == sk2;
	}

	public static int test_0_spill_regress_3 () {
		new Tests ().instance_method_1 (null, null, 0, 0, 0);
		return 0;
	}

	unsafe bool MatchesBackward (string s, ref int idx, int end, int orgStart, int ti, byte* sortkey, bool noLv4, ref object ctx) {
		// Avoid inlining
		byte *b = stackalloc byte [4];

		if (ctx == null)
			throw new Exception ();

		idx -= 1;
		return false;
	}

	unsafe int LastIndexOfSortKey (string s, int start, int orgStart, int length, byte* sortkey, int ti, bool noLv4, ref object ctx)
	{
		// ctx is initially allocated to the stack, when it is reloaded before the call,
		// %rax is spilled to free up the register, then ctx is allocated to %rax for its
		// whole lifetime, but %rax is not available for this since it is clobbered by the
		// call
		int end = start - length;
		int idx = start;
		while (idx > end) {
			int cur = idx;
			if (MatchesBackward (s, ref idx, end, orgStart,
								 ti, sortkey, noLv4, ref ctx))
				return cur;
		}
		return -1;
	}

	public unsafe static int test_0_spill_regress_4 () {
		object o = new Object ();
		new Tests ().LastIndexOfSortKey ("", 10, 0, 5, null, 0, false, ref o);

		return 0;
	}

	public static bool IsEqual (Type type, Type base_type) {
		return (type.GetHashCode () == base_type.GetHashCode ());
	}

	public static bool IsNestedFamilyAccessible (Type type, Type base_type)
	{
		do {
			if (IsEqual (type, base_type))
				return true;

			type = type.DeclaringType;
		} while (type != null);

		return false;
	}

	public static int test_0_do_while_critical_edges () {
		IsNestedFamilyAccessible (typeof (int), typeof (object));

		return 0;
	}

	public static string return_string (string s) {
		for (int i = 0; i < 1000; ++i)
			;
		return s;
	}

	public static int test_0_switch_critical_edges () {
		// A spill load is inserted at the end of the bblock containing the OP_BR_REG,
		// overwriting the source reg of the OP_BR_REG. The edge is not really 
		// a critical edge, since its source bblock only has 1 exit, but it must
		// be treated as such.
		for (int i=0; i < UInt16.MaxValue; i++) {
			Char c = Convert.ToChar (i);
			switch (i) {
			case 0x0009:
			case 0x000A:
			case 0x000B:
			case 0x2028:
			case 0x2029:
			case 0x202F:
			case 0x205F:
			case 0x3000:
				//Console.WriteLine ((i.ToString () + (int)c));
				return_string ((i.ToString () + (int)c));
				break;
			default:
				break;
			}
		}

		return 0;
	}

	static int balanceSegment(int start, int end) {
		int median = 1;

		if ((3 * median) <= (end - start + 1)) {
			median += median;
			median += (start - 1);
			return median;
		}
		return 99;
	}

	public static int test_2_spiller_bug_56194 () {
		return balanceSegment (1, 3);
	}

}
