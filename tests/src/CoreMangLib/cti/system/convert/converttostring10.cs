// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Convert.ToString(System.Decimal,System.IFormatProvider)
/// </summary>
public class ConvertToString10
{
    public static int Main()
    {
        ConvertToString10 testObj = new ConvertToString10();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Decimal)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is a random positive Decimal and IFormatProvider is en-US CultureInfo ... ";
        string c_TEST_ID = "P001";

        Random rand = new Random(-55);
        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = true;
        Byte scale = (byte)rand.Next(0, 27);
        Decimal decimalValue = new Decimal(low, mid, hi, isNagetive, scale);

        IFormatProvider provider = new CultureInfo("en-US");

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue ;
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
        string c_TEST_DESC = "PosTest2: Verify value is a random negative Decimal... ";
        string c_TEST_ID = "P002";

        Random rand = new Random(-55);
        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = false;
        Byte scale = (byte)rand.Next(0, 27);
        Decimal decimalValue = new Decimal(low, mid, hi, isNagetive, scale);

        IFormatProvider provider = new CultureInfo("en-US");

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue;
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
        string c_TEST_DESC = "PosTest3: Verify value is Decimal.MaxValue... ";
        string c_TEST_ID = "P003";


        Decimal decimalValue = Decimal.MaxValue;
        IFormatProvider provider = new CultureInfo("en-US");

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue;
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
        string c_TEST_DESC = "PosTest4: Verify value is Decimal.MinValue... ";
        string c_TEST_ID = "P004";


        Decimal decimalValue = Decimal.MinValue;
        IFormatProvider provider = new CultureInfo("en-US");

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue;
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
        string c_TEST_DESC = "PosTest5: Verify value is 0... ";
        string c_TEST_ID = "P005";


        Decimal decimalValue = Decimal.Zero;
        IFormatProvider provider = new CultureInfo("en-US");

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue;
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
        string c_TEST_DESC = "PosTest6: Verify value is a random Decimal and IFormatProvider is fr-FR CultureInfo ... ";
        string c_TEST_ID = "P006";

        Random rand = new Random(-55);
        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = true;
        Byte scale = (byte)rand.Next(0, 27);
        Decimal decimalValue = new Decimal(low, mid, hi, isNagetive, scale);

        IFormatProvider provider = new CultureInfo("fr-FR");

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue,provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue;
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
        string c_TEST_DESC = "PosTest7: Verify value is a random Decimal and IFormatProvider is nl-NL CultureInfo ... ";
        string c_TEST_ID = "P007";

        Random rand = new Random(-55);
        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = true;
        Byte scale = (byte)rand.Next(0, 27);
        Decimal decimalValue = new Decimal(low, mid, hi, isNagetive, scale);

        IFormatProvider provider = new CultureInfo("nl-NL");

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue;
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
        string c_TEST_DESC = "PosTest8: Verify value is a random positive Decimal and IFormatProvider is a null reference... ";
        string c_TEST_ID = "P008";

        Random rand = new Random(-55);
        int low = TestLibrary.Generator.GetInt32(-55);
        int mid = TestLibrary.Generator.GetInt32(-55);
        int hi = TestLibrary.Generator.GetInt32(-55);
        bool isNagetive = true;
        Byte scale = (byte)rand.Next(0, 27);
        Decimal decimalValue = new Decimal(low, mid, hi, isNagetive, scale);

        IFormatProvider provider = null;

        String actualValue = decimalValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(decimalValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue.ToString() + " as expected: Actual is " + actualValue.ToString();
                errorDesc += "\n decimal value is " + decimalValue;
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
