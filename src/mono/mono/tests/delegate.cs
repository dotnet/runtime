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
		return "G got: " + a.ToString ();
	}
	public string H (int a) {
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
		Console.WriteLine (d2 (2));
		Console.WriteLine (d3 (3));
		return 0;
	}
}
}
