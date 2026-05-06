using System;
using System.Runtime.InteropServices;

class Tests {
	delegate void SimpleDelegate ();

	static void F1 () {
		v += 1;
		Console.WriteLine ("Test.F1");
	}
	static void F2 () {
		v += 2;
		Console.WriteLine ("Test.F2");
	}
	static void F4 () {
		v += 4;
		Console.WriteLine ("Test.F4");
	}
	static void F8 () {
		v += 8;
		Console.WriteLine ("Test.F8");
	}

	public static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	static int v = 0;
	static bool check_is_expected_v (SimpleDelegate d, int expected_v)
	{
		v = 0;
		d ();
		return v == expected_v;
	}

	static public int test_0_test () {
		SimpleDelegate d1 = new SimpleDelegate (F1);
		SimpleDelegate d2 = new SimpleDelegate (F2);
		SimpleDelegate d4 = new SimpleDelegate (F4);
		SimpleDelegate d8 = new SimpleDelegate (F8);

		if (d1 - d1 != null)
			return 1;
		if (!check_is_expected_v (d1 - d2, 1))
			return 2;
		if (!check_is_expected_v (d1 - d4, 1))
			return 3;

		if (!check_is_expected_v (d2 - d1, 2))
			return 4;
		if (d2 - d2 != null)
			return 5;
		if (!check_is_expected_v (d2 - d4, 2))
			return 6;

		if (!check_is_expected_v (d4 - d1, 4))
			return 7;
		if (!check_is_expected_v (d4 - d2, 4))
			return 8;
		if (d4 - d4 != null)
			return 9;

		SimpleDelegate d12 = d1 + d2;
		SimpleDelegate d14 = d1 + d4;
		SimpleDelegate d24 = d2 + d4;

		if (!check_is_expected_v (d12 - d1, 2))
			return 11;
		if (!check_is_expected_v (d12 - d2, 1))
			return 12;
		if (!check_is_expected_v (d12 - d4, 3))
			return 13;

		if (!check_is_expected_v (d14 - d1, 4))
			return 14;
		if (!check_is_expected_v (d14 - d2, 5))
			return 15;
		if (!check_is_expected_v (d14 - d4, 1))
			return 16;

		if (!check_is_expected_v (d14 - d1, 4))
			return 17;
		if (!check_is_expected_v (d14 - d2, 5))
			return 18;
		if (!check_is_expected_v (d14 - d4, 1))
			return 19;

		if (d12 - d12 != null)
			return 21;
		if (!check_is_expected_v (d12 - d14, 3))
			return 22;
		if (!check_is_expected_v (d12 - d24, 3))
			return 23;

		if (!check_is_expected_v (d14 - d12, 5))
			return 24;
		if (d14 - d14 != null)
			return 25;
		if (!check_is_expected_v (d14 - d24, 5))
			return 26;

		if (!check_is_expected_v (d24 - d12, 6))
			return 27;
		if (!check_is_expected_v (d24 - d14, 6))
			return 28;
		if (d24 - d24 != null)
			return 29;

		SimpleDelegate d124 = d1 + d2 + d4;

		if (!check_is_expected_v (d124 - d1, 6))
			return 31;
		if (!check_is_expected_v (d124 - d2, 5))
			return 32;
		if (!check_is_expected_v (d124 - d4, 3))
			return 33;

		if (!check_is_expected_v (d124 - d12, 4))
			return 34;
		if (!check_is_expected_v (d124 - d14, 7))
			return 35;
		if (!check_is_expected_v (d124 - d24, 1))
			return 36;

		if (d124 - d124 != null)
			return 37;

		SimpleDelegate d1248 = d1 + d2 + d4 + d8;

		if (!check_is_expected_v (d1248 - (d1 + d2), 12))
			return 41;
		if (!check_is_expected_v (d1248 - (d1 + d4), 15))
			return 42;
		if (!check_is_expected_v (d1248 - (d1 + d8), 15))
			return 43;
		if (!check_is_expected_v (d1248 - (d2 + d4), 9))
			return 44;
		if (!check_is_expected_v (d1248 - (d2 + d8), 15))
			return 45;
		if (!check_is_expected_v (d1248 - (d4 + d8), 3))
			return 46;

		if (!check_is_expected_v (d1248 - (d1 + d2 + d4), 8))
			return 51;
		if (!check_is_expected_v (d1248 - (d1 + d2 + d8), 15))
			return 52;
		if (!check_is_expected_v (d1248 - (d1 + d4 + d8), 15))
			return 53;
		if (!check_is_expected_v (d1248 - (d2 + d4 + d8), 1))
			return 54;
		if (!check_is_expected_v (d1248 - (d2 + d4 + d8), 1))
			return 54;

		if (d1248 - d1248 != null)
			return 55;

		return 0;
	}

	// Regression test for bug #50366
	static public int test_0_delegate_equality () {
		if (new SimpleDelegate (F1) == new SimpleDelegate (F1))
			return 0;
		else
			return 1;
	}
}
