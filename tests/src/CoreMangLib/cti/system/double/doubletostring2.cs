// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// ToString(System.IFormatProvider)
/// </summary>

public class DoubleToString1
{
    private static NumberFormatInfo nfi;

    public static void InitializeIFormatProvider()
    {
        nfi = new CultureInfo("en-US").NumberFormat;
        //For "G"
        // NegativeSign, NumberDecimalSeparator, NumberDecimalDigits, PositiveSign
        nfi.NumberDecimalDigits = 2;            //Default: 2
        nfi.PositiveSign = "+";                //Default: "+"
        nfi.NegativeSign = "-";                 //Default: "-"
        nfi.NumberDecimalSeparator = ".";       //Default: "."
        nfi.NaNSymbol = "NaN";
    }

    public static int Main()
    {
        InitializeIFormatProvider();
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
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure the String decimal part will be rounded to 15 digits when the Double decimal part is longer than 15.");

        try
        {
            Double d1 = 0.123456789123456789123D;
            String s1 = d1.ToString(nfi);
            if (s1 != "0.123456789123457")
            {
                TestLibrary.TestFramework.LogError("P01.1", "The String decimal part is not rounded to 15 digits when the Double decimal part is longer than 15!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure the suffix zero will be discarded in String after Double decimal part is rounded (become zero after carry).");

        try
        {
            Double d1 = 0.123456789123459789123D;
            String s1 = d1.ToString(nfi);
            if (s1 != "0.12345678912346")
            {
                TestLibrary.TestFramework.LogError("P02.1", "The suffix zero is not discarded in String after Double decimal part is rounded (become zero after carry)!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Ensure the suffix zero will be discarded in String after Double decimal part is rounded (original zero).");

        try
        {
            Double d1 = 0.123456789123000009123D;
            String s1 = d1.ToString(nfi);
            if (s1 != "0.123456789123")
            {
                TestLibrary.TestFramework.LogError("P03.1", "The suffix zero is not discarded in String after Double decimal part is rounded (original zero)");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Ensure the result will be correct when Double is NaN.");

        try
        {
            Double d1 = Double.NaN;
            String s1 = d1.ToString(nfi);
            if (s1 != "NaN")
            {
                TestLibrary.TestFramework.LogError("P04.1", "The result is not correct when Double is NaN!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P04.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
