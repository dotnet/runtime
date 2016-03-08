using System;
using System.Threading;

public class Program {
	const int num_threads = 10;
	static int aborted = 0;
	static Barrier barrier = new Barrier (num_threads + 1);

	public static void ThreadFunc (object lock_obj)
	{
		try {
			barrier.SignalAndWait ();
			Monitor.Enter (lock_obj);
			Monitor.Exit (lock_obj);
		} catch (ThreadAbortException) {
			Interlocked.Increment (ref aborted);
			Thread.ResetAbort ();
		}
	}

	public static int Main (string[] args)
	{
		Thread[] tarray = new Thread [num_threads];

		for (int i = 0; i < num_threads; i++) {
			object lock_obj = new object ();
			Monitor.Enter (lock_obj);
			tarray [i] = new Thread (new ParameterizedThreadStart (ThreadFunc));
			tarray [i].Start (lock_obj);
		}

		barrier.SignalAndWait ();

		for (int i = 0; i < num_threads; i++)
			tarray [i].Abort ();

		for (int i = 0; i < num_threads; i++)
			tarray [i].Join ();

		Console.WriteLine ("Aborted {0}", aborted);
		if (aborted != num_threads)
			return -1;
		return 0;
	}
}
