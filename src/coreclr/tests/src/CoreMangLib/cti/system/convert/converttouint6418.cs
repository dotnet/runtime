// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt64(String,Int32)
/// </summary>
public class ConvertToUInt6418
{
    public static int Main()
    {
        ConvertToUInt6418 convertToUInt6418 = new ConvertToUInt6418();
        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt6418");
        if (convertToUInt6418.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert to UInt64 from string 1");
        try
        {
            string strVal = "1111";
            ulong ulongVal1 = Convert.ToUInt64(strVal, 2);
            ulong ulongVal2 = Convert.ToUInt64(strVal, 8);
            ulong ulongVal3 = Convert.ToUInt64(strVal, 10);
            ulong ulongVal4 = Convert.ToUInt64(strVal, 16);
            if (ulongVal1 != (UInt64)(1 * Math.Pow(2, 3) + 1 * Math.Pow(2, 2) + 1 * Math.Pow(2, 1) + 1 * Math.Pow(2, 0)) ||
                ulongVal2 != (UInt64)(1 * Math.Pow(8, 3) + 1 * Math.Pow(8, 2) + 1 * Math.Pow(8, 1) + 1 * Math.Pow(8, 0)) ||
                ulongVal3 != (UInt64)(1 * Math.Pow(10, 3) + 1 * Math.Pow(10, 2) + 1 * Math.Pow(10, 1) + 1 * Math.Pow(10, 0)) ||
                ulongVal4 != (UInt64)(1 * Math.Pow(16, 3) + 1 * Math.Pow(16, 2) + 1 * Math.Pow(16, 1) + 1 * Math.Pow(16, 0))
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert to UInt64 from string 2");
        try
        {
            ulong sourceVal = (UInt64)TestLibrary.Generator.GetInt64(-55);
            string strVal = "+" + sourceVal.ToString();
            ulong ulongVal = Convert.ToUInt64(strVal, 10);
            if (ulongVal != sourceVal)
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert to UInt64 from string 3");
        try
        {
            string strVal = null;
            ulong ulongVal1 = Convert.ToUInt64(strVal, 2);
            ulong ulongVal2 = Convert.ToUInt64(strVal, 8);
            ulong ulongVal3 = Convert.ToUInt64(strVal, 10);
            ulong ulongVal4 = Convert.ToUInt64(strVal, 16);
            if (ulongVal1 != 0 || ulongVal2 != 0 || ulongVal3 != 0 || ulongVal4 != 0)
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert to UInt64 from string 4");
        try
        {
            string strVal = "0xff";
            ulong ulongVal = Convert.ToUInt64(strVal, 16);
            if (ulongVal != (UInt64)(15 * Math.Pow(16, 1) + 15 * Math.Pow(16, 0)))
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
            ulong ulongVal = Convert.ToUInt64(strVal, 100);
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
            ulong ulongVal = Convert.ToUInt64(strVal, 2);
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
            ulong ulongVal = Convert.ToUInt64(strVal, 2);
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
            ulong ulongVal = Convert.ToUInt64(strVal, 8);
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
            ulong ulongVal = Convert.ToUInt64(strVal, 10);
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
            ulong ulongVal = Convert.ToUInt64(strVal, 16);
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
        TestLibrary.TestFramework.BeginScenario("NegTest7: the string represents base 10 number is less than UInt64.minValue");
        try
        {
            int intVal = this.GetInt32(1, Int32.MaxValue);
            string strVal = "-" + intVal.ToString();
            ulong ulongVal = Convert.ToUInt64(strVal, 10);
            TestLibrary.TestFramework.LogError("N013", "the string represent base 10 number is less than UInt64.minValue but not throw exception");
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
        TestLibrary.TestFramework.BeginScenario("NegTest8: the string represents base 10 number is greater than UInt64.maxValue");
        try
        {
            string strVal = UInt64.MaxValue.ToString() + "1";
            ulong ulongVal = Convert.ToUInt64(strVal, 10);
            TestLibrary.TestFramework.LogError("N015", "the string represent base 10 number is greater than UInt64.maxValue but not throw exception");
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
