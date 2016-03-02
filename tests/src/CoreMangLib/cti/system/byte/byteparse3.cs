// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;

/// <summary>
/// System.Byte.Parse(System.String,System.Globalization.NumberStyles,System.IFormatProvider)
/// </summary>
public class ByteParse3
{
    public static int Main(string[] args)
    {
        ByteParse3 parse3 = new ByteParse3();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.Parse(System.String,System.Globalization.NumberStyles,System.IFormatProvider)...");

        if (parse3.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify PositiveSign of format provider takes effect...");

        try
        {
            string byteString = "plus128";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.PositiveSign = "plus";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);

            if (myByte != 128)
            {
                TestLibrary.TestFramework.LogError("001", "The format provider does not takes effect!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify NumberGroupSeperator of format provider takes effect...");

        try
        {
            string byteString = "1_2_3";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.NumberGroupSeparator = "_";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);

            if (myByte != 123)
            {
                TestLibrary.TestFramework.LogError("003", "The NumberGroupSeparator of format provider does not takes effect!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify currency symbol of format provider should take effect...");

        try
        {
            string byteString = "@123";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency, numberFormat);

            if (myByte != 123)
            {
                TestLibrary.TestFramework.LogError("005", "The currency symbol of format provider does not take effect!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify byteString is maxValue...");

        try
        {
            string byteString = "@255";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);

            if (myByte != 255)
            {
                TestLibrary.TestFramework.LogError("007", "The format provider should takes effect!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify byteString is minValue...");

        try
        {
            string byteString = "@0";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);

            if (myByte != 0)
            {
                TestLibrary.TestFramework.LogError("009", "The format provider should takes effect!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: Verify byteString is more than maxValue...");

        try
        {
            string byteString = "@256";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
                         
            TestLibrary.TestFramework.LogError("101", "The format provider should takes effect!");
            retVal = false;

        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: Verify byteString is less than minValue...");

        try
        {
            string byteString = "@-1";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);

            TestLibrary.TestFramework.LogError("103", "The format provider should takes effect!");
            retVal = false;

        }
        catch (OverflowException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest3: Verify byteString is null referrence...");

        try
        {
            string byteString = null;
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.PositiveSign = "plus";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);

            TestLibrary.TestFramework.LogError("105", "No exception occurs!");
            retVal = false;
        }
        catch (ArgumentNullException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest4: Verify byteString is not of the correct format...");

        try
        {
            string byteString = "plusHelloWorld!";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.PositiveSign = "plus";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);

            TestLibrary.TestFramework.LogError("107", "No exception occurs!");
            retVal = false;
        }
        catch (FormatException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest5: Verify style is not a combination of AllowHexSpecifier and HexNumber values...");

        try
        {
            string byteString = "#2fD";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "#";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number | NumberStyles.Currency | NumberStyles.AllowHexSpecifier, numberFormat);

            TestLibrary.TestFramework.LogError("109", "No exception occurs!");
            retVal = false;
        }
        catch (ArgumentException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
