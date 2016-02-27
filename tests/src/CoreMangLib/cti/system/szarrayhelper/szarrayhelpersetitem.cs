// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class Test
{
	public static int Main()
	{
		bool retVal = true;

		TestLibrary.TestFramework.BeginTestCase("SZArray");

		TestLibrary.TestFramework.BeginScenario("SZArray is used when casting T[] to IList<T>");

		int[] szArray = new int[100];
		IList<int> list = szArray;

		for (int i = 0; i < 100; i++)
		{
			list[i] = 100-i; 
		}

		for (int i = 0; i < 100; i++)
		{
			if (100-i != szArray[i])
			{
				TestLibrary.TestFramework.LogError("000", "Incorrect value: Expected("+ (100-i) +") Actual("+ szArray[i] +")");
				retVal = false;
			}
		}

		TestLibrary.TestFramework.EndTestCase();

		if (retVal)
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

