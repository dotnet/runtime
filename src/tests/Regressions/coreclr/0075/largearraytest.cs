// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/* NAME:		LargeArrayTest
 * SDET:		clyon
 * DATE:		2004-03-02
 * DESCRIPTION: creates arrays of size Int32.MaxValue through Int32.MaxValue-100 inclusive
 * PURPOSE:	 regression test for VSWhidbey 244717
 */

using System;
using Xunit;

public class LargeArrayTest
{
	
	[Fact]
	public static int TestEntryPoint() 
	{
		int lowerBound = 100;

		if (!TestLibrary.Utilities.IsWindows) lowerBound = 35;

		TestLibrary.TestFramework.BeginTestCase("Large array test");

		TestLibrary.TestFramework.BeginScenario("Allocate arrays of values Int32.MaxValue to Int32.MaxValue-" + lowerBound);

		for (int i=0; i<= lowerBound; i++)
		{
			try
			{
				TestLibrary.Logging.Write("now try Int32.MaxValue-"+i+": ");
				Array a = Array.CreateInstance((new byte().GetType()), Int32.MaxValue-i);
				TestLibrary.Logging.WriteLine("" + a.GetLength(0));
				a = null; // need to null a, or we hit VSWhidbey 135712
			}
			catch (OutOfMemoryException e)
			{
				TestLibrary.TestFramework.LogInformation("");
				TestLibrary.TestFramework.LogInformation("" + e.Message);
			} 
			catch (Exception e)
			{
				TestLibrary.TestFramework.LogError("000", "");
				TestLibrary.TestFramework.LogError("000", "Unexpected Exception!");
				TestLibrary.TestFramework.LogError("000", "" + e);

				TestLibrary.TestFramework.EndTestCase();

				TestLibrary.TestFramework.LogError("000", "Test Failed!");
				return 0;
			}
		}

		TestLibrary.TestFramework.EndTestCase();
		TestLibrary.TestFramework.LogInformation("Test Passed!");
		return 100;
	}

}
