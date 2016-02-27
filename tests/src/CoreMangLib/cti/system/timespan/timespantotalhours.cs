// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TotalHours
/// </summary>
public class TimeSpanTotalHours
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call TotalHours with valid values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 0, 0, 0), 24.0, "001.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0.0, "001.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 23, 0, 0), 23.0, "001.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 60, 0), 1.0, "001.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 3600), 1.0, "001.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 500), 0.138888888888889, "001.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 10, 0), 0.166666666666667, "001.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 10000, 0), 166.666666666667, "001.8") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call TotalHours with boundary values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 23, 59, 59), 47.9997222222222, "002.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(47, 59, 59), 47.9997222222222, "002.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 86400), 24.0, "002.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 86399), 23.9997222222222, "002.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 1), 0.0002777777777778, "002.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 1, 0), 0.01666666666666, "002.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(1, 1, 1), 1.01694444444444, "002.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(1, 1, 1, 1), 25.01694444444, "002.8") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, 1), 2.77777777777778E-07, "002.9") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call TotalHours on negative TimeSpan instances");

        try
        {
            retVal = VerificationHelper(new TimeSpan(-1, 0, 0, 0), -24.0, "003.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -23, 0, 0), -23.0, "003.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(-48, 1, 0), -47.98333333333, "003.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -60, 0), -1.0, "003.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, -3600), -1.0, "003.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, -500), -0.13888888888889, "003.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -10, 0), -0.1666666666667, "003.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -10000, 0), -166.66666666667, "003.8") && retVal;
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
        TimeSpanTotalHours test = new TimeSpanTotalHours();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTotalHours");

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

        double actual = ts.TotalHours;
        double result = actual - expected;
        if (result < 0)
        {
            result *= -1;
        }

        if (result >= 0.00000001)
        {
            TestLibrary.TestFramework.LogError(errorno, "TotalHours returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts = " + ts + ", expected = " + expected + ", actual = " + actual + ", result = " + result);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
