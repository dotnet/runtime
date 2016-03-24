// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// SByte.CompareTo(SByte)
/// </summary>
public class SByteCompareTo2
{
    public static int Main()
    {
        SByteCompareTo2 sbyteCT2 = new SByteCompareTo2();
        TestLibrary.TestFramework.BeginTestCase("SByteCompareTo2");
        if (sbyteCT2.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: The sbyte compared to is less than the source");
        try
        {
            int intA = this.GetInt32(0, 127) + this.GetInt32(0, 129) * (-1);
            sbyte desByte = (sbyte)intA;
            sbyte sourceByte = sbyte.MaxValue;
            int retInt = sourceByte.CompareTo(desByte);
            if (retInt <= 0 )
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: The sbyte compared to is larger than the source");
        try
        {
            int intA = this.GetInt32(0, 127) + this.GetInt32(0, 128) * (-1);
            sbyte desByte = (sbyte)intA;
            sbyte sourceByte = sbyte.MinValue;
            int retInt = sourceByte.CompareTo(desByte);
            if (retInt >= 0)
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
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: The sbyte compared to is equal to the source");
        try
        {
            int intA = this.GetInt32(0, 127) + this.GetInt32(0, 128) * (-1);
            sbyte desByte = (sbyte)intA;
            sbyte sourceByte = (sbyte)intA;
            int retInt = sourceByte.CompareTo(desByte);
            if (retInt != 0)
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is not the ActualResult");
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
