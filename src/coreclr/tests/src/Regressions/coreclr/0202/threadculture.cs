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
