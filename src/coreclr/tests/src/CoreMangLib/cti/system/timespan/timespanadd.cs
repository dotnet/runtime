// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Add(System.TimeSpan)
/// </summary>
public class TimeSpanAdd
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Add with rand valid value");

        try
        {
            retVal = VerificationHelper(new TimeSpan(TestLibrary.Generator.GetInt32(-55)),
                new TimeSpan(TestLibrary.Generator.GetInt32(-55)),
                "001.1") && retVal;
            retVal = VerificationHelper(new TimeSpan(TestLibrary.Generator.GetInt32(-55)),
                new TimeSpan(0),
                "001.2") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Add with boundary value");

        try
        {
            retVal = VerificationHelper(TimeSpan.MaxValue, new TimeSpan(0), "002.1") && retVal;
            retVal = VerificationHelper(TimeSpan.MinValue, new TimeSpan(0), "002.2") && retVal;
            retVal = VerificationHelper(TimeSpan.MaxValue, TimeSpan.MinValue, "002.3") && retVal;
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException should be thrown when The resulting TimeSpan is less than MinValue or greater than MaxValue");

        try
        {
            retVal = VerificationHelper(TimeSpan.MaxValue, new TimeSpan(-1), "101.1") && retVal;
            retVal = VerificationHelper(TimeSpan.MinValue, new TimeSpan(1), "101.2") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TimeSpanAdd test = new TimeSpanAdd();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanAdd");

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

    #region Private Fields
    private bool VerificationHelper(TimeSpan span1, TimeSpan span2, string errorNo)
    {
        bool retVal = true;

        TimeSpan result = span1.Add(span2);

        long desired = span1.Ticks + span2.Ticks;
        long actual = result.Ticks;

        if (desired != actual)
        {
            TestLibrary.TestFramework.LogError(errorNo, "Calling Add method returns a wrong TimeSpan instance");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] desired = " + desired + ", actual = " + actual + ", span1.Ticks = " + span1.Ticks + ", span2.Ticks = " + span2.Ticks);
            retVal = false;
        }

        return retVal;
    }

    private bool VerificationHelper(TimeSpan span1, TimeSpan span2, Type desiredException, string errorNo)
    {
        bool retVal = true;

        try
        {
            TimeSpan result = span1.Add(span2);

            TestLibrary.TestFramework.LogError(errorNo + ".1", desiredException + " is not thrown");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] span1.Ticks = " + span1.Ticks + ", span2.Ticks = " + span2.Ticks);
            retVal = false;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errorNo + ".0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
