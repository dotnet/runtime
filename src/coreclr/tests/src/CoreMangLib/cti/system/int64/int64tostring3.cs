// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
using TestLibrary;
/// <summary>
/// Int64.ToString(System.String)
/// </summary>
public class Int64ToString3
{
    public static int Main()
    {
        Int64ToString3 int64ts3 = new Int64ToString3();
        TestLibrary.TestFramework.BeginTestCase("Int64ToString3");
        if (int64ts3.RunTests())
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
        retVal = PosTest(2, Int64.MinValue, "Int64.MinValue", "x", "8000000000000000") && retVal;

        TestLibrary.Utilities.CurrentCulture = CustomCulture;
        retVal &= PosTest(1, Int64.MinValue, "Int64.MinValue", "g", "@9223372036854775808");
        retVal &= PosTest(3, Int64.MaxValue, "Int64.MaxValue", "g", "9223372036854775807");
        retVal &= PosTest(4, Int64.MaxValue, "Int64.MaxValue", "x", "7fffffffffffffff");
        retVal &= PosTest(5, Int64.MaxValue, "Int64.MaxValue", "f", "9223372036854775807,000");
        retVal &= PosTest(6, Int64.MaxValue, "Int64.MaxValue", "E", TestLibrary.Utilities.IsWindows ? "9,223372E++018" : "9,223372E18");
        retVal &= PosTest(7, Int64.MaxValue, "Int64.MaxValue", "N", "9#22#33#72#03#68#54#77#58#07,000");
        retVal &= PosTest(8, Int64.MaxValue, "Int64.MaxValue", "P", "922,337,203,685,477,580,700,0000~");
        retVal &= PosTest(9, Int64.MaxValue, "Int64.MaxValue", "D", "9223372036854775807");
        retVal &= PosTest(10, 00065536, "00065536", "G", "65536");
        retVal &= PosTest(11, 00065536, "00065536", "P", "6,553,600,0000~");
        retVal &= PosTest(12, 00065536, "00065536", "E", TestLibrary.Utilities.IsWindows ? "6,553600E++004" : "6,553600E4");
        retVal &= PosTest(13, 00065536, "00065536", string.Empty, "65536");
        retVal &= PosTest(14, 00065536, "00065536", null, "65536");
        retVal &= PosTest(15, -00065536, "-00065536", "P", "@~6,553,600,0000");
        retVal &= PosTest(16, -00065536, "-00065536", "E", TestLibrary.Utilities.IsWindows ? "@6,553600E++004" : "@6,553600E4");

        TestLibrary.Utilities.CurrentCulture = CurrentCulture;

        TestLibrary.TestFramework.LogInformation("[NegTest]");
        retVal &= NegTest1();
        return retVal;
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
                nfi.PercentGroupSeparator = ",";        //Default: "," in most cultures, but "." in TR and DE
                
                customCulture.NumberFormat = nfi;
            }
            return customCulture;
        }
    }
    #endregion

    #region PositiveTest
    public bool PosTest(int seqNumber, long value, string description, string formatString, string expectedValue)
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest " + seqNumber.ToString() + ": " + description + ".ToString('" + formatString + "')");
        try
        {
            string actual = value.ToString(formatString);
            if (formatString == null) formatString = "G";

            if (!actual.Equals(expectedValue))
            {
                TestLibrary.TestFramework.LogError("00" + seqNumber.ToString() + ".1",
                    "Expected: " + expectedValue + ", Actual: " + actual);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("00" + seqNumber.ToString() + ".2", "Unexpected exception:" + e);
            retVal = false;
        }
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
            long int64A = TestLibrary.Generator.GetInt64();
            String strA = int64A.ToString("Q");
            retVal = false;
            TestLibrary.TestFramework.LogError("N001", "the format param is Invalid but not throw exception");
        }
        catch (FormatException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
