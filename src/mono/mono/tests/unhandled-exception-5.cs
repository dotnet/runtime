using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CustomException : Exception
{
}

class Driver
{
	static ManualResetEvent mre = new ManualResetEvent (false);

	class FinalizedClass
	{
		~FinalizedClass ()
		{
			try {
				throw new CustomException ();
			} finally {
				mre.Set ();
			}
		}
	}

	/* expected exit code: 255 */
	static void Main (string[] args)
	{
		if (Environment.GetEnvironmentVariable ("TEST_UNHANDLED_EXCEPTION_HANDLER") != null)
			AppDomain.CurrentDomain.UnhandledException += (s, e) => {};

		new FinalizedClass();

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		/* Give a chance to the finalizer thread to finish executing the exception unwinding
		 * after the finally, before we exit with status 0 on the current thread */
		Thread.Sleep (1000);

		Environment.Exit (0);
	}
}
