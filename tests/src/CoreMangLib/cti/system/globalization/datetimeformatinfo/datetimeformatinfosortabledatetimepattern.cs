// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// SortableDateTimePattern
/// </summary>
public class DateTimeFormatInfoSortableDateTimePattern
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call SortableDateTimePattern getter method should return correct value for InvariantInfo");

        try
        {
            retVal = VerificationHelper(DateTimeFormatInfo.InvariantInfo, "yyyy'-'MM'-'dd'T'HH':'mm':'ss", "001.1") && retVal;
            retVal = VerificationHelper(new CultureInfo("en-us").DateTimeFormat, "yyyy'-'MM'-'dd'T'HH':'mm':'ss", "001.2") && retVal;
			try
			{
				retVal = VerificationHelper(new CultureInfo("ja-JP").DateTimeFormat, "yyyy'-'MM'-'dd'T'HH':'mm':'ss", "001.3") && retVal;
			}
			catch (ArgumentException)
			{
				TestLibrary.TestFramework.LogInformation("East Asian Languages are not installed. Skipping Japanese culture test(s).");
				retVal = retVal && true;
			}
			retVal = VerificationHelper(new CultureInfo("fr-fr").DateTimeFormat, "yyyy'-'MM'-'dd'T'HH':'mm':'ss", "001.4") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoSortableDateTimePattern test = new DateTimeFormatInfoSortableDateTimePattern();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoSortableDateTimePattern");

        if (test.RunTests())
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

    #region Private Methods
    private bool VerificationHelper(DateTimeFormatInfo info, string expected, string errorno)
    {
        bool retval = true;

        string actual = info.SortableDateTimePattern;
        if (actual != expected)
        {
            TestLibrary.TestFramework.LogError(errorno, "Call SortableDateTimePattern returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
            retval = false;
        }

        return retval;
    }
    #endregion
}
