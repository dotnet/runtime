// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;


/// <summary>
/// System.Byte.ToString(System.IFormatProvider)
/// </summary>
public class ByteToString2
{
    public static int Main(string[] args)
    {
        ByteToString2 toString2 = new ByteToString2();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.ToString(System.IFormatProvider)...");

        if (toString2.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify NumberGroupSeparator of provider is _");

        try
        {
            Byte myByte = 128;
            
            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.NumberGroupSizes = new int[] { 1 };
            numInfo.NumberGroupSeparator = "_";
            numInfo.NumberDecimalDigits = 0;

            string myByteString = myByte.ToString(numInfo);
            if (myByteString != "128")
            {
                TestLibrary.TestFramework.LogError("001","The byte string is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify NumberGroupSeparator of provider is _");

        try
        {
            Byte myByte = 128;

            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.CurrencySymbol = "$";
            numInfo.CurrencyDecimalDigits = 0;

            string myByteString = myByte.ToString(numInfo);
            if (myByteString != "128")
            {
                TestLibrary.TestFramework.LogError("003", "The byte string is wrong!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify PositiveSign of provider is plus...");

        try
        {
            Byte myByte = 128;

            CultureInfo culture = new CultureInfo("");
            NumberFormatInfo numInfo = culture.NumberFormat;
            numInfo.PositiveSign = "plus";
            numInfo.NumberDecimalDigits = 0;

            string myByteString = myByte.ToString("N",numInfo);
            if (myByteString != "128")
            {
                TestLibrary.TestFramework.LogError("005", "The byte string is wrong!");
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
}
