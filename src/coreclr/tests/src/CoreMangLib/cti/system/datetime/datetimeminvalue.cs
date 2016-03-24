// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// MinValue
/// </summary>
public class DateTimeMinValue
{
    #region Private Fields
    private const long DATETIME_MIN_VALUE_TICKS = 0;
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: DateTime.MinValue should be 00:00:00.0000000, January 1, 0001");

        try
        {
            DateTime desiredValue = new DateTime(DATETIME_MIN_VALUE_TICKS);
            if (!desiredValue.Equals(DateTime.MinValue))
            {
                TestLibrary.TestFramework.LogError("001", "DateTime.MinValue is not 00:00:00.0000000, January 1, 0001");
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
        DateTimeMinValue test = new DateTimeMinValue();

        TestLibrary.TestFramework.BeginTestCase("DateTimeMinValue");

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
