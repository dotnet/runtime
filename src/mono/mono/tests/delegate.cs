using System;
namespace Bah {
class Test {
	delegate void SimpleDelegate ();
	delegate string NotSimpleDelegate (int a);
	
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
	public Test () {
		data = 5;
	}
	static int Main () {
		Test test = new Test ();
		SimpleDelegate d = new SimpleDelegate (F);
		NotSimpleDelegate d2 = new NotSimpleDelegate (G);
		NotSimpleDelegate d3 = new NotSimpleDelegate (test.H);
		d ();
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
		
		return 0;
	}
}
}
