using System;
using System.Security.Policy;
using System.Threading;

class Client {

	[LoaderOptimization (LoaderOptimization.SingleDomain)]
	static int Main (string[] args)
	{
		int res = 0;
		
		foreach (string s in args) {
			res += Convert.ToInt32 (s);
		}

		Console.WriteLine ("(appdomain-client.exe) Sum: " + res);
		return res;
	}
}
