// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// GetHashCode
/// </summary>
public class TimeSpanGetHashCode
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: GetHashCode should always return the same value for same TimeSpan instance");

        try
        {
            long randValue = TestLibrary.Generator.GetInt64(-55);
            TimeSpan ts = new TimeSpan(randValue);

            int hash1 = ts.GetHashCode();
            int hash2 = ts.GetHashCode();

            if (hash1 != hash2)
            {
                TestLibrary.TestFramework.LogError("001.1", "GetHashCode not always return the same value for same TimeSpan instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] hash1 = " + hash1 + ", hash2 = " + hash2 + ", randValue = " + randValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: GetHashCode should always return the same value for equal TimeSpan instance");

        try
        {
            long randValue = TestLibrary.Generator.GetInt64(-55);
            TimeSpan ts1 = new TimeSpan(randValue);
            TimeSpan ts2 = new TimeSpan(randValue);

            int hash1 = ts1.GetHashCode();
            int hash2 = ts2.GetHashCode();

            if (hash1 != hash2)
            {
                TestLibrary.TestFramework.LogError("002.1", "GetHashCode not always return the same value for equal TimeSpan instance");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] hash1 = " + hash1 + ", hash2 = " + hash2 + ", randValue = " + randValue);
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
    #endregion
    #endregion

    public static int Main()
    {
        TimeSpanGetHashCode test = new TimeSpanGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("TimeSpanGetHashCode");

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
