using System;
using System.Threading;

public class Tests {

	public static void Test1 ()
	{
		bool called_finally = false;
		bool failed_abort = false;
		bool finished = false;

		Thread thr = new Thread (() => {
			try {
				try {
					Thread.CurrentThread.Abort ();
				} finally {
					called_finally = true;
					Thread.CurrentThread.Abort ();
					failed_abort = true;
				}
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			}
			finished = true;
		});

		thr.Start ();
		thr.Join ();

		if (!called_finally)
			Environment.Exit (1);
		if (failed_abort)
			Environment.Exit (2);
		if (!finished)
			Environment.Exit (3);
	}

	public static void Main ()
	{
		Test1 ();
		Console.WriteLine ("done, all things good");
	}
}
