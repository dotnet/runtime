// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class DateRepro
{
	public static int Main()
	{
		TestLibrary.TestFramework.BeginTestCase("DateTime");

		TestLibrary.TestFramework.BeginScenario("DateTime.Parse(January 02/35)");

		DateTime oDate = DateTime.Parse("January 02/35");

		Console.WriteLine(oDate);

		if (oDate.Year == 1935)
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogInformation("PASS");
			return 100;
		}
		else
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogInformation("FAIL");
			return 0;
		}
	}
}
