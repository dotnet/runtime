using System;
using System.Threading;

public class FinalizerException {
	~FinalizerException () {
		throw new Exception ();
	}

	/*
	 * We allocate the exception object deep down the stack so
	 * that it doesn't get pinned.
	 */
	public static unsafe void MakeException (int depth) {
		// Avoid tail calls
		int* values = stackalloc int [20];
		if (depth <= 0) {
			new FinalizerException ();
			return;
		}
		MakeException (depth - 1);
	}

	public static int Main () { 
		AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
			Console.WriteLine ("caught");
			Environment.Exit (0);
		};

		MakeException (1024);

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		Thread.Sleep (Timeout.Infinite); // infinite wait so we don't race against the unhandled exception callback

		return 2;
	}
}
