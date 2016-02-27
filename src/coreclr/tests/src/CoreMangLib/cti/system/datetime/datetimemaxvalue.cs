// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// MaxValue
/// </summary>
public class DateTimeMaxValue
{
    #region Private Fields
    private const long DATETIME_MAX_VALUE_TICKS = 3155378975999999999;
    #endregion

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: DateTime.MaxValue should be 23:59:59.9999999, December 31, 9999");

        try
        {
            DateTime desiredValue = new DateTime(DATETIME_MAX_VALUE_TICKS);
            if (!desiredValue.Equals(DateTime.MaxValue))
            {
                TestLibrary.TestFramework.LogError("001", "DateTime.MaxValue is not 23:59:59.9999999, December 31, 9999");
                retVal = false;
            } 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeMaxValue test = new DateTimeMaxValue();

        TestLibrary.TestFramework.BeginTestCase("DateTimeMaxValue");

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
