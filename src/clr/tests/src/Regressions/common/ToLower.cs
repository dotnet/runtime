// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class ToLowerRepro
{
	private const string c_UPPERCASE = "TELESTO TEST TEaM";
	private const string c_LOWERCASE = "telesto test team";

	public static int Main(string[] args)
	{
		string lowerCase;

		TestLibrary.TestFramework.BeginTestCase("ToLower stack overflow test");

		TestLibrary.TestFramework.BeginScenario("ToLower(\""+ c_UPPERCASE +"\")");

		lowerCase = c_UPPERCASE.ToLower();

		if (lowerCase.Equals(c_LOWERCASE))
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogInformation("PASS");
			return 100;
		}
		else
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogError("001", "String was not converted to lower case: Expected("+c_LOWERCASE+") Actual("+lowerCase+")");
			TestLibrary.TestFramework.LogInformation("FAIL");
			return 0;
		}
	}

}
