using System;

class Tests {

	struct TestStruct {
		public int i;

		public TestStruct (int i) {
			this.i = i;
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
