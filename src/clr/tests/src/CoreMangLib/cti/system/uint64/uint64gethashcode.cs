// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.UInt64.GetHashCode
/// </summary>
public class UInt64GetHashCode
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the return hash code not equal to zero");

        try
        {
            UInt64 u64 = this.GetUInt64(0, UInt64.MaxValue);
            int hash = u64.GetHashCode();
            if (hash == 0)
            {
                TestLibrary.TestFramework.LogError("001", "UInt64.gethashcode is equal to zero : " + u64);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check the return hash code is the same");

        try
        {
            UInt64 u64 = this.GetUInt64(0, UInt64.MaxValue);
            int hash1 = u64.GetHashCode();
            int hash2 = u64.GetHashCode();
            if (hash1 != hash2)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected " + u64);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        UInt64GetHashCode test = new UInt64GetHashCode();

        TestLibrary.TestFramework.BeginTestCase("UInt64GetHashCode");

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
    private UInt64 GetUInt64(ulong minValue, ulong maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return (ulong)(minValue + (ulong)TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
}
