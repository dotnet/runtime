// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt16(string,int32)
/// </summary>
public class ConvertToUInt1617
{
    public static int Main()
    {
        ConvertToUInt1617 convertToUInt1617 = new ConvertToUInt1617();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt1617");
        if (convertToUInt1617.RunTests())
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
        retVal = PosTest4() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert to UInt16 from string 1");
        try
        {
            string strVal = "1111";
            ushort ushortVal1 = Convert.ToUInt16(strVal, 2);
            ushort ushortVal2 = Convert.ToUInt16(strVal, 8);
            ushort ushortVal3 = Convert.ToUInt16(strVal, 10);
            ushort ushortVal4 = Convert.ToUInt16(strVal, 16);
            if (ushortVal1 != (UInt16)(1*Math.Pow(2,3) + 1*Math.Pow(2,2) + 1*Math.Pow(2,1) + 1*Math.Pow(2,0)) ||
                ushortVal2 != (UInt16)(1 * Math.Pow(8, 3) + 1 * Math.Pow(8, 2) + 1 * Math.Pow(8, 1) + 1 * Math.Pow(8, 0)) ||
                ushortVal3 != (UInt16)(1 * Math.Pow(10, 3) + 1 * Math.Pow(10, 2) + 1 * Math.Pow(10, 1) + 1 * Math.Pow(10, 0)) ||
                ushortVal4 != (UInt16)(1 * Math.Pow(16, 3) + 1 * Math.Pow(16, 2) + 1 * Math.Pow(16, 1) + 1 * Math.Pow(16, 0))
                )
            {
                TestLibrary.TestFramework.LogError("001", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert to UInt16 from string 2");
        try
        {
            int intVal = this.GetInt32(0, (int)UInt16.MaxValue);
            string strVal = "+" + intVal.ToString();
            ushort ushortVal = Convert.ToUInt16(strVal, 10);
            if (ushortVal != (UInt16)intVal)
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert to UInt16 from string 3");
        try
        {
            string strVal = null;
            ushort ushortVal1 = Convert.ToUInt16(strVal, 2);
            ushort ushortVal2 = Convert.ToUInt16(strVal, 8);
            ushort ushortVal3 = Convert.ToUInt16(strVal, 10);
            ushort ushortVal4 = Convert.ToUInt16(strVal, 16);
            if (ushortVal1 != 0 || ushortVal2 != 0 || ushortVal3 != 0 || ushortVal4 != 0)
            {
                TestLibrary.TestFramework.LogError("005", "The ExpectResult is not the ActualResult");
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
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert to UInt16 from string 4");
        try
        {
            string strVal = "0xff";
            ushort ushortVal = Convert.ToUInt16(strVal, 16);
            if (ushortVal != (UInt16)(15 * Math.Pow(16, 1) + 15 * Math.Pow(16, 0)))
            {
                TestLibrary.TestFramework.LogError("007", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the parameter of fromBase is not 2, 8, 10, or 16");
        try
        {
            string strVal = "1111";
            ushort ushortVal = Convert.ToUInt16(strVal, 100);
            TestLibrary.TestFramework.LogError("N001", "the parameter of fromBase is not 2, 8, 10, or 16 but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
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
        TestLibrary.TestFramework.BeginScenario("NegTest2: the string represents a non-base 10 signed number and is prefixed with a negative sign");
        try
        {
            string strVal = "-0";
            ushort ushortVal = Convert.ToUInt16(strVal, 2);
            TestLibrary.TestFramework.LogError("N003", "the string represents a non-base 10 signed number and is prefixed with a negative sign but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the string contains a character that is not a valid digit in the base specified by fromBase param 1");
        try
        {
            string strVal = "1234";
            ushort ushortVal = Convert.ToUInt16(strVal, 2);
            TestLibrary.TestFramework.LogError("N005", "the string contains a character that is not a valid digit in the base specified by fromBase param but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the string contains a character that is not a valid digit in the base specified by fromBase param 2");
        try
        {
            string strVal = "9999";
            ushort ushortVal = Convert.ToUInt16(strVal, 8);
            TestLibrary.TestFramework.LogError("N007", "the string contains a character that is not a valid digit in the base specified by fromBase param but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5: the string contains a character that is not a valid digit in the base specified by fromBase param 3");
        try
        {
            string strVal = "abcd";
            ushort ushortVal = Convert.ToUInt16(strVal, 10);
            TestLibrary.TestFramework.LogError("N009", "the string contains a character that is not a valid digit in the base specified by fromBase param but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest6: the string contains a character that is not a valid digit in the base specified by fromBase param 4");
        try
        {
            string strVal = "gh";
            ushort ushortVal = Convert.ToUInt16(strVal, 16);
            TestLibrary.TestFramework.LogError("N011", "the string contains a character that is not a valid digit in the base specified by fromBase param but not throw exception");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N012", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest7: the string represents base 10 number is less than UInt16.minValue");
        try
        {
            int intVal = this.GetInt32(1, Int32.MaxValue);
            string strVal = "-" + intVal.ToString();
            ushort ushortVal = Convert.ToUInt16(strVal, 10);
            TestLibrary.TestFramework.LogError("N013", "the string represent base 10 number is less than UInt16.minValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N014", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest8: the string represents base 10 number is greater than UInt16.maxValue");
        try
        {
            int intVal = this.GetInt32((int)UInt16.MaxValue + 1, Int32.MaxValue);
            string strVal = intVal.ToString();
            ushort ushortVal = Convert.ToUInt16(strVal, 10);
            TestLibrary.TestFramework.LogError("N015", "the string represent base 10 number is greater than UInt16.maxValue but not throw exception");
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N016", "Unexpect exception:" + e);
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
