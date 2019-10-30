// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
