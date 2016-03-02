// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// UInt16.CompareTo(System.UInt16)
/// </summary>
public class UInt16CompareTo
{
    public static int Main()
    {
        UInt16CompareTo ui32ct2 = new UInt16CompareTo();
        TestLibrary.TestFramework.BeginTestCase("for method: UInt16.CompareTo(UInt16)");

        if (ui32ct2.RunTests())
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
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest1: UInt16 value is larger than the instance");
        try
        {
            UInt16 uintA = 0;
            UInt16 comValue = (UInt16)this.GetInt32(1, UInt16.MaxValue);
            ActualResult = uintA.CompareTo(comValue);
            if (ActualResult >= 0)
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt16 value is less than the instance");
        try
        {
            UInt16 uintA = 0xffff;
            UInt16 comValue = (UInt16)this.GetInt32(0, UInt16.MaxValue);
            ActualResult = uintA.CompareTo(comValue);
            if (ActualResult <= 0)
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest3: UInt16 value is equal the instance");
        try
        {
            UInt16 uintA = 0xffff;
            UInt16 comValue = UInt16.MaxValue;
            ActualResult = uintA.CompareTo(comValue);
            if (ActualResult != 0)
            {
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        int ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest4: UInt16 value is UInt16 min value");
        try
        {
            UInt32 uintA = 0xffff;
            UInt32 comValue = UInt16.MinValue;
            ActualResult = uintA.CompareTo(comValue);
            if (ActualResult <= 0)
            {
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestObject
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }

    #endregion
}
