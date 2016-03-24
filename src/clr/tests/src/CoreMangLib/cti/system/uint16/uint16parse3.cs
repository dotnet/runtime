// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// UInt16.Parse(String, NumberStyles, IFormatProvider)
/// </summary>
public class UInt16Parse
{
    public static int Main()
    {
        UInt16Parse ui32ct2 = new UInt16Parse();
        TestLibrary.TestFramework.BeginTestCase("for method: UInt16.Parse(String, NumberStyles, IFormatProvider)");

        if (ui32ct2.RunTests())
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
        retVal = PosTest5() && retVal;

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
        string errorDesc;

        UInt16 actualValue;
        NumberStyles numberStyle;
        CultureInfo culture;
        IFormatProvider provider;

        numberStyle = NumberStyles.Integer;
        string strValue = UInt16.MaxValue.ToString();
        strValue = "  " + strValue + "  ";
        culture = CultureInfo.InvariantCulture;
        provider = culture.NumberFormat;

        TestLibrary.TestFramework.BeginScenario("PosTest1: UInt16.MaxValue, number style is NumberStyles.Integer.");
        try
        {
            actualValue = UInt16.Parse(strValue, numberStyle, provider);
            if (actualValue != UInt16.MaxValue)
            {
                errorDesc = "The parse value of " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat + 
                                 " is not the value " + UInt16.MaxValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc = "\nThe string representation is " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        UInt16 actualValue;
        NumberStyles numberStyle;
        CultureInfo culture;
        IFormatProvider provider;

        string strValue = UInt16.MinValue.ToString();
        numberStyle = NumberStyles.None;
        culture = CultureInfo.InvariantCulture;
        provider = culture.NumberFormat;

        TestLibrary.TestFramework.BeginScenario("PosTest2: UInt16.MinValue, number style is NumberStyles.None.");
        try
        {
            actualValue = UInt16.Parse(strValue, numberStyle,provider);
            if (actualValue != UInt16.MinValue)
            {
                errorDesc = "The parse value of " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat +
                                 " is not the value " + UInt16.MaxValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc = "\nThe string representation is " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        UInt16 expectedValue;
        UInt16 actualValue;
        NumberStyles numberStyle;
        CultureInfo culture;
        IFormatProvider provider;

        expectedValue = (UInt16)this.GetInt32(0, UInt16.MaxValue);
        string strValue = expectedValue.ToString("x");
        numberStyle = NumberStyles.HexNumber;
        culture = CultureInfo.InvariantCulture;
        provider = culture.NumberFormat;

        TestLibrary.TestFramework.BeginScenario("PosTest3: random hexadecimal UInt16 value between 0 and UInt16.MaxValue");
        try
        {
            actualValue = UInt16.Parse(strValue, numberStyle,provider);
            if (actualValue != expectedValue)
            {
                errorDesc = "The parse value of " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat +
                                 " is not the value " + UInt16.MaxValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc = "\nThe string representation is " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string errorDesc;

        UInt16 expectedValue;
        UInt16 actualValue;
        NumberStyles numberStyle;
        CultureInfo culture;
        IFormatProvider provider;
        string strValue;

        expectedValue = (UInt16)this.GetInt32(0, UInt16.MaxValue);
        strValue = expectedValue.ToString("c");
        numberStyle = NumberStyles.Currency;
        culture = CultureInfo.CurrentCulture;
        provider = culture.NumberFormat;

        TestLibrary.TestFramework.BeginScenario("PosTest4: random currency UInt16 value between 0 and UInt16.MaxValue");
        try
        {
            actualValue = UInt16.Parse(strValue, numberStyle,provider);
            if (actualValue != expectedValue)
            {
                errorDesc = "The parse value of " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat +
                                 " is not the value " + UInt16.MaxValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("007", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc = "\nThe string representation is " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat;
            TestLibrary.TestFramework.LogError("008", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string errorDesc;

        UInt16 expectedValue;
        UInt16 actualValue;
        NumberStyles numberStyle;
        CultureInfo culture;
        IFormatProvider provider;

        expectedValue = (UInt16)this.GetInt32(0, UInt16.MaxValue);
        string strValue = expectedValue.ToString("f");
        numberStyle = NumberStyles.Any;
        culture = CultureInfo.InvariantCulture;
        provider = culture.NumberFormat;

        TestLibrary.TestFramework.BeginScenario("PosTest5: random UInt16 value between 0 and UInt16.MaxValue, number styles is NumberStyels.Any");
        try
        {
            actualValue = UInt16.Parse(strValue, numberStyle);
            if (actualValue != expectedValue)
            {
                errorDesc = "The parse value of " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat +
                                 " is not the value " + UInt16.MaxValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("009", "the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc = "\nThe string representation is " + strValue + " with number styles " +
                                 numberStyle + " and format " + culture.NumberFormat;
            TestLibrary.TestFramework.LogError("010", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Negative tests
    //ArgumentNullException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: string representation is a null reference";
        string errorDesc;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(null, NumberStyles.Integer, null);
            errorDesc = "ArgumentNullException is not thrown as expected.";
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;

        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //FormatException
    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: String representation is not in the correct format";
        string errorDesc;

        string strValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strValue = "Incorrect";
            UInt16.Parse(strValue, NumberStyles.Integer, null);
            errorDesc = "FormatException is not thrown as expected.";
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: String representation does not match the correct number style";
        string errorDesc;

        string strValue;
        strValue = this.GetInt32(0, UInt16.MaxValue).ToString("c");
        NumberStyles numberStyle = NumberStyles.None;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(strValue, numberStyle, null);

            errorDesc = "FormatException is not thrown as expected.";

            errorDesc += string.Format("\nString representation is {0}, number styles is {1}", 
                                                    strValue, 
                                                    numberStyle);

            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                                        strValue,
                                        numberStyle);
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //OverflowException
    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "N004";
        const string c_TEST_DESC = "NegTest4: String representation is greater than UInt16.MaxValue";
        string errorDesc;

        string strValue;
        int i;

        i = this.GetInt32(UInt16.MaxValue + 1, int.MaxValue);
        strValue = i.ToString();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(strValue, NumberStyles.None, null);
            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                            strValue,
                            NumberStyles.None);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                            strValue,
                            NumberStyles.None);
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "N005";
        const string c_TEST_DESC = "NegTest5: String representation is less than UInt16.MaxValue";
        string errorDesc;

        string strValue;
        int i;

        i = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        strValue = i.ToString();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(strValue, NumberStyles.Integer, null);
            errorDesc = "OverflowException is not thrown as expected.";
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                            strValue,
                            NumberStyles.Integer);
            TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                                        strValue,
                                        NumberStyles.Integer);
            TestLibrary.TestFramework.LogError("018" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    //ArgumentException
    public bool NegTest6()
    {
        bool retVal = true;

        const string c_TEST_ID = "N006";
        const string c_TEST_DESC = "NegTest6: style is not a NumberStyles value.";
        string errorDesc;

        string strValue;
        UInt16 i;

        i = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        strValue = i.ToString();
        NumberStyles numberStyle = (NumberStyles)(0x204 + TestLibrary.Generator.GetInt16(-55));

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(strValue, numberStyle, null);

            errorDesc = "ArgumentException is not thrown as expected.";
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                            strValue,
                            numberStyle);
            TestLibrary.TestFramework.LogError("019" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                            strValue,
                            numberStyle);
            TestLibrary.TestFramework.LogError("020" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;

        const string c_TEST_ID = "N007";
        const string c_TEST_DESC = "NegTest7: style is not a combination of AllowHexSpecifier and HexNumber values.";
        string errorDesc;

        string strValue;
        UInt16 i;

        i = (UInt16)(TestLibrary.Generator.GetInt32(-55) % (UInt16.MaxValue + 1));
        strValue = i.ToString();
        NumberStyles numberStyle = (NumberStyles)(NumberStyles.AllowHexSpecifier |
                                                                       NumberStyles.AllowCurrencySymbol);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            UInt16.Parse(strValue, numberStyle, null);

            errorDesc = "ArgumentException is not thrown as expected.";
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                            strValue,
                            numberStyle);
            TestLibrary.TestFramework.LogError("021" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += string.Format("\nString representation is {0}, number styles is {1}",
                            strValue,
                            numberStyle);
            TestLibrary.TestFramework.LogError("022" + " TestId-" + c_TEST_ID, errorDesc);
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
