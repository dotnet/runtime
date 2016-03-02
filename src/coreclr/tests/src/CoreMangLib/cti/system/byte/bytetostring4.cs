// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;


/// <summary>
/// System.Byte.ToString(System.String,System.IFormatProvider)
/// </summary>
public class ByteToString4
{
    public static int Main(string[] args)
    {
        ByteToString4 toString4 = new ByteToString4();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.ToString(System.String,System.IFormatProvider)...");

        if (toString4.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify NumberGroupSeparator of provider is _ and format string is N");

        try
        {
            Byte myByte = 128;

            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.NumberGroupSizes = new int[] { 1 };
            numInfo.NumberGroupSeparator = "_";
            numInfo.NumberDecimalDigits = 0;

            string myByteString = myByte.ToString("N",numInfo);
            if (myByteString != "1_2_8")
            {
                TestLibrary.TestFramework.LogError("001", "The byte string is wrong!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify CurrencySymbol of provider is $ and format string is C...");

        try
        {
            Byte myByte = 128;

            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.CurrencySymbol = "$";
            numInfo.CurrencyDecimalDigits = 0;

            string myByteString = myByte.ToString("C",numInfo);
            if (myByteString != "$128")
            {
                TestLibrary.TestFramework.LogError("003", "The currency string is wrong!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify X format convert hex string...");

        try
        {
            Byte myByte = 128;
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.PositiveSign = "plus";
            string myByteString = myByte.ToString("X",numInfo);

            if (myByteString != "80")
            {
                TestLibrary.TestFramework.LogError("005", "The hex string is wrong!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify D format convert decimal string...");

        try
        {
            Byte myByte = 128;
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.PositiveSign = "plus";
            string myByteString = myByte.ToString("D", numInfo);

            if (myByteString != "128")
            {
                TestLibrary.TestFramework.LogError("007", "The decimal string is wrong!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify E format convert exponential format string...");

        try
        {
            Byte myByte = 128;
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.PositiveSign = "plus";
            string myByteString = myByte.ToString("E", numInfo);

            if (myByteString != "1.280000Eplus002")
            {
                TestLibrary.TestFramework.LogError("009", "The exponential string is wrong!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify F format convert Fixed-point string...");

        try
        {
            Byte myByte = 128;
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.PositiveSign = "plus";
            string myByteString = myByte.ToString("F", numInfo);

            if (myByteString != "128.00")
            {
                TestLibrary.TestFramework.LogError("011", "The Fixed-point string is wrong!");
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

}
