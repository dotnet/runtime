// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UInt64.System.IConvertible.ToUInt64
/// </summary>
public class UInt64IConvertibleToUInt64
{
    public static int Main()
    {
        UInt64IConvertibleToUInt64 ui64ictuint64 = new UInt64IConvertibleToUInt64();
        TestLibrary.TestFramework.BeginTestCase("UInt64IConvertibleToUInt64");
        if (ui64ictuint64.RunTests())
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

        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:UInt64 MinValue to UInt64");
        try
        {
            UInt64 uintA = UInt64.MinValue;
            IConvertible iConvert = (IConvertible)(uintA);
            UInt64 uint64A = iConvert.ToUInt64(null);
            if (uint64A != uintA)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:UInt64 MaxValue to UInt64");
        try
        {
            UInt64 uintA = UInt64.MaxValue;
            IConvertible iConvert = (IConvertible)(uintA);
            UInt64 uint64A = iConvert.ToUInt64(null);
            if (uint64A != uintA)
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

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert a random Uint64 to UInt64");
        try
        {
            UInt64 uintA = this.GetInt64(1, UInt64.MaxValue); ;
            IConvertible iConvert = (IConvertible)(uintA);
            UInt64 uint64A = iConvert.ToUInt64(null);
            if (uint64A != uintA)
            {
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
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
