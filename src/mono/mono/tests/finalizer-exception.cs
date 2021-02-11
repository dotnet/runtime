using System;
using System.Threading;

public class FinalizerException {

	~FinalizerException () {
		throw new Exception ();
	}

	static IntPtr aptr;

	/*
	 * We allocate the exception object deep down the stack so
	 * that it doesn't get pinned.
	 */
	public static unsafe void MakeException (int depth) {
		// Avoid tail calls
		int* values = stackalloc int [20];
		aptr = new IntPtr (values);
		if (depth <= 0) {
			for (int i = 0; i < 10; i++)
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

		var t = new Thread (delegate () { MakeException (1024); });
		t.Start ();
		t.Join ();

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		Thread.Sleep (Timeout.Infinite); // infinite wait so we don't race against the unhandled exception callback

		return 2;
	}
}
