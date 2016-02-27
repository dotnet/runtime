// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

public class CompEx
{
	public static int Main()
	{
		Int64 total, initial, newValue;
		Int64 newInitial;
		
		TestLibrary.TestFramework.BeginTestCase("CompareExchange(Int64&,Int64,Int64)");
		
		TestLibrary.TestFramework.BeginScenario("CompareExchange(0,1,0)");

		total = 0L;
		newInitial = 0L;
		for(int i=0; i<2; i++)
		{
			initial = total;
			newValue = initial + 1L;
			TestLibrary.TestFramework.LogInformation("BEFORE: T("+total+") NI("+newInitial+") NV("+newValue+") I("+initial+")");
			newInitial = Interlocked.CompareExchange( ref total, newValue, initial);
			TestLibrary.TestFramework.LogInformation("AFTER: T("+total+") NI("+newInitial+") NV("+newValue+") I("+initial+")");
			TestLibrary.TestFramework.LogInformation("");
		}
		
		TestLibrary.TestFramework.EndTestCase();

		if (2d == total)
		{
			TestLibrary.TestFramework.LogInformation("PASS");
			return 100;
		}
		else
		{
			TestLibrary.TestFramework.LogInformation("FAIL");
			return 0;
		}
	}
		
}

