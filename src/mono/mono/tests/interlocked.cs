using System;
using System.Threading;

public class InterlockTest
{
	public int test;
	public int add;
	public int rem;

	static int s_test;
	
	public static int Main() {
		int a,b;

		InterlockTest it = new InterlockTest ();

		it.test = 0;
		int c = Interlocked.Exchange (ref it.test, 1);
		if (c != 0)
			return -1;

		if (it.test != 1)
			return -2;

		a = 1;
		b = Interlocked.Increment (ref a);
		if (a != 2)
			return -3;
		if (b != 2)
			return -4;

		a = 2;
		b = Interlocked.Decrement (ref a);
		if (b != 1)
			return -3;
		if (a != 1)
			return -4;

		string s = IncTest ();
		if (s != "A1")
			return -5;

		s = IncTest ();
		if (s != "A2")
			return -6;

		Console.WriteLine ("done!");

		return 0;
	}

	public static string IncTest () {
		return "A" + Interlocked.Increment (ref s_test);
	}
}
