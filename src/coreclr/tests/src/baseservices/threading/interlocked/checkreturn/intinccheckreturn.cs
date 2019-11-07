// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace InterlockedTest
{
	class InterlockTest
	{

		public static int Main(string[] Args)
		{
			int rValue;
			IntTest testInt = new IntTest(100, 10);

			Console.WriteLine("Check Inc Returnt: {0}", rValue = testInt.CheckIncReturn());
			Console.WriteLine("Test {0}", 100 == rValue ? "Passed" : "Failed");
			return rValue;
		}
	}
}
