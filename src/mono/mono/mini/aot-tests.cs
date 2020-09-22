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
		public static T Get_T3 (double d, int i, T t) {
			return t;
		}
		public static T Get_T4 (int i, double d, T t)
		{
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
	static int test_0_amd64_dyncall_double_int_double ()
	{
		double arg1 = 1.0f;
		int arg2 = 1;
		double s = 2.0f;
		var res = (double)typeof (Foo2<double>).GetMethod ("Get_T3").Invoke (null, new object [] { arg1, arg2, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_amd64_dyncall_double_int_float ()
	{
		double arg1 = 1.0f;
		int arg2 = 1;
		float s = 2.0f;
		var res = (float)typeof (Foo2<float>).GetMethod ("Get_T3").Invoke (null, new object [] { arg1, arg2, s });
		if (res != 2.0f)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_amd64_dyncall_int_double_int ()
	{
		int arg1 = 1;
		double arg2 = 1.0f;
		int s = 2;
		var res = (int)typeof (Foo2<int>).GetMethod ("Get_T4").Invoke (null, new object [] { arg1, arg2, s });
		if (res != 2)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_amd64_dyncall_int_double_ref ()
	{
		int arg1 = 1;
		double arg2 = 1.0f;
		object s = new object ();
		var res = (object)typeof (Foo2<object>).GetMethod ("Get_T4").Invoke (null, new object [] { arg1, arg2, s });
		if (res != s)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
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
	static int test_0_amd64_dyncall_use_stack_float ()
	{
		float s = 10.0f;
		var res = (float)typeof (Foo2<float>).GetMethod ("Get_T2").Invoke (null, new object [] { 1.0f, 2, 3, 4, 5, 6, 7, 8, 9, s });
		if (res != s)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_amd64_dyncall_use_stack_double ()
	{
		double s = 10.0f;
		var res = (double)typeof (Foo2<double>).GetMethod ("Get_T2").Invoke (null, new object [] { 1.0f, 2, 3, 4, 5, 6, 7, 8, 9, s });
		if (res != s)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_amd64_dyncall_use_stack_int ()
	{
		int s = 10;
		var res = (int)typeof (Foo2<int>).GetMethod ("Get_T2").Invoke (null, new object [] { 1.0f, 2, 3, 4, 5, 6, 7, 8, 9, s });
		if (res != s)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_amd64_dyncall_use_stack_ref ()
	{
		object s = new object ();
		var res = (object)typeof (Foo2<object>).GetMethod ("Get_T2").Invoke (null, new object [] { 1.0f, 2, 3, 4, 5, 6, 7, 8, 9, s });
		if (res != s)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
	static int test_0_amd64_dyncall_use_stack_struct ()
	{
		Struct1 s = new Struct1 ();
		s.a = 10.0f;
		s.b = 11.0f;
		var res = (Struct1)typeof (Foo2<Struct1>).GetMethod ("Get_T2").Invoke (null, new object [] { 1.0f, 2, 3, 4, 5, 6, 7, 8, 9, s });
		if (res.a != s.a || res.b != s.b)
			return 1;
		return 0;
	}

	[Category ("DYNCALL")]
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

	[Category ("DYNCALL")]
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

		public static long vtype_by_val<T1, T2, T3, T4, T5> (T1 t1, T2 t2, T3 t3, T4 t4, long? t5) {
			return (long)t5;
		}
	}

	[Category ("DYNCALL")]
	[Category ("GSHAREDVT")]
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

	[Category ("DYNCALL")]
	[Category ("GSHAREDVT")]
	static int test_42_arm64_dyncall_vtypebyval () {
		var method = typeof (Foo5<string>).GetMethod ("vtype_by_val").MakeGenericMethod (new Type [] { typeof (int), typeof (long?), typeof (long?), typeof (long?), typeof (long?) });
		long res = (long)method.Invoke (null, new object [] { 1, 2L, 3L, 4L, 42L });
		return (int)res;
	}

	struct Struct7 {
		public string value;
	}

	class Foo7 {
		public static string vtypeonstack_align (string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, bool b, Struct7 s) {
			return s.value;
		}
	}

	[Category ("DYNCALL")]
	static int test_0_arm64_ios_dyncall_vtypeonstack_align () {
		var m = typeof (Foo7).GetMethod ("vtypeonstack_align");

		string s = (string)m.Invoke (null, new object [] { null, null, null, null, null, null, null, null, true, new Struct7 () { value = "ABC" } });
		return s == "ABC" ? 0 : 1;
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
		public static int NullableMany(long? i1, long? i2, long? i3, long? i4, long? i5, long? i6, long? i7, long? i8,
										long? i11, long? i12, long? i13, long? i14, long? i15, long? i16, long? i17, long? i18,
										long? i21, long? i22, long? i23, long? i24, long? i25, long? i26, long? i27, long? i28,
										long? i31, long? i32, long? i33, long? i34, long? i35, long? i36, long? i37, long? i38) {
			return (int)((i1 + i8 + i11 + i18 + i21 + i28 + i31 + i38).Value);
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static Nullable<T> GetNull<T>() where T : struct {
			return null;
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static bool GetHasValueManyArgs<T>(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, T? value) where T : struct
		{
			return value.HasValue;
		}
	}

	[Category ("DYNCALL")]
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

		NullableMethods.NullableMany (1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8);
		res2 = (int?)typeof (NullableMethods).GetMethod ("NullableMany").Invoke (null, new object [] { 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L });
		if (res2 != 36)
			return 6;
		return 0;
	}

	[Category ("DYNCALL")]
	public static int test_0_arm64_dyncall_vtypebyrefonstack () {
		var s = new LargeStruct () { a = 1, b = 2, c = 3, d = 4 };

		NullableMethods.GetHasValueManyArgs<LargeStruct> (1, 2, 3, 4, 5, 6, 7, 8, s);

		Type type = typeof (LargeStruct?).GetGenericArguments () [0];
		var m = typeof(NullableMethods).GetMethod("GetHasValueManyArgs", BindingFlags.Static | BindingFlags.Public);
		bool b1 = (bool)m.MakeGenericMethod (new Type[] {type}).Invoke (null, new object[] { 1, 2, 3, 4, 5, 6, 7, 8, s });
		if (!b1)
			return 1;
		bool b2 = (bool)m.MakeGenericMethod (new Type[] {type}).Invoke (null, new object[] { 1, 2, 3, 4, 5, 6, 7, 8, null });
		if (b2)
			return 2;
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
	public static int test_0_large_nullable_invoke () {
		var s = new LargeStruct () { a = 1, b = 2, c = 3, d = 4 };

		NullableMethods.GetHasValue<LargeStruct> (s);

		var m = typeof(NullableMethods).GetMethod("GetHasValue", BindingFlags.Static | BindingFlags.Public);

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
		} catch (Exception) {
		}
		return 0;
	}

	public static int test_0_regress_gh_7364 () {
		var map1 = new Dictionary <Type, IntPtr> (EqualityComparer<Type>.Default);
		var map2 = new Dictionary <IntPtr, WeakReference> (EqualityComparer<IntPtr>.Default);
		return 0;
	}

	public static int test_0_byte_equality_compater_devirt () {
		var dict = new Dictionary<byte, Struct1>();
		dict [1] = new Struct1 ();
		dict [1] = new Struct1 ();
		return 0;
	}

	// Requires c# 7.2
#if !__MonoCS__
	public interface GameComponent {
	}

	public struct Components<T> {
        public T[] Collection;
        public int Count;
    }

	struct AStruct : GameComponent {
	}

	public class ReadonlyTest<T> where T: GameComponent {
		private static Components<T> _components;

        public static T[] GetArray()
		{
			ref readonly Components<T> components = ref GetComponents();
			return components.Collection;
		}

		public static ref readonly Components<T> GetComponents()
		{
			return ref _components;
		}
	}

	// gh #8701
	public static int test_0_readonly_modopt () {
		typeof (ReadonlyTest<>).MakeGenericType (new Type[] { typeof (AStruct) }).GetMethod ("GetArray").Invoke (null, null);
		return 0;
	}
#endif

	struct DummyStruct {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void array_ienumerable<T1, T> (T t) where T: IEnumerable<T1> {
		var e = t.GetEnumerator ();
	}

	public static int test_0_array_ienumerable_constrained () {
		array_ienumerable<DummyStruct, DummyStruct[]> (new DummyStruct [0]);
		return 0;
	}
}
