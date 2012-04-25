using System;
using System.Threading;

public class FinalizerException {
	~FinalizerException () {
		throw new Exception ();
	}

	public static int Main () { 
		AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
			Console.WriteLine ("caught");
			Environment.Exit (0);
		};

		new FinalizerException ();

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		Thread.Sleep (Timeout.Infinite); // infinite wait so we don't race against the unhandled exception callback

		return 2;
	}
}
