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

		var a = new Action (() => { try { throw new CustomException (); } finally { mre.Set (); } });
		var ares = a.BeginInvoke (null, null);

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		try {
			a.EndInvoke (ares);
			Environment.Exit (4);
		} catch (CustomException) {
			/* expected behaviour */
			Environment.Exit (255);
		} catch (Exception ex) {
			Console.WriteLine (ex);
			Environment.Exit (3);
		}

		Environment.Exit (5);
	}
}
