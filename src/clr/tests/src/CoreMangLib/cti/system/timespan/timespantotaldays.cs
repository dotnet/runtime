// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TotalDays
/// </summary>
public class TimeSpanTotalDays
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call TotalDays with valid values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 0, 0, 0), 1.0, "001.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0.0, "001.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(23, 0, 0, 0), 23.0, "001.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(23, 0, 0), 0.958333333333, "001.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(1, 0, 0, 0, 500), 1.0000057870370369, "001.5") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call TotalDays with boundary values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(23, 23, 59, 59), 23.9999884259259, "002.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(47, 59, 59), 1.99998842592593, "002.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 86400), 1.0, "002.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 86399), 0.999988425925926, "002.4") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call TotalDays on negative TimeSpan instances");

        try
        {
            retVal = VerificationHelper(new TimeSpan(-1, 0, 0, 0), -1.0, "003.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(-23, 0, 0, 0), -23.0, "003.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(-23, 0, 0), -0.95833333, "003.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(-48, 0, 0), -2.0, "003.4") && retVal;
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
        TimeSpanTotalDays test = new TimeSpanTotalDays();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTotalDays");

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

        double actual = ts.TotalDays;
        double result = actual - expected;
        if (result < 0)
        {
            result *= -1;
        }

        if (result >= 0.00000001)
        {
            TestLibrary.TestFramework.LogError(errorno, "TotalDays returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts = " + ts + ", expected = " + expected + ", actual = " + actual + ", result = " + result);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
