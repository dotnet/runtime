using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;

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

		var action = new Action (Delegate);
		var ares = action.BeginInvoke (Callback, null);

		Thread.Sleep (5000);

		Environment.Exit (1);
	}

	static void Delegate ()
	{
		throw new CustomException ();
	}

	static void Callback (IAsyncResult iares)
	{
		((Action) ((AsyncResult) iares).AsyncDelegate).EndInvoke (iares);
	}
}
