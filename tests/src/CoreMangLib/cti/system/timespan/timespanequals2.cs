// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TimeSpanEquals2
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
        long randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Equals should return true when compare with self instance");

        try
        {
            randValue = TestLibrary.Generator.GetInt64(-55);
            TimeSpan ts = new TimeSpan(randValue);

            if (!ts.Equals(ts))
            {
                TestLibrary.TestFramework.LogError("001.1", "Equals does not return true when compare with self instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts.Ticks = " + ts.Ticks + ", randValue = " + randValue);
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
        long randValue = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Equals should return true when compare with equal instance");

        try
        {
            randValue = TestLibrary.Generator.GetInt64(-55);
            TimeSpan ts1 = new TimeSpan(randValue);
            TimeSpan ts2 = new TimeSpan(randValue);

            if (!ts1.Equals(ts2))
            {
                TestLibrary.TestFramework.LogError("002.1", "Equals does not return true when compare with self instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts1.Ticks = " + ts1.Ticks + ", ts2.Ticks = " + ts2.Ticks + ", randValue = " + randValue);
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
        long randValue1 = 0;
        long randValue2 = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Equals should return false when compare with not equal instance");

        try
        {
            randValue1 = TestLibrary.Generator.GetInt64(-55);
            do
            {
                randValue2 = TestLibrary.Generator.GetInt64(-55);
            } while (randValue2 == randValue1);

            TimeSpan ts1 = new TimeSpan(randValue1);
            TimeSpan ts2 = new TimeSpan(randValue2);

            if (ts1.Equals(ts2))
            {
                TestLibrary.TestFramework.LogError("003.1", "Equals does not return false when compare with not equal instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] ts1.Ticks = " + ts1.Ticks + ", ts2.Ticks = " + ts2.Ticks + ", randValue1 = " + randValue1 + ", randValue2 = " + randValue2);
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
    #endregion

    public static int Main()
    {
        TimeSpanEquals2 test = new TimeSpanEquals2();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanEquals2");

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
