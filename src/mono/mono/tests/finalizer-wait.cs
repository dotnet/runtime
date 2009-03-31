using System;
using System.Threading;
using System.Runtime.ConstrainedExecution;

class P {

	static public int count = 0;
	~P () {
		T.finalized = true;
		Thread.Sleep (1000);
		//Console.WriteLine ("finalizer done");
		count++;
	}
}

class T {

	static public bool finalized = false;

	static void makeP () {
		P p = new P ();
		p = null;
	}

	static void callMakeP () {
		makeP ();
	}

	static int Main () {
		callMakeP ();

		GC.Collect ();
		while (!finalized) {
			Thread.Sleep (100);
		}
		GC.WaitForPendingFinalizers ();

		if (P.count == 0)
			return 1;
		return 0;
	}
}
