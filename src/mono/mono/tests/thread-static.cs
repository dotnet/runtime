using System;
using System.Threading;

class T {
	[ThreadStatic]
	static int var = 5;

	static bool tfailed = true;

	static void thread () {
		tfailed = false;
		if (var != 0)
			tfailed = true;
		Console.WriteLine ("start value: {0}", var);
		for (int i = 0; i < 10; ++i) {
			var += 10;
			Thread.Sleep (5);
		}
		Console.WriteLine ("end value: {0}", var);
		if (var != 100)
			tfailed = true;
	}
	
	static int Main () {
		bool failed = false;
		var  = 10;
		Thread thr = new Thread (new ThreadStart (thread));
		thr.Start ();
		if (var != 10)
			failed = true;
		var = 20;
		Console.WriteLine ("value in main thread: {0}", var);
		thr.Join ();
		Console.WriteLine ("value in main thread after join: {0}", var);
		if (var != 20)
			failed = true;

		if (failed)
			return 1;
		if (tfailed)
			return 2;
		return 0;
	}
}
