using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using MonoTests.Helpers;

public class Toggleref {
	public int __test;
	public string id;
	public List<object> link = new List<object> ();
	public const int DROP = 0;
	public const int STRONG = 1;
	public const int WEAK = 2;
	
	~Toggleref () {

	}
}

[StructLayout (LayoutKind.Explicit)]
public struct Helper {
	[FieldOffset(0)]
	IntPtr ptr;
	[FieldOffset(0)]
	object obj;
	public static IntPtr ObjToPtr (object obj)
	{
		Helper h = default (Helper);
		h.obj = obj;
		return h.ptr;
	}
}

class Driver {
	static WeakReference<Toggleref> root, child;

	[DllImport ("__Internal", EntryPoint="mono_gc_toggleref_add")]
	static extern int mono_gc_toggleref_add (IntPtr ptr, bool strong_ref);

	static void Register (object obj)
	{
		mono_gc_toggleref_add (Helper.ObjToPtr (obj), true);
	}

	static Toggleref a, b;

	static void SetupLinks () {
		var r = new Toggleref () { id = "root" };
		var c = new Toggleref () { id = "child" };
		r.link.Add (c);
		r.__test = Toggleref.STRONG;
		c.__test = Toggleref.WEAK;
		Register (r);
		Register (c);
		root = new WeakReference<Toggleref> (r, false);
		child = new WeakReference<Toggleref> (c, false);
	}

	static int test_0_root_keeps_child ()
	{
		Console.WriteLine ("test_0_root_keeps_child");
		FinalizerHelpers.PerformNoPinAction (SetupLinks);
		
		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		Console.WriteLine ("try get A {0}", root.TryGetTarget (out a));
		Console.WriteLine ("try get B {0}", child.TryGetTarget (out b));
		Console.WriteLine ("a is null {0}", a == null);
		Console.WriteLine ("b is null {0}", b == null);
		if (a == null || b == null)
			return 1;
		Console.WriteLine ("a test {0}", a.__test);
		Console.WriteLine ("b test {0}", b.__test);

		//now we break the link and switch b to strong
		a.link.Clear ();
		b.__test = Toggleref.STRONG;
		a = b = null;

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		Console.WriteLine ("try get A {0}", root.TryGetTarget (out a));
		Console.WriteLine ("try get B {0}", child.TryGetTarget (out b));
		Console.WriteLine ("a is null {0}", a == null);
		Console.WriteLine ("b is null {0}", b == null);
		if (a == null || b == null)
			return 2;
		Console.WriteLine ("a test {0}", a.__test);
		Console.WriteLine ("b test {0}", b.__test);


		return 0;
	}

	static void SetupLinks2 () {
		var r = new Toggleref () { id = "root" };
		var c = new Toggleref () { id = "child" };

		r.__test = Toggleref.STRONG;
		c.__test = Toggleref.WEAK;
		Register (r);
		Register (c);
		root = new WeakReference<Toggleref> (r, false);
		child = new WeakReference<Toggleref> (c, false);
	}

	static int test_0_child_goes_away ()
	{
		Console.WriteLine ("test_0_child_goes_away");

		FinalizerHelpers.PerformNoPinAction (SetupLinks2);

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		Console.WriteLine ("try get A {0}", root.TryGetTarget (out a));
		Console.WriteLine ("try get B {0}", child.TryGetTarget (out b));
		Console.WriteLine ("a is null {0}", a == null);
		Console.WriteLine ("b is null {0}", b == null);
		if (a == null || b != null)
			return 1;
		Console.WriteLine ("a test {0}", a.__test);

		return 0;
	}

	static ConditionalWeakTable<Toggleref, object> cwt = new ConditionalWeakTable<Toggleref, object> ();
	static WeakReference<object> root_value, child_value;
	static object a_val, b_val;


	static void SetupLinks3 () {
		var r = new Toggleref () { id = "root" };
		var c = new Toggleref () { id = "child" };

		r.__test = Toggleref.STRONG;
		c.__test = Toggleref.WEAK;
		Register (r);
		Register (c);
		root = new WeakReference<Toggleref> (r, false);
		child = new WeakReference<Toggleref> (c, false);

		var root_val = new object ();
		var child_val = new object ();

		cwt.Add (r, root_val);
		cwt.Add (c, child_val);

		root_value = new WeakReference<object> (root_val, false);
		child_value = new WeakReference<object> (child_val, false);
	}

	static int test_0_CWT_keep_child_alive ()
	{
		Console.WriteLine ("test_0_CWT_keep_child_alive");

		FinalizerHelpers.PerformNoPinAction (SetupLinks3);

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		Console.WriteLine ("try get A {0}", root.TryGetTarget (out a));
		Console.WriteLine ("try get B {0}", child.TryGetTarget (out b));
		Console.WriteLine ("a is null {0}", a == null);
		Console.WriteLine ("b is null {0}", b == null);
		if (a == null || b != null)
			return 1;
		Console.WriteLine ("a test {0}", a.__test);

		Console.WriteLine ("try get a_val {0}", root_value.TryGetTarget (out a_val));
		Console.WriteLine ("try get v_val {0}", child_value.TryGetTarget (out b_val));

		//the strong toggleref must keep the CWT value to remains alive
		if (a_val == null)
			return 2;

		//the weak toggleref should allow the CWT value to go away
		if (b_val != null)
			return 3;

		object res_value = null;
		bool res = cwt.TryGetValue (a, out res_value);
		Console.WriteLine ("CWT result {0} -> {1}", res, res_value == a_val);

		//the strong val is not on the CWT
		if (!res)
			return 4;

		//for some reason the value is not the right one
		if (res_value != a_val)
			return 5;

		return 0;
	}

	static int Main (string[] args)
	{
		return TestDriver.RunTests (typeof (Driver), args);
	}

}
