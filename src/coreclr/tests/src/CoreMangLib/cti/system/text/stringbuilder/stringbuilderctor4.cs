// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
/// <summary>
/// StringBuilder.ctor(Int32,Int32)
/// </summary>
public class StringBuilderctor4
{
    public static int Main()
    {
        StringBuilderctor4 sbctor4 = new StringBuilderctor4();
        TestLibrary.TestFramework.BeginTestCase("StringBuilderctor4");
        if (sbctor4.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize StringBuilder with capacity and maxcapacity 1");
        try
        {
            StringBuilder sb = new StringBuilder(0, 1);
            if (sb == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "StringBuilder(0, 1) should not be null");
                retVal = false;
            }
            else if (sb.Length != 0)
            {
                TestLibrary.TestFramework.LogError("001.2", "StringBuilder(0, 1).Length should be 0, is " + sb.Length.ToString());
                retVal = false;
            }
            else if (sb.Capacity > 1)
            {
                TestLibrary.TestFramework.LogError("001.3", "StringBuilder(0, 1).Capacity should not exceed 1, is " + sb.Capacity.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize StringBuilder with capacity and maxcapacity 2");
        try
        {
            int capacity = this.GetInt32(0, 256);
            int maxcapacity = Int32.MaxValue;
            StringBuilder sb = new StringBuilder(capacity, maxcapacity);
            if (sb == null)
            {
                TestLibrary.TestFramework.LogError("003.1", "StringBuilder(" + capacity.ToString() + ", " + maxcapacity.ToString() + ") should not be null");
                retVal = false;
            }
            else if (sb.Capacity < capacity)
            {
                TestLibrary.TestFramework.LogError("003.2", "StringBuilder(" + capacity.ToString() + ", " + maxcapacity.ToString() + ").Capacity == " + sb.Capacity.ToString());
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:Initialize StringBuilder with capacity which is less than 0");
        try
        {
            int capacity = this.GetInt32(1, Int32.MaxValue) * (-1);
            int maxcapacity = this.GetInt32(1, Int32.MaxValue);
            StringBuilder sb = new StringBuilder(capacity, maxcapacity);
            TestLibrary.TestFramework.LogError("N001", "capacity is less than 0 but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2:Initialize StringBuilder with maxcapacity which is less than 1");
        try
        {
            int capacity = 0;
            int maxcapacity = this.GetInt32(0, Int32.MaxValue) * (-1);
            StringBuilder sb = new StringBuilder(capacity, maxcapacity);
            TestLibrary.TestFramework.LogError("N003", "maxcapacity is less than 1 but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3:Initialize StringBuilder which capacity is greater than maxcapacity");
        try
        {
            int maxcapacity = this.GetInt32(1, Int32.MaxValue);
            int capacity = maxcapacity + this.GetInt32(1, Int32.MaxValue);
            StringBuilder sb = new StringBuilder(capacity, maxcapacity);
            TestLibrary.TestFramework.LogError("N005", "maxcapacity is less than capacity but not throw exception");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region HelpMethod
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
