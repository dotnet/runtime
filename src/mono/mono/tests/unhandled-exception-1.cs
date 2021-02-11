using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CustomException : Exception
{
}

class Driver
{
	/* Expected exit code: 1 */
	static void Main (string[] args)
	{
		if (Environment.GetEnvironmentVariable ("TEST_UNHANDLED_EXCEPTION_HANDLER") != null)
			AppDomain.CurrentDomain.UnhandledException += (s, e) => {};

		ManualResetEvent mre = new ManualResetEvent (false);

		var t = new Thread (new ThreadStart (() => { try { throw new CustomException (); } finally { mre.Set (); } }));
		t.Start ();

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		/* Give a chance to the thread to finish executing the exception unwinding
		 * after the finally, before we exit with status 0 on the current thread */
		Thread.Sleep (1000);

		Environment.Exit (0);
	}
}
