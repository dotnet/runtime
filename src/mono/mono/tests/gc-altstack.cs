using System;
using System.Threading;
using System.Linq;

public class Tests
{
	static bool finished = false;

	static void fault () {
		while (!finished) {
			object o = null;
			try {
				o.ToString ();
			} catch {
			}
		}
	}

	static void gc (int niter) {
		int i = 0;
		while (i < niter) {
			i ++;
			if (i % 100 == 0)
				Console.Write (".");
			GC.Collect ();
		}
		finished = true;
		Console.WriteLine ();
	}

	static void test (bool main, int niter) {
		finished = false;

		if (main) {
			var t = new Thread (delegate () {
					gc (niter);
				});
			t.Start ();

			fault ();
		} else {
			var t = new Thread (delegate () {
					fault ();
				});
			t.Start ();

			gc (niter);
		}
	}

	public static void Main (String[] args) {
		/* Test for running a GC while executing a SIGSEGV handler on an altstack */
		int niter;

		if (args.Length > 0)
			niter = Int32.Parse (args [0]);
		else
			niter = 1000;

		test (false, niter);
		test (true, niter);
	}
}

