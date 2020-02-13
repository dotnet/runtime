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
#endif

#if !__MOBILE__
	static int Main (string[] args)
	{
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

	public static int test_1_ldelem_stelem_any_single () {
		float[] arr = new float [3];
		stelem_any (arr, 1);

		return (int) ldelem_any (arr);
	}

	public static int test_1_ldelem_stelem_any_double () {
		double[] arr = new double [3];
		stelem_any (arr, 1);

		return (int) ldelem_any (arr);
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

	interface NonGenericInterface {
		int return_field ();
	}

	interface GenericInterface<T> : NonGenericInterface {
		T not_used ();
	}

	struct ImplementGenericInterface<T> : GenericInterface<T> {
		public Object padding1;
		public Object padding2;
		public Object padding3;
		public T[] arr_t;

		public ImplementGenericInterface (T[] arr_t) {
			this.padding1 = null;
			this.padding2 = null;
			this.padding3 = null;
			this.arr_t = arr_t;
		}

		public T not_used () {
			return arr_t [0];
		}

		public int return_field () {
			return arr_t.Length;
		}
	}

	public static int test_8_struct_implements_generic_interface () {
		int[] arr = {1, 2, 3, 4};
		NonGenericInterface s = new ImplementGenericInterface<int> (arr);
		return s.return_field () + s.return_field ();
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

	public class MRO : MarshalByRefObject {
		public GenericStruct<int> struct_field;
		public GenericClass<int> class_field;
	}

	public class MRO<T> : MarshalByRefObject {
		public T gen_field;

		public T stfld_ldfld (T t) {
			var m = this;
			m.gen_field = t;
			return m.gen_field;
		}
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

		// gshared
		var m2 = new MRO<string> ();
		if (m2.stfld_ldfld ("A") != "A")
			return 5;

		return 0;
	}

	// FIXME:
	[Category ("!FULLAOT")]
    public static int test_0_generic_virtual_call_on_vtype_unbox () {
		object o = new Object ();
        IFoo h = new Handler(o);

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

#if !__MOBILE__
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
#endif

	public static int test_0_ldvirtftn_generic_method () {
		new GenericsTests ().ldvirtftn<string> ();

		return the_type == typeof (string) ? 0 : 1;
	}

	public static int test_0_throw_dead_this () {
        new Foo<string> ("").throw_dead_this ();
		return 0;
	}

	struct S<T> {}

	public static int test_0_inline_infinite_polymorphic_recursion () {
           f<int>(0);

		   return 0;
	}

	private static void f<T>(int i) {
		if(i==42) f<S<T>>(i);
	}

	// This cannot be made to work with full-aot, since there it is impossible to
	// statically determine that Foo<string>.Bar <int> is needed, the code only
	// references IFoo.Bar<int>
	[Category ("!FULLAOT")]
	public static int test_0_generic_virtual_on_interfaces () {
		Foo<string>.count1 = 0;
		Foo<string>.count2 = 0;
		Foo<string>.count3 = 0;

		IFoo f = new Foo<string> ("");
		for (int i = 0; i < 1000; ++i) {
			f.Bar <int> ();
			f.Bar <string> ();
			f.NonGeneric ();
		}

		if (Foo<string>.count1 != 1000)
			return 1;
		if (Foo<string>.count2 != 1000)
			return 2;
		if (Foo<string>.count3 != 1000)
			return 3;

		VirtualInterfaceCallFromGenericMethod<long> (f);

		return 0;
	}

	public static int test_0_generic_virtual_on_interfaces_ref () {
		Foo<string>.count1 = 0;
		Foo<string>.count2 = 0;
		Foo<string>.count3 = 0;
		Foo<string>.count4 = 0;

		IFoo f = new Foo<string> ("");
		for (int i = 0; i < 1000; ++i) {
			f.Bar <string> ();
			f.Bar <object> ();
			f.NonGeneric ();
		}

		if (Foo<string>.count2 != 1000)
			return 2;
		if (Foo<string>.count3 != 1000)
			return 3;
		if (Foo<string>.count4 != 1000)
			return 4;

		return 0;
	}

	//repro for #505375
	[Category ("!FULLAOT")]
	public static int test_2_cprop_bug () {
		int idx = 0;
		int a = 1;
		var cmp = System.Collections.Generic.Comparer<int>.Default ;
		if (cmp.Compare (a, 0) > 0)
			a = 0;
		do { idx++; } while (cmp.Compare (idx - 1, a) == 0);
		return idx;
	}

	enum MyEnumUlong : ulong {
		Value_2 = 2
	}

	public static int test_0_regress_550964_constrained_enum_long () {
        MyEnumUlong a = MyEnumUlong.Value_2;
        MyEnumUlong b = MyEnumUlong.Value_2;

        return Pan (a, b) ? 0 : 1;
	}

    static bool Pan<T> (T a, T b)
    {
        return a.Equals (b);
    }

	public class XElement {
		public string Value {
			get; set;
		}
	}

	public static int test_0_fullaot_linq () {
		var allWords = new XElement [] { new XElement { Value = "one" } };
		var filteredWords = allWords.Where(kw => kw.Value.StartsWith("T"));
		return filteredWords.Count ();
	}

	public static int test_0_fullaot_comparer_t () {
		var l = new SortedList <TimeSpan, int> ();
		return l.Count;
	}

	public static int test_0_fullaot_comparer_t_2 () {
		var l = new Dictionary <TimeSpan, int> ();
		return l.Count;
	}

	static void enumerate<T> (IEnumerable<T> arr) {
		foreach (var o in arr)
			;
		int c = ((ICollection<T>)arr).Count;
	}

	/* Test that treating arrays as generic collections works with full-aot */
	public static int test_0_fullaot_array_wrappers () {
		GenericsTests[] arr = new GenericsTests [10];
		enumerate<GenericsTests> (arr);
		return 0;
	}

	static int cctor_count = 0;

    public abstract class Beta<TChanged> 
    {		
        static Beta()
        {
			cctor_count ++;
        }
    }   
    
    public class Gamma<T> : Beta<T> 
    {   
        static Gamma()
        {
        }
    }

	// #519336    
	public static int test_2_generic_class_init_gshared_ctor () {
		new Gamma<object>();
		new Gamma<string>();

		return cctor_count;
	}

	static int cctor_count2 = 0;

	class ServiceController<T> {
		static ServiceController () {
			cctor_count2 ++;
		}

		public ServiceController () {
		}
	}

	static ServiceController<T> Create<T>() {
		return new ServiceController<T>();
	}

	// #631409
	public static int test_2_generic_class_init_gshared_ctor_from_gshared () {
		Create<object> ();
		Create<string> ();

		return cctor_count2;
	}

	public static Type get_type<T> () {
		return typeof (T);
	}

	public static int test_0_gshared_delegate_rgctx () {
		Func<Type> t = new Func<Type> (get_type<string>);

		if (t () == typeof (string))
			return 0;
		else
			return 1;
	}

	// Creating a delegate from a generic method from gshared code
	public static int test_0_gshared_delegate_from_gshared () {
		if (gshared_delegate_from_gshared <object> () != 0)
			return 1;
		if (gshared_delegate_from_gshared <string> () != 0)
			return 2;
		return 0;
	}

	public static int gshared_delegate_from_gshared <T> () {
		Func<Type> t = new Func<Type> (get_type<T>);

		return t () == typeof (T) ? 0 : 1;
	}

	public static int test_0_marshalbyref_call_from_gshared_virt_elim () {
		/* Calling a virtual method from gshared code which is changed to a nonvirt call */
		Class1<object> o = new Class1<object> ();
		o.Do (new Class2<object> ());
		return 0;
	}

	class Pair<TKey, TValue> {
		public static KeyValuePair<TKey, TValue> make_pair (TKey key, TValue value)
			{
				return new KeyValuePair<TKey, TValue> (key, value);
			}

		public delegate TRet Transform<TRet> (TKey key, TValue value);
	}

	public static int test_0_bug_620864 () {
		var d = new Pair<string, Type>.Transform<KeyValuePair<string, Type>> (Pair<string, Type>.make_pair);

		var p = d ("FOO", typeof (int));
		if (p.Key != "FOO" || p.Value != typeof (int))
			return 1;

		return 0;
	}


	struct RecStruct<T> {
		public void foo (RecStruct<RecStruct<T>> baz) {
		}
	}

	public static int test_0_infinite_generic_recursion () {
		// Check that the AOT compile can deal with infinite generic recursion through
		// parameter types
		RecStruct<int> bla;

		return 0;
	}

	struct FooStruct {
	}

	bool IsNull2 <T> (object value) where T : struct {
		T? item = (T?) value;

		if (item.HasValue)
			return false;

		return true;
	}

	public static int test_0_full_aot_nullable_unbox_from_gshared_code () {
		if (!new GenericsTests ().IsNull2<FooStruct> (null))
			return 1;
		if (new GenericsTests ().IsNull2<FooStruct> (new FooStruct ()))
			return 2;
		return 0;
	}

	public static int test_0_partial_sharing () {
		if (PartialShared1 (new List<string> (), 1) != typeof (string))
			return 1;
		if (PartialShared1 (new List<GenericsTests> (), 1) != typeof (GenericsTests))
			return 2;
		if (PartialShared2 (new List<string> (), 1) != typeof (int))
			return 3;
		if (PartialShared2 (new List<GenericsTests> (), 1) != typeof (int))
			return 4;
		return 0;
	}

	[Category ("GSHAREDVT")]
	public static int test_6_partial_sharing_linq () {
		var messages = new List<Message> ();

		messages.Add (new Message () { MessageID = 5 });
		messages.Add (new Message () { MessageID = 6 });

		return messages.Max(i => i.MessageID);
	}

	public static int test_0_partial_shared_method_in_nonshared_class () {
		var c = new Class1<double> ();
		return (c.Foo<string> (5).GetType () == typeof (Class1<string>)) ? 0 : 1;
	}

	class Message {
		public int MessageID {
			get; set;
		}
	}

	public static Type PartialShared1<T, K> (List<T> list, K k) {
		return typeof (T);
	}

	public static Type PartialShared2<T, K> (List<T> list, K k) {
		return typeof (K);
	}

    public class Class1<T> {
		public virtual void Do (Class2<T> t) {
			t.Foo ();
		}

		public virtual object Foo<U> (T t) {
			return new Class1<U> ();
		}
	}

	public interface IFace1<T> {
		void Foo ();
	}

	public class Class2<T> : MarshalByRefObject, IFace1<T> {
		public void Foo () {
		}
	}



	public static void VirtualInterfaceCallFromGenericMethod <T> (IFoo f) {
		f.Bar <T> ();
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

	public interface IFoo {
		void NonGeneric ();
		object Bar<T>();
	}

	public class Foo<T1> : IFoo
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

		public void throw_dead_this () {
			try {
				new SomeClass().ThrowAnException();
			}
			catch {
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

		public static int count1, count2, count3, count4;

		public void NonGeneric () {
			count3 ++;
		}

		public object Bar <T> () {
			if (typeof (T) == typeof (int))
				count1 ++;
			else if (typeof (T) == typeof (string))
				count2 ++;
			else if (typeof (T) == typeof (object))
				count4 ++;
			return null;
		}
	}

	public class SomeClass {
		public void ThrowAnException() {
			throw new Exception ("Something went wrong");
		}
	}		

	struct Handler : IFoo {
		object o;

		public Handler(object o) {
			this.o = o;
		}

		public void NonGeneric () {
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

	interface IDefaultRetriever
	{
		T GetDefault<T>();
	}

	class DefaultRetriever : IDefaultRetriever
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		public T GetDefault<T>()
		{
			return default(T);
		}
	}

	[Category ("!FULLAOT")]
	[Category ("!BITCODE")]
	public static int test_0_regress_668095_synchronized_gshared () {
		return DoSomething (new DefaultRetriever ());
	}

    static int DoSomething(IDefaultRetriever foo) {
		int result = foo.GetDefault<int>();
		return result;
	}

	class SyncClass<T> {
		[MethodImpl(MethodImplOptions.Synchronized)]
		public Type getInstance() {
			return typeof (T);
		}
	}

	[Category ("GSHAREDVT")]
	static int test_0_synchronized_gshared () {
		var c = new SyncClass<string> ();
		if (c.getInstance () != typeof (string))
			return 1;
		return 0;
	}

	class Response {
	}

	public static int test_0_687865_isinst_with_cache_wrapper () {
		object o = new object ();
		if (o is Action<IEnumerable<Response>>)
			return 1;
		else
			return 0;
	}

	enum DocType {
		One,
		Two,
		Three
	}

	class Doc {
		public string Name {
			get; set;
		}

		public DocType Type {
			get; set;
		}
	}

	// #2155
	[Category ("GSHAREDVT")]
	public static int test_0_fullaot_sflda_cctor () {
		List<Doc> documents = new List<Doc>();
		documents.Add(new Doc { Name = "Doc1", Type = DocType.One } );
		documents.Add(new Doc { Name = "Doc2", Type = DocType.Two } );
		documents.Add(new Doc { Name = "Doc3", Type = DocType.Three } );
		documents.Add(new Doc { Name = "Doc4", Type = DocType.One } );
		documents.Add(new Doc { Name = "Doc5", Type = DocType.Two } );
		documents.Add(new Doc { Name = "Doc6", Type = DocType.Three } );
		documents.Add(new Doc { Name = "Doc7", Type = DocType.One } );
		documents.Add(new Doc { Name = "Doc8", Type = DocType.Two } );
		documents.Add(new Doc { Name = "Doc9", Type = DocType.Three } );

		List<DocType> categories = documents.Select(d=>d.Type).Distinct().ToList<DocType>().OrderBy(d => d).ToList();
		foreach(DocType cat in categories) {
			List<Doc> catDocs = documents.Where(d => d.Type == cat).OrderBy(d => d.Name).ToList<Doc>();
		}
		return 0;
	}

	class A { }

    static List<A> sources = new List<A>();

	// #6112
    public static int test_0_fullaot_imt () {
        sources.Add(null);
        sources.Add(null);

        int a = sources.Count;
        var enumerator = sources.GetEnumerator() as IEnumerator<object>;

        while (enumerator.MoveNext())
        {
            object o = enumerator.Current;
        }

		return 0;
	}

	class AClass {
	}

	class BClass : AClass {
	}

	public static int test_0_fullaot_variant_iface () {
		var arr = new BClass [10];
		var enumerable = (IEnumerable<AClass>)arr;
		enumerable.GetEnumerator ();
		return 0;
	}

	struct Record : Foo2<Record>.IRecord {
		int counter;
		int Foo2<Record>.IRecord.DoSomething () {
			return counter++;
		}
	}

	class Foo2<T> where T : Foo2<T>.IRecord {
		public interface IRecord {
			int DoSomething ();
		}

		public static int Extract (T[] t) {
			return t[0].DoSomething ();
		}
	}

	class Foo3<T> where T : IComparable {
		public static int CompareTo (T[] t) {
			// This is a constrained call to Enum.CompareTo ()
			return t[0].CompareTo (t [0]);
		}
	}

	public static int test_1_regress_constrained_iface_call_7571 () {
        var r = new Record [10];
        Foo2<Record>.Extract (r);
		return Foo2<Record>.Extract (r);
	}

	enum ConstrainedEnum {
		Val = 1
	}

	public static int test_0_regress_constrained_iface_call_enum () {
		var r = new ConstrainedEnum [10];
		return Foo3<ConstrainedEnum>.CompareTo (r);
	}

	public interface IFoo2 {
		void MoveNext ();
	}

	public struct Foo2 : IFoo2 {
		public void MoveNext () {
		}
	}

	public static Action Dingus (ref Foo2 f) {
		return new Action (f.MoveNext);
	}

	public static int test_0_delegate_unbox_full_aot () {
		Foo2 foo = new Foo2 ();
		Dingus (ref foo) ();
		return 0;
	}

	public static int test_0_arrays_ireadonly () {
		int[] arr = new int [10];
		for (int i = 0; i < 10; ++i)
			arr [i] = i;
		IReadOnlyList<int> a = (IReadOnlyList<int>)(object)arr;
		if (a.Count != 10)
			return 1;
		if (a [0] != 0)
			return 2;
		if (a [1] != 1)
			return 3;
		return 0;
	}

	public static int test_0_volatile_read_write () {
		string foo = "ABC";
		Volatile.Write (ref foo, "DEF");
		return Volatile.Read (ref foo) == "DEF" ? 0 : 1;
	}

	// FIXME: Doesn't work with --regression as Interlocked.Add(ref long) is only implemented as an intrinsic
#if FALSE
	public static async Task<T> FooAsync<T> (int i, int j) {
		Task<int> t = new Task<int> (delegate () { Console.WriteLine ("HIT!"); return 0; });
		var response = await t;
		return default(T);
	}

	public static int test_0_fullaot_generic_async () {
		Task<string> t = FooAsync<string> (1, 2);
		t.RunSynchronously ();
		return 0;
	}
#endif

	public static int test_0_delegate_callvirt_fullaot () {
		Func<string> f = delegate () { return "A"; };
        var f2 = (Func<Func<string>, string>)Delegate.CreateDelegate (typeof
(Func<Func<string>, string>), null, f.GetType ().GetMethod ("Invoke"));

        var s = f2 (f);
		return s == "A" ? 0 : 1;
	}

    public interface ICovariant<out R>
    {
    }

    // Deleting the `out` modifier from this line stop the problem
    public interface IExtCovariant<out R> : ICovariant<R>
    {
    }

    public class Sample<R> : ICovariant<R>
    {
    }

    public interface IMyInterface
    {
    }

	public static int test_0_variant_cast_cache () {
		object covariant = new Sample<IMyInterface>();

		var foo = (ICovariant<IMyInterface>)(covariant);

		try {
			var extCovariant = (IExtCovariant<IMyInterface>)covariant;
			return 1;
		} catch {
			return 0;
		}
	}

	struct FooStruct2 {
		public int a1, a2, a3;
	}

	class MyClass<T> where T: struct {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public MyClass(int a1, int a2, int a3, int a4, int a5, int a6, Nullable<T> a) {
		}

		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static MyClass<T> foo () {
			Nullable<T> a = new Nullable<T> ();
			return new MyClass<T> (0, 0, 0, 0, 0, 0, a);
		}
	}

	public static int test_0_newobj_generic_context () {
		MyClass<FooStruct2>.foo ();
		return 0;
	}

	enum AnEnum {
		A,
		B
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static string constrained_tostring<T> (T t) {
		return t.ToString ();
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

	public static int test_0_constrained_partial_sharing () {
		string s;

		s = constrained_tostring<int> (5);
		if (s != "5")
			return 1;
		s = constrained_tostring<AnEnum> (AnEnum.B);
		if (s != "B")
			return 2;

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

	enum Enum1 {
		A,
		B
	}

	enum Enum2 {
		A,
		B
	}

	public static int test_0_partial_sharing_ginst () {
		var l1 = new List<KeyValuePair<int, Enum1>> ();
		l1.Add (new KeyValuePair<int, Enum1>(5, Enum1.A));
		if (l1 [0].Key != 5)
			return 1;
		if (l1 [0].Value != Enum1.A)
			return 2;
		var l2 = new List<KeyValuePair<int, Enum2>> ();
		l2.Add (new KeyValuePair<int, Enum2>(5, Enum2.B));
		if (l2 [0].Key != 5)
			return 3;
		if (l2 [0].Value != Enum2.B)
			return 4;
		return 0;
	}

	static object delegate_8_args_res;

	public static int test_0_delegate_8_args () {
		delegate_8_args_res = null;
		Action<string, string, string, string, string, string, string,
			string> test = (a, b, c, d, e, f, g, h) =>
            {
				delegate_8_args_res = h;
            };
		test("a", "b", "c", "d", "e", "f", "g", "h");
		return delegate_8_args_res == "h" ? 0 : 1;
	}

	static void throw_catch_t<T> () where T: Exception {
		try {
			throw new NotSupportedException ();
		} catch (T) {
		}
	}

	public static int test_0_gshared_catch_open_type () {
		throw_catch_t<NotSupportedException> ();
		return 0;
	}

	class ThrowClass<T> where T: Exception {
		public void throw_catch_t () {
			try {
				throw new NotSupportedException ();
			} catch (T) {
			}
		}
	}

	public static int test_0_gshared_catch_open_type_instance () {
		var c = new ThrowClass<NotSupportedException> ();
		c.throw_catch_t ();
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool is_ref_or_contains_refs<T> () {
		return RuntimeHelpers.IsReferenceOrContainsReferences<T> ();
	}

	class IsRefClass<T> {
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public bool is_ref () {
			return RuntimeHelpers.IsReferenceOrContainsReferences<T> ();
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool is_ref_or_contains_refs_gen_ref<T> () {
		return RuntimeHelpers.IsReferenceOrContainsReferences<GenStruct<T>> ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool is_ref_or_contains_refs_gen_noref<T> () {
		return RuntimeHelpers.IsReferenceOrContainsReferences<NoRefGenStruct<T>> ();
	}

	struct GenStruct<T> {
		T t;
	}

	struct NoRefGenStruct<T> {
	}

	struct RefStruct {
		string s;
	}

	struct NestedRefStruct {
		RefStruct r;
	}

	struct NoRefStruct {
		int i;
	}

	struct AStruct3<T1, T2, T3> {
		T1 t1;
		T2 t2;
		T3 t3;
	}

	public static int test_0_isreference_intrins () {
		if (RuntimeHelpers.IsReferenceOrContainsReferences<int> ())
			return 1;
		if (!RuntimeHelpers.IsReferenceOrContainsReferences<string> ())
			return 2;
		if (!RuntimeHelpers.IsReferenceOrContainsReferences<RefStruct> ())
			return 3;
		if (!RuntimeHelpers.IsReferenceOrContainsReferences<NestedRefStruct> ())
			return 4;
		if (RuntimeHelpers.IsReferenceOrContainsReferences<NoRefStruct> ())
			return 5;
		// Generic code
		if (is_ref_or_contains_refs<int> ())
			return 6;
		// Shared code
		if (!is_ref_or_contains_refs<string> ())
			return 7;
		// Complex type from shared code
		if (!is_ref_or_contains_refs_gen_ref<string> ())
			return 8;
		if (is_ref_or_contains_refs_gen_ref<int> ())
			return 9;
		if (is_ref_or_contains_refs_gen_noref<string> ())
			return 10;

		// Complex type from shared class method
		var c1 = new IsRefClass<AStruct3<int, int, int>> ();
		if (c1.is_ref ())
			return 11;
		var c2 = new IsRefClass<AStruct3<string, int, int>> ();
		if (!c2.is_ref ())
			return 12;

		return 0;
	}

	class LdobjStobj {
		public int counter;
		public LdobjStobj buffer1;
		public LdobjStobj buffer2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void swap<T>(ref T first, ref T second) {
		second = first;
	}

	public static int test_42_ldobj_stobj_ref () {
		var obj = new LdobjStobj ();
		obj.counter = 42;
		swap (ref obj.buffer1, ref obj.buffer2);
		return obj.counter;
	}

	public interface ICompletion {
		Type UnsafeOnCompleted ();
	}

	public struct TaskAwaiter<T> : ICompletion {
		public Type UnsafeOnCompleted () {
			typeof(T).GetHashCode ();
			return typeof(T);
		}
	}

	public struct AStruct {
        public Type Caller<TAwaiter>(ref TAwaiter awaiter)
            where TAwaiter : ICompletion {
			return awaiter.UnsafeOnCompleted();
		}
	}

    public static int test_0_partial_constrained_call_llvmonly () {
		var builder = new AStruct ();
		var awaiter = new TaskAwaiter<bool> ();
		var res = builder.Caller (ref awaiter);
		return res == typeof (bool) ? 0 : 1;
	}

	struct OneThing<T1> {
		public T1 Item1;
	}

	[MethodImpl (MethodImplOptions.NoInlining)]
	static T FromResult<T> (T result) {
		return result;
	}

	public static int test_42_llvm_gsharedvt_small_vtype_in_regs () {
		var t = FromResult<OneThing<int>>(new OneThing<int> {Item1 = 42});
		return t.Item1;
	}

	class ThreadLocalClass<T> {
		[ThreadStatic]
		static T v;

		public T Value {
			[MethodImpl (MethodImplOptions.NoInlining)]
			get {
				return v;
			}
			[MethodImpl (MethodImplOptions.NoInlining)]
			set {
				v = value;
			}
		}
	}

	public static int test_0_tls_gshared () {
		var c = new ThreadLocalClass<string> ();
		c.Value = "FOO";
		return c.Value == "FOO" ? 0 : 1;
	}
}

#if !__MOBILE__
class GenericsTests : Tests
{
}
#endif
