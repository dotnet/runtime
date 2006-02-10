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

	public static int test_1_nullable_unbox_reftype ()
	{
		return Unbox<String?> ("ABC").Value == "ABC" ? 1 : 0;
	}

	public static int test_1_nullable_unbox_null_reftype ()
	{
		return Unbox<String?> (null).HasValue ? 0 : 1;
	}

	public static int test_1_nullable_box_reftype ()
	{
		return ((String)(Box<String?> ("ABC"))) == "ABC" ? 1 : 0;
	}

	public static int test_1_nullable_box_null_reftype ()
	{
		return Box<String?> (null) == null ? 1 : 0;
	}

	public static int test_1_isinst_nullable_reftype ()
	{
		object o = "ABC";
		return (o is String?) ? 1 : 0;
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

	static object Box<T> (T t)
	{
		return t;
	}
	
	static T Unbox <T> (object o) {
		return (T) o;
	}
}
