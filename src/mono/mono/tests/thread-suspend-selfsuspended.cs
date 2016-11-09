
using System;
using System.Threading;

class Driver
{
	public static void Main ()
	{
		bool finished = false;

		Thread t1 = Thread.CurrentThread;

		Thread t2 = new Thread (() => {
			while (!finished) {
				GC.Collect ();

				try {
					t1.Resume ();
				} catch (ThreadStateException) {
				}

				Thread.Yield ();
			}
		});

		t2.Start ();

		Thread.Sleep (10);

		for (int i = 0; i < 50 * 40 * 20; ++i) {
			Thread.CurrentThread.Suspend ();
			if ((i + 1) % (50) == 0)
				Console.Write (".");
			if ((i + 1) % (50 * 40) == 0)
				Console.WriteLine ();
		}

		finished = true;

		t2.Join ();
	}
}
