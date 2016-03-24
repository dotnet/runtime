// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// IsReadOnly
/// </summary>
public class DateTimeFormatInfoIsReadOnly
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: IsReadOnly should return false for DateTimeFormatInfo created from non invariant culture");

        try
        {
            DateTimeFormatInfo info = new CultureInfo("en-us").DateTimeFormat;
            if (info.IsReadOnly)
            {
                TestLibrary.TestFramework.LogError("001.1", "IsReadOnly returns true for DateTimeFormatInfo created from non invariant culture");
                retVal = false;
            }
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: IsReadOnly should return true for DateTimeFormatInfo created from invariant culture");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.InvariantInfo;
            if (!info.IsReadOnly)
            {
                TestLibrary.TestFramework.LogError("002.1", "IsReadOnly returns false for DateTimeFormatInfo created from non invariant culture");
                retVal = false;
            }
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: IsReadOnly should return true for DateTimeFormatInfo created by ReadOnly method");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.ReadOnly(new CultureInfo("en-us").DateTimeFormat);
            if (!info.IsReadOnly)
            {
                TestLibrary.TestFramework.LogError("003.1", "IsReadOnly returns false for DateTimeFormatInfo created by ReadOnly method");
                retVal = false;
            }
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
        DateTimeFormatInfoIsReadOnly test = new DateTimeFormatInfoIsReadOnly();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoIsReadOnly");

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
}
