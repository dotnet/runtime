using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

struct Foo {
	public int i, j, k, l, m, n;
}

struct GFoo<T> {
	public T dummy;
	public T t;
	public int i;
	public Foo f;
	public static T static_dummy;
	public static T static_t;
	public static Foo static_f;
}

struct GFoo2<T> {
	public T t, t2, t3;
}

class GFoo3<T> {
	public T t, t2;

	public GFoo3 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public GFoo3 (T i1, T i2) {
		t = i1;
		t2 = i2;
	}
}

//
// Tests for generic sharing of vtypes.
// The tests use arrays to pass/receive values to keep the calling convention of the methods stable, which is a current limitation of the runtime support for gsharedvt.
//

//
// Interfaces are used to prevent the AOT compiler from discovering instantiations, thus forcing the usage of the gsharedvt
// versions of methods. Unused vtype type arguments are used to test gsharedvt methods with ref type arguments, i.e.
// when calling foo<T,T2> as foo<object,bool>, the gsharedvt version is used, but with a ref type argument.
//

// FIXME: Add mixed ref/noref tests, i.e. Dictionary<string, int>

#if __MOBILE__
public class GSharedTests
#else
public class Tests
#endif
{
#if !__MOBILE__
	public static int Main (String[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
#endif

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void gshared<T> (T [] array, int i, int j) {
		T tmp = array [i];
		array [i] = array [j];
		array [j] = tmp;
	}

	// Test that the gshared and gsharedvt versions don't mix
	public static int test_0_vt_gshared () {
		string[] sarr = new string [2] { "A", "B" };

		gshared<string> (sarr, 0, 1);

		Foo[] arr = new Foo [2];
		arr [0] = new Foo () { i = 1, j = 2 };
		arr [1] = new Foo () { i = 3, j = 4 };

		gshared<Foo> (arr, 0, 1);
		if (arr [0].i != 3 || arr [0].j != 4)
			return 1;
		if (arr [1].i != 1 || arr [1].j != 2)
			return 2;

		return 0;
	}

	static void ldelem_stelem<T> (T [] array, int i, int j) {
		T tmp = array [i];
		array [i] = array [j];
		array [j] = tmp;
	}

	public static int test_0_vt_ldelem_stelem () {
		Foo[] arr = new Foo [2];
		arr [0] = new Foo () { i = 1, j = 2 };
		arr [1] = new Foo () { i = 3, j = 4 };

		ldelem_stelem<Foo> (arr, 0, 1);
		if (arr [0].i != 3 || arr [0].j != 4)
			return 1;
		if (arr [1].i != 1 || arr [1].j != 2)
			return 2;

		int[] arr2 = new int [2] { 1, 2 };
		ldelem_stelem<int> (arr2, 0, 1);
		if (arr2 [0] !=2 || arr2 [1] != 1)
			return 3;

		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	private static void initobj<T> (T [] array, int i, int j) {
		T x = default(T);
		array [i] = x;
	}

	public static int test_0_vt_initobj () {
		Foo[] arr = new Foo [2];
		arr [0] = new Foo () { i = 1, j = 2 };
		arr [1] = new Foo () { i = 3, j = 4 };

		initobj<Foo> (arr, 0, 1);
		if (arr [0].i != 0 || arr [0].j != 0)
			return 1;
		if (arr [1].i != 3 || arr [1].j != 4)
			return 2;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T ldobj_stobj<T> (ref T t1, ref T t2) {
		t1 = t2;
		T t = t2;
		t2 = default(T);
		return t;
	}

	public static int test_0_vt_ldobj_stobj () {
		int i = 5;
		int j = 6;
 		if (ldobj_stobj (ref i, ref j) != 6)
			return 1;
		if (i != 6 || j != 0)
			return 2;
		double d1 = 1.0;
		double d2 = 2.0;
 		if (ldobj_stobj (ref d1, ref d2) != 2.0)
			return 3;
		if (d1 != 2.0 || d2 != 0.0)
			return 4;		
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	private static void box<T1, T> (T [] array, object[] arr) {
		object x = array [0];
		arr [0] = x;
	}

	public static int test_0_vt_box () {
		Foo[] arr = new Foo [2];
		arr [0] = new Foo () { i = 1, j = 2 };

		object[] arr2 = new object [16];
		box<int, Foo> (arr, arr2);
		if (arr2 [0].GetType () != typeof (Foo))
			return 1;
		Foo f = (Foo)arr2 [0];
		if (f.i != 1 || f.j != 2)
			return 2;
		string[] arr3 = new string [16];
		object[] arr4 = new object [16];
		arr3 [0] = "OK";
		box<int, string> (arr3, arr4);
		if (arr4 [0] != (object)arr3 [0])
			return 3;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	private static void unbox_any<T> (T [] array, object[] arr) {
		T t = (T)arr [0];
		array [0] = t;
	}

	public static int test_0_vt_unbox_any () {
		int[] iarr = new int [16];
		unbox_any<int> (iarr, new object [] { 12 });

		Foo[] arr = new Foo [2];

		object[] arr2 = new object [16];
		arr2 [0] = new Foo () { i = 1, j = 2 };
		unbox_any<Foo> (arr, arr2);
		if (arr [0].i != 1 || arr [0].j != 2)
			return 2;
		return 0;
	}

	interface IFaceUnbox {
		T Unbox<T, T2> (T t, T2 t2, object o);
	}

	class ClassUnbox : IFaceUnbox {
		public T Unbox<T, T2> (T t, T2 t2, object o) {
			return (T)o;
		}
	}

	// unbox.any on a ref type in a gsharedvt method
	public static int test_0_ref_gsharedvt_aot_unbox_any () {
		IFaceUnbox iface = new ClassUnbox ();
		string s = iface.Unbox<string, int> ("A", 2, "A");
		if (s != "A")
			return 1;
		return 0;
	}

	public static int test_0_unbox_any_enum () {
		IFaceUnbox iface = new ClassUnbox ();
		AnEnum res = iface.Unbox<AnEnum, int> (AnEnum.One, 0, 1);
		if (res != AnEnum.Two)
			return 1;
		res = iface.Unbox<AnEnum, int> (AnEnum.One, 0, AnEnum.Two);
		if (res != AnEnum.Two)
			return 2;
		int res2 = iface.Unbox<int, AnEnum> (0, AnEnum.One, AnEnum.Two);
		if (res2 != 1)
			return 3;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void ldfld_nongeneric<T> (GFoo<T>[] foo, int[] arr) {
		arr [0] = foo [0].i;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void ldfld<T> (GFoo<T>[] foo, T[] arr) {
		arr [0] = foo [0].t;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void stfld_nongeneric<T> (GFoo<T>[] foo, int[] arr) {
		foo [0].i = arr [0];
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void stfld<T> (GFoo<T>[] foo, T[] arr) {
		foo [0].t = arr [0];
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void ldflda<T> (GFoo<T>[] foo, int[] arr) {
		arr [0] = foo [0].f.i;
	}

	public static int test_0_vt_ldfld_stfld () {
		var foo = new GFoo<Foo> () { t = new Foo () { i = 1, j = 2 }, i = 5, f = new Foo () { i = 5, j = 6 } };
		var farr = new GFoo<Foo>[] { foo };

		/* Normal fields with a variable offset */
		var iarr = new int [10];
		ldfld_nongeneric<Foo> (farr, iarr);
		if (iarr [0] != 5)
			return 1;
		iarr [0] = 16;
		stfld_nongeneric<Foo> (farr, iarr);
		if (farr [0].i != 16)
			return 2;

		/* Variable type field with a variable offset */
		var arr = new Foo [10];
		ldfld<Foo> (farr, arr);
		if (arr [0].i != 1 || arr [0].j != 2)
			return 3;
		arr [0] = new Foo () { i = 3, j = 4 };
		stfld<Foo> (farr, arr);
		if (farr [0].t.i != 3 || farr [0].t.j != 4)
			return 4;

		ldflda<Foo> (farr, iarr);
		if (iarr [0] != 5)
			return 5;

		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void stsfld<T> (T[] arr) {
		GFoo<T>.static_t = arr [0];
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void ldsfld<T> (T[] arr) {
		arr [0] = GFoo<T>.static_t;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void ldsflda<T> (int[] iarr) {
		iarr [0] = GFoo<T>.static_f.i;
	}
	
	public static int test_0_stsfld () {
		Foo[] farr = new Foo [] { new Foo () { i = 1, j = 2 } };
		stsfld<Foo> (farr);

		if (GFoo<Foo>.static_t.i != 1 || GFoo<Foo>.static_t.j != 2)
			return 1;

		Foo[] farr2 = new Foo [1];
		ldsfld<Foo> (farr2);
		if (farr2 [0].i != 1 || farr2 [0].j != 2)
			return 2;

		var iarr = new int [10];
		GFoo<Foo>.static_f = new Foo () { i = 5, j = 6 };
		ldsflda<Foo> (iarr);
		if (iarr [0] != 5)
			return 3;

		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static object newarr<T> () {
		object o = new T[10];
		return o;
	}

	public static int test_0_vt_newarr () {
		object o = newarr<Foo> ();
		if (!(o is Foo[]))
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static Type ldtoken<T> () {
		return typeof (GFoo<T>);
	}

	public static int test_0_vt_ldtoken () {
		Type t = ldtoken<Foo> ();
		if (t != typeof (GFoo<Foo>))
			return 1;
		t = ldtoken<int> ();
		if (t != typeof (GFoo<int>))
			return 2;

		return 0;
	}

	public static int test_0_vtype_list () {
		List<int> l = new List<int> ();

		l.Add (5);
		if (l.Count != 1)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int args_simple<T> (T t, int i) {
		return i;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int args_simple<T> (T t, int i, T t2) {
		return i;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static Type args_rgctx<T> (T t, int i) {
		return typeof (T);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static Type eh_in<T> (T t, int i) {
		throw new OverflowException ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T return_t<T> (T t) {
		return t;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	T return_this_t<T> (T t) {
		return t;
	}

	interface IFaceGSharedVtIn {
		T return_t<T> (T t);
	}

	class ClassGSharedVtIn : IFaceGSharedVtIn {
		public T return_t<T> (T t) {
			return t;
		}
	}

	public static int test_0_gsharedvt_in () {
		// Check that the non-generic argument is passed at the correct stack position
		int r = args_simple<bool> (true, 42);
		if (r != 42)
			return 1;
		r = args_simple<Foo> (new Foo (), 43);
		if (r != 43)
			return 2;
		// Check that the proper rgctx is passed to the method
		Type t = args_rgctx<int> (5, 42);
		if (t != typeof (int))
			return 3;
		var v = args_simple<GFoo2<int>> (new GFoo2<int> () { t = 11, t2 = 12 }, 44, new GFoo2<int> () { t = 11, t2 = 12 });
		if (v != 44)
			return 4;
		// Check that EH works properly
		try {
			eh_in<int> (1, 2);
		} catch (OverflowException) {
		}
		return 0;
	}

	public static int test_0_gsharedvt_in_ret () {
		int i = return_t<int> (42);
		if (i != 42)
			return 1;
		long l = return_t<long> (Int64.MaxValue);
		if (l != Int64.MaxValue)
			return 2;
		double d = return_t<double> (3.0);
		if (d != 3.0)
			return 3;
		float f = return_t<float> (3.0f);
		if (f != 3.0f)
			return 4;
		short s = return_t<short> (16);
		if (s != 16)
			return 5;
		var v = new GFoo2<int> () { t = 55, t2 = 32 };
		var v2 = return_t<GFoo2<int>> (v);
		if (v2.t != 55 || v2.t2 != 32)
			return 6;
		IFaceGSharedVtIn o = new ClassGSharedVtIn ();
		var v3 = new GFoo2<long> () { t = 55, t2 = 32 };
		var v4 = o.return_t<GFoo2<long>> (v3);
		if (v4.t != 55 || v4.t2 != 32)
			return 7;
		i = new GSharedTests ().return_this_t<int> (42);
		if (i != 42)
			return 8;
		return 0;
	}

	public static int test_0_gsharedvt_in_delegates () {
		Func<int, int> f = new Func<int, int> (return_t<int>);
		if (f (42) != 42)
			return 1;
		return 0;
	}

	class DelClass {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static T return_t<T> (T t) {
			return t;
		}
	}

	public static int test_0_gsharedvt_in_delegates_reflection () {
		var m = typeof(DelClass).GetMethod ("return_t").MakeGenericMethod (new Type [] { typeof (int) });
		Func<int, int> f = (Func<int, int>)Delegate.CreateDelegate (typeof (Func<int,int>), null, m, false);
		if (f (42) != 42)
			return 1;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T return2_t<T> (T t) {
		return return_t (t);
	}

	public static int test_0_gsharedvt_calls () {
		if (return2_t (2) != 2)
			return 1;
		if (return2_t ("A") != "A")
			return 2;
		if (return2_t (2.0) != 2.0)
			return 3;
		return 0;
	}

	static GFoo3<T> newobj<T> (T t1, T t2) {
		return new GFoo3<T> (t1, t2);
	}
	
	public static int test_0_gshared_new () {
		var g1 = newobj (1, 2);
		if (g1.t != 1 || g1.t2 != 2)
			return 1;
		var g2 = newobj (1.0, 2.0);
		if (g1.t != 1.0 || g1.t2 != 2.0)
			return 2;

		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static GFoo2<T> newobj_vt<T> (T t1, T t2) {
		return new GFoo2<T> () { t = t1, t2 = t2 };
	}

	public static int test_0_gshared_new_vt () {
		GFoo2<int> v1 = newobj_vt (1, 2);
		if (v1.t != 1 || v1.t2 != 2)
			return 1;
		GFoo2<double> v2 = newobj_vt (1.0, 2.0);
		if (v2.t != 1.0 || v2.t2 != 2.0)
			return 2;
		return 0;
	}

	//
	// Tests for transitioning out of gsharedvt code
	//

	// T1=Nullable<..> is not currently supported by gsharedvt

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T return_t_nogshared<T,T1> (T t) {
		object o = t;
		T t2 = (T)o;
		//Console.WriteLine ("X: " + t);
		return t;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int return_int_nogshared<T,T1> (T t) {
		object o = t;
		T t2 = (T)o;
		return 2;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static A return_vtype_nogshared<T,T1> (T t) {
		object o = t;
		T t2 = (T)o;
		return new A () { a = 1, b = 2, c = 3 };
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T return2_t_out<T> (T t) {
		return return_t_nogshared<T, int?> (t);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int return2_int_out<T> (T t) {
		return return_int_nogshared<T, int?> (t);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static A return2_vtype_out<T> (T t) {
		return return_vtype_nogshared<T, int?> (t);
	}

	struct A {
		public int a, b, c;
	}

	[Category ("!FULLAOT")]
	public static int test_0_gsharedvt_out () {
		if (return2_t_out (2) != 2)
			return 1;
		if (return2_t_out ("A") != "A")
			return 2;
		if (return2_t_out (2.0) != 2.0)
			return 3;
		if (return2_t_out (2.0f) != 2.0f)
			return 4;
		A a = new A () { a = 1, b = 2, c = 3 };
		A a2 = return2_t_out (a);
		if (a2.a != 1 || a2.b != 2 || a2.c != 3)
			return 5;
		// Calls with non gsharedvt return types
		if (return2_int_out (1) != 2)
			return 6;
		A c = return2_vtype_out (a);
		if (a2.a != 1 || a2.b != 2 || a2.c != 3)
			return 7;
		return 0;
	}

	public class GenericClass<T> {
		public static T Z (IList<T> x, int index)
		{
			return x [index];
		}
	}

	public static int test_0_generic_array_helpers () {
		int[] x = new int[] {100, 200};

		// Generic array helpers should be treated as gsharedvt-out
		if (GenericClass<int>.Z (x, 0) != 100)
			return 1;

		return 0;
	}

	internal class IntComparer : IComparer<int>
	{
		public int Compare (int ix, int iy)
		{
			if (ix == iy)
				return 0;

			if (((uint) ix) < ((uint) iy))
				return -1;
			return 1;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int gshared_out_iface<T> (T t1, T t2, IComparer<T> comp) {
		return comp.Compare (t1, t2);
	}

	public static int test_0_gshared_out_iface () {
		// Call out from gshared to a nongeneric method through a generic interface method
		if (gshared_out_iface (2, 2, new IntComparer ()) != 0)
			return 1;
		return 0;
	}

	struct Foo1 {
		public int i1, i2, i3;
	}

	struct Foo2<T> {
		int i1, i2, i3, i4, i5;
		public T foo;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]	
	public static void locals<T> (T t) {
		Foo2<T> t2 = new Foo2<T> ();
		object o = t2;
	}

	public static int test_0_locals () {
		// Test that instantiations of type parameters are allocated the proper local type
		int i = 1;
		for (int j = 0; j < 10; ++j)
			i ++;
		locals<Foo1> (new Foo1 () { i1 = 1, i2 = 2, i3 = 3 });
		return 0;
	}

	public interface IFace<T> {
		T return_t_iface (T t);
	}

	public class Parent<T> {
		public virtual T return_t_vcall (T t) {
			throw new Exception ();
			return t;
		}
	}

	public class Child<T> : Parent<T>, IFace<T> {
		public override T return_t_vcall (T t) {
			return t;
		}
		public T return_t_iface (T t) {
			return t;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T return_t_vcall<T> (Parent<T> r, T t) {
		return r.return_t_vcall (t);
	}

	public static int test_0_vcalls () {
		if (return_t_vcall (new Child<int> (), 2) != 2)
			return 1;
		// Patching
		for (int i = 0; i < 10; ++i) {
			if (return_t_vcall (new Child<int> (), 2) != 2)
				return 2;
		}
		if (return_t_vcall (new Child<double> (), 2.0) != 2.0)
			return 3;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T return_t_iface<T> (IFace<T> r, T t) {
		return r.return_t_iface (t);
	}

	public static int test_0_iface_calls () {
		if (return_t_iface (new Child<int> (), 2) != 2)
			return 1;
		if (return_t_iface (new Child<double> (), 2.0) != 2.0)
			return 3;
		return 0;
	}

	interface IFaceKVP {
		T do_kvp<T> (T a);
	}

	static KeyValuePair<T1, T2> make_kvp<T1, T2> (T1 t1, T2 t2) {
		return new KeyValuePair<T1, T2> (t1, t2);
	}

	static T2 use_kvp<T1, T2> (KeyValuePair<T1, T2> kvp) {
		return kvp.Value;
	}

	class ClassKVP : IFaceKVP {
		public T do_kvp<T> (T a) {
			var t = make_kvp (a, a);
			// argument is an instance of a vtype instantiated with gsharedvt type arguments
			return use_kvp (t);
		}
	}

	public static int test_0_gsharedvt_ginstvt_constructed_arg () {
		{
			// AOT: Force a instantiation of use_kvp<long>
			long a = 1;
			var t = make_kvp (a, a);
			var z = use_kvp (t);
		}

		IFaceKVP c = new ClassKVP ();
		if (c.do_kvp<long> (1) != 1)
			return 1;
		return 0;
	}

	public static int test_0_gsharedvt_ginstvt_constructed_arg_float () {
		{
			// AOT: Force a instantiation of use_kvp<double>
			double a = 1;
			var t = make_kvp (a, a);
			var z = use_kvp (t);
		}

		IFaceKVP c = new ClassKVP ();
		if (c.do_kvp<double> (1) != 1)
			return 1;
		return 0;
	}

	interface IGetter
	{
		T Get<T>();
	}

	class Getter : IGetter
	{
		public T Get<T>() { return default(T); }
	}

	abstract class Session
	{
		public abstract IGetter Getter { get; }
	}

	class IosSession : Session
	{
		private IGetter getter = new Getter();
		public override IGetter Getter { get { return getter; } }
	}

	enum ENUM_TYPE {
	}

	public static int test_0_regress_5156 () {
		new IosSession().Getter.Get<ENUM_TYPE>();
		return 0;
	}

	public struct VT
	{
		public Action a;
	}

	public class D
	{
	}

	public class A3
	{
		public void OuterMethod<TArg1>(TArg1 value)
		{
			this.InnerMethod<TArg1, long>(value, 0);
		}

		private void InnerMethod<TArg1, TArg2>(TArg1 v1, TArg2 v2)
		{
			//Console.WriteLine("{0} {1}",v1,v2);
		}
	}

	public static int test_0_regress_2096 () {
		var a = new A3();

		// The following work:
		a.OuterMethod<int>(1);
		a.OuterMethod<DateTime>(DateTime.Now);

		var v = new VT();
		a.OuterMethod<VT>(v);

		var x = new D();
		// Next line will crash with Attempting to JIT compile method on device
		//  Attempting to JIT compile method
		a.OuterMethod<D>(x);
		return 0;
	}

	public class B
	{
		public void Test<T>()
		{
			//System.Console.WriteLine(typeof(T));
		}
	}

	public class A<T>
	{
		public void Test()
		{
			new B().Test<System.Collections.Generic.KeyValuePair<T, T>>();
		}
	}

    public static int test_0_regress_6040 () {
        //new B().Test<System.Collections.Generic.KeyValuePair<string, string>>();
        new A<int>().Test();
        new A<object>().Test();
        new A<string>().Test();
		return 0;
    }

	class ArrayContainer<T> {
		private T[,] container = new T[1,1];

		public T Prop {
			[MethodImplAttribute (MethodImplOptions.NoInlining)]
			get {
				return container [0, 0];
			}
			[MethodImplAttribute (MethodImplOptions.NoInlining)]
			set {
				container [0, 0] = value;
			}
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int test_0_multi_dim_arrays () {
		var c = new ArrayContainer<int> ();
		c.Prop = 5;
		return c.Prop == 5 ? 0 : 1;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static T2 rgctx_in_call_innner_inner<T1, T2> (T1 t1, T2 t2) {
		return t2;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static GFoo3<T> rgctx_in_call_inner<T> (T t) {
		return rgctx_in_call_innner_inner (1, new GFoo3<T> ());
	}

    public static int test_0_rgctx_in_call () {
		// The call is made through the rgctx call, and it needs an IN trampoline
		var t = rgctx_in_call_inner (1);
		if (t is GFoo3<int>)
			return 0;
		return 1;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void arm_params1<T> (T t1, T t2, T t3, T t4, T t5, T t6) {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static void arm_params2<T> (T t1, T t2, T t3, long t4, T t5, T t6) {
	}

	public static int test_0_arm_param_passing () {
		arm_params1<int> (1, 2, 3, 4, 5, 6);
		arm_params1<int> (1, 2, 3, 4, 5, 6);
		return 0;
	}

	sealed class ScheduledItem<TAbsolute, TValue> {
		private readonly object _scheduler;
		private readonly TValue _state;
		private readonly object _action;

		public ScheduledItem(object o, TValue state, object action, TAbsolute dueTime) {
			_state = state;
		}
	}

    abstract class VirtualTimeSchedulerBase<TAbsolute, TRelative> {
        public abstract void ScheduleAbsolute<TState>(TState state, TAbsolute dueTime);
	}

	class VirtualTimeScheduler<TAbsolute, TRelative> : VirtualTimeSchedulerBase<TAbsolute, TRelative> {
		public override void ScheduleAbsolute<TState>(TState state, TAbsolute dueTime) {
			var si = new ScheduledItem<TAbsolute, TState>(this, state, null, dueTime);
		}
	}

	public static int test_0_rx_mixed_regress () {
		var v = new VirtualTimeScheduler<long, long> ();
		v.ScheduleAbsolute<Action> (null, 22);
		return 0;
	}

	public class Base {
		public virtual T foo<T> (T t) {
			return t;
		}
	}

	class Class1 : Base {
		public object o;

		public override T foo<T> (T t) {
			o = t;
			return t;
		}
	}

	class Class2 : Base {
		public object o;

		public override T foo<T> (T t) {
			o = t;
			return t;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bar<T> (Base b, T t) {
		b.foo (t);
	}

	public static int test_0_virtual_generic () {
		Class1 c1 = new Class1 ();
		Class2 c2 = new Class2 ();
		bar (c1, 5);
		if (!(c1.o is int) || ((int)c1.o != 5))
			return 1;
		bar (c1, 6.0);
		if (!(c1.o is double) || ((double)c1.o != 6.0))
			return 2;
		bar (c1, 7.0f);
		if (!(c1.o is float) || ((float)c1.o != 7.0f))
			return 3;
		bar (c2, 5);
		if (!(c2.o is int) || ((int)c2.o != 5))
			return 4;
		bar (c2, 6.0);
		bar (c2, 7.0f);
		return 0;
	}

	public interface IFace1<T> {
		void m1 ();
		void m2 ();
		void m3 ();
		void m4 ();
		void m5 ();
	}

	public class ClassIFace<T> : IFace1<T> {
		public void m1 () {
		}
		public void m2 () {
		}
		public void m3 () {
		}
		public void m4 () {
		}
		public void m5 () {
		}
	}

	interface IFaceIFaceCall {
		void call<T, T2> (IFace1<object> iface);
	}

	class MakeIFaceCall : IFaceIFaceCall {
		public void call<T, T2> (IFace1<object> iface) {
			iface.m1 ();
		}
	}

	// Check normal interface calls from gsharedvt call to fully instantiated methods
	public static int test_0_instatiated_iface_call () {
		ClassIFace<object> c1 = new ClassIFace<object> ();

		IFaceIFaceCall c = new MakeIFaceCall ();

		c.call<object, int> (c1);
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static string to_string<T, T2>(T t, T2 t2) {
		return t.ToString ();
	}

	public enum AnEnum {
		One,
		Two
	};

	public static int test_0_constrained_tostring () {
		if (to_string<int, int> (1, 1) != "1")
			return 1;
		if (to_string<AnEnum, int> (AnEnum.One, 1) != "One")
			return 2;
		if (to_string<string, int> ("A", 1) != "A")
			return 3;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static int get_hash<T, T2>(T t, T2 t2) {
		return t.GetHashCode ();
	}

	public static int test_0_constrained_get_hash () {
		if (get_hash<int, int> (1, 1) != 1.GetHashCode ())
			return 1;
		if (get_hash<double, int> (1.0, 1) != 1.0.GetHashCode ())
			return 2;
		if (get_hash<AnEnum, int> (AnEnum.One, 1) != AnEnum.One.GetHashCode ())
			return 3;
		if (get_hash<string, int> ("A", 1) != "A".GetHashCode ())
			return 4;
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static bool equals<T, T2>(T t, T2 t2) {
		return t.Equals (t);
	}

	public static int test_0_constrained_equals () {
		if (equals<int, int> (1, 1) != true)
			return 1;
		if (equals<double, int> (1.0, 1) != true)
			return 2;
		if (equals<AnEnum, int> (AnEnum.One, 1) != true)
			return 3;
		if (equals<string, int> ("A", 1) != true)
			return 4;
		return 0;
	}

	interface IGetType {
		Type gettype<T, T2>(T t, T2 t2);
	}

	public class CGetType : IGetType {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public Type gettype<T, T2>(T t, T2 t2) {
			return t.GetType ();
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public Type gettype2<T>(T t) {
			return t.GetType ();
		}
	}

	public static int test_0_constrained_gettype () {
		IGetType c = new CGetType ();
		if (c.gettype<int, int> (1, 1) != typeof (int))
			return 1;
		if (c.gettype<string, int> ("A", 1) != typeof (string))
			return 2;
		/* Partial sharing */
		var c2 = new CGetType ();
		if (c2.gettype2<long> (1) != typeof (long))
			return 3;
		return 0;
	}

	interface IConstrainedCalls {
		Pair<int, int> vtype_ret<T, T2>(T t, T2 t2) where T: IReturnVType;
		AnEnum enum_ret<T, T2>(T t, T2 t2) where T: IReturnVType;
	}

	public interface IReturnVType {
		Pair<int, int> return_vtype ();
		AnEnum return_enum ();
	}

	public class CConstrainedCalls : IConstrainedCalls {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public Pair<int, int> vtype_ret<T, T2>(T t, T2 t2) where T : IReturnVType {
			return t.return_vtype ();
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public AnEnum enum_ret<T, T2>(T t, T2 t2) where T : IReturnVType {
			return t.return_enum ();
		}
	}

	class ReturnVType : IReturnVType {
		public Pair<int, int> return_vtype () {
			return new Pair<int, int> () { First = 1, Second = 2 };
		}
		public AnEnum return_enum () {
			return AnEnum.Two;
		}
	}

	public static int test_0_constrained_vtype_ret () {
		IConstrainedCalls c = new CConstrainedCalls ();
		var r = c.vtype_ret<ReturnVType, int> (new ReturnVType (), 1);
		if (r.First != 1 || r.Second != 2)
			return 1;
		return 0;
	}

	public static int test_0_constrained_enum_ret () {
		IConstrainedCalls c = new CConstrainedCalls ();
		var r = c.enum_ret<ReturnVType, int> (new ReturnVType (), 1);
		if (r != AnEnum.Two)
			return 1;
		return 0;
	}

	public struct Pair<T1, T2> {
		public T1 First;
		public T2 Second;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static TState call_del<TState>(TState state, Func<object, TState, TState> action) {
		return action(null, state);
	}

	[Category ("!FULLAOT")]
	public static int test_0_delegate_wrappers () {
		Func<object, Pair<int, int>, Pair<int, int>> del1 = delegate (object o, Pair<int, int> p) { return p; };
		Func<object, Pair<int, int>, Pair<int, int>> del2 = delegate (object o, Pair<int, int> p) { return p; };
		Func<object, Pair<double, int>, Pair<double, int>> del3 = delegate (object o, Pair<double, int> p) { return p; };
		var r1 = call_del<Pair<int, int>> (new Pair<int, int> { First = 1, Second = 2}, del1);
		if (r1.First != 1 || r1.Second != 2)
			return 1;
		var r2 = call_del<Pair<int, int>> (new Pair<int, int> { First = 3, Second = 4}, del2);
		if (r2.First != 3 || r2.Second != 4)
			return 2;
		var r3 = call_del<Pair<double, int>> (new Pair<double, int> { First = 1.0, Second = 2}, del3);
		if (r3.First != 1.0 || r3.Second != 2)
			return 3;
		return 0;
	}

	class Base<T> {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public object foo<T1> (T1 t1, T t, object o) {
			return o;
		}
	}

	class AClass : Base<long> {

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public object bar<T> (T t, long time, object o) {
			return foo (t, time, o);
		}
	}

	public static int test_0_out_in_wrappers () {
		var a = new AClass ();
		object o1 = "A";
		object o2 = a.bar<long> (1024, 0, o1);
		if (o1 != o2)
			return 1;
		return 0;		
	}

		interface BIFace {
			object AMethod ();
		}

		class Base<TAbsolute, T2> : BIFace {

			public TAbsolute Clock { get; set; }

			public virtual object AMethod () {
				return Clock;
			}
		}

		class BClass : Base<long, long> {
		}

	public static int test_0_regress_1 () {
		BIFace c = new BClass ();
		object o = c.AMethod ();
		if (!(o is long) || ((long)o != 0))
			return 1;
		return 0;
	}

	interface IFace3 {
		T unbox_any<T> (object o);
	}

	class Class3 : IFace3 {
		public virtual T unbox_any<T> (object o) {
			return (T)o;
		}
	}

	public static int test_0_unbox_any () {
		IFace3 o = new Class3 ();
		if (o.unbox_any<int> (16) != 16)
			return 1;
		if (o.unbox_any<long> ((long)32) != 32)
			return 2;
		if (o.unbox_any<double> (2.0) != 2.0)
			return 3;
		try {
			o.unbox_any<int> (2.0);
			return 4;
		} catch (Exception) {
		}
		return 0;
	}

	interface IFace4 {
		TSource Catch<TSource, TException>(TSource t)  where TException : Exception;
	}

	class Class4 : IFace4 {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
			public TSource Catch<TSource, TException>(TSource t)  where TException : Exception {
			return t;
		}
	}

	// Check that mixed instantiations are correctly created/found in AOT
	public static int test_0_constraints () {
		IFace4 o = new Class4 ();
		o.Catch<int, Exception> (1);
		return 0;
	}

	internal static Type Process<TSource, TElement> (TSource[] arr, Action<TElement, TElement> call) {
		arr [0] = default (TSource);
		return typeof (TSource);
	}

	interface IFace5 {
		Type foo<T> ();
	}

	class Class5 : IFace5 {
		public Type foo<T> () {
			return Process<KeyValuePair<long, T>, T> (new KeyValuePair<long, T> [10], null);
		}
	}

	public static int test_0_rgctx_call_from_gshared_code () {
		var c = new Class5 ();
		if (c.foo<string> () != typeof (KeyValuePair<long, string>))
			return 1;
		return 0;
	}

	public class Enumbers<T> {
		public object Enumerate (List<KeyValuePair<T, string>> alist)
		{
			return alist.ToArray ();
		}
	}

	public static int test_0_checkthis_gshared_call () {
		Enumbers<string> e = new Enumbers<string> ();
		try {
			e.Enumerate (null);
			return 1;
		}
		catch (NullReferenceException) {
		}
		return 0;
	}

	interface IFace6 {
		T[] Del<T> (T t);
	}

	class Class6 : IFace6 {
		public T[] Del<T> (T t) {
			var res = new T [5];
			Func<T, T, T, T, T> func = delegate(T t1, T t2, T t3, T t4) { res [0] = t1; res [1] = t2; res [2] = t3; res [3] = t4; return t1; };
			var v = func.BeginInvoke(t, t, t, t, null, null);
			res [4] = func.EndInvoke (v);
			return res;
		}
	}

	// FIXME: The runtime-invoke wrapper used by BeginInvoke is not found
	[Category ("!FULLAOT")]
	public static int test_0_begin_end_invoke () {
		IFace6 o = new Class6 ();
		var arr1 = o.Del (1);
		if (arr1 [0] != 1 || arr1 [1] != 1 || arr1 [2] != 1 || arr1 [3] != 1 || arr1 [4] != 1)
			return 1;
		var arr2 = o.Del (2.0);
		if (arr2 [0] != 2.0 || arr2 [1] != 2.0 || arr2 [2] != 2.0 || arr2 [3] != 2.0 || arr2 [4] != 2.0)
			return 2;
		return 0;
	}

	public class TAbstractTableItem<TC> {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void SetProperty<TV> () {    }

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void Test () {
			SetProperty<bool> ();
		}
	}

	public static int test_0_gsharedvt_method_on_shared_class () {
       TAbstractTableItem<object>.Test ();
	   return 0;
	}

	interface IFaceBox {
		object box<T> (T t);
		bool is_null<T> (T t);
	}

	class ClassBox : IFaceBox {
		public object box<T> (T t) {
			object o = t;
			return o;
		}

		public bool is_null<T> (T t) {
			if (!(default(T) == null))
				return false;
			return true;
		}
	}

	public static int test_0_nullable_box () {
		IFaceBox c = new ClassBox ();
		int i = 5;
		object o = c.box<int?> (i);
		if ((int)o != i)
			return 1;
		if (c.box<int?> (null) != null)
			return 2;
		long l = Int64.MaxValue - 1;
		o = c.box<long?> (l);
		if ((long)o != l)
			return 3;
		if (c.box<long?> (null) != null)
			return 4;
		string s = "A";
		if (c.box<string> (s) != (object)s)
			return 5;
		return 0;
	}

	public static int test_0_nullable_box_brtrue_opt () {
		IFaceBox c = new ClassBox ();

		if (c.is_null<double?> (null))
			return 0;
		else
			return 1;
	}

	interface IFaceUnbox2 {
		T unbox<T> (object o);
	}

	class ClassUnbox2 : IFaceUnbox2 {
		public T unbox<T> (object o) {
			return (T)o;
		}
	}

	public static int test_0_nullable_unbox () {	
		IFaceUnbox2 c = new ClassUnbox2 ();
		int? i = c.unbox<int?> (5);
		if (i != 5)
			return 1;
		int? j = c.unbox<int?> (null);
		if (j != null)
			return 2;
		return 0;
	}

	interface IConstrained {
		void foo ();
		void foo_ref_arg (string s);
	}

	interface IConstrained<T3> {
		void foo_gsharedvt_arg (T3 s);
		T3 foo_gsharedvt_ret (T3 s);
	}

	static object constrained_res;

	struct ConsStruct : IConstrained {
		public int i;

		public void foo () {
			constrained_res = i;
		}

		public void foo_ref_arg (string s) {
			constrained_res = s == "A" ? 42 : 0;
		}
	}

	class ConsClass : IConstrained {
		public int i;

		public void foo () {
			constrained_res = i;
		}

		public void foo_ref_arg (string s) {
			constrained_res = s == "A" ? 43 : 0;
		}
	}

	struct ConsStruct<T> : IConstrained<T> {
		public void foo_gsharedvt_arg (T s) {
			constrained_res = s;
		}

		public T foo_gsharedvt_ret (T s) {
			return s;
		}
	}

	struct ConsStructThrow : IConstrained {
		public void foo () {
			throw new Exception ();
		}

		public void foo_ref_arg (string s) {
		}
	}

	interface IFaceConstrained {
		void constrained_void_iface_call<T, T2>(T t, T2 t2) where T2 : IConstrained;
		void constrained_void_iface_call_ref_arg<T, T2>(T t, T2 t2) where T2 : IConstrained;
		void constrained_void_iface_call_gsharedvt_arg<T, T2, T3>(T t, T2 t2, T3 t3) where T2 : IConstrained<T>;
		T constrained_iface_call_gsharedvt_ret<T, T2, T3>(T t, T2 t2, T3 t3) where T2 : IConstrained<T>;
		T2 constrained_normal_call<T, T2>(T t, T2 t2) where T2 : VClass;
	}

	class ClassConstrained : IFaceConstrained {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public void constrained_void_iface_call<T, T2>(T t, T2 t2) where T2 : IConstrained {
			t2.foo ();
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public void constrained_void_iface_call_ref_arg<T, T2>(T t, T2 t2) where T2 : IConstrained {
			t2.foo_ref_arg ("A");
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public void constrained_void_iface_call_gsharedvt_arg<T, T2, T3>(T t, T2 t2, T3 t3) where T2 : IConstrained<T> {
			t2.foo_gsharedvt_arg (t);
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public T constrained_iface_call_gsharedvt_ret<T, T2, T3>(T t, T2 t2, T3 t3) where T2 : IConstrained<T> {
			return t2.foo_gsharedvt_ret (t);
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public T2 constrained_normal_call<T, T2>(T t, T2 t2) where T2 : VClass {
			/* This becomes a constrained call even through 't2' is forced to be a reference type by the constraint */
			return (T2)t2.foo (5);
		}
	}

	class VClass {
		public virtual VClass foo (int i) {
			return this;
		}
	}

	public static int test_0_constrained_void_iface_call () {
		IFaceConstrained c = new ClassConstrained ();
		var s = new ConsStruct () { i = 42 };
		constrained_res = null;
		c.constrained_void_iface_call<int, ConsStruct> (1, s);
		if (!(constrained_res is int) || ((int)constrained_res) != 42)
			return 1;
		constrained_res = null;
		c.constrained_void_iface_call_ref_arg<int, ConsStruct> (1, s);
		if (!(constrained_res is int) || ((int)constrained_res) != 42)
			return 2;
		var s2 = new ConsClass () { i = 43 };
		constrained_res = null;
		c.constrained_void_iface_call<int, ConsClass> (1, s2);
		if (!(constrained_res is int) || ((int)constrained_res) != 43)
			return 3;
		constrained_res = null;
		c.constrained_void_iface_call_ref_arg<int, ConsClass> (1, s2);
		if (!(constrained_res is int) || ((int)constrained_res) != 43)
			return 4;
		return 0;
	}

	public static int test_0_constrained_eh () {
		var s2 = new ConsStructThrow () { };
		try {
			IFaceConstrained c = new ClassConstrained ();
			c.constrained_void_iface_call<int, ConsStructThrow> (1, s2);
			return 1;
		} catch (Exception) {
			return 0;
		}
	}

	public static int test_0_constrained_void_iface_call_gsharedvt_arg () {
		// This tests constrained calls through interfaces with one gsharedvt arg, like IComparable<T>.CompareTo ()
		IFaceConstrained c = new ClassConstrained ();

		var s = new ConsStruct<int> ();
		constrained_res = null;
		c.constrained_void_iface_call_gsharedvt_arg<int, ConsStruct<int>, int> (42, s, 55);
		if (!(constrained_res is int) || ((int)constrained_res) != 42)
			return 1;

		var s2 = new ConsStruct<string> ();
		constrained_res = null;
		c.constrained_void_iface_call_gsharedvt_arg<string, ConsStruct<string>, int> ("A", s2, 55);
		if (!(constrained_res is string) || ((string)constrained_res) != "A")
			return 2;

		return 0;
	}

	public static int test_0_constrained_iface_call_gsharedvt_ret () {
		IFaceConstrained c = new ClassConstrained ();

		var s = new ConsStruct<int> ();
		int ires = c.constrained_iface_call_gsharedvt_ret<int, ConsStruct<int>, int> (42, s, 55);
		if (ires != 42)
			return 1;

		var s2 = new ConsStruct<string> ();
		string sres = c.constrained_iface_call_gsharedvt_ret<string, ConsStruct<string>, int> ("A", s2, 55);
		if (sres != "A")
			return 2;

		return 0;
	}

	public static int test_0_constrained_normal_call () {
		IFaceConstrained c = new ClassConstrained ();

		var o = new VClass ();
		var res = c.constrained_normal_call<int, VClass> (1, o);
		return res == o ? 0 : 1;
	}

	public static async Task<T> FooAsync<T> (int i, int j) {
		Task<int> t = new Task<int> (delegate () { return 42; });
		var response = await t;
		return default(T);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void call_async<T> (int i, int j) {
		Task<T> t = FooAsync<T> (1, 2);
		// FIXME: This doesn't work
		//t.RunSynchronously ();
	}

	// In AOT mode, the async infrastructure depends on gsharedvt methods
	public static int test_0_async_call_from_generic () {
		call_async<string> (1, 2);
		return 0;
	}

	public static int test_0_array_helper_gsharedvt () {
		var arr = new AnEnum [16];
		var c = new ReadOnlyCollection<AnEnum> (arr);
		return c.Contains (AnEnum.Two) == false ? 0 : 1;
	}

	interface IFaceCallPatching {
		bool caller<T, T2> ();
	}

	class CallPatching2<T> {
		T t;
		public object o;

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public bool callee () {
			return (string)o == "ABC";
		}
	}

	class CallPatching : IFaceCallPatching {
		public bool caller<T, T2> () {
			var c = new CallPatching2<T> ();
			c.o = "ABC";
			return c.callee ();
		}
	}

	//
	// This tests that generic calls made from gsharedvt methods are not patched normally.
	// If they are, the first call to 'caller' would patch in the gshared version of
	// 'callee', causing the second call to fail because the gshared version of callee
	// wouldn't work with CallPatching2<bool> since it has a different object layout.
	//
	public static int test_0_call_patching () {
		IFaceCallPatching c = new CallPatching ();
		c.caller<object, bool> ();
		if (!c.caller<bool, bool> ())
			return 1;
		return 0;
	}

	struct EmptyStruct {
	}

	public struct BStruct {
		public int a, b, c, d;
	}

	interface IFoo3<T> {
		int Bytes (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
				   byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8);
		int SBytes (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
					sbyte b1, sbyte b2, sbyte b3, sbyte b4);
		int Shorts (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
					short b1, short b2, short b3, short b4);
		int UShorts (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
					ushort b1, ushort b2, ushort b3, ushort b4);
		int Ints (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
				  int i1, int i2, int i3, int i4);
		int UInts (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
				   uint i1, uint i2, uint i3, uint i4);
		int Structs (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
					 BStruct s);
		void Generic<T2> (T t, T2[] arr, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
						  T2 i1, T2 i2, T2 i3, T2 i4);
	}

	class Foo3<T> : IFoo3<T> {
		public int Bytes (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
						  byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8) {
			return b1 + b2 + b3 + b4 + b5 + b6 + b7 + b8;
		}
		public int SBytes (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
						  sbyte b1, sbyte b2, sbyte b3, sbyte b4) {
			return b1 + b2 + b3 + b4;
		}
		public int Shorts (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
						   short b1, short b2, short b3, short b4) {
			return b1 + b2 + b3 + b4;
		}
		public int UShorts (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
							ushort b1, ushort b2, ushort b3, ushort b4) {
			return b1 + b2 + b3 + b4;
		}
		public int Ints (T t, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8,
						   int i1, int i2, int i3, int i4) {
			return i1 + i2 + i3 + i4;
		}
		public int UInts (T t, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8,
						  uint i1, uint i2, uint i3, uint i4) {
			return (int)(i1 + i2 + i3 + i4);
		}
		public int Structs (T t, int dummy1, int a2, int a3, int a4, int a5, int a6, int a7, int dummy8,
							BStruct s) {
			return s.a + s.b + s.c + s.d;
		}

		public void Generic<T2> (T t, T2[] arr, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, T2 i1, T2 i2, T2 i3, T2 i4) {
			arr [0] = i1;
			arr [1] = i2;
			arr [2] = i3;
			arr [3] = i4;
		}
	}

	// Passing small normal arguments on the stack
	public static int test_0_arm64_small_stack_args () {
		IFoo3<EmptyStruct> o = (IFoo3<EmptyStruct>)Activator.CreateInstance (typeof (Foo3<>).MakeGenericType (new Type [] { typeof (EmptyStruct) }));
		int res = o.Bytes (new EmptyStruct (), 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8);
		if (res != 36)
			return 1;
		int res2 = o.SBytes (new EmptyStruct (), 1, 2, 3, 4, 5, 6, 7, 8, -1, -2, -3, -4);
		if (res2 != -10)
			return 2;
		int res3 = o.Shorts (new EmptyStruct (), 1, 2, 3, 4, 5, 6, 7, 8, -1, -2, -3, -4);
		if (res3 != -10)
			return 3;
		int res4 = o.UShorts (new EmptyStruct (), 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4);
		if (res4 != 10)
			return 4;
		int res5 = o.Ints (new EmptyStruct (), 1, 2, 3, 4, 5, 6, 7, 8, -1, -2, -3, -4);
		if (res5 != -10)
			return 5;
		int res6 = o.UInts (new EmptyStruct (), 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4);
		if (res6 != 10)
			return 6;
		int[] arr = new int [4];
		o.Generic<int> (new EmptyStruct (), arr, 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4);
		if (arr [0] != 1 || arr [1] != 2 || arr [2] != 3 || arr [3] != 4)
			return 7;
		return 0;
	}

	interface ISmallArg {
		T foo<T> (string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8,
				  string s9, string s10, string s11, string s12, string s13, T t);
	}

	class SmallArgClass : ISmallArg {
			public T foo<T> (string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8,
							 string s9, string s10, string s11, string s12, string s13, T t) {
				return t;
			}
		}

	public static int test_1_small_gsharedvt_stack_arg_ios () {
		ISmallArg o = new SmallArgClass ();
		return o.foo<int> ("", "", "", "", "", "", "", "", "", "", "", "", "", 1);
	}

	// Passing vtype normal arguments on the stack
	public static int test_0_arm64_vtype_stack_args () {
		IFoo3<EmptyStruct> o = (IFoo3<EmptyStruct>)Activator.CreateInstance (typeof (Foo3<>).MakeGenericType (new Type [] { typeof (EmptyStruct) }));
		int res = o.Structs (new EmptyStruct (), 1, 2, 3, 4, 5, 6, 7, 8, new BStruct () { a = 1, b = 2, c = 3, d = 4 });
		if (res != 10)
			return 1;
		return 0;
	}

	interface IFoo4<T> {
		T Get(T[,] arr, T t);
	}

	class Foo4<T> : IFoo4<T> {
		public T Get(T[,] arr, T t) {
			arr [1, 1] = t;
			return arr [1, 1];
		}
	}

	struct AStruct {
		public int a, b;
	}

	public static int test_0_multi_dim_arrays_2 () {
		IFoo4<int> foo = new Foo4<int> ();
		var arr = new int [10, 10];
		int res = foo.Get (arr, 10);
		if (res != 10)
			return 1;

		IFoo4<AStruct> foo2 = new Foo4<AStruct> ();
		var arr2 = new AStruct [10, 10];
		var res2 = foo2.Get (arr2, new AStruct () { a = 1, b = 2 });
		if (res2.a != 1 || res2.b != 2)
			return 2;
		return 0;
	}

	public interface IFaceTest {
		int iface_method ();
	}

	public interface IFaceConstrainedIFace {
		int foo<T, T2> (ref T val) where T: IFaceTest;
	}

	class ConstrainedIFace : IFaceConstrainedIFace {
		public int foo<T, T2> (ref T val) where T: IFaceTest {
			return val.iface_method ();
		}
	}

	class ClassTest : IFaceTest {
		public int iface_method () {
			return 42;
		}
	}

	struct StructTest : IFaceTest {

		int i;

		public StructTest (int arg) {
			i = arg;
		}

		public int iface_method () {
			return i;
		}
	}

	// Test constrained calls on an interface made from gsharedvt methods
	public static int test_42_gsharedvt_constrained_iface () {
		IFaceConstrainedIFace obj = new ConstrainedIFace ();
		IFaceTest t = new ClassTest ();
		return obj.foo<IFaceTest, int> (ref t);
	}

	public static int test_42_gsharedvt_constrained_iface_vtype () {
		IFaceConstrainedIFace obj = new ConstrainedIFace ();
		IFaceTest t = new StructTest (42);
		return obj.foo<IFaceTest, int> (ref t);
	}

	// Sign extension tests
	// 0x55   == 85    == 01010101
	// 0xAA   == 170   == 10101010
	// 0x5555 == 21845 == 0101010101010101
	// 0xAAAA == 43690 == 1010101010101010
	// 0x55555555 == 1431655765
	// 0xAAAAAAAA == 2863311530
	// 0x5555555555555555 == 6148914691236517205
	// 0xAAAAAAAAAAAAAAAA == 12297829382473034410

	public interface SEFace<T> {
		T Copy (int a, int b, int c, int d, T t);
	}

	class SEClass<T> : SEFace<T> {
		public T Copy (int a, int b, int c, int d, T t) {
			return t;
		}
	}

	// Test extension
	static int test_20_signextension_sbyte () {
		Type t = typeof (sbyte);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<sbyte>)o;

		long zz = i.Copy (1,2,3,4,(sbyte)(-0x55));

		bool success = zz == -0x55;
		return success ? 20 : 1;
	}

	static int test_20_signextension_byte () {
		Type t = typeof (byte);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<byte>)o;

		ulong zz = i.Copy (1,2,3,4,(byte)(0xAA));

		bool success = zz == 0xAA;
		return success ? 20 : 1;
	}

	static int test_20_signextension_short () {
		Type t = typeof (short);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<short>)o;

		long zz = i.Copy (1,2,3,4,(short)(-0x5555));

		bool success = zz == -0x5555;
		return success ? 20 : 1;
	}

	static int test_20_signextension_ushort () {
		Type t = typeof (ushort);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<ushort>)o;

		ulong zz = i.Copy (1,2,3,4,(ushort)(0xAAAA));

		bool success = zz == 0xAAAA;
		return success ? 20 : 1;
	}

	static int test_20_signextension_int () {
		Type t = typeof (int);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<int>)o;

		long zz = i.Copy (1,2,3,4,(int)(-0x55555555));

		bool success = zz == -0x55555555;
		return success ? 20 : 1;
	}

	static int test_20_signextension_uint () {
		Type t = typeof (uint);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<uint>)o;

		ulong zz = i.Copy (1,2,3,4,(uint)(0xAAAAAAAA));

		bool success = zz == 0xAAAAAAAA;
		return success ? 20 : 1;
	}

	static int test_20_signextension_long () {
		Type t = typeof (long);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<long>)o;

		long zz = i.Copy (1,2,3,4,(long)(-0x5555555555555555));

		bool success = zz == -0x5555555555555555;
		return success ? 20 : 1;
	}

	static int test_20_signextension_ulong () {
		Type t = typeof (ulong);
		object o = Activator.CreateInstance (typeof (SEClass<>).MakeGenericType (new Type[] { t }));
		var i = (SEFace<ulong>)o;

		ulong zz = i.Copy (1,2,3,4,(ulong)(0xAAAAAAAAAAAAAAAA));

		bool success = zz == 0xAAAAAAAAAAAAAAAA;
		return success ? 20 : 1;
	}

	void gsharedvt_try_at_offset_0<T> (ref T disposable)
		where T : class, IDisposable {
			try {
				disposable.Dispose ();
			} finally {
				disposable = null;
			}
		}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	static DateTimeOffset gsharedvt_vphi_inner<T> (T t) {
		return DateTimeOffset.MinValue;
	}

	static DateTimeOffset gsharedvt_vphi<T> (T t) {
		int[] arr = new int [10];

		try {
			DateTimeOffset v;
			if (arr [0] == 0)
				v = gsharedvt_vphi_inner (t);
			else
				v = gsharedvt_vphi_inner (t);
			return v;
		} catch {
			return DateTimeOffset.MinValue;
		}
	}

	static int test_0_gsharedvt_vphi_volatile () {
		gsharedvt_vphi (0);
		return 0;
	}

	struct AStruct3<T1, T2, T3> {
		T1 t1;
		T2 t2;
		T3 t3;
	}

	interface IFaceIsRef {
		bool is_ref<T> ();
	}

	class ClassIsRef : IFaceIsRef {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public bool is_ref<T> () {
			return RuntimeHelpers.IsReferenceOrContainsReferences<T> ();
		}
	}

	public static int test_0_isreference_intrins () {
		IFaceIsRef iface = new ClassIsRef ();
		if (iface.is_ref<AStruct3<int, int, int>> ())
			return 1;
		if (!iface.is_ref<AStruct3<string, int, int>> ())
			return 2;
		return 0;
	}

	interface IFace59956 {
		int foo<T> ();
	}

	class Impl59956 : IFace59956 {
		public int foo<T> () {
			var builder = new SparseArrayBuilder<T>(true);

			return builder.Markers._count;
		}
	}

	public static int test_1_59956_regress () {
		IFace59956 iface = new Impl59956 ();
		return iface.foo<int> ();
	}
}

// #13191
public class MobileServiceCollection<TTable, TCol>
{
	public async Task<int> LoadMoreItemsAsync(int count = 0) {
		await Task.Delay (1000);
		int results = await ProcessQueryAsync ();
		return results;
	}

	protected async virtual Task<int> ProcessQueryAsync() {
		await Task.Delay (1000);
		throw new Exception ();
	}
}

// #59956
internal struct Marker
{
	public Marker(int count, int index) {
	}
}

public struct ArrayBuilder<T>
{
	private T[] _array;
	public int _count;

	public ArrayBuilder(int capacity) {
		_array = new T[capacity];
		_count = 1;
	}
}

internal struct SparseArrayBuilder<T>
{
	private ArrayBuilder<Marker> _markers;

	public SparseArrayBuilder(bool initialize) : this () {
		_markers = new ArrayBuilder<Marker> (10);
	}

	public ArrayBuilder<Marker> Markers => _markers;
}

#if !__MOBILE__
public class GSharedTests : Tests {
}
#endif
