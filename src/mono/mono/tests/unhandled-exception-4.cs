using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CustomException : Exception
{
}

class Driver
{
	/* expected exit code: 255 */
	static void Main (string[] args)
	{
		if (Environment.GetEnvironmentVariable ("TEST_UNHANDLED_EXCEPTION_HANDLER") != null)
			AppDomain.CurrentDomain.UnhandledException += (s, e) => {};

		ManualResetEvent mre = new ManualResetEvent (false);

		var t = Task.Factory.StartNew (new Action (() => { try { throw new CustomException (); } finally { mre.Set (); } }));

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		try {
			t.Wait ();
			Environment.Exit (5);
		} catch (AggregateException ae) {
			Console.WriteLine (ae);
			if (ae.InnerExceptions [0] is CustomException) {
				/* expected behaviour */
				Environment.Exit (255);
			}
		} catch (Exception ex) {
			Console.WriteLine (ex);
			Environment.Exit (3);
		}

		Environment.Exit (6);
	}
}
