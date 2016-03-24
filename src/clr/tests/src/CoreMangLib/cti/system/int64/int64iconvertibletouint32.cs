// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
/// <summary>
/// Int64.System.IConvertible.ToUInt32()
/// </summary>
public class Int64IConvertibleToUInt32
{
    public static int Main()
    {
        Int64IConvertibleToUInt32 ui64IContUInt32 = new Int64IConvertibleToUInt32();
        TestLibrary.TestFramework.BeginTestCase("Int64IConvertibleToUInt32");
        if (ui64IContUInt32.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[PosTest]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        TestLibrary.TestFramework.LogInformation("[NegTest]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1:The Int64 value which in the range of UInt32 IConvertible To UInt32 1");
        try
        {
            long int64A = UInt32.MaxValue;
            IConvertible iConvert = (IConvertible)(int64A);
            uint uint32A = iConvert.ToUInt32(provider);
            if (uint32A != int64A)
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
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest2:The Int64 value which in the range of UInt32 IConvertible To UInt32 2");
        try
        {
            long int64A = this.GetInt32(0, Int32.MaxValue);
            IConvertible iConvert = (IConvertible)(int64A);
            uint uint32A = iConvert.ToUInt32(provider);
            if (uint32A != int64A)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:The Int64 value which in the range of UInt32 IConvertible To UInt32 3");
        try
        {
            long int64A = this.GetInt32(0, Int32.MaxValue);
            IConvertible iConvert = (IConvertible)(int64A);
            uint uint32A = iConvert.ToUInt32(null);
            if (uint32A != int64A)
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
    public bool NegTest1()
    {
        bool retVal = true;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("NegTest1: The current Int64 value is out of UInt32 range 1");
        try
        {
            long int64A = UInt32.MaxValue + this.GetInt32(1, Int32.MaxValue);
            IConvertible iConvert = (IConvertible)(int64A);
            uint uint32A = iConvert.ToUInt32(provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N001", "The current Int64 value is out of UInt32 range but not throw exception");
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: The current Int64 value is out of UInt32 range 2");
        try
        {
            long int64A = (this.GetInt32(0,Int32.MaxValue) + 1) * (-1);
            IConvertible iConvert = (IConvertible)(int64A);
            uint uint32A = iConvert.ToUInt32(null);
            retVal = false;
            TestLibrary.TestFramework.LogError("N003", "The current Int64 value is out of UInt32 range but not throw exception");
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpected exception: " + e);
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
