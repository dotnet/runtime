using System;
using System.Threading;
using System.Collections;

class T {
	/* each thread will create n domains */
	static int threads = 5;
	static int domains = 100;
	static int allocs = 1000;
	static int loops = 1;
	static int errors = 0;

	public static void worker () {
		Console.WriteLine ("Domain start " + AppDomain.CurrentDomain.FriendlyName + " " + Thread.CurrentThread.GetHashCode ());
		ArrayList list = new ArrayList ();
		for (int i = 0; i < allocs; ++i) {
			list.Add (new object ());
			list.Add (new ArrayList ());
			list.Add (new String ('x', 34));
			int[] a = new int [5];
			list.Add (new WeakReference (a));
			if ((i % 1024) == 0) {
				list.RemoveRange (0, list.Count / 2);
			}
		}
		Console.WriteLine ("Domain end " + AppDomain.CurrentDomain.FriendlyName + " " + Thread.CurrentThread.GetHashCode ());
	}

	static void thread_start () {
		Console.WriteLine ("Thread start " + Thread.CurrentThread.GetHashCode ());
		for (int i = 0; i < domains; ++i) {
			AppDomain appDomain = AppDomain.CreateDomain("Test-" + i);
			appDomain.DoCallBack (new CrossAppDomainDelegate (worker));
			try {
				AppDomain.Unload (appDomain);
			} catch {
				Interlocked.Increment (ref errors);
				Console.WriteLine ("Error unloading " + "Test-" + i);
			}
		}
		Console.WriteLine ("Thread end " + Thread.CurrentThread.GetHashCode ());
	}
	static int Main (string[] args) {
		if (args.Length > 0)
			threads = int.Parse (args [0]);
		if (args.Length > 1)
			domains = int.Parse (args [1]);
		if (args.Length > 2)
			allocs = int.Parse (args [2]);
		if (args.Length > 3)
			loops = int.Parse (args [3]);
		for (int j = 0; j < loops; ++j) {
			Thread[] ta = new Thread [threads];
			for (int i = 0; i < threads; ++i) {
				Thread t = new Thread (new ThreadStart (thread_start));
				ta [i] = t;
				t.Start ();
			}
			for (int i = 0; i < threads; ++i) {
				ta [i].Join ();
			}
		}
		//thread_start ();
		//Console.ReadLine ();
		return 0;
	}
}

