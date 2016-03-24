// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using TestLibrary;

/// <summary>
/// ToString(System.String,System.IFormatProvider)
/// </summary>

public class DoubleToString3
{
    private static CultureInfo ci = new CultureInfo("en-US");
    private static NumberFormatInfo nfi = ci.NumberFormat;

    public static int Main()
    {
        DoubleToString3 test = new DoubleToString3();

        TestLibrary.TestFramework.BeginTestCase("DoubleToString3");

        if (test.RunTests())
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

    private NumberFormatInfo customNFI = null;

    private NumberFormatInfo CustomNFI
    {
        get
        {
            if (null == customNFI)
            {
                customNFI = new CultureInfo(CultureInfo.CurrentCulture.Name).NumberFormat;
                //For "E"
                // PositiveSign, NegativeSign, and NumberDecimalSeparator. 
                // If precision specifier is omitted, a default of six digits after the decimal point is used.
                customNFI.PositiveSign = "++";                //Default: "+"
                customNFI.NegativeSign = "@";                 //Default: "-"
                customNFI.NumberDecimalSeparator = ",";       //Default: "."
                
                //For "G"
                // NegativeSign, NumberDecimalSeparator, NumberDecimalDigits, PositiveSign
                customNFI.NumberDecimalDigits = 3;            //Default: 2
                customNFI.NaNSymbol = "NaN";

                //For "R"
                // NegativeSign, NumberDecimalSeparator and PositiveSign
            }
            return customNFI;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal &= VerifyToString("PostTestG1", Double.NaN, "G", "NaN");
        retVal &= VerifyToString("PostTestG2", 0.123456789123456789123D, "G", "0,123456789123457");
        retVal &= VerifyToString("PostTestG3", 0.123456789123459789D, "G", "0,12345678912346");
        retVal &= VerifyToString("PostTestG4", 0.123456789123000009123D, "G", "0,123456789123");
                
        retVal &= VerifyToString("PostTestR1", Double.NaN, "R", "NaN");
        retVal &= VerifyToString("PostTestR2", 0.123456789123456789123D, "R", TestLibrary.Utilities.IsWindows ? "0,12345678912345678" : "0,123456789123457");
        retVal &= VerifyToString("PostTestR3", 0.123456789123459789D, "R", TestLibrary.Utilities.IsWindows ? "0,1234567891234598" : "0,12345678912346");
        retVal &= VerifyToString("PostTestR4", 0.123456789123000009123D, "R", TestLibrary.Utilities.IsWindows ? "0,12345678912300001" : "0,123456789123");

        retVal &= VerifyToString("PostTestE1", Double.NaN, "E", "NaN");
        retVal &= VerifyToString("PostTestE2", 0.123456789123456789123D, "E", TestLibrary.Utilities.IsWindows ? "1,234568E@001" : "1,234568E@1");
        retVal &= VerifyToString("PostTestE3", 0.123456789123459789D, "E", TestLibrary.Utilities.IsWindows ? "1,234568E@001" : "1,234568E@1");
        retVal &= VerifyToString("PostTestE4", 0.123456789123000009123D, "E", TestLibrary.Utilities.IsWindows ? "1,234568E@001" : "1,234568E@1");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal &= NegTest1();

        return retVal;
    }

    private bool VerifyToString(String id, Double myDouble, String format, String expected)
    {
        try
        {
            String actual = myDouble.ToString(format, CustomNFI);
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: FormatException should be thrown when the format string is invalid.");

        try
        {
            Double d1 = 0.123456009123000009123D;
            String s1 = d1.ToString("H", nfi);
            TestLibrary.TestFramework.LogError("N01.1", "FormatException is not thrown when the format string is invalid!");
            retVal = false;
        }
        catch (FormatException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
