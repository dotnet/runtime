using System.Reflection;
using System;

public delegate void TestDelegate ();

public class Bla {
	public static void test<T> () {}
}

public class main {
	public static int Main () {
		TestDelegate del = new TestDelegate (Bla.test<object>);
		MethodInfo minfo = del.Method;
		Type[] args = minfo.GetGenericArguments ();

		if (args.Length == 1)
			return 0;
		return 1;
	}
}
