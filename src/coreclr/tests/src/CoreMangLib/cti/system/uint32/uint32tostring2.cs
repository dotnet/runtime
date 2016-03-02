// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using TestLibrary;

/// <summary>
/// UInt32.ToString(System.string)
/// </summary>
public class UInt32ToString2
{
    public static int Main()
    {
        UInt32ToString2 ui32ts2 = new UInt32ToString2();
        TestLibrary.TestFramework.BeginTestCase("UInt32ToString2");

        if (ui32ts2.RunTests())
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

    #region Private Methods
    private CultureInfo CurrentCulture = TestLibrary.Utilities.CurrentCulture;
    private CultureInfo customCulture = null;

    private CultureInfo CustomCulture
    {
        get
        {
            if (null == customCulture)
            {
                customCulture = new CultureInfo(CultureInfo.CurrentCulture.Name);
                NumberFormatInfo nfi = customCulture.NumberFormat;
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

                customCulture.NumberFormat = nfi;
            }
            return customCulture;
        }
    }

    private bool VerifyToString(String id, UInt32 myInt, String format, String expected)
    {
        try
        {
            String actual = myInt.ToString(format);
            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError(id + "_001", "Expected: " + expected + " Actual: " + actual);
                return false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(id + "_002", "Unexpected exception occurs: " + e);
            return false;
        }
        return true;
    }
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal &= VerifyToString("PostTest1", UInt32.MaxValue, "X", "FFFFFFFF");

        TestLibrary.Utilities.CurrentCulture = CustomCulture;
        retVal &= VerifyToString("PostTest2", UInt32.MinValue, "G", "0");
        retVal &= VerifyToString("PostTest3", UInt32.MinValue, "f", "0,000");
        retVal &= VerifyToString("PostTest4", UInt32.MaxValue, "G", "4294967295");
        retVal &= VerifyToString("PostTest5", UInt32.MaxValue, "f", "4294967295,000");
        retVal &= VerifyToString("PostTest6", UInt32.MaxValue, "E", TestLibrary.Utilities.IsWindows? "4,294967E++009" : "4,294967E9");
        retVal &= VerifyToString("PostTest7", UInt32.MaxValue, "N", "42#94#96#72#95,000");
        retVal &= VerifyToString("PostTest8", UInt32.MaxValue, "P", "429,496,729,500,0000~");
        retVal &= VerifyToString("PostTest9", UInt32.MaxValue, "D", "4294967295");
        retVal &= VerifyToString("PostTest10", 00065536, "G", "65536");
        retVal &= VerifyToString("PostTest11", 00065536, "P", "6,553,600,0000~");
        retVal &= VerifyToString("PostTest12", 00065536, "E", TestLibrary.Utilities.IsWindows? "6,553600E++004" : "6,553600E4");
        retVal &= VerifyToString("PostTest13", 00065536, String.Empty, "65536");
        retVal &= VerifyToString("PostTest14", 00065536, null, "65536");

        TestLibrary.Utilities.CurrentCulture = CurrentCulture;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal &= NegTest1();

        return retVal;
    }
    #endregion
    
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: The Format parameter is Invalid");
        try
        {
            UInt32 uintA = (UInt32)(this.GetInt32(1, Int32.MaxValue) + this.GetInt32(0, Int32.MaxValue));
            String strA = uintA.ToString("Q");
            retVal = false;
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpected exception: " + e);
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
                return minValue + TestLibrary.Generator.GetInt32() % (maxValue - minValue);
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
