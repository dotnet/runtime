// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// Convert.ToString(System.Int64,System.IFormatProvider)
/// </summary>
public class ConvertToString20
{
    public static int Main()
    {
        ConvertToString20 testObj = new ConvertToString20();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToString(System.Int64,System.IFormatProvider)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string c_TEST_DESC = "PosTest1: Verify value is a random Int64 and IFormatProvider is a null reference... ";
        string c_TEST_ID = "P001";

        Int64 intValue = TestLibrary.Generator.GetInt64(-55);
        IFormatProvider provider = null;

        String actualValue = intValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n Int64 value is " + intValue;
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
        string c_TEST_DESC = "PosTest2: Verify value is a random Int64 and IFormatProvider is en-US CultureInfo... ";
        string c_TEST_ID = "P002";

        Int64 intValue = TestLibrary.Generator.GetInt64(-55);
        IFormatProvider provider = new CultureInfo("en-US");

        String actualValue = intValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n Int64 value is " + intValue;
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
        string c_TEST_DESC = "PosTest3: Verify value is a random Int64 and IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P003";


        Int64 intValue = TestLibrary.Generator.GetInt64(-55);
        IFormatProvider provider = new CultureInfo("fr-FR");

        String actualValue = intValue.ToString(provider);
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n Int64 value is " + intValue;
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
        string c_TEST_DESC = "PosTest4: Verify value is  -64465641235 and IFormatProvider is user-defined NumberFormatInfo... ";
        string c_TEST_ID = "P004";

        Int64 intValue = -465641235;

        NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
        numberFormatInfo.NegativeSign = "minus ";
        numberFormatInfo.NumberDecimalSeparator = " point ";

        String actualValue = "minus 465641235";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue, numberFormatInfo);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
                errorDesc += "\n Int64 value is " + intValue;
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
        string c_TEST_DESC = "PosTest5: Verify value is Int64.MaxValue and IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P005";

        Int64 intValue = Int64.MaxValue;
        IFormatProvider provider = new CultureInfo("fr-FR");

        String actualValue = "9223372036854775807";
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
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
        string c_TEST_DESC = "PosTest6: Verify value is Int64.MinValue and IFormatProvider is fr-FR CultureInfo... ";
        string c_TEST_ID = "P006";

        Int64 intValue = Int64.MinValue;
        IFormatProvider provider = new CultureInfo("fr-FR");

        String actualValue = "-9223372036854775808";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            String resValue = Convert.ToString(intValue, provider);
            if (actualValue != resValue)
            {
                string errorDesc = "value is not " + resValue + " as expected: Actual is " + actualValue;
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
    #endregion
}
