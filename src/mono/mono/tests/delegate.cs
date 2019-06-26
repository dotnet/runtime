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
class Tests {
	[DllImport("cygwin1.dll", EntryPoint="puts", CharSet=CharSet.Ansi)]
	public static extern int puts (string name);

	delegate void SimpleDelegate ();
	delegate string NotSimpleDelegate (int a);
	delegate int AnotherDelegate (string s);

	delegate string StringDelegate (); 
	
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
	
	public Tests () {
		data = 5;
	}

	static int Main (String[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_tests () {
		// Check that creation of delegates do not runs the class cctor
		DoIt doit = new DoIt (B.method);
		if (A.b_cctor_run)
			return 1;

		Tests test = new Tests ();
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
			return 2;

		if (d3.Method == null)
			return 3;
		
		object [] args = {3};
		Console.WriteLine (d3.DynamicInvoke (args));

		AnotherDelegate d4 = new AnotherDelegate (puts);
		if (d4.Method == null)
			return 4;

		Console.WriteLine (d4.Method);
		Console.WriteLine (d4.Method.Name);
		Console.WriteLine (d4.Method.DeclaringType);
		
		return 0;
	}

	public static int test_0_unbox_this () {
		int x = 10;
		StringDelegate d5 = new StringDelegate (x.ToString);
		return d5 () == "10" ? 0 : 1;
	}

	delegate long LongDelegate (long l);

	static long long_delegate (long l) {
		return l + 1;
	}

	public static int test_56_long () {
		LongDelegate l = new LongDelegate (long_delegate);

		return (int)l (55);
	}

	delegate float FloatDelegate (float l);

	static float float_delegate (float l) {
		return l + 1;
	}

	public static int test_56_float () {
		FloatDelegate l = new FloatDelegate (float_delegate);

		return (int)l (55);
	}

	delegate double DoubleDelegate (double l);

	static double double_delegate (double l) {
		return l + 1;
	}

	public static int test_56_double () {
		DoubleDelegate l = new DoubleDelegate (double_delegate);

		return (int)l (55);
	}

	static int count = 0;

	public static void inc_count () {
		count ++;
	}

	public static int test_0_multicast () {
		SimpleDelegate d = new SimpleDelegate (inc_count);

		d += inc_count;

		d ();
		return count == 2 ? 0 : 1;
	}

	public delegate int Delegate0 ();

	public delegate int Delegate1 (int i);

	public delegate int Delegate2 (int i, int j);

	public int int_field;

	public int adder0 () {
		return int_field;
	}

	public static int adder0_static () {
		return 1;
	}

	public int adder1 (int i) {
		return int_field + i;
	}

	public static int adder1_static (int i) {
		return i;
	}

	public int adder2 (int i, int j) {
		return int_field + i + j;
	}

	public static int adder2_static (int i, int j) {
		return i + j;
	}

	public static int test_0_delegate_opt () {
		Tests d = new Tests ();
		d.int_field = 1;

		if (new Delegate0 (d.adder0) () != 1)
			return 1;

		if (new Delegate1 (d.adder1) (2) != 3)
			return 2;

		if (new Delegate2 (d.adder2) (2, 3) != 6)
			return 3;

		if (new Delegate0 (adder0_static) () != 1)
			return 4;

		if (new Delegate1 (adder1_static) (2) != 2)
			return 5;

		if (new Delegate2 (adder2_static) (2, 3) != 5)
			return 6;

		return 0;
	}
}
}
