// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// GetFormat(System.Type)
/// </summary>
public class DateTimeFormatInfoGetFormat
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetFormat to to get an valid DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo expected = new DateTimeFormatInfo();
            object obj = expected.GetFormat(typeof(DateTimeFormatInfo));

            if (!(obj is DateTimeFormatInfo))
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling GetFormat returns a non DateTimeFormatInfo instance");
                retVal = false;
            }

            DateTimeFormatInfo actual = obj as DateTimeFormatInfo;
            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling GetFormat returns wrong instance");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: If the format type is not supported, null reference should be return");

        try
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            if (info.GetFormat(typeof(Object)) != null)
            {
                TestLibrary.TestFramework.LogError("002.1", "If the format type is not supported, null reference is not returned");
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
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoGetFormat test = new DateTimeFormatInfoGetFormat();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoGetFormat");

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
