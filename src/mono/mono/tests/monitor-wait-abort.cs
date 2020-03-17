using System;
using System.Threading;

public class Program {
	const int num_threads = 10;
	public static Barrier barrier = new Barrier (num_threads + 1);

	public static void ThreadFunc ()
	{
		object lock_obj = new object ();
		lock (lock_obj) {
			try {
				barrier.SignalAndWait ();
				Monitor.Wait (lock_obj);
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			}
		}
	}

	public static void Main (string[] args)
	{
		Thread[] tarray = new Thread [num_threads];

		for (int i = 0; i < num_threads; i++) {
			tarray [i] = new Thread (new ThreadStart (ThreadFunc));
			tarray [i].Start ();
		}

		barrier.SignalAndWait ();

		for (int i = 0; i < num_threads; i++)
			tarray [i].Abort ();

		for (int i = 0; i < num_threads; i++)
			tarray [i].Join ();
	}
}
