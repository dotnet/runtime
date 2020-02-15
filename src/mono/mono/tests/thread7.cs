//
// Subset of thread6.cs, just to watch for the hang at end.
// Note this test does not indicate success or failure well,
// it just runs quickly or slowly.
//
using System;
using System.Threading;

public class Tests {

	public static int Main() {
		return test_0_regress_4413();
	}

	public static int test_0_regress_4413 () {
		// Check that thread abort exceptions originating in another thread are not automatically rethrown
		object o = new object ();
		Thread t = null;
		bool waiting = false;
		Action a = delegate () {
			t = Thread.CurrentThread;
			while (true) {
				lock (o) {
					if (waiting) {
						Monitor.Pulse (o);
						break;
					}
				}

				Thread.Sleep (10);
			}
			while (true) {
				Thread.Sleep (1000);
			}
		};
		var ar = a.BeginInvoke (null, null);
		lock (o) {
			waiting = true;
			Monitor.Wait (o);
		}

		t.Abort ();

		try {
			try {
				a.EndInvoke (ar);
			} catch (ThreadAbortException) {
			}
		} catch (ThreadAbortException) {
			// This will fail
			Thread.ResetAbort ();
			return 1;
		}

		return 0;
	}
}
