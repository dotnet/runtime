// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

/// <summary>
/// UInt32.Parse(System.string,NumberStyle style,IFormatProvider provider)
/// </summary>
public class UInt32Parse3
{
    public static int Main()
    {
        UInt32Parse3 ui32parse3 = new UInt32Parse3();
        TestLibrary.TestFramework.BeginTestCase("UInt32Parse3");

        if (ui32parse3.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        UInt32 uintA;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1: the string corresponding UInt32 is UInt32 MaxValue 1");
        try
        {
            string strA = UInt32.MaxValue.ToString();
            NumberStyles style = NumberStyles.Any;
            uintA = UInt32.Parse(strA, style,provider);
            if (uintA != UInt32.MaxValue)
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
        UInt32 uintA;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest2: the string corresponding UInt32 is UInt32 MaxValue 2");
        try
        {
            string strA = "ffffffff";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt32.Parse(strA, style,provider);
            if (uintA != UInt32.MaxValue)
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
        UInt32 uintA;
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest3: The string parameter contains a number of the form:[ws][sign]digits[ws]");
        try
        {
            string strA = "\u0009" + "abcde" + "\u000A";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt32.Parse(strA, style, provider);
            UInt32 expUIntA = (UInt32)(10 * Math.Pow(16, 4)) + (UInt32)(11 * Math.Pow(16, 3)) + (UInt32)(12 * Math.Pow(16, 2)) + (UInt32)(13 * Math.Pow(16, 1)) + 14;
            if (uintA != expUIntA)
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
        UInt32 uintA;
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest4: The string parameter contains a number of the form:[ws]hexdigits[ws]");
        try
        {
            string strA = "fffff";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt32.Parse(strA, style, provider);
            UInt32 expUIntA = (UInt32)(15 * Math.Pow(16, 4)) + (UInt32)(15 * Math.Pow(16, 3)) + (UInt32)(15 * Math.Pow(16, 2)) + (UInt32)(15 * Math.Pow(16, 1)) + 15;
            if (uintA != expUIntA)
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
        UInt32 uintA; 
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the parameter string is null");

        try
        {
            string strA = null;
            uintA = UInt32.Parse(strA, NumberStyles.Integer,provider);
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        UInt32 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest2: the style is not the NumberStyles value");

        try
        {
            string strA = "12345";
            NumberStyles numstyle = (NumberStyles)(-1);
            uintA = UInt32.Parse(strA, numstyle, provider);
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest3()
    {
        bool retVal = true;
        UInt32 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the string is not in a format compliant with style 1");

        try
        {
            string strA = "abcd";
            NumberStyles style = NumberStyles.Integer;
            uintA = UInt32.Parse(strA, style, provider);
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest4()
    {
        bool retVal = true;
        UInt32 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the string is not in a format compliant with style 2");

        try
        {
            string strA = "gabcd";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt32.Parse(strA, style, provider);
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest5()
    {
        bool retVal = true;
        UInt32 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest5: the parameter string corresponding number is less than UInt32 minValue");

        try
        {
            Int32 Testint = (-1) * this.GetInt32(1, Int32.MaxValue);
            string strA = Testint.ToString();
            NumberStyles style = NumberStyles.Integer;
            uintA = UInt32.Parse(strA, style, provider);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N005", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest6()
    {
        bool retVal = true;
        UInt32 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest6: the parameter string corresponding number is larger than UInt32 maxValue");

        try
        {
            UInt32 uinta = UInt32.MaxValue;
            UInt32 uintb = (UInt32)this.GetInt32(1, Int32.MaxValue);
            UInt64 TestUint = (UInt64)uinta + (UInt64)uintb;
            string strA = TestUint.ToString();
            NumberStyles style = NumberStyles.Integer;
            uintA = UInt32.Parse(strA, style, provider);
            retVal = false;
        }
        catch (OverflowException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest7()
    {
        bool retVal = true;
        UInt32 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest7: The string parameter contains a number of the form:[ws][sign]hexdigits[ws]");
        try
        {
            string strA = "\u0009" + "+" + "abcde" + "\u0009";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt32.Parse(strA, style, provider);
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N007", "Unexpected exception: " + e);
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
