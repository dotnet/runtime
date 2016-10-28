
using System;
using System.Collections.Concurrent;
using System.Threading;

class Driver
{
	public static void Main ()
	{
		BlockingCollection<Thread> threads = new BlockingCollection<Thread> (new ConcurrentQueue<Thread> (), 128);

		bool finished = false;

		Thread gcThread = new Thread (() => {
			while (!finished) {
				GC.Collect ();
				Thread.Yield ();
			}
		});

		Thread joinThread = new Thread (() => {
			for (int i = 0; ; ++i) {
				Thread t = threads.Take ();
				if (t == null)
					break;
				t.Join ();
				if ((i + 1) % (50) == 0)
					Console.Write (".");
				if ((i + 1) % (50 * 50) == 0)
					Console.WriteLine ();
			}
		});

		gcThread.Start ();
		joinThread.Start ();

		for (int i = 0; i < 10 * 1000; ++i) {
			Thread t = new Thread (() => { Thread.Yield (); });
			t.Start ();

			threads.Add (t);
		}

		threads.Add (null);

		joinThread.Join ();

		finished = true;
		gcThread.Join ();
	}
}