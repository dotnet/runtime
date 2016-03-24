// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
/// <summary>
/// Int64.System.IConvertible.ToInt16()
/// </summary>
public class Int64IConvertibleToInt16
{
    public static int Main()
    {
        Int64IConvertibleToInt16 ui64IContInt16 = new Int64IConvertibleToInt16();
        TestLibrary.TestFramework.BeginTestCase("Int64IConvertibleToInt16");
        if (ui64IContInt16.RunTests())
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
        retVal = PosTest4() && retVal;
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:The Int64 value which in the range of Int16 IConvertible To Int16 1");
        try
        {
            long int64A = Int16.MaxValue;
            IConvertible iConvert = (IConvertible)(int64A);
            short int16A = iConvert.ToInt16(provider);
            if (int16A != int64A)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:The Int64 value which in the range of Int16 IConvertible To Int16 2");
        try
        {
            long int64A = Int16.MinValue;
            IConvertible iConvert = (IConvertible)(int64A);
            short int16A = iConvert.ToInt16(provider);
            if (int16A != int64A)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:The Int64 value which in the range of Int16 IConvertible To Int16 3");
        try
        {
            long int64A = this.GetInt32(0, Int16.MaxValue);
            IConvertible iConvert = (IConvertible)(int64A);
            short int16A = iConvert.ToInt16(null);
            if (int16A != int64A)
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
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:The Int64 value which in the range of Int16 IConvertible To Int16 4");
        try
        {
            long int64A = (this.GetInt32(0, Int16.MaxValue) + 1) * (-1);
            IConvertible iConvert = (IConvertible)(int64A);
            short int16A = iConvert.ToInt16(null);
            if (int16A != int64A)
            {
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
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
        TestLibrary.TestFramework.BeginScenario("NegTest1: The current Int64 value is out of Int16 range 1");
        try
        {
            long int64A = (long)Int16.MaxValue + (long)(this.GetInt32(1, Int16.MaxValue));
            IConvertible iConvert = (IConvertible)(int64A);
            short int16A = iConvert.ToInt16(provider);
            retVal = false;
            TestLibrary.TestFramework.LogError("N001", "The current Int64 value is out of Int16 range but not throw exception");
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
        TestLibrary.TestFramework.BeginScenario("NegTest2: The current Int64 value is out of Int16 range 2");
        try
        {
            long int64A = (long)Int16.MinValue + (long)(this.GetInt32(1, Int16.MaxValue) + 1) * (-1);
            IConvertible iConvert = (IConvertible)(int64A);
            short int16A = iConvert.ToInt16(null);
            retVal = false;
            TestLibrary.TestFramework.LogError("N003", "The current Int64 value is out of Int16 range but not throw exception");
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
