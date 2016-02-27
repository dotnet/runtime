// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt16(Single)
/// </summary>
public class ConvertToUInt1615
{
    public static int Main()
    {
        ConvertToUInt1615 convertToUInt1615 = new ConvertToUInt1615();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt1615");
        if (convertToUInt1615.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Convert To UInt16 from Single 1");
        try
        {
            float singleVal = (float)this.GetInt32(0, (int)UInt16.MaxValue + 1);
            ushort ushortVal = Convert.ToUInt16(singleVal);
            if (ushortVal != (UInt16)singleVal)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Convert To UInt16 from Single 2");
        try
        {
            float singleVal1 = 1.5F;
            float singleVal2 = 1.1F;
            float singleVal3 = 1.9F;
            ushort ushortVal1 = Convert.ToUInt16(singleVal1);
            ushort ushortVal2 = Convert.ToUInt16(singleVal2);
            ushort ushortVal3 = Convert.ToUInt16(singleVal3);
            if (ushortVal1 != 2 || ushortVal2 != 1 || ushortVal3 != 2)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Convert To UInt16 from Single 3");
        try
        {
            float singleVal1 = 2.5F;
            float singleVal2 = 2.1F;
            float singleVal3 = 2.9F;
            ushort ushortVal1 = Convert.ToUInt16(singleVal1);
            ushort ushortVal2 = Convert.ToUInt16(singleVal2);
            ushort ushortVal3 = Convert.ToUInt16(singleVal3);
            if (ushortVal1 != 2 || ushortVal2 != 2 || ushortVal3 != 3)
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1:The Single is out of the range of UInt16 1");
        try
        {
            float singleVal = (float)this.GetInt32((int)UInt16.MaxValue + 1, Int32.MaxValue);
            ushort ushortVal = Convert.ToUInt16(singleVal);
            TestLibrary.TestFramework.LogError("N001", "The Single is out of the range of UInt16 but not throw exception");
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
        TestLibrary.TestFramework.BeginScenario("NegTest2:The Single is out of the range of UInt16 2");
        try
        {
            float singleVal = (float)this.GetInt32(1, (int)UInt16.MaxValue + 1)*(-1);
            ushort ushortVal = Convert.ToUInt16(singleVal);
            TestLibrary.TestFramework.LogError("N003", "The Single is out of the range of UInt16 but not throw exception");
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
