using System;
using System.Threading;

public class InterlockTest
{
	public int test;
	public long ltest;

	public static int Main() {
		int a,b;
		long la, lb;

		InterlockTest it = new InterlockTest ();

		/* int */
		it.test = 2;
		int c = Interlocked.Add (ref it.test, 1);
		if (c != 3)
			return 1;

		if (it.test != 3)
			return 2;

		a = 1;
		b = Interlocked.Add (ref a, 1);
		if (a != 2)
			return 3;
		if (b != 2)
			return 4;

		/* long */
		it.ltest = 2;
		long lc = Interlocked.Add (ref it.ltest, 1);
		if (lc != 3)
			return 5;

		if (it.ltest != 3)
			return 6;

		la = 1;
		lb = Interlocked.Add (ref la, 1);
		if (la != 2)
			return 7;
		if (lb != 2)
			return 8;

		if (Interlocked.Read (ref la) != 2)
			return 9;

		la = 1;
		lc = Interlocked.Exchange (ref la, 2);
		if (lc != 1)
			return 10;

		if (la != 2)
			return 11;

		/* Generics */
		InterlockTest o1 = new InterlockTest ();
		InterlockTest o2 = new InterlockTest ();
		InterlockTest o = o1;

		InterlockTest o3 = Interlocked.CompareExchange (ref o, o2, o2);
		if (o3 != o1)
			return 12;
		if (o != o1)
			return 13;

		InterlockTest o4 = Interlocked.CompareExchange (ref o, o2, o1);
		if (o4 != o1)
			return 14;
		if (o != o2)
			return 15;

		/* long increment/decrement */
		la = 0x12345678;
		lb = Interlocked.Increment (ref la);
		if (la != 0x12345679)
			return 16;
		if (lb != 0x12345679)
			return 16;
		lb = Interlocked.Decrement (ref la);
		if (la != 0x12345678)
			return 17;
		if (lb != 0x12345678)
			return 18;		

		la = 1;
		lb = Interlocked.CompareExchange (ref la, 2, 1);
		if (la != 2)
			return 19;
		if (lb != 1)
			return 20;

		Console.WriteLine ("done!");

		return 0;
	}
}
