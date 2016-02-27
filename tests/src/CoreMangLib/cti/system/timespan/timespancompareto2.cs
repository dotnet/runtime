// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// CompareTo(System.TimeSpan)
/// </summary>
public class TimeSpanCompareTo2
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call CompareTo with t1 less than t2 should return -1");

        try
        {
            retVal = VerificationHelper(new TimeSpan(-1), new TimeSpan(0), -1, "001.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(0), new TimeSpan(1), -1, "001.2") && retVal;
            retVal = VerificationHelper(TimeSpan.MinValue, TimeSpan.MaxValue, -1, "001.3") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call CompareTo with t1 equal to  t2 should return 0");

        try
        {
            retVal = VerificationHelper(new TimeSpan(1), new TimeSpan(1), 0, "002.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(-1), new TimeSpan(-1), 0, "002.2") && retVal;
            retVal = VerificationHelper(new TimeSpan(0), new TimeSpan(0), 0, "002.3") && retVal;
            retVal = VerificationHelper(TimeSpan.MinValue, TimeSpan.MinValue, 0, "0014") && retVal;
            retVal = VerificationHelper(TimeSpan.MaxValue, TimeSpan.MaxValue, 0, "002.5") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call CompareTo with t1 greater than t2 should return 1");

        try
        {
            retVal = VerificationHelper(new TimeSpan(0), new TimeSpan(-1), 1, "003.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(1), new TimeSpan(0), 1, "003.2") && retVal;
            retVal = VerificationHelper(TimeSpan.MaxValue, TimeSpan.MinValue, 1, "003.3") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TimeSpanCompareTo2 test = new TimeSpanCompareTo2();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanCompareTo2");

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
    private bool VerificationHelper(TimeSpan span1, TimeSpan span2, int desired, string errorno)
    {
        bool retVal = true;

        int actual = span1.CompareTo(span2);

        if (actual != desired)
        {
            TestLibrary.TestFramework.LogError(errorno, "CompareTo returns wrong result");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] span1.Ticks = " + span1.Ticks + ", span2.Ticks = " + span2.Ticks + ", desired = " + desired + ", actual = " + actual);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
