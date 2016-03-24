// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TicksPerSecond
/// </summary>
public class TimeSpanTicksPerSecond
{
    #region Private Fields
    private const long c_DESIRED_TICKSPERSECOND = 10000000;
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: The value of TicksPerSecond should be 10000000");

        try
        {
            long actual = TimeSpan.TicksPerSecond;

            if (actual != c_DESIRED_TICKSPERSECOND)
            {
                TestLibrary.TestFramework.LogError("001.1", "The value of TicksPerSecond is not 10000000");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", c_DESIRED_TICKSPERSECOND = " + c_DESIRED_TICKSPERSECOND);
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
        TimeSpanTicksPerSecond test = new TimeSpanTicksPerSecond();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTicksPerSecond");

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
