using System;
using System.Reflection;

class T {
	int v;
	int a;
	public T () {
		v = 1;
		// note: a not modified
	}
	static int Main () {
		Type t = typeof (T);
		T obj = new T ();
		MethodBase m1;
		Console.WriteLine ("after ctor a is {0}", obj.a);
		Console.WriteLine ("after ctor v is {0}", obj.v);
		obj.a = 2;
		obj.v = 5;
		Console.WriteLine ("a is {0}", obj.a);
		Console.WriteLine ("v is {0}", obj.v);

		m1 = t.GetConstructor (Type.EmptyTypes);
		m1.Invoke (obj, null);
		Console.WriteLine ("after reinit a is {0}", obj.a);
		Console.WriteLine ("after reinit v is {0}", obj.v);
		/* value not preserved */
		if (obj.a != 2)
			return 1;
		/* value not reinitialized */
		if (obj.v != 1)
			return 2;

		return 0;
	}
}

