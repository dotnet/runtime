using System;
using System.Runtime.InteropServices;

class A {
	public static bool b_cctor_run = false;
}

class B {
	static B () {
		A.b_cctor_run = true;
	}
	public static void method () {
	}
}

delegate void DoIt ();

namespace Bah {
class Test {
	[DllImport("cygwin1.dll", EntryPoint="puts", CharSet=CharSet.Ansi)]
	public static extern int puts (string name);

	delegate void SimpleDelegate ();
	delegate string NotSimpleDelegate (int a);
	delegate int AnotherDelegate (string s);
	
	public int data;
	
	static void F () {
		Console.WriteLine ("Test.F from delegate");
	}
	public static string G (int a) {
		if (a != 2)
			throw new Exception ("Something went wrong in G");
		return "G got: " + a.ToString ();
	}
	public string H (int a) {
		if (a != 3)
			throw new Exception ("Something went wrong in H");
		return "H got: " + a.ToString () + " and " + data.ToString ();
	}

	public virtual void VF () {
		Console.WriteLine ("Test.VF from delegate");
	}
	
	public Test () {
		data = 5;
	}
	static int Main () {
		// Check that creation of delegates do not runs the class cctor
		DoIt doit = new DoIt (B.method);
		if (A.b_cctor_run)
			return 1;

		Test test = new Test ();
		SimpleDelegate d = new SimpleDelegate (F);
		SimpleDelegate d1 = new SimpleDelegate (test.VF);
		NotSimpleDelegate d2 = new NotSimpleDelegate (G);
		NotSimpleDelegate d3 = new NotSimpleDelegate (test.H);
		d ();
		d1 ();
		// we run G() and H() before and after using them as delegates
		// to be sure we don't corrupt them.
		G (2);
		test.H (3);
		Console.WriteLine (d2 (2));
		Console.WriteLine (d3 (3));
		G (2);
		test.H (3);

		if (d.Method.Name != "F")
			return 1;

		if (d3.Method == null)
			return 1;
		
		object [] args = {3};
		Console.WriteLine (d3.DynamicInvoke (args));

		AnotherDelegate d4 = new AnotherDelegate (puts);
		if (d4.Method == null)
			return 1;

		Console.WriteLine (d4.Method);
		Console.WriteLine (d4.Method.Name);
		Console.WriteLine (d4.Method.DeclaringType);
		
		return 0;


	}
}
}
