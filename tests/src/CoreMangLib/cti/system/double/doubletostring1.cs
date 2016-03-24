// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// ToString()
/// </summary>

public class DoubleToString1
{
    public static int Main()
    {
        DoubleToString1 test = new DoubleToString1();

        TestLibrary.TestFramework.BeginTestCase("DoubleToString1");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        TestLibrary.Utilities.CurrentCulture = CustomCulture;

        retVal &= VerifyToString("PostTest0", Double.NaN, "NaN");
        retVal &= VerifyToString("PosTest1", 0.123456789123456789123D, "0,123456789123457");
        retVal &= VerifyToString("PosTest2", 0.123456789123459789123D, "0,12345678912346");
        retVal &= VerifyToString("PosTest3", 0.123456789123000009123D, "0,123456789123");
        
        TestLibrary.Utilities.CurrentCulture = CurrentCulture;

        return retVal;
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

                //For "G"
                // NegativeSign, NumberDecimalSeparator, NumberDecimalDigits, PositiveSign
                nfi.NumberDecimalDigits = 3;            //Default: 2
                nfi.PositiveSign = "++";                //Default: "+"
                nfi.NegativeSign = "@";                 //Default: "-"
                nfi.NumberDecimalSeparator = ",";       //Default: "."
                nfi.NaNSymbol = "NaN";

                customCulture.NumberFormat = nfi;
            }
            return customCulture;
        }
    }

    private bool VerifyToString(String id, Double myDouble, String expected)
    {
        try
        {
            String actual = myDouble.ToString();
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
