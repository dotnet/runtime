using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CustomException : Exception
{
}

class CustomException2 : Exception
{
}

class Driver
{
	/* expected exit code: 0 */
	static void Main (string[] args)
	{
		ManualResetEvent mre = new ManualResetEvent (false);
		ManualResetEvent mre2 = new ManualResetEvent (false);

		var a = new Action (() => { try { throw new CustomException (); } finally { mre.Set (); } });
		var ares = a.BeginInvoke (_ => { mre2.Set (); throw new CustomException2 (); }, null);

		if (!mre.WaitOne (5000))
			Environment.Exit (2);
		if (!mre2.WaitOne (5000))
			Environment.Exit (22);

		try {
			a.EndInvoke (ares);
			Environment.Exit (4);
		} catch (CustomException) {
		} catch (Exception ex) {
			Console.WriteLine (ex);
			Environment.Exit (3);
		}

		Environment.Exit (0);
	}
}
