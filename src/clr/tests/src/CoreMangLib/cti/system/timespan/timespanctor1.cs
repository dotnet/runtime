// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.Int32,System.Int32,System.Int32)
/// </summary>
public class TimeSpanCtor1
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
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor with rand valid value");

        try
        {
            int hours = TestLibrary.Generator.GetInt16(-55);
            int minutes = TestLibrary.Generator.GetInt32(-55);
            int seconds = TestLibrary.Generator.GetInt32(-55);

            retVal = VerificationHelper(hours, minutes, seconds, "001.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor with all value set to 0");

        try
        {
            retVal = VerificationHelper(0, 0, 0, "002.1") && retVal;
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when The parameters specify a TimeSpan value less than MinValue");

        try
        {
            TimeSpan ts = new TimeSpan(Int32.MinValue, Int32.MinValue, Int32.MinValue);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown when The parameters specify a TimeSpan value less than MinValue");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException should be thrown when The parameters specify a TimeSpan value less than MaxValue");

        try
        {
            TimeSpan ts = new TimeSpan(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentOutOfRangeException is not thrown when The parameters specify a TimeSpan value less than MaxValue");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TimeSpanCtor1 test = new TimeSpanCtor1();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanCtor1");

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
    private bool VerificationHelper(int hours, int minutes, int seconds, string errorNo)
    {
        bool retVal = true;

        TimeSpan ts = new TimeSpan(hours, minutes, seconds);

        long desiredTicks = hours * TimeSpan.TicksPerHour + minutes * TimeSpan.TicksPerMinute + seconds * TimeSpan.TicksPerSecond;
        long actualTicks = ts.Ticks;

        if (desiredTicks != actualTicks)
        {
            TestLibrary.TestFramework.LogError(errorNo + ".1", "Ticks of the instance contructed by ctor(int, int, int) is unexpected");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE]  hours = " + hours + 
                                                                                                                ", minutes = " + minutes + 
                                                                                                                ", seconds = " + seconds +
                                                                                                                ", desiredTicks = " + desiredTicks + 
                                                                                                                ", actualTicks = " + actualTicks);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
