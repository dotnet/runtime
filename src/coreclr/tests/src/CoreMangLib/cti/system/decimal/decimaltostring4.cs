// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Globalization;
/// <summary>
/// ToString(System.String,System.IFormatProvider)
/// </summary>
public class DecimalToString4
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Calling ToString method,format string is G.");

        try
        {
            decimal d1 = 11111.111m;
            CultureInfo myCulture=CultureInfo.InvariantCulture ;
            string seperator=myCulture.NumberFormat.CurrencyDecimalSeparator;
            string expectValue="11111"+seperator+"111";
            string actualValue = d1.ToString("G9", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToString should return "+expectValue);
                retVal = false;
            }
            d1 = -11111.1234567891m;
            expectValue = "-11111" + seperator + "1235";
            actualValue = d1.ToString("G9", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.2", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1" ;
            actualValue = d1.ToString("G1", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.3", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1" + seperator + "1235";
            actualValue = d1.ToString("G5", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.4", "ToString should return " + expectValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Calling ToString method,format string is N.");

        try
        {
            decimal d1 = 11111.111m;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            string seperator = myCulture.NumberFormat.CurrencyDecimalSeparator;
            string expectValue = "11" + myCulture.NumberFormat.NumberGroupSeparator + "111" + seperator + "111000000";
            string actualValue = d1.ToString("N9", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToString should return " + expectValue);
                retVal = false;
            }
            d1 = -11111.1234567891m;
            expectValue = "-11" + myCulture.NumberFormat.NumberGroupSeparator + "111" + seperator + "123456789";
            actualValue = d1.ToString("N9", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1" + seperator + "1";
            actualValue = d1.ToString("N1", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.3", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1" + seperator + "12346";
            actualValue = d1.ToString("N5", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.4", "ToString should return " + expectValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Calling ToString method,format string is C.");

        try
        {
            decimal d1 = 11111.111m;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            string seperator = myCulture.NumberFormat.CurrencyDecimalSeparator;
            string expectValue = myCulture.NumberFormat.CurrencySymbol + "11" + myCulture.NumberFormat.NumberGroupSeparator + "111" + seperator + "111000000";
            string actualValue = d1.ToString("C9", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.1", "ToString should return " + expectValue);
                retVal = false;
            }
            d1 = 11111.1234567891m;
            expectValue = myCulture.NumberFormat.CurrencySymbol + "11" + myCulture.NumberFormat.NumberGroupSeparator + "111" + seperator + "123456789";
            actualValue = d1.ToString("C9", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.2", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = 1.1234567891m;
            expectValue = myCulture.NumberFormat.CurrencySymbol + "1" + seperator + "1";
            actualValue = d1.ToString("C1", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.3", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = 1.1234567891m;
            expectValue = myCulture.NumberFormat.CurrencySymbol + "1" + seperator + "12346";
            actualValue = d1.ToString("C5", myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.4", "ToString should return " + expectValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    
    
    #endregion

    public static int Main()
    {
        DecimalToString4 test = new DecimalToString4();

        TestLibrary.TestFramework.BeginTestCase("DecimalToString4");

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
  
}
