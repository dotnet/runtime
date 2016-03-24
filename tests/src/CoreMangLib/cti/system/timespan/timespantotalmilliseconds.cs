// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TotalMilliseconds
/// </summary>
public class TimeSpanTotalMilliseconds
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call TotalMilliseconds with valid values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 0, 0, 0), 86400000.0, "001.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0, "001.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 23, 0, 0, 999), 82800999.0, "001.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(48, 0, 0, 0, 99), 4147200099.0, "001.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 60, 9), 3609000.0, "001.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 1), 1000.0, "001.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 1, 0), 60000.0, "001.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(1, 0, 0), 3600000.0, "001.8") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, 1), 1.0, "001.9") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call TotalMilliseconds on negative TimeSpan instances");

        try
        {
            retVal = VerificationHelper(new TimeSpan(-1, 0, 0, 0), -86400000.0, "002.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0.0, "002.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -23, 0, 0, 999), -82799001.0, "002.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(-48, 0, 0, 0, -99), -4147200099.0, "002.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, -1), -1000, "002.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -1, 0), -60000, "002.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(-1, 0, 0), -3600000, "002.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, -1), -1.0, "002.8") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TimeSpanTotalMilliseconds test = new TimeSpanTotalMilliseconds();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTotalMilliseconds");

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

        double actual = ts.TotalMilliseconds;
        double result = actual - expected;
        if (result < 0)
        {
            result *= -1;
        }

        if (result >= 0.00000001)
        {
            TestLibrary.TestFramework.LogError(errorno, "TotalMilliseconds returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts = " + ts + ", expected = " + expected + ", actual = " + actual + ", result = " + result);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
