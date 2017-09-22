using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Driver
{
	/* expected exit code: 1 */
	static void Main (string[] args)
	{
		if (Environment.GetEnvironmentVariable ("TEST_UNHANDLED_EXCEPTION_HANDLER") != null)
			AppDomain.CurrentDomain.UnhandledException += (s, e) => {};

		throw new AppDomainUnloadedException ();
	}
}
