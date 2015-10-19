using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

/// <summary>
/// UInt64.Parse(System.string,NumberStyle style,IFormatProvider provider)
/// </summary>
public class UInt64Parse3
{
    public static int Main()
    {
        UInt64Parse3 ui64parse3 = new UInt64Parse3();
        TestLibrary.TestFramework.BeginTestCase("UInt64Parse3");

        if (ui64parse3.RunTests())
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
        UInt64 uintA;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest1: the string corresponding UInt64 is UInt64 MaxValue 1");
        try
        {
            string strA = UInt64.MaxValue.ToString();
            NumberStyles style = NumberStyles.Any;
            uintA = UInt64.Parse(strA, style, provider);
            if (uintA != UInt64.MaxValue)
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
        UInt64 uintA;
        CultureInfo myculture = new CultureInfo("en-us");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest2: the string corresponding UInt64 is UInt64 MaxValue 2");
        try
        {
            string strA = "FFFFFFFFFFFFFFFF";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt64.Parse(strA, style, provider);
            if (uintA != UInt64.MaxValue)
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
        UInt64 uintA;
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest3: The string parameter contains a number of the form:[ws][sign]digits[ws]");
        try
        {
            string strA = "\u0009" + "abcde" + "\u000A";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt64.Parse(strA, style, provider);
            UInt64 expUIntA = (UInt64)(10 * Math.Pow(16, 4)) + (UInt64)(11 * Math.Pow(16, 3)) + (UInt64)(12 * Math.Pow(16, 2)) + (UInt64)(13 * Math.Pow(16, 1)) + 14;
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
        UInt64 uintA;
        CultureInfo myculture = new CultureInfo("el-GR");
        IFormatProvider provider = myculture.NumberFormat;
        TestLibrary.TestFramework.BeginScenario("PosTest4: The string parameter contains a number of the form:[ws]hexdigits[ws]");
        try
        {
            string strA = "fffff";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt64.Parse(strA, style, provider);
            UInt64 expUIntA = (UInt64)(15 * Math.Pow(16, 4)) + (UInt64)(15 * Math.Pow(16, 3)) + (UInt64)(15 * Math.Pow(16, 2)) + (UInt64)(15 * Math.Pow(16, 1)) + 15;
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
        UInt64 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the parameter string is null");

        try
        {
            string strA = null;
            uintA = UInt64.Parse(strA, NumberStyles.Integer, provider);
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
        UInt64 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest2: the style is not the NumberStyles value");

        try
        {
            string strA = "12345";
            NumberStyles numstyle = (NumberStyles)(-1);
            uintA = UInt64.Parse(strA, numstyle, provider);
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
        UInt64 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest3: the string is not in a format compliant with style 1");

        try
        {
            string strA = "abcd";
            NumberStyles style = NumberStyles.Integer;
            uintA = UInt64.Parse(strA, style, provider);
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
        UInt64 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest4: the string is not in a format compliant with style 2");

        try
        {
            string strA = "gabcd";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt64.Parse(strA, style, provider);
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
        UInt64 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest5: the parameter string corresponding number is less than UInt64 minValue");

        try
        {
            Int64 Testint = (-1) * Convert.ToInt64(this.GetInt64(1, Int64.MaxValue));
            string strA = Testint.ToString();
            NumberStyles style = NumberStyles.Integer;
            uintA = UInt64.Parse(strA, style, provider);
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
        UInt64 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest6: the parameter string corresponding number is larger than UInt64 maxValue");

        try
        {
            string strA = "18446744073709551616";
            NumberStyles style = NumberStyles.Integer;
            uintA = UInt64.Parse(strA, style, provider);
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
        UInt64 uintA;
        IFormatProvider provider = null;
        TestLibrary.TestFramework.BeginScenario("NegTest7: The string parameter contains a number of the form:[ws][sign]hexdigits[ws]");
        try
        {
            string strA = "\u0009" + "+" + "abcde" + "\u0009";
            NumberStyles style = NumberStyles.HexNumber;
            uintA = UInt64.Parse(strA, style, provider);
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
