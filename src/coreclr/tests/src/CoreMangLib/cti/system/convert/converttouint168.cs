// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt16(Int64)
/// </summary>
public class ConvertToUInt168
{
    public static int Main()
    {
        ConvertToUInt168 convertToUInt168 = new ConvertToUInt168();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt168");
        if (convertToUInt168.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert to UInt16 from Int64 1");
        try
        {
            Int64 Int64Val = (Int64)UInt16.MaxValue;
            ushort ushortVal = Convert.ToUInt16(Int64Val);
            if (ushortVal != UInt16.MaxValue)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert to UInt16 from Int64 2");
        try
        {
            Int64 Int64Val = (Int64)this.GetInt32(0, (int)UInt16.MaxValue);
            ushort ushortVal = Convert.ToUInt16(Int64Val);
            if (ushortVal != (ushort)Int64Val)
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
        TestLibrary.TestFramework.BeginScenario("NegTest1: the Int64 is larger than UInt16 maxValue");
        try
        {
            Int64 Int64Val = (Int64)this.GetInt32((int)UInt16.MaxValue + 1, Int32.MaxValue) + (Int64)this.GetInt32(0, Int32.MaxValue);
            ushort ushortVal = Convert.ToUInt16(Int64Val);
            TestLibrary.TestFramework.LogError("N001", "the Int64 is larger than UInt16 maxValue but not throw exception");
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
        TestLibrary.TestFramework.BeginScenario("NegTest2: the Int64 is less than UInt16 minValue");
        try
        {
            Int64 Int64Val = ((Int64)(this.GetInt32(1, Int32.MaxValue)) + (Int64)(this.GetInt32(0, Int32.MaxValue))) * (-1);
            ushort ushortVal = Convert.ToUInt16(Int64Val);
            TestLibrary.TestFramework.LogError("N003", "the Int64 is less than UInt16 minValue but not throw exception");
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
