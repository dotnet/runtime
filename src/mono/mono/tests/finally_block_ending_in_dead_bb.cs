using System;
using System.Threading;

class Driver {
	static volatile bool foo = false;
	static int res = 1;

	static void Stuff () {
		res = 2;
		try {
			res = 3;
		} finally {
			res = 4;
			while (!foo); 
			Thread.ResetAbort ();
			res = 0;
		}
	}
	
	static int Main () {
		Thread t = new Thread (Stuff);
		t.Start ();
		Thread.Sleep (100);
		t.Abort ();
		foo = true;
		t.Join ();
		Thread.Sleep (500);
		if (res != 0)
			Console.WriteLine ("Could not abort thread final state {0}", res);
		return res;
	}
}