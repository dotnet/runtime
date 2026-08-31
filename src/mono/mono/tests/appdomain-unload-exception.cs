using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
	static void ThrowTP ()
	{
		ManualResetEvent mre = new ManualResetEvent (false);

		ThreadPool.QueueUserWorkItem (_ => { try { throw new AppDomainUnloadedException (); } finally { mre.Set (); } });

		if (!mre.WaitOne (5000))
			Environment.Exit (1);

		/* Wait for exception unwinding */
		Thread.Sleep (500);
	}

	static void ThrowThread ()
	{
		Thread thread = new Thread (_ => { throw new AppDomainUnloadedException (); });
		thread.Start ();
		thread.Join ();
	}

	static int Main (string[] args)
	{
		ThrowTP ();
		ThrowThread ();

		return 0;
	}
}
