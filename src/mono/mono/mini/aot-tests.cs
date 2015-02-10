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
	static int Main (String[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
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
		/* The 'd' argument is used to shift the register indexes so 't' doesn't start at the first reg */
		public static T Get_T (double d, T t) {
			return t;
		}
	}

	class Foo2<T> {
		public static T Get_T (double d, T t) {
			return t;
		}
	}

	class Foo3<T> {
		public static T Get_T (double d, T t) {
			return Foo2<T>.Get_T (d, t);
		}
	}

	[Category ("!AMD64")]
	static int test_0_arm64_dyncall_double () {
		double arg1 = 1.0f;
		double s = 2.0f;
		var res = (double)typeof (Foo<double>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	[Category ("!AMD64")]
	static int test_0_arm64_dyncall_float () {
		double arg1 = 1.0f;
		float s = 2.0f;
		var res = (float)typeof (Foo<float>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	[Category ("!AMD64")]
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

	[Category ("!AMD64")]
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

	[Category ("GSHAREDVT")]
	static int test_0_arm64_dyncall_gsharedvt_out_hfa_double () {
		/* gsharedvt out trampoline with double hfa argument */
		double arg1 = 1.0f;

		var s = new Struct1 ();
		s.a = 1.0f;
		s.b = 2.0f;
		// Call Foo2.Get_T directly, so its gets an instance
		Foo2<Struct1>.Get_T (arg1, s);
		Type t = typeof (Foo3<>).MakeGenericType (new Type [] { typeof (Struct1) });
		// Call Foo3.Get_T, this will call the gsharedvt instance, which will call the non-gsharedvt instance
		var s_res = (Struct1)t.GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (s_res.a != 1.0f || s_res.b != 2.0f)
			return 1;
		return 0;
	}

	[Category ("GSHAREDVT")]
	static int test_0_arm64_dyncall_gsharedvt_out_hfa_float () {
		/* gsharedvt out trampoline with double hfa argument */
		double arg1 = 1.0f;

		var s = new Struct2 ();
		s.a = 1.0f;
		s.b = 2.0f;
		// Call Foo2.Get_T directly, so its gets an instance
		Foo2<Struct2>.Get_T (arg1, s);
		Type t = typeof (Foo3<>).MakeGenericType (new Type [] { typeof (Struct2) });
		// Call Foo3.Get_T, this will call the gsharedvt instance, which will call the non-gsharedvt instance
		var s_res = (Struct2)t.GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (s_res.a != 1.0f || s_res.b != 2.0f)
			return 1;
		return 0;
	}

	interface IFaceFoo4<T> {
		T Get_T (double d, T t);
	}

	class Foo4<T> : IFaceFoo4<T> {
		public T Get_T (double d, T t) {
			return Foo2<T>.Get_T (d, t);
		}
	}

	struct VTypeByRefStruct {
		public long o1, o2, o3;
	}

	[Category ("GSHAREDVT")]
	public static int test_0_arm64_gsharedvt_out_vtypebyref () {
		/* gsharedvt out trampoline with vtypebyref argument */
		var s = new VTypeByRefStruct () { o1 = 1, o2 = 2, o3 = 3 };

		// Call Foo2.Get_T directly, so its gets an instance
		Foo2<VTypeByRefStruct>.Get_T (1.0f, s);
		var o = (IFaceFoo4<VTypeByRefStruct>)Activator.CreateInstance (typeof (Foo4<>).MakeGenericType (new Type [] { typeof (VTypeByRefStruct) }));
		// Call Foo4.Get_T, this will call the gsharedvt instance, which will call the non-gsharedvt instance
		var s_res = o.Get_T (1.0f, s);
		if (s_res.o1 != 1 || s_res.o2 != 2 || s_res.o3 != 3)
			return 1;
		return 0;
	}
}
