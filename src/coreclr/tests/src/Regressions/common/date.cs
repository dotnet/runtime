// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
