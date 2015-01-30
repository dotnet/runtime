// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Threading;

public class ThreadCultureRegression
{
	public static int Main()
	{
		// On Mac setting the current thread culture to german
		//  resulted in an error.

		CultureInfo german = new CultureInfo("de-DE");

		TestLibrary.Utilities.CurrentCulture = german;

		// verify that it was changed
		if (TestLibrary.Utilities.CurrentCulture.Name == "de-DE")
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 0;
		}

	}
}
