// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;
using TestLibrary;

/// <summary>
/// System.Byte.ToString3(System.String)
/// </summary>
public class ByteToString3
{
    public static int Main(string[] args)
    {
        ByteToString3 toString3 = new ByteToString3();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.ToString(System.String)...");

        if (toString3.RunTests())
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
                //For "X", The result string isn't affected by the formatting information of the current NumberFormatInfo

                //For "C"
                // CurrencyPositivePattern, CurrencySymbol, CurrencyDecimalDigits, CurrencyDecimalSeparator, CurrencyGroupSeparator, CurrencyGroupSizes.
                // Not consider NegativeSign and CurrencyNegativePattern for Byte
                nfi.CurrencyDecimalDigits = 3;          //Default: 2
                nfi.CurrencyDecimalSeparator = ",";     //Default: ","
                nfi.CurrencyGroupSeparator = ".";       //Default: "."
                nfi.CurrencyGroupSizes = new int[] { 2 };  //Default: new int[]{3}
                //nfi.NegativeSign = "-";
                //nfi.CurrencyNegativePattern = 0;
                nfi.CurrencyPositivePattern = 1;        //Default: 0
                nfi.CurrencySymbol = "USD";             //Default: "$" 

                //For "D"
                // NegativeSign isn't taken care of consideration for Byte

                //For "E"
                // PositiveSign, and NumberDecimalSeparator. Not consider NegativeSign for Byte. 
                // If precision specifier is omitted, a default of six digits after the decimal point is used.
                nfi.PositiveSign = "++";                //Default: "+"
                nfi.NumberDecimalSeparator = ",";       //Default: "."

                //For "F"
                // NumberDecimalDigits, and NumberDecimalSeparator. Not consider NegativeSign for Byte.
                nfi.NumberDecimalDigits = 3;            //Default: 2

                //For "N"
                // NumberGroupSizes, NumberGroupSeparator, NumberDecimalSeparator and NumberDecimalDigits. Not considfer NumberNegativePattern and NegativeSign for Byte
                nfi.NumberGroupSizes = new int[] { 2 }; //Default: 3
                nfi.NumberGroupSeparator = "#";         //Default: ","

                customCulture.NumberFormat = nfi;
            }
            return customCulture;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        retVal &= VerifyToString("PostTest1", 128, "X", "80");
        retVal &= VerifyToString("PostTest2", 128, "D", "128");

        TestLibrary.Utilities.CurrentCulture = CustomCulture;

        retVal &= VerifyToString("PostTest3", 128, "C", "1.28,000USD");
        retVal &= VerifyToString("PostTest4, default", 128, "E", TestLibrary.Utilities.IsWindows ? "1,280000E++002" : "1,280000E2");
        retVal &= VerifyToString("PostTest4, 4digits", 128, "E4", TestLibrary.Utilities.IsWindows ? "1,2800E++002" : "1,2800E2");
        retVal &= VerifyToString("PostTest5", 128, "F", "128,000");
        retVal &= VerifyToString("PostTest6", 128, "N", "1#28,000");

        TestLibrary.Utilities.CurrentCulture = CurrentCulture;

        return retVal;
    }

    private bool VerifyToString(String id, Byte myByte, String format, String expected)
    {
        try
        {
            String actual = myByte.ToString(format);
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
}
