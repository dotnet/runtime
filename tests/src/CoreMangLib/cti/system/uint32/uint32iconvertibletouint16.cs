// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UInt32.System.IConvertible.ToUInt16
/// </summary>
public class UInt32IConvertibleToUInt16
{
    public static int Main()
    {
        UInt32IConvertibleToUInt16 ui32ictuint16 = new UInt32IConvertibleToUInt16();
        TestLibrary.TestFramework.BeginTestCase("UInt32IConvertibleToUInt16");
        if (ui32ictuint16.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:UInt32 MinValue to UInt16");
        try
        {
            UInt32 uintA = UInt32.MinValue;
            IConvertible iConvert = (IConvertible)(uintA);
            UInt16 uint16A = iConvert.ToUInt16(null);
            if (uint16A != uintA)
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:UInt32 num between UInt32.MinValue to UInt16.MaxValue");
        try
        {
            UInt32 uintA = (UInt32)(this.GetInt32(0, UInt16.MaxValue + 1));
            IConvertible iConvert = (IConvertible)(uintA);
            UInt16 uint16A = iConvert.ToUInt16(null);
            if (uint16A != uintA)
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:UInt32 num between UInt16.MaxValue to UInt32.MaxValue");

        try
        {
            UInt32 uintA = (UInt32)(this.GetInt32(UInt16.MaxValue + 1, Int32.MaxValue) + this.GetInt32(0, Int32.MaxValue));
            IConvertible iConvert = (IConvertible)(uintA);
            UInt16 uint16A = iConvert.ToUInt16(null);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpected exception: " + e);
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
