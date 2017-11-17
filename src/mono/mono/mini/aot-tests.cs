using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/*
 * Regression tests for the AOT/FULL-AOT code.
 */

#if __MOBILE__
class AotTests
#else
class Tests
#endif
{
#if !__MOBILE__
	static int Main (String[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif

	public delegate void ArrayDelegate (int[,] arr);

	[Category ("!WASM")] //Requires a working threadpool
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
		public static T Get_T2 (double d, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, T t) {
			return t;
		}
	}

	class Foo3<T> {
		public static T Get_T (double d, T t) {
			return Foo2<T>.Get_T (d, t);
		}
	}

	[Category ("DYNCALL")]
	static int test_0_arm64_dyncall_double () {
		double arg1 = 1.0f;
		double s = 2.0f;
		var res = (double)typeof (Foo<double>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_arm64_dyncall_float () {
		double arg1 = 1.0f;
		float s = 2.0f;
		var res = (float)typeof (Foo<float>).GetMethod ("Get_T").Invoke (null, new object [] { arg1, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	[Category ("!FULLAOT-AMD64")]
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

	[Category ("DYNCALL")]
	[Category ("!FULLAOT-AMD64")]
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

	[Category ("DYNCALL")]
	[Category ("GSHAREDVT")]
	[Category ("!FULLAOT-AMD64")]
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

	[Category ("DYNCALL")]
	[Category ("GSHAREDVT")]
	[Category ("!FULLAOT-AMD64")]
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
		T Get_T2 (double d, T t);
	}

	class Foo4<T> : IFaceFoo4<T> {
		public T Get_T (double d, T t) {
			return Foo2<T>.Get_T (d, t);
		}
		public T Get_T2 (double d, T t) {
			return Foo2<T>.Get_T2 (d, 1, 2, 3, 4, 5, 6, 7, 8, t);
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
		// Same with the byref argument passed on the stack
		s_res = o.Get_T2 (1.0f, s);
		if (s_res.o1 != 1 || s_res.o2 != 2 || s_res.o3 != 3)
			return 2;
		return 0;
	}

	class Foo5<T> {
		public static T Get_T (object o) {
			return (T)o;
		}
	}

	[Category ("DYNCALL")]
	[Category ("GSHAREDVT")]
	[Category ("!FULLAOT-AMD64")]
	static int test_0_arm64_dyncall_vtypebyref_ret () {
		var s = new VTypeByRefStruct () { o1 = 1, o2 = 2, o3 = 3 };
		Type t = typeof (Foo5<>).MakeGenericType (new Type [] { typeof (VTypeByRefStruct) });
		var o = Activator.CreateInstance (t);
		try {
			var s_res = (VTypeByRefStruct)t.GetMethod ("Get_T").Invoke (o, new object [] { s });
			if (s_res.o1 != 1 || s_res.o2 != 2 || s_res.o3 != 3)
				return 1;
		} catch (TargetInvocationException) {
			return 2;
		}
		return 0;
	}

	class Foo6 {
		public T reg_stack_split_inner<T> (int i, int j, T l) {
			return l;
		}
	}

	[Category ("DYNCALL")]
	[Category ("GSHAREDVT")]
	static int test_0_arm_dyncall_reg_stack_split () {
		var m = typeof (Foo6).GetMethod ("reg_stack_split_inner").MakeGenericMethod (new Type[] { typeof (long) });
		var o = new Foo6 ();
		if ((long)m.Invoke (o, new object [] { 1, 2, 3 }) != 3)
			return 1;
		if ((long)m.Invoke (o, new object [] { 1, 2, Int64.MaxValue }) != Int64.MaxValue)
			return 2;
		return 0;
	}

	static int test_0_partial_sharing_regress_30204 () {
		var t = typeof (System.Collections.Generic.Comparer<System.Collections.Generic.KeyValuePair<string, string>>);
		var d = new SortedDictionary<string, string> ();
		d.Add ("key1", "banana");
		return d ["key1"] == "banana" ? 0 : 1;
	}

	class NullableMethods {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static bool GetHasValue<T>(Nullable<T> value) where T : struct {
			return value.HasValue;
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static T GetValue<T>(Nullable<T> value) where T : struct {
			return value.Value;
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static Nullable<T> Get<T>(T t) where T : struct {
			return t;
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static Nullable<T> GetNull<T>() where T : struct {
			return null;
		}
	}

	[Category ("DYNCALL")]
	[Category ("!FULLAOT-AMD64")]
	public static int test_0_dyncall_nullable () {
		int? v;

		v = 42;
		NullableMethods.GetHasValue (v);
		bool b = (bool)typeof (NullableMethods).GetMethod ("GetHasValue").MakeGenericMethod (new Type [] { typeof (int) }).Invoke (null, new object [] { v });
		if (!b)
			return 1;
		v = null;
		b = (bool)typeof (NullableMethods).GetMethod ("GetHasValue").MakeGenericMethod (new Type [] { typeof (int) }).Invoke (null, new object [] { v });
		if (b)
			return 2;

		v = 42;
		NullableMethods.GetValue (v);
		var res = (int)typeof (NullableMethods).GetMethod ("GetValue").MakeGenericMethod (new Type [] { typeof (int) }).Invoke (null, new object [] { v });
		if (res != 42)
			return 3;

		NullableMethods.Get (42);
		var res2 = (int?)typeof (NullableMethods).GetMethod ("Get").MakeGenericMethod (new Type [] { typeof (int) }).Invoke (null, new object [] { 42 });
		if (res2 != 42)
			return 4;
		res2 = (int?)typeof (NullableMethods).GetMethod ("GetNull").MakeGenericMethod (new Type [] { typeof (int) }).Invoke (null, new object [] { });
		if (res2.HasValue)
			return 5;
		return 0;
	}

	enum AnEnum {
		A = 0,
		B = 1
	}

	public static int test_0_enum_eq_comparer () {
		var c = EqualityComparer<AnEnum>.Default;
		return (!c.Equals (AnEnum.A, AnEnum.B) && c.Equals (AnEnum.A, AnEnum.A)) ? 0 : 1;
	}

	public static int test_0_enum_comparer () {
		var c = Comparer<AnEnum>.Default;
		return c.Compare (AnEnum.A, AnEnum.A);
	}

	private static Dictionary<long, TValue> ConvertDictionary<TValue>(Dictionary<long, IList<TValue>> source) {
		return source.ToDictionary(pair => pair.Key, pair => pair.Value[0]);
	}

	[Category ("GSHAREDVT")]
	public static int test_0_gsharedvt_non_variable_arg () {
		Dictionary<long, IList<int>> data = new Dictionary<long, IList<int>>
            {
				{123L, new List<int> {2}}
            };
		Dictionary<long, int> newDict = ConvertDictionary(data);
		if (newDict.Count != 1)
			return 1;
		return 0;
	}

	enum LongEnum : ulong {
		A = 1
			}

	public static int test_0_long_enum_eq_comparer () {
		var c = EqualityComparer<LongEnum>.Default;
		c.GetHashCode (LongEnum.A);
		return 0;
	}

	enum UInt32Enum : uint {
		A = 1
			}

	enum Int32Enum : int {
		A = 1
			}

	enum Int16Enum : short {
		A = 1
			}

	enum UInt16Enum : ushort {
		A = 1
			}

	enum Int8Enum : sbyte {
		A = 1
			}

	enum UInt8Enum : byte {
		A = 1
			}

	public static int test_0_int_enum_eq_comparer () {
		var t1 = new Dictionary<Int32Enum, object> ();
		t1 [Int32Enum.A] = "foo";

		var t2 = new Dictionary<UInt32Enum, object> ();
		t2 [UInt32Enum.A] = "foo";

		var t3 = new Dictionary<UInt16Enum, object> ();
		t3 [UInt16Enum.A] = "foo";

		var t4 = new Dictionary<Int16Enum, object> ();
		t4 [Int16Enum.A] = "foo";

		var t5 = new Dictionary<Int8Enum, object> ();
		t5 [Int8Enum.A] = "foo";

		var t6 = new Dictionary<UInt8Enum, object> ();
		t6 [UInt8Enum.A] = "foo";

		return 0;
	}

	[Category ("DYNCALL")]
	public static int test_0_array_accessor_runtime_invoke_ref () {
		var t = typeof (string[]);
		var arr = Array.CreateInstance (typeof (string), 1);
		arr.GetType ().GetMethod ("Set").Invoke (arr, new object [] { 0, "A" });
		var res = (string)arr.GetType ().GetMethod ("Get").Invoke (arr, new object [] { 0 });
		if (res != "A")
			return 1;
		return 0;
	}

	public static void SetArrayValue_<T> (T[] values) {
		values.Select (x => x).ToArray ();
	}

	[Category ("GSHAREDVT")]
	public static int test_0_delegate_invoke_wrappers_gsharedvt () {
		var enums = new LongEnum [] { LongEnum.A };
		SetArrayValue_ (enums);
		return 0;
	}

	struct LargeStruct {
		public int a, b, c, d;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool GetHasValue<T>(T? value) where T : struct
	{
		return value.HasValue;
	}

	[Category ("DYNCALL")]
	[Category ("!FULLAOT-AMD64")]
	[Category ("!WASM")] //Interp fails	
	public static int test_0_large_nullable_invoke () {
		var s = new LargeStruct () { a = 1, b = 2, c = 3, d = 4 };

		GetHasValue<LargeStruct> (s);

#if __MOBILE__
		var m = typeof(AotTests).GetMethod("GetHasValue", BindingFlags.Static | BindingFlags.Public);
#else
		var m = typeof(Tests).GetMethod("GetHasValue", BindingFlags.Static | BindingFlags.Public);
#endif

		Type type = typeof (LargeStruct?).GetGenericArguments () [0];
		bool b1 = (bool)m.MakeGenericMethod (new Type[] {type}).Invoke (null, new object[] { s });
		if (!b1)
			return 1;
		bool b2 = (bool)m.MakeGenericMethod (new Type[] {type}).Invoke (null, new object[] { null });
		if (b2)
			return 2;
		return 0;
	}

	struct FpStruct {
		public float a, b, c;
	}

	struct LargeStruct2 {
		public FpStruct x;
		public int a, b, c, d, e, f, g, h;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int pass_hfa_on_stack (FpStruct s1, FpStruct s2, FpStruct s3) {
		return (int)s3.c;
	}

	public static int test_10_arm64_hfa_on_stack_llvm () {
		var arr = new LargeStruct2 [10, 10];
		for (int i = 0; i < 10; ++i)
			for (int j = 0; j < 10; ++j)
				arr [i, j].x = new FpStruct ();

		var s1 = new FpStruct () { a = 1, b = 1, c = 10 };
		return pass_hfa_on_stack (s1, s1, s1);
	}

	public static int test_0_get_current_method () {
		var m = MethodBase.GetCurrentMethod ();
#if __MOBILE__
		var m2 = typeof (AotTests).GetMethod ("test_0_get_current_method");
#else
		var m2 = typeof (Tests).GetMethod ("test_0_get_current_method");
#endif
		return m == m2 ? 0 : 1;
	}

	class GetCurrentMethodClass<T> {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public MethodBase get_current () {
			return MethodBase.GetCurrentMethod ();
		}
	}

	public static int test_0_get_current_method_generic () {
		var c = new GetCurrentMethodClass<string> ();
		var m = c.get_current ();
		var m2 = typeof (GetCurrentMethodClass<>).GetMethod ("get_current");
		return m == m2 ? 0 : 1;
	}

	public static int test_0_array_wrappers_runtime_invoke () {
		string[][] arr = new string [10][];
		IEnumerable<string[]> iface = arr;
		var m = typeof(IEnumerable<string[]>).GetMethod ("GetEnumerator");
		m.Invoke (arr, null);
		return 0;
	}

	public static int test_0_fault_clauses () {
		object [] data = { 1, 2, 3 };
		int [] expected = { 1, 2, 3 };

		try {
			Action d = delegate () { data.Cast<IEnumerable> ().GetEnumerator ().MoveNext (); };
			d ();
		} catch (Exception ex) {
		}
		return 0;
	}
}
