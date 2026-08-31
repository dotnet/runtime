using System;
using System.Threading;

class T {

	static object mutex = new object ();
	static int count = 1000000;

	static void dolock ()
	{
		for (int i = 0; i < count; ++i) {
			lock (mutex) {
			}
		}
	}

	static void Main (string[] args) {
		Thread t = new Thread (dolock);
		t.Start ();
		dolock ();
		t.Join ();
	}
}

