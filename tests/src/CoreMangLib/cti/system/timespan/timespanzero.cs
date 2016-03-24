// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Zero
/// </summary>
public class TimeSpanZero
{
    #region Private Fields
    private const string c_EXPECTED_STRING_REPRESENTATION = "00:00:00";
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: the value Zero is equivalent to 0 and the string representation of this value is 00:00:00");

        try
        {
            long actual = TimeSpan.Zero.Ticks;
            long desired = 0;

            if (actual != desired)
            {
                TestLibrary.TestFramework.LogError("001.1", "the value Zero is not equivalent to 0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] actual = " + actual + ", desired = " + desired);
                retVal = false;
            }

            string actualString = TimeSpan.Zero.ToString();
            if (actualString != c_EXPECTED_STRING_REPRESENTATION)
            {
                TestLibrary.TestFramework.LogError("001.2", "string representation of MaxValue is 00:00:00");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] actualString = " + actualString);
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
        TimeSpanZero test = new TimeSpanZero();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanZero");

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
