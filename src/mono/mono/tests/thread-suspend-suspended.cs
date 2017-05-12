
using System;
using System.Runtime.CompilerServices;
using System.Threading;

class Driver
{
	public static void Main ()
	{
		bool finished = false;
		int can_gc = 0;

		Thread t1 = new Thread (() => {
			while (!finished) {}
		});

		Thread t2 = new Thread (() => {
			while (!finished) {
				int local_can_gc = can_gc;
				if (local_can_gc > 0 && Interlocked.CompareExchange (ref can_gc, local_can_gc - 1, local_can_gc) == local_can_gc)
					GC.Collect ();
				Thread.Yield ();
			}
		});

		t1.Start ();
		t2.Start ();

		Thread.Sleep (10);

		for (int i = 0; i < 50 * 40 * 5; ++i) {
			t1.Suspend ();
			Interlocked.Increment (ref can_gc);
			Thread.Yield ();
			t1.Resume ();
			if ((i + 1) % (50) == 0)
				Console.Write (".");
			if ((i + 1) % (50 * 40) == 0)
				Console.WriteLine ();
		}

		finished = true;

		t1.Join ();
		t2.Join ();
	}
}
