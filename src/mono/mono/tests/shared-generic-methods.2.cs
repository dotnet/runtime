using System;
using System.Collections.Generic;
using System.Reflection;

namespace GenericSharingTest {

public class ClassA {}
public class ClassB {}
public class ClassC {}

public class GenA<T> {
	public int genericMethod<M> () {
		return 123;
	}

	public int genericMethodCaller () {
		return genericMethod<int> ();
	}
}

public class main {
	static bool haveError = false;

	public static void error (string message) {
		haveError = true;
		Console.WriteLine (message);
	}

	public static void typeCheck (String method, Object obj, Type t) {
		if (obj.GetType () != t)
			error ("object from " + method + " should have type " + t.ToString () + " but has type " + obj.GetType ().ToString ());
	}

	public static int Main ()
	{
		GenA<ClassA> ga = new GenA<ClassA> ();
		GenA<GenA<ClassB>> gaab = new GenA<GenA<ClassB>> ();

		if (ga.genericMethodCaller () != 123)
			error ("ga.genericMethodCaller");

		if (gaab.genericMethodCaller () != 123)
			error ("gaab.genericMethodCaller");

		if (haveError)
			return 1;
		return 0;
	}
}

}
