// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace R2RDumpTest
{
    public class BasicTests
    {
		static int Main(string[] args)
		{
			Console.WriteLine("Starting the test");

			TestHelpers.RunTest(args[0], "HelloWorld");
			TestHelpers.RunTest(args[0], "MultipleRuntimeFunctions");
			TestHelpers.RunTest(args[0], "GenericFunctions");
			TestHelpers.RunTest(args[0], "GcInfoTransitions");
			
			Console.WriteLine("PASSED");
			return 100;
		}
    }
}
