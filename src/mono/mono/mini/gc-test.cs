using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Threading;

/*
 * Regression tests for the GC support in the JIT
 */
#if __MOBILE__
class GcTests
#else
class Tests
#endif
{
#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif

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

	/*
	 * Test liveness and loops.
	 */
	public static int test_0_liveness_2 () {
		object o = new object ();
		for (int n = 0; n < 10; ++n) {
			/* Exhaust all registers so 'o' is stack allocated */
			int sum = 0, i, j, k, l, m;
			for (i = 0; i < 100; ++i)
				sum ++;
			for (j = 0; j < 100; ++j)
				sum ++;
			for (k = 0; k < 100; ++k)
				sum ++;
			for (l = 0; l < 100; ++l)
				sum ++;
			for (m = 0; m < 100; ++m)
				sum ++;

			if (o != null)
				o.ToString ();

			GC.Collect (1);

			if (o != null)
				o.ToString ();

			sum += i + j + k;

			GC.Collect (1);
		}

		return 0;
	}

	/*
	 * Test liveness and stack slot sharing
	 * This doesn't work yet, its hard to make the JIT share the stack slots of the
	 * two 'o' variables.
	 */
	public static int test_0_liveness_3 () {
		bool b = false;
		bool b2 = true;

		/* Exhaust all registers so 'o' is stack allocated */
		int sum = 0, i, j, k, l, m, n, s;
		for (i = 0; i < 100; ++i)
			sum ++;
		for (j = 0; j < 100; ++j)
			sum ++;
		for (k = 0; k < 100; ++k)
			sum ++;
		for (l = 0; l < 100; ++l)
			sum ++;
		for (m = 0; m < 100; ++m)
			sum ++;
		for (n = 0; n < 100; ++n)
			sum ++;
		for (s = 0; s < 100; ++s)
			sum ++;

		if (b) {
			object o = new object ();

			/* Make sure o is global */
			if (b2)
				Console.WriteLine ();

			o.ToString ();
		}

		GC.Collect (1);

		if (b) {
			object o = new object ();

			/* Make sure o is global */
			if (b2)
				Console.WriteLine ();

			o.ToString ();
		}

		sum += i + j + k + l + m + n + s;

		return 0;
	}

	/*
	 * Test liveness of variables used to handle items on the IL stack.
	 */
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static string call1 () {
		return "A";
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static string call2 () {
		GC.Collect (1);
		return "A";
	}

	public static int test_0_liveness_4 () {
		bool b = false;
		bool b2 = true;

		/* Exhaust all registers so 'o' is stack allocated */
		int sum = 0, i, j, k, l, m, n, s;
		for (i = 0; i < 100; ++i)
			sum ++;
		for (j = 0; j < 100; ++j)
			sum ++;
		for (k = 0; k < 100; ++k)
			sum ++;
		for (l = 0; l < 100; ++l)
			sum ++;
		for (m = 0; m < 100; ++m)
			sum ++;
		for (n = 0; n < 100; ++n)
			sum ++;
		for (s = 0; s < 100; ++s)
			sum ++;

		string o = b ? call1 () : call2 ();

		GC.Collect (1);

		sum += i + j + k + l + m + n + s;

		return 0;
	}


	/*
	 * Test liveness of volatile variables
	 */
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void liveness_5_1 (out object o) {
		o = new object ();
	}

	public static int test_0_liveness_5 () {
		bool b = false;
		bool b2 = true;

		/* Exhaust all registers so 'o' is stack allocated */
		int sum = 0, i, j, k, l, m, n, s;
		for (i = 0; i < 100; ++i)
			sum ++;
		for (j = 0; j < 100; ++j)
			sum ++;
		for (k = 0; k < 100; ++k)
			sum ++;
		for (l = 0; l < 100; ++l)
			sum ++;
		for (m = 0; m < 100; ++m)
			sum ++;
		for (n = 0; n < 100; ++n)
			sum ++;
		for (s = 0; s < 100; ++s)
			sum ++;

		object o;

		liveness_5_1 (out o);

		for (int x = 0; x < 10; ++x) {

			o.ToString ();

			GC.Collect (1);
		}

		sum += i + j + k + l + m + n + s;

		return 0;
	}

	/*
	 * Test the case when a stack slot becomes dead, then live again due to a backward
	 * branch.
	 */

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static object alloc_obj () {
		return new object ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static bool return_true () {
		return true;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static bool return_false () {
		return false;
	}

	public static int test_0_liveness_6 () {
		bool b = false;
		bool b2 = true;

		/* Exhaust all registers so 'o' is stack allocated */
		int sum = 0, i, j, k, l, m, n, s;
		for (i = 0; i < 100; ++i)
			sum ++;
		for (j = 0; j < 100; ++j)
			sum ++;
		for (k = 0; k < 100; ++k)
			sum ++;
		for (l = 0; l < 100; ++l)
			sum ++;
		for (m = 0; m < 100; ++m)
			sum ++;
		for (n = 0; n < 100; ++n)
			sum ++;
		for (s = 0; s < 100; ++s)
			sum ++;

		for (int x = 0; x < 10; ++x) {

			GC.Collect (1);

			object o = alloc_obj ();

			o.ToString ();

			GC.Collect (1);
		}

		sum += i + j + k + l + m + n + s;

		return 0;
	}

	public static int test_0_multi_dim_ref_array_wbarrier () {
        string [,] arr = new string [256, 256];
        for (int i = 0; i < 256; ++i) {
            for (int j = 0; j < 100; ++j)
                arr [i, j] = "" + i + " " + j;
		}
		GC.Collect ();

		return 0;
	}

	/*
	 * Liveness + out of line bblocks
	 */
	public static int test_0_liveness_7 () {
		/* Exhaust all registers so 'o' is stack allocated */
		int sum = 0, i, j, k, l, m, n, s;
		for (i = 0; i < 100; ++i)
			sum ++;
		for (j = 0; j < 100; ++j)
			sum ++;
		for (k = 0; k < 100; ++k)
			sum ++;
		for (l = 0; l < 100; ++l)
			sum ++;
		for (m = 0; m < 100; ++m)
			sum ++;
		for (n = 0; n < 100; ++n)
			sum ++;
		for (s = 0; s < 100; ++s)
			sum ++;

		// o is dead here
		GC.Collect (1);

		if (return_false ()) {
			// This bblock is in-line
			object o = alloc_obj ();
			// o is live here
			if (return_false ()) {
				// This bblock is out-of-line, and o is live here
				throw new Exception (o.ToString ());
			}
		}

		// o is dead here too
		GC.Collect (1);

		return 0;
	}

	// Liveness + finally clauses
	public static int test_0_liveness_8 () {
		/* Exhaust all registers so 'o' is stack allocated */
		int sum = 0, i, j, k, l, m, n, s;
		for (i = 0; i < 100; ++i)
			sum ++;
		for (j = 0; j < 100; ++j)
			sum ++;
		for (k = 0; k < 100; ++k)
			sum ++;
		for (l = 0; l < 100; ++l)
			sum ++;
		for (m = 0; m < 100; ++m)
			sum ++;
		for (n = 0; n < 100; ++n)
			sum ++;
		for (s = 0; s < 100; ++s)
			sum ++;

		object o = null;
		try {
			o = alloc_obj ();
		} finally {
			GC.Collect (1);
		}

		o.GetHashCode ();
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static object alloc_string () {
		return "A";
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static object alloc_obj_and_gc () {
		GC.Collect (1);
		return new object ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void clobber_regs_and_gc () {
		int sum = 0, i, j, k, l, m, n, s;
		for (i = 0; i < 100; ++i)
			sum ++;
		for (j = 0; j < 100; ++j)
			sum ++;
		for (k = 0; k < 100; ++k)
			sum ++;
		for (l = 0; l < 100; ++l)
			sum ++;
		for (m = 0; m < 100; ++m)
			sum ++;
		for (n = 0; n < 100; ++n)
			sum ++;
		for (s = 0; s < 100; ++s)
			sum ++;
		GC.Collect (1);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void liveness_9_call1 (object o1, object o2, object o3) {
		o1.GetHashCode ();
		o2.GetHashCode ();
		o3.GetHashCode ();
	}

	// Liveness + JIT temporaries
	public static int test_0_liveness_9 () {
		// the result of alloc_obj () goes into a vreg, which gets converted to a
		// JIT temporary because of the branching introduced by the cast
		// FIXME: This doesn't crash if MONO_TYPE_I is not treated as a GC ref
		liveness_9_call1 (alloc_obj (), (string)alloc_string (), alloc_obj_and_gc ());
		return 0;
	}

	// Liveness for registers
	public static int test_0_liveness_10 () {
		// Make sure this goes into a register
		object o = alloc_obj ();
		o.GetHashCode ();
		o.GetHashCode ();
		o.GetHashCode ();
		o.GetHashCode ();
		o.GetHashCode ();
		o.GetHashCode ();
		// Break the bblock so o doesn't become a local vreg
		if (return_true ())
			// Clobber it with a call and run a GC
			clobber_regs_and_gc ();
		// Access it again
		o.GetHashCode ();
		return 0;
	}

	class ObjWithShiftOp {
		public static ObjWithShiftOp operator >> (ObjWithShiftOp bi1, int shiftVal) {
			clobber_regs_and_gc ();
			return bi1;
		}
	}

	// Liveness for spill slots holding managed pointers
	public static int test_0_liveness_11 () {
		ObjWithShiftOp[] arr = new ObjWithShiftOp [10];
		// This uses an ldelema internally
		// FIXME: This doesn't crash if mp-s are not correctly tracked, just writes to
		// an old object.
		arr [0] >>= 1;

		return 0;
	}


	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void liveness_12_inner (int a, int b, int c, int d, int e, int f, object o, ref string s) {
		GC.Collect (1);
		o.GetHashCode ();
		s.GetHashCode ();
	}

	class FooClass {
		public string s;
	}

	// Liveness for param area
	public static int test_0_liveness_12 () {
		var f = new FooClass () { s = "A" };
		// The ref argument should be passed on the stack
		liveness_12_inner (1, 2, 3, 4, 5, 6, new object (), ref f.s);
		return 0;
	}
	
	interface IFace {
		int foo ();
	}

	struct BarStruct : IFace {
		int i;

		public int foo () {
			GC.Collect ();
			return i;
		}
	}
		
	public static int test_0_liveness_unbox_trampoline () {
		var s = new BarStruct ();
		
		IFace iface = s;
		iface.foo ();
		return 0;
	}

	public static void liveness_13_inner (ref ArrayList arr) {
		// The value of arr will be stored in a spill slot
		arr.Add (alloc_obj_and_gc ());
	}

	// Liveness for byref arguments in spill slots
	public static int test_0_liveness_13 () {
		var arr = new ArrayList ();
		liveness_13_inner (ref arr);
		return 0;
	}

	static ThreadLocal<object> tls;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void alloc_tls_obj () {
		tls = new ThreadLocal<object> ();
		tls.Value = new object ();
	}

	public static int test_0_thread_local () {
		alloc_tls_obj ();
		GC.Collect ();
		Type t = tls.Value.GetType ();
		if (t == typeof (object))
			return 0;
		else
			return 1;
	}

	struct LargeBitmap {
		public object o1, o2, o3;
		public int i;
		public object o4, o5, o6, o7, o9, o10, o11, o12, o13, o14, o15, o16, o17, o18, o19, o20, o21, o22, o23, o24, o25, o26, o27, o28, o29, o30, o31, o32;
	}

	public static int test_12_large_bitmap () {
		LargeBitmap lb = new LargeBitmap ();
		lb.o1 = 1;
		lb.o2 = 2;
		lb.o3 = 3;
		lb.o32 = 6;

		GC.Collect (0);

		return (int)lb.o1 + (int)lb.o2 + (int)lb.o3 + (int)lb.o32;
	}
}
