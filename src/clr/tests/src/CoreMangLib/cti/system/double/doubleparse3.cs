// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;
using System.Globalization;

/// <summary>
/// Parse(System.String,System.Globalization.NumberStyles,System.IFormatProvider)
/// </summary>

public class DoubleParse3
{
    private static NumberFormatInfo nfi;
    private static string currencySymbol;

    public static void InitializeIFormatProvider()
    {
        nfi = new CultureInfo("en-US").NumberFormat;
        currencySymbol = nfi.CurrencySymbol;
    }

    public static int Main()
    {
        InitializeIFormatProvider();

        DoubleParse3 test = new DoubleParse3();

        TestLibrary.TestFramework.BeginTestCase("DoubleParse3");

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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure the result is correct when NumberStyles is set to AllowCurrencySymbol.");

        try
        {
            Double d = Double.Parse("123" + currencySymbol, NumberStyles.AllowCurrencySymbol, nfi);
            if (d.CompareTo(123.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P01.1", "The result is not correct when NumberStyles is set to AllowCurrencySymbol!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure the result is correct when NumberStyles is set to AllowDecimalPoint.");

        try
        {
            Double d = Double.Parse("123.1", NumberStyles.AllowDecimalPoint, nfi);
            if (d.CompareTo(123.1) != 0)
            {
                TestLibrary.TestFramework.LogError("P02.1", "The result is not correct when NumberStyles is set to AllowDecimalPoint!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Ensure the result is correct when NumberStyles is set to AllowExponent.");

        try
        {
            Double d = Double.Parse("123E2", NumberStyles.AllowExponent, nfi);
            if (d.CompareTo(12300.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P03.1", "The result is not correct when NumberStyles is set to AllowExponent!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Ensure the result is correct when NumberStyles is set to AllowLeadingSign.");

        try
        {
            Double d = Double.Parse("-12345", NumberStyles.AllowLeadingSign, nfi);
            if (d.CompareTo(-12345.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P04.1", "The result is not correct when NumberStyles is set to AllowLeadingSign!");
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

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Ensure the result is correct when NumberStyles is set to AllowLeadingWhite.");

        try
        {
            Double d = Double.Parse("   1234", NumberStyles.AllowLeadingWhite, nfi);
            if (d.CompareTo(1234.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P05.1", "The result is not correct when NumberStyles is set to AllowLeadingWhite!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P05.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Ensure the result is correct when NumberStyles is set to AllowParentheses.");

        try
        {
            Double d = Double.Parse("(456)", NumberStyles.AllowParentheses, nfi);
            if (d.CompareTo(-456.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P06.1", "The result is not correct when NumberStyles is set to AllowParentheses!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P06.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Ensure the result is correct when NumberStyles is set to AllowThousands.");

        try
        {
            Double d = Double.Parse("123,456", NumberStyles.AllowThousands, nfi);
            if (d.CompareTo(123456.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P07.1", "The result is not correct when NumberStyles is set to AllowThousands!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P07.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest8: Ensure the result is correct when NumberStyles is set to AllowTrailingSign.");

        try
        {
            Double d = Double.Parse("123-", NumberStyles.AllowTrailingSign, nfi);
            if (d.CompareTo(-123.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P08.1", "The result is not correct when NumberStyles is set to AllowTrailingSign!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P08.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest9: Ensure the result is correct when NumberStyles is set to AllowTrailingWhite.");

        try
        {
            Double d = Double.Parse("123    ", NumberStyles.AllowTrailingWhite, nfi);
            if (d.CompareTo(123.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P09.1", "The result is not correct when NumberStyles is set to AllowTrailingWhite!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P09.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest10: Ensure the result is correct when NumberStyles is set to Any.");

        try
        {
            Double d = Double.Parse("-123E2  " + currencySymbol, NumberStyles.Any, nfi);
            if (d.CompareTo(-12300.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P10.1", "The result is not correct when NumberStyles is set to Any!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P10.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest11: Ensure the result is correct when NumberStyles is set to Currency.");

        try
        {
            Double d = Double.Parse("-123  " + currencySymbol, NumberStyles.Currency, nfi);
            if (d.CompareTo(-123.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P11.1", "The result is not correct when NumberStyles is set to Currency!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P11.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest12: Ensure the result is correct when NumberStyles is set to Float.");

        try
        {
            Double d = Double.Parse("    -123.4   ", NumberStyles.Float, nfi);
            if (d.CompareTo(-123.4) != 0)
            {
                TestLibrary.TestFramework.LogError("P12.1", "The result is not correct when NumberStyles is set to Float!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P12.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest13: Ensure the result is correct when NumberStyles is set to Integer.");

        try
        {
            Double d = Double.Parse("    -123    ", NumberStyles.Integer, nfi);
            if (d.CompareTo(-123.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P13.1", "The result is not correct when NumberStyles is set to Integer!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P13.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest14: Ensure the result is correct when NumberStyles is set to None.");

        try
        {
            Double d = Double.Parse("123", NumberStyles.None, nfi);
            if (d.CompareTo(123.0) != 0)
            {
                TestLibrary.TestFramework.LogError("P14.1", "The result is not correct when NumberStyles is set to None!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P14.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest15: Ensure the result is correct when NumberStyles is set to Number.");

        try
        {
            Double d = Double.Parse("    4,123.1-    ", NumberStyles.Number, nfi);
            if (d.CompareTo(-4123.1) != 0)
            {
                TestLibrary.TestFramework.LogError("P15.1", "The result is not correct when NumberStyles is set to Number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P15.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest16: Ensure the result is correct when NumberStyles is set to None but System.String has currency symbol.");

        try
        {
            Double d = Double.Parse("123$", NumberStyles.None, nfi);
            TestLibrary.TestFramework.LogError("P16.1", "The result is not correct when NumberStyles is set to None but System.String has currency symbol!");
            retVal = false;
        }
        catch (FormatException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P16.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when System.String is a null reference.");

        try
        {
            Double d = Double.Parse(null, NumberStyles.Any, nfi);
            TestLibrary.TestFramework.LogError("N01.1", "ArgumentNullException is not thrown when System.String is a null reference!");
            retVal = false;
        }
        catch (ArgumentNullException)
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

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: FormatException should be thrown when System.String is not a number in a valid format.");

        try
        {
            Double d = Double.Parse("123,456.5.66", NumberStyles.None, nfi);
            TestLibrary.TestFramework.LogError("N02.1", "FormatException is not thrown when System.String is not a number in a valid format!");
            retVal = false;
        }
        catch (FormatException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: OverflowException should be thrown when System.String represents a number greater than MaxValue.");

        try
        {
            Double d = Double.Parse("1.79769313486233e308", NumberStyles.Float, nfi);
            TestLibrary.TestFramework.LogError("N03.1", "OverflowException is not thrown when System.String represents a number greater than MaxValue!");
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentException should be thrown when NumberStyles contains AllowHexSpecifier value.");

        try
        {
            Double d = Double.Parse("108", NumberStyles.HexNumber, nfi);
            TestLibrary.TestFramework.LogError("N04.1", "ArgumentException is not thrown when NumberStyles contains AllowHexSpecifier value!");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N04.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5: ArgumentException should be thrown when NumberStyles is the AllowHexSpecifier value.");

        try
        {
            Double d = Double.Parse("108", NumberStyles.AllowHexSpecifier, nfi);
            TestLibrary.TestFramework.LogError("N05.1", "ArgumentException is not thrown when NumberStyles is the AllowHexSpecifier value!");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N05.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest6: ArgumentException should be thrown when NumberStyles is not a NumberStyles value.");

        try
        {
            Double d = Double.Parse("123", (NumberStyles)5000, nfi);
            TestLibrary.TestFramework.LogError("N06.1", "ArgumentException is not thrown when NumberStyles is not a NumberStyles value!");
            retVal = false;
        }
        catch (ArgumentException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N06.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
