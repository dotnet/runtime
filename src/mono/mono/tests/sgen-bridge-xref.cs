using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Bridge {
	public int __test;
	public string id;
	public List<object> link = new List<object> ();
	
	~Bridge () {
	}
}

class Driver {
	static WeakReference<Bridge> root, child;

	static void SetupLinks () {
		var a = new Bridge () { id = "bridge" };
		var b = new Bridge () { id = "child" };
		a.link.Add (b);
		a.__test = 1;
		b.__test = 0;
		root = new WeakReference<Bridge> (a, true);
		child = new WeakReference<Bridge> (b, true);
	}

	static int Main ()
	{
		var t = new Thread (SetupLinks);
		t.Start ();
		t.Join ();
		
		GC.Collect ();
		Bridge a, b;
		a = b = null;
		Console.WriteLine ("try get A {0}", root.TryGetTarget (out a));
		Console.WriteLine ("try get B {0}", child.TryGetTarget (out b));
		Console.WriteLine ("a is null {0}", a == null);
		Console.WriteLine ("b is null {0}", b == null);
		if (a == null || b == null)
			return 1;

		Console.WriteLine ("a test {0}", a.__test);
		Console.WriteLine ("b test {0}", b.__test);

		if (a.__test != 1 || b.__test != 3)
			return 2;

		return 0;
	}
}