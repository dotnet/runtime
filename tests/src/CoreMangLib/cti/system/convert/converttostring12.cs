// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Convert.ToString(System.Double,System.IFormatProvider)
/// </summary>
public class ConvertToString12
{
    public static int Main()
    {
        ConvertToString12 testObj = new ConvertToString12();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Double,System.IFormatProvider)");
        if (testObj.RunTests())
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is a random  Double and IFormatProvider is a null reference... ";
        string c_TEST_ID = "P001";


        Double doubleValue = TestLibrary.Generator.GetDouble(-55);
        IFormatProvider provider = null;
        String actualValue = doubleValue.ToString(provider);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest2: Verify value is a random negative Double and IFormatProvider is en-US CultureInfo... ";
        string c_TEST_ID = "P002";


        Double doubleValue = TestLibrary.Generator.GetDouble(-55);
        doubleValue = -doubleValue;
        IFormatProvider provider = new CultureInfo("en-US");

        String actualValue = doubleValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest3: Verify value is a random  Double and IFormatProvider is fr_FR CultureInfo... ";
        string c_TEST_ID = "P003";

        Double doubleValue = TestLibrary.Generator.GetDouble(-55);
        IFormatProvider provider = new CultureInfo("fr-FR"); 
        String actualValue = doubleValue.ToString(provider);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest4: Verify value is -61680.3855 and IFormatProvider is user-defined NumberFormatInfo... ";
        string c_TEST_ID = "P004";

        Double doubleValue = -61680.3855;
        NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
        numberFormatInfo.NegativeSign = "minus ";
        numberFormatInfo.NumberDecimalSeparator = " point ";

        String actualValue = "minus 61680 point 3855"; 
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue,numberFormatInfo);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest5: Verify value is Double.Epsilon and IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P005";

        Double doubleValue = Double.Epsilon;
        IFormatProvider provider = new CultureInfo("fr-FR");
        String actualValue = "4,94065645841247E-324";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest6: Verify value is 0 and IFormatProvider is IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P006";

        Double doubleValue = 0.00;
        IFormatProvider provider = new CultureInfo("fr-FR");
        String actualValue = doubleValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest7: Verify value is Double.NaN and IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P007";

        Double doubleValue = Double.NaN;
        IFormatProvider provider = new CultureInfo("fr-FR");
        String actualValue = doubleValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest8: Verify value is Double.NegativeInfinity and IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P008";

        Double doubleValue = Double.NegativeInfinity;
        IFormatProvider provider = new CultureInfo("fr-FR");
        String actualValue = doubleValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(doubleValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n double value is " + doubleValue;
                TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion


}
