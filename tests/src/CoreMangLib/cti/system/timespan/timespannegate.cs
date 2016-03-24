// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Negate
/// </summary>
public class TimeSpanNegate
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Negate when current TimeSpan is a positive value");

        try
        {
            long randValue = 0;

            do
            {
                randValue = TestLibrary.Generator.GetInt64(-55);
            } while (randValue <= 0);

            TimeSpan expected = new TimeSpan(randValue);
            TimeSpan res = expected.Negate();

            if (res.Ticks != (expected.Ticks * -1))
            {
                TestLibrary.TestFramework.LogError("001.1", "Call Negate when current TimeSpan is a positive value does not return a negative value");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] res.Ticks = " + res.Ticks + ", expected.Ticks = " + expected.Ticks + ", expected = " + expected + ", randValue = " + randValue);
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Negate when current TimeSpan is a negative value");

        try
        {
            long randValue = 0;

            do
            {
                randValue = TestLibrary.Generator.GetInt64(-55);
            } while ((randValue == 0) || (randValue == Int64.MinValue));

            if (randValue > 0)
            {
                randValue *= -1;
            }

            TimeSpan expected = new TimeSpan(randValue);
            TimeSpan res = expected.Negate();

            if (res.Ticks != (expected.Ticks * -1))
            {
                TestLibrary.TestFramework.LogError("002.1", "Call Negate when current TimeSpan is a negative value does not return a positive value");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] res.Ticks = " + res.Ticks + ", expected.Ticks = " + expected.Ticks + ", expected = " + expected + ", randValue = " + randValue);
                retVal = false;
            }
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Negate when current TimeSpan is 0");

        try
        {
            TimeSpan expected = new TimeSpan(0);
            TimeSpan res = expected.Negate();

            if (res.Ticks != 0)
            {
                TestLibrary.TestFramework.LogError("003.1", "Call Negate when current TimeSpan is a negative value does not return a positive value");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] res.Ticks = " + res.Ticks);
                retVal = false;
            }
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

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException should be thrown when the value is TimeSpan.MinValue");

        try
        {
            TimeSpan res = TimeSpan.MinValue.Negate();

            TestLibrary.TestFramework.LogError("101.1", "OverflowException is not thrown when the value is TimeSpan.MinValue");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] res = " + res);
            retVal = false;
        }
        catch (OverflowException)
        {
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
        TimeSpanNegate test = new TimeSpanNegate();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanNegate");

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
