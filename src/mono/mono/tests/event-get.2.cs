using System;
using System.Reflection;

class Test {

	static int Main () {
		Assembly ass = Assembly.LoadFrom ("event-il.exe");
		Type t = ass.GetType ("T");
		EventInfo ev = t.GetEvent ("MyDo");
		Console.WriteLine (ev.GetAddMethod ());
		MethodInfo[] others = ev.GetOtherMethods ();
		for (int i = 0; i < others.Length; ++i) {
			Console.WriteLine (others [i]);
		}
		if (others.Length != 1)
			return 1;
		Console.WriteLine ("now with non-public, too:");
		others = ev.GetOtherMethods (true);
		for (int i = 0; i < others.Length; ++i) {
			Console.WriteLine (others [i]);
		}
		if (others.Length != 2)
			return 2;
		return 0;
	}
}

