// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UInt64.System.IConvertible.ToInt32
/// </summary>
public class UInt64IConvertibleToInt32
{
    public static int Main()
    {
        UInt64IConvertibleToInt32 ui64ictuint32 = new UInt64IConvertibleToInt32();
        TestLibrary.TestFramework.BeginTestCase("UInt64IConvertibleToInt32");
        if (ui64ictuint32.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:UInt64 MinValue to Int32");
        try
        {
            UInt64 uintA = UInt64.MinValue;
            IConvertible iConvert = (IConvertible)(uintA);
            Int32 int32A = iConvert.ToInt32(null);
            if ((ulong)int32A != uintA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:UInt64 number between UInt64.MinValue to Int32.MaxValue");
        try
        {
            UInt64 uintA = this.GetInt64(0, Int32.MaxValue);
            IConvertible iConvert = (IConvertible)(uintA);
            Int32 int32A = iConvert.ToInt32(null);
            if ((ulong)int32A != uintA)
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
        TestLibrary.TestFramework.BeginScenario("NegTest1:UInt64 number between Int32.MaxValue to UInt64.MaxValue");

        try
        {
            UInt64 uintA = this.GetInt64((ulong)(Int32.MaxValue) + 1, UInt64.MaxValue); ;
            IConvertible iConvert = (IConvertible)(uintA);
            Int32 int32A = iConvert.ToInt32(null);
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
    private UInt64 GetInt64(UInt64 minValue, UInt64 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + (UInt64)TestLibrary.Generator.GetInt64(-55) % (maxValue - minValue);
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
