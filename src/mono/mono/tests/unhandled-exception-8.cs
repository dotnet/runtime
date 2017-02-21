
using System;
using System.Threading;

class CustomException : Exception
{
}

class Driver
{
	/* expected exit code: 255 */
	public static void Main ()
	{
		if (Environment.GetEnvironmentVariable ("TEST_UNHANDLED_EXCEPTION_HANDLER") != null)
			AppDomain.CurrentDomain.UnhandledException += (s, e) => {};

		ManualResetEvent mre = new ManualResetEvent(false);

		ThreadPool.RegisterWaitForSingleObject (mre, (state, timedOut) => { throw new CustomException (); }, null, -1, true);
		mre.Set();

		Thread.Sleep (5000);
	}
}