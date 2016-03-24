// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt32(Int64)
/// </summary>
public class ConvertToUInt329
{
    public static int Main()
    {
        ConvertToUInt329 convertToUInt329 = new ConvertToUInt329();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt329");
        if (convertToUInt329.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert to UInt32 from int64 1");
        try
        {
            Int64 Int64Val = (Int64)UInt32.MaxValue;
            uint uintVal = Convert.ToUInt32(Int64Val);
            if (uintVal != UInt32.MaxValue)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert to UInt32 from int64 2");
        try
        {
            Int64 Int64Val = (Int64)this.GetInt32(0, Int32.MaxValue) + (Int64)this.GetInt32(0, Int32.MaxValue);
            uint uintVal = Convert.ToUInt32(Int64Val);
            if (uintVal != (uint)Int64Val)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("NegTest1: the int64 is less than UInt32 minValue");
        try
        {
            Int64 Int64Val = this.GetInt32(1, Int32.MaxValue) * (-1);
            uint uintVal = Convert.ToUInt32(Int64Val);
            TestLibrary.TestFramework.LogError("N001", "the int64 is less than UInt32 minValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
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
        TestLibrary.TestFramework.BeginScenario("NegTest2: the int64 is larger than UInt32 maxValue");
        try
        {
            Int64 Int64Val = (Int64)UInt32.MaxValue + (Int64)this.GetInt32(1, Int32.MaxValue);
            uint uintVal = Convert.ToUInt32(Int64Val);
            TestLibrary.TestFramework.LogError("N003", "the int64 is greater than UInt32 maxValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
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
