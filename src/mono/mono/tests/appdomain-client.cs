using System;
using System.Security.Policy;
using System.Threading;

class Client {

	[LoaderOptimization (LoaderOptimization.SingleDomain)]
	static int Main (string[] args)
	{
		int res = 0;
		
		foreach (string s in args) {
			Console.WriteLine (s);
			res += Convert.ToInt32 (s);
		}

		Console.WriteLine ("Sum: " + res);
		return res;
	}
}
