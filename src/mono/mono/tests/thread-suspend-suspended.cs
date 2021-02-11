
using System;
using System.Runtime.CompilerServices;
using System.Threading;

class Driver
{

	public static void Main ()
	{
		bool finished = false;
		AutoResetEvent start_gc = new AutoResetEvent (false);
		AutoResetEvent finished_gc = new AutoResetEvent (false);

		Thread t1 = new Thread (() => {
			while (!Volatile.Read(ref finished)) {}
		});

		Thread t2 = new Thread (() => {
			while (!Volatile.Read(ref finished)) {
				if (start_gc.WaitOne (0)) {
					GC.Collect ();
					finished_gc.Set ();
				}

				Thread.Yield ();
			}
		});

		t1.Start ();
		t2.Start ();

		Thread.Sleep (10);

		for (int i = 0; i < 50 * 40 * 5; ++i) {
			t1.Suspend ();
			start_gc.Set ();
			finished_gc.WaitOne ();
			t1.Resume ();

			if ((i + 1) % (50) == 0)
				Console.Write (".");
			if ((i + 1) % (50 * 40) == 0)
				Console.WriteLine ();
		}

		Volatile.Write(ref finished, true);

		t1.Join ();
		t2.Join ();
	}
}
