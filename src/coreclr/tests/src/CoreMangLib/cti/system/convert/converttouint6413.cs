// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt64(Single)
/// </summary>
public class ConvertToUInt6413
{
    public static int Main()
    {
        ConvertToUInt6413 convertToUInt6413 = new ConvertToUInt6413();
        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt6413");
        if (convertToUInt6413.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Convert To UInt64 from Single 1");
        try
        {
            float singleVal = (float)TestLibrary.Generator.GetInt64(-55);
            ulong ulongVal = Convert.ToUInt64(singleVal);
            if (ulongVal != (ulong)singleVal)
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Convert To UInt64 from Single 2");
        try
        {
            float singleVal1 = 1.5F;
            float singleVal2 = 1.1F;
            float singleVal3 = 1.9F;
            ulong ulongVal1 = Convert.ToUInt64(singleVal1);
            ulong ulongVal2 = Convert.ToUInt64(singleVal2);
            ulong ulongVal3 = Convert.ToUInt64(singleVal3);
            if (ulongVal1 != 2 || ulongVal2 != 1 || ulongVal3 != 2)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Convert To UInt64 from Single 3");
        try
        {
            float singleVal1 = 2.5F;
            float singleVal2 = 2.1F;
            float singleVal3 = 2.9F;
            ulong ulongVal1 = Convert.ToUInt64(singleVal1);
            ulong ulongVal2 = Convert.ToUInt64(singleVal2);
            ulong ulongVal3 = Convert.ToUInt64(singleVal3);
            if (ulongVal1 != 2 || ulongVal2 != 2 || ulongVal3 != 3)
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
        TestLibrary.TestFramework.BeginScenario("NegTest1:The Single is out of the range of UInt64 1");
        try
        {
            float singleVal = (float)this.GetInt32(1, Int32.MaxValue) + (float)(UInt64.MaxValue);
            ulong ulongVal = Convert.ToUInt64(singleVal);
            TestLibrary.TestFramework.LogError("N001", "The Single is out of the range of UInt64 but not throw exception");
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
        TestLibrary.TestFramework.BeginScenario("NegTest2:The Single is out of the range of UInt64 2");
        try
        {
            float singleVal = (float)(this.GetInt32(1, Int32.MaxValue) * (-1));
            ulong ulongVal = Convert.ToUInt64(singleVal);
            TestLibrary.TestFramework.LogError("N003", "The Single is out of the range of UInt64 but not throw exception");
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
