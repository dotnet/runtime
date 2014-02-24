using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

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

	static void SetupLinks () {
		var a = new Toggleref () { id = "root" };
		var b = new Toggleref () { id = "child" };
		a.link.Add (b);
		a.__test = Toggleref.STRONG;
		b.__test = Toggleref.WEAK;
		Register (a);
		Register (b);
		root = new WeakReference<Toggleref> (a, false);
		child = new WeakReference<Toggleref> (b, false);
	}

	static Toggleref a, b;

	static int Main ()
	{
		
		var t = new Thread (SetupLinks);
		t.Start ();
		t.Join ();
		
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
}