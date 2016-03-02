// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization; //for NumberFormatInfo
using TestLibrary;

/// <summary>
/// UInt16.System.IConvertible.ToString(string, IFormatProvider)
/// Converts the numeric value of this instance to its equivalent string representation 
/// using the specified format and culture-specific format information. 
/// </summary>
public class UInt16ToString
{
    public static int Main()
    {
        UInt16ToString testObj = new UInt16ToString();

        TestLibrary.TestFramework.BeginTestCase("for method: UInt16.System.ToString(string, IFormatProvider)");
        if(testObj.RunTests())
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

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");

        retVal &= DoPosTest("PosTest1: Value is UInt16.MinValue, format is hexadecimal \"X\".", "PostTest1", UInt16.MinValue, "X", "0", CurrentNFI);
        retVal &= DoPosTest("PosTest2: Value is UInt16 integer, format is hexadecimal \"X\".", "PostTest2", 8542, "X", "215E", CurrentNFI);
        retVal &= DoPosTest("PosTest3: Value is UInt16.MaxValue, format is hexadecimal \"X\".", "PostTest3", UInt16.MaxValue, "X", "FFFF", CurrentNFI);
        retVal &= DoPosTest("PosTest4: Value is UInt16.MinValue, format is hexadecimal \"X\".", "PosTest4", UInt16.MinValue, "X", "0", CustomNFI);

        retVal &= DoPosTest("PosTest5: Value is UInt16 integer,  format is general \"G\".", "PosTest5", 5641, "G", "5641", CustomNFI);
        retVal &= DoPosTest("PosTest6: Value is UInt16.MaxValue, format is general \"G\".", "PosTest6", UInt16.MaxValue, "G", "65535", CustomNFI);

        retVal &= DoPosTest("PosTest7: Value is UInt16 integer, format is currency \"C\".", "PosTest7", 8423, "C", "84.23,000USD", CustomNFI);
        retVal &= DoPosTest("PosTest8: Value is UInt16.MaxValue, format is currency \"C\".", "PosTes8", UInt16.MaxValue, "C", "6.55.35,000USD", CustomNFI);
        retVal &= DoPosTest("PosTest9: Value is UInt16.MinValue, format is currency \"C\".", "PosTes9", UInt16.MinValue, "C", "0,000USD", CustomNFI);

        retVal &= DoPosTest("PosTest10: Value is UInt16 integer, format is decimal \"D\".", "PosTest10", 2351, "D", "2351", CustomNFI);
        retVal &= DoPosTest("PosTest11: Value is UInt16.MaxValue integer, format is decimal \"D\".", "PosTest11", UInt16.MaxValue, "D", "65535", CustomNFI);
        retVal &= DoPosTest("PosTest12: Value is UInt16.MinValue integer, format is decimal \"D\".", "PosTest12", UInt16.MinValue, "D", "0", CustomNFI);

        retVal &= DoPosTest("PosTest13: Value is UInt16 integer, format is decimal \"E\".", "PosTest13", 2351, "E", TestLibrary.Utilities.IsWindows ? "2,351000E++003" : "2,351000E3", CustomNFI);
        retVal &= DoPosTest("PosTest14: Value is UInt16.MaxValue integer, format is decimal \"E\".", "PosTest14", UInt16.MaxValue, "E", TestLibrary.Utilities.IsWindows ? "6,553500E++004" : "6,553500E4", CustomNFI);
        retVal &= DoPosTest("PosTest15: Value is UInt16.MinValue integer, format is decimal \"E\".", "PosTest15", UInt16.MinValue, "E", TestLibrary.Utilities.IsWindows ? "0,000000E++000" : "0,000000E0", CustomNFI);

        retVal &= DoPosTest("PosTest16: Value is UInt16 integer, format is decimal \"F\".", "PosTest16", 2341, "F", "2341,000", CustomNFI);

        retVal &= DoPosTest("PosTest17: Value is UInt16 integer, format is decimal \"P\".", "PosTest17", 2341, "P", "234,100,0000~", CustomNFI);

        retVal &= DoPosTest("PosTest18: Value is UInt16 integer, format is decimal \"N\".", "PosTest18", 2341, "N", "23#41,000", CustomNFI);

        retVal &= DoPosTest("PosTest19: Value is UInt16 integer, format is decimal \"N\".", "PosTest19", 2341, null, "2341", CustomNFI);

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }
    #endregion

