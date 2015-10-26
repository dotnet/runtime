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
		new FinalizedClass();

		GC.Collect ();
		GC.WaitForPendingFinalizers ();

		if (!mre.WaitOne (5000))
			Environment.Exit (2);

		Environment.Exit (0);
	}
}
