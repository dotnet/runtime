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

struct Simple {
	public int a;
	public byte b;
	public short c;
	public long d;
}

class Sample {
	public int a;
	public Sample (int v) {
		a = v;
	}
}

enum SampleEnum {
	A,
	B,
	C
}

class Tests {

	static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}
	
	static int test_0_return () {
		Simple s;
		s.a = 1;
		s.b = 2;
		s.c = (short)(s.a + s.b);
		s.d = 4;
		return s.a - 1;
	}

	static int test_0_string_access () {
		string s = "Hello";
		if (s [1] != 'e')
			return 1;
		return 0;
	}

	static int test_0_string_virtual_call () {
		string s = "Hello";
		string s2 = s.ToString ();
		if (s2 [1] != 'e')
			return 1;
		return 0;
	}

	static int test_0_iface_call () {
		string s = "Hello";
		object o = ((ICloneable)s).Clone ();
		return 0;
	}

	static int test_5_newobj () {
		Sample s = new Sample (5);
		return s.a;
	}

	static int test_4_box () {
		object obj = 4;
		return (int)obj;
	}

	static int test_0_enum_unbox () {
		SampleEnum x = SampleEnum.A;
		object o = x;
		
		int res = 1;

		res = (int)o;
		
		return res;
	}
	
	static Simple get_simple (int v) {
		Simple r = new Simple ();
		r.a = v;
		r.b = (byte)(v + 1);
		r.c = (short)(v + 2);
		r.d = v + 3;

		return r;
	}

	static int test_3_return_struct () {
		Simple v = get_simple (1);

		if (v.a != 1)
			return 0;
		if (v.b != 2)
			return 0;
		if (v.c != 3)
			return 0;
		if (v.d != 4)
			return 0;
		return 3;
	}

	public virtual Simple v_get_simple (int v)
	{
		return get_simple (v);
	}
	
	static int test_2_return_struct_virtual () {
		Tests t = new Tests ();
		Simple v = t.v_get_simple (2);

		if (v.a != 2)
			return 0;
		if (v.b != 3)
			return 0;
		if (v.c != 4)
			return 0;
		if (v.d != 5)
			return 0;
		return 2;
	}

	static int receive_simple (int a, Simple v, int b) {
		if (v.a != 1)
			return 0;
		if (v.b != 2)
			return 0;
		if (v.c != 3)
			return 0;
		if (v.d != 4)
			return 0;
		if (a != 7)
			return 0;
		if (b != 9)
			return 0;
		return 1;
	}
	
	static int test_5_pass_struct () {
		Simple v = get_simple (1);
		if (receive_simple (7, v, 9) != 1)
			return 0;
		if (receive_simple (7, get_simple (1), 9) != 1)
			return 1;
		return 5;
	}

	class TestRegA {

		long buf_start;
		int buf_length, buf_offset;
	
		public long Seek (long position) {
			long pos = position;
			/* interaction between the register allocator and
			 * allocating arguments to registers */
			if (pos >= buf_start && pos <= buf_start + buf_length) {
				buf_offset = (int) (pos - buf_start);
				return pos;
			}
			return buf_start;
		}

	}

	static int test_0_seektest () {
		TestRegA t = new TestRegA ();
		return (int)t.Seek (0);
	}

	class Super : ICloneable {
		public virtual object Clone () {
			return null;
		}
	}
	class Duper: Super {
	}
	
	static int test_0_super_cast () {
		Duper d = new Duper ();
		Super sup = d;
		Object o = d;

		if (!(o is Super))
			return 1;
		try {
			d = (Duper)sup;
		} catch {
			return 2;
		}
		if (!(d is Object))
			return 3;
		try {
			d = (Duper)(object)sup;
		} catch {
			return 4;
		}
		return 0;
	}

	static int test_0_super_cast_array () {
		Duper[] d = new Duper [0];
		Super[] sup = d;
		Object[] o = d;

		if (!(o is Super[]))
			return 1;
		try {
			d = (Duper[])sup;
		} catch {
			return 2;
		}
		if (!(d is Object[]))
			return 3;
		try {
			d = (Duper[])(object[])sup;
		} catch {
			return 4;
		}
		return 0;
	}

	static int test_0_enum_array_cast () {
		TypeCode[] tc = new TypeCode [0];
		object[] oa;
		ValueType[] vta;
		int[] inta;
		Array a = tc;
		bool ok;

		if (a is object[])
			return 1;
		if (a is ValueType[])
			return 2;
		if (a is Enum[])
			return 3;
		try {
			ok = false;
			oa = (object[])a;
		} catch {
			ok = true;
		}
		if (!ok)
			return 4;
		try {
			ok = false;
			vta = (ValueType[])a;
		} catch {
			ok = true;
		}
		if (!ok)
			return 5;
		try {
			ok = true;
			inta = (int[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 6;
		return 0;
	}

	static int test_0_more_cast_corner_cases () {
		ValueType[] vta = new ValueType [0];
		Enum[] ea = new Enum [0];
		Array a = vta;
		object[] oa;
		bool ok;

		if (!(a is object[]))
			return 1;
		if (!(a is ValueType[]))
			return 2;
		if (a is Enum[])
			return 3;
		a = ea;
		if (!(a is object[]))
			return 4;
		if (!(a is ValueType[]))
			return 5;
		if (!(a is Enum[]))
			return 6;

		try {
			ok = true;
			oa = (object[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 7;
	
		try {
			ok = true;
			oa = (Enum[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 8;
	
		try {
			ok = true;
			oa = (ValueType[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 9;

		a = vta;
		try {
			ok = true;
			oa = (object[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 10;
	
		try {
			ok = true;
			oa = (ValueType[])a;
		} catch {
			ok = false;
		}
		if (!ok)
			return 11;
	
		try {
			ok = false;
			vta = (Enum[])a;
		} catch {
			ok = true;
		}
		if (!ok)
			return 12;
		return 0;
	}

	static int test_0_cast_iface_array () {
		object o = new ICloneable [0];
		object o2 = new Duper [0];
		object t;
		bool ok;

		if (!(o is object[]))
			return 1;
		if (!(o2 is ICloneable[]))
			return 2;

		try {
			ok = true;
			t = (object[])o;
		} catch {
			ok = false;
		}
		if (!ok)
			return 3;
	
		try {
			ok = true;
			t = (ICloneable[])o2;
		} catch {
			ok = false;
		}
		if (!ok)
			return 4;

		try {
			ok = true;
			t = (ICloneable[])o;
		} catch {
			ok = false;
		}
		if (!ok)
			return 5;

		/* add tests for interfaces that 'inherit' interfaces */
		return 0;
	}

	private static int[] daysmonthleap = { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

	private static int AbsoluteDays (int year, int month, int day)
	{
		int temp = 0, m = 1;
		int[] days = daysmonthleap;
		while (m < month)
			temp += days[m++];
		return ((day-1) + temp + (365* (year-1)) + ((year-1)/4) - ((year-1)/100) + ((year-1)/400));
	}

	static int test_719162_complex_div () {
		int adays = AbsoluteDays (1970, 1, 1);
		return adays;
	}

	static int test_1_store_decimal () {
		decimal[,] a = {{1}};

		if (a[0,0] != 1m)
			return 0;
		return 1;
	}

	static int test_2_intptr_stobj () {
		System.IntPtr [] arr = { new System.IntPtr () };

		if (arr [0] != (System.IntPtr)0)
			return 1;
		return 2;
	}
}