    #region Helper method for tests
    public bool DoPosTest(string testDesc, string id, UInt16 uintA, string format, String expectedValue, NumberFormatInfo _NFI)
    {
        bool retVal = true;
        string errorDesc;

        string actualValue;

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            actualValue = uintA.ToString(format, _NFI);

            if (actualValue != expectedValue)
            {
                errorDesc =
                    string.Format("The string representation of {0} is not the value {1} as expected: actual({2})",
                    uintA, expectedValue, actualValue);
                errorDesc += "\nThe format info is \"" + ((format == null) ? "null" : format) + "\" speicifed";
                TestLibrary.TestFramework.LogError(id + "_001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe UInt16 integer is " + uintA + ", format info is \"" + format + "\" speicifed.";
            TestLibrary.TestFramework.LogError(id + "_002", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Private Methods
    private NumberFormatInfo CurrentNFI = CultureInfo.CurrentCulture.NumberFormat;
    private NumberFormatInfo nfi = null;

    private NumberFormatInfo CustomNFI
    {
        get
        {
            if (null == nfi)
            {
                nfi = new CultureInfo(CultureInfo.CurrentCulture.Name).NumberFormat;
                //For "G"
                // NegativeSign, NumberDecimalSeparator, NumberDecimalDigits, PositiveSign
                nfi.NegativeSign = "@";                 //Default: "-"
                nfi.NumberDecimalSeparator = ",";       //Default: "."
                nfi.NumberDecimalDigits = 3;            //Default: 2
                nfi.PositiveSign = "++";                //Default: "+"

                //For "E"
                // PositiveSign, NegativeSign, and NumberDecimalSeparator. 
                // If precision specifier is omitted, a default of six digits after the decimal point is used.

                //For "R"
                // NegativeSign, NumberDecimalSeparator and PositiveSign

                //For "X", The result string isn't affected by the formatting information of the current NumberFormatInfo

                //For "C"
                // CurrencyPositivePattern, CurrencySymbol, CurrencyDecimalDigits, CurrencyDecimalSeparator, CurrencyGroupSeparator, CurrencyGroupSizes, NegativeSign and CurrencyNegativePattern
                nfi.CurrencyDecimalDigits = 3;          //Default: 2
                nfi.CurrencyDecimalSeparator = ",";     //Default: ","
                nfi.CurrencyGroupSeparator = ".";       //Default: "."
                nfi.CurrencyGroupSizes = new int[] { 2 };  //Default: new int[]{3}
                nfi.CurrencyNegativePattern = 2;        //Default: 0
                nfi.CurrencyPositivePattern = 1;        //Default: 0
                nfi.CurrencySymbol = "USD";             //Default: "$" 

                //For "D"
                // NegativeSign

                //For "E"
                // PositiveSign, NumberDecimalSeparator and NegativeSign. 
                // If precision specifier is omitted, a default of six digits after the decimal point is used.
                nfi.PositiveSign = "++";                //Default: "+"
                nfi.NumberDecimalSeparator = ",";       //Default: "."

                //For "F"
                // NumberDecimalDigits, and NumberDecimalSeparator and NegativeSign.
                nfi.NumberDecimalDigits = 3;            //Default: 2

                //For "N"
                // NumberGroupSizes, NumberGroupSeparator, NumberDecimalSeparator, NumberDecimalDigits, NumberNegativePattern and NegativeSign.
                nfi.NumberGroupSizes = new int[] { 2 }; //Default: 3
                nfi.NumberGroupSeparator = "#";         //Default: ","

                //For "P"
                // PercentPositivePattern, PercentNegativePattern, NegativeSign, PercentSymbol, PercentDecimalDigits, PercentDecimalSeparator, PercentGroupSeparator and PercentGroupSizes 
                nfi.PercentPositivePattern = 1;         //Default: 0
                nfi.PercentNegativePattern = 2;         //Default: 0
                nfi.PercentSymbol = "~";                //Default: "%"
                nfi.PercentDecimalDigits = 4;           //Default: 2
                nfi.PercentDecimalSeparator = ",";      //Default: "."
                nfi.PercentGroupSizes[0] = 2;           //Default: 3
                nfi.PercentGroupSeparator = ",";
            }
            return nfi;
        }
    }
    #endregion

    #region Negative tests
    //FormatException
    public bool NegTest1()
    {
        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: The format parameter is invalid -- \"R\". ";
        CultureInfo culture = CultureInfo.CurrentCulture;

        return this.DoInvalidFormatTest(c_TEST_ID, c_TEST_DESC, "39", "40", "R", culture);
    }

    public bool NegTest2()
    {
        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: The format parameter is invalid -- \"r\". ";
        CultureInfo culture = CultureInfo.CurrentCulture;

        return this.DoInvalidFormatTest(c_TEST_ID, c_TEST_DESC, "41", "42", "r", culture);
    }

    public bool NegTest3()
    {
        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: The format parameter is invalid -- \"z\". ";
        CultureInfo culture = CultureInfo.CurrentCulture;

        return this.DoInvalidFormatTest(c_TEST_ID, c_TEST_DESC, "43", "44", "z", culture);
    }
    #endregion

    #region Helper methods for negative tests
    public bool DoInvalidFormatTest(string testId,
                                                  string testDesc,
                                                  string errorNum1,
                                                  string errorNum2,
                                                  string format,
                                                  CultureInfo culture)
    {
        bool retVal = true;
        string errorDesc;

        IFormatProvider provider;
        UInt16 uintA = (UInt16)(TestLibrary.Generator.GetInt32() % (UInt16.MaxValue + 1));

        TestLibrary.TestFramework.BeginScenario(testDesc);
        try
        {
            provider = (IFormatProvider)culture.NumberFormat;
            uintA.ToString(format, provider);
            errorDesc = "FormatException is not thrown as expected.";
            errorDesc = string.Format("\nUInt16 value is {0}, format string is {1} and the culture is {3}.", 
                             uintA, format, culture.Name);
            TestLibrary.TestFramework.LogError(errorNum1 + " TestId-" + testId, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc = string.Format("\nUInt16 value is {0}, format string is {1} and the culture is {3}.",
                             uintA, format, culture.Name);
            TestLibrary.TestFramework.LogError(errorNum2 + " TestId-" + testId, errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}

