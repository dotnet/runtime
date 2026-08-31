using System;
using System.Threading;

class T {
	static int count = 20000;
	static int loops = 80;
	static int threads = 10;
	static object global_obj;
	static void stress_loop () {
		object obj = new object ();
		lock (obj) {
			object [] array = new object [count];
			for (int i = 0; i < count; ++i) {
				array [i] = new object ();
			}
			for (int i = 0; i < count; ++i) {
				lock (array [i]) {
					global_obj = new String ('x', 32);
					if ((i % 12) == 0) {
						array [i] = global_obj;
					}
				}
			}
			// again, after a GC
			GC.Collect ();
			for (int i = 0; i < count; ++i) {
				lock (array [i]) {
				}
			}
			// two times, with feeling
			for (int i = 0; i < count; ++i) {
				lock (array [i]) {
					for (int j = 0; i < count; ++i) {
						lock (array [j]) {
						}
					}
				}
			}
		}
	}

	static void worker () {
		for (int i = 0; i < loops; ++i)
			stress_loop ();
	}
	static void Main (string[] args) {
		if (args.Length > 0)
			loops = int.Parse (args [0]);
		if (args.Length > 1)
			count = int.Parse (args [1]);
		if (args.Length > 1)
			threads = int.Parse (args [2]);
		for (int i = 0; i < threads; ++i) {
			Thread t = new Thread (new ThreadStart (worker));
			t.Start ();
		}
		/* for good measure */
		worker ();
	}
}
