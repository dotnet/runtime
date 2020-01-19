using System;
using System.Threading;
using System.Runtime.ConstrainedExecution;

class P
{
	int index;
	ManualResetEvent mre;

	public static int Count = 0;

	public P (int index, ManualResetEvent mre)
	{
		this.index = index;
		this.mre = mre;
	}

	~P ()
	{
		mre.Set ();

		Console.Write (String.Format ("[{0}] Finalize\n", index));
		Count ++;
		Console.Write (String.Format ("[{0}] Finalize -- end\n", index));
	}
}

class Driver
{
	static int Main ()
	{
		Thread thread;
		ManualResetEvent mre;
		int collected, total = 100;

		for (int i = 1; i <= 1000; ++i) {
			P.Count = 0;

			mre = new ManualResetEvent (false);

			thread = new Thread (() => {
				for (int j = 0; j < total; ++j)
					new P (i, mre);
			});
			thread.Start ();
			thread.Join ();

			GC.Collect ();

			Console.Write (String.Format ("[{0}] Wait for pending finalizers\n", i));
			GC.WaitForPendingFinalizers ();
			Console.Write (String.Format ("[{0}] Wait for pending finalizers -- end\n", i));

			collected = P.Count;
			if (collected == 0) {
				if (!mre.WaitOne (5000)) {
					Console.Write (String.Format ("[{0}] Finalizer never started\n", i));
					return 1;
				}

				Console.Write (String.Format ("[{0}] Wait for pending finalizers (2)\n", i));
				GC.WaitForPendingFinalizers ();
				Console.Write (String.Format ("[{0}] Wait for pending finalizers (2) -- end\n", i));

				collected = P.Count;
				if (collected == 0) {
					/* At least 1 finalizer started (as mre has been Set), but P.Count has not been incremented */
					Console.Write (String.Format ("[{0}] Did not wait for finalizers to run\n", i));
					return 2;
				}
			}

			if (collected != total) {
				/* Not all finalizer finished, before returning from WaitForPendingFinalizers. Or not all objects
				 * have been garbage collected; this might be due to false pinning */
				Console.Write (String.Format ("[{0}] Finalized {1} of {2} objects\n", i, collected, total));
			}
		}
		return 0;
	}
}
