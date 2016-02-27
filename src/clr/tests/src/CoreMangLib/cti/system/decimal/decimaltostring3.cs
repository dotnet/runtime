// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Globalization;
/// <summary>
///ToString(System.String)
/// </summary>
public class DecimalToString3
{
    #region privates
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
                nfi.NumberDecimalDigits = 2;
                nfi.PositiveSign = "+";     
                nfi.NegativeSign = "-";
                nfi.NumberDecimalSeparator = ".";

                //For "N"
                //NegativeSign, NumberNegativePattern, NumberGroupSizes, NumberGroupSeparator, NumberDecimalSeparator, NumberDecimalDigits
                nfi.NumberNegativePattern = 1;
                nfi.NumberGroupSizes = new int[]{3};
                nfi.NumberGroupSeparator = ",";
                
                customCulture.NumberFormat = nfi;
            }
            return customCulture;
        }
    }
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");

        TestLibrary.Utilities.CurrentCulture = CustomCulture; 
        
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        
        TestLibrary.Utilities.CurrentCulture = CurrentCulture; 
        
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
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string seperator = myCulture.NumberFormat.NumberDecimalSeparator;
            string expectValue = "11111" + seperator + "111";
            string actualValue = d1.ToString("G9");
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToString should return " + expectValue);
                retVal = false;
            }
            d1 = -11111.1234567891m;
            expectValue = "-11111" + seperator + "1235";
            actualValue = d1.ToString("G9");
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.2", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1";
            actualValue = d1.ToString("G1");
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.3", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1" + seperator + "1235";
            actualValue = d1.ToString("G5");
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
            CultureInfo myCulture = CultureInfo.CurrentCulture;
            string seperator = myCulture.NumberFormat.NumberDecimalSeparator;
            string expectValue = "11" + myCulture.NumberFormat.NumberGroupSeparator + "111" + seperator + "111000000";
            string actualValue = d1.ToString("N9");
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToString should return " + expectValue);
                retVal = false;
            }
            d1 = -11111.1234567891m;
            expectValue = "-11" + myCulture.NumberFormat.NumberGroupSeparator + "111" + seperator + "123456789";
            actualValue = d1.ToString("N9");
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1" + seperator + "1";
            actualValue = d1.ToString("N1");
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.3", "ToString should return " + expectValue);
                retVal = false;
            }

            d1 = -1.1234567891m;
            expectValue = "-1" + seperator + "12346";
            actualValue = d1.ToString("N5");
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

    #endregion


    #endregion

    public static int Main()
    {
        DecimalToString3 test = new DecimalToString3();

        TestLibrary.TestFramework.BeginTestCase("DecimalToString3");

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
