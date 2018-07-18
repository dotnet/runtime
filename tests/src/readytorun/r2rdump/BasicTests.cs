// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace R2RDumpTest
{
    public class BasicTests
    {
		static int Main(string[] args)
		{
			Console.WriteLine("Starting the test");

			TestHelpers.RunTest("HelloWorld");
			TestHelpers.RunTest("MultipleRuntimeFunctions");
			TestHelpers.RunTest("GenericFunctions");
			TestHelpers.RunTest("GcInfoTransitions");
			
			Console.WriteLine("PASSED");
			return 100;
		}
    }
}
