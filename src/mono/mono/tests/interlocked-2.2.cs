using System;
using System.Threading;

public class InterlockTest
{
	public int test;
	public int ltest;

	static int s_test;
	
	public static int Main() {
		int a,b;
		long la, lb;

		InterlockTest it = new InterlockTest ();

		/* int */
		it.test = 2;
		int c = Interlocked.Add (ref it.test, 1);
		if (c != 3)
			return -1;

		if (it.test != 3)
			return -2;

		a = 1;
		b = Interlocked.Add (ref a, 1);
		if (a != 2)
			return -3;
		if (b != 2)
			return -4;

		/* long */
		it.ltest = 2;
		int lc = Interlocked.Add (ref it.ltest, 1);
		if (lc != 3)
			return -5;

		if (it.ltest != 3)
			return -6;

		la = 1;
		lb = Interlocked.Add (ref la, 1);
		if (la != 2)
			return -7;
		if (lb != 2)
			return -8;

		Console.WriteLine ("done!");

		return 0;
	}
}
