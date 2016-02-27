// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// TotalSeconds
/// </summary>
public class TimeSpanTotalSeconds
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call TotalSeconds with valid values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 0, 0, 0), 86400, "001.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0, "001.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 23, 59, 59), 86399, "001.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(48, 0, -59), 172741, "001.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 61), 61, "001.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, 1000), 1, "001.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 1, 0, 0, 0), 3600.0, "001.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 1, 0, 0), 60.0, "001.8") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 1, 0), 1.0, "001.9") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 23, 59, 59, 900), 86399.9, "001.10") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call TotalSeconds with boundary values");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1, 23, 119, 59), 176399, "002.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(47, 59, 59), 172799, "002.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 60), 60, "002.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, 999), 0.999, "002.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, 1), 0.001, "002.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, -1), -0.001, "002.6") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call TotalSeconds on negative TimeSpan instances");

        try
        {
            retVal = VerificationHelper(new TimeSpan(-1, 0, 0, 0), -86400, "003.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0), 0, "003.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -23, -59, -59), -86399, "003.3") && retVal;
            retVal = VerificationHelper(new TimeSpan(-48, 59, 59), -169201, "003.4") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, -61), -61, "003.5") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, 0, -1000), -1, "003.6") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, -1, 0, 0, 0), -3600.0, "003.7") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, -1, 0, 0), -60.0, "003.8") && retVal;
            retVal = VerificationHelper(new TimeSpan(0, 0, 0, -1, 0), -1.0, "003.9") && retVal;
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
        TimeSpanTotalSeconds test = new TimeSpanTotalSeconds();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanTotalSeconds");

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

        double actual = ts.TotalSeconds;
        double result = actual - expected;
        if (result < 0)
        {
            result *= -1;
        }

        if (result >= 0.00000001)
        {
            TestLibrary.TestFramework.LogError(errorno, "TotalSeconds returns wrong value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts = " + ts + ", expected = " + expected + ", actual = " + actual + ", result = " + result);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
