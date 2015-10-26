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
		ManualResetEvent mre = new ManualResetEvent (false);

		var t = new Thread (new ThreadStart (() => { try { throw new CustomException (); } finally { mre.Set (); } }));
		t.Start ();

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		t.Join ();

		Environment.Exit (0);
	}
}
