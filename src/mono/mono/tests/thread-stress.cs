using System;
using System.Threading;

class T {
	static int loops = 20;
	static int threads = 100;

	static void worker () {
		/* a writeline happens to involve lots of code */
		Console.WriteLine ("Thread start " + Thread.CurrentThread.GetHashCode ());
	}

	static void doit () {
		Thread[] ta = new Thread [threads];
		for (int i = 0; i < threads; ++i) {
			ta [i] = new Thread (new ThreadStart (worker));
			ta [i].Start ();
		}
		for (int i = 0; i < threads; ++i) {
			ta [i].Join ();
		}
	}
	static void Main (string[] args) {
		if (args.Length > 0)
			loops = int.Parse (args [0]);
		if (args.Length > 1)
			threads = int.Parse (args [1]);
		for (int i = 0; i < loops; ++i) {
			doit ();
		}
	}
}

