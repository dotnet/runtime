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

	public static int test_36_simple () {
		// Overflow the registers
		object o1 = (1);
		object o2 = (2);
		object o3 = (3);
		object o4 = (4);
		object o5 = (5);
		object o6 = (6);
		object o7 = (7);
		object o8 = (8);

		/* Prevent the variables from being local to a bb */
		bool b = o1 != null;
		GC.Collect (0);

		if (b)
			return (int)o1 + (int)o2 + (int)o3 + (int)o4 + (int)o5 + (int)o6 + (int)o7 + (int)o8;
		else
			return 0;
	}

	public static int test_36_liveness () {
		object o = 5;
		object o1, o2, o3, o4, o5, o6, o7, o8;

		bool b = o != null;

		GC.Collect (1);

		o1 = (1);
		o2 = (2);
		o3 = (3);
		o4 = (4);
		o5 = (5);
		o6 = (6);
		o7 = (7);
		o8 = (8);

		if (b)
			return (int)o1 + (int)o2 + (int)o3 + (int)o4 + (int)o5 + (int)o6 + (int)o7 + (int)o8;
		else
			return 0;
	}

	struct FooStruct {
		public object o1;
		public int i;
		public object o2;

		public FooStruct (int i1, int i, int i2) {
			this.o1 = i1;
			this.i = i;
			this.o2 = i2;
		}
	}

	public static int test_4_vtype () {
		FooStruct s = new FooStruct (1, 2, 3);

		GC.Collect (1);

		return (int)s.o1 + (int)s.o2;
	}

	class BigClass {
		public object o1, o2, o3, o4, o5, o6, o7, o8, o9, o10;
		public object o11, o12, o13, o14, o15, o16, o17, o18, o19, o20;
		public object o21, o22, o23, o24, o25, o26, o27, o28, o29, o30;
		public object o31, o32;
	}

	static void set_fields (BigClass b) {
		b.o31 = 31;
		b.o32 = 32;

		b.o1 = 1;
		b.o2 = 2;
		b.o3 = 3;
		b.o4 = 4;
		b.o5 = 5;
		b.o6 = 6;
		b.o7 = 7;
		b.o8 = 8;
		b.o9 = 9;
		b.o10 = 10;
		b.o11 = 11;
		b.o12 = 12;
		b.o13 = 13;
		b.o14 = 14;
		b.o15 = 15;
		b.o16 = 16;
		b.o17 = 17;
		b.o18 = 18;
		b.o19 = 19;
		b.o20 = 20;
		b.o21 = 21;
		b.o22 = 22;
		b.o23 = 23;
		b.o24 = 24;
		b.o25 = 25;
		b.o26 = 26;
		b.o27 = 27;
		b.o28 = 28;
		b.o29 = 29;
		b.o30 = 30;
	}

	// Test marking of objects with > 32 fields
	public static int test_528_mark_runlength_large () {
		BigClass b = new BigClass ();

		/* 
		 * Do the initialization in a separate method so no object refs remain in
		 * spill slots.
		 */
		set_fields (b);

		GC.Collect (1);

		return 
			(int)b.o1 + (int)b.o2 + (int)b.o3 + (int)b.o4 + (int)b.o5 +
			(int)b.o6 + (int)b.o7 + (int)b.o8 + (int)b.o9 + (int)b.o10 +
			(int)b.o11 + (int)b.o12 + (int)b.o13 + (int)b.o14 + (int)b.o15 +
			(int)b.o16 + (int)b.o17 + (int)b.o18 + (int)b.o19 + (int)b.o20 +
			(int)b.o21 + (int)b.o22 + (int)b.o23 + (int)b.o24 + (int)b.o25 +
			(int)b.o26 + (int)b.o27 + (int)b.o28 + (int)b.o29 + (int)b.o30 +
			(int)b.o31 + (int)b.o32;
	}
}