using System;
using System.Runtime.InteropServices;

class Tests {
	delegate void SimpleDelegate ();

	public static int v = 0;
	
	static void F1 () {
		v += 1;
		Console.WriteLine ("Test.F1");
	}
	static void F2 () {
		v += 2;
		Console.WriteLine ("Test.F2");
	}
	static void F3 () {
		v += 4;
		Console.WriteLine ("Test.F3");
	}

	public static int Main () {
		return TestDriver.RunTests (typeof (Tests));
	}

	static public int test_0_test () {
		SimpleDelegate t;
		SimpleDelegate d1 = new SimpleDelegate (F1);
		SimpleDelegate d2 = new SimpleDelegate (F2);
		SimpleDelegate d3 = new SimpleDelegate (F3);

		SimpleDelegate d12 = d1 + d2;
		SimpleDelegate d13 = d1 + d3;
		SimpleDelegate d23 = d2 + d3;
		SimpleDelegate d123 = d1 + d2 + d3;

		v = 0;
		t = d123 - d13;
		t ();
		if (v != 7)
			return 1;
		
		v = 0;
		t = d123 - d12;
		t ();
		if (v != 4)
			return 1;
		
		v = 0;
		t = d123 - d23;
		t ();
		if (v != 1)
			return 1;
		
		
		return 0;
	}

	// Regression test for bug #50366
	static public int test_0_delegate_equality () {
		if (new SimpleDelegate (F1) == new SimpleDelegate (F1))
			return 0;
		else
			return 1;
	}
}
