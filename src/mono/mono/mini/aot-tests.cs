using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

/*
 * Regression tests for the AOT/FULL-AOT code.
 */

#if MOBILE
class AotTests
#else
class Tests
#endif
{
#if !MOBILE
	static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}
#endif

	public delegate void ArrayDelegate (int[,] arr);

	static int test_0_array_delegate_full_aot () {
		ArrayDelegate d = delegate (int[,] arr) {
		};
		int[,] a = new int[5, 6];
		d.BeginInvoke (a, null, null);
		return 0;
	}

	struct Struct1 {
		public double a, b;
	}

	struct Struct2 {
		public float a, b;
	}

	class Foo<T> {
		/* The 'd' argument is used to shift the register indexes so 't' doesn't start at the first reg
		public static T Get_T (double d, T t) {
			return t;
		}
	}

	static int test_0_arm64_dyncall_double () {
		double arg1 = 1.0f;
		double s = 2.0f;
		var res = (double)typeof (Foo<double>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	static int test_0_arm64_dyncall_float () {
		double arg1 = 1.0f;
		float s = 2.0f;
		var res = (float)typeof (Foo<float>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	static int test_0_arm64_dyncall_hfa_double () {
		double arg1 = 1.0f;
		// HFA with double members
		var s = new Struct1 ();
		s.a = 1.0f;
		s.b = 2.0f;
		var s_res = (Struct1)typeof (Foo<Struct1>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (s_res.a != 1.0f || s_res.b != 2.0f)
			return 1;
		return 0;
	}

	static int test_0_arm64_dyncall_hfa_float () {
		double arg1 = 1.0f;
		var s = new Struct2 ();
		s.a = 1.0f;
		s.b = 2.0f;
		var s_res = (Struct2)typeof (Foo<Struct2>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (s_res.a != 1.0f || s_res.b != 2.0f)
			return 1;
		return 0;
	}
}
