using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class CustomException : Exception
{
}

class Driver
{
	/* expected exit code: 0 */
	static void Main (string[] args)
	{
		ManualResetEvent mre = new ManualResetEvent (false);

		ThreadPool.QueueUserWorkItem (_ => { try { throw new CustomException (); } finally { mre.Set (); } });

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		Environment.Exit (0);
	}
}
