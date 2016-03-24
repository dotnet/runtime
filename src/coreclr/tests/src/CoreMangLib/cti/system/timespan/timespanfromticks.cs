// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// FromTicks(System.Int64)
/// </summary>
public class TimeSpanFromTicks
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call FromTicks with rand Int64 values");

        try
        {
            long randValue = TestLibrary.Generator.GetInt64(-55);

            retVal = VerificationHelper(randValue, "001.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call FromTicks with boundary values");

        try
        {
            retVal = VerificationHelper(0, "002.1") && retVal;
            retVal = VerificationHelper(Int64.MinValue, "002.2") && retVal;
            retVal = VerificationHelper(Int64.MaxValue, "002.3") && retVal;
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
        TimeSpanFromTicks test = new TimeSpanFromTicks();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanFromTicks");

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

    #region Private Methods
    private bool VerificationHelper(long value, string errorno)
    {
        bool retVal = true;

        TimeSpan actual = TimeSpan.FromTicks(value);

        if (actual.Ticks != value)
        {
            TestLibrary.TestFramework.LogError(errorno, "FromTicks returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", value = " + value + ", actual.Ticks = " + actual.Ticks);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
