/*
 * dtest-app.cs:
 *
 *   Application program used by the debugger tests.
 */
using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

public class TestsBase
{
#pragma warning disable 0414
#pragma warning disable 0169
	public int base_field_i;
	public string base_field_s;
	static int base_static_i = 57;
	static string base_static_s = "C";
#pragma warning restore 0414
#pragma warning restore 0169
}

public enum AnEnum {
	A = 0,
	B= 1
}

[DebuggerDisplay ("Tests", Name="FOO", Target=typeof (int))]
[DebuggerTypeProxy (typeof (Tests))]
public class Tests2 {
	[DebuggerBrowsableAttribute (DebuggerBrowsableState.Collapsed)]
	public int field_j;
	public static int static_field_j;

	[DebuggerBrowsableAttribute (DebuggerBrowsableState.Collapsed)]
	public int AProperty {
		get {
			return 0;
		}
	}

	public void invoke () {
	}
}

public struct AStruct {
	public int i;
	public string s;
	public byte k;
	public IntPtr j;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public int foo (int val) {
		return val;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int static_foo (int val) {
		return val;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public int invoke_return_int () {
		return i;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int invoke_static () {
		return 5;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public IntPtr invoke_return_intptr () {
		return j;
	}
}

public class GClass<T> {
	public T field;
	public static T static_field;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public GClass () {
	}
}

public struct GStruct<T> {
	public T i;

	public int j;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public int invoke_return_int () {
		return j;
	}
}

public class Tests : TestsBase
{
#pragma warning disable 0414
	int field_i;
	string field_s;
	AnEnum field_enum;
	bool field_bool1, field_bool2;
	char field_char;
	byte field_byte;
	sbyte field_sbyte;
	short field_short;
	ushort field_ushort;
	long field_long;
	ulong field_ulong;
	float field_float;
	double field_double;
	Thread field_class;
	IntPtr field_intptr;
	static int static_i = 55;
	static string static_s = "A";
	public const int literal_i = 56;
	public const string literal_s = "B";
	public object child;
	public AStruct field_struct;
	public object field_boxed_struct;
	public GStruct<int> generic_field_struct;
	[ThreadStatic]
	public static int tls_i;
	public static bool is_attached = Debugger.IsAttached;

#pragma warning restore 0414

	public class NestedClass {
	}

	public int IntProperty {
		get {
			return field_i;
		}
		set {
			field_i = value;
		}
	}

	public int ReadOnlyProperty {
		get {
			return field_i;
		}
	}

	public int this [int index] {
		get {
			return field_i;
		}
	}

	public static int Main (String[] args) {
		if (args.Length > 0 && args [0] == "suspend-test")
			/* This contains an infinite loop, so execute it conditionally */
			suspend ();
		breakpoints ();
		single_stepping ();
		arguments ();
		objects ();
		objrefs ();
		vtypes ();
		locals ();
		line_numbers ();
		type_info ();
		assembly_load ();
		invoke ();
		exceptions ();
		threads ();
		dynamic_methods ();
		if (args.Length > 0 && args [0] == "domain-test")
			/* This takes a lot of time, so execute it conditionally */
			domains ();
		if (args.Length > 0 && args [0] == "ref-emit-test")
			ref_emit ();
		if (args.Length > 0 && args [0] == "frames-in-native")
			frames_in_native ();
		if (args.Length >0 && args [0] == "invoke-single-threaded")
			new Tests ().invoke_single_threaded ();
		return 3;
	}

	public static void breakpoints () {
		/* Call these early so it is JITted by the time a breakpoint is placed on it */
		bp3 ();
		bp7<int> ();
		bp7<string> ();

		bp1 ();
		bp2 ();
		bp3 ();
		bp4 ();
		bp4 ();
		bp4 ();
		bp5 ();
		bp6<string> ();
		bp7<int> ();
		bp7<string> ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bp1 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bp2 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bp3 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bp4 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bp5 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bp6<T> () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void bp7<T> () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void single_stepping () {
		ss1 ();
		ss2 ();
		ss3 ();
		ss3_2 ();
		ss4 ();
		ss5 (new int [] { 1, 2, 3 }, new Func<int, bool> (is_even));
		try {
			ss6 (true);
		} catch {
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ss1 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ss2 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int ss3 () {
		int sum = 0;

		for (int i = 0; i < 10; ++i)
			sum += i;

		return sum;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ss3_2 () {
		ss3_2_2 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ss3_2_2 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int ss4 () {
		ss1 (); ss1 ();
		ss2 ();
		return 0;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ss5 (int[] arr, Func<int, bool> selector) {
		// Call into linq which calls back into this assembly
		arr.Count (selector);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ss6 (bool b) {
		if (b) {
			ss7 ();
			throw new Exception ();
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ss7 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static bool is_even (int i) {
		return i % 2 == 0;
	}

	/*
		lock (static_s) {
			Console.WriteLine ("HIT!");
		}
		return 0;
	}
	*/

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void arguments () {
		arg1 (SByte.MaxValue - 5, Byte.MaxValue - 5, true, Int16.MaxValue - 5, UInt16.MaxValue - 5, 'F', Int32.MaxValue - 5, UInt32.MaxValue - 5, Int64.MaxValue - 5, UInt64.MaxValue - 5, 1.2345f, 6.78910, new IntPtr (Int32.MaxValue - 5), new UIntPtr (UInt32.MaxValue - 5));
		int i = 42;
		arg2 ("FOO", null, "BLA", ref i, new GClass <int> { field = 42 }, new object ());
		Tests t = new Tests () { field_i = 42, field_s = "S" };
		t.arg3 ("BLA");
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static int arg1 (sbyte sb, byte b, bool bl, short s, ushort us, char c, int i, uint ui, long l, ulong ul, float f, double d, IntPtr ip, UIntPtr uip) {
		return (int)(sb + b + (bl ? 0 : 1) + s + us + (int)c + i + ui + l + (long)ul + f + d + (int)ip + (int)uip);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static string arg2 (string s, string s3, object o, ref int i, GClass <int> gc, object o2) {
		return s + (s3 != null ? "" : "") + o + i + gc.field + o2;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public object arg3 (string s) {
		return s + s + s + s + this;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void objects () {
		Tests t = new Tests () { field_i = 42, field_bool1 = true, field_bool2 = false, field_char = 'A', field_byte = 129, field_sbyte = -33, field_short = Int16.MaxValue - 5, field_ushort = UInt16.MaxValue - 5, field_long = Int64.MaxValue - 5, field_ulong = UInt64.MaxValue - 5, field_float = 3.14f, field_double = 3.14f, field_s = "S", base_field_i = 43, base_field_s = "T", field_enum = AnEnum.B, field_class = null, field_intptr = new IntPtr (Int32.MaxValue - 5) };
		t.o1 (new Tests2 () { field_j = 43 }, new GClass <int> { field = 42 }, new GClass <string> { field = "FOO" });
		o2 (new string [] { "BAR", "BAZ" }, new int[] { 42, 43 }, new int [,] { { 1, 2 }, { 3, 4 }}, (int[,])Array.CreateInstance (typeof (int), new int [] { 2, 2}, new int [] { 1, 3}), new int[] { 0 });
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public object o1 (Tests2 t, GClass <int> gc1, GClass <string> gc2) {
		if (t == null || gc1 == null || gc2 == null)
			return null;
		else
			return this;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static string o2 (string[] s2, int[] s3, int[,] s4, int[,] s5, IList<int> s6) {
		return s2 [0] + s3 [0] + s4 [0, 0] + s6 [0];
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void objrefs () {
		Tests t = new Tests () {};
		set_child (t);
		t.objrefs1 ();
		t.child = null;
		GC.Collect ();
		objrefs2 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void set_child (Tests t) {
		t.child = new Tests ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public void objrefs1 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void objrefs2 () {
	}

	public static void vtypes () {
		Tests t = new Tests () { field_struct = new AStruct () { i = 42, s = "S", k = 43 }, generic_field_struct = new GStruct<int> () { i = 42 }, field_boxed_struct = new AStruct () { i = 42 }};
		AStruct s = new AStruct { i = 44, s = "T", k = 45 };
		AStruct[] arr = new AStruct[] { 
			new AStruct () { i = 1, s = "S1" },
			new AStruct () { i = 2, s = "S2" } };
		t.vtypes1 (s, arr);
		vtypes2 (s);
		vtypes3 (s);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public object vtypes1 (AStruct s, AStruct[] arr) {
		if (arr != null)
			return this;
		else
			return null;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void vtypes2 (AStruct s) {
		s.foo (5);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void vtypes3 (AStruct s) {
		AStruct.static_foo (5);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void locals () {
		locals1 (null);
		locals2 (null, 5);
		locals3 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void locals1 (string[] args) {
		long foo = 42;

		for (int j = 0; j < 10; ++j) {
			foo ++;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void locals2 (string[] args, int arg) {
		long i = 42;
		string s = "AB";

		for (int j = 0; j < 10; ++j) {
			if (s != null)
				i ++;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void locals3 () {
		string s = "B";
		s.ToString ();

		{
			long i = 42;
			i ++;
			locals4 ();
		}
		{
			string i = "A";
			i.ToString ();
			locals5 ();
		}
		{
			long j = 42;
			j ++;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void locals4 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void locals5 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void line_numbers () {
		LineNumbers.ln1 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void suspend () {
		long i = 5;

		while (true) {
			i ++;
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void type_info () {
		Tests t = new Tests () { field_i = 42, field_s = "S", base_field_i = 43, base_field_s = "T", field_enum = AnEnum.B };
		t.ti1 (new Tests2 () { field_j = 43 }, new GClass <int> { field = 42 }, new GClass <string> { field = "FOO" });
		int val = 0;
		unsafe {
			AStruct s = new AStruct () { i = 42, s = "S", k = 43 };

			ti2 (new string [] { "BAR", "BAZ" }, new int[] { 42, 43 }, new int [,] { { 1, 2 }, { 3, 4 }}, ref val, (int*)IntPtr.Zero, 5, s, new Tests (), new Tests2 (), new GClass <int> (), AnEnum.B);
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public object ti1 (Tests2 t, GClass <int> gc1, GClass <string> gc2) {
		if (t == null || gc1 == null || gc2 == null)
			return null;
		else
			return this;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static unsafe string ti2 (string[] s2, int[] s3, int[,] s4, ref int ri, int* ptr, int i, AStruct s, Tests t, Tests2 t2, GClass<int> g, AnEnum ae) {
		return s2 [0] + s3 [0] + s4 [0, 0];
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void assembly_load () {
		assembly_load_2 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void assembly_load_2 () {
		// This will load System.dll while holding the loader lock
		new Foo ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void invoke () {
		new Tests ().invoke1 (new Tests2 (), new AStruct () { i = 42, j = (IntPtr)43 }, new GStruct<int> { j = 42 });
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public void invoke1 (Tests2 t, AStruct s, GStruct<int> g) {
		invoke2 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public void invoke2 () {
	}

	int counter;

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public void invoke_single_threaded () {
		// Spawn a thread incrementing a counter
		bool finished = false;

		new Thread (delegate () {
				while (!finished)
					counter ++;
		}).Start ();

		Thread.Sleep (100);

		invoke_single_threaded_2 ();

		finished = true;
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public void invoke_single_threaded_2 () {
	}

	public void invoke_return_void () {
	}

	public string invoke_return_ref () {
		return "ABC";
	}

	public object invoke_return_null () {
		return null;
	}

	public int invoke_return_primitive () {
		return 42;
	}

	public void invoke_type_load () {
		new Class3 ();
	}

	class Class3 {
	}

	public long invoke_pass_primitive (byte ub, sbyte sb, short ss, ushort us, int i, uint ui, long l, ulong ul, char c, bool b, float f, double d) {
		return ub + sb + ss + us + i + ui + l + (long)ul + (int)c + (b ? 1 : 0) + (int)f + (int)d;
	}

	public int invoke_pass_primitive2 (bool b) {
		return b ? 1 : 0;
	}

	public string invoke_pass_ref (string s) {
		return s;
	}

	public static string invoke_static_pass_ref (string s) {
		return s;
	}

	public static void invoke_static_return_void () {
	}

	public static void invoke_throws () {
		throw new Exception ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void exceptions () {
		try {
			throw new OverflowException ();
		} catch (Exception) {
		}
		try {
			throw new OverflowException ();
		} catch (Exception) {
		}
		try {
			throw new ArgumentException ();
		} catch (Exception) {
		}
		try {
			throw new OverflowException ();
		} catch (Exception) {
		}

		object o = null;
		try {
			o.GetType ();
		} catch (Exception) {
		}
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void threads () {
		Thread t = new Thread (delegate () {});

		t.Start ();
		t.Join ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void domains () {
		AppDomain domain = AppDomain.CreateDomain ("domain");

		CrossDomain o = (CrossDomain)domain.CreateInstanceAndUnwrap (
				   typeof (CrossDomain).Assembly.FullName, "CrossDomain");

		o.invoke_2 ();

		o.invoke ();

		o.invoke_2 ();

		AppDomain.Unload (domain);

		domains_2 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void domains_2 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void invoke_in_domain () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void invoke_in_domain_2 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void dynamic_methods () {
		var m = new DynamicMethod ("dyn_method", typeof (void), new Type []  { typeof (int) }, typeof (object).Module);
		var ig = m.GetILGenerator ();

		ig.Emit (OpCodes.Ldstr, "FOO");
		ig.Emit (OpCodes.Call, typeof (Tests).GetMethod ("dyn_call"));
		ig.Emit (OpCodes.Ret);

		var del = (Action<int>)m.CreateDelegate (typeof (Action<int>));

		del (0);
	}

	public static void dyn_call (string s) {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ref_emit () {
		AssemblyName assemblyName = new AssemblyName ();
		assemblyName.Name = "foo";

		AssemblyBuilder assembly =
			Thread.GetDomain ().DefineDynamicAssembly (
													   assemblyName, AssemblyBuilderAccess.RunAndSave);

		ModuleBuilder module = assembly.DefineDynamicModule ("foo.dll");

		TypeBuilder tb = module.DefineType ("foo", TypeAttributes.Public, typeof (object));
		MethodBuilder mb = tb.DefineMethod ("ref_emit_method", MethodAttributes.Public|MethodAttributes.Static, CallingConventions.Standard, typeof (void), new Type [] { });
		ILGenerator ig = mb.GetILGenerator ();
		ig.Emit (OpCodes.Ldstr, "FOO");
		ig.Emit (OpCodes.Call, typeof (Tests).GetMethod ("ref_emit_call"));
		ig.Emit (OpCodes.Ret);

		Type t = tb.CreateType ();

		t.GetMethod ("ref_emit_method").Invoke (null, null);
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ref_emit_call (string s) {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void frames_in_native () {
		Thread.Sleep (500);
	}
}

public class CrossDomain : MarshalByRefObject
{
	public void invoke () {
		Tests.invoke_in_domain ();
	}

	public void invoke_2 () {
		Tests.invoke_in_domain_2 ();
	}
}	

public class Foo
{
	public ProcessStartInfo info;
}

// Class used for line number info testing, don't change its layout
public class LineNumbers
{
	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ln1 () {
		ln2 ();
		ln3 ();
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ln2 () {
	}

	[MethodImplAttribute (MethodImplOptions.NoInlining)]
	public static void ln3 () {
	}
}
