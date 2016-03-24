// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TotalMinutes
/// </summary>
public class TimeSpanTotalMinutes
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call TotalMinutes with valid values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 0, 0, 0), 1440.0, "001.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0.0, "001.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 23, 59, 0), 1439.0, "001.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 1, 0), 1, "001.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 61, 0), 61, "001.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 61), 1.01666666666667, "001.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(1, 0, 0), 60, "001.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, 500), 0.008333333333, "001.8") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 23, -59, 0), 1321.0, "001.9") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call TotalMinutes with boundary values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 23, 119, 59), 2939.983333333, "002.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(47, 59, 59), 2879.983333333, "002.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 60), 1, "002.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 59), 0.983333333333333, "002.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 1), 0.01666666666666, "002.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, 1), 1.66666666666667E-05, "002.6") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call TotalMinutes on negative TimeSpan instances");

        try
        {
            retVal = VerificationHelper(new TimeSpan(-1, 0, 0, 0), -1440, "003.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0, "003.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -23, -59, 0), -1439, "003.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 1, 0), 1, "003.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -61, 0), -61, "003.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, -61), -1.016666666666, "003.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(-1, 0, 0), -60, "003.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, -500), -0.00833333333333333, "003.8") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -23, 59, 0), -1321.0, "003.9") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TimeSpanTotalMinutes test = new TimeSpanTotalMinutes();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTotalMinutes");

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
    private bool VerificationHelper(TimeSpan ts, double expected, string errorno)
    {
        bool retVal = true;

        double actual = ts.TotalMinutes;
        double result = actual - expected;
        if (result < 0)
        {
            result *= -1;
        }

        if (result >= 0.00000001)
        {
            TestLibrary.TestFramework.LogError(errorno, "TotalMinutes returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts = " + ts + ", expected = " + expected + ", actual = " + actual + ", result = " + result);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
