// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;

public class MyClass
{
    private int m_value = 0;

    public MyClass()
    {
        this.m_value = 0;
    }

    public MyClass(int value)
    {
        this.m_value = value;
    }
}

/// <summary>
/// System.Byte.IConvertible.ToType(System.Type,System.IFormatProvider)
/// </summary>
public class ByteIConvertibleToType
{
    public static int Main(string[] args)
    {
        ByteIConvertibleToType toType = new ByteIConvertibleToType();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.IConvertible.ToType(System.Type,System.IFormatProvider)...");

        if (toType.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        //retVal = NegTest3() && retVal;


        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify convert byte value when positiveSign is set and type is Boolean...");

        try
        {
            string byteString = "plus128";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.PositiveSign = "plus";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);
            object conVertObj = ((IConvertible)myByte).ToType(typeof(Boolean),numberFormat);

            if (conVertObj.ToString() != "True")
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify convert byte value when CurrencySymbol is set and type is Byte...");

        try
        {
            string byteString = "@10";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertObj = ((IConvertible)myByte).ToType(typeof(Byte),numberFormat);

            if (conVertObj.ToString() != "10")
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify NumberGroupSeparator of format provider is set and type is decimal...");

        try
        {
            string byteString = "1_2_3";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.NumberGroupSeparator = "_";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Number, numberFormat);
            object conVertObj = ((IConvertible)myByte).ToType(typeof(Decimal),numberFormat);

            if (conVertObj.ToString() != "123")
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify byteString is maxValue and type is double...");

        try
        {
            string byteString = "@255";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertObj = ((IConvertible)myByte).ToType(typeof(Double),numberFormat);

            if (conVertObj.ToString() != "255")
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify byteString is minValue and type is single...");

        try
        {
            string byteString = "@0";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertSingle = ((IConvertible)myByte).ToType(typeof(Single),numberFormat);

            if (conVertSingle.ToString() != "0")
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

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify byteString is the middle value and type is Int16...");

        try
        {
            string byteString = "@128";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertInt16 = ((IConvertible)myByte).ToType(typeof(Int16), numberFormat);

            if (conVertInt16.ToString() != "128")
            {
                TestLibrary.TestFramework.LogError("011", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify byteString is the middle value and type is Int32...");

        try
        {
            string byteString = "@128";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertInt32 = ((IConvertible)myByte).ToType(typeof(Int32), numberFormat);

            if (conVertInt32.ToString() != "128")
            {
                TestLibrary.TestFramework.LogError("013", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest8: Verify byteString is the middle value and type is Int64...");

        try
        {
            string byteString = "@128";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertInt64 = ((IConvertible)myByte).ToType(typeof(Int64), numberFormat);

            if (conVertInt64.ToString() != "128")
            {
                TestLibrary.TestFramework.LogError("015", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest9: Verify byteString is the valid value for SByte and type is SByte...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertSByte = ((IConvertible)myByte).ToType(typeof(SByte), numberFormat);

            if (conVertSByte.ToString() != "127")
            {
                TestLibrary.TestFramework.LogError("017", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest10: Verify byteString is the valid value for UInt16 and type is UInt16...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertUInt16 = ((IConvertible)myByte).ToType(typeof(UInt16), numberFormat);

            if (conVertUInt16.ToString() != "127")
            {
                TestLibrary.TestFramework.LogError("019", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest11: Verify byteString is the valid value for UInt32 and type is UInt32...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertUInt32 = ((IConvertible)myByte).ToType(typeof(UInt32), numberFormat);

            if (conVertUInt32.ToString() != "127")
            {
                TestLibrary.TestFramework.LogError("021", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest12: Verify byteString is the valid value for UInt32 and type is UInt64...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertUInt64 = ((IConvertible)myByte).ToType(typeof(UInt64), numberFormat);

            if (conVertUInt64.ToString() != "127")
            {
                TestLibrary.TestFramework.LogError("023", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest13: Verify byteString is the valid value and type is object...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertOBj = ((IConvertible)myByte).ToType(typeof(Object), numberFormat);

            if (conVertOBj.ToString() != "127")
            {
                TestLibrary.TestFramework.LogError("025", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest14: Verify the type is char...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertChar = ((IConvertible)myByte).ToType(typeof(Char), numberFormat);

            if (conVertChar.ToString() != "")
            {
                TestLibrary.TestFramework.LogError("027", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: Verify InvalidCastException is thrown when type is DateTime...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertDateTime = ((IConvertible)myByte).ToType(typeof(DateTime), numberFormat);

            if (conVertDateTime.ToString() != "127")
            {
                TestLibrary.TestFramework.LogError("101", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (InvalidCastException)
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
        TestLibrary.TestFramework.BeginScenario("NegTest2: Verify InvalidCastException is thrown when type is customer defined...");

        try
        {
            string byteString = "@127";
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numberFormat = culture.NumberFormat;
            numberFormat.CurrencySymbol = "@";

            Byte myByte = Byte.Parse(byteString, NumberStyles.Currency | NumberStyles.Number, numberFormat);
            object conVertMyClass = ((IConvertible)myByte).ToType(typeof(MyClass), numberFormat);

            if (conVertMyClass.ToString() != "127")
            {
                TestLibrary.TestFramework.LogError("101", "The convert byte is not equal to original!");
                retVal = false;
            }
        }
        catch (InvalidCastException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
