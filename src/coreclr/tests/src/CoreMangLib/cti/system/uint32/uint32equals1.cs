// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UInt32.Equals(System.Object)
/// </summary>
public class UInt32Equals1
{
    public static int Main()
    {
        UInt32Equals1 ui32e1 = new UInt32Equals1();
        TestLibrary.TestFramework.BeginTestCase("UInt32Equals1");

        if (ui32e1.RunTests())
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        return retVal;
    }
    #region
    public bool PosTest1()
    {
        bool retVal = true;
        bool ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest1: object value is null");
        try
        {
            UInt32 uintA = (UInt32)this.GetInt32(0, Int32.MaxValue);
            object comValue = null;
            ActualResult = uintA.Equals(comValue);
            if (ActualResult)
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
        bool ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest2: object value is declaring class");
        try
        {
            UInt32 uintA = (UInt32)this.GetInt32(0, Int32.MaxValue);
            object comValue = new MyTest();
            ActualResult = uintA.Equals(comValue);
            if (ActualResult)
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
        bool ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest3: object value is the instance value but the types are different");
        try
        {
            int comValue = this.GetInt32(0, Int32.MaxValue);
            UInt32 uintA = (UInt32)comValue;
            ActualResult = uintA.Equals(comValue);
            if (ActualResult)
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
        bool ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest4: object and the instance have the same type but different value");
        try
        {
            int intA = this.GetInt32(0,Int32.MaxValue);
            UInt32 uintA = (UInt32)intA;
            UInt32 comValue = (UInt32)(intA + 1);
            ActualResult = uintA.Equals(comValue);
            if (ActualResult)
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
    public bool PosTest5()
    {
        bool retVal = true;
        bool ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest5: object and the instance have the same type and the same value 1");
        try
        {
            UInt32 uintA = 0xffffffff;
            UInt32 comValue = UInt32.MaxValue;
            ActualResult = uintA.Equals(comValue);
            if (!ActualResult)
            {
                TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        bool ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest6: object and the instance have the same type and the same value 2");
        try
        {
            UInt32 uintA = 0;
            UInt32 comValue = UInt32.MinValue;
            ActualResult = uintA.Equals(comValue);
            if (!ActualResult)
            {
                TestLibrary.TestFramework.LogError("011", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region ForTestObject
    public class MyTest { }
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
