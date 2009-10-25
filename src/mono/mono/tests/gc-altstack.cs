using System;
using System.Threading;
using System.Collections;

class T {

	static bool finished = false;

	static void segv () {
		while (true) {
			if (finished)
				break;
			try {
				object o = null;
				o.ToString ();
			} catch (NullReferenceException) {
			}
		}
	}

	static void Main (string[] args) {
		/* Test for running a GC while executing a SIGSEGV handler on an altstack */
		Thread t = new Thread (delegate () { 
				segv ();
			});

		t.Start ();

		for (int i = 0; i < 100000; ++i) {
			new ArrayList ();
		}

		finished = true;

		t.Join ();
	}
}

