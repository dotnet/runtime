//
// This is:
//
// http://bugzilla.ximian.com/show_bug.cgi?id=72740
//

using System;
using System.Threading;

class T {
	static int loops = 20;
	static int threads = 100;

	static void worker () {
		// This hits alot of runtime code.
		while (true)
			typeof (object).Assembly.GetTypes ();
	}

	static void doit () {
		Thread[] ta = new Thread [threads];
		for (int i = 0; i < threads; ++i) {
			ta [i] = new Thread (new ThreadStart (worker));
			ta [i].Start ();
		}
		for (int i = 0; i < threads; ++i) {
			ta [i].Abort ();
		}
	}
	static void Main (string[] args) {
		if (args.Length > 0)
			loops = int.Parse (args [0]);
		if (args.Length > 1)
			threads = int.Parse (args [1]);
		for (int i = 0; i < loops; ++i) {
			Console.Write ('.');
			doit ();
		}
	}
}

