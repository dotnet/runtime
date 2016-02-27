// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// InvariantInfo
/// </summary>
public class DateTimeFormatInfoInvariantInfo
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: InvariantInfo should return a read-only DateTimeFormatInfo");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.InvariantInfo;
            if (!info.IsReadOnly)
            {
                TestLibrary.TestFramework.LogError("001.1", "InvariantInfo does not return a read-only DateTimeFormatInfo");
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
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoInvariantInfo test = new DateTimeFormatInfoInvariantInfo();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoInvariantInfo");

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
