// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TicksPerMinute
/// </summary>
public class TimeSpanTicksPerMinute
{
    #region Private Fields
    private const long c_DESIRED_TICKSPERMINUTE = 600000000;
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The value of TicksPerMinute should be 600000000");

        try
        {
            long actual = TimeSpan.TicksPerMinute;

            if (actual != c_DESIRED_TICKSPERMINUTE)
            {
                TestLibrary.TestFramework.LogError("001.1", "The value of TicksPerMinute is not 600000000");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", c_DESIRED_TICKSPERMINUTE = " + c_DESIRED_TICKSPERMINUTE);
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
        TimeSpanTicksPerMinute test = new TimeSpanTicksPerMinute();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTicksPerMinute");

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
