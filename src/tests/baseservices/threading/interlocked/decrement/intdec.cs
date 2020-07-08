// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace InterlockedTest
{
	class InterlockTest
	{

		public static int Main(string[] Args)
		{
			int rValue;
			int loops = 5000;
			int threads = 50;
			if (Args.Length == 2)
			{
				loops = Int32.Parse(Args[0]);
				threads = Int32.Parse(Args[1]);
			}
			Console.WriteLine("Starting Interlocked test on {0} threads for {1} iterations.",threads,loops);
			
			IntTest testInt = new IntTest(loops, threads);

			Console.WriteLine("Check Decrement: {0}", rValue = testInt.Dec());
			Console.WriteLine("Test {0}", 100 == rValue ? "Passed" : "Failed");
			return rValue;
		}
	}
}
