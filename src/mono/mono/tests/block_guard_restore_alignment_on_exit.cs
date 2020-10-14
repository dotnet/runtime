using System;
using System.Threading;

class Driver {
	static volatile bool foo = false;
	static int res = 1;
	static void InnerFunc () {
		res = 2;
		try {
			res = 3;
		} finally {
			res = 4;
			Console.WriteLine ("EEE");
			while (!foo);
			res = 5;
			Console.WriteLine ("in the finally block");
			Thread.ResetAbort ();
			res = 6;
		}
		res = 7;
		throw new Exception ("lalala");
	}

	static void Func () {
		try {
			InnerFunc ();
		} catch (Exception e) {
			res = 0;
		}
	}

	static int Main () {
		Thread t = new Thread (Func);
		t.Start ();
		Thread.Sleep (100);
		t.Abort ();
		foo = true;
		Console.WriteLine ("What now?");
		t.Join ();
		Thread.Sleep (500);
		return res;
	}
}
