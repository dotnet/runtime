// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Clone
/// </summary>
public class DateTimeFormatInfoClone
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Clone method on a instance created from Ctor");

        try
        {
            DateTimeFormatInfo expected = new DateTimeFormatInfo();

            retVal = VerificationHelper(expected, expected.Clone(), "001.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Clone method on a instance created from several cultures");

        try
        {
            DateTimeFormatInfo expected = new CultureInfo("en-us").DateTimeFormat;
            retVal = VerificationHelper(expected, expected.Clone(), "002.1") && retVal;
            
            expected = new CultureInfo("fr-FR").DateTimeFormat;
            retVal = VerificationHelper(expected, expected.Clone(), "002.2") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Clone method on a readonly instance created from several cultures");

        try
        {
            DateTimeFormatInfo expected = CultureInfo.InvariantCulture.DateTimeFormat;
            retVal = VerificationHelper(expected, expected.Clone(), "003.1") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoClone test = new DateTimeFormatInfoClone();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoClone");

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
    private bool VerificationHelper(DateTimeFormatInfo expected, Object obj, string errorno)
    {
        bool retval = true;

        if (!(obj is DateTimeFormatInfo))
        {
            TestLibrary.TestFramework.LogError(errorno + ".1", "Calling Clone method does not return an DateTimeFormatInfo copy");
            retval = false;
        }

        DateTimeFormatInfo actual = obj as DateTimeFormatInfo;
        if ( actual.IsReadOnly )
        {
            TestLibrary.TestFramework.LogError(errorno + ".2", "Calling Clone method makes DateTimeFormatInfo copy read only");
            retval = false;
        }

        retval =
            IsEquals(actual.AbbreviatedDayNames, expected.AbbreviatedDayNames, errorno + ".3") &&
            IsEquals(actual.AbbreviatedMonthGenitiveNames, expected.AbbreviatedMonthGenitiveNames, errorno + ".4") &&
            IsEquals(actual.AbbreviatedMonthNames, expected.AbbreviatedMonthNames, errorno + ".5") &&
            IsEquals(actual.DayNames, expected.DayNames, errorno + ".6") &&
            IsEquals(actual.MonthGenitiveNames, expected.MonthGenitiveNames, errorno + ".7") &&
            IsEquals(actual.MonthNames, expected.MonthNames, errorno + ".8") &&
            IsEquals(actual.ShortestDayNames, expected.ShortestDayNames, errorno + ".9") &&
            IsEquals(actual.AMDesignator, expected.AMDesignator, errorno + ".10") &&
			//DateTimeFormatInfo.DateSeparator property has been removed
            IsEquals(actual.FullDateTimePattern, expected.FullDateTimePattern, errorno + ".12") &&
            IsEquals(actual.LongDatePattern, expected.LongDatePattern, errorno + ".13") &&
            IsEquals(actual.LongTimePattern, expected.LongTimePattern, errorno + ".14") &&
            IsEquals(actual.MonthDayPattern, expected.MonthDayPattern, errorno + ".15") &&
            IsEquals(actual.PMDesignator, expected.PMDesignator, errorno + ".17") &&
            IsEquals(actual.RFC1123Pattern, expected.RFC1123Pattern, errorno + ".18") &&
            IsEquals(actual.ShortDatePattern, expected.ShortDatePattern, errorno + ".19") &&
            IsEquals(actual.ShortTimePattern, expected.ShortTimePattern, errorno + ".20") &&
            IsEquals(actual.SortableDateTimePattern, expected.SortableDateTimePattern, errorno + ".21") &&
			//DateTimeFormatInfo.TimeSeparator property has been removed
			IsEquals(actual.UniversalSortableDateTimePattern, expected.UniversalSortableDateTimePattern, errorno + ".23") &&
            IsEquals(actual.YearMonthPattern, expected.YearMonthPattern, errorno + ".24") &&
            IsEquals(actual.CalendarWeekRule, expected.CalendarWeekRule, errorno + ".25") &&
            IsEquals(actual.FirstDayOfWeek, expected.FirstDayOfWeek, errorno + ".26") &&
            retval;

        return retval;
    }

    private bool IsEquals(string str1, string str2, string errorno)
    {
        bool retVal = true;

        if (str1 != str2)
        {
            TestLibrary.TestFramework.LogError(errorno, "Two string are not equal");
            TestLibrary.TestFramework.LogInformation("WARNING[LOCAL VARIABLES] str1 = " + str1 + ", str2 = " + str2);
            retVal = false;
        }

        return retVal;
    }

    private bool IsEquals(DayOfWeek value1, DayOfWeek value2, string errorno)
    {
        bool retVal = true;

        if (value1 != value2)
        {
            TestLibrary.TestFramework.LogError(errorno, "Two values are not equal");
            TestLibrary.TestFramework.LogInformation("WARNING[LOCAL VARIABLES] value1 = " + value1 + ", value2 = " + value2);
            retVal = false;
        }

        return retVal;
    }

    private bool IsEquals(CalendarWeekRule value1, CalendarWeekRule value2, string errorno)
    {
        bool retVal = true;

        if (value1 != value2)
        {
            TestLibrary.TestFramework.LogError(errorno, "Two values are not equal");
            TestLibrary.TestFramework.LogInformation("WARNING[LOCAL VARIABLES] value1 = " + value1 + ", value2 = " + value2);
            retVal = false;
        }

        return retVal;
    }

    private bool IsEquals(string[] array1, string[] array2, string errorno)
    {
        bool retval = true;

        if ((array1 == null) && (array2 == null))
        {
            return true;
        }
        if ((array1 == null) && (array2 != null))
        {
            return false;
        }
        if ((array1 != null) && (array2 == null))
        {
            return false;
        }
        if (array1.Length != array2.Length)
        {
            return false;
        }

        for (int i = 0; i < array1.Length; i++)
        {
            if (array1[i] != array2[i])
            {
                TestLibrary.TestFramework.LogError(errorno, "Two arrays are not equal");
                TestLibrary.TestFramework.LogInformation("WARNING[LOCAL VARIABLES] array1[i] = " + array1[i] + ", array2[i] = " + array2[i] + ", i = " + i);
                retval = false;
                break;
            }
        }

        return retval;
    }
    #endregion
}
