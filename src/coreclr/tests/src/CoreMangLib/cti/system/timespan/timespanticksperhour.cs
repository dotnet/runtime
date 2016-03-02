// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TicksPerHour
/// </summary>
public class TimeSpanTicksPerHour
{
    #region Private Fields
    private const long c_DESIRED_TICKSPERHOUR = 36000000000;
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The value of TicksPerHour should be 36000000000");

        try
        {
            long actual = TimeSpan.TicksPerHour;

            if (actual != c_DESIRED_TICKSPERHOUR)
            {
                TestLibrary.TestFramework.LogError("001.1", "The value of TicksPerHour is not 36000000000");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", c_DESIRED_TICKSPERHOUR = " + c_DESIRED_TICKSPERHOUR);
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
        TimeSpanTicksPerHour test = new TimeSpanTicksPerHour();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTicksPerHour");

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
