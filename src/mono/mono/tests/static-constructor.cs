
using System;
using System.Reflection;

struct A {
	public A (int i) {
	}
}

struct B {
	public B (int i) {
	}
}

public class Tests {
    	static int last = 42;
    	static int burp;

	static Tests () {
		/* 
		 * This is really at test of the compiler: it should init
		 * last before getting here.
		*/
		if (last != 42)
			burp = 5;
		else
			burp = 4;
	}
	public static int Main() {
		if (last != 42)
			return 1;
		if (burp != 4)
			return 1;

		// Regression test for bug #59193 (shared runtime wrappers)
		ConstructorInfo con1 = typeof (A).GetConstructor (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type [1] { typeof (int) }, null);
		ConstructorInfo con2 = typeof (B).GetConstructor (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type [1] { typeof (int) }, null);

		con1.Invoke (null, new Object [] { 0 });
		con2.Invoke (null, new Object [] { 0 });

		return 0;
	}
}
