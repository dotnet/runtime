using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#if __MOBILE__
class GenericsTests
#else
class Tests
#endif
{
	struct TestStruct {
		public int i;
		public int j;

		public TestStruct (int i, int j) {
			this.i = i;
			this.j = j;
		}
	}

#if !__MOBILE__
	public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif

	public static int test_1_nullable_unbox ()
	{
		return Unbox<int?> (1).Value;
	}

	public static int test_1_nullable_unbox_null ()
	{
		return Unbox<int?> (null).HasValue ? 0 : 1;
	}

	public static int test_1_nullable_box ()
	{
		return (int) Box<int?> (1);
	}

	public static int test_1_nullable_box_null ()
	{
		return Box<int?> (null) == null ? 1 : 0;
	}

	public static int test_1_isinst_nullable ()
	{
		object o = 1;
		return (o is int?) ? 1 : 0;
	}

	public static int test_1_nullable_unbox_vtype ()
	{
		return Unbox<TestStruct?> (new TestStruct (1, 2)).Value.i;
	}

	public static int test_1_nullable_unbox_null_vtype ()
	{
		return Unbox<TestStruct?> (null).HasValue ? 0 : 1;
	}

	public static int test_1_nullable_box_vtype ()
	{
		return ((TestStruct)(Box<TestStruct?> (new TestStruct (1, 2)))).i;
	}

	public static int test_1_nullable_box_null_vtype ()
	{
		return Box<TestStruct?> (null) == null ? 1 : 0;
	}

	public static int test_1_isinst_nullable_vtype ()
	{
		object o = new TestStruct (1, 2);
		return (o is TestStruct?) ? 1 : 0;
	}

	public static int test_0_nullable_normal_unbox ()
	{
		int? i = 5;

		object o = i;
		// This uses unbox instead of unbox_any
		int? j = (int?)o;

		if (j != 5)
			return 1;

		return 0;
	}

	public static void stelem_any<T> (T[] arr, T elem) {
		arr [0] = elem;
	}

	public static T ldelem_any<T> (T[] arr) {
		return arr [0];
	}

	public static int test_1_ldelem_stelem_any_int () {
		int[] arr = new int [3];
		stelem_any (arr, 1);

		return ldelem_any (arr);
	}

	public static T return_ref<T> (ref T t) {
		return t;
	}

	public static T ldelema_any<T> (T[] arr) {
		return return_ref<T> (ref arr [0]);
	}

	public static int test_0_ldelema () {
		string[] arr = new string [1];

		arr [0] = "Hello";

		if (ldelema_any <string> (arr) == "Hello")
			return 0;
		else
			return 1;
	}

	public static T[,] newarr_multi<T> () {
		return new T [1, 1];
	}

	public static int test_0_newarr_multi_dim () {
		return newarr_multi<string> ().GetType () == typeof (string[,]) ? 0 : 1;
	}

	interface ITest
	{
		void Foo<T> ();
	}

	public static int test_0_iface_call_null_bug_77442 () {
		ITest test = null;

		try {
			test.Foo<int> ();
		}
		catch (NullReferenceException) {
			return 0;
		}
		
		return 1;
	}

	public static int test_18_ldobj_stobj_generics () {
		GenericClass<int> t = new GenericClass <int> ();
		int i = 5;
		int j = 6;
		return t.ldobj_stobj (ref i, ref j) + i + j;
	}

	public static int test_5_ldelem_stelem_generics () {
		GenericClass<TestStruct> t = new GenericClass<TestStruct> ();

		TestStruct s = new TestStruct (5, 5);
		return t.ldelem_stelem (s).i;
	}

	public static int test_0_constrained_vtype_box () {
		GenericClass<TestStruct> t = new GenericClass<TestStruct> ();

#if __MOBILE__
		return t.toString (new TestStruct ()) == "GenericsTests+TestStruct" ? 0 : 1;
#else
		return t.toString (new TestStruct ()) == "Tests+TestStruct" ? 0 : 1;
#endif
	}

	public static int test_0_constrained_vtype () {
		GenericClass<int> t = new GenericClass<int> ();

		return t.toString (1234) == "1234" ? 0 : 1;
	}

	public static int test_0_constrained_reftype () {
		GenericClass<String> t = new GenericClass<String> ();

		return t.toString ("1234") == "1234" ? 0 : 1;
	}

	public static int test_0_box_brtrue_optimizations () {
		if (IsNull<int>(5))
			return 1;

		if (!IsNull<object>(null))
			return 1;

		return 0;
	}

	[Category ("!FULLAOT")]
	public static int test_0_generic_get_value_optimization_int () {
		int[] x = new int[] {100, 200};

		if (GenericClass<int>.Z (x, 0) != 100)
			return 2;

		if (GenericClass<int>.Z (x, 1) != 200)
			return 3;

		return 0;
	}

	public static int test_0_nullable_ldflda () {
		return GenericClass<string>.BIsAClazz == false ? 0 : 1;
	}

	public struct GenericStruct<T> {
		public T t;

		public GenericStruct (T t) {
			this.t = t;
		}
	}

	public class GenericClass<T> {
		public T t;

		public GenericClass (T t) {
			this.t = t;
		}

		public GenericClass () {
		}

		public T ldobj_stobj (ref T t1, ref T t2) {
			t1 = t2;
			T t = t1;

			return t;
		}

		public T ldelem_stelem (T t) {
			T[] arr = new T [10];
			arr [0] = t;

			return arr [0];
		}

		public String toString (T t) {
			return t.ToString ();
		}

		public static IEnumerator<T> Y (IEnumerable <T> x)
		{
			return x.GetEnumerator ();
		}

		public static T Z (IList<T> x, int index)
		{
			return x [index];
		}

        protected static T NullB = default(T);       
        private static Nullable<bool>  _BIsA = null;
        public static bool BIsAClazz {
            get {
                _BIsA = false;
                return _BIsA.Value;
            }
        }
	}

	static bool IsNull<T> (T t)
	{
		if (t == null)
			return true;
		else
			return false;
	}

	static object Box<T> (T t)
	{
		return t;
	}

	static T Unbox <T> (object o) {
		return (T) o;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool constrained_equals<T> (T t1, T t2) {
		var c = EqualityComparer<T>.Default;

		return c.Equals (t1, t2);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int constrained_gethashcode<T> (T t) {
		return t.GetHashCode ();
	}

	enum AnEnum {
		A,
		B
	}

	public static int test_0_constrained_partial_sharing () {
		if (!constrained_equals<int> (1, 1))
			return 3;
		if (constrained_equals<int> (1, 2))
			return 4;
		if (!constrained_equals<AnEnum> (AnEnum.A, AnEnum.A))
			return 5;
		if (constrained_equals<AnEnum> (AnEnum.A, AnEnum.B))
			return 6;

		int i = constrained_gethashcode<int> (5);
		if (i != 5)
			return 7;
		i = constrained_gethashcode<AnEnum> (AnEnum.B);
		if (i != 1)
			return 8;
		return 0;
	}
}
