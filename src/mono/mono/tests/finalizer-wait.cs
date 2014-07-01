using System;
using System.Threading;
using System.Runtime.ConstrainedExecution;

class P {

	static public int count = 0;
	~P () {
		// Console.WriteLine ("finalizer done");
		count++;
	}
}

class T {
	static int Main () {
		for (int i = 0; i < 1000; ++i) {
			var t = new Thread (() => {
					P p = new P ();
				});
			t.Start ();
			t.Join ();

			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			if (P.count != i + 1)
				return 1;
		}
		return 0;
	}
}
