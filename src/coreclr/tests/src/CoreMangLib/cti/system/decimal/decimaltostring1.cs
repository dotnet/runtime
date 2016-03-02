// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Globalization;
/// <summary>
/// ToString
/// </summary>
public class DecimalToString1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Calling ToString method.");

        try
        {
            decimal d1 = 11111.111m;
            CultureInfo myCulture=CultureInfo.CurrentCulture ;
            string seperator=myCulture.NumberFormat.CurrencyDecimalSeparator;
            string expectValue="11111"+seperator+"111";
            string actualValue = d1.ToString();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToString should return "+expectValue);
                retVal = false;
            }
            d1 = -11111.111m;
            expectValue = "-11111" + seperator + "111";
            actualValue = d1.ToString();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.2", "ToString should return " + expectValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Calling ToString method and the value is Decimal.MaxValue and Decimal.MinValue.");

        try
        {
            decimal d1 = Decimal.MaxValue;
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string seperator = myCulture.NumberFormat.CurrencyDecimalSeparator;
            string expectValue = "79228162514264337593543950335";
            string actualValue = d1.ToString();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToString should return " + expectValue);
                retVal = false;
            }
            d1 = Decimal.MinValue;
            expectValue = "-79228162514264337593543950335";
            actualValue = d1.ToString();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToString should return " + expectValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Calling ToString method,the decimal has a long fractional digits.");

        try
        {
            int exponent = 28;
            decimal d1 = 1e-28m;
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string seperator = myCulture.NumberFormat.CurrencyDecimalSeparator;
            string expectValue = "0" + seperator;
            for (int i = 1; i < exponent; i++)
            {
                expectValue += "0";
            }
            expectValue = expectValue + "1";
            string actualValue = d1.ToString();
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("003.1", "ToString should return " + expectValue);
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
        DecimalToString1 test = new DecimalToString1();

        TestLibrary.TestFramework.BeginTestCase("DecimalToString1");

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
