// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// GetAbbreviatedMonthName(System.Int32)
/// </summary>
public class DateTimeFormatInfoGetAbbreviatedMonthName
{
    #region Private Fields
    private const int c_MIN_MONTH_VALUE = 1;
    private const int c_MAX_MONTH_VALUE = 13;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetAbbreviatedDayName on default invariant DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = CultureInfo.InvariantCulture.DateTimeFormat;
            string[] expected = new string[] {
                "",
                "Jan",
                "Feb", 
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec",
                "",
            };

            retVal = VerificationHelper(info, expected, "001.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call GetAbbreviatedDayName on en-us culture DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;
            string[] expected = new string[] {
                "",
                "Jan",
                "Feb", 
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec",
                "",
            };

            retVal = VerificationHelper(info, expected, "002.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call GetAbbreviatedDayName on fr-FR culture DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = new CultureInfo("fr-FR").DateTimeFormat;
            string[] expected = new string[] {
                "",
                "janv.",
                "f\u00e9vr.", 
                "mars",
                "avr.",
                "mai",
                "juin",
                "juil.",
                "ao\u00fbt",
                "sept.",
                "oct.",
                "nov.",
                "d\u00e9c.",
                "",
            };

            retVal = VerificationHelper(info, expected, "003.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call GetAbbreviatedDayName on DateTimeFormatInfo instance created from ctor");

        try
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            string[] expected = new string[] {
                "",
                "Jan",
                "Feb", 
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec",
                "",
            };

            retVal = VerificationHelper(info, expected, "004.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when dayofweek is not a valid System.DayOfWeek value. ");

        try
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();

            info.GetAbbreviatedMonthName(c_MIN_MONTH_VALUE - 1);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();

            info.GetAbbreviatedMonthName(c_MAX_MONTH_VALUE + 1);

            TestLibrary.TestFramework.LogError("101.3", "ArgumentOutOfRangeException is not thrown");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.4", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoGetAbbreviatedMonthName test = new DateTimeFormatInfoGetAbbreviatedMonthName();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoGetAbbreviatedMonthName");

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
    private bool VerificationHelper(DateTimeFormatInfo info, string[] expected, string errorno)
    {
        bool retval = true;

        for (int i = c_MIN_MONTH_VALUE; i <= c_MAX_MONTH_VALUE; ++i)
        {
            string actual = info.GetAbbreviatedMonthName(i);
            if (actual != expected[i])
            {
                TestLibrary.TestFramework.LogError(errorno, "GetAbbreviatedDayName returns wrong value");
                TestLibrary.TestFramework.LogInformation("WARNING[LOCAL VARIABLES] i = " + i + ", expected[i] = " + expected[i] + ", actual = " + actual);
                retval = false;
            }
        }

        return retval;
    }
    #endregion
}
