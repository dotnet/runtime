using System;

class Tests {

	struct TestStruct {
		public int i;

		public TestStruct (int i) {
			this.i = i;
		}
	}

	class Enumerator <T> : IEnumerator <T> {
		T IEnumerator<T>.Current {
			get {
				return default(T);
			}
		}

		bool IEnumerator<T>.MoveNext () {
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
		return Unbox<TestStruct?> (new TestStruct (1)).Value.i;
	}

	public static int test_1_nullable_unbox_null_vtype ()
	{
		return Unbox<TestStruct?> (null).HasValue ? 0 : 1;
	}

	public static int test_1_nullable_box_vtype ()
	{
		return ((TestStruct)(Box<TestStruct?> (new TestStruct (1)))).i;
	}

	public static int test_1_nullable_box_null_vtype ()
	{
		return Box<TestStruct?> (null) == null ? 1 : 0;
	}

	public static int test_1_isinst_nullable_vtype ()
	{
		object o = new TestStruct (1);
		return (o is TestStruct?) ? 1 : 0;
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

	public static int test_0_variance_reflection () {
		// covariance on IEnumerator
		if (!typeof (IEnumerator<object>).IsAssignableFrom (typeof (IEnumerator<string>)))
			return 1;
		// covariance on IEnumerator and covariance on arrays
		if (!typeof (IEnumerator<object>[]).IsAssignableFrom (typeof (IEnumerator<string>[])))
			return 2;
		// covariance and implemented interfaces
		if (!typeof (IEnumerator<object>).IsAssignableFrom (typeof (Enumerator<string>)))
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
		
		readonly T1 m_t1;
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

	static object Box<T> (T t)
	{
		return t;
	}
	
	static T Unbox <T> (object o) {
		return (T) o;
	}
}
