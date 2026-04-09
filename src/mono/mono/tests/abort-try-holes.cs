using System;
using System.Threading;

public class Program
{
	private static readonly ReaderWriterLockSlim rwl = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

	private static int abort_count = 0;
	private const int num_iterations = 10;

	private static int Main(string[] args)
	{
		for (int i = 0; i < num_iterations; i++) {
			Console.WriteLine("Next iter");

			Barrier barrier = new Barrier (3);

			Thread reader = new Thread (UseLockForRead);
			Thread writer = new Thread (UseLockForWrite);
			reader.Start (barrier);
			writer.Start (barrier);

			barrier.SignalAndWait ();

			writer.Abort();
			reader.Abort();

			reader.Join();
			writer.Join();
		}

		if (abort_count != (num_iterations * 2)) {
			Console.WriteLine ("Only {0} aborts", abort_count);
			return 1;
		}
		return 0;
	}

	private static void UseLockForRead (object barrier)
	{
		bool locked = false;
		try {
			((Barrier)barrier).SignalAndWait ();
			for (;;) {
				try {
					try {}
					finally	{
						rwl.EnterReadLock();
						locked=true;
					}
				}
				finally {
					if (locked) {
						rwl.ExitReadLock();
						locked=false;
					}
				}
			}
		} catch (ThreadAbortException) {
			Interlocked.Increment (ref abort_count);
		}
	}

	private static void UseLockForWrite (object barrier)
	{
		bool locked = false;

		try {
			((Barrier)barrier).SignalAndWait ();
			for (;;) {
				try {
					try {}
					finally	{
						rwl.EnterWriteLock();
						locked = true;
					}
				} finally {
					if (locked)
					{
						rwl.ExitWriteLock();
						locked=false;
					}
				}
			}
		} catch (ThreadAbortException) {
			Interlocked.Increment (ref abort_count);
		}
	}
}
