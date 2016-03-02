// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// MinValue
/// </summary>
public class TimeSpanMinValue
{
    #region Private Fields
    private const string c_EXPECTED_STRING_REPRESENTATION = "-10675199.02:48:05.4775808";
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: the value MinValue is equivalent to Int64.MinValue and the string representation of this value is -10675199.02:48:05.4775808");

        try
        {
            long actual = TimeSpan.MinValue.Ticks;
            long desired = Int64.MinValue;

            if (actual != desired)
            {
                TestLibrary.TestFramework.LogError("001.1", "the value MinValue is not equivalent to Int64.MinValue");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] actual = " + actual + ", desired = " + desired);
                retVal = false;
            }

            string actualString = TimeSpan.MinValue.ToString();
            if (actualString != c_EXPECTED_STRING_REPRESENTATION)
            {
                TestLibrary.TestFramework.LogError("001.2", "string representation of MaxValue is -10675199.02:48:05.4775808");
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
        TimeSpanMinValue test = new TimeSpanMinValue();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanMinValue");

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
