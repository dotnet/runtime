// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TicksPerDay
/// </summary>
public class TimeSpanTicksPerDay
{
    #region Private Fields
    private const long c_DESIRED_TICKSPERDAY = 864000000000;
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The value of TicksPerDay should be 864000000000");

        try
        {
            long actual = TimeSpan.TicksPerDay;

            if (actual != c_DESIRED_TICKSPERDAY)
            {
                TestLibrary.TestFramework.LogError("001.1", "The value of TicksPerDay is not 864000000000");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", c_DESIRED_TICKSPERDAY = " + c_DESIRED_TICKSPERDAY);
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
        TimeSpanTicksPerDay test = new TimeSpanTicksPerDay();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTicksPerDay");

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
