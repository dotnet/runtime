// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

[SecurityCritical]
public unsafe class A
{
	public static int Main(string[] args)
	{
		bool retVal = true;
		int[] array = new int[10];

		TestLibrary.TestFramework.BeginTestCase("Using unsafe code and building against CoreCLR mscorlib");

		TestLibrary.TestFramework.BeginScenario("Unsafe code take a dependecny on System.Security.Permissions.SecurityPermissionAttribute");

		for(int j=0; j<array.Length; j++)
		{
			array[j] = j*35234 + 23;
		}

		fixed(int* i = array)
		{
			for(int j=0; j<array.Length; j++)
			{
				*(i+j) = j;
			}
		}

		for(int j=0; j<array.Length; j++)
		{
			if (j != array[j])
			{
				TestLibrary.TestFramework.LogError("001", "FAIL!  Wrong value " + j + " != " + array[j]);
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
