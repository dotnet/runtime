using System;
using System.Collections.Generic;

class Tests {

	struct TestStruct {
		public int i;
		public int j;

		public TestStruct (int i, int j) {
			this.i = i;
			this.j = j;
		}
	}

	class Enumerator <T> : MyIEnumerator <T> {
		T MyIEnumerator<T>.Current {
			get {
				return default(T);
			}
		}

		bool MyIEnumerator<T>.MoveNext () {
			return true;
		}
	}

	class Comparer <T> : IComparer <T> {
		bool IComparer<T>.Compare (T x, T y) {
			return true;
		}
	}

	static int Main ()
	{
		return TestDriver.RunTests (typeof (Tests));
	}

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

		return t.toString (new TestStruct ()) == "Tests+TestStruct" ? 0 : 1;
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

	public static int test_0_generic_get_value_optimization_int () {
		int[] x = new int[] {100, 200};

		if (GenericClass<int>.Z (x, 0) != 100)
			return 2;

		if (GenericClass<int>.Z (x, 1) != 200)
			return 3;

		return 0;
	}

	public static int test_0_generic_get_value_optimization_vtype () {
		TestStruct[] arr = new TestStruct[] { new TestStruct (100, 200), new TestStruct (300, 400) };
		IEnumerator<TestStruct> enumerator = GenericClass<TestStruct>.Y (arr);
		TestStruct s;
		int sum = 0;
		while (enumerator.MoveNext ()) {
			s = enumerator.Current;
			sum += s.i + s.j;
		}

		if (sum != 1000)
			return 1;

		s = GenericClass<TestStruct>.Z (arr, 0);
		if (s.i != 100 || s.j != 200)
			return 2;

		s = GenericClass<TestStruct>.Z (arr, 1);
		if (s.i != 300 || s.j != 400)
			return 3;

		return 0;
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
	}

	public class MRO : MarshalByRefObject {
		public GenericStruct<int> struct_field;
		public GenericClass<int> class_field;
	}

	public static int test_0_ldfld_stfld_mro () {
		MRO m = new MRO ();
		GenericStruct<int> s = new GenericStruct<int> (5);
		// This generates stfld
		m.struct_field = s;

		// This generates ldflda
		if (m.struct_field.t != 5)
			return 1;

		// This generates ldfld
		GenericStruct<int> s2 = m.struct_field;
		if (s2.t != 5)
			return 2;

		if (m.struct_field.t != 5)
			return 3;

		m.class_field = new GenericClass<int> (5);
		if (m.class_field.t != 5)
			return 4;

		return 0;
	}

    public static int test_0_generic_virtual_call_on_vtype_unbox () {
		object o = new Object ();
        IMyHandler h = new Handler(o);

        if (h.Bar<object> () != o)
			return 1;
		else
			return 0;
    }

	public static int test_0_box_brtrue_opt () {
		Foo<int> f = new Foo<int> (5);

		f [123] = 5;

		return 0;
	}

	public static int test_0_box_brtrue_opt_regress_81102 () {
		if (new Foo<int>(5).ToString () == "null")
			return 0;
		else
			return 1;
	}

	struct S {
		public int i;
	}

	public static int test_0_ldloca_initobj_opt () {
		if (new Foo<S> (new S ()).get_default ().i != 0)
			return 1;
		if (new Foo<object> (null).get_default () != null)
			return 2;
		return 0;
	}

	public static int test_0_variance_reflection () {
		// covariance on IEnumerator
		if (!typeof (MyIEnumerator<object>).IsAssignableFrom (typeof (MyIEnumerator<string>)))
			return 1;
		// covariance on IEnumerator and covariance on arrays
		if (!typeof (MyIEnumerator<object>[]).IsAssignableFrom (typeof (MyIEnumerator<string>[])))
			return 2;
		// covariance and implemented interfaces
		if (!typeof (MyIEnumerator<object>).IsAssignableFrom (typeof (Enumerator<string>)))
			return 3;

		// contravariance on IComparer
		if (!typeof (IComparer<string>).IsAssignableFrom (typeof (IComparer<object>)))
			return 4;
		// contravariance on IComparer, contravariance on arrays
		if (!typeof (IComparer<string>[]).IsAssignableFrom (typeof (IComparer<object>[])))
			return 5;
		// contravariance and interface inheritance
		if (!typeof (IComparer<string>[]).IsAssignableFrom (typeof (IKeyComparer<object>[])))
			return 6;
		return 0;
	}

	public static int test_0_ldvirtftn_generic_method () {
		new Tests ().ldvirtftn<string> ();		

		return the_type == typeof (string) ? 0 : 1;
	}

	public static Type the_type;

	public void ldvirtftn<T> () {
		Foo <T> binding = new Foo <T> (default (T));

		binding.GenericEvent += event_handler;
		binding.fire ();
	}

	public virtual void event_handler<T> (Foo<T> sender) {
		the_type = typeof (T);
	}

	public class Foo<T1>
	{
		public Foo(T1 t1)
		{
			m_t1 = t1;
		}
		
		public override string ToString()
		{
			return Bar(m_t1 == null ? "null" : "null");
		}

		public String Bar (String s) {
			return s;
		}

		public int this [T1 key] {
			set {
				if (key == null)
					throw new ArgumentNullException ("key");
			}
		}

		public T1 get_default () {
			return default (T1);
		}
		
		readonly T1 m_t1;

		public delegate void GenericEventHandler (Foo<T1> sender);

		public event GenericEventHandler GenericEvent;

		public void fire () {
			GenericEvent (this);
		}

	}

	public interface IMyHandler {
		object Bar<T>();
	}

	struct Handler : IMyHandler {
		object o;

		public Handler(object o) {
			this.o = o;
		}

		public object Bar<T>() {
			return o;
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
}
