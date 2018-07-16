	static bool slow_or_reflection = true; // FIXME test both
	//static bool slow_or_reflection = false; // FIXME test both

	t [] array1 = new t [10]{
		newt (1), newt (2), newt (3), newt (4), newt (5),
		newt (6), newt (7), newt (8), newt (9), newt (10)
	};

	t [,] array2 = new t [10, 3] {
		{newt (10), newt (20), newt (30)},
		{newt (100), newt (200), newt (300)},
		{newt (1000), newt (2000), newt (3000)},
		{newt (10000), newt (20000), newt (30000)},
		{newt (100000), newt (200000), newt (300000)},
		{newt (11), newt (21), newt (31)},
		{newt (101), newt (201), newt (301)},
		{newt (1001), newt (2001), newt (3001)},
		{newt (10001), newt (20001), newt (30001)},
		{newt (100001), newt (200001), newt (300001)}
	};

	t [][] array3 = new t [10][] {
		new t [1]{newt (2)}, new t [1]{newt (3)}, new t [1]{newt (4)}, new t [1]{newt (5)}, new t [1]{newt (6)},
		new t [1]{newt (7)}, new t [1]{newt (8)}, new t [1]{newt (9)}, new t [1]{newt (10)}, new t [1]{newt (11)}
	};

	static void assert (bool expr)
	{
		if (expr)
			return;
		System.Console.WriteLine ("failure");
		Environment.Exit (1);
	}

	void test_clear ()
	{
		int [] a = new int [100]; // This not System.Array.Clear.

		t [] array1 = new t [10] {
			newt (1), newt (2), newt (3), newt (4), newt (5),
			newt (6), newt (7), newt (8), newt (9), newt (10)
		};
		var dt0 = newt (0);

		assert (array1 [0] != dt0);
		assert (array1 [1] != dt0);
		assert (array1 [2] != dt0);
		assert (array1 [3] != dt0);
		System.Array.Clear (array1, 0, 2);
		assert (array1 [0] == dt0);
		assert (array1 [1] == dt0);
		assert (array1 [2] != dt0);
		assert (array1 [3] != dt0);
		System.Array.Clear (array1, 3, 1);
		assert (array1 [0] == dt0);
		assert (array1 [1] == dt0);
		assert (array1 [2] != dt0);
		assert (array1 [3] == dt0);

		t [][] array3 = new t [10][] {
			new t [1]{newt (2)}, new t [1]{newt (3)}, new t [1]{newt (4)}, new t [1]{newt (5)}, new t [1]{newt (6)},
			new t [1]{newt (7)}, new t [1]{newt (8)}, new t [1]{newt (9)}, new t [1]{newt (10)}, new t [1]{newt (11)}
		};

		assert (array3 [0] != null);
		assert (array3 [1] != null);
		assert (array3 [2] != null);
		assert (array3 [3] != null);
		System.Array.Clear (array3, 1, 2);
		assert (array3 [0] != null);
		assert (array3 [1] == null);
		assert (array3 [2] == null);
		assert (array3 [3] != null);
	}

	void test_get_value ()
	{
		// Fast tests, should not print.
		assert (array1 [0] != array1 [1]);
		assert (array1 [2] != array1 [3]);

		if (!slow_or_reflection)
			return;

		// While these are not reflecton, this is still presumed
		// ok to be slow.
		assert ((t)array1.GetValue (0) == array1 [0]);
		assert ((t)array1.GetValue (3) == array1 [3]);

		Type type = typeof (System.Array);
		MethodInfo mi = type.GetMethod ("GetValue", new Type [] { typeof(int) } );
		assert (mi != null);
		assert ((t)mi.Invoke (array1, new object [ ] { 0 }) == array1 [0]);
		assert ((t)mi.Invoke (array1, new object [ ] { 3 }) == array1 [3]);
	}

	void test_get_rank ()
	{
		// Fast tests, should not print.
		assert (array1.Rank != array2.Rank);
		assert (array2.Rank != array3.Rank);

		if (!slow_or_reflection)
			return;
		Type type = typeof (System.Array);
		PropertyInfo pi = type.GetProperty ("Rank");
		assert ((int)pi.GetValue (array1) == array1.Rank);
		assert ((int)pi.GetValue (array2) == array2.Rank);
		assert ((int)pi.GetValue (array3) == array3.Rank);
		assert ((int)pi.GetValue (array3 [0]) == array3 [0].Rank);
	}

	void test_get_length ()
	{
		// Fast tests, should not print.
		assert (array1.Length != array2.Length);
		assert (array2.Length != array3.Length);

		if (!slow_or_reflection)
			return;
		Type type = typeof (System.Array);
		PropertyInfo pi = type.GetProperty ("Length");
		assert ((int)pi.GetValue (array1) == array1.Length);
		assert ((int)pi.GetValue (array2) == array2.Length);
		assert ((int)pi.GetValue (array3) == array3.Length);
	}

	void test_get_longlength ()
	{
		// Fast tests, should not print.
		assert (array1.LongLength != array2.LongLength);
		assert (array2.LongLength != array3.LongLength);

		if (!slow_or_reflection)
			return;
		Type type = typeof (System.Array);
		PropertyInfo pi = type.GetProperty ("LongLength");
		assert ((long)pi.GetValue (array1) == array1.LongLength);
		assert ((long)pi.GetValue (array2) == array2.LongLength);
		assert ((long)pi.GetValue (array3) == array3.LongLength);
	}

	void test_get_lower_bound_and_get_value_with_bounds ()
	{
		// This test also goes overboard in testing slow paths.

		Console.WriteLine ("test_get_lower_bound_and_get_value_with_bounds1");

		var lengths      = new int [ ] {1, 2, 3, 4};
		var lower_bounds = new int [ ] {3, 2, 1, 0};
		var a = (t[,,,])System.Array.CreateInstance (typeof (t), lengths, lower_bounds);

		Console.WriteLine ("test_get_lower_bound_and_get_value_with_bounds1.1");
		a [3, 2, 1, 0] = newt (-1);
		Console.WriteLine ("test_get_lower_bound_and_get_value_with_bounds1.2");
		a [3, 3, 2, 1] = newt (-2);
		Console.WriteLine ("test_get_lower_bound_and_get_value_with_bounds1.3");

		MethodInfo mi = null;
		if (slow_or_reflection) {
			Type type = typeof (System.Array);
			mi = type.GetMethod ("GetLowerBound", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			assert (mi != null);
		}

		Console.WriteLine ("test_get_lower_bound_and_get_value_with_bounds2");

		for (int b = 0; b < 3; ++b)
		{
			assert (a.GetLowerBound (b) == lower_bounds [b]);
			if (slow_or_reflection)
				assert ((int)mi.Invoke (a, new object [ ] { b }) == lower_bounds [b]);
		}

		var a2 = new t [1];
		assert (a2    .GetLowerBound (0) == 0);
		assert (array1.GetLowerBound (0) == 0);
		assert (array2.GetLowerBound (0) == 0);
		assert (array3.GetLowerBound (0) == 0);

		Console.WriteLine ("test_get_lower_bound_and_get_value_with_bounds3");

		if (slow_or_reflection) {
			assert ((int)mi.Invoke (a2,     new object [ ] { 0 }) == 0);
			assert ((int)mi.Invoke (array1, new object [ ] { 0 }) == 0);
			assert ((int)mi.Invoke (array2, new object [ ] { 0 }) == 0);
			assert ((int)mi.Invoke (array3, new object [ ] { 0 }) == 0);
		}
		assert ((t)a.GetValue (new int [ ] {3, 2, 1, 0}) == newt (-1));
		assert ((t)a.GetValue (new int [ ] {3, 3, 2, 1}) == newt (-2));
	}

	void test_get_generic_value ()
	{
		return; // fails for FullAOT, or even with AOT runtime, but not with full JIT that CI does not run

		if (!slow_or_reflection)
			return;

		Type type = typeof (System.Array);
		MethodInfo mig = type.GetMethod ("GetGenericValueImpl", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		assert (mig != null);
	        MethodInfo mi = mig.MakeGenericMethod (typeof (t));
		assert (mi != null);

		var args = new object [2];
		for (int i = 0; i < array1.Length; ++i) {
			args [0] = i;
			args [1] = null;
			mi.Invoke (array1, args);
			assert (array1 [i] == (t)args [1]);
		}
	}

	void test_set_generic_value ()
	{
		if (!slow_or_reflection)
			return;

		t [] array2 = new t [10];

		for (int i = 0; i < array1.Length; ++i)
			assert (array1 [i] != array2 [i]);

		Type type = typeof (System.Array);
		MethodInfo mig = type.GetMethod ("SetGenericValueImpl", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		assert (mig != null);
	        MethodInfo mi = mig.MakeGenericMethod (typeof (t));
		assert (mi != null);

		var args = new object [2];
		for (int i = 0; i < array1.Length; ++i) {
			args [0] = i;
			args [1] = newt (i + 1);
			mi.Invoke (array2, args);
		}

		for (int i = 0; i < array1.Length; ++i)
			assert (array1 [i] == array2 [i]);
	}

	void main ()
	{
		Console.WriteLine ("test_set_generic_value");
		try {
			test_set_generic_value ();
		}
		catch (System.Reflection.TargetInvocationException) // for FullAOT
		{
			Console.WriteLine ("test_set_generic_value raise exception");
		}
		Console.WriteLine ("test_get_generic_value");
		test_get_generic_value ();
		Console.WriteLine ("test_clear");
		test_clear ();
		Console.WriteLine ("test_get_value");
		test_get_value ();
		Console.WriteLine ("test_get_rank");
		test_get_rank ();
		Console.WriteLine ("test_get_length");
		test_get_length ();
		Console.WriteLine ("test_get_longlength");
		test_get_longlength ();
		Console.WriteLine ("test_get_lower_bound_and_get_value_with_bounds");
		test_get_lower_bound_and_get_value_with_bounds ();
	}

	public static void Main (string[] args)
	{
		new test ().main ();
	}
}
