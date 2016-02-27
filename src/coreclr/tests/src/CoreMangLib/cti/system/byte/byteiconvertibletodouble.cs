// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;


/// <summary>
/// System.Byte.IConvertible.ToDecimal(System.IFormatProvide)
/// </summary>
public class ByteIConvertibleToDouble
{
    public static int Main(string[] args)
    {
        ByteIConvertibleToDouble toDouble = new ByteIConvertibleToDouble();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.IConvertible.ToDouble(System.IFormatProvide)...");

        if (toDouble.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify convert byte value when positiveSign is set...");

        try
        {
            string byteString = "plus128";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.PositiveSign = "plus";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);
            Double conVertDouble = ((IConvertible)myByte).ToDouble(numberFormat);

            if (conVertDouble != 128.0)
            {
                TestLibrary.TestFramework.LogError("001", "The convert byte is not equal to original!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify convert byte value when CurrencySymbol is set...");

        try
        {
            string byteString = "@10";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            Double conVertDouble = ((IConvertible)myByte).ToDouble(numberFormat);

            if (conVertDouble != 10.0)
            {
                TestLibrary.TestFramework.LogError("003", "The convert byte is not equal to original!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify NumberGroupSeparator of format provider is set...");

        try
        {
            string byteString = "1_2_3";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.NumberGroupSeparator = "_";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);
            Double conVertDouble = ((IConvertible)myByte).ToDouble(numberFormat);

            if (conVertDouble != 123.0)
            {
                TestLibrary.TestFramework.LogError("005", "The convert byte is not equal to original!");
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
            Double conVertDouble = ((IConvertible)myByte).ToDouble(numberFormat);

            if (conVertDouble != 255.0)
            {
                TestLibrary.TestFramework.LogError("007", "The convert byte is not equal to original!");
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
            Double conVertDouble = ((IConvertible)myByte).ToDouble(numberFormat);

            if (conVertDouble != 0.0)
            {
                TestLibrary.TestFramework.LogError("009", "The convert byte is not equal to original!");
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

}

