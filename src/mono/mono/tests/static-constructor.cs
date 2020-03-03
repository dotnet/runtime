
using System;
using System.Reflection;
using System.Threading;

struct A {
	public A (int i) {
	}
}

struct B {
	public B (int i) {
	}
}

public class StaticInitFails {

	public static string s;

	static StaticInitFails () {
		Thread.Sleep (1000);
		throw new Exception ();
		s = "FOO";
	}

	public static void foo () {
		Console.WriteLine (s);
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

		con1.Invoke (new Object [] { 0 });
		con2.Invoke (new Object [] { 0 });

		// Test what happens when static initialization throws an exception
		// First start a thread which will trigger the initialization
		Thread t = new Thread (new ThreadStart (Run));
		t.Start ();

		Thread.Sleep (500);

		// While the thread waits, trigger initialization on this thread too so this
		// thread will wait for the other thread to finish initialization
		try {
			Run2 ();
			return 1;
		}
		catch (TypeInitializationException ex) {
		}

		// Try again synchronously
		try {
			Run2 ();
			return 1;
		}
		catch (TypeInitializationException ex) {
		}

		return 0;
	}

	private static void Run () {
		try {
			StaticInitFails.foo ();
		}
		catch (Exception) {
		}
	}

	private static void Run2 () {
		StaticInitFails.foo ();
	}

}
